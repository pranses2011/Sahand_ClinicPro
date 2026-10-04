using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// حضور و غیاب پرسنل - ورود و خروج روزانه
/// </summary>
public class EmployeeAttendance : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کارمند</summary>
    public long EmployeeId { get; set; }

    /// <summary>تاریخ</summary>
    public DateOnly AttendanceDate { get; set; }

    /// <summary>ساعت ورود</summary>
    public TimeOnly? CheckIn { get; set; }

    /// <summary>ساعت خروج</summary>
    public TimeOnly? CheckOut { get; set; }

    /// <summary>شناسه شیفت</summary>
    public long? ShiftId { get; set; }

    /// <summary>وضعیت حضور</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>ساعات کارکرد</summary>
    public decimal WorkedHours { get; set; } = 0m;

    /// <summary>ساعات اضافه‌کاری</summary>
    public decimal OvertimeHours { get; set; } = 0m;

    /// <summary>کارمند</summary>
    public Employee? Employee { get; set; }

    /// <summary>شیفت</summary>
    public Shift? Shift { get; set; }
}
