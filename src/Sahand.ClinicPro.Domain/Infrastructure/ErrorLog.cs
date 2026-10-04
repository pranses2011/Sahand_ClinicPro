using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// لاگ خطای سیستم - برای پایش سلامت نرم‌افزار
/// </summary>
public class ErrorLog : AuditableEntity
{
    /// <summary>زمان خطا</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>سطح خطا</summary>
    public ErrorLogLevel Level { get; set; }

    /// <summary>محل وقوع خطا</summary>
    public string? Source { get; set; }

    /// <summary>پیام خطا</summary>
    public string? Message { get; set; }

    /// <summary>متن استثنا</summary>
    public string? Exception { get; set; }

    /// <summary>ردی پشته</summary>
    public string? StackTrace { get; set; }

    /// <summary>شناسه کاربر در لحظه خطا</summary>
    public long? UserId { get; set; }

    /// <summary>نشانی درخواست</summary>
    public string? RequestUrl { get; set; }

    /// <summary>رسیدگی شده</summary>
    public bool Resolved { get; set; } = false;
}
