namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// پرچم غیرطبیعی نتیجه
/// </summary>
public enum AbnormalFlag : byte
{
    /// <summary>پایین‌تر از حد</summary>
    Low = 1,
    /// <summary>بالاتر از حد</summary>
    High = 2,
    /// <summary>خیلی پایین</summary>
    VeryLow = 3,
    /// <summary>خیلی بالا</summary>
    VeryHigh = 4,
}
