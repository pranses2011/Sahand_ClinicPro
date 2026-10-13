using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// نوبت - قلب ماژول نوبت‌دهی
/// </summary>
public class Appointment : AuditableEntity, ISoftDeletable
{
    /// <summary>کد پیگیری - فرمت APT-XXXXXXXX با SQL Sequence</summary>
    public string? AppointmentCode { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>شناسه تخصص</summary>
    public long? SpecialtyId { get; set; }

    /// <summary>تاریخ نوبت</summary>
    public DateOnly AppointmentDate { get; set; }

    /// <summary>تاریخ شمسی نوبت</summary>
    public string? AppointmentDateShamsi { get; set; }

    /// <summary>ساعت نوبت</summary>
    public TimeOnly AppointmentTime { get; set; }

    /// <summary>مدت به دقیقه</summary>
    public int Duration { get; set; } = 15;

    /// <summary>وضعیت نوبت</summary>
    public AppointmentStatus Status { get; set; } = Domain.Enums.AppointmentStatus.Booked;

    /// <summary>نوع نوبت</summary>
    public AppointmentType AppointmentType { get; set; } = Domain.Enums.AppointmentType.Initial;

    /// <summary>شناسه اتاق ویزیت</summary>
    public long? RoomId { get; set; }

    /// <summary>شماره صف</summary>
    public int? QueueNumber { get; set; }

    /// <summary>منبع ثبت نوبت</summary>
    public AppointmentSource Source { get; set; } = Domain.Enums.AppointmentSource.InPerson;

    /// <summary>اولین مراجعه بیمار</summary>
    public bool IsFirstVisit { get; set; } = false;

    /// <summary>اولویت نوبت</summary>
    public Priority Priority { get; set; } = Domain.Enums.Priority.Normal;

    /// <summary>تخمین هزینه به ریال</summary>
    public decimal EstimatedFee { get; set; } = 0m;

    /// <summary>دلیل لغو نوبت</summary>
    public string? CancellationReason { get; set; }

    /// <summary>زمان لغو</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>شناسه کاربر لغو‌کننده</summary>
    public long? CancelledBy { get; set; }

    /// <summary>پیامک یادآوری ارسال شده</summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// پیامک یادآوری دوم (۲ ساعت مانده به نوبت) ارسال شده — چرخهٔ ۶۹ (فاز ۳ / F-03)
    /// «یادآوری خودکار نوبت»: دو موج پیامک (۲۴ ساعت و ۲ ساعت قبل) با رعایت ساعات سکوت؛
    /// این پرچم مستقل از موج اول است تا بیمار هر دو را دریافت کند.
    /// </summary>
    public bool Reminder2Sent { get; set; } = false;

    /// <summary>توضیحات</summary>
    public string? Notes { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>تخصص</summary>
    public Specialty? Specialty { get; set; }

    /// <summary>اتاق ویزیت</summary>
    public ExaminationRoom? Room { get; set; }

    /// <summary>پذیرش مرتبط - رابطه یک‌به‌یک</summary>
    public Reception? Reception { get; set; }
}
