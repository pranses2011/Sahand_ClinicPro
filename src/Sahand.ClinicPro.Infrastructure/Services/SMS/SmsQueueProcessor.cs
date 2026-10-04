using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.SMS;

/// <summary>
/// پردازشگر صف پیامک — با Hangfire هر دقیقه اجرا می‌شود
/// </summary>
public class SmsQueueProcessor
{
    private readonly ClinicProDbContext _context;
    private readonly ISmsService _sms;
    private readonly ILogger<SmsQueueProcessor> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public SmsQueueProcessor(ClinicProDbContext context, ISmsService sms,
        ILogger<SmsQueueProcessor> logger)
    {
        _context = context;
        _sms = sms;
        _logger = logger;
    }

    /// <summary>
    /// پردازش آیتم‌های سررسیده صف — حداکثر ۵۰ پیام در هر اجرا
    /// سه تلاش ناموفق باعث رد شدن آیتم می‌شود
    /// </summary>
    public async Task ProcessQueueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var dueItems = await _context.SmsQueue
            .Where(q => q.Status == SmsStatus.Queued && q.ScheduledAt <= now)
            .OrderBy(q => q.Priority).ThenBy(q => q.ScheduledAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var item in dueItems)
        {
            var result = await _sms.SendAsync(item.Recipient, item.Message ?? string.Empty, ct);

            if (result.Success)
            {
                item.Status = SmsStatus.Sent;
                item.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                item.Attempts++;
                if (item.Attempts >= 3)
                {
                    item.Status = SmsStatus.Failed;
                    item.ProcessedAt = DateTime.UtcNow;
                }
            }
        }

        if (dueItems.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("{Count} آیتم صف پیامک پردازش شد", dueItems.Count);
        }
    }
}
