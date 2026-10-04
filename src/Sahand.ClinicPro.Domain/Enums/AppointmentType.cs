namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع نوبت
/// </summary>
public enum AppointmentType : byte
{
    /// <summary>اولیه</summary>
    Initial = 1,
    /// <summary>پیگیری</summary>
    FollowUp = 2,
    /// <summary>اورژانس</summary>
    Emergency = 3,
}
