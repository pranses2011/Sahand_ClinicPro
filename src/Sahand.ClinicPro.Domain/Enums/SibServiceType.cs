namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع خدمت سیب
/// </summary>
public enum SibServiceType : byte
{
    /// <summary>ارجاع</summary>
    Referral = 1,
    /// <summary>نسخه</summary>
    Prescription = 2,
    /// <summary>نتیجه آزمایش</summary>
    LabResult = 3,
}
