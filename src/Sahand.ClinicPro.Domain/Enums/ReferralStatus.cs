namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت ارجاع
/// </summary>
public enum ReferralStatus : byte
{
    /// <summary>ایجاد شده</summary>
    Created = 1,
    /// <summary>ارجاع شده</summary>
    Referred = 2,
    /// <summary>تکمیل</summary>
    Completed = 3,
    /// <summary>لغو</summary>
    Cancelled = 4,
}
