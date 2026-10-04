namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت قلم آزمایش
/// </summary>
public enum LabItemStatus : byte
{
    /// <summary>در انتظار</summary>
    Pending = 1,
    /// <summary>در حال انجام</summary>
    InProgress = 2,
    /// <summary>نتیجه‌دار</summary>
    Resulted = 3,
    /// <summary>تأیید شده</summary>
    Verified = 4,
}
