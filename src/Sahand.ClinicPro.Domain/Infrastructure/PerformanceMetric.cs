namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// سنجه عملکرد - نمونه‌برداری دوره‌ای سلامت سیستم
/// </summary>
public class PerformanceMetric : AuditableEntity
{
    /// <summary>نوع سنجه مثل CPU</summary>
    public string? MetricType { get; set; }

    /// <summary>نام سنجه</summary>
    public string? MetricName { get; set; }

    /// <summary>مقدار اندازه‌گیری</summary>
    public decimal Value { get; set; } = 0m;

    /// <summary>واحد</summary>
    public string? Unit { get; set; }

    /// <summary>زمان نمونه</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>بافت اضافی - JSON</summary>
    public string? Context { get; set; }
}
