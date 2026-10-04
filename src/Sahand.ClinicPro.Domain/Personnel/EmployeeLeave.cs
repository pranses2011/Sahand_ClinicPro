using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// مرخصی پرسنل - درخواست و تأیید
/// </summary>
public class EmployeeLeave : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کارمند</summary>
    public long EmployeeId { get; set; }

    /// <summary>نوع مرخصی</summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>از تاریخ</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>تا تاریخ</summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>تعداد روز</summary>
    public decimal Days { get; set; } = 0m;

    /// <summary>دلیل</summary>
    public string? Reason { get; set; }

    /// <summary>وضعیت</summary>
    public LeaveStatus Status { get; set; }

    /// <summary>شناسه تأییدکننده</summary>
    public long? ApprovedBy { get; set; }

    /// <summary>کارمند</summary>
    public Employee? Employee { get; set; }
}
