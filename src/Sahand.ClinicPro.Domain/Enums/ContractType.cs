namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع قرارداد
/// </summary>
public enum ContractType : byte
{
    /// <summary>دائم</summary>
    Permanent = 1,
    /// <summary>موقت</summary>
    Temporary = 2,
    /// <summary>پروژه‌ای</summary>
    Project = 3,
}
