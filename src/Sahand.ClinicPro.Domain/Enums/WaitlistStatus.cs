namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت ردیف لیست انتظار نوبت — چرخه ۱۳ (درخواست ۳-۴)
/// </summary>
public enum WaitlistStatus : byte
{
    /// <summary>در انتظار نوبت آزاد</summary>
    Waiting = 1,

    /// <summary>پیامک پیشنهاد ارسال شد — منتظر تأیید بیمار</summary>
    Notified = 2,

    /// <summary>بیمار تأیید کرد — آماده نوبت‌دهی</summary>
    Confirmed = 3,

    /// <summary>منقضی — بازه دلخواه گذشته</summary>
    Expired = 4,

    /// <summary>لغوشده توسط بیمار یا اپراتور</summary>
    Cancelled = 5
}
