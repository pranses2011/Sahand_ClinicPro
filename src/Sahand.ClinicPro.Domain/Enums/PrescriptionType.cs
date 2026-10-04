namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع نسخه
/// </summary>
public enum PrescriptionType : byte
{
    /// <summary>دارویی</summary>
    Medication = 1,
    /// <summary>پاراکلینیک</summary>
    Paraclinical = 2,
    /// <summary>ترکیبی</summary>
    Combined = 3,
}
