namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت سند حسابداری
/// </summary>
public enum JournalEntryStatus : byte
{
    /// <summary>پیش‌نویس</summary>
    Draft = 1,
    /// <summary>تأیید شده</summary>
    Approved = 2,
    /// <summary>ابطال شده</summary>
    Voided = 3,
}
