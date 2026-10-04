namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت جلسه درمانی
/// </summary>
public enum SessionStatus : byte
{
    /// <summary>زمان‌بندی شده</summary>
    Scheduled = 1,
    /// <summary>انجام شده</summary>
    Completed = 2,
    /// <summary>لغو</summary>
    Cancelled = 3,
    /// <summary>غیبت بیمار</summary>
    Missed = 4,
}
