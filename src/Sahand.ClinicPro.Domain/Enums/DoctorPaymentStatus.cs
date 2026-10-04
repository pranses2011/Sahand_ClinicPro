namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت کارانه پزشک
/// </summary>
public enum DoctorPaymentStatus : byte
{
    /// <summary>محاسبه شده</summary>
    Calculated = 1,
    /// <summary>تأیید شده</summary>
    Approved = 2,
    /// <summary>پرداخت شده</summary>
    Paid = 3,
}
