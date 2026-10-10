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
/// ردیف فهرست انبار — «در سطح قلم» نه بچ — چرخهٔ چهل‌وسوم (گزارش مالک:
/// «لیست اقلام مصرفی نمایش داده نمیشه و همچنین لیست داروها هم نمایش
/// داده نمیشه»): فهرست قبلی فقط بچ‌های واردشده را برمی‌گرداند و قلمِ
/// تعریف‌شدهٔ بدون ورود کالا (موجودی صفر) هرگز دیده نمی‌شد؛ اکنون هر ردیف
/// یک «قلم» است با جمع موجودی همهٔ بچ‌ها، قیمت‌های آخرین بچ و انقضای
/// نزدیک‌ترین بچ — قلم‌های بی‌بچ هم با موجودی صفر می‌آیند.
/// </summary>
public record InventoryItemDto(
    // شناسهٔ «قلم» (Medication) — نه شناسهٔ بچ
    long ItemId,
    // نام نمایشی قلم (فارسی یا ژنریک)
    string MedicationName,
    // نوع قلم — ۱ دارو، ۲ لوازم مصرفی
    int ItemKind,
    // متن فارسی نوع — «دارو» یا «لوازم مصرفی»
    string KindText,
    // جمع موجودی همهٔ بچ‌های سالم این قلم
    decimal TotalQuantity,
    // بیشینهٔ نقطهٔ سفارش بین بچ‌های قلم (صفر = بدون تنظیم)
    decimal ReorderLevel,
    // تاریخ انقضای نزدیک‌ترین بچ تاره (شمسی) — تهی یعنی بدون بچ/بدون انقضا
    string? NearestExpiryShamsi,
    // جمع موجودی زیر نقطهٔ سفارش است؟ (قلم بی‌بچ هرگز «کمبود» نیست)
    bool IsLowStock,
    // نزدیک‌ترین انقضا در ۹۰ روز آینده است؟
    bool IsExpiringSoon,
    // قیمت فروش آخرین بچ تازهٔ قلم — صفر برای قلم بی‌بچ
    decimal SellPrice,
    // قیمت خرید آخرین بچ تازهٔ قلم — صفر برای قلم بی‌بچ
    decimal PurchasePrice,
    // شمار بچ‌های ثبت‌شدهٔ این قلم
    int BatchCount);

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
/// فرمان ورود موجودی جدید (خرید) — چرخهٔ ۴۳: پارامتر ExpectedItemKind برای
/// قفل‌کردن «نوع قلم» به بافت فراخواننده اضافه شد: مسیر مصرفی‌ها فقط قلم
/// مصرفی می‌پذیرد و داروخانه فقط دارو — جلوگیری از ورود کالای دارویی به
/// انبار مصرفی‌ها بر اثر جستجوی اشتباه (گزارش مالک: «چون اقلام مصرفی دیگه
/// از داروخانه مستقل شده… فقط دارو در اونجا ثبت میشه»).
/// </summary>
public record StockInCommand(
    long MedicationId, string? BatchNumber, decimal Quantity,
    decimal PurchasePrice, decimal SellPrice,
    string? ExpiryDateShamsi, long? SupplierId,
    // چرخهٔ ۴۳ — نوع موردانتظار قلم؛ تهی = بدون قید (سازگار رو به عقب)
    MedicationItemKind? ExpectedItemKind = null) : IRequest<Result<long>>;

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

    /// <summary>
    /// اجرای ورود موجودی — چرخهٔ ۴۳ دو اصلاح ساختاری:
    /// ۱) اعتبارسنجی صریح وجود قلم و تطابق «نوع» با بافت فراخواننده
    ///    (مصرفی‌ها فقط قلم مصرفی؛ داروخانه فقط دارو) — پیش‌تر شناسهٔ نامعتبر
    ///    به‌جای پیام فارسی به نقض کلید خارجی و خطای ۵۰۰ می‌رسید؛
    /// ۲) اتصال تراکنش انبار از طریق «ناوبری» به‌جای FK عددیِ مقدارِ موقت —
    ///    الگوی مستند EF Core برای روابط پیش از ذخیره (انتشار خودکار کلید).
    /// </summary>
    public async Task<Result<long>> Handle(StockInCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // ═══ چرخهٔ ۴۳ — گارد وجود قلم و تطابق نوع با بافت ═══
        var medication = await _unitOfWork.Repository<Medication>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, cancellationToken);
        if (medication is null)
            return Result<long>.Failure(Error.NotFound("قلم انبار", request.MedicationId));
        if (request.ExpectedItemKind.HasValue && medication.ItemKind != request.ExpectedItemKind.Value)
            return Result<long>.Failure(new Error("PHARMACY.STOCKIN.KIND",
                request.ExpectedItemKind == MedicationItemKind.Consumable
                    ? $"«{medication.GenericNameFa ?? medication.GenericName}» داروست نه لوازم مصرفی — ورود کالای داروها از صفحهٔ داروخانه انجام می‌شود"
                    : $"«{medication.GenericNameFa ?? medication.GenericName}» لوازم مصرفی است نه دارو — ورود کالای مصرفی‌ها از صفحهٔ «لوازم مصرفی» انجام می‌شود"));

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

        // ═══ چرخهٔ ۴۳ — ناوبری به‌جای FK عددی موقت ═══
        // پیش‌تر «InventoryId = inventory.Id» مقدار موقت (منفی) EF را کپی می‌کرد و
        // منتظر انتشار خودکار کلید می‌ماندیم؛ انتساب مستقیم مرجع ناوبری همان
        // نتیجه را با الگوی رسمیِ مستند EF می‌دهد و از هر ابهامی در انتشار
        // مقدار موقت بی‌نیاز می‌کند.
        await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
        {
            Inventory = inventory,
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
/// پرس‌وجوی موجودی انبار «در سطح قلم» — چرخهٔ ۴۳ بازطراحی کامل:
/// فهرست قبلی بچ‌محور بود و قلمِ تعریف‌شدهٔ بدون ورود کالا هرگز دیده نمی‌شد
/// (ریشهٔ گزارش مالک: «لیست اقلام مصرفی نمایش داده نمیشه و لیست داروها هم
/// نمایش داده نمیشه» — قلم ساخته می‌شد ولی چون ورود کالایش شکست می‌خورد،
/// فهرست همیشه خالی می‌ماند). اکنون هر ردیف یک قلم است؛ جمع موجودی و
/// قیمت‌ها و انقضای نزدیک‌ترین بچ تجمیع می‌شود و اقلام بی‌بچ با مقدار صفر
/// می‌آیند تا همیشه «کاتالوگ کامل انبار» دیده شود.
/// </summary>
/// <param name="LowStock">فقط اقلام زیر نقطهٔ سفارش</param>
/// <param name="ItemKind">فیلتر نوع قلم — دارو (۱)، لوازم مصرفی (۲) یا هر دو (تهی)</param>
public record GetInventoryQuery(bool? LowStock = null, MedicationItemKind? ItemKind = null)
    : IRequest<Result<List<InventoryItemDto>>>;

/// <summary>گرداننده فهرست موجودی قلم‌محور</summary>
public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, Result<List<InventoryItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInventoryQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// اجرای پرس‌وجو — دو کوئری جدا (اقلام + بچ‌های آن‌ها) و تجمیع در حافظه؛
    /// بازگشت مقادیر تجمیعی (Sum/Min/Max) در یک کوئری SQL نیازمند GroupBy روی
    /// ناوبری است که به بازنویسی پیچیده می‌انجامد — الگوی مستقر پروژه:
    /// خواندن ساده + محاسبه در حافظه.
    /// </summary>
    public async Task<Result<List<InventoryItemDto>>> Handle(GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soon = today.AddDays(90);

        // ═══ گام ۱ — شناسهٔ قلم‌های کاندید ═══
        // حالت کمبود: از بچ‌های زیر نقطهٔ سفارش شروع می‌کنیم تا Take(200)
        // پیش از پالایش کمبود اعمال نشود و اقلام کمبودِ فراتر از مرز جا نمانند
        List<long> candidateIds;
        if (request.LowStock == true)
        {
            candidateIds = await _unitOfWork.Repository<PharmacyInventory>().QueryableNoTracking
                .Where(i => !i.IsDeleted && i.Medication != null && !i.Medication.IsDeleted
                            && i.Medication.IsActive
                            && i.Medication.ItemKind == (request.ItemKind ?? i.Medication.ItemKind)
                            && i.ReorderLevel > 0 && i.Quantity <= i.ReorderLevel)
                .Select(i => i.MedicationId)
                .Distinct()
                .OrderBy(id => id)
                .Take(200)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var medsQuery = _unitOfWork.Repository<Medication>().QueryableNoTracking
                .Where(m => !m.IsDeleted && m.IsActive);
            if (request.ItemKind.HasValue)
                medsQuery = medsQuery.Where(m => m.ItemKind == request.ItemKind.Value);

            candidateIds = await medsQuery
                .OrderBy(m => m.GenericNameFa ?? m.GenericName)
                .Select(m => m.Id)
                .Take(200)
                .ToListAsync(cancellationToken);
        }

        if (candidateIds.Count == 0)
            return Result<List<InventoryItemDto>>.Success(new List<InventoryItemDto>());

        // ═══ گام ۲ — قلم‌های کاندید ═══
        var meds = await _unitOfWork.Repository<Medication>().QueryableNoTracking
            .Where(m => candidateIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                Name = m.GenericNameFa ?? m.GenericName,
                Kind = (int)m.ItemKind
            })
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        // ═══ گام ۳ — بچ‌های فعال این قلم‌ها (تجمیع در حافظه) ═══
        var batches = await _unitOfWork.Repository<PharmacyInventory>().QueryableNoTracking
            .Where(i => !i.IsDeleted && candidateIds.Contains(i.MedicationId))
            .Select(i => new
            {
                i.MedicationId, i.Quantity, i.ReorderLevel, i.ExpiryDate,
                i.SellPrice, i.PurchasePrice, i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // ═══ گام ۴ — ساخت ردیف‌های قلم‌محور ═══
        var rows = new List<InventoryItemDto>();
        foreach (var id in candidateIds)
        {
            if (!meds.TryGetValue(id, out var med)) continue;

            var own = batches.Where(b => b.MedicationId == id).ToList();
            var totalQuantity = own.Sum(b => b.Quantity);
            var reorderLevel = own.Count > 0 ? own.Max(b => b.ReorderLevel) : 0m;

            // نزدیک‌ترین انقضای آینده — بچ منقضی برای هشدار معتبر نیست
            var nearestExpiry = own
                .Where(b => b.ExpiryDate.HasValue)
                .Select(b => b.ExpiryDate!.Value)
                .DefaultIfEmpty()
                .Min();
            var hasExpiry = own.Any(b => b.ExpiryDate.HasValue);

            // قیمت‌های مرجع: آخرین بچ ثبت‌شده (تازه‌ترین خرید)
            var latest = own.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

            var row = new InventoryItemDto(
                id, med.Name, med.Kind,
                med.Kind == (int)MedicationItemKind.Consumable ? "لوازم مصرفی" : "دارو",
                totalQuantity, reorderLevel,
                hasExpiry
                    ? Application.Common.Helpers.PersianDateHelper.ToShamsi(nearestExpiry.ToDateTime(TimeOnly.MinValue))
                    : null,
                reorderLevel > 0 && totalQuantity <= reorderLevel,
                hasExpiry && nearestExpiry <= soon,
                latest?.SellPrice ?? 0m,
                latest?.PurchasePrice ?? 0m,
                own.Count);
            rows.Add(row);
        }

        // حافظ محفوظات: در حالت کمبود، گام ۱ کاندیدها را از پیش پالایش کرده
        return Result<List<InventoryItemDto>>.Success(
            rows.OrderBy(r => r.MedicationName, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
