namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت مطالبه بیمه
/// </summary>
public enum ClaimStatus : byte
{
    /// <summary>پیش‌نویس</summary>
    Draft = 1,
    /// <summary>ارسال شده</summary>
    Submitted = 2,
    /// <summary>تأیید شده</summary>
    Approved = 3,
    /// <summary>تأیید جزئی</summary>
    PartiallyApproved = 4,
    /// <summary>رد شده</summary>
    Rejected = 5,
    /// <summary>دریافت شده</summary>
    Paid = 6,
}
