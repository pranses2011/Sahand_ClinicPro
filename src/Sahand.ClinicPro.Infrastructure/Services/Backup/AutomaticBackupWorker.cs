using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Category == "Backup")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

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
            _logger.LogWarning("پشتیبان خودکار ناموفق بود — {Error}", error);
        else
            _logger.LogInformation("پشتیبان خودکار فشرده شد — {Zip}", zipPath);
    }

    /// <summary>خواندن مقدار کلید از دیکشنری تنظیمات</summary>
    private static string? GetSetting(Dictionary<string, string?> map, string key)
        => map.TryGetValue(key, out var v) ? v : null;
}
