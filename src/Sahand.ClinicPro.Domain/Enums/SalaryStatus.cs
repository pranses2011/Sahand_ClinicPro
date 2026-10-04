namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت حقوق
/// </summary>
public enum SalaryStatus : byte
{
    /// <summary>محاسبه شده</summary>
    Calculated = 1,
    /// <summary>تأیید شده</summary>
    Approved = 2,
    /// <summary>پرداخت شده</summary>
    Paid = 3,
}
