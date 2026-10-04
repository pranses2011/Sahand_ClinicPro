namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع پیامک
/// </summary>
public enum SmsMessageType : byte
{
    /// <summary>یادآوری نوبت</summary>
    AppointmentReminder = 1,
    /// <summary>خوش‌آمدگویی</summary>
    Welcome = 2,
    /// <summary>نتیجه آزمایش</summary>
    LabResult = 3,
    /// <summary>رسید پرداخت</summary>
    PaymentReceipt = 4,
    /// <summary>تبلیغاتی</summary>
    Marketing = 5,
    /// <summary>سیستمی</summary>
    System = 6,
}
