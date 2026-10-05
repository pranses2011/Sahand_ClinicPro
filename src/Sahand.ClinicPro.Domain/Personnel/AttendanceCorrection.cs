using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// درخواست اصلاح کارت حضور — جریان کاری تصدیق‌شده برای فراموشی/خطای ضربه
/// پرسنل درخواست می‌دهد، سرپرست تأیید می‌کند، سپس رکورد حضور اصلاح می‌شود
/// </summary>
public class AttendanceCorrection : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پرسنل</summary>
    public long EmployeeId { get; set; }

    /// <summary>تاریخ موردنظر</summary>
    public DateOnly AttendanceDate { get; set; }

    /// <summary>فیلد مورد اصلاح: «CheckIn» یا «CheckOut»</summary>
    public string FieldName { get; set; } = null!;

    /// <summary>مقدار فعلی ثبت‌شده (متن آزاد ساعت)</summary>
    public string? OldValue { get; set; }

    /// <summary>مقدار درخواستی</summary>
    public string NewValue { get; set; } = null!;

    /// <summary>دلیل — اجباری</summary>
    public string Reason { get; set; } = null!;

    /// <summary>وضعیت</summary>
    public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;

    /// <summary>شناسه تأییدکننده</summary>
    public long? ApprovedBy { get; set; }

    /// <summary>زمان تأیید/رد (UTC)</summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>ملاحظات تأییدکننده</summary>
    public string? ApproverNote { get; set; }

    /// <summary>شناسه رکورد حضور اصلاح‌شده پس از تأیید</summary>
    public long? AppliedAttendanceId { get; set; }

    /// <summary>پرسنل</summary>
    public Employee? Employee { get; set; }
}
