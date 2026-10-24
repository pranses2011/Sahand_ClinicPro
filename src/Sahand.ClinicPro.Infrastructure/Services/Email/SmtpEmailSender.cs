using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;

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

    /// <summary>سازنده با تزریق پیکربندی و ثبت‌گر</summary>
    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
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

    /// <summary>آیا SMTP پیکربندی شده است؟</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    /// <summary>
    /// ارسال ایمیل با SMTP — بدنه HTML، پیوست‌های اختیاری، خطای همیشه گزارش‌شده.
    /// </summary>
    public async Task<EmailSendResult> SendAsync(string to, string subject, string body,
        IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return EmailSendResult.Fail(
                "سرویس ایمیل در سرور تنظیم نشده است — بخش Email در appsettings را پر کنید");

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
