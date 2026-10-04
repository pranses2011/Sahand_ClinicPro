namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت انبارگردانی
/// </summary>
public enum StockTakingStatus : byte
{
    /// <summary>در حال شمارش</summary>
    InProgress = 1,
    /// <summary>تکمیل</summary>
    Completed = 2,
    /// <summary>تأیید شده</summary>
    Approved = 3,
}
