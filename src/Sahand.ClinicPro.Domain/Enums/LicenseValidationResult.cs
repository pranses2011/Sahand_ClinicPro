namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نتیجه اعتبارسنجی لایسنس
/// </summary>
public enum LicenseValidationResult : byte
{
    /// <summary>معتبر</summary>
    Valid = 1,
    /// <summary>منقضی</summary>
    Expired = 2,
    /// <summary>امضای نامعتبر</summary>
    InvalidSignature = 3,
    /// <summary>عدم تطابق سخت‌افزار</summary>
    HardwareMismatch = 4,
    /// <summary>ابطال‌شده از راه دور</summary>
    Revoked = 5,
    /// <summary>یافت نشد</summary>
    NotFound = 6,
}
