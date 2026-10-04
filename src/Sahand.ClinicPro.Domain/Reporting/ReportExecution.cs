namespace Sahand.ClinicPro.Domain.Entities.Reporting;

/// <summary>
/// اجرا گزارش - رکورد هر بار تولید گزارش
/// </summary>
public class ReportExecution : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه گزارش</summary>
    public long ReportId { get; set; }

    /// <summary>شناسه کاربر اجراکننده</summary>
    public long UserId { get; set; }

    /// <summary>زمان اجرا</summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>پارامترهای استفاده‌شده - JSON</summary>
    public string? Parameters { get; set; }

    /// <summary>مدت اجرا به میلی‌ثانیه</summary>
    public int? Duration { get; set; }

    /// <summary>قالب خروجی مثل PDF</summary>
    public string? ExportFormat { get; set; }

    /// <summary>تعداد سطرهای نتیجه</summary>
    public int? RowCount { get; set; }

    /// <summary>گزارش اجراشده</summary>
    public ReportDefinition? Report { get; set; }
}
