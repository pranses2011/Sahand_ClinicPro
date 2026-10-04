namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// مسیر مصرف دارو
/// </summary>
public enum MedicationRoute : byte
{
    /// <summary>خوراکی</summary>
    Oral = 1,
    /// <summary>تزریقی</summary>
    Injection = 2,
    /// <summary>موضعی</summary>
    Topical = 3,
    /// <summary>استنشاقی</summary>
    Inhalation = 4,
    /// <summary>مقعدی</summary>
    Rectal = 5,
    /// <summary>چشمی</summary>
    Ophthalmic = 6,
    /// <summary>گوشی</summary>
    Otic = 7,
    /// <summary>بینی</summary>
    Nasal = 8,
    /// <summary>زیرزبانی</summary>
    Sublingual = 9,
}
