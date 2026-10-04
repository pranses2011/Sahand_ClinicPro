namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت همگام‌سازی
/// </summary>
public enum SyncStatus : byte
{
    /// <summary>در انتظار</summary>
    Pending = 1,
    /// <summary>همگام شده</summary>
    Synced = 2,
    /// <summary>ناموفق</summary>
    Failed = 3,
    /// <summary>بازتلاش زمان‌بندی‌شده</summary>
    RetryScheduled = 4,
}
