namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت کاربر
/// </summary>
public enum UserStatus : byte
{
    /// <summary>فعال</summary>
    Active = 1,
    /// <summary>غیرفعال</summary>
    Inactive = 2,
    /// <summary>معلق</summary>
    Suspended = 3,
}
