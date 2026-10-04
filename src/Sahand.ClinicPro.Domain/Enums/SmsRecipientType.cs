namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع گیرنده پیامک
/// </summary>
public enum SmsRecipientType : byte
{
    /// <summary>بیمار</summary>
    Patient = 1,
    /// <summary>پزشک</summary>
    Doctor = 2,
    /// <summary>کارمند</summary>
    Staff = 3,
    /// <summary>سایر</summary>
    Other = 4,
}
