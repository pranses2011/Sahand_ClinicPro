namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت درخواست آزمایش
/// </summary>
public enum LabOrderStatus : byte
{
    /// <summary>ثبت شده</summary>
    Ordered = 1,
    /// <summary>نمونه گرفته شده</summary>
    SampleCollected = 2,
    /// <summary>در حال آزمایش</summary>
    InProgress = 3,
    /// <summary>نتیجه آماده</summary>
    ResultReady = 4,
    /// <summary>تأیید شده</summary>
    Verified = 5,
    /// <summary>تحویل شده</summary>
    Delivered = 6,
    /// <summary>لغو</summary>
    Cancelled = 7,
}
