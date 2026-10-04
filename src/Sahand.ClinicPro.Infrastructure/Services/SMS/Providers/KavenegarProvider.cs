using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
