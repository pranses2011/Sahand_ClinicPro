namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت حضور
/// </summary>
public enum AttendanceStatus : byte
{
    /// <summary>حاضر</summary>
    Present = 1,
    /// <summary>غایب</summary>
    Absent = 2,
    /// <summary>مرخصی</summary>
    Leave = 3,
    /// <summary>مأموریت</summary>
    Mission = 4,
    /// <summary>تأخیر</summary>
    Late = 5,
}
