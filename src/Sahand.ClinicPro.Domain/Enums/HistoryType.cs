namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع سابقه بیمار
/// </summary>
public enum HistoryType : byte
{
    /// <summary>جراحی</summary>
    Surgical = 1,
    /// <summary>بیماری</summary>
    Medical = 2,
    /// <summary>آلرژی</summary>
    Allergy = 3,
    /// <summary>خانوادگی</summary>
    Family = 4,
    /// <summary>بستری</summary>
    Hospitalization = 5,
    /// <summary>دارویی</summary>
    Medication = 6,
}
