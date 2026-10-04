namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت نسخه
/// </summary>
public enum PrescriptionStatus : byte
{
    /// <summary>صادر شده</summary>
    Issued = 1,
    /// <summary>ارسال به بیمه</summary>
    SentToInsurance = 2,
    /// <summary>تأیید بیمه</summary>
    InsuranceApproved = 3,
    /// <summary>تحویل شده</summary>
    Delivered = 4,
    /// <summary>لغو</summary>
    Cancelled = 5,
    /// <summary>منقضی</summary>
    Expired = 6,
}
