using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Pharmacy;

/* ═══════════════════════════════════════════════════════════
   ماژول انبارگردانی داروخانه — گام P2 (چرخه ششم v1.4)
   چرخه سه‌مرحله‌ای: شروع (عکس فوری موجودی) ← شمارش تدریجی ←
   تکمیل ← تأیید نهایی با اعمال خودکار مغایرت به موجودی
   ═══════════════════════════════════════════════════════════ */

/// <summary>ردیف فهرست جلسات انبارگردانی</summary>
public record StocktakingListDto(
    long Id, string? TakingNumber, string DateShamsi,
    string Status, string StatusColor, int TotalItems, int CountedItems,
    int DifferenceItems, decimal TotalShortage, decimal TotalSurplus, string? Notes,
    // چرخهٔ ۴۳ — دامنهٔ جلسه: «داروها» / «لوازم مصرفی» / «کل انبار»
    string ItemKindText = "کل انبار");

/// <summary>ردیف قلم انبارگردانی با مغایرت رنگی</summary>
public record StocktakingItemDto(
    long ItemId, long InventoryId, string MedicationName, string? BatchNumber,
    decimal SystemQuantity, decimal CountedQuantity, decimal Difference,
    bool IsCounted, string? Notes);

/// <summary>جزئیات کامل جلسه</summary>
public record StocktakingDetailDto(
    long Id, string? TakingNumber, string DateShamsi, string Status, string StatusColor,
    int TotalItems, int CountedItems, int DifferenceItems,
    decimal TotalShortage, decimal TotalSurplus, string? Notes,
    List<StocktakingItemDto> Items,
    // چرخهٔ ۴۳ — دامنهٔ جلسه: «داروها» / «لوازم مصرفی» / «کل انبار»
    string ItemKindText = "کل انبار");

/// <summary>
/// پرس‌وجوی فهرست جلسات انبارگردانی — جدیدترین در ابتدا
/// </summary>
public record GetStocktakingsQuery : IRequest<Result<List<StocktakingListDto>>>;

/// <summary>گرداننده فهرست جلسات</summary>
public class GetStocktakingsQueryHandler : IRequestHandler<GetStocktakingsQuery, Result<List<StocktakingListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetStocktakingsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<StocktakingListDto>>> Handle(GetStocktakingsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _unitOfWork.Repository<StockTaking>().QueryableNoTracking
            .Include(s => s.Items)
            .OrderByDescending(s => s.TakingDate)
            .ThenByDescending(s => s.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var rows = sessions.Select(s => new StocktakingListDto(
            s.Id,
            s.TakingNumber,
            PersianDateHelper.ToShamsi((s.TakingDate ?? DateOnly.FromDateTime(s.CreatedAt)).ToDateTime(TimeOnly.MinValue)),
            StatusFa(s.Status), StatusColor(s.Status),
            s.Items.Count,
            s.Items.Count(i => i.IsCounted),
            s.Items.Count(i => i.IsCounted && i.Difference != 0),
            s.Items.Where(i => i.IsCounted && i.Difference > 0).Sum(i => i.Difference),
            s.Items.Where(i => i.IsCounted && i.Difference < 0).Sum(i => -i.Difference),
            s.Notes,
            ScopeFa(s.ItemKind))).ToList();

        return Result<List<StocktakingListDto>>.Success(rows);
    }

    /// <summary>متن فارسی دامنهٔ جلسه — چرخهٔ ۴۳</summary>
    internal static string ScopeFa(MedicationItemKind? kind) => kind switch
    {
        MedicationItemKind.Drug => "داروها",
        MedicationItemKind.Consumable => "لوازم مصرفی",
        _ => "کل انبار"
    };

    internal static string StatusFa(StockTakingStatus status) => status switch
    {
        StockTakingStatus.InProgress => "در حال شمارش",
        StockTakingStatus.Completed => "تکمیل شده (در انتظار تأیید)",
        StockTakingStatus.Approved => "تأیید و اعمال شده",
        _ => status.ToString()
    };

    internal static string StatusColor(StockTakingStatus status) => status switch
    {
        StockTakingStatus.InProgress => "#FB8C00",
        StockTakingStatus.Completed => "#1E88E5",
        StockTakingStatus.Approved => "#43A047",
        _ => "#757575"
    };
}

/// <summary>
/// پرس‌وجوی جزئیات یک جلسه انبارگردانی با اقلام
/// </summary>
public record GetStocktakingDetailQuery(long Id) : IRequest<Result<StocktakingDetailDto>>;

/// <summary>گرداننده جزئیات جلسه</summary>
public class GetStocktakingDetailQueryHandler : IRequestHandler<GetStocktakingDetailQuery, Result<StocktakingDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetStocktakingDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<StocktakingDetailDto>> Handle(GetStocktakingDetailQuery request, CancellationToken cancellationToken)
    {
        var s = await _unitOfWork.Repository<StockTaking>().QueryableNoTracking
            .Include(x => x.Items).ThenInclude(i => i.Inventory).ThenInclude(inv => inv!.Medication)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (s is null)
            return Result<StocktakingDetailDto>.Failure(Error.NotFound("جلسه انبارگردانی", request.Id));

        var items = s.Items
            .OrderBy(i => i.IsCounted ? 1 : 0) // شمارش‌نشده‌ها در انتها
            .ThenBy(i => i.Inventory != null && i.Inventory.Medication != null
                ? (i.Inventory.Medication.BrandName ?? i.Inventory.Medication.GenericName) : "")
            .Select(i => new StocktakingItemDto(
                i.Id, i.InventoryId,
                i.Inventory != null && i.Inventory.Medication != null
                    ? (i.Inventory.Medication.BrandName ?? i.Inventory.Medication.GenericName) : "بچ #" + i.InventoryId,
                i.Inventory?.BatchNumber,
                i.SystemQuantity,
                i.CountedQuantity,
                i.Difference,
                i.IsCounted,
                i.Notes))
            .ToList();

        var dto = new StocktakingDetailDto(
            s.Id, s.TakingNumber,
            PersianDateHelper.ToShamsi((s.TakingDate ?? DateOnly.FromDateTime(s.CreatedAt)).ToDateTime(TimeOnly.MinValue)),
            GetStocktakingsQueryHandler.StatusFa(s.Status), GetStocktakingsQueryHandler.StatusColor(s.Status),
            items.Count,
            items.Count(i => i.IsCounted),
            items.Count(i => i.IsCounted && i.Difference != 0),
            items.Where(i => i.IsCounted && i.Difference > 0).Sum(i => i.Difference),
            items.Where(i => i.IsCounted && i.Difference < 0).Sum(i => -i.Difference),
            s.Notes, items, GetStocktakingsQueryHandler.ScopeFa(s.ItemKind));

        return Result<StocktakingDetailDto>.Success(dto);
    }
}

/// <summary>نتیجه شروع جلسه</summary>
public record StartStocktakingResultDto(long Id, string? TakingNumber, int TotalItems);

/// <summary>
/// فرمان شروع جلسه انبارگردانی — عکس فوری از تمام بچ‌های موجودی ثبت می‌شود
/// </summary>
public record StartStocktakingCommand(
    string? Notes,
    // چرخهٔ ۴۳ — دامنهٔ جلسه: تهی = کل انبار؛ ۱ داروها؛ ۲ لوازم مصرفی
    MedicationItemKind? ItemKind = null) : IRequest<Result<StartStocktakingResultDto>>;

/// <summary>اعتبارسنجی شروع</summary>
public class StartStocktakingCommandValidator : AbstractValidator<StartStocktakingCommand>
{
    /// <summary>قواعد</summary>
    public StartStocktakingCommandValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("توضیحات حداکثر ۵۰۰ حرف است");
    }
}

/// <summary>گرداننده شروع انبارگردانی</summary>
public class StartStocktakingCommandHandler : IRequestHandler<StartStocktakingCommand, Result<StartStocktakingResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public StartStocktakingCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>
    /// اجرای شروع — چرخهٔ ۴۳: تنها یک جلسهٔ بازِ «هم‌دامنه» مجاز است؛
    /// دو جلسهٔ با دامنهٔ متفاوت (مثلاً انبارگردانی داروها و انبارگردانی
    /// لوازم مصرفی) می‌توانند هم‌زمان باز باشند چون شمارش‌گرها و قفسه‌های
    /// متفاوتی را می‌شمارند و با هم تداخلی ندارند (گزارش مالک: «برای اقلام
    /// مصرفی هم انبارگردانی بزار»).
    /// </summary>
    public async Task<Result<StartStocktakingResultDto>> Handle(StartStocktakingCommand request, CancellationToken cancellationToken)
    {
        var hasOpen = await _unitOfWork.Repository<StockTaking>().QueryableNoTracking
            .AnyAsync(s => !s.IsDeleted && s.Status == StockTakingStatus.InProgress
                           && s.ItemKind == request.ItemKind, cancellationToken);
        if (hasOpen)
            return Result<StartStocktakingResultDto>.Failure(new Error("OPEN_SESSION_EXISTS",
                $"یک جلسه انبارگردانی باز برای «{GetStocktakingsQueryHandler.ScopeFa(request.ItemKind)}» در حال شمارش وجود دارد — ابتدا آن را تکمیل یا لغو کنید"));

        // عکس فوری از تمام بچ‌های دارای موجودی (حتی صفر — برای شمارش فیزیکی قفسه)؛
        // چرخهٔ ۴۳ — فقط بچ‌های اقلامِ داخل دامنهٔ جلسه
        var inventories = await _unitOfWork.Repository<PharmacyInventory>().QueryableNoTracking
            .Where(inv => !inv.IsDeleted
                          && (request.ItemKind == null || inv.Medication != null && inv.Medication.ItemKind == request.ItemKind))
            .OrderBy(inv => inv.Medication != null ? (inv.Medication.BrandName ?? inv.Medication.GenericName) : "")
            .ThenBy(inv => inv.BatchNumber)
            .Select(inv => new { inv.Id, inv.Quantity })
            .ToListAsync(cancellationToken);

        if (inventories.Count == 0)
            return Result<StartStocktakingResultDto>.Failure(new Error("NO_INVENTORY", "هیچ رکورد موجودی برای انبارگردانی وجود ندارد"));

        var session = new StockTaking
        {
            TakingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PerformedBy = _currentUser.UserId,
            Status = StockTakingStatus.InProgress,
            Notes = request.Notes,
            TakingNumber = "ST-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmm"),
            // چرخهٔ ۴۳ — دامنهٔ جلسه برای تفکیک دارو/لوازم مصرفی
            ItemKind = request.ItemKind
        };

        foreach (var inv in inventories)
            session.Items.Add(new StockTakingItem
            {
                InventoryId = inv.Id,
                SystemQuantity = inv.Quantity,
                CountedQuantity = 0m,
                IsCounted = false
            });

        await _unitOfWork.Repository<StockTaking>().AddAsync(session, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<StartStocktakingResultDto>.Success(new StartStocktakingResultDto(session.Id, session.TakingNumber, session.Items.Count));
    }
}

/// <summary>
/// فرمان ثبت شمارش یک قلم — مقدار واقعی شمارش‌شده + یادداشت اختیاری
/// </summary>
public record RecordCountCommand(long StockTakingId, long ItemId, decimal CountedQuantity, string? Notes) : IRequest<Result>;

/// <summary>اعتبارسنجی ثبت شمارش</summary>
public class RecordCountCommandValidator : AbstractValidator<RecordCountCommand>
{
    /// <summary>قواعد</summary>
    public RecordCountCommandValidator()
    {
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0).WithMessage("مقدار شمارش نمی‌تواند منفی باشد");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("یادداشت حداکثر ۵۰۰ حرف است");
    }
}

/// <summary>گرداننده ثبت شمارش</summary>
public class RecordCountCommandHandler : IRequestHandler<RecordCountCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordCountCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت شمارش قلم</summary>
    public async Task<Result> Handle(RecordCountCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Repository<StockTakingItem>().QueryableTracking
            .Include(i => i.StockTaking)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.StockTakingId == request.StockTakingId, cancellationToken);

        if (item is null)
            return Result.Failure(Error.NotFound("قلم انبارگردانی", request.ItemId));
        if (item.StockTaking!.Status != StockTakingStatus.InProgress)
            return Result.Failure(new Error("NOT_IN_PROGRESS", "این جلسه در حالت شمارش نیست — ثبت مقدار ممکن نیست"));

        item.CountedQuantity = request.CountedQuantity;
        item.IsCounted = true;
        item.Notes = request.Notes;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// فرمان تکمیل جلسه — پس از تکمیل، شمارش‌ها قابل ویرایش نیستند و جلسه به تأیید می‌رسد
/// </summary>
public record FinalizeStocktakingCommand(long StockTakingId) : IRequest<Result>;

/// <summary>گرداننده تکمیل جلسه</summary>
public class FinalizeStocktakingCommandHandler : IRequestHandler<FinalizeStocktakingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public FinalizeStocktakingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای تکمیل</summary>
    public async Task<Result> Handle(FinalizeStocktakingCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Repository<StockTaking>().QueryableTracking
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.StockTakingId, cancellationToken);

        if (session is null)
            return Result.Failure(Error.NotFound("جلسه انبارگردانی", request.StockTakingId));
        if (session.Status != StockTakingStatus.InProgress)
            return Result.Failure(new Error("NOT_IN_PROGRESS", "این جلسه قبلاً تکمیل یا تأیید شده است"));

        if (session.Items.All(i => !i.IsCounted))
            return Result.Failure(new Error("NO_COUNTS", "هیچ قلمی شمارش نشده است — ابتدا اقلام را شمارش کنید"));

        session.Status = StockTakingStatus.Completed;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// فرمان تأیید نهایی و اعمال مغایرت — برای هر قلم شمارش‌شده با مغایرت:
/// موجودی بچ اصلاح + تراکنش Adjustment با علامت ثبت می‌شود — عملیات اتمیک و یک‌بار مصرف
/// </summary>
public record ApproveStocktakingCommand(long StockTakingId) : IRequest<Result<ApproveStocktakingResultDto>>;

/// <summary>نتیجه اعمال مغایرت</summary>
public record ApproveStocktakingResultDto(int AdjustedItems, decimal TotalShortage, decimal TotalSurplus);

/// <summary>گرداننده تأیید و اعمال</summary>
public class ApproveStocktakingCommandHandler : IRequestHandler<ApproveStocktakingCommand, Result<ApproveStocktakingResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ApproveStocktakingCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>اجرای تأیید — تراکنش اصلاح برای هر مغایرت + به‌روزرسانی موجودی</summary>
    public async Task<Result<ApproveStocktakingResultDto>> Handle(ApproveStocktakingCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Repository<StockTaking>().QueryableTracking
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.StockTakingId, cancellationToken);

        if (session is null)
            return Result<ApproveStocktakingResultDto>.Failure(Error.NotFound("جلسه انبارگردانی", request.StockTakingId));
        if (session.Status != StockTakingStatus.Completed)
            return Result<ApproveStocktakingResultDto>.Failure(new Error("NOT_COMPLETED",
                "ابتدا جلسه باید تکمیل شود (وضعیت: در انتظار تأیید)"));

        // بچ‌های دارای مغایرت به‌صورت یکجا بارگذاری می‌شوند
        var inventoryIds = session.Items.Where(i => i.IsCounted && i.Difference != 0).Select(i => i.InventoryId).ToList();
        var inventories = await _unitOfWork.Repository<PharmacyInventory>().QueryableTracking
            .Where(inv => inventoryIds.Contains(inv.Id))
            .ToDictionaryAsync(inv => inv.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var adjusted = 0;
        decimal shortage = 0m, surplus = 0m;

        foreach (var item in session.Items.Where(i => i.IsCounted && i.Difference != 0))
        {
            if (!inventories.TryGetValue(item.InventoryId, out var inventory))
                continue;

            // مغایرت مثبت = شمارش بیشتر از سیستم (زیادندی/اضافه انبار)؛ منفی = کسری
            inventory.Quantity = item.CountedQuantity;
            adjusted++;

            if (item.Difference > 0) surplus += item.Difference;
            else shortage += -item.Difference;

            await _unitOfWork.Repository<PharmacyTransaction>().AddAsync(new PharmacyTransaction
            {
                InventoryId = inventory.Id,
                TransactionType = PharmacyTransactionType.Adjustment,
                // Quantity مثبت = افزایش موجودی، منفی = کاهش
                Quantity = item.Difference,
                UnitPrice = inventory.SellPrice,
                TotalPrice = item.Difference * inventory.SellPrice,
                TransactionDate = now,
                ReferenceType = PharmacyReferenceType.StockTaking,
                ReferenceId = session.Id,
                UserId = _currentUser.UserId,
                Notes = $"اعمال مغایرت انبارگردانی {session.TakingNumber} — سیستم: {item.SystemQuantity}، شمارش: {item.CountedQuantity}"
            }, cancellationToken);
        }

        session.Status = StockTakingStatus.Approved;
        session.ApprovedAt = now;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<ApproveStocktakingResultDto>.Success(new ApproveStocktakingResultDto(adjusted, shortage, surplus));
    }
}

/// <summary>
/// فرمان لغو جلسه در حال شمارش — جلسه و اقلام حذف نرم می‌شوند و هیچ اثری بر موجودی ندارد
/// </summary>
public record CancelStocktakingCommand(long StockTakingId) : IRequest<Result>;

/// <summary>گرداننده لغو جلسه</summary>
public class CancelStocktakingCommandHandler : IRequestHandler<CancelStocktakingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CancelStocktakingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای لغو — فقط جلسات در حال شمارش قابل لغو‌اند</summary>
    public async Task<Result> Handle(CancelStocktakingCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Repository<StockTaking>().QueryableTracking
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.StockTakingId, cancellationToken);

        if (session is null)
            return Result.Failure(Error.NotFound("جلسه انبارگردانی", request.StockTakingId));
        if (session.Status != StockTakingStatus.InProgress)
            return Result.Failure(new Error("NOT_IN_PROGRESS", "فقط جلسه در حال شمارش قابل لغو است"));

        session.IsDeleted = true;
        foreach (var item in session.Items)
            item.IsDeleted = true;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
