using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Services.SMS;

namespace Sahand.ClinicPro.Infrastructure.Services.SMS;

/// <summary>
/// سرویس پیامک — ارسال با زنجیره جایگزینی و ثبت کامل در دیتابیس
/// </summary>
public class SmsService : ISmsService
{
    private readonly SmsProviderFactory _factory;
    private readonly ClinicProDbContext _context;
    private readonly ILogger<SmsService> _logger;

    /// <summary>سازنده با تزریق کارخانه پنل‌ها و کانتکست</summary>
    public SmsService(SmsProviderFactory factory, ClinicProDbContext context,
        ILogger<SmsService> logger)
    {
        _factory = factory;
        _context = context;
        _logger = logger;
    }

    /// <summary>ارسال فوری — همه پنل‌های زنجیره به ترتیب امتحان می‌شوند</summary>
    public async Task<SmsSendResult> SendAsync(string recipient, string message,
        CancellationToken ct = default)
    {
        var chain = _factory.GetChain();
        if (chain.Count == 0)
            return SmsSendResult.Fail("هیچ پنل پیامکی فعال نیست");

        foreach (var provider in chain)
        {
            var result = await provider.SendRawAsync(recipient, message, ct);
            if (result.Success)
            {
                await LogAsync(recipient, message, result, ct);
                return result;
            }
            _logger.LogWarning("پنل {Provider} ناموفق: {Error} — تلاش پنل بعدی",
                provider.ProviderCode, result.Error);
        }

        var failure = SmsSendResult.Fail("همه پنل‌های پیامک ناموفق بودند");
        await LogAsync(recipient, message, failure, ct);
        return failure;
    }

    /// <summary>ارسال با قالب — متغیرهای {name} جایگزین می‌شوند</summary>
    public async Task<SmsSendResult> SendTemplateAsync(string recipient, string templateCode,
        Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var template = await _context.SmsTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, ct);

        if (template?.Content is null)
            return SmsSendResult.Fail($"قالب پیامک {templateCode} یافت نشد");

        var message = template.Content;
        foreach (var (key, value) in variables)
            message = message.Replace($"{{{key}}}", value);

        var result = await SendAsync(recipient, message, ct);

        if (result.Success)
        {
            // به‌روزرسانی آمار قالب
            template.Variables = variables.Count.ToString();
            await _context.SaveChangesAsync(ct);
        }

        return result;
    }

    /// <summary>
    /// افزودن به صف ارسال پس‌زمینه — چرخهٔ ۶۹: رفع باگ تاریخی
    /// پیش از این «کدِ قالب» عیناً در ستون Message صف می‌نشست و پردازشگر صف
    /// همان رشتهٔ خام («APPOINTMENT_REMINDER») را برای بیمار پیامک می‌کرد!
    /// اکنون قالب از دیتابیس خوانده و متغیرها جایگزین می‌شوند و «متن نهایی»
    /// در صف می‌نشیند؛ اگر قالب فعال یافت نشد، صف رد می‌شود و خطا ثبت می‌شود
    /// (پیام بی‌متن به بیمار نرود).
    /// </summary>
    public async Task EnqueueAsync(string recipient, string templateCode,
        Dictionary<string, string> variables, DateTime? scheduledAt = null,
        CancellationToken ct = default)
    {
        // رندر قالب پیش از صف‌کردن — متن واقعی باید در صف بنشیند نه کد قالب
        var template = await _context.SmsTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, ct);
        if (template?.Content is null)
        {
            _logger.LogError(
                "صف پیامک رد شد: قالب فعال {Code} یافت نشد — پیام بی‌متن برای {Recipient} ارسال نمی‌شود",
                templateCode, recipient);
            return;
        }

        var message = template.Content;
        foreach (var (key, value) in variables)
            message = message.Replace($"{{{key}}}", value);

        _context.SmsQueue.Add(new SmsQueueItem
        {
            Recipient = recipient,
            Message = message,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            Status = SmsStatus.Queued,
            Priority = 3
        });
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// افزودن پیام آماده (بدون قالب) به صف — چرخهٔ ۶۹ (فاز ۳ / F-03)
    /// برای پیام‌های پویا (یادآوری نوبت با تاریخ/ساعت/پزشک) که قالب دیتابیسی ندارند.
    /// </summary>
    public async Task EnqueueRawAsync(string recipient, string message,
        DateTime? scheduledAt = null, byte priority = 3,
        CancellationToken ct = default)
    {
        // گارد متن خالی — پیام بی‌متن هرگز وارد صف نمی‌شود (هم‌الگوی SendAsync)
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("صف پیامک خام رد شد: متن خالی برای {Recipient}", recipient);
            return;
        }

        _context.SmsQueue.Add(new SmsQueueItem
        {
            Recipient = recipient,
            Message = message,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            Status = SmsStatus.Queued,
            Priority = priority
        });
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>ثبت پیامک در دیتابیس — با یا بدون موفقیت</summary>
    private async Task LogAsync(string recipient, string message,
        SmsSendResult result, CancellationToken ct)
    {
        _context.SmsMessages.Add(new SmsMessage
        {
            Recipient = recipient,
            Message = message,
            Provider = result.Provider,
            ProviderMessageId = result.MessageId,
            Status = result.Success ? SmsStatus.Sent : SmsStatus.Failed,
            SentAt = result.Success ? DateTime.UtcNow : null,
            ErrorMessage = result.Error,
            MessageType = SmsMessageType.System
        });
        await _context.SaveChangesAsync(ct);
    }
}
