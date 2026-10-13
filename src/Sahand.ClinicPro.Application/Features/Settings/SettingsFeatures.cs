using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Caching;
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
/// IP / مقادیر جدید / مقادیر قبیل جزئیات واقعی داشته باشند</summary>
public record AuditLogDto(
    long Id, string? Username, string Action, string? EntityName,
    long? EntityId, string? Module, string TimeShamsi, string Time,
    string? IpAddress, string? NewValues, string? OldValues);

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
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogDto(
                l.Id, l.Username,
                l.ActionType.ToString(),
                l.EntityName, l.EntityId, l.Module,
                Common.Helpers.PersianDateHelper.ToShamsi(l.Timestamp),
                l.Timestamp.ToString("HH:mm:ss"),
                l.IpAddress, l.NewValues, l.OldValues))
            .ToListAsync(cancellationToken);

        return Result<List<AuditLogDto>>.Success(items);
    }
}
