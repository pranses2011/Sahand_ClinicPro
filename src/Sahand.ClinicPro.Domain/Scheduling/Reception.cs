using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// پذیرش - ورود بیمار به جریان ویزیت در روز مراجعه
/// </summary>
public class Reception : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره پذیرش - فرمت RCP-XXXXXXXX</summary>
    public string? ReceptionNumber { get; set; }

    /// <summary>شناسه نوبت مرتبط - پذیرش حضوری بدون نوبت هم مجاز است</summary>
    public long? AppointmentId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ و ساعت پذیرش</summary>
    public DateTime ReceptionDate { get; set; }

    /// <summary>تاریخ شمسی پذیرش</summary>
    public string? ReceptionDateShamsi { get; set; }

    /// <summary>بیمه استفاده‌شده در این مراجعه</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>بیمه به‌صورت آنلاین تأیید شده</summary>
    public bool InsuranceVerified { get; set; } = false;

    /// <summary>کد تأیید بیمه</summary>
    public string? InsuranceApprovalCode { get; set; }

    /// <summary>کد بیمه تکمیلی</summary>
    public string? SupplementaryInsuranceCode { get; set; }

    /// <summary>کد ارجاع</summary>
    public string? ReferralCode { get; set; }

    /// <summary>نام پزشک ارجاع‌دهنده</summary>
    public string? ReferringDoctor { get; set; }

    /// <summary>وضعیت پذیرش</summary>
    public ReceptionStatus Status { get; set; } = Domain.Enums.ReceptionStatus.Admitted;

    /// <summary>شناسه منشی پذیرش‌کننده</summary>
    public long? ReceptionistId { get; set; }

    /// <summary>ساعت ورود بیمار</summary>
    public DateTime? EnterTime { get; set; }

    /// <summary>ساعت خروج بیمار</summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>زمان انتظار به دقیقه</summary>
    public int? WaitingTime { get; set; }

    /// <summary>یادداشت پذیرش</summary>
    public string? Notes { get; set; }

    /// <summary>نوبت مرتبط</summary>
    public Appointment? Appointment { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>شرکت بیمه</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }

    /// <summary>ویزیت مرتبط - رابطه یک‌به‌یک</summary>
    public Visit? Visit { get; set; }

    /// <summary>ورودی‌های صف مرتبط</summary>
    public ICollection<Queue> QueueEntries { get; set; } = new List<Queue>();
}
