using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Caching;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Settings;

/// <summary>تنظیم سیستم</summary>
public record SettingDto(string Key, string? Value, string? Description, bool IsEncrypted);

/// <summary>
/// پرس‌وجوی همه تنظیمات — به تفکیک دسته
/// </summary>
public record GetSettingsQuery(string? Category = null) : IRequest<Result<List<SettingDto>>>;

/// <summary>گرداننده فهرست تنظیمات</summary>
public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<List<SettingDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کش مرجع تنظیمات — چرخهٔ ۶۰؛ باطل با هر ذخیرهٔ تنظیم</summary>
    private readonly IReferenceCache _cache;

    /// <summary>سازنده با تزریق واحد کار و کش مرجع</summary>
    public GetSettingsQueryHandler(IUnitOfWork unitOfWork, IReferenceCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>اجرای پرس‌وجو — از کش L1 با عمر ۹۰ ثانیه (چرخهٔ ۶۰)</summary>
    public async Task<Result<List<SettingDto>>> Handle(GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _cache.GetOrSetAsync(
            ReferenceCacheKeys.Settings(request.Category),
            async ct =>
            {
                IQueryable<SystemSetting> query = _unitOfWork.Repository<SystemSetting>().QueryableNoTracking;

                if (!string.IsNullOrEmpty(request.Category))
                    query = query.Where(s => s.Category == request.Category);

                return await query
                    .OrderBy(s => s.Category).ThenBy(s => s.Key)
                    .Select(s => new SettingDto(s.Key, s.Value, s.Description, s.IsEncrypted))
                    .ToListAsync(ct);
            },
            TimeSpan.FromSeconds(90),
            cancellationToken);

        return Result<List<SettingDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ذخیره تنظیم — ایجاد یا به‌روزرسانی
/// </summary>
public record SaveSettingCommand(string Key, string? Value, string Category) : IRequest<Result>;

/// <summary>گرداننده ذخیره تنظیم</summary>
/// <remarks>
/// ═══ چرخهٔ ۶۰ (P-06) ═══ پس از هر ذخیره، کل پیشوند تنظیمات باطل می‌شود
/// («همهٔ دسته‌ها» + دستهٔ جاری) تا خوانندهٔ بعدی مقدار تازه بگیرد.
/// </remarks>
public class SaveSettingCommandHandler : IRequestHandler<SaveSettingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کش مرجع — باطل‌سازی پس از ذخیره</summary>
    private readonly IReferenceCache _cache;

    /// <summary>سازنده با تزریق واحد کار و کش مرجع</summary>
    public SaveSettingCommandHandler(IUnitOfWork unitOfWork, IReferenceCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>اجرای ذخیره تنظیم</summary>
    public async Task<Result> Handle(SaveSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                Key = request.Key,
                Value = request.Value,
                Category = request.Category,
                DataType = Domain.Enums.SettingDataType.String
            };
            await _unitOfWork.Repository<SystemSetting>().AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = request.Value;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // چرخهٔ ۶۰ — باطل‌سازی همهٔ کلیدهای تنظیمات (دستهٔ «همه» + هر دستهٔ دیگر)
        _cache.EvictByPrefix(ReferenceCacheKeys.SettingsPrefix);

        return Result.Success();
    }
}

/// <summary>ردیف لاگ عملیات — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
/// «در صفحه لاگ عملیات چیزی نشان نمی‌دهد» — DTO غنی‌تر شد تا ستون‌های
/// IP / مقادیر جدید / مقادیر قبلی جزئیات واقعی داشته باشند.
/// ═══ چرخهٔ ۷۰ (بازطراحی لاگ) ═══ دو فیلد «فارسی» افزوده شد:
/// PersianEntity/PersianModule — خروجی کاتالوگ معنایی روی نامِ ذخیره‌شده؛
/// ردیف‌های قدیمی انگلیسی نیز در لحظهٔ خواندن فارسی می‌شوند.
/// ═══ چرخهٔ ۷۲ (گزارش مالک: «در لاگ عملیات به جای نام کامل، نام کاربری رو
/// مینویسه») ═══ UserId به پروجکشن اضافه و UserDisplayName در هندلر از
/// پرسنل متصل کاربر پر می‌شود — ردیف‌های قدیمی (که نام کاربری در ستون
/// Username نشسته‌اند) هم در لحظهٔ خواندن نام کامل می‌گیرند و کلاینت
/// UserDisplayName ?? Username را نشان می‌دهد.</summary>
public record AuditLogDto(
    string? Username, string Action, string? EntityName,
    long? EntityId, string? Module, string TimeShamsi, string Time,
    string? IpAddress, string? NewValues, string? OldValues, long Id = 0,
    long? UserId = null)
{
    /// <summary>نام فارسی موجودیت — از کاتالوگ معنایی؛ ردیف‌های تازه از پیش فارسی‌اند</summary>
    public string PersianEntity { get; set; } = string.Empty;

    /// <summary>نام فارسی ماژول — از کاتالوگ معنایی؛ ردیف‌های تازه از پیش فارسی‌اند</summary>
    public string PersianModule { get; set; } = string.Empty;

    /// <summary>
    /// نام کامل پرسنل متصل — چرخهٔ ۷۲؛ برای ردیف‌های قدیمی و جدید یکسان.
    /// تهی یعنی پرسنل متصل نیست و کلاینت به Username برمی‌گردد.
    /// </summary>
    public string? UserDisplayName { get; set; }
}

/// <summary>
/// پرس‌وجوی لاگ عملیات با فیلتر کاربر، ماژول و نوع عملیات
/// (چرخه بیست‌وچهارم: فیلتر «عملیات» به‌عنوان پارامتر مستقل اضافه شد —
/// پیش‌تر کلاینت مقدار عملیات را به entityName می‌فرستاد که بی‌اثر بود)
/// </summary>
public record GetAuditLogsQuery(int Page = 1, int PageSize = 50, long? UserId = null,
    string? EntityName = null, string? Action = null) : IRequest<Result<List<AuditLogDto>>>;

/// <summary>گرداننده لاگ عملیات</summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<List<AuditLogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<AuditLogDto>>> Handle(GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        // ═══ چرخهٔ ۶۲ (P-03) — گارد صفحه‌بندی ═══ اندازه/شمارهٔ معیوب هرگز
        // به Skip/Take نمی‌رسد: صفحهٔ صفر Skip منفی (استثنای EF) می‌سازید و
        // اندازهٔ غول‌پیکر پاسخ چند‌مگابایتی؛ مرز: صفحه ≥۱ و اندازه ≤۲۰۰
        var pageNumber = Common.Helpers.PagingGuard.Page(request.Page);
        var pageSize = Common.Helpers.PagingGuard.Size(request.PageSize, 200);

        IQueryable<Domain.Entities.Infrastructure.AuditLog> query =
            _unitOfWork.Repository<Domain.Entities.Infrastructure.AuditLog>().QueryableNoTracking;

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId.Value);
        if (!string.IsNullOrEmpty(request.EntityName))
            query = query.Where(l => l.EntityName == request.EntityName);
        // فیلتر نوع عملیات — نام enum به صورت رشته‌ای مقایسه می‌شود
        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.ActionType.ToString() == request.Action);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip(PagingGuard.Skip(pageNumber, pageSize))
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Username,
                l.ActionType.ToString(),
                l.EntityName, l.EntityId, l.Module,
                Common.Helpers.PersianDateHelper.ToShamsi(l.Timestamp),
                l.Timestamp.ToString("HH:mm:ss"),
                l.IpAddress, l.NewValues, l.OldValues, l.Id,
                l.UserId))
            .ToListAsync(cancellationToken);

        // ═══ چرخهٔ ۷۲ (گزارش مالک) — نام کامل پرسنل برای ردیف‌های قدیمی ═══
        // ردیف‌های پیش از این نسخه، نام کاربری را در ستون Username دارند
        // (ادعای displayName توکن قدیمی همان Username بود). UserId که اکنون
        // به پروجکشن آمده، نگاشت به «نام کامل پرسنل متصل» را ممکن می‌کند؛
        // فقط کاربران همین صفحهٔ جاری واکشی می‌شوند (≤۲۰۰ ردیف) — بدون
        // هزینهٔ اضافه روی دیتابیس بزرگ لاگ.
        var userIds = items.Where(i => i.UserId is > 0)
            .Select(i => i.UserId!.Value).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var fullNames = await _unitOfWork.Repository<Domain.Entities.Security.User>()
                .QueryableNoTracking
                .Where(u => userIds.Contains(u.Id) && u.Employee != null)
                .Select(u => new { u.Id, Name = u.Employee!.FirstName + " " + u.Employee!.LastName })
                .ToListAsync(cancellationToken);

            var nameMap = fullNames
                .Where(n => !string.IsNullOrWhiteSpace(n.Name))
                .GroupBy(n => n.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);

            foreach (var row in items)
            {
                if (row.UserId is { } uid && nameMap.TryGetValue(uid, out var full))
                    row.UserDisplayName = full;
            }
        }

        // ═══ بازطراحی چرخهٔ ۷۰ (گزارش مالک) — فارسی‌سازی ردیف‌های «قدیمی» ═══
        // ردیف‌های پیش از این نسخه با نام انگلیسی موجودیت (patients/roles و…)
        // و ماژول ثابت «API» در دیتابیس نشسته‌اند. به‌جای بازنویسی دیتابیس،
        // در لحظهٔ خواندن از همان کاتالوگ معنایی ترجمه می‌شوند — ردیف‌های تازه
        // از پیش فارسی‌اند و «بدون تغییر» پاس داده می‌شوند (PersianizeEntity/
        // PersianizeModule هر دو «شفاف»‌اند: متن فارسی را دست‌نخورده برمی‌گردانند).
        foreach (var row in items)
        {
            row.PersianEntity = Common.AuditSemanticCatalog.PersianizeEntity(row.EntityName);
            row.PersianModule = Common.AuditSemanticCatalog.PersianizeModule(row.Module);
        }

        return Result<List<AuditLogDto>>.Success(items);
    }
}
