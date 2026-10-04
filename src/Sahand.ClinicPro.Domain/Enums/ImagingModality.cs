namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع تصویربرداری
/// </summary>
public enum ImagingModality : byte
{
    /// <summary>سونوگرافی</summary>
    Ultrasound = 1,
    /// <summary>رادیوگرافی</summary>
    XRay = 2,
    /// <summary>سی‌تی‌اسکن</summary>
    CT = 3,
    /// <summary>ام‌آر‌آی</summary>
    MRI = 4,
    /// <summary>ماموگرافی</summary>
    Mammography = 5,
}
