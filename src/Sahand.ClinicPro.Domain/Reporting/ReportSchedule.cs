using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Reporting;

/// <summary>
/// زمان‌بندی گزارش - تولید خودکار دوره‌ای
/// </summary>
public class ReportSchedule : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه گزارش</summary>
    public long ReportId { get; set; }

    /// <summary>دوره تکرار</summary>
    public ReportFrequency Frequency { get; set; }

    /// <summary>ساعت اجرا</summary>
    public TimeOnly? ScheduleTime { get; set; }

    /// <summary>گیرندگان خروجی</summary>
    public string? Recipients { get; set; }

    /// <summary>اجرای بعدی</summary>
    public DateTime? NextRun { get; set; }

    /// <summary>اجرای قبلی</summary>
    public DateTime? LastRun { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>گزارش والد</summary>
    public ReportDefinition? Report { get; set; }
}
