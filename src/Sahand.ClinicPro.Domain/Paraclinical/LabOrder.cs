using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// درخواست آزمایش - از پزشک برای بیمار
/// </summary>
public class LabOrder : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره درخواست - یکتا</summary>
    public string? OrderNumber { get; set; }

    /// <summary>شناسه ویزیت</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک درخواست‌کننده</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ درخواست</summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>اولویت</summary>
    public Priority Priority { get; set; }

    /// <summary>وضعیت درخواست</summary>
    public LabOrderStatus Status { get; set; } = Domain.Enums.LabOrderStatus.Ordered;

    /// <summary>اطلاعات بالینی همراه درخواست</summary>
    public string? ClinicalInfo { get; set; }

    /// <summary>زمان نمونه‌گیری</summary>
    public DateTime? SampleCollectedAt { get; set; }

    /// <summary>شناسه نمونه‌گیر</summary>
    public long? SampleCollectedBy { get; set; }

    /// <summary>زمان آماده شدن نتیجه</summary>
    public DateTime? ResultReadyAt { get; set; }

    /// <summary>شناسه تأییدکننده نتیجه</summary>
    public long? VerifiedBy { get; set; }

    /// <summary>زمان تحویل به بیمار</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پزشک درخواست‌کننده</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>اقلام درخواست</summary>
    public ICollection<LabOrderItem> Items { get; set; } = new List<LabOrderItem>();
}
