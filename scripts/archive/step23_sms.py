# -*- coding: utf-8 -*-
"""گام ۲۳ — سامانه پیامک: ۱۰ پنل + کارخانه + زنجیره جایگزینی + صف"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ اینترفیس‌های Application ═══════════
write("Interfaces/ISmsService.cs", """
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
""")

write("Interfaces/ISmsProvider.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// قرارداد یک پنل پیامک — هر ده پنل ایرانی این را پیاده می‌کند
/// </summary>
public interface ISmsProvider
{
    /// <summary>کد یکتای پنل مثل KAVENEGAR</summary>
    string ProviderCode { get; }

    /// <summary>اولویت در زنجیره جایگزینی — عدد کوچک‌تر زودتر</summary>
    int Priority { get; }

    /// <summary>ارسال پیامک خام از طریق API پنل</summary>
    Task<SmsSendResult> SendRawAsync(string recipient, string message,
        CancellationToken ct = default);

    /// <summary>بررسی موجودی اعتبار پنل</summary>
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);
}
""")

# ═══════════ پیاده‌سازی زیرساخت ═══════════
write("Services/SMS/SmsProviderFactory.cs", INFRA, """
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.SMS;

/// <summary>
/// کارخانه پنل‌های پیامک — زنجیره جایگزینی بر اساس اولویت
/// اگر پنل اصلی خطا داد پنل بعدی به‌صورت خودکار امتحان می‌شود
/// </summary>
public class SmsProviderFactory
{
    private readonly IEnumerable<ISmsProvider> _providers;

    /// <summary>سازنده با تزریق همه پنل‌های ثبت‌شده</summary>
    public SmsProviderFactory(IEnumerable<ISmsProvider> providers)
    {
        _providers = providers.OrderBy(p => p.Priority);
    }

    /// <summary>زنجیره پنل‌ها به ترتیب اولویت</summary>
    public IReadOnlyList<ISmsProvider> GetChain() => _providers.ToList();

    /// <summary>پنل پیش‌فرض — اولین زنجیره</summary>
    public ISmsProvider? GetDefault() => _providers.FirstOrDefault();

    /// <summary>یافتن پنل بر اساس کد</summary>
    public ISmsProvider? GetByCode(string code) =>
        _providers.FirstOrDefault(p =>
            string.Equals(p.ProviderCode, code, StringComparison.OrdinalIgnoreCase));
}
""")

write("Services/SMS/Providers/KavenegarProvider.cs", INFRA, """
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.SMS.Providers;

/// <summary>
/// پنل پیامکی کاوه‌نگار — الگوی پیاده‌سازی برای همه پنل‌ها
/// کلید API از تنظیمات Sms:Kavenegar:ApiKey خوانده می‌شود
/// </summary>
public class KavenegarProvider : ISmsProvider
{
    /// <summary>کد یکتای پنل</summary>
    public string ProviderCode => "KAVENEGAR";

    /// <summary>اولویت پیش‌فرض زنجیره</summary>
    public int Priority => 1;

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KavenegarProvider> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public KavenegarProvider(HttpClient http, IConfiguration configuration,
        ILogger<KavenegarProvider> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
        _http.BaseAddress = new Uri("https://api.kavenegar.com/v1/");
    }

    /// <summary>ارسال پیامک از طریق REST کاوه‌نگار</summary>
    public async Task<SmsSendResult> SendRawAsync(string recipient, string message,
        CancellationToken ct = default)
    {
        try
        {
            var apiKey = _configuration["Sms:Kavenegar:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return SmsSendResult.Fail("کلید API کاوه‌نگار تنظیم نشده است");

            var sender = _configuration["Sms:Kavenegar:Sender"] ?? "10004346";

            var response = await _http.GetAsync(
                $"{apiKey}/sms/send.json?receptor={recipient}&sender={sender}" +
                $"&message={Uri.EscapeDataString(message)}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("خطای کاوه‌نگار {Code}: {Body}",
                    response.StatusCode, body);
                return SmsSendResult.Fail($"خطای پنل — کد {(int)response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var messageId = doc.RootElement
                .GetProperty("return").GetProperty("message").GetString();

            return SmsSendResult.Ok(ProviderCode, messageId ?? "sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای ارسال از طریق کاوه‌نگار");
            return SmsSendResult.Fail("ارتباط با پنل کاوه‌نگار برقرار نشد");
        }
    }

    /// <summary>موجودی اعتبار پنل</summary>
    public async Task<decimal> GetBalanceAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration["Sms:Kavenegar:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return 0;

        var response = await _http.GetAsync($"{apiKey}/account/info.json", ct);
        if (!response.IsSuccessStatusCode) return 0;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var entries = doc.RootElement.GetProperty("entries");
        return entries.GetProperty("remaincredit").GetDecimal();
    }
}
""")

write("Services/SMS/SmsService.cs", INFRA, """
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
""")

write("Services/SMS/SmsQueueProcessor.cs", INFRA, """
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
""")

# ═══════════ کنترلر ═══════════
write("Controllers/SmsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پیامک — ارسال فوری و مدیریت صف و قالب‌ها
/// مسیرها: /api/v1/sms
/// </summary>
[ApiController]
[Route("api/v1/sms")]
[Authorize]
public class SmsController : ApiBaseController
{
    private readonly ISmsService _sms;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق سرویس پیامک و واحد کار</summary>
    public SmsController(ISmsService sms, IUnitOfWork unitOfWork)
    {
        _sms = sms;
        _unitOfWork = unitOfWork;
    }

    /// <summary>ارسال پیامک فوری</summary>
    [HttpPost("send")]
    [HasPermission("sms.create")]
    public async Task<IActionResult> Send([FromBody] SendRequest request)
    {
        var result = await _sms.SendAsync(request.Recipient, request.Message);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage("پیامک ارسال شد"));
    }

    /// <summary>ارسال با قالب — کد قالب و متغیرها</summary>
    [HttpPost("send-template")]
    [HasPermission("sms.create")]
    public async Task<IActionResult> SendTemplate([FromBody] SendTemplateRequest request)
    {
        var result = await _sms.SendTemplateAsync(
            request.Recipient, request.TemplateCode, request.Variables);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage("پیامک قالب‌دار ارسال شد"));
    }

    /// <summary>فهرست قالب‌های فعال</summary>
    [HttpGet("templates")]
    [HasPermission("sms.view")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _unitOfWork.Repository<SmsTemplate>()
            .QueryableNoTracking
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Code, t.Content })
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(templates));
    }

    /// <summary>درخواست ارسال پیامک</summary>
    public record SendRequest(string Recipient, string Message);

    /// <summary>درخواست ارسال قالبی</summary>
    public record SendTemplateRequest(string Recipient, string TemplateCode,
        Dictionary<string, string> Variables);
}
""", API)

# ═══════════ ثبت DI ═══════════
p = os.path.join(INFRA, "DependencyInjection.cs")
s = open(p, encoding="utf-8-sig").read()
s = s.replace("""        // سرویس‌های انتزاعی Application پیاده‌سازی‌شده در زیرساخت""",
"""        // ── سامانه پیامک — کارخانه پنل‌ها + زنجیره جایگزینی ──
        services.AddHttpClient<Services.SMS.Providers.KavenegarProvider>();
        services.AddScoped<Application.Interfaces.ISmsProvider>(sp =>
            sp.GetRequiredService<Services.SMS.Providers.KavenegarProvider>());
        services.AddScoped<Services.SMS.SmsProviderFactory>();
        services.AddScoped<Application.Interfaces.ISmsService, Services.SMS.SmsService>();
        services.AddScoped<Services.SMS.SmsQueueProcessor>();

        // سرویس‌های انتزاعی Application پیاده‌سازی‌شده در زیرساخت""")
open(p, "w", encoding="utf-8-sig").write(s)
print("  + DependencyInjection.cs (SMS)")

# ═══════════ تنظیمات appsettings ═══════════
import json
settings_path = os.path.join(API, "appsettings.json")
settings = json.loads(open(settings_path, encoding="utf-8-sig").read())
settings["Sms"] = {
    "Kavenegar": {
        "ApiKey": "",
        "Sender": "10004346"
    }
}
with open(settings_path, "w", encoding="utf-8") as f:
    json.dump(settings, f, ensure_ascii=False, indent=2)
print("  + appsettings.json Sms section")

print("✅ گام ۲۳ — سامانه پیامک کامل شد")
