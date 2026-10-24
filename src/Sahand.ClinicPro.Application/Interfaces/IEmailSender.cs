namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// پیوست ایمیل — فایل پشتیبان، گزارش یا سند چاپی.
/// </summary>
/// <param name="FileName">نام فایل برای گیرنده (مثلاً SahandClinicPro_Auto_14040712_0210.zip)</param>
/// <param name="Content">محتوای بایت‌شدهٔ فایل</param>
/// <param name="ContentType">نوع MIME — تهی یعنی application/octet-stream</param>
public sealed record EmailAttachment(string FileName, byte[] Content, string? ContentType = null);

/// <summary>
/// نتیجهٔ ارسال ایمیل — همان الگوی <see cref="SmsSendResult"/> تا مسیرهای
/// پیام‌رسانی برنامه یک‌دست باشند.
/// </summary>
/// <param name="Success">ارسال با موفقیت انجام شد؟</param>
/// <param name="Error">پیام خطای فارسی — فقط در شکست</param>
public sealed record EmailSendResult(bool Success, string? Error)
{
    /// <summary>ارسال موفق</summary>
    public static EmailSendResult Ok() => new(true, null);

    /// <summary>ارسال ناموفق با دلیل فارسی</summary>
    public static EmailSendResult Fail(string error) => new(false, error);
}

/// <summary>
/// فرستندهٔ ایمیل سامانه — چرخهٔ ۸۲ (گزارش مالک):
/// «فراموشی رمز» در صفحهٔ ورود بی‌اثر بود و «ارسال بکاپ به ایمیل» هم وجود نداشت.
/// </summary>
/// <remarks>
/// ═══ چرا یک انتزاع در لایهٔ Application ═══
/// دو مصرف‌کنندهٔ متفاوت داریم — بازیابی رمز در لایهٔ احراز هویت و ارسال
/// پشتیبان در کارگر پس‌زمینهٔ بکاپ — و هر دو نباید به SMTP وابسته باشند.
/// پیاده‌سازی واقعی (SmtpEmailSender) در زیرساخت است و پیکربندی از بخش
/// «Email» در appsettings خوانده می‌شود:
///
///   "Email": {
///     "Host": "smtp.example.com",
///     "Port": 587,
///     "UseSsl": true,
///     "User": "clinic@example.com",
///     "Password": "***",
///     "From": "clinic@example.com",
///     "FromName": "درمانگاه سهند"
///   }
///
/// اگر این بخش پر نشده باشد <see cref="IsConfigured"/> نادرست است و سرویس‌های
/// بالادست پیام فارسی روشن («سرویس ایمیل تنظیم نشده است») می‌دهند — نه کرش.
/// </remarks>
public interface IEmailSender
{
    /// <summary>آیا SMTP پیکربندی شده است؟ (میزبان و نشانی فرستنده پر باشند)</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// ارسال یک ایمیل — بدنه به‌صورت HTML ارسال می‌شود (متن ساده هم پشتیبانی می‌شود).
    /// </summary>
    /// <param name="to">نشانی گیرنده</param>
    /// <param name="subject">موضوع نامه</param>
    /// <param name="body">بدنهٔ نامه — HTML</param>
    /// <param name="attachments">پیوست‌های اختیاری</param>
    /// <param name="ct">توکن لغو</param>
    /// <returns>نتیجهٔ ارسال — هرگز استثنا پرتاب نمی‌کند</returns>
    Task<EmailSendResult> SendAsync(string to, string subject, string body,
        IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken ct = default);
}
