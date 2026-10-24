using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup;

/// <summary>نتیجهٔ اطلاع‌رسانی ایمیلی یک پشتیبان تازه‌ساخته‌شده</summary>
/// <param name="SupportSent">ایمیل پشتیبانی ارسال شد؟ (همیشه تلاش می‌شود)</param>
/// <param name="UserSent">ایمیل کاربر درمانگاه ارسال شد؟ (فقط با تنظیم او)</param>
/// <param name="Note">یادداشت فارسی خلاصه برای نمایش/لاگ</param>
public sealed record BackupEmailNotice(bool SupportSent, bool UserSent, string Note);

/// <summary>
/// اطلاع‌رسان ایمیل پشتیبان — مسیر مشترک «بکاپ خودکار» و «بکاپ فوری».
/// قاعدهٔ مالک: ایمیل پشتیبانی (از سرور لایسنس) «همیشه و در همه حال» پس از
/// هر پشتیبان موفق ارسال می‌شود و هیچ تنظیمی خاموشش نمی‌کند؛ تنظیم
/// «ارسال به ایمیل» در تنظیمات بکاپ خودکار فقط برای ایمیل خودِ درمانگاه است.
/// نامهٔ پشتیبانی مشخصات کامل درمانگاه را دارد تا بکاپ درمانگاه‌های مختلف
/// برای پشتیبانی قابل تشخیص باشند. هیچ خطایی از این سرویس بیرون نمی‌رود تا
/// موفقیتِ خودِ پشتیبان هرگز زیر سؤال نرود.
/// </summary>
public class BackupEmailNotifier
{
    private readonly ClinicProDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<BackupEmailNotifier> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public BackupEmailNotifier(ClinicProDbContext db, IEmailSender emailSender,
        ILicenseService licenseService, ILogger<BackupEmailNotifier> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>
    /// ارسال ایمیل‌های یک پشتیبان موفق — پشتیبانی همیشه + کاربر در صورت تنظیم.
    /// </summary>
    /// <param name="backupPath">مسیر فایل پشتیبان ساخته‌شده (ZIP یا BAK)</param>
    /// <param name="isAutomatic">خودکار (شبانه) یا فوری (دستی)</param>
    /// <param name="ct">توکن لغو</param>
    public async Task<BackupEmailNotice> NotifyAsync(string backupPath, bool isAutomatic,
        CancellationToken ct = default)
    {
        var kindText = isAutomatic ? "خودکار" : "فوری";

        // تنظیم ایمیل کاربر — فقط همین دو کلید رفتار کاربر را تعیین می‌کنند
        var backupSettings = await ReadSettingsAsync("Backup", ct);
        var userEnabled = string.Equals(Get(backupSettings, "EmailEnabled"), "true",
            StringComparison.OrdinalIgnoreCase);
        var userEmail = Get(backupSettings, "EmailTo")?.Trim();

        var clinicIdentity = await BuildClinicIdentityAsync(ct);

        var supportSent = false;
        var userSent = false;
        string? supportEmail = null;

        // ۱) پشتیبانی — همیشه و در همه حال، بدون هیچ تنظیمی
        try
        {
            var support = await _licenseService.GetSupportInfoAsync(ct);
            supportEmail = support?.Email?.Trim();
            if (string.IsNullOrWhiteSpace(supportEmail))
                _logger.LogWarning("ایمیل پشتیبانی از سرور لایسنس در دسترس نیست — ارسال بکاپ {Kind} به پشتیبانی انجام نشد", kindText);
            else
                supportSent = await SendOneAsync(supportEmail, backupPath, kindText,
                    clinicIdentity, isSupport: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال ایمیل بکاپ {Kind} به پشتیبانی ناموفق بود", kindText);
        }

        // ۲) کاربر درمانگاه — فقط اگر خودش در تنظیمات فعال کرده باشد؛
        // اگر نشانی‌اش همان ایمیل پشتیبانی باشد، نامهٔ پشتیبانی (با مشخصات
        // درمانگاه) قبلاً رسیده و دوباره ارسال نمی‌شود.
        if (userEnabled && !string.IsNullOrWhiteSpace(userEmail)
            && !string.Equals(userEmail, supportEmail, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                userSent = await SendOneAsync(userEmail, backupPath, kindText,
                    clinicIdentity, isSupport: false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ارسال ایمیل بکاپ {Kind} به {To} ناموفق بود", kindText, userEmail);
            }
        }

        var note = (supportSent, userSent) switch
        {
            (true, true) => "ایمیل پشتیبان برای پشتیبانی و ایمیل درمانگاه ارسال شد",
            (true, false) => userEnabled
                ? "ایمیل پشتیبان برای پشتیبانی ارسال شد (ارسال به ایمیل درمانگاه ناموفق یا هم‌نشانی با پشتیبانی بود)"
                : "ایمیل پشتیبان برای پشتیبانی ارسال شد",
            (false, true) => "ایمیل پشتیبان برای ایمیل درمانگاه ارسال شد (ارسال به پشتیبانی ناموفق بود)",
            _ => "ارسال ایمیل پشتیبان انجام نشد — اتصال ایمیل/لایسنس را بررسی کنید",
        };
        return new BackupEmailNotice(supportSent, userSent, note);
    }

    /// <summary>ارسال یک نامه — با پیوست تا سقف ۱۵ مگابایت، وگرنه اطلاع‌رسانی با مسیر فایل</summary>
    private async Task<bool> SendOneAsync(string to, string backupPath, string kindText,
        (string Name, string Html) clinicIdentity, bool isSupport, CancellationToken ct)
    {
        const long maxAttachmentBytes = 15L * 1024 * 1024;
        var file = new FileInfo(backupPath);
        var exists = file.Exists;
        var sizeText = exists ? $"{file.Length / (1024d * 1024d):0.##} مگابایت" : "نامشخص";

        List<EmailAttachment>? attachments = null;
        if (exists && file.Length <= maxAttachmentBytes)
            attachments = new List<EmailAttachment>
            {
                new(file.Name, await File.ReadAllBytesAsync(backupPath, ct),
                    backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                        ? "application/zip" : "application/octet-stream")
            };

        var attachNote = attachments is null
            ? "<p style=\"color:#B71C1C\">حجم فایل از ۱۵ مگابایت بیشتر است؛ به‌جای پیوست، فایل را از مسیر بالا روی سرور بردارید.</p>"
            : "<p>فایل پشتیبان به این نامه پیوست شده است.</p>";

        // نامهٔ پشتیبانی مشخصات درمانگاه را دارد؛ نامهٔ کاربر خلاصهٔ خودِ درمانگاه است
        var identityBlock = isSupport ? clinicIdentity.Html : string.Empty;
        var footer = isSupport
            ? "این نامه پس از هر پشتیبان‌گیری (خودکار یا فوری) به‌صورت خودکار برای پشتیبانی ارسال می‌شود."
            : "این نامه طبق تنظیم «ارسال به ایمیل» در تنظیمات بکاپ خودکار ارسال شده است.";

        var body = $"""
        <div dir="rtl" style="font-family:Tahoma,Segoe UI,sans-serif;text-align:right;line-height:1.9;
                    color:#263238;background:#F7FAFD;padding:18px;border-radius:12px">
          <h2 style="margin:0 0 10px;color:#0078D7">پشتیبان‌گیری {kindText} انجام شد</h2>
          <p>نسخهٔ پشتیبان کامل با موفقیت ساخته شد.</p>
          <table style="border-collapse:collapse;font-size:13px" cellpadding="6">
            <tr><td style="color:#78909C">نوع پشتیبان</td><td>{kindText}</td></tr>
            <tr><td style="color:#78909C">نام فایل</td><td><b>{file.Name}</b></td></tr>
            <tr><td style="color:#78909C">حجم</td><td>{sizeText}</td></tr>
            <tr><td style="color:#78909C">مسیر روی سرور</td><td dir="ltr" style="font-family:Consolas,monospace">{backupPath}</td></tr>
            <tr><td style="color:#78909C">زمان</td><td>{PersianDateHelper.ToShamsiWithTime(DateTime.Now)}</td></tr>
          </table>
          {identityBlock}
          {attachNote}
          <hr style="border:none;border-top:1px dashed #B0BEC5" />
          <p style="font-size:12px;color:#78909C">{footer}</p>
        </div>
        """;

        var subject = isSupport
            ? $"پشتیبان {kindText} — {clinicIdentity.Name} — {file.Name}"
            : $"پشتیبان {kindText} درمانگاه — {file.Name}";

        var result = await _emailSender.SendAsync(to, subject, body, attachments, ct);
        if (result.Success)
            _logger.LogInformation("ایمیل بکاپ {Kind} به {To} ارسال شد (پشتیبانی: {Support}، پیوست: {Attached})",
                kindText, to, isSupport, attachments is not null);
        else
            _logger.LogWarning("ارسال ایمیل بکاپ {Kind} به {To} ناموفق بود: {Error}", kindText, to, result.Error);
        return result.Success;
    }

    /// <summary>
    /// مشخصات درمانگاه استفاده‌کننده برای نامهٔ پشتیبانی — از تنظیمات
    /// ClinicInfo (همگام‌شده از سرور لایسنس) + رکورد لایسنس محلی.
    /// </summary>
    private async Task<(string Name, string Html)> BuildClinicIdentityAsync(CancellationToken ct)
    {
        var map = await ReadSettingsAsync("ClinicInfo", ct);
        var license = await _db.ClinicLicense.AsNoTracking().FirstOrDefaultAsync(ct);

        string Get(string key) => map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? System.Net.WebUtility.HtmlEncode(v.Trim()) : "—";
        var nameRaw = map.TryGetValue("ClinicName", out var n) && !string.IsNullOrWhiteSpace(n)
            ? n.Trim() : "درمانگاه (نام ثبت نشده)";
        var expiry = license?.ExpiryDate is { } e
            ? PersianDateHelper.ToShamsi(new DateTime(e.Year, e.Month, e.Day)) : "—";

        var html = $"""
          <h3 style="margin:14px 0 6px;color:#0078D7">مشخصات درمانگاه استفاده‌کننده</h3>
          <table style="border-collapse:collapse;font-size:13px" cellpadding="6">
            <tr><td style="color:#78909C">نام درمانگاه</td><td><b>{System.Net.WebUtility.HtmlEncode(nameRaw)}</b></td></tr>
            <tr><td style="color:#78909C">شماره/کلید لایسنس</td><td dir="ltr" style="font-family:Consolas,monospace">{Get("LicenseNumber")}</td></tr>
            <tr><td style="color:#78909C">شهر</td><td>{Get("City")}</td></tr>
            <tr><td style="color:#78909C">مدیر درمانگاه</td><td>{Get("ManagerName")}</td></tr>
            <tr><td style="color:#78909C">تلفن</td><td>{Get("Phone")}</td></tr>
            <tr><td style="color:#78909C">ایمیل درمانگاه</td><td dir="ltr">{Get("Email")}</td></tr>
            <tr><td style="color:#78909C">نشانی</td><td>{Get("Address")}</td></tr>
            <tr><td style="color:#78909C">کد اقتصادی / شناسهٔ ملی</td><td>{Get("EconomicCode")} / {Get("NationalId")}</td></tr>
            <tr><td style="color:#78909C">انقضای لایسنس</td><td>{expiry}</td></tr>
            <tr><td style="color:#78909C">نام ماشین سرور</td><td dir="ltr" style="font-family:Consolas,monospace">{System.Net.WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
          </table>
          """;
        return (nameRaw, html);
    }

    /// <summary>خواندن امن تنظیمات یک دسته — اولین ردیف هر کلید ملاک (مقاوم به ردیف تکراری)</summary>
    private async Task<Dictionary<string, string?>> ReadSettingsAsync(string category, CancellationToken ct)
    {
        var rows = await _db.SystemSettings.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Category == category)
            .Select(s => new { s.Key, s.Value }).ToListAsync(ct);
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            if (!string.IsNullOrEmpty(row.Key) && !map.ContainsKey(row.Key))
                map[row.Key] = row.Value;
        return map;
    }

    /// <summary>خواندن مقدار کلید از دیکشنری تنظیمات</summary>
    private static string? Get(Dictionary<string, string?> map, string key)
        => map.TryGetValue(key, out var v) ? v : null;
}
