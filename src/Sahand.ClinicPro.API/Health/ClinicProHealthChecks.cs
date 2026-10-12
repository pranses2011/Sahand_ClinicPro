using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.API.Health;

/// <summary>
/// نقاط بررسی سلامت استاندارد — چرخهٔ ۵۵ (BUG-20):
/// /health/live فقط «زنده‌بودن فرآیند» را می‌گوید (هیچ وابستگی‌ای نمی‌سنجد —
/// برای مانیتورینگِ restart) و /health/ready آمادگیِ سرویس‌دهی را می‌سنجد:
/// اتصال دیتابیس، فضای دیسکِ پشتیبان‌گیری و در صورت فعال‌بودن، ذخیرهٔ Hangfire.
/// نقطهٔ قدیمی /health (که کلاینت دسکتاپ با آن دسترسی سرور را می‌سنجد) دست‌نخورده ماند.
/// </summary>
public static class ClinicProHealthChecks
{
    /// <summary>حداقل فضای آزاد مجاز برای پشتیبان‌گیری — زیر آن، آمادگی «تخریب‌شده» است</summary>
    private const long MinimumFreeBytesForBackup = 1L * 1024 * 1024 * 1024; // ۱ گیگابایت

    /// <summary>نسخهٔ کامل محصول از فرادادهٔ اسمبلی (InformationalVersion = Directory.Build.props)</summary>
    private static string ProductVersion()
    {
        var attr = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attr?.InformationalVersion.Split('+')[0] ?? "unknown";
    }

    /// <summary>
    /// نگاشت نقاط سلامت — باید پس از Build و پیش از Run صدا زده شود.
    /// live/ready هر دو ناشناس‌اند (مانیتورینگ/پراب) و از توضیحات OpenAPI کنار گذاشته شده‌اند.
    /// </summary>
    public static IEndpointRouteBuilder MapClinicProHealthChecks(this IEndpointRouteBuilder app)
    {
        // ═══ Liveness — بدون هیچ سنجشی؛ فقط «فرآیند بالا است» ═══
        app.MapGet("/health/live", () => Results.Ok(new
        {
            status = "Healthy",
            serverTime = DateTime.UtcNow,
            version = ProductVersion(),
        }))
        .AllowAnonymous()
        .ExcludeFromDescription();

        // ═══ Readiness — اتصال DB · فضای دیسک پشتیبان · ذخیرهٔ Hangfire ═══
        app.MapGet("/health/ready", async (IServiceProvider sp, CancellationToken ct) =>
        {
            var logger = sp.GetRequiredService<ILogger<Program>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var checks = new Dictionary<string, object>();
            var allHealthy = true;
            var anyDegraded = false;

            // ۱) دیتابیس — اتصال‌پذیری زنده
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
                var canConnect = await db.Database.CanConnectAsync(ct);
                checks["database"] = new
                {
                    healthy = canConnect,
                    note = canConnect ? "اتصال به دیتابیس برقرار است" : "اتصال به دیتابیس برقرار نیست",
                };
                if (!canConnect) allHealthy = false;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "بررسی آمادگی دیتابیس ناموفق");
                checks["database"] = new { healthy = false, note = "خطا در سنجش اتصال دیتابیس" };
                allHealthy = false;
            }

            // ۲) فضای دیسک پشتیبان‌گیری — ریشهٔ مسیر Backup:Directory
            try
            {
                var backupDir = config["Backup:Directory"] ?? "";
                var root = Path.GetPathRoot(Path.GetFullPath(
                    string.IsNullOrWhiteSpace(backupDir) ? AppContext.BaseDirectory : backupDir));
                DriveInfo? drive = null;
                if (!string.IsNullOrEmpty(root))
                {
                    try { drive = new DriveInfo(root); }
                    catch (ArgumentException) { drive = null; } // ریشهٔ غیردرایوی (کانتینر/لینوکس CI)
                    catch (IOException) { drive = null; }     // درایو آماده نیست
                }
                if (drive is not null)
                {
                    var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    var okSpace = drive.AvailableFreeSpace >= MinimumFreeBytesForBackup;
                    checks["disk"] = new
                    {
                        healthy = okSpace,
                        freeGb = Math.Round(freeGb, 1),
                        note = okSpace ? "فضای دیسک کافی است" : "فضای دیسک پشتیبان‌گیری کمتر از ۱ گیگابایت است",
                    };
                    if (!okSpace)
                    {
                        allHealthy = false;
                        anyDegraded = true;
                    }
                }
                else
                {
                    // محیط بدون درایو (کانتینر/لینوکس CI) — سنجش غیرقابل‌اعمال، نه ناسالم
                    checks["disk"] = new { healthy = true, note = "سنجش فضای دیسک در این میزبان قابل اعمال نیست" };
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "بررسی فضای دیسک ناموفق");
                checks["disk"] = new { healthy = true, note = "سنجش فضای دیسک نادیده گرفته شد" };
            }

            // ۳) Hangfire — فقط وقتی Jobs فعال است معنی دارد (ذخیره = همان دیتابیس)
            try
            {
                var jobsEnabled = string.Equals(config["Jobs:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
                if (!jobsEnabled)
                {
                    checks["jobs"] = new { healthy = true, note = "کارهای زمان‌بندی‌شده غیرفعال است" };
                }
                else if (Hangfire.JobStorage.Current is null)
                {
                    checks["jobs"] = new { healthy = false, note = "ذخیرهٔ Hangfire مقداردهی نشده است" };
                    allHealthy = false;
                }
                else
                {
                    // خودِ ذخیره در همان دیتابیس است — اتصالِ سنجش بالا تضمینش می‌کند
                    checks["jobs"] = new { healthy = true, note = "ذخیرهٔ Hangfire متصل است" };
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "بررسی Hangfire ناموفق");
                checks["jobs"] = new { healthy = true, note = "سنجش Hangfire نادیده گرفته شد" };
            }

            var status = allHealthy ? "Healthy" : anyDegraded ? "Degraded" : "Unhealthy";
            var payload = new
            {
                status,
                checks,
                serverTime = DateTime.UtcNow,
                version = ProductVersion(),
            };
            return allHealthy
                ? Results.Ok(payload)
                : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }
}
