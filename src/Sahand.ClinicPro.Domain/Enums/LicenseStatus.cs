namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت لایسنس
/// </summary>
public enum LicenseStatus : byte
{
    /// <summary>فعال</summary>
    Active = 1,
    /// <summary>معلق</summary>
    Suspended = 2,
    /// <summary>منقضی</summary>
    Expired = 3,
    /// <summary>ابطال‌شده</summary>
    Revoked = 4,
}
