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

    /// <summary>افزودن به صف ارسال پس‌زمینه</summary>
    public async Task EnqueueAsync(string recipient, string templateCode,
        Dictionary<string, string> variables, DateTime? scheduledAt = null,
        CancellationToken ct = default)
    {
        _context.SmsQueue.Add(new SmsQueueItem
        {
            Recipient = recipient,
            Message = templateCode,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            Status = SmsStatus.Queued,
            Priority = 3
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
