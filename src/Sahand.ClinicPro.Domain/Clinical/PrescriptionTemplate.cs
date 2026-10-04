namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// قالب نسخه - نسخه‌های آماده پزشکان برای بیماری‌های پرتکرار
/// </summary>
public class PrescriptionTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پزشک صاحب قالب</summary>
    public long DoctorId { get; set; }

    /// <summary>نام قالب</summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>دسته قالب</summary>
    public string? Category { get; set; }

    /// <summary>اقلام قالب - JSON</summary>
    public string? Items { get; set; }

    /// <summary>تعداد استفاده</summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>پزشک صاحب قالب</summary>
    public Doctor? Doctor { get; set; }
}
