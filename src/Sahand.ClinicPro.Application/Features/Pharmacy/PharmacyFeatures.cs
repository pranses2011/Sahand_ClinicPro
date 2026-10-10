using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Pharmacy;

/// <summary>
/// ردیف موجودی داروخانه — چرخهٔ ۳۹: نوع قلم (دارو/لوازم مصرفی) هم برمی‌گردد
/// تا تب «موجودی» داروخانه بتواند هر دسته را جدا فیلتر/برچسب کند
/// </summary>
public record InventoryListDto(
    long Id, string MedicationName, string? BatchNumber,
    decimal Quantity, decimal ReorderLevel, string? ExpiryDateShamsi,
    bool IsLowStock, bool IsExpiringSoon,
    // نوع قلم — ۱ دارو، ۲ لوازم مصرفی (چرخهٔ ۳۹) — کامنت عادی (درس CS1587)
    int ItemKind = 1,
    // متن فارسی نوع — «دارو» یا «لوازم مصرفی»
    string KindText = "دارو",
    // قیمت فروش هر واحد این بچ — چرخهٔ ۴۱: صفحهٔ مستقل لوازم مصرفی برای
    // نمایش ارزش انبار و قیمت‌گذاری مصرف می‌خواند (افزایشی؛ داروخانه نادیده می‌گیرد)
    decimal SellPrice = 0,
    // قیمت خرید هر واحد این بچ — همان‌طور افزایشی برای صفحهٔ چرخهٔ ۴۱
    decimal PurchasePrice = 0);

/// <summary>ردیف فهرست اقلام نسخه برای تحویل</summary>
public record PrescriptionItemRowDto(
    long ItemId, string MedicationName, string? Dosage, string? Frequency,
    decimal Quantity, string? Unit, bool IsDelivered);

/// <summary>
/// فرمان تحویل نسخه — کاهش موجودی بچ‌ها به ترتیب انقضا + تراکنش + هشدار کمبود
/// </summary>
public record DispensePrescriptionCommand(long PrescriptionId) : IRequest<Result>;

/// <summary>گرداننده تحویل نسخه</summary>
public class DispensePrescriptionCommandHandler : IRequestHandler<DispensePrescriptionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DispensePrescriptionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای تحویل نسخه</summary>
    public async Task<Result> Handle(DispensePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _unitOfWork.Repository<Prescription>()
            .QueryableTracking
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return Result.Failure(Error.NotFound("نسخه", request.PrescriptionId));

        if (prescription.Items.All(i => i.IsDelivered))
            return Result.Failure(new Error("ALREADY_DELIVERED", "این نسخه قبلاً تحویل شده است"));

        var now = DateTime.UtcNow;

        foreach (var item in prescription.Items.Where(i => !i.IsDelivered))
        {
            // موجودی بچ‌های این دارو به ترتیب نزدیک‌ترین انقضا
            var batches = await _unitOfWork.Repository<PharmacyInventory>()
                .QueryableTracking
                .Where(inv => inv.MedicationId == item.MedicationId
                           && inv.Quantity > 0
                           && (inv.ExpiryDate == null || inv.ExpiryDate >= DateOnly.FromDateTime(now)))
                .OrderBy(inv => inv.ExpiryDate)
                .ToListAsync(cancellationToken);

            var remaining = item.Quantity;

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var take = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= take;
                remaining -= take;

                await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
                {
                    InventoryId = batch.Id,
                    TransactionType = PharmacyTransactionType.Dispense,
                    Quantity = take,
                    UnitPrice = batch.SellPrice,
                    TotalPrice = take * batch.SellPrice,
                    TransactionDate = now,
                    ReferenceType = PharmacyReferenceType.Prescription,
                    ReferenceId = prescription.Id,
                    PrescriptionId = prescription.Id,
                    Notes = "تحویل نسخه"
                }, cancellationToken);

                // هشدار کمبود موجودی — رویداد دامنه برای نوتیفیکیشن
                if (batch.Quantity <= batch.ReorderLevel)
                {
                    batch.AddDomainEvent(new LowStockAlertEvent(
                        batch.Id, batch.MedicationId,
                        item.Medication?.GenericNameFa ?? item.Medication?.GenericName ?? "دارو",
                        batch.Quantity, batch.ReorderLevel));
                }
            }

            if (remaining > 0)
                return Result.Failure(new Error("INSUFFICIENT_STOCK",
                    $"موجودی داروی {item.Medication?.GenericNameFa} کافی نیست — {remaining} عدد کمبود"));

            item.IsDelivered = true;
            item.DeliveredAt = now;
            item.DeliveredQuantity = item.Quantity;
        }

        prescription.Status = PrescriptionStatus.Delivered;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// فرمان ورود موجودی جدید (خرید)
/// </summary>
public record StockInCommand(
    long MedicationId, string? BatchNumber, decimal Quantity,
    decimal PurchasePrice, decimal SellPrice,
    string? ExpiryDateShamsi, long? SupplierId) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ورود موجودی</summary>
public class StockInCommandValidator : AbstractValidator<StockInCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public StockInCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0).WithMessage("انتخاب دارو الزامی است");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
        RuleFor(x => x.SellPrice).GreaterThanOrEqualTo(0).WithMessage("قیمت فروش نامعتبر است");
    }
}

/// <summary>گرداننده ورود موجودی — رکورد بچ جدید یا افزایش بچ موجود</summary>
public class StockInCommandHandler : IRequestHandler<StockInCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public StockInCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ورود موجودی</summary>
    public async Task<Result<long>> Handle(StockInCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiry = Application.Common.Helpers.PersianDateHelper.ToGregorian(request.ExpiryDateShamsi);

        var inventory = new PharmacyInventory
        {
            MedicationId = request.MedicationId,
            BatchNumber = request.BatchNumber,
            Quantity = request.Quantity,
            PurchasePrice = request.PurchasePrice,
            SellPrice = request.SellPrice,
            ExpiryDate = expiry.HasValue ? DateOnly.FromDateTime(expiry.Value) : null,
            SupplierId = request.SupplierId,
            LastPurchaseDate = DateOnly.FromDateTime(now),
            IsActive = true
        };

        await _unitOfWork.Repository<PharmacyInventory>().AddAsync(inventory, cancellationToken);

        await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
        {
            InventoryId = inventory.Id,
            TransactionType = PharmacyTransactionType.StockIn,
            Quantity = request.Quantity,
            UnitPrice = request.PurchasePrice,
            TotalPrice = request.Quantity * request.PurchasePrice,
            TransactionDate = now,
            ReferenceType = PharmacyReferenceType.Manual,
            Notes = "ورود موجودی"
        }, cancellationToken);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(inventory.Id);
    }
}

/// <summary>
/// پرس‌وجوی موجودی داروخانه با هشدار کمبود و انقضا
/// </summary>
/// <param name="LowStock">فقط اقلام کمبود</param>
/// <param name="ItemKind">فیلتر نوع قلم — چرخهٔ ۳۹: دارو (۱)، لوازم مصرفی (۲) یا هر دو (تهی)</param>
public record GetInventoryQuery(bool? LowStock = null, MedicationItemKind? ItemKind = null)
    : IRequest<Result<List<InventoryListDto>>>;

/// <summary>گرداننده فهرست موجودی</summary>
public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, Result<List<InventoryListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInventoryQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<InventoryListDto>>> Handle(GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(90);

        IQueryable<PharmacyInventory> query = _unitOfWork.Repository<PharmacyInventory>()
            .QueryableNoTracking
            .Include(i => i.Medication);

        if (request.LowStock == true)
            query = query.Where(i => i.Quantity <= i.ReorderLevel);

        // چرخهٔ ۳۹ — فیلتر نوع قلم: تب موجودی داروخانه بتواند فقط دارو یا فقط
        // لوازم مصرفی را نشان دهد
        if (request.ItemKind.HasValue)
            query = query.Where(i => i.Medication!.ItemKind == request.ItemKind.Value);

        var items = await query
            .OrderBy(i => i.Medication!.GenericName)
            .Take(200)
            .Select(i => new InventoryListDto(
                i.Id,
                i.Medication != null ? i.Medication.GenericNameFa ?? i.Medication.GenericName : "",
                i.BatchNumber,
                i.Quantity, i.ReorderLevel,
                i.ExpiryDate.HasValue
                    ? Application.Common.Helpers.PersianDateHelper.ToShamsi(i.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue))
                    : null,
                i.Quantity <= i.ReorderLevel,
                i.ExpiryDate.HasValue && i.ExpiryDate.Value <= soon,
                // چرخهٔ ۳۹ — نوع قلم + متن فارسی
                (int)i.Medication!.ItemKind,
                i.Medication.ItemKind == MedicationItemKind.Consumable ? "لوازم مصرفی" : "دارو",
                // چرخهٔ ۴۱ — قیمت‌های هر بچ برای صفحهٔ مستقل لوازم مصرفی (افزایشی)
                i.SellPrice,
                i.PurchasePrice))
            .ToListAsync(cancellationToken);

        return Result<List<InventoryListDto>>.Success(items);
    }
}
