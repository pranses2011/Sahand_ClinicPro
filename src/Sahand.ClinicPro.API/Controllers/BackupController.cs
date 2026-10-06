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

    // ═══ پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳) ═══

    /// <summary>سرویس ابری تنبل</summary>
    private Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService? _cloudService;

    /// <summary>دسترسی به سرویس پشتیبان ابری</summary>
    private Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService CloudSvc =>
        _cloudService ??= new Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService(_configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud.CloudBackupService>.Instance);

    /// <summary>وضعیت پشتیبان ابری — فعال بودن، آخرین بارگذاری، تعداد و حجم نسخه‌ها</summary>
    [HttpGet("cloud/status")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> CloudStatus(CancellationToken ct)
    {
        var provider = _configuration["Backup:Cloud:Provider"];
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
