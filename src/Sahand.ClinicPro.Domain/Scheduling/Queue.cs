using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// صف انتظار پویا - فراخوان بیماران نزد پزشک
/// </summary>
public class Queue : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پذیرش</summary>
    public long ReceptionId { get; set; }

    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ صف</summary>
    public DateOnly QueueDate { get; set; }

    /// <summary>شماره نوبت صف</summary>
    public int QueueNumber { get; set; }

    /// <summary>وضعیت در صف</summary>
    public QueueStatus Status { get; set; }

    /// <summary>اولویت</summary>
    public Priority Priority { get; set; }

    /// <summary>زمان رسیدن بیمار</summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>زمان فراخوان</summary>
    public DateTime? CalledAt { get; set; }

    /// <summary>زمان ورود به اتاق</summary>
    public DateTime? EnteredAt { get; set; }

    /// <summary>زمان پایان</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>پذیرش مرتبط</summary>
    public Reception? Reception { get; set; }

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }
}
