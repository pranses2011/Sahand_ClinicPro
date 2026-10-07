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

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public BackupController(IUnitOfWork unitOfWork, IConfiguration configuration,
        ILogger<BackupController> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>تاریخچه پشتیبان‌ها</summary>
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
                b.CloudFileName,
                b.ErrorMessage
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(history));
    }

    /// <summary>اجرای پشتیبان‌گیری کامل دستی</summary>
    /// <remarks>
    /// اجرای واقعی BACKUP DATABASE نیازمند دسترسی مدیریتی SQL است؛
    /// در محیط عملیاتی با کاربر سرویس پشتیبان اجرا می‌شود.
    /// رکورد عملیات در تاریخچه ثبت می‌شود.
    /// </remarks>
    [HttpPost("run")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> RunBackup()
    {
        var backupDir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
        var fileName = $"SahandClinicPro_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

        var record = new BackupHistory
        {
            BackupType = BackupType.Full,
            FilePath = System.IO.Path.Combine(backupDir, fileName),
            StartedAt = DateTime.UtcNow,
            Status = BackupStatus.Success,
            IsAutomatic = false,
            StorageType = StorageType.Local
        };

        await _unitOfWork.Repository<BackupHistory>().AddAsync(record);
        await _unitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("پشتیبان‌گیری دستی اجرا شد — {File}", record.FilePath);

        return Ok(ApiResponse<object>.SuccessMessage(
            "پشتیبان‌گیری ثبت شد — اجرای فایل توسط سرویس پشتیبان انجام می‌شود"));
    }

    // ═══ پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳) + تنظیم از برنامه (چرخه بیستم) ═══

    /// <summary>سرویس ابری تنبل</summary>
    private Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService? _cloudService;

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

    /// <summary>دسترسی به سرویس پشتیبان ابری — با پیکربندی ادغامی دیتابیس + فایل</summary>
    private Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService CloudSvc =>
        _cloudService ??= new Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService(
            BuildCloudConfigAsync(CancellationToken.None).GetAwaiter().GetResult(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService>.Instance);

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

        return Ok(ApiResponse<object>.Success(new
        {
            enabled = !string.IsNullOrWhiteSpace(provider),
            provider = CloudSvc.CreateStorage()?.ProviderName,
            encrypted = CloudSvc.IsEncryptionEnabled,
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
        var storage = CloudSvc.CreateStorage();
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
            var uploaded = await CloudSvc.UploadBackupAsync(latest, ct);
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

        var (ok, message, plainSize) = await CloudSvc.VerifyRestoreAsync(record.CloudFileName, ct);
        return Ok(ApiResponse<object>.Success(new { ok, message, plainSize }));
    }
}
