using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Pharmacy;

/// <summary>
/// گزینهٔ قلم مصرفی برای پذیرش — موجودی کل و قیمت پیشنهادی فروش
/// </summary>
/// <param name="MedicationId">شناسهٔ قلم</param>
/// <param name="Name">نام نمایشی (فارسی یا ژنریک)</param>
/// <param name="Form">فرم/جنس — مثلاً «ست تزریقی»</param>
/// <param name="Strength">اندازه — مثلاً «۵cc»</param>
/// <param name="TotalQuantity">جمع موجودی همهٔ بچ‌های سالم</param>
/// <param name="SellPrice">قیمت فروش هر واحد — از بچِ دارای موجودی، نزدیک‌ترین به میانگین</param>
public record ConsumableOptionDto(
    long MedicationId,
    string Name,
    string? Form,
    string? Strength,
    decimal TotalQuantity,
    decimal SellPrice);

/// <summary>
/// پرس‌وجوی اقلام مصرفی در دسترس — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// فهرستِ تجمیعیِ لوازم مصرفیِ فعالِ دارای موجودی برای بخش «لوازم استفاده‌شده»
/// فرم پذیرش: بچ‌های هر قلم جمع می‌شوند و قیمت فروش از بچِ مرجع (نزدیک‌ترین
/// به میانگین وزنی قیمت بچ‌های دارای موجودی) پیشنهاد می‌شود؛ قیمت واقعی هنگام
/// مصرف در فرمان ConsumeStock به تفکیک بچ محاسبه می‌شود.
/// </remarks>
public record GetConsumablesQuery(string? Search = null) : IRequest<Result<List<ConsumableOptionDto>>>;

/// <summary>گردانندهٔ فهرست اقلام مصرفی</summary>
public class GetConsumablesQueryHandler : IRequestHandler<GetConsumablesQuery, Result<List<ConsumableOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetConsumablesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — تجمیع بچ‌ها به تفکیک قلم مصرفی</summary>
    public async Task<Result<List<ConsumableOptionDto>>> Handle(GetConsumablesQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var search = request.Search?.Trim();

        // فقط بچ‌های دارای موجودی و سالم (تهی یا آیندهٔ انقضا)؛ بچ منقضی هرگز
        // به بیمار تعلق نمی‌گیرد — همان قاعدهٔ تحویل نسخه (FEFO)
        IQueryable<PharmacyInventory> query = _unitOfWork.Repository<PharmacyInventory>()
            .QueryableNoTracking
            .Include(i => i.Medication)
            .Where(i => i.Quantity > 0
                && !i.IsDeleted
                && (i.ExpiryDate == null || i.ExpiryDate >= today)
                && i.Medication != null
                && i.Medication.ItemKind == MedicationItemKind.Consumable
                && !i.Medication.IsDeleted
                && i.Medication.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                (i.Medication!.GenericNameFa != null && i.Medication.GenericNameFa.Contains(search)) ||
                i.Medication!.GenericName.Contains(search));

        var rows = await query
            .OrderBy(i => i.Medication!.GenericNameFa ?? i.Medication.GenericName)
            .Take(200)
            .Select(i => new
            {
                i.MedicationId,
                Name = i.Medication!.GenericNameFa ?? i.Medication.GenericName,
                i.Medication.Form,
                i.Medication.Strength,
                i.Quantity,
                i.SellPrice
            })
            .ToListAsync(cancellationToken);

        // تجمیع در حافظه — گروه‌بندی روی مقدار محاسبه‌شدهٔ نام در SQL باید در
        // حافظه انجام شود (الگوی مستقر در پروژه برای نگاشت‌های پیچیده)
        var items = rows
            .GroupBy(r => r.MedicationId)
            .Select(g => new ConsumableOptionDto(
                g.Key,
                g.First().Name,
                g.First().Form,
                g.First().Strength,
                g.Sum(x => x.Quantity),
                // قیمت پیشنهادی: بچ پرموجودی‌ترین سهم — عدد نزدیک به آنچه واقعاً
                // هنگام مصرف از بچ‌ها حساب می‌شود
                g.OrderByDescending(x => x.Quantity).First().SellPrice))
            .ToList();

        return Result<List<ConsumableOptionDto>>.Success(items);
    }
}

/// <summary>ردیف مصرف لوازم برای یک پذیرش</summary>
/// <param name="MedicationId">شناسهٔ قلم مصرفی</param>
/// <param name="Quantity">تعداد مصرف‌شده</param>
public record ConsumeItemDto(long MedicationId, decimal Quantity);

/// <summary>قلم مصرف‌شدهٔ واقعی با قیمت محاسبه‌شده از بچ‌ها</summary>
public record ConsumeResultItemDto(
    long MedicationId,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

/// <summary>رسید مصرف لوازم — جمع هزینه برای افزودن به فاکتور بیمار</summary>
public record ConsumeStockResultDto(
    long ReceptionId,
    string ReceptionNumber,
    List<ConsumeResultItemDto> Items,
    decimal TotalCost);

/// <summary>
/// فرمان مصرف لوازم درمانگاه در پذیرش خدمتی — چرخهٔ سی‌ونه‌م (گزارش مالک)
/// </summary>
/// <remarks>
/// «وقتی بیمار با خودش نمیاره، باید از لوازم خود درمانگاه استفاده بکنیم و
/// در پذیرش برای اونها هم هزینه به هزینه‌های بیمار اضافه بشه» — این فرمان
/// موجودی را به ترتیب نزدیک‌ترین انقضا (FEFO — همان الگوی تحویل نسخه) کم
/// می‌کند، برای هر برداشت تراکنش انبار با منشأ Reception ثبت می‌کند (ردیابی
/// کامل «چه چیزی برای کدام پذیرش مصرف شد») و فهرست اقلام با قیمت واقعی
/// بچ‌ها + جمع کل را برمی‌گرداند تا فراخواننده آن را به فاکتور بیمار
/// بیفزاید. کمبود موجودی قبل از هر ذخیره رد می‌شود (تراکنش اتمیک واحد کار).
/// </remarks>
public record ConsumeStockCommand(
    // شناسهٔ پذیرشِ مصرف‌کننده — مبنای منشأ تراکنش‌ها
    // [نکتهٔ CS1587 چرخهٔ ۳۶: کامنت روی پارامترهای پرانتزی رکورد باید عادی باشد]
    long ReceptionId,
    // فهرست اقلام مصرف‌شده
    List<ConsumeItemDto> Items,
    // شناسهٔ کاربر انجام‌دهنده — از توکن پر می‌شود؛ برای رد پایهٔ تراکنش
    long? UserId = null) : IRequest<Result<ConsumeStockResultDto>>;

/// <summary>گردانندهٔ مصرف لوازم</summary>
public class ConsumeStockCommandHandler : IRequestHandler<ConsumeStockCommand, Result<ConsumeStockResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsumeStockCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار و لاگر (چرخهٔ ۶۷ — سند حسابداری مصرف)</summary>
    public ConsumeStockCommandHandler(IUnitOfWork unitOfWork, ILogger<ConsumeStockCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای مصرف — FEFO + تراکنش + هشدار کمبود + خروجی فاکتوری</summary>
    public async Task<Result<ConsumeStockResultDto>> Handle(ConsumeStockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return Result<ConsumeStockResultDto>.Failure(new Error("PHARMACY.CONSUME.EMPTY",
                "فهرست اقلام مصرفی خالی است"));

        if (request.Items.Any(i => i.Quantity <= 0))
            return Result<ConsumeStockResultDto>.Failure(new Error("PHARMACY.CONSUME.QTY",
                "تعداد هر قلم مصرفی باید بیشتر از صفر باشد"));

        // پذیرش باید واقعی باشد — منشأ تراکنش‌ها به آن گره می‌خورد
        var reception = await _unitOfWork.Repository<Reception>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && !r.IsDeleted, cancellationToken);
        if (reception is null)
            return Result<ConsumeStockResultDto>.Failure(Error.NotFound("پذیرش", request.ReceptionId));

        var now = DateTime.UtcNow;
        var resultItems = new List<ConsumeResultItemDto>();
        // ═══ چرخهٔ ۶۷ — جمع بهای تمام‌شدهٔ اقلام مصرفی (قیمت خرید × تعداد) ═══
        // مبنای سند حسابداری هزینه؛ قیمت فروش (taken) فقط برای نمایش خروجی است.
        var totalCost = 0m;

        foreach (var item in request.Items)
        {
            // نام قلم برای پیام خطا و خروجی
            var medication = await _unitOfWork.Repository<Domain.Entities.Clinical.Medication>()
                .QueryableNoTracking
                .FirstOrDefaultAsync(m => m.Id == item.MedicationId && !m.IsDeleted, cancellationToken);
            if (medication is null)
                return Result<ConsumeStockResultDto>.Failure(
                    Error.NotFound("قلم مصرفی", item.MedicationId));

            // بچ‌های سالم این قلم به ترتیب نزدیک‌ترین انقضا (FEFO)
            var batches = await _unitOfWork.Repository<PharmacyInventory>()
                .QueryableTracking
                .Where(inv => inv.MedicationId == item.MedicationId
                           && inv.Quantity > 0
                           && !inv.IsDeleted
                           && (inv.ExpiryDate == null || inv.ExpiryDate >= DateOnly.FromDateTime(now)))
                .OrderBy(inv => inv.ExpiryDate)
                .ToListAsync(cancellationToken);

            var remaining = item.Quantity;
            var taken = 0m; // جمع هزینهٔ واقعی این قلم از بچ‌ها

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var take = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= take;
                remaining -= take;
                taken += take * batch.SellPrice;
                // چرخهٔ ۶۷: بهای تمام‌شده (خرید) — نه قیمت فروش؛ حسابداری هزینه
                // باید به مبنای خرید باشد تا سود خالص واقعی باشد
                totalCost += take * batch.PurchasePrice;

                await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
                {
                    InventoryId = batch.Id,
                    TransactionType = PharmacyTransactionType.StockOut,
                    Quantity = take,
                    UnitPrice = batch.SellPrice,
                    TotalPrice = take * batch.SellPrice,
                    TransactionDate = now,
                    ReferenceType = PharmacyReferenceType.Reception,
                    ReferenceId = reception.Id,
                    UserId = request.UserId,
                    Notes = $"مصرف در پذیرش {reception.ReceptionNumber}"
                }, cancellationToken);

                // هشدار کمبود — همان رویداد دامنهٔ تحویل نسخه؛ نقطهٔ سفارش مجدد
                if (batch.Quantity <= batch.ReorderLevel)
                {
                    batch.AddDomainEvent(new LowStockAlertEvent(
                        batch.Id, batch.MedicationId,
                        medication.GenericNameFa ?? medication.GenericName,
                        batch.Quantity, batch.ReorderLevel));
                }
            }

            if (remaining > 0)
                return Result<ConsumeStockResultDto>.Failure(new Error("PHARMACY.CONSUME.STOCK",
                    $"موجودی «{medication.GenericNameFa ?? medication.GenericName}» کافی نیست — {remaining} عدد کمبود دارد؛ ابتدا از صفحهٔ «لوازم مصرفی» ورود کالا ثبت کنید"));

            resultItems.Add(new ConsumeResultItemDto(
                medication.Id,
                medication.GenericNameFa ?? medication.GenericName,
                item.Quantity,
                item.Quantity > 0 ? taken / item.Quantity : 0m,
                taken));
        }

        // ═══ چرخهٔ ۶۷ (گزارش مالک: «در تب سود و زیان مبلغ هزینه‌ها صفر است») ═══
        // سند حسابداری هزینهٔ مصرف — بدهکار 5102 هزینهٔ خرید دارو و مواد /
        // بستانکار 1104 موجودی انبار — پیش از ذخیرهٔ اصلی در همان تراکنش می‌نشیند؛
        // شناسهٔ پذیرش واقعی است (رکورد موجود) پس مرجع سند درست می‌ماند.
        await PharmacyJournalPoster.PostConsumptionAsync(
            _unitOfWork, _logger, reception.Id,
            reception.ReceptionNumber ?? reception.Id.ToString(),
            totalCost, cancellationToken);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<ConsumeStockResultDto>.Success(new ConsumeStockResultDto(
            reception.Id,
            reception.ReceptionNumber ?? "",
            resultItems,
            resultItems.Sum(x => x.TotalPrice)));
    }
}
