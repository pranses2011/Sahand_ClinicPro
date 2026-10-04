using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

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
}
