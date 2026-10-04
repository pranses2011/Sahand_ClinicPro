using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>
/// درخواست تصویربرداری - سونو یا رادیو یا CT یا MRI
/// </summary>
public class ImagingOrder : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره درخواست - یکتا</summary>
    public string? OrderNumber { get; set; }

    /// <summary>شناسه ویزیت</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک ارجاع‌دهنده</summary>
    public long? ReferringDoctorId { get; set; }

    /// <summary>شناسه رادیولوژیست گزارش‌دهنده</summary>
    public long? RadiologistId { get; set; }

    /// <summary>نوع تصویربرداری</summary>
    public ImagingModality Modality { get; set; }

    /// <summary>ناحیه بدن</summary>
    public string? BodyPart { get; set; }

    /// <summary>اطلاعات بالینی</summary>
    public string? ClinicalInfo { get; set; }

    /// <summary>تکنیک انجام</summary>
    public string? Technique { get; set; }

    /// <summary>یافته‌های تصویربرداری</summary>
    public string? Findings { get; set; }

    /// <summary>نتیجه‌گیری رادیولوژیست</summary>
    public string? Impression { get; set; }

    /// <summary>توصیه‌های تکمیلی</summary>
    public string? Recommendation { get; set; }

    /// <summary>اولویت</summary>
    public Priority Priority { get; set; }

    /// <summary>وضعیت</summary>
    public ImagingOrderStatus Status { get; set; } = Domain.Enums.ImagingOrderStatus.Ordered;

    /// <summary>زمان‌بندی شده برای</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>زمان انجام</summary>
    public DateTime? PerformedAt { get; set; }

    /// <summary>زمان گزارش</summary>
    public DateTime? ReportedAt { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>فایل‌های تصویر</summary>
    public ICollection<ImagingFile> Files { get; set; } = new List<ImagingFile>();
}
