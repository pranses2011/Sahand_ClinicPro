namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت پیامک
/// </summary>
public enum SmsStatus : byte
{
    /// <summary>در صف</summary>
    Queued = 1,
    /// <summary>ارسال شده</summary>
    Sent = 2,
    /// <summary>تحویل شده</summary>
    Delivered = 3,
    /// <summary>ناموفق</summary>
    Failed = 4,
}
