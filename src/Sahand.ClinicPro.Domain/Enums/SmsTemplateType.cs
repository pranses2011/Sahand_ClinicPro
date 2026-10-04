namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع قالب پیامک
/// </summary>
public enum SmsTemplateType : byte
{
    /// <summary>یادآوری</summary>
    Reminder = 1,
    /// <summary>تأیید</summary>
    Confirmation = 2,
    /// <summary>نتیجه</summary>
    Result = 3,
    /// <summary>رسید</summary>
    Receipt = 4,
    /// <summary>سفارشی</summary>
    Custom = 5,
}
