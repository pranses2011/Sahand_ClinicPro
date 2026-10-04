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
}
