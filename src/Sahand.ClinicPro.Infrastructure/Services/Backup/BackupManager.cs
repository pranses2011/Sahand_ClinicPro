using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using System.IO.Compression;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup;

/// <summary>
/// مدیر پشتیبان‌گیری — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «فایل‌های پشتیبان با تاریخ و ساعت شمسی ذخیره شوند و فشرده شوند تا جای
/// کمتری اشغال کنند» + «فایل‌های پشتیبان‌گیری به‌صورت فشرده و زیپ ذخیره بشن
/// و در هنگام بازیابی زیپ را باز و بازیابی کنن و امکان انتخاب دستی فایل
/// پشتیبان برای بازیابی هم باشه».
/// ─────────────────────────────────────────────────────────────────────
/// همهٔ مسیرهای ساخت/بازیابی پشتیبان از این کلاس می‌گذرند تا کارگر خودکار و
/// دکمهٔ دستی برنامه دقیقاً یک رفتار داشته باشند:
///   ۱) پشتیبان واقعی BACKUP DATABASE روی همان اتصال
///   ۲) نام فایل با تاریخ/ساعت «شمسی» — SahandClinicPro_14040712_1530.zip
///   ۳) فشرده‌سازی ZIP با سطح بهینه — فایل خام .bak پس از فشرده‌سازی موفق حذف
///      می‌شود تا فقط نسخهٔ کم‌حجم روی دیسک بماند (کاهش مصرف فضا)
///   ۴) بازیابی از ZIP: استخراج ورودی نخست → RESTORE DATABASE با REPLACE
///      (تک‌کاربر + قطع اتصال‌های دیگر پیش از بازیابی)
///   ۵) فهرست فایل‌های پشتیبان پوشه برای انتخاب دستی در رابط کاربری
/// خطاها در BackupHistory ثبت می‌شوند و هرگز استثنا به بیرون نمی‌ریزند مگر در
/// بازیابی که علت شکست را کاربر باید ببیند.
/// </summary>
public class BackupManager
{
    private readonly ClinicProDbContext _db;
    private readonly ILogger<BackupManager> _logger;

    /// <summary>سازنده با کانتکست و ثبت‌گر</summary>
    public BackupManager(ClinicProDbContext db, ILogger<BackupManager> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// ساخت نام فایل پشتیبان با تاریخ/ساعت شمسی — «SahandClinicPro_14040712_1530»
    /// (چرخه بیست‌وسوم: پیش‌تر میلادی yyyyMMdd بود و کاربر تاریخ شمسی خواست)
    /// </summary>
    public static string ShamsiFileStamp(DateTime moment) =>
        PersianDateHelper.ToShamsi(moment).Replace("/", "").Replace("-", "") + "_" + moment.ToString("HHmm");

    /// <summary>
    /// اجرای پشتیبان کامل واقعی + فشرده‌سازی ZIP + ثبت تاریخچه.
    /// نام نهایی: SahandClinicPro[_Auto]_14040712_1530.zip در پوشهٔ مقصد.
    /// خروجی: مسیر ZIP و موفقیت.
    /// </summary>
    /// <param name="directory">پوشهٔ مقصد — در نبود ساخته می‌شود</param>
    /// <param name="isAutomatic">آیا اجرای خودکار است (برای نام و تاریخچه)</param>
    /// <param name="ct">توکن لغو</param>
    /// <param name="format">فرمت خروجی — «zip» فشردهٔ پیش‌فرض، «bak» فایل خام (چرخه ۲۴:
    /// «فرمت رو خودمون انتخاب بکنیم»)؛ بازیابی هر دو فرمت را خودکار تشخیص می‌دهد</param>
    public async Task<(bool Ok, string ZipPath, string? Error)> CreateFullBackupAsync(
        string directory, bool isAutomatic, CancellationToken ct = default, string format = "zip")
    {
        // ساخت پوشهٔ مقصد در نبود — بازگشتی (گزارش پانزدهم مالک)
        Directory.CreateDirectory(directory);

        var stamp = ShamsiFileStamp(DateTime.Now);
        var bakName = $"SahandClinicPro_{(isAutomatic ? "Auto_" : "")}{stamp}.bak";
        var zipName = $"SahandClinicPro_{(isAutomatic ? "Auto_" : "")}{stamp}.zip";
        var bakPath = Path.Combine(directory, bakName);
        var zipPath = Path.Combine(directory, zipName);

        // چرخه ۲۴ — فرمت خام BAK: فایل اصلی همان خروجی SQL می‌ماند و فشرده‌سازی انجام نمی‌شود
        var rawBak = string.Equals(format, "bak", StringComparison.OrdinalIgnoreCase);
        var finalPath = rawBak ? bakPath : zipPath;

        var record = new BackupHistory
        {
            BackupType = BackupType.Full,
            FilePath = finalPath, // چرخه ۲۴: بسته به فرمت انتخابی — ZIP یا BAK خام
            StartedAt = DateTime.Now,
            IsAutomatic = isAutomatic,
            StorageType = StorageType.Local,
            Status = BackupStatus.InProgress,
        };
        _db.BackupHistory.Add(record);
        await _db.SaveChangesAsync(ct);

        try
        {
            // ─── ۱) پشتیبان واقعی روی همان اتصال جاری ───
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            try
            {
                var databaseName = connection.Database;
                using var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "BACKUP DATABASE [" + databaseName.Replace("]", "]]") + "] " +
                    "TO DISK = @path WITH INIT, STATS = 25, CHECKSUM";
                var param = cmd.CreateParameter();
                param.ParameterName = "@path";
                param.Value = bakPath;
                cmd.Parameters.Add(param);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            finally
            {
                await connection.CloseAsync();
            }

            // ─── ۲) فشرده‌سازی ZIP (کاهش مصرف فضا — گزارش شانزدهم مالک) —
            // چرخه ۲۴: فقط وقتی فرمت فشرده انتخاب شده (پیش‌فرض)؛ در حالت BAK خام
            // فایلِ نوشته‌شده توسط خود SQL همان خروجی نهایی است
            if (!rawBak)
                await CompressFileAsync(bakPath, zipPath, ct);

            // ─── ۳) ثبت موفقیت — حجم نسخهٔ فشرده ملاک است ───
            record.Status = BackupStatus.Success;
            record.CompletedAt = DateTime.Now;
            // چرخه ۲۴: در حالت BAK خام، حجم همان فایل اصلی است نه نسخهٔ فشرده
            record.FileSize = new FileInfo(finalPath).Exists ? new FileInfo(finalPath).Length : null;
            _logger.LogInformation("پشتیبان {Kind} موفق — {Path} (حجم: {Size} بایت)",
                isAutomatic ? "خودکار" : "دستی", finalPath, record.FileSize);

            // فایل خام فقط وقتی فشرده‌سازی شد حذف می‌شود (در حالت BAK خودش خروجی نهایی است)
            if (!rawBak)
                TryDelete(bakPath);

            await _db.SaveChangesAsync(ct);
            return (true, zipPath, null);
        }
        catch (Exception ex)
        {
            record.Status = BackupStatus.Failed;
            record.CompletedAt = DateTime.Now;
            record.ErrorMessage = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
            _logger.LogError(ex, "پشتیبان {Kind} ناموفق — {File}",
                isAutomatic ? "خودکار" : "دستی", bakPath);
            await _db.SaveChangesAsync(ct);
            return (false, zipPath, ex.Message);
        }
    }

    /// <summary>
    /// فشرده‌سازی یک فایل در بایگانی ZIP تک‌ورودی با سطح بهینه —
    /// حذف فایل خام با تمیزی پس از موفقیت در جای خود انجام می‌شود
    /// </summary>
    private static async Task CompressFileAsync(string sourcePath, string zipPath, CancellationToken ct)
    {
        await using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);
        var entry = archive.CreateEntry(Path.GetFileName(sourcePath), CompressionLevel.Optimal);
        await using (var entryStream = entry.Open())
        await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await source.CopyToAsync(entryStream, 81_920, ct);
        }
    }

    /// <summary>حذف امن فایل — خطای حذف نادیده (فایل شاید در استفاده باشد)</summary>
    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* حذف ناکام نباید موفقیت پشتیبان را زیر سؤال ببرد */ }
    }

    /// <summary>
    /// بازیابی از فایل پشتیبان — پذیرش ZIP (استخراج ورودی نخست) یا .bak خام:
    /// ۱) تک‌کاربر + قطع فوری اتصال‌های دیگر (ROLLBACK IMMEDIATE)
    /// ۲) RESTORE DATABASE … WITH REPLACE, RECOVERY
    /// نکتهٔ مهم: بازیابی داده‌های فعلی را جایگزین می‌کند — فراخواننده باید
    /// تأییدیهٔ کاربر را گرفته باشد.
    /// </summary>
    /// <param name="zipOrBakPath">مسیر فایل ZIP یا BAK انتخاب‌شده</param>
    /// <param name="ct">توکن لغو</param>
    public async Task<(bool Ok, string Message)> RestoreFromBackupAsync(string zipOrBakPath, CancellationToken ct = default)
    {
        if (!File.Exists(zipOrBakPath))
            return (false, "فایل پشتیبان یافت نشد — مسیر را بررسی کنید");

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — رفع «بازیابی ناموفق:
        // Cannot open backup device» ═══ ریشه: ZIP در پوشهٔ Temp «کاربر»
        // استخراج می‌شد؛ سرویس SQL Server با حساب دیگری اجرا می‌شود و به آن
        // مسیر دسترسی خواندن ندارد → RESTORE با خطای «Cannot open backup
        // device … Operating system error 5 (Access is denied)» شکست
        // می‌خورد. راه‌حل قطعی: استخراج در پوشهٔ «پشتیبان پیش‌فرض خودِ
        // SQL Server» (از رجیستریِ نمونه — همیشه برای حساب سرویس SQL
        // خواناست)؛ در نبود آن، همان پوشهٔ فایل ZIP (پوشهٔ پشتیبان‌های
        // برنامه که SQL خودش در آن نوشته است) و در آخر Temp.
        string bakPath;
        var isZip = zipOrBakPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        var tempDir = string.Empty;
        try
        {
            if (isZip)
            {
                // مسیر استخراج امن — به‌ترتیبِ اولویت (بالا → پایین)
                var extractDir = await TryGetSqlServerBackupDirectoryAsync(ct)
                    ?? (Directory.Exists(zipOrBakPath) ? zipOrBakPath : null)
                    ?? Path.GetDirectoryName(zipOrBakPath);
                if (string.IsNullOrWhiteSpace(extractDir) || !Directory.Exists(extractDir))
                {
                    tempDir = Path.Combine(Path.GetTempPath(),
                        "SahandClinicProRestore_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);
                    extractDir = tempDir;
                }

                using var archive = ZipFile.OpenRead(zipOrBakPath);
                // نخستین ورودیِ .bak — در بایگانی‌های این برنامه تک‌ورودی است
                var entry = archive.Entries.FirstOrDefault(e =>
                               e.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                           ?? archive.Entries.FirstOrDefault();
                if (entry is null)
                    return (false, "بایگانی ZIP فایل پشتیبان ندارد");

                // نام یکتا برای هر بازیابی — تداخل با پشتیبان‌های موجود نگیرد
                bakPath = Path.Combine(extractDir,
                    "SahandClinicPro_Restore_" + Guid.NewGuid().ToString("N")[..8] + ".bak");
                entry.ExtractToFile(bakPath, overwrite: true);
            }
            else
            {
                bakPath = zipOrBakPath;
            }

            // ─── ۲) بازیابی روی همان دیتابیس اتصال جاری ───
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            try
            {
                var databaseName = connection.Database;

                // تک‌کاربر + قطع اتصال‌های دیگر — الزام بازیابی با REPLACE
                using var singleUser = connection.CreateCommand();
                singleUser.CommandText =
                    "USE master; ALTER DATABASE [" + databaseName.Replace("]", "]]") +
                    "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                await singleUser.ExecuteNonQueryAsync(ct);

                try
                {
                    using var restore = connection.CreateCommand();
                    restore.CommandText =
                        "USE master; RESTORE DATABASE [" + databaseName.Replace("]", "]]") + "] " +
                        "FROM DISK = @path WITH REPLACE, RECOVERY, STATS = 25";
                    var param = restore.CreateParameter();
                    param.ParameterName = "@path";
                    param.Value = bakPath;
                    restore.Parameters.Add(param);
                    await restore.ExecuteNonQueryAsync(ct);
                }
                finally
                {
                    // بازگرداندن دسترسی چندکاربر — حتی پس از شکست بازیابی
                    try
                    {
                        using var multiUser = connection.CreateCommand();
                        multiUser.CommandText =
                            "USE master; ALTER DATABASE [" + databaseName.Replace("]", "]]") +
                            "] SET MULTI_USER";
                        await multiUser.ExecuteNonQueryAsync(ct);
                    }
                    catch
                    {
                        // اگر خود بازیابی شکست خورده باشد و دیتابیس در دسترس نباشد،
                        // این تنظیم ممکن است شکست بخورد — علت اصلی در پیام است
                    }
                }
            }
            finally
            {
                await connection.CloseAsync();
            }

            _logger.LogInformation("بازیابی از {File} با موفقیت انجام شد", zipOrBakPath);
            return (true, "بازیابی با موفقیت انجام شد — برنامه را ری‌استارت کنید تا داده‌های تازه بار شود");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "بازیابی از {File} ناموفق", zipOrBakPath);
            return (false, "بازیابی ناموفق: " + ex.Message);
        }
        finally
        {
            // پاک‌سازی پوشهٔ موقت استخراج
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
            catch { /* پاک‌سازی موقت اهمیت حیاتی ندارد */ }
        }
    }

    /// <summary>
    /// خواندن پوشهٔ «پشتیبان پیش‌فرض» SQL Server از رجیستری نمونه — چرخه ۲۴:
    /// مسیری که حساب سرویس SQL به‌طور قطعی مجاز به خواندن/نوشتن آن است؛
    /// برای استخراج امن فایل BAK درون ZIP پیش از RESTORE به‌کار می‌رود.
    /// خروجی تهی یعنی خواندن ناموفق بود (نبود دسترسی/کلید رجیستری).
    /// </summary>
    private async Task<string?> TryGetSqlServerBackupDirectoryAsync(CancellationToken ct)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(ct);
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DECLARE @dir nvarchar(4000); " +
                    "EXEC xp_instance_regread N'HKEY_LOCAL_MACHINE', " +
                    "N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', " +
                    "N'BackupDirectory', @dir OUTPUT; " +
                    "SELECT @dir;";
                var result = await cmd.ExecuteScalarAsync(ct);
                var dir = result as string;
                return string.IsNullOrWhiteSpace(dir) ? null : dir;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        catch
        {
            return null; // مسیر جایگزین (پوشهٔ ZIP) استفاده می‌شود
        }
    }

    /// <summary>
    /// فهرست فایل‌های پشتیبان پوشه برای انتخاب دستی — گزارش شانزدهم مالک:
    /// «امکان انتخاب دستی فایل پشتیبان برای بازیابی». هر فایل ZIP/BAK با
    /// نام، حجم و تاریخ آخرین تغییر برمی‌گردد (تازه‌ترین اول).
    /// </summary>
    /// <param name="directory">پوشهٔ پشتیبان‌ها</param>
    public static List<BackupFileInfo> ListBackupFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return new List<BackupFileInfo>();

        return Directory.EnumerateFiles(directory)
            .Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new BackupFileInfo(
                    info.Name,
                    f,
                    info.Length,
                    info.LastWriteTime,
                    f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "zip" : "bak");
            })
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();
    }
}

/// <summary>اطلاعات یک فایل پشتیبان برای فهرست انتخاب دستی — چرخه بیست‌وسوم</summary>
public record BackupFileInfo(
    string FileName,
    string FullPath,
    long SizeBytes,
    DateTime LastWriteTime,
    string Kind);
