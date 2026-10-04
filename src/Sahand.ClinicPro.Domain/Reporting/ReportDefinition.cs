namespace Sahand.ClinicPro.Domain.Entities.Reporting;

/// <summary>
/// تعریف گزارش - قالب فایل گزارش یا کوئری SQL
/// </summary>
public class ReportDefinition : AuditableEntity, ISoftDeletable
{
    /// <summary>نام گزارش</summary>
    public string ReportName { get; set; } = null!;

    /// <summary>کد یکتای گزارش</summary>
    public string? Code { get; set; }

    /// <summary>دسته گزارش</summary>
    public string? Category { get; set; }

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>فایل قالب گزارش</summary>
    public byte[]? ReportFile { get; set; }

    /// <summary>کوئری منبع داده</summary>
    public string? SqlQuery { get; set; }

    /// <summary>پارامترهای گزارش - JSON</summary>
    public string? Parameters { get; set; }

    /// <summary>دسترسی موردنیاز اجرا</summary>
    public string? RequiredPermission { get; set; }

    /// <summary>سوابق اجرا</summary>
    public ICollection<ReportExecution> Executions { get; set; } = new List<ReportExecution>();

    /// <summary>زمان‌بندی‌های گزارش</summary>
    public ICollection<ReportSchedule> Schedules { get; set; } = new List<ReportSchedule>();
}
