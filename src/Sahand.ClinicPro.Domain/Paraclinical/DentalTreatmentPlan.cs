using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// طرح درمان دندانپزشکی - برنامه کامل مراحل درمان
/// </summary>
public class DentalTreatmentPlan : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه دندانپزشک</summary>
    public long? DentistId { get; set; }

    /// <summary>تاریخ تنظیم طرح</summary>
    public DateOnly? CreatedDate { get; set; }

    /// <summary>هزینه کل</summary>
    public decimal TotalCost { get; set; } = 0m;

    /// <summary>تعداد جلسات برآوردی</summary>
    public int? EstimatedSessions { get; set; }

    /// <summary>وضعیت طرح</summary>
    public TreatmentPlanStatus Status { get; set; }

    /// <summary>جزئیات طرح - JSON</summary>
    public string? PlanDetails { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
