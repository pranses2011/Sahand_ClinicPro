namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت پشتیبان
/// </summary>
public enum BackupStatus : byte
{
    /// <summary>در حال انجام</summary>
    InProgress = 1,
    /// <summary>موفق</summary>
    Success = 2,
    /// <summary>ناموفق</summary>
    Failed = 3,
    /// <summary>حذف‌شده — چرخه ۲۴: فایل پشتیبان به‌صورت دستی از دیسک پاک شده است</summary>
    Deleted = 4,
}
