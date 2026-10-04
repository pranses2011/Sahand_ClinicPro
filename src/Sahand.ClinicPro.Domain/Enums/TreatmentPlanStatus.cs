namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت طرح درمان
/// </summary>
public enum TreatmentPlanStatus : byte
{
    /// <summary>پیشنهادی</summary>
    Proposed = 1,
    /// <summary>تأیید شده</summary>
    Approved = 2,
    /// <summary>در حال انجام</summary>
    InProgress = 3,
    /// <summary>تکمیل شده</summary>
    Completed = 4,
    /// <summary>لغو</summary>
    Cancelled = 5,
}
