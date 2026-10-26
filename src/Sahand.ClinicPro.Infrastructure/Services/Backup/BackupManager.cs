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

        var stamp = ShamsiFileStamp(PersianDateHelper.IranNow());
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
            StartedAt = PersianDateHelper.IranNow(),
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
            record.CompletedAt = PersianDateHelper.IranNow();
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
            record.CompletedAt = PersianDateHelper.IranNow();
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
    /// بازیابی از فایل پشتیبان — ZIP استخراج و RESTORE واقعی اجرا می‌شود.
    /// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع «بازیابی ناموفق: access to
    /// the path is denied» ═══ پیش‌تر ZIP در پوشهٔ «پشتیبان پیش‌فرض خودِ SQL
    /// Server» استخراج می‌شد؛ آن پوشه برای حساب سرویس SQL قفل است و فرایند
    /// API معمولاً «مجوز نوشتن» در آن ندارد → استخراج با
    /// UnauthorizedAccessException می‌شکست. ترتیب صحیح پوشهٔ استخراج:
    /// ۱) پوشهٔ پشتیبان‌های برنامه (هم API در آن ZIP می‌نویسد و هم سرویس SQL
    ///    هنگام BACKUP در آن .bak نوشته — یعنی هر دو حساب دسترسی کامل دارند)
    /// ۲) پوشهٔ خود فایل ZIP (مثلاً فلش)
    /// ۳) پوشهٔ پیش‌فرض SQL (اگر نوشتن در آن ممکن بود)
    /// ۴) پوشهٔ موقت — آخرین راه
    /// در هر گام «آزمون نوشتن» انجام می‌شود تا اولین پوشهٔ واقعاً قابل‌نوشتن
    /// برگزیده شود؛ BAK استخراج‌شده پس از پایان بازیابی (موفق یا ناموفق)
    /// پاک می‌شود تا پوشهٔ پشتیبان شلوغ نشود.
    /// نکتهٔ مهم: بازیابی داده‌های فعلی را جایگزین می‌کند — فراخواننده باید
    /// تأییدیهٔ کاربر را گرفته باشد.
    /// </summary>
    /// <param name="zipOrBakPath">مسیر کامل فایل ZIP/BAK</param>
    /// <param name="preferredExtractDirectory">پوشهٔ پیشنهادی استخراج — پوشهٔ پشتیبان‌های برنامه</param>
    /// <param name="ct">توکن لغو</param>
    public async Task<(bool Ok, string Message)> RestoreFromBackupAsync(string zipOrBakPath,
        string? preferredExtractDirectory = null, CancellationToken ct = default)
    {
        if (!File.Exists(zipOrBakPath))
            return (false, "فایل پشتیبان یافت نشد — مسیر را بررسی کنید");

        string bakPath;
        var isZip = zipOrBakPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        var tempDir = string.Empty;
        // آیا BAK نهایی «مااخت خودمان» است (باید در پایان پاک شود)؟
        var extractedBakPath = string.Empty;
        try
        {
            if (isZip)
            {
                // ═══ چرخهٔ ۲۶ — انتخاب پوشهٔ استخراج با آزمون نوشتن ═══
                var sqlBackupDir = await TryGetSqlServerBackupDirectoryAsync(ct);
                var zipDir = Path.GetDirectoryName(zipOrBakPath);

                string? extractDir = null;
                foreach (var candidate in new[]
                         {
                             preferredExtractDirectory, // پوشهٔ پشتیبان‌های برنامه — بهترین گزینه
                             zipDir, // پوشهٔ خود فایل (مثلاً فلش)
                             sqlBackupDir // پوشهٔ SQL — اگر نوشتن ممکن بود
                         })
                {
                    if (string.IsNullOrWhiteSpace(candidate) || !Directory.Exists(candidate))
                        continue;
                    if (!CanWriteToDirectory(candidate))
                        continue; // حساب فرایند API مجوز نوشتن ندارد (ریشهٔ خطای دسترسی)
                    extractDir = candidate;
                    break;
                }

                // هیچ‌کدام قابل‌نوشتن نبود — پوشهٔ موقت (آخرین راه)
                if (extractDir is null)
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
                extractedBakPath = bakPath; // مااخت است — پس از بازیابی پاک می‌شود
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

                // ═══ بازیابی نسخه‌های قدیمی (گزارش مالک: «بازیابی از فایل
                // پشتیبان نسخه‌های قبلی خطا می‌دهد») ═══ فایل BAK نسخه‌های
                // قدیمی/نصب دیگر، مسیر فیزیکی فایل‌های دیتابیس همان نصب
                // قبلی را در خودش دارد؛ RESTORE بدون MOVE به همان مسیرهای
                // قدیمی می‌رود و با خطای دسترسی/نبود مسیر شکست می‌خورد.
                // راه‌حل استاندارد: فهرست منطقی فایل‌های داخل BAK خوانده و
                // هر فایل با MOVE به مسیر فیزیکی فعلی همین دیتابیس نگاشت
                // می‌شود — مسیر مقصد همیشه مسیر زندهٔ نصب فعلی است.
                var currentFiles = new List<(string Name, string Physical, string Type)>();
                using (var filesCmd = connection.CreateCommand())
                {
                    filesCmd.CommandTimeout = 0; // بدون مهلت — دیتابیس‌های بزرگ
                    filesCmd.CommandText =
                        "SELECT name, physical_name, CAST(type AS int) FROM sys.database_files ORDER BY file_id";
                    using var filesReader = await filesCmd.ExecuteReaderAsync(ct);
                    while (await filesReader.ReadAsync(ct))
                        currentFiles.Add((filesReader.GetString(0), filesReader.GetString(1),
                            filesReader.GetInt32(2) == 1 ? "L" : "D"));
                }

                var backupFiles = new List<(string Logical, string Type)>();
                using (var listCmd = connection.CreateCommand())
                {
                    listCmd.CommandTimeout = 0; // بدون مهلت — خواندن BAK بزرگ
                    listCmd.CommandText = "RESTORE FILELISTONLY FROM DISK = @path";
                    var listParam = listCmd.CreateParameter();
                    listParam.ParameterName = "@path";
                    listParam.Value = bakPath;
                    listCmd.Parameters.Add(listParam);
                    using var listReader = await listCmd.ExecuteReaderAsync(ct);
                    var logicalOrdinal = listReader.GetOrdinal("LogicalName");
                    var typeOrdinal = listReader.GetOrdinal("Type");
                    while (await listReader.ReadAsync(ct))
                        backupFiles.Add((listReader.GetString(logicalOrdinal),
                            listReader.GetString(typeOrdinal)));
                }

                var dataTargets = currentFiles.Where(f => f.Type == "D").Select(f => f.Physical).ToList();
                var logTargets = currentFiles.Where(f => f.Type == "L").Select(f => f.Physical).ToList();
                var moveClauses = new List<string>();
                var dataIndex = 0;
                var logIndex = 0;
                foreach (var (logical, type) in backupFiles)
                {
                    string? target;
                    if (type == "L")
                        target = logIndex < logTargets.Count ? logTargets[logIndex++] : logTargets.FirstOrDefault();
                    else
                        target = dataIndex < dataTargets.Count ? dataTargets[dataIndex++] : dataTargets.FirstOrDefault();
                    if (string.IsNullOrEmpty(target)) continue;
                    moveClauses.Add(
                        "MOVE '" + logical.Replace("'", "''") + "' TO '" + target.Replace("'", "''") + "'");
                }
                var moveSql = moveClauses.Count == 0 ? "" : ", " + string.Join(", ", moveClauses);

                // ═══ بازیابی از اتصال اختصاصی master (مقاوم‌سازی بازیابی) ═══
                // پیش‌تر SINGLE_USER/RESTORE روی همان اتصال EF به دیتابیس مقصد
                // اجرا می‌شد؛ ROLLBACK IMMEDIATE اتصال‌های استخر EF را می‌کشد و
                // روی دیتابیس‌های بزرگ هم مهلت پیش‌فرض ۳۰ ثانیهٔ فرمان، بازیابی
                // را نیمه‌کاره قطع می‌کرد («Execution Timeout Expired»).
                // اکنون: اتصال جدا به master + مهلت نامحدود برای هر سه فرمان.
                var masterBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
                    connection.ConnectionString) { InitialCatalog = "master" };
                await using var masterConnection = new Microsoft.Data.SqlClient.SqlConnection(
                    masterBuilder.ConnectionString);
                await masterConnection.OpenAsync(ct);

                await using (var singleUser = new Microsoft.Data.SqlClient.SqlCommand(
                    "ALTER DATABASE [" + databaseName.Replace("]", "]]") +
                    "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", masterConnection))
                {
                    singleUser.CommandTimeout = 0;
                    await singleUser.ExecuteNonQueryAsync(ct);
                }

                try
                {
                    await using var restore = new Microsoft.Data.SqlClient.SqlCommand(
                        "RESTORE DATABASE [" + databaseName.Replace("]", "]]") + "] " +
                        "FROM DISK = @path WITH REPLACE, RECOVERY" + moveSql + ", STATS = 25",
                        masterConnection);
                    restore.CommandTimeout = 0; // بازیابی دیتابیس بزرگ نباید با مهلت ۳۰ ثانیه بمیرد
                    restore.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@path", bakPath));
                    await restore.ExecuteNonQueryAsync(ct);
                }
                finally
                {
                    // بازگرداندن دسترسی چندکاربر — حتی پس از شکست بازیابی
                    try
                    {
                        await using var multiUser = new Microsoft.Data.SqlClient.SqlCommand(
                            "ALTER DATABASE [" + databaseName.Replace("]", "]]") +
                            "] SET MULTI_USER", masterConnection);
                        multiUser.CommandTimeout = 0;
                        await multiUser.ExecuteNonQueryAsync(ct);
                    }
                    catch
                    {
                        // اگر خود بازیابی شکست خورده باشد و دیتابیس در دسترس نباشد،
                        // این تنظیم ممکن است شکست بخورد — علت اصلی در پیام است
                    }
                }

                // استخر اتصال‌های EF به دیتابیس پیش از بازیابی وصل بوده‌اند؛ پاک
                // شدنشان وادار می‌کند درخواست‌های بعدی روی دیتابیس تازه‌وصل شوند
                Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
            }
            finally
            {
                await connection.CloseAsync();
            }

            _logger.LogInformation("بازیابی از {File} با موفقیت انجام شد", zipOrBakPath);
            return (true, "بازیابی با موفقیت انجام شد — برنامه را ری‌استارت کنید تا داده‌های تازه بار شود");
        }
        catch (UnauthorizedAccessException ex)
        {
            // خطای دسترسی — پیام کاربردی فارسی با راهنمای رفع (چرخه ۲۶)
            _logger.LogError(ex, "بازیابی از {File} — خطای دسترسی", zipOrBakPath);
            return (false,
                "دسترسی به مسیر استخراج داده نشد (Access Denied). سرور API به پوشهٔ مقصد اجازهٔ نوشتن ندارد؛ " +
                "پوشهٔ پشتیبان برنامه را در تنظیمات به مسیری قابل‌نوشتن تغییر دهید (مثلاً C:\\Backups\\SahandClinicPro) " +
                "یا دسترسی کامل کاربر سرویس را بدهید.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "بازیابی از {File} ناموفق", zipOrBakPath);
            return (false, "بازیابی ناموفق: " + ex.Message);
        }
        finally
        {
            // پاک‌سازی: پوشهٔ موقت استخراج + فایل BAK مااخت (پوشهٔ پشتیبان شلوغ نشود)
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
            catch { /* پاک‌سازی موقت اهمیت حیاتی ندارد */ }
            try
            {
                if (!string.IsNullOrEmpty(extractedBakPath) && File.Exists(extractedBakPath))
                    File.Delete(extractedBakPath);
            }
            catch { /* اگر سرویس SQL هنوز فایل را نگه داشته، در اجرای بعدی پاک می‌شود */ }
        }
    }

    /// <summary>
    /// آزمون نوشتن در پوشه — ساخت و حذف یک فایل کاوشگر کوچک؛
    /// چرخهٔ بیست‌وششم: برای انتخاب پوشهٔ استخراج ZIP که هم برای فرایند API
    /// قابل‌نوشتن باشد (ریشهٔ «access to the path is denied")
    /// </summary>
    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, ".write_probe_" + Guid.NewGuid().ToString("N")[..8]);
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false; // حساب فرایند جاری اجازهٔ نوشتن ندارد
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
