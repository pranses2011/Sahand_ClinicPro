namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// قرارداد سرویس پیامک — ارسال با قالب و مدیریت صف
/// </summary>
public interface ISmsService
{
    /// <summary>ارسال فوری پیامک متنی</summary>
    Task<SmsSendResult> SendAsync(string recipient, string message,
        CancellationToken ct = default);

    /// <summary>ارسال با قالب — متغیرها جایگزین می‌شوند</summary>
    Task<SmsSendResult> SendTemplateAsync(string recipient, string templateCode,
        Dictionary<string, string> variables, CancellationToken ct = default);

    /// <summary>افزودن به صف ارسال پس‌زمینه — پردازش با Hangfire</summary>
    Task EnqueueAsync(string recipient, string templateCode,
        Dictionary<string, string> variables, DateTime? scheduledAt = null,
        CancellationToken ct = default);

    /// <summary>
    /// افزودن پیام آماده (بدون قالب) به صف ارسال پس‌زمینه — چرخهٔ ۶۹ (فاز ۳ / F-03)
    /// یادآوری خودکار نوبت متن پیام را پویا می‌سازد (تاریخ/ساعت/پزشک) و نیازی به
    /// قالب دیتابیسی ندارد؛ ScheduledAt همان سازوکار «ساعات سکوت» است: پیامِ
    /// داخل بازهٔ سکوت برای اولین لحظهٔ فعال‌بودن ارسال برنامه‌ریزی می‌شود.
    /// </summary>
    /// <param name="recipient">شماره موبایل گیرنده</param>
    /// <param name="message">متن کامل پیام</param>
    /// <param name="scheduledAt">زمان برنامه‌ریزی‌شدهٔ ارسال (UTC) — پیش‌فرض: فوری</param>
    /// <param name="priority">اولویت صف — عدد کوچک‌تر = زودتر (پیش‌فرض ۳ = عادی)</param>
    Task EnqueueRawAsync(string recipient, string message,
        DateTime? scheduledAt = null, byte priority = 3,
        CancellationToken ct = default);
}

/// <summary>
/// نتیجه ارسال پیامک
/// </summary>
public record SmsSendResult(bool Success, string? Provider, string? MessageId, string? Error)
{
    /// <summary>ساخت نتیجه موفق</summary>
    public static SmsSendResult Ok(string provider, string messageId) =>
        new(true, provider, messageId, null);

    /// <summary>ساخت نتیجه ناموفق</summary>
    public static SmsSendResult Fail(string error) => new(false, null, null, error);
}
