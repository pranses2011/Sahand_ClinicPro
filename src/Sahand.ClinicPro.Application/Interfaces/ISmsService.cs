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
