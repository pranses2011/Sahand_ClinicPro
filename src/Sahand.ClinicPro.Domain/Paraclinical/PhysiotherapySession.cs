using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// جلسه فیزیوتراپی - تکرارهای دوره درمان
/// </summary>
public class PhysiotherapySession : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه طرح درمان</summary>
    public long? TreatmentPlanId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه فیزیوتراپیست</summary>
    public long? TherapistId { get; set; }

    /// <summary>تاریخ جلسه</summary>
    public DateTime? SessionDate { get; set; }

    /// <summary>شماره جلسه</summary>
    public int SessionNumber { get; set; } = 1;

    /// <summary>اقدامات جلسه - JSON</summary>
    public string? Treatments { get; set; }

    /// <summary>مدت به دقیقه</summary>
    public int? Duration { get; set; }

    /// <summary>شدت درد صفر تا ده</summary>
    public byte? PainScore { get; set; }

    /// <summary>یادداشت پیشرفت</summary>
    public string? ProgressNote { get; set; }

    /// <summary>وضعیت جلسه</summary>
    public SessionStatus Status { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
