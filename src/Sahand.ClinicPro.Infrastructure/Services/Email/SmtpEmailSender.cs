using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.Email;

/// <summary>
/// فرستندهٔ ایمیل با SMTP — چرخهٔ ۸۲ (گزارش مالک):
/// «فراموشی رمز» در صفحهٔ ورود بی‌اثر بود («رمز فعلی به ایمیل کاربر ارسال
/// شود») و در تنظیمات بکاپ خودکار هم گزینهٔ «ارسال به ایمیل» نبود.
/// </summary>
/// <remarks>
/// ═══ پیکربندی ═══
/// بخش «Email» در appsettings سرور؛ همهٔ کلیدها اختیاری‌اند و پیش‌فرض‌های امن
/// دارند (پورت ۵۸۷، SSL روشن). همان الگوی پنل پیامکی کاوه‌نگار — پیکربندی از
/// <see cref="IConfiguration"/> و نه هاردکد.
///
/// ═══ چرا SmtpClient ═══
/// پروژه هیچ کتابخانهٔ ایمیل بیرونی ندارد و افزودن بستهٔ نو در این چرخه یعنی
/// ریسک بیلد/بازبینی ممیزی؛ <c>System.Net.Mail.SmtpClient</c> در .NET 8 موجود
/// است و برای ایمیل تراکنشیِ یک درمانگاه (چند نامه در روز) کافی است.
/// خطا هرگز به بیرون پرتاب نمی‌شود؛ همه‌چیز به <see cref="EmailSendResult"/>
/// فارسی تبدیل می‌شود تا مصرف‌کننده (بازیابی رمز/بکاپ) پیام کاربرپسند بدهد.
/// </remarks>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly ClinicProDbContext _db;

    /// <summary>سازنده با تزریق پیکربندی و ثبت‌گر</summary>
    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger,
        ClinicProDbContext db)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    /// <summary>میزبان SMTP — تهی یعنی پیکربندی نشده</summary>
    private string? Host => _configuration["Email:Host"];

    /// <summary>پورت SMTP — پیش‌فرض ۵۸۷ (STARTTLS)</summary>
    private int Port => int.TryParse(_configuration["Email:Port"], out var p) && p > 0 ? p : 587;

    /// <summary>استفاده از SSL — پیش‌فرض روشن</summary>
    private bool UseSsl =>
        !string.Equals(_configuration["Email:UseSsl"], "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>نشانی فرستنده — در نبود «From» همان نام کاربری SMTP</summary>
    private string? FromAddress =>
        _configuration["Email:From"] is { Length: > 0 } from ? from : _configuration["Email:User"];

    /// <summary>نام نمایشی فرستنده — پیش‌فرض «درمانگاه سهند»</summary>
    private string FromName =>
        _configuration["Email:FromName"] is { Length: > 0 } name ? name : "درمانگاه سهند";

    /// <summary>آیا SMTP دستی پیکربندی شده است؟</summary>
    private bool SmtpConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    /// <summary>
    /// آیا ارسال ایمیل ممکن است؟ — بله، حتی بدون تنظیمات SMTP: در آن حالت نامه
    /// از «رلهٔ ایمیل سرور لایسنس» (action=send_email) با تابع mail() هاست
    /// پشتیبانی ارسال می‌شود و درمانگاه هیچ تنظیم SMTPای لازم ندارد.
    /// </summary>
    public bool IsConfigured => true;

    /// <summary>
    /// ارسال ایمیل با SMTP — بدنه HTML، پیوست‌های اختیاری، خطای همیشه گزارش‌شده.
    /// </summary>
    public async Task<EmailSendResult> SendAsync(string to, string subject, string body,
        IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken ct = default)
    {
        // بدون SMTP دستی ← رلهٔ سرور لایسنس (بدون نیاز به هیچ تنظیمی در درمانگاه)
        if (!SmtpConfigured)
            return await SendViaLicenseRelayAsync(to, subject, body, attachments, ct);

        if (string.IsNullOrWhiteSpace(to))
            return EmailSendResult.Fail("نشانی ایمیل گیرنده خالی است");

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(FromAddress!, FromName),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = LooksLikeHtml(body),
            };
            message.To.Add(to.Trim());

            foreach (var attachment in attachments ?? Array.Empty<EmailAttachment>())
            {
                var stream = new MemoryStream(attachment.Content, writable: false);
                var mailAttachment = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? new Attachment(stream, attachment.FileName)
                    : new Attachment(stream, attachment.FileName, attachment.ContentType);
                message.Attachments.Add(mailAttachment);
            }

            using var client = new SmtpClient(Host!, Port)
            {
                EnableSsl = UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000,
            };

            // احراز هویت فقط وقتی نام کاربری تنظیم شده باشد (سرورهای داخلی بدون auth)
            var user = _configuration["Email:User"];
            var password = _configuration["Email:Password"];
            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, password ?? string.Empty);

            await client.SendMailAsync(message, ct);

            var masked = MaskAddress(to);
            _logger.LogInformation("ایمیل «{Subject}» به {To} ارسال شد", subject, masked);
            return EmailSendResult.Ok();
        }
        catch (Exception ex)
        {
            // پیام دقیق فقط در لاگ؛ به کاربر پیام فارسی کوتاه
            _logger.LogError(ex, "ارسال ایمیل «{Subject}» به {To} ناموفق بود", subject,
                MaskAddress(to));
            return EmailSendResult.Fail("ارسال ایمیل ناموفق بود — پیکربندی SMTP را بررسی کنید");
        }
    }

    /// <summary>
    /// ارسال از رلهٔ ایمیل سرور لایسنس — سرور با mail() داخلی هاست ارسال می‌کند؛
    /// احراز با کلید لایسنس + اثر انگشت همین درمانگاه انجام می‌شود تا رله
    /// به اسپم‌فرست عمومی تبدیل نشود. پیوست بالای سقف رله بدون پیوست ارسال می‌شود.
    /// </summary>
    private async Task<EmailSendResult> SendViaLicenseRelayAsync(string to, string subject,
        string body, IReadOnlyList<EmailAttachment>? attachments, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to))
            return EmailSendResult.Fail("نشانی ایمیل گیرنده خالی است");
        try
        {
            var license = await _db.ClinicLicense.AsNoTracking().FirstOrDefaultAsync(ct);
            if (license is null || string.IsNullOrWhiteSpace(license.LicenseKey))
                return EmailSendResult.Fail(
                    "ارسال ایمیل بدون SMTP نیازمند لایسنس فعال است — ابتدا لایسنس را فعال کنید یا SMTP را تنظیم کنید");

            var storedUrl = await _db.SystemSettings.AsNoTracking()
                .Where(s => s.Category == "License" && s.Key == "License.PortalUrl" && !s.IsDeleted)
                .Select(s => s.Value).FirstOrDefaultAsync(ct);
            var portalUrl = LicensePortalHelper.NormalizeApiUrl(
                !string.IsNullOrWhiteSpace(storedUrl) ? storedUrl : _configuration["License:PortalUrl"]);

            var fingerprint = _configuration["License:HardwareFingerprint"];
            if (string.IsNullOrWhiteSpace(fingerprint))
                fingerprint = license.HardwareFingerprint;
            if (string.IsNullOrWhiteSpace(fingerprint))
                fingerprint = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        Encoding.UTF8.GetBytes(Environment.MachineName + "|" + Environment.OSVersion)));

            // سقف پیوست رله (JSON+Base64 روی هاست اشتراکی) — بالاتر از آن نامه بدون پیوست می‌رود
            const long maxRelayAttachment = 8L * 1024 * 1024;
            var attachment = attachments?.FirstOrDefault(a => a.Content.Length <= maxRelayAttachment);
            var droppedAttachment = attachments is { Count: > 0 } && attachment is null;

            var payload = new
            {
                action = "send_email",
                licenseKey = license.LicenseKey,
                fingerprint,
                to = to.Trim(),
                subject,
                body,
                attachmentName = attachment?.FileName,
                attachmentType = attachment?.ContentType,
                attachmentBase64 = attachment is null ? null : Convert.ToBase64String(attachment.Content),
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(portalUrl, content, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
            if (response.IsSuccessStatusCode && ok)
            {
                _logger.LogInformation("ایمیل «{Subject}» از رلهٔ سرور لایسنس به {To} ارسال شد (پیوست: {Attached})",
                    subject, MaskAddress(to), attachment is not null);
                if (droppedAttachment)
                    _logger.LogWarning("پیوست ایمیل «{Subject}» به‌علت حجم بالای سقف رله ارسال نشد", subject);
                return EmailSendResult.Ok();
            }
            var message = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : null;
            _logger.LogWarning("رلهٔ ایمیل سرور لایسنس نپذیرفت: {Message}", message);
            return EmailSendResult.Fail(message ?? "ارسال ایمیل از سرور لایسنس ناموفق بود");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ارسال ایمیل از رلهٔ سرور لایسنس به {To} ناموفق بود", MaskAddress(to));
            return EmailSendResult.Fail("ارسال ایمیل ناموفق بود — اتصال اینترنت سرور یا وضعیت لایسنس را بررسی کنید");
        }
    }

    /// <summary>بدنهٔ HTML؟ (رشتهٔ ساده هم مجاز است)</summary>
    private static bool LooksLikeHtml(string body) =>
        body.Contains('<') && body.Contains('>');

    /// <summary>پوشاندن بخشی از نشانی در لاگ — جلوگیری از ثبت داده شناسایی کاربر</summary>
    private static string MaskAddress(string address)
    {
        var at = address.IndexOf('@');
        if (at <= 1) return "***";
        return $"{address[0]}***{address[at..]}";
    }
}
