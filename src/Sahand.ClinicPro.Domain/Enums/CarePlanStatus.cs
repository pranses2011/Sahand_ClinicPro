namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت طرح مراقبت بیمار مزمن — گام R2
/// </summary>
public enum CarePlanStatus : byte
{
    /// <summary>فعال — در حال پیگیری</summary>
    Active = 1,
    /// <summary>نیازمند بازبینی — زمان بازبینی رسیده است</summary>
    ReviewDue = 2,
    /// <summary>تکمیل‌شده — هدف درمان محقق شد</summary>
    Completed = 3,
    /// <summary>متوقف‌شده — با تشخیص پزشک قطع شد</summary>
    Discontinued = 4
}
