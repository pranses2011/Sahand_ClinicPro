using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

using Sahand.ClinicPro.Infrastructure.Services.Backup;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پشتیبان‌گیری — تاریخچه و اجرای دستی
/// مسیرها: /api/v1/backup
/// چرخه بیستم (گزارش چهاردهم مالک): تنظیم و فعال‌سازی پشتیبان ابری از
/// داخل برنامه — پیکربندی در SystemSettings (دستهٔ BackupCloud) ذخیره و
/// روی پیکربندی فایل رویه (overlay) می‌شود تا نیازی به ویرایش appsettings نباشد
/// </summary>
[ApiController]
[Route("api/v1/backup")]
[Authorize]
public class BackupController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupController> _logger;

    /// <summary>ارائه‌دهندهٔ خدمات درخواست — برای ساخت BackupManager با کانتکست همان درخواست (چرخه بیست‌وسوم)</summary>
    private readonly IServiceProvider _services;

    /// <summary>ثبت‌گر اختصاصی مدیر پشتیبان — چرخه بیست‌وسوم</summary>
    private readonly ILogger<BackupManager> _backupLogger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public BackupController(IUnitOfWork unitOfWork, IConfiguration configuration,
        ILogger<BackupController> logger, IServiceProvider services,
        ILogger<BackupManager> backupLogger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _services = services;
        _backupLogger = backupLogger;
    }

    /// <summary>
    /// ساخت مدیر پشتیبان با کانتکست دامنهٔ همین درخواست — ثبت تاریخچه و اجرای
    /// BACKUP/RESTORE روی همان اتصال (چرخه بیست‌وسوم)
    /// </summary>
    private BackupManager CreateManager() =>
        new(_services.GetRequiredService<Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext>(),
            _backupLogger);

    /// <summary>
    /// پوشهٔ پشتیبان‌های برنامه — ترکیب تنظیم فایل (Backup:Directory) و
    /// رویهٔ دیتابیسی (SystemSetting دستهٔ Backup، کلید Directory)؛ چرخهٔ
    /// بیست‌وششم: به‌عنوان «پوشهٔ ترجیحی استخراج» به بازیابی داده می‌شود تا
    /// استخراج ZIP همیشه در مسیری انجام شود که هم API مجاز به نوشتنش است
    /// و هم سرویس SQL می‌تواند بخواند (رفع خطای دسترسی).
    /// </summary>
    private async Task<string> GetBackupDirectoryAsync(CancellationToken ct)
    {
        var backupDir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
        var dirSetting = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Category == "Backup" && s.Key == "Directory", ct);
        if (dirSetting is not null && !string.IsNullOrWhiteSpace(dirSetting.Value))
            backupDir = dirSetting.Value;
        return backupDir;
    }

    /// <summary>
    /// تاریخچه پشتیبان‌ها — چرخه بیست‌ویکم: مسیر فایل هم برگردانده می‌شود
    /// تا با کلیک روی هر ردیف، جزئیات کامل آن (مسیر/حجم/خطا) دیده شود.
    /// </summary>
    [HttpGet("history")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _unitOfWork.Repository<BackupHistory>()
            .QueryableNoTracking
            .OrderByDescending(b => b.StartedAt)
            .Take(50)
            .Select(b => new
            {
                b.Id,
                type = b.BackupType.ToString(),
                status = b.Status.ToString(),
                startedAt = b.StartedAt,
                completedAt = b.CompletedAt,
                fileSize = b.FileSize,
                isAutomatic = b.IsAutomatic,
                storage = b.StorageType.ToString(),
                filePath = b.FilePath,
                b.CloudFileName,
                b.ErrorMessage
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(history));
    }

    /// <summary>
    /// اجرای پشتیبان‌گیری کامل دستی — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// پیش‌تر این endpoint فقط رکورد «در حال اجرا» می‌ساخت و BACKUP DATABASE
    /// واقعی اجرا نمی‌شد («بکاپ دستی انجام نمی‌شود»). اکنون از طریق BackupManager:
    ///   • پشتیبان واقعی SQL Server روی همان اتصال
    ///   • نام فایل با تاریخ/ساعت شمسی — SahandClinicPro_14040712_1530.zip
    ///   • فشرده‌سازی ZIP (حجم کمتر) — فایل خام پس از فشرده‌سازی حذف می‌شود
    ///   • نتیجهٔ هم‌زمان (موفق/ناموفق + مسیر + حجم) در پاسخ و تاریخچه
    /// </summary>
    [HttpPost("run")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> RunBackup(CancellationToken ct,
        [FromQuery] string? format)
    {
        var backupDir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";

        // تنظیم پوشه از دیتابیس (تنظیمات برنامه) بر تنظیم فایل اولویت دارد
        var dirSetting = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Category == "Backup" && s.Key == "Directory", ct);
        if (dirSetting is not null && !string.IsNullOrWhiteSpace(dirSetting.Value))
            backupDir = dirSetting.Value;

        var manager = CreateManager();

        // چرخه ۲۴ — فرمت خروجی به انتخاب کاربر: zip فشردهٔ پیش‌فرض یا bak خام
        var chosenFormat = string.Equals(format, "bak", StringComparison.OrdinalIgnoreCase) ? "bak" : "zip";
        var (ok, zipPath, error) = await manager.CreateFullBackupAsync(
            backupDir, isAutomatic: false, ct, chosenFormat);

        if (!ok)
            return BadRequest(ApiResponse<object>.Failure($"پشتیبان‌گیری ناموفق بود: {error}"));

        // ═══ بکاپ فوری هم ایمیل می‌شود (گزارش مالک) ═══
        // از همان مسیر مشترک بکاپ خودکار: ایمیل پشتیبانی «همیشه و در همه
        // حال» (بدون تنظیم، با مشخصات درمانگاه) و ایمیل کاربر فقط اگر در
        // تنظیمات بکاپ خودکار فعالش کرده باشد. شکست ایمیل، بکاپ موفق را
        // ناموفق نمی‌کند — فقط در پیام پاسخ گزارش می‌شود.
        var notice = await _services.GetRequiredService<BackupEmailNotifier>()
            .NotifyAsync(zipPath, isAutomatic: false, ct);

        return Ok(ApiResponse<object>.Success(new
        {
            fileName = System.IO.Path.GetFileName(zipPath),
            filePath = zipPath,
            emailSupportSent = notice.SupportSent,
            emailUserSent = notice.UserSent,
            emailNote = notice.Note,
            message = $"پشتیبان‌گیری با موفقیت انجام شد — {notice.Note}"
        }, $"پشتیبان‌گیری با موفقیت انجام شد — {notice.Note}"));
    }

    // ═══ فهرست و بازیابی فایل‌های پشتیبان — چرخه بیست‌وسوم (گزارش شانزدهم مالک) ═══
    // «امکان انتخاب دستی فایل پشتیبان برای بازیابی» + «در هنگام بازیابی زیپ را
    // باز و بازیابی کنن» — دو endpoint زیر فهرست فایل‌های پوشهٔ پشتیبان و اجرای
    // بازیابی واقعی (RESTORE DATABASE با REPLACE) را ممکن می‌کنند.

    /// <summary>فهرست فایل‌های پشتیبان پوشه — برای انتخاب دستی بازیابی (تازه‌ترین اول)</summary>
    [HttpGet("files")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> ListFiles(CancellationToken ct)
    {
        var backupDir = await ResolveBackupDirectoryAsync(ct);
        var files = BackupManager.ListBackupFiles(backupDir);

        return Ok(ApiResponse<object>.Success(files.Select(f => new
        {
            fileName = f.FileName,
            fullPath = f.FullPath,
            sizeBytes = f.SizeBytes,
            sizeText = $"{f.SizeBytes / 1024.0 / 1024.0:0.#} مگابایت",
            lastWrite = f.LastWriteTime,
            kind = f.Kind,
        })));
    }

    /// <summary>
    /// بازیابی از فایل پشتیبان انتخاب‌شده — ZIP استخراج و RESTORE واقعی اجرا می‌شود.
    /// ⚠️ داده‌های فعلی دیتابیس به‌طور کامل با نسخهٔ پشتیبان جایگزین می‌شود —
    /// کلاینت باید پیش از فراخوانی، تأییدیهٔ صریح کاربر را گرفته باشد.
    /// </summary>
    /// <param name="request">مسیر کامل فایل ZIP/BAK (از فهرست files) — یا تهی برای آخرین نسخه</param>
    /// <param name="ct">توکن لغو هم‌زمان</param>
    [HttpPost("restore")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> Restore([FromBody] RestoreRequest request, CancellationToken ct)
    {
        string path;
        if (!string.IsNullOrWhiteSpace(request?.FullPath))
        {
            path = request.FullPath.Trim();
        }
        else
        {
            // پیش‌فرض: تازه‌ترین پشتیبان موفق ثبت‌شده
            var latest = await _unitOfWork.Repository<BackupHistory>().QueryableNoTracking
                .Where(b => !b.IsDeleted && b.Status == BackupStatus.Success && b.FilePath != null)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync(ct);
            if (latest?.FilePath is null)
                return BadRequest(ApiResponse<string>.Failure("هیچ فایل پشتیبانی برای بازیابی یافت نشد"));
            path = latest.FilePath;
        }

        var manager = CreateManager();

        // ═══ چرخهٔ بیست‌وششم — پوشهٔ ترجیحی استخراج = پوشهٔ پشتیبان‌های برنامه ═══
        // ریشهٔ «access to the path is denied»: پیش‌تر ZIP در پوشهٔ بکاپ خودِ
        // SQL استخراج می‌شد که فرایند API مجوز نوشتن در آن را ندارد؛ اکنون
        // پوشهٔ برنامه اولویت اول است (با آزمون نوشتن در BackupManager)
        var preferredDir = await GetBackupDirectoryAsync(ct);

        var (ok, message) = await manager.RestoreFromBackupAsync(path, preferredDir, ct);
        if (!ok)
            return BadRequest(ApiResponse<string>.Failure(message));

        return Ok(ApiResponse<object>.SuccessMessage(message));
    }

    /// <summary>
    /// بارگذاری فایل پشتیبان خارجی و بازیابی — چرخهٔ بیست‌وششم (گزارش هجدهم
    /// مالک): «امکان انتخاب فایل پشتیبانی که در لیست نیست وجود ندارد و
    /// می‌خوام از هارد یا فلش انتخاب کنم». فایل ZIP/BAK از هر درایوی
    /// (چون روی کلاینت است و سرور شاید روی ماشین دیگری باشد) با فرم
    /// چندبخشی بارگذاری و در پوشهٔ پشتیبان‌های برنامه ذخیره می‌شود، سپس
    /// همان‌جا بازیابی اجرا می‌شود — مسیری که هم برای API قابل‌نوشتن است و
    /// هم برای سرویس SQL خواناست. پس از بازیابی موفق نسخهٔ بارگذاری‌شده
    /// پاک می‌شود (اصل فایل روی فلش کاربر سالم می‌ماند).
    /// </summary>
    [HttpPost("upload-restore")]
    [HasPermission("backup.create")]
    [RequestSizeLimit(2_147_483_647)] // تا ۲ گیگابایت — فایل بکاپ سنگین است
    public async Task<IActionResult> UploadAndRestore(CancellationToken ct)
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Failure("فایل پشتیبان ارسال نشده است"));

        // امنیت: فقط پسوندهای مجاز پشتیبان
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".zip" && extension != ".bak")
            return BadRequest(ApiResponse<object>.Failure("فقط فایل‌های پشتیبان ZIP یا BAK قابل بازیابی‌اند"));

        // ذخیرهٔ امن در پوشهٔ پشتیبان‌های برنامه — مسیر ترجیحی استخراج همان است
        var backupDir = await GetBackupDirectoryAsync(ct);
        Directory.CreateDirectory(backupDir);
        var savedPath = Path.Combine(backupDir,
            "SahandClinicPro_ExternalRestore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + extension);

        try
        {
            await using (var stream = System.IO.File.Create(savedPath))
                await file.CopyToAsync(stream, ct);

            var manager = CreateManager();
            var (ok, message) = await manager.RestoreFromBackupAsync(savedPath, backupDir, ct);
            if (!ok)
                return BadRequest(ApiResponse<string>.Failure(message));

            return Ok(ApiResponse<object>.SuccessMessage(message));
        }
        finally
        {
            // پاک‌سازی نسخهٔ بارگذاری‌شده پس از تلاش بازیابی — اصل فایل نزد کاربر است
            try
            {
                if (System.IO.File.Exists(savedPath))
                    System.IO.File.Delete(savedPath);
            }
            catch { /* اگر سرویس SQL هنوز فایل را نگه دارد، دفعهٔ بعد پاک می‌شود */ }
        }
    }

    /// <summary>بدنهٔ درخواست بازیابی — تهی یعنی آخرین نسخهٔ موفق</summary>
    public sealed class RestoreRequest
    {
        /// <summary>مسیر کامل فایل ZIP یا BAK — از فهرست backup/files</summary>
        public string? FullPath { get; set; }
    }

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — حذف فایل‌های پشتیبان قدیمی ═══
    // «امکان حذف فایل‌های پشتیبان قدیمی به‌صورت دستی و اتوماتیک هم باشد» —
    // حذف خودکار از طریق سهمیهٔ نگهداشت (Backup:RetentionCount) در کار شبانه
    /// انجام می‌شود؛ حذف دستی با endpoint زیر از پنجرهٔ بازیابی ممکن است.

    /// <summary>حذف یک فایل پشتیبان (ZIP/BAK) از دیسک — همراه با تاریخچهٔ مربوط</summary>
    /// <param name="request">مسیر کامل فایل</param>
    [HttpPost("delete-file")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> DeleteBackupFile([FromBody] DeleteFileRequest request)
    {
        var path = request?.FullPath?.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(ApiResponse<object>.Failure("مسیر فایل لازم است"));

        // امنیت: فقط فایل zip/bak در «پوشهٔ پشتیبان‌های برنامه» قابل حذف است —
        // مسیرهای بیرونی (مثل فایل‌های سیستم) هرگز پاک نمی‌شوند
        var backupDir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
        var dirSetting = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Category == "Backup" && s.Key == "Directory");
        if (dirSetting is not null && !string.IsNullOrWhiteSpace(dirSetting.Value))
            backupDir = dirSetting.Value;

        var fullBakDir = Path.GetFullPath(backupDir);
        var fullTarget = Path.GetFullPath(path);
        if (!fullTarget.StartsWith(fullBakDir, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Failure("حذف فقط در پوشهٔ پشتیبان‌های برنامه مجاز است"));
        if (!Path.GetExtension(fullTarget).Equals(".zip", StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(fullTarget).Equals(".bak", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Failure("فقط فایل‌های پشتیبان ZIP/BAK قابل حذف‌اند"));

        if (!System.IO.File.Exists(fullTarget))
            return BadRequest(ApiResponse<object>.Failure("فایل یافت نشد"));

        try
        {
            System.IO.File.Delete(fullTarget);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Failure("حذف فایل ناموفق بود: " + ex.Message));
        }

        // تاریخچهٔ مرتبط (در صورت وجود) هم غیرفعال/حذف نرم شود تا در فهرست نیاید
        var history = _unitOfWork.Repository<BackupHistory>().QueryableTracking
            .Where(b => b.FilePath == fullTarget);
        foreach (var h in history)
        {
            h.IsDeleted = true;
            h.Status = BackupStatus.Deleted;
        }
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.SuccessMessage("فایل پشتیبان حذف شد"));
    }

    /// <summary>بدنهٔ درخواست حذف فایل پشتیبان</summary>
    public sealed class DeleteFileRequest
    {
        /// <summary>مسیر کامل فایل ZIP/BAK</summary>
        public string? FullPath { get; set; }
    }

    /// <summary>حل پوشهٔ پشتیبان — تنظیم دیتابیس مقدم بر فایل پیکربندی</summary>
    private async Task<string> ResolveBackupDirectoryAsync(CancellationToken ct)
    {
        var dirSetting = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Category == "Backup" && s.Key == "Directory", ct);
        return !string.IsNullOrWhiteSpace(dirSetting?.Value)
            ? dirSetting.Value
            : _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
    }

    // ═══ تنظیمات پشتیبان‌گیری خودکار — چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
    // «بکاپ خودکار انجام نمی‌شود» — کارگر پس‌زمینه (AutomaticBackupWorker) این
    // تنظیمات را از SystemSettings دستهٔ Backup می‌خواند؛ دو endpoint زیر تنظیم
    // از داخل برنامه را ممکن می‌کنند: فعال/غیرفعال، ساعت روزانه و پوشهٔ ذخیره.

    /// <summary>خواندن تنظیمات پشتیبان‌گیری خودکار</summary>
    [HttpGet("auto-config")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> GetAutoConfig(CancellationToken ct)
    {
        var rows = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == "Backup")
            .ToListAsync(ct);
        string? Value(string key) => rows.FirstOrDefault(r => r.Key == key)?.Value;

        return Ok(ApiResponse<object>.Success(new
        {
            enabled = string.Equals(Value("AutoEnabled"), "true", StringComparison.OrdinalIgnoreCase),
            hour = int.TryParse(Value("AutoHour"), out var h) ? h : 2,
            directory = Value("Directory") ?? _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro",
            // چرخهٔ ۸۲ — «ارسال به ایمیل»: مقصد ارسال نسخهٔ شبانه
            emailEnabled = string.Equals(Value("EmailEnabled"), "true", StringComparison.OrdinalIgnoreCase),
            emailTo = Value("EmailTo") ?? string.Empty
        }));
    }

    /// <summary>ذخیرهٔ تنظیمات پشتیبان‌گیری خودکار</summary>
    [HttpPut("auto-config")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> SaveAutoConfig([FromBody] SaveAutoConfigRequest request,
        CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<SystemSetting>();
        var existing = await repo.QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == "Backup")
            .ToListAsync(ct);

        // upsert سه کلید — مقادیر null نادیده گرفته می‌شوند
        async Task UpsertAsync(string key, string value)
        {
            var row = existing.FirstOrDefault(s => s.Key == key);
            if (row is null)
            {
                await repo.AddAsync(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Category = "Backup",
                    DataType = Sahand.ClinicPro.Domain.Enums.SettingDataType.String,
                }, ct);
            }
            else
            {
                var tracked = await repo.QueryableTracking.FirstAsync(s => s.Id == row.Id, ct);
                tracked.Value = value;
            }
        }

        if (request.Enabled is { } enabled)
            await UpsertAsync("AutoEnabled", enabled ? "true" : "false");
        if (request.Hour is { } hour)
            await UpsertAsync("AutoHour", Math.Clamp(hour, 0, 23).ToString());
        if (!string.IsNullOrWhiteSpace(request.Directory))
            await UpsertAsync("Directory", request.Directory.Trim());

        // ═══ چرخهٔ ۸۲ (گزارش مالک) — «ارسال به ایمیل» بکاپ خودکار ═══
        // «در تنظیمات بکاپ‌گیری خودکار، گزینهٔ ارسال به ایمیل و یک فیلد برای
        // ایمیل مقصد بگذار». کلیدها در همان دستهٔ Backup می‌نشینند تا کارگر
        // پس‌زمینه (AutomaticBackupWorker) آن‌ها را در هر چرخه بخواند.
        if (request.EmailEnabled is { } emailEnabled)
            await UpsertAsync("EmailEnabled", emailEnabled ? "true" : "false");
        if (request.EmailTo is not null)
        {
            // خالی‌کردن صریح فیلد هم باید ذخیره شود (غیرفعال‌سازی مقصد)
            var target = request.EmailTo.Trim();
            await UpsertAsync("EmailTo", target);
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return Ok(ApiResponse<object>.SuccessMessage("تنظیمات پشتیبان‌گیری خودکار ذخیره شد"));
    }

    /// <summary>بدنهٔ درخواست تنظیم پشتیبان خودکار</summary>
    public sealed class SaveAutoConfigRequest
    {
        /// <summary>فعال بودن پشتیبان روزانه</summary>
        public bool? Enabled { get; set; }
        /// <summary>ساعت اجرا (۰-۲۳) — به وقت محلی سرور</summary>
        public int? Hour { get; set; }
        /// <summary>پوشهٔ ذخیرهٔ فایل‌های پشتیبان</summary>
        public string? Directory { get; set; }

        /// <summary>ارسال نسخهٔ پشتیبان خودکار به ایمیل — چرخهٔ ۸۲</summary>
        public bool? EmailEnabled { get; set; }

        /// <summary>نشانی ایمیل مقصد — رشتهٔ خالی یعنی پاک‌کردن مقصد</summary>
        public string? EmailTo { get; set; }
    }

    // ═══ پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳) + تنظیم از برنامه (چرخه بیستم) ═══

    /// <summary>سرویس ابری تنبل</summary>
    private Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService? _cloudService;

    /// <summary>قفل ساخت یک‌بارهٔ سرویس ابری — چرخهٔ ۵۰ (BUG-02): ساخت سرویس خودش async است (خواندن پیکربندی از دیتابیس)؛ قفل سبک SemaphoreSlim جایگزین انسداد رشته‌ای شد</summary>
    private readonly SemaphoreSlim _cloudInitLock = new(1, 1);

    /// <summary>
    /// پیکربندی ادغامی ابری — چرخه بیستم (گزارش چهاردهم مالک):
    /// مقادیر ذخیره‌شدهٔ دیتابیس (دستهٔ BackupCloud) روی پیکربندی فایل
    /// رویه می‌شوند تا تنظیم از برنامه بدون دست‌زدن به appsettings.json کار کند
    /// </summary>
    private async Task<IConfiguration> BuildCloudConfigAsync(CancellationToken ct)
    {
        var overlay = new Dictionary<string, string?>();
        var rows = await _unitOfWork.Repository<SystemSetting>().QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == "BackupCloud")
            .ToListAsync(ct);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key) || row.Value is null)
                continue;
            // کلید Cloud.Provider → Backup:Cloud:Provider و غیره
            overlay[$"Backup:Cloud:{row.Key}"] = row.Value;
        }

        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddConfiguration(_configuration);
        if (overlay.Count > 0)
            builder.AddInMemoryCollection(overlay);
        return builder.Build();
    }

    /// <summary>
    /// دسترسی async به سرویس پشتیبان ابری — با پیکربندی ادغامی دیتابیس + فایل.
    /// چرخهٔ ۵۰ (BUG-02): پیش‌تر این ساخت به‌صورت هم‌گامِ مسدودکننده انجام می‌شد
    /// (فروش نتیجهٔ Task با تماس هم‌گام) — نخِ پردازش درخواست هنگام خواندن
    /// دیتابیسِ پیکربندی قفل می‌شد و زیر بار همزمان بن‌بست رشته‌ای می‌ساخت؛
    /// اکنون ساخت یک‌باره با قفل SemaphoreSlim و کاملاً بدون انسداد انجام
    /// می‌شود (الگوی رسمی async lazy).
    /// </summary>
    private async Task<Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService> GetCloudServiceAsync(CancellationToken ct)
    {
        if (_cloudService is not null) return _cloudService;
        await _cloudInitLock.WaitAsync(ct);
        try
        {
            // دوباره‌سنجی پس از گرفتن قفل — شاید نخِ دیگری هم‌زمان ساخته باشد
            if (_cloudService is not null) return _cloudService;
            return _cloudService = new Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService(
                await BuildCloudConfigAsync(ct),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService>.Instance);
        }
        finally
        {
            _cloudInitLock.Release();
        }
    }

    /// <summary>
    /// خواندن پیکربندی پشتیبان ابری — چرخه بیستم (گزارش چهاردهم مالک):
    /// «جایی برای فعال و تنظیم کردن پشتیبان ابری نیست» — این endpoint وضعیت
    /// فعلی (بدون افشای کلیدها) را برای فرم تنظیمات ابری برنامه برمی‌گرداند.
    /// </summary>
    [HttpGet("cloud/config")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> GetCloudConfig(CancellationToken ct)
    {
        var config = await BuildCloudConfigAsync(ct);
        var provider = config["Backup:Cloud:Provider"] ?? "";
        return Ok(ApiResponse<object>.Success(new
        {
            provider = provider.ToLowerInvariant(),
            endpoint = config["Backup:Cloud:Endpoint"] ?? "",
            bucket = config["Backup:Cloud:Bucket"] ?? "",
            region = config["Backup:Cloud:Region"] ?? "us-east-1",
            prefix = config["Backup:Cloud:Prefix"] ?? "backups/",
            localFolder = config["Backup:Cloud:LocalFolder"] ?? "",
            hasAccessKey = !string.IsNullOrWhiteSpace(config["Backup:Cloud:AccessKey"]),
            hasSecretKey = !string.IsNullOrWhiteSpace(config["Backup:Cloud:SecretKey"]),
            hasEncryptionKey = !string.IsNullOrWhiteSpace(config["Backup:Cloud:EncryptionKey"]),
            autoUpload = string.Equals(config["Backup:Cloud:AutoUpload"], "true",
                StringComparison.OrdinalIgnoreCase),
        }));
    }

    /// <summary>
    /// ذخیرهٔ پیکربندی پشتیبان ابری — چرخه بیستم. کلیدهای خالی به معنای
    /// «بدون تغییر» هستند تا رمز‌ها هر بار از کلاینت ارسال نشوند؛ provider
    /// خالی یعنی غیرفعال‌سازی. مقادیر در SystemSettings دستهٔ BackupCloud
    /// ذخیره و بلافاصله برای تست اتصال قابل استفاده‌اند.
    /// </summary>
    [HttpPut("cloud/config")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> SaveCloudConfig([FromBody] SaveCloudConfigRequest request,
        CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<SystemSetting>();
        var existing = await repo.QueryableNoTracking
            .Where(s => !s.IsDeleted && s.Category == "BackupCloud")
            .ToListAsync(ct);

        async Task UpsertAsync(string key, string? value)
        {
            if (value is null) // null = بدون تغییر (مثلاً رمز‌ها)
                return;
            var row = existing.FirstOrDefault(s => s.Key == key);
            if (row is null)
            {
                await repo.AddAsync(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Category = "BackupCloud",
                    DataType = Sahand.ClinicPro.Domain.Enums.SettingDataType.String,
                }, ct);
            }
            else
            {
                var tracked = await repo.QueryableTracking.FirstAsync(s => s.Id == row.Id, ct);
                tracked.Value = value;
            }
        }

        await UpsertAsync("Provider", string.IsNullOrWhiteSpace(request.Provider) ? "" : request.Provider.Trim().ToLowerInvariant());
        await UpsertAsync("Endpoint", request.Endpoint);
        await UpsertAsync("Bucket", request.Bucket);
        await UpsertAsync("Region", request.Region);
        await UpsertAsync("Prefix", request.Prefix);
        await UpsertAsync("LocalFolder", request.LocalFolder);
        await UpsertAsync("AccessKey", request.AccessKey);
        await UpsertAsync("SecretKey", request.SecretKey);
        await UpsertAsync("EncryptionKey", request.EncryptionKey);
        await UpsertAsync("AutoUpload", request.AutoUpload == true ? "true" : "false");

        await _unitOfWork.SaveEntitiesAsync(ct);
        _cloudService = null; // بازسازی سرویس با پیکربندی نو در فراخوانی بعدی
        return Ok(ApiResponse<object>.SuccessMessage("تنظیمات پشتیبان ابری ذخیره شد"));
    }

    /// <summary>بدنهٔ ذخیرهٔ پیکربندی ابری — مقادیر null یعنی بدون تغییر</summary>
    public sealed class SaveCloudConfigRequest
    {
        /// <summary>ارائه‌دهنده: خالی/«off» = غیرفعال، s3 = سازگار S3 (آروان/آبرار)، local = پوشهٔ محلی</summary>
        public string? Provider { get; set; }
        /// <summary>نشانی نقطهٔ اتصال S3 — مثلاً s3.ir-thr-at1.arvanstorage.ir</summary>
        public string? Endpoint { get; set; }
        /// <summary>نام سطل</summary>
        public string? Bucket { get; set; }
        /// <summary>ناحیه — پیش‌فرض us-east-1</summary>
        public string? Region { get; set; }
        /// <summary>پیشوند کلیدها در سطل — پیش‌فرض backups/</summary>
        public string? Prefix { get; set; }
        /// <summary>پوشهٔ محلی برای ارائه‌دهندهٔ local</summary>
        public string? LocalFolder { get; set; }
        /// <summary>کلید دسترسی — null یعنی بدون تغییر</summary>
        public string? AccessKey { get; set; }
        /// <summary>کلید رمزی — null یعنی بدون تغییر</summary>
        public string? SecretKey { get; set; }
        /// <summary>کلید رمزنگاری AES-256 — null یعنی بدون تغییر</summary>
        public string? EncryptionKey { get; set; }
        /// <summary>بارگذاری خودکار پس از هر پشتیبان موفق</summary>
        public bool? AutoUpload { get; set; }
    }

    /// <summary>وضعیت پشتیبان ابری — فعال بودن، آخرین بارگذاری، تعداد و حجم نسخه‌ها</summary>
    [HttpGet("cloud/status")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> CloudStatus(CancellationToken ct)
    {
        var config = await BuildCloudConfigAsync(ct);
        var provider = config["Backup:Cloud:Provider"];
        var cloudRecords = await _unitOfWork.Repository<BackupHistory>().QueryableNoTracking
            .Where(b => !b.IsDeleted && b.StorageType == StorageType.Cloud)
            .OrderByDescending(b => b.CloudUploadedAt)
            .ToListAsync(ct);

        // چرخهٔ ۵۰ (BUG-02): ساخت سرویس ابری به‌صورت async — بدون قفل رشته
        var cloudSvc = await GetCloudServiceAsync(ct);
        return Ok(ApiResponse<object>.Success(new
        {
            enabled = !string.IsNullOrWhiteSpace(provider),
            provider = cloudSvc.CreateStorage()?.ProviderName,
            encrypted = cloudSvc.IsEncryptionEnabled,
            lastUpload = cloudRecords.FirstOrDefault()?.CloudUploadedAt,
            lastFile = cloudRecords.FirstOrDefault()?.CloudFileName,
            versions = cloudRecords.Count,
            totalSize = cloudRecords.Sum(b => b.FileSize ?? 0),
        }));
    }

    /// <summary>تست اتصال به مقصد ابری</summary>
    [HttpPost("cloud/test")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> CloudTest(CancellationToken ct)
    {
        // چرخهٔ ۵۰ (BUG-02): ساخت سرویس ابری به‌صورت async — بدون قفل رشته
        var storage = (await GetCloudServiceAsync(ct)).CreateStorage();
        if (storage is null)
            return Ok(ApiResponse<object>.Success(new { ok = false, message = "پشتیبان ابری فعال نیست — Backup:Cloud:Provider را تنظیم کنید" }));

        var (ok, message) = await storage.TestConnectionAsync(ct);
        return Ok(ApiResponse<object>.Success(new { ok, message }));
    }

    /// <summary>بارگذاری دستی آخرین پشتیبان به ابر</summary>
    /// <remarks>جدیدترین رکورد موفق با فایل موجود روی دیسک انتخاب و با رمزنگاری AES-256-GCM بارگذاری می‌شود</remarks>
    [HttpPost("cloud/upload-latest")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> CloudUploadLatest(CancellationToken ct)
    {
        var latest = await _unitOfWork.Repository<BackupHistory>().QueryableTracking
            .Where(b => !b.IsDeleted && b.Status == BackupStatus.Success && b.FilePath != null)
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync(ct);
        if (latest is null)
            return BadRequest(ApiResponse<string>.Failure("هیچ پشتیبانی برای بارگذاری ثبت نشده است"));

        try
        {
            // چرخهٔ ۵۰ (BUG-02): ساخت سرویس ابری به‌صورت async — بدون قفل رشته
            var uploaded = await (await GetCloudServiceAsync(ct)).UploadBackupAsync(latest, ct);
            await _unitOfWork.SaveEntitiesAsync(ct);
            return Ok(ApiResponse<object>.Success(new
            {
                fileName = uploaded.FileName,
                size = uploaded.Size,
                encrypted = latest.IsCloudEncrypted,
                message = "بارگذاری ابری انجام شد",
            }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(ex.Message));
        }
    }

    /// <summary>تست صحت بازیابی — دانلود و رمزگشایی بدون دست‌زدن به بانک اصلی</summary>
    [HttpPost("cloud/verify-restore/{id:long}")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> CloudVerifyRestore(long id, CancellationToken ct)
    {
        var record = await _unitOfWork.Repository<BackupHistory>().QueryableNoTracking
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted && b.StorageType == StorageType.Cloud, ct);
        if (record?.CloudFileName is null)
            return BadRequest(ApiResponse<string>.Failure("این رکورد نسخه ابری ندارد"));

        // چرخهٔ ۵۰ (BUG-02): ساخت سرویس ابری به‌صورت async — بدون قفل رشته
        var (ok, message, plainSize) = await (await GetCloudServiceAsync(ct)).VerifyRestoreAsync(record.CloudFileName, ct);
        return Ok(ApiResponse<object>.Success(new { ok, message, plainSize }));
    }
}
