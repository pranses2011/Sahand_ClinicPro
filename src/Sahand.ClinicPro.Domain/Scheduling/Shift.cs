using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// شیفت کاری - الگوی زمانی کار پرسنل
/// </summary>
public class Shift : AuditableEntity, ISoftDeletable
{
    /// <summary>نام شیفت</summary>
    public string ShiftName { get; set; } = null!;

    /// <summary>کد یکتای شیفت</summary>
    public string? ShiftCode { get; set; }

    /// <summary>ساعت شروع</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>ساعت پایان</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>نوع شیفت</summary>
    public ShiftType ShiftType { get; set; }

    /// <summary>شیفت شب</summary>
    public bool IsNightShift { get; set; } = false;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>حضورهای این شیفت</summary>
    public ICollection<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();
}
