namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت صف انتظار
/// </summary>
public enum QueueStatus : byte
{
    /// <summary>در انتظار</summary>
    Waiting = 1,
    /// <summary>فراخوانده شده</summary>
    Called = 2,
    /// <summary>در حال انجام</summary>
    InProgress = 3,
    /// <summary>تکمیل</summary>
    Completed = 4,
    /// <summary>جدا شده</summary>
    Skipped = 5,
}
