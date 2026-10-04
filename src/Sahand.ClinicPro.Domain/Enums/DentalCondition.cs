namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت دندان
/// </summary>
public enum DentalCondition : byte
{
    /// <summary>سالم</summary>
    Healthy = 1,
    /// <summary>پوسیده</summary>
    Carious = 2,
    /// <summary>کشیده شده</summary>
    Missing = 3,
    /// <summary>ترمیم شده</summary>
    Filled = 4,
    /// <summary>روکش شده</summary>
    Crowned = 5,
    /// <summary>ایمپلنت</summary>
    Implant = 6,
    /// <summary>عصب‌کشی شده</summary>
    RootCanal = 7,
}
