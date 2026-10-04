using MediatR;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetSettingsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<SettingDto>>> Handle(GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<SystemSetting> query = _unitOfWork.Repository<SystemSetting>().QueryableNoTracking;

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(s => s.Category == request.Category);

        var items = await query
            .OrderBy(s => s.Category).ThenBy(s => s.Key)
            .Select(s => new SettingDto(s.Key, s.Value, s.Description, s.IsEncrypted))
            .ToListAsync(cancellationToken);

        return Result<List<SettingDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ذخیره تنظیم — ایجاد یا به‌روزرسانی
/// </summary>
public record SaveSettingCommand(string Key, string? Value, string Category) : IRequest<Result>;

/// <summary>گرداننده ذخیره تنظیم</summary>
public class SaveSettingCommandHandler : IRequestHandler<SaveSettingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveSettingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
        return Result.Success();
    }
}

/// <summary>ردیف لاگ عملیات</summary>
public record AuditLogDto(
    long Id, string? Username, string Action, string? EntityName,
    long? EntityId, string? Module, string TimeShamsi, string Time);

/// <summary>
/// پرس‌وجوی لاگ عملیات با فیلتر کاربر و ماژول
/// </summary>
public record GetAuditLogsQuery(int Page = 1, int PageSize = 50, long? UserId = null,
    string? EntityName = null) : IRequest<Result<List<AuditLogDto>>>;

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

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogDto(
                l.Id, l.Username,
                l.ActionType.ToString(),
                l.EntityName, l.EntityId, l.Module,
                Common.Helpers.PersianDateHelper.ToShamsi(l.Timestamp),
                l.Timestamp.ToString("HH:mm:ss")))
            .ToListAsync(cancellationToken);

        return Result<List<AuditLogDto>>.Success(items);
    }
}
