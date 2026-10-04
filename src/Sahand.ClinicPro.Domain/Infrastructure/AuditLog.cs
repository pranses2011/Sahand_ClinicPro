using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// لاگ عملیات - ثبت تمام تغییرات داده‌ها با مقادیر قبل و بعد
/// </summary>
public class AuditLog : AuditableEntity
{
    /// <summary>شناسه کاربر عملیات</summary>
    public long? UserId { get; set; }

    /// <summary>نام کاربری در لحظه عملیات</summary>
    public string? Username { get; set; }

    /// <summary>نوع عملیات</summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>نام موجودیت تغییر‌یافته</summary>
    public string? EntityName { get; set; }

    /// <summary>شناسه رکورد</summary>
    public long? EntityId { get; set; }

    /// <summary>مقادیر قبلی - JSON</summary>
    public string? OldValues { get; set; }

    /// <summary>مقادیر جدید - JSON</summary>
    public string? NewValues { get; set; }

    /// <summary>فهرست فیلدهای تغییر‌یافته</summary>
    public string? ChangedFields { get; set; }

    /// <summary>نشانی IP</summary>
    public string? IpAddress { get; set; }

    /// <summary>عامل کاربر</summary>
    public string? UserAgent { get; set; }

    /// <summary>زمان عملیات</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>ماژول عملیات</summary>
    public string? Module { get; set; }

    /// <summary>شدت رخداد</summary>
    public AuditSeverity Severity { get; set; }
}
