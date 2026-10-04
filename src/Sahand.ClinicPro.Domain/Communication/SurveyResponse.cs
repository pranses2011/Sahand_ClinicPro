namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// پاسخ نظرسنجی - ثبت امتیاز و نظرات بیمار
/// </summary>
public class SurveyResponse : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه نظرسنجی</summary>
    public long SurveyId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long? PatientId { get; set; }

    /// <summary>شناسه ویزیت مرتبط</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه پزشک مورد نظر</summary>
    public long? DoctorId { get; set; }

    /// <summary>امتیاز یک تا پنج</summary>
    public byte? Rating { get; set; }

    /// <summary>پاسخ‌ها - JSON</summary>
    public string? Answers { get; set; }

    /// <summary>نظرات آزاد</summary>
    public string? Comments { get; set; }

    /// <summary>زمان ثبت</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>نظرسنجی والد</summary>
    public Survey? Survey { get; set; }
}
