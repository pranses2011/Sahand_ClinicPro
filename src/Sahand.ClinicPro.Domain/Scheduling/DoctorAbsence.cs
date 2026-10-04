using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// غیبت برنامه‌ریزی‌شده پزشک - مسدودکننده اسلات‌های نوبت
/// </summary>
public class DoctorAbsence : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ غیبت</summary>
    public DateOnly AbsenceDate { get; set; }

    /// <summary>از ساعت</summary>
    public TimeOnly? FromTime { get; set; }

    /// <summary>تا ساعت</summary>
    public TimeOnly? ToTime { get; set; }

    /// <summary>نوع غیبت</summary>
    public AbsenceType AbsenceType { get; set; }

    /// <summary>دلیل غیبت</summary>
    public string? Reason { get; set; }

    /// <summary>شناسه تأییدکننده</summary>
    public long? ApprovedBy { get; set; }

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }
}
