namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع بیمار
/// </summary>
public enum PatientType : byte
{
    /// <summary>عادی</summary>
    Normal = 1,
    /// <summary>ویژه</summary>
    Vip = 2,
    /// <summary>کارمند</summary>
    Staff = 3,
}
