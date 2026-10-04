namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منبع ثبت نوبت
/// </summary>
public enum AppointmentSource : byte
{
    /// <summary>حضوری</summary>
    InPerson = 1,
    /// <summary>تلفنی</summary>
    Phone = 2,
    /// <summary>آنلاین</summary>
    Online = 3,
}
