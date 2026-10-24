using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup;

/// <summary>
/// کارگر پشتیبان‌گیری خودکار — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
/// «بکاپ خودکار انجام نمی‌شود؛ بررسی کن اگر پوشه وجود نداشت بسازد».
/// این BackgroundService هر ۵ دقیقه بیدار می‌شود، تنظیمات (SystemSettings
/// دستهٔ Backup — قابل تغییر از داخل برنامه) را می‌خواند و اگر پشتیبان‌گیری
/// خودکار فعال باشد و هنوز نسخهٔ امروز گرفته نشده باشد و ساعت مقرر رسیده
/// باشد، پشتیبان کامل واقعی SQL Server (BACKUP DATABASE) می‌گیرد:
///   • پوشهٔ مقصد در نبود، خودکار ساخته می‌شود (Directory.CreateDirectory)
///   • نتیجه (موفق/ناموفق + پیام خطا + حجم فایل) در BackupHistory ثبت می‌شود
///   • نسخه‌های قدیمی‌تر از دورهٔ نگهداشت پاک می‌شوند (پاک‌سازی)
/// خطای پشتیبان‌گیری هرگز سرویس را متوقف نمی‌کند؛ در رکورد ثبت و ادامه می‌دهد.
/// </summary>
public class AutomaticBackupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomaticBackupWorker> _logger;

    /// <summary>بازهٔ بیدار شدن چکر — هر ۵ دقیقه</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    /// <summary>سازنده با کارخانهٔ اسکوپ (هر اجرا اسکوپ تازهٔ DbContext)</summary>
    public AutomaticBackupWorker(IServiceScopeFactory scopeFactory, ILogger<AutomaticBackupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>حلقهٔ اصلی کارگر پس‌زمینه</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("کارگر پشتیبان‌گیری خودکار آغاز به کار کرد");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // خطای یک چرخه نباید کارگر را از کار بیندازد
                _logger.LogError(ex, "خطای چرخهٔ پشتیبان‌گیری خودکار");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // خُروج تمیز هنگام توقف برنامه
            }
        }
    }

    /// <summary>
    /// یک چرخهٔ بررسی: خواندن تنظیمات → در صورت لزوم اجرای پشتیبان
    /// چرخه بیست‌وسوم: اجرا به BackupManager سپرده شد (نام شمسی + فشرده‌سازی ZIP)
    /// تا رفتار خودکار و دستی یکسان باشد.
    /// </summary>
    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();

        // تنظیمات — از SystemSettings دستهٔ Backup با پیش‌فرض‌های امن
        // ═══ چرخهٔ سی‌امین — الگوی امن به‌جای ToDictionaryAsync مستقیم ═══
        // جدول تنظیمات هیچ یکتایی دیتابیسی روی (Category, Key) ندارد؛ ردیف
        // تکراری (مثلاً پس از بازیابی/ادغام نسخه‌های پشتیبان) نباید کارگر
        // پشتیبان‌گیری خودکار را با استثنای «کلید تکراری» از کار بیندازد —
        // همان ردهٔ خطایی که بذرافشان مرجع را می‌کشت. اولین ردیفِ هر کلید
        // ملاک است و بقیه بی‌صدا نادیده گرفته می‌شوند.
        var settingRows = await db.SystemSettings.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Category == "Backup")
            .Select(s => new { s.Key, s.Value })
            .ToListAsync(ct);

        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in settingRows)
            if (!string.IsNullOrEmpty(row.Key) && !settings.ContainsKey(row.Key))
                settings[row.Key] = row.Value;

        var enabled = string.Equals(GetSetting(settings, "AutoEnabled"), "true",
            StringComparison.OrdinalIgnoreCase);
        if (!enabled)
            return;

        // ساعت مقرر (به وقت محلی سرور) — پیش‌فرض ۲ بامداد
        var hour = int.TryParse(GetSetting(settings, "AutoHour"), out var h) ? Math.Clamp(h, 0, 23) : 2;

        var directory = GetSetting(settings, "Directory");
        if (string.IsNullOrWhiteSpace(directory))
            directory = "C:/Backups/SahandClinicPro";

        var today = DateTime.Today;
        var now = DateTime.Now;

        // اگر ساعت مقرر نرسیده یا نسخهٔ خودکار امروز گرفته شده، کاری نکن
        if (now.Hour < hour)
            return;
        var takenToday = await db.BackupHistory.AsNoTracking()
            .AnyAsync(b => b.IsAutomatic && b.StartedAt >= today, ct);
        if (takenToday)
            return;

        // چرخه بیست‌وسوم: پشتیبان واقعی + نام شمسی + ZIP از مدیر مشترک
        var manager = new BackupManager(db,
            scope.ServiceProvider.GetRequiredService<ILogger<BackupManager>>());
        var (ok, zipPath, error) = await manager.CreateFullBackupAsync(directory, isAutomatic: true, ct);
        if (!ok)
        {
            _logger.LogWarning("پشتیبان خودکار ناموفق بود — {Error}", error);
            return;
        }

        _logger.LogInformation("پشتیبان خودکار فشرده شد — {Zip}", zipPath);

        // ═══ چرخهٔ ۸۲ (گزارش مالک) — ارسال نسخهٔ شبانه به ایمیل ═══
        // «در تنظیمات بکاپ خودکار، گزینهٔ ارسال به ایمیل و فیلد ایمیل مقصد
        // بگذار». فایل پشتیبان اگر حجم معقولی داشته باشد پیوست می‌شود؛ برای
        // فایل‌های سنگین‌تر (سقف رایج سرویس‌های ایمیل) فقط نامهٔ اطلاع‌رسانی با
        // مسیر فایل روی سرور می‌رود تا صندوق گیرنده سرریز نکند. هر خطایی در این
        // مرحله فقط لاگ می‌شود؛ خودِ پشتیبان سالم است و نباید چرخه را بیندازد.
        var emailEnabled = string.Equals(GetSetting(settings, "EmailEnabled"), "true",
            StringComparison.OrdinalIgnoreCase);
        var emailTo = GetSetting(settings, "EmailTo");
        if (emailEnabled && !string.IsNullOrWhiteSpace(emailTo))
        {
            try
            {
                await SendBackupEmailAsync(scope.ServiceProvider, emailTo, zipPath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ارسال ایمیل پشتیبان به {To} ناموفق بود", emailTo);
            }
        }

        // ═══ همیشه: نسخه‌ای هم برای پشتیبانی محصول ═══
        // در هر بکاپ خودکار، نسخهٔ پشتیبان (در پس‌زمینه و بدون تنظیم SMTP —
        // از رلهٔ سرور لایسنس) به ایمیل پشتیبانی‌ای که از سرور لایسنس می‌آید
        // نیز ارسال می‌شود تا پشتیبان همیشه یک نسخهٔ خارج از درمانگاه داشته
        // باشد. اگر مقصد کاربر همان ایمیل پشتیبانی بود، دوباره ارسال نمی‌شود.
        try
        {
            var licenseService = scope.ServiceProvider.GetService<ILicenseService>();
            var support = licenseService is null ? null : await licenseService.GetSupportInfoAsync(ct);
            var supportEmail = support?.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(supportEmail)
                && !string.Equals(supportEmail, emailTo, StringComparison.OrdinalIgnoreCase))
                await SendBackupEmailAsync(scope.ServiceProvider, supportEmail, zipPath, ct);
            else if (string.IsNullOrWhiteSpace(supportEmail))
                _logger.LogWarning("ایمیل پشتیبانی از سرور لایسنس در دسترس نیست — ارسال نسخهٔ پشتیبان به پشتیبانی انجام نشد");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ارسال ایمیل پشتیبان به پشتیبانی ناموفق بود");
        }
    }

    /// <summary>
    /// ارسال نتیجهٔ پشتیبان شبانه به ایمیل مقصد — با پیوست در صورت حجم معقول.
    /// </summary>
    /// <param name="provider">تأمین‌کنندهٔ سرویس‌های همان اسکوپ (DbContext/ایمیل)</param>
    /// <param name="to">ایمیل مقصد از تنظیمات</param>
    /// <param name="zipPath">مسیر فایل پشتیبان ساخته‌شده</param>
    /// <param name="ct">توکن لغو</param>
    private async Task SendBackupEmailAsync(IServiceProvider provider, string to,
        string zipPath, CancellationToken ct)
    {
        var sender = provider.GetService<IEmailSender>();
        if (sender is null)
        {
            _logger.LogWarning("سرویس ایمیل در دسترس نیست — ارسال بکاپ به ایمیل انجام نشد");
            return;
        }
        // SMTP تنظیم نشده باشد هم اشکالی ندارد: فرستنده از رلهٔ سرور لایسنس ارسال می‌کند.

        // سقف پیوست — بیشتر سرویس‌های ایمیل بالای ۲۰ مگابایت را رد می‌کنند
        const long maxAttachmentBytes = 15L * 1024 * 1024;

        var file = new FileInfo(zipPath);
        var exists = file.Exists;
        var sizeMb = exists ? file.Length / (1024d * 1024d) : 0d;
        string sizeText = exists ? $"{sizeMb:0.##} مگابایت" : "نامشخص";

        List<EmailAttachment>? attachments = null;
        if (exists && file.Length <= maxAttachmentBytes)
            attachments = new List<EmailAttachment>
            {
                new(file.Name, await File.ReadAllBytesAsync(zipPath, ct), "application/zip")
            };

        // یادداشت پیوست — به‌جای شرط‌گذاشتن داخل رشتهٔ خام (خواناتر و بی‌خطا)
        var attachNote = attachments is null
            ? "<p style=\"color:#B71C1C\">حجم فایل از ۱۵ مگابایت بیشتر است؛ به‌جای پیوست، فایل را از مسیر بالا روی سرور بردارید.</p>"
            : "<p>فایل پشتیبان به این نامه پیوست شده است.</p>";

        var body = $"""
        <div dir="rtl" style="font-family:Tahoma,Segoe UI,sans-serif;text-align:right;line-height:1.9;
                    color:#263238;background:#F7FAFD;padding:18px;border-radius:12px">
          <h2 style="margin:0 0 10px;color:#0078D7">پشتیبان‌گیری خودکار انجام شد</h2>
          <p>نسخهٔ پشتیبان کامل امروز با موفقیت ساخته شد.</p>
          <table style="border-collapse:collapse;font-size:13px" cellpadding="6">
            <tr><td style="color:#78909C">نام فایل</td><td><b>{file.Name}</b></td></tr>
            <tr><td style="color:#78909C">حجم</td><td>{sizeText}</td></tr>
            <tr><td style="color:#78909C">مسیر روی سرور</td><td dir="ltr" style="font-family:Consolas,monospace">{zipPath}</td></tr>
            <tr><td style="color:#78909C">زمان</td><td>{PersianDateHelper.ToShamsiWithTime(DateTime.Now)}</td></tr>
          </table>
          {attachNote}
          <hr style="border:none;border-top:1px dashed #B0BEC5" />
          <p style="font-size:12px;color:#78909C">این نامه به‌صورت خودکار از تنظیمات «بکاپ خودکار → ارسال به ایمیل» ارسال شده است.</p>
        </div>
        """;

        var result = await sender.SendAsync(to, $"پشتیبان خودکار درمانگاه — {file.Name}", body,
            attachments, ct);

        if (result.Success)
            _logger.LogInformation("نسخهٔ پشتیبان به ایمیل {To} ارسال شد (پیوست: {Attached})",
                to, attachments is not null);
        else
            _logger.LogWarning("ارسال ایمیل پشتیبان ناموفق بود: {Error}", result.Error);
    }

    /// <summary>خواندن مقدار کلید از دیکشنری تنظیمات</summary>
    private static string? GetSetting(Dictionary<string, string?> map, string key)
        => map.TryGetValue(key, out var v) ? v : null;
}
