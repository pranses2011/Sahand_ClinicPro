using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// ردیف لیست انتظار نوبت — چرخه ۱۳ (درخواست ۳-۴)
/// بیماران منتظر جای خالی؛ با لغو نوبت، جای آزاد به متناظرترین ردیف پیشنهاد می‌شود
/// </summary>
public class WaitlistEntry : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک دلخواه — تهی یعنی هر پزشک تخصص</summary>
    public long? DoctorId { get; set; }

    /// <summary>شناسه تخصص دلخواه — وقتی پزشک خاص مدنظر نیست</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>اولین تاریخ قابل قبول</summary>
    public DateOnly PreferredFrom { get; set; }

    /// <summary>آخرین تاریخ قابل قبول</summary>
    public DateOnly PreferredTo { get; set; }

    /// <summary>بازه روز دلخواه — مثل صبح/عصر/هر ساعت</summary>
    public string? PreferredTimeOfDay { get; set; }

    /// <summary>اولویت — بالاتر اول پیشنهاد می‌شود</summary>
    public Priority Priority { get; set; } = Priority.Normal;

    /// <summary>وضعیت ردیف</summary>
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    /// <summary>زمان ثبت پیشنهاد و صف پیامک؛ تأیید تحویل نیست</summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>نوبتی که برایش پیشنهاد شد — پس از لغو</summary>
    public long? OfferedAppointmentId { get; set; }

    /// <summary>زمان تأیید بیمار</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>شماره تماس در زمان ثبت — عکس‌فوری</summary>
    public string? ContactMobile { get; set; }

    /// <summary>یادداشت</summary>
    public string? Notes { get; set; }

    /// <summary>بیمار</summary>
    public People.Patient? Patient { get; set; }

    /// <summary>پزشک دلخواه</summary>
    public People.Doctor? Doctor { get; set; }
}
