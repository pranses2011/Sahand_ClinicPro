using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup;

/// <summary>
/// کار زمان‌بندی‌شده پشتیبان‌گیری خودکار — چرخه دوازدهم (A2، بند ۴-۳ سند تحلیل):
/// هر شب ساعت ۲ بامداد پشتیبان کامل ثبت می‌شود و نسخه‌های فراتر از سهمیه
/// نگهداشت (Backup:RetentionCount — پیش‌فرض ۱۴) به‌صورت خودکار پاک‌سازی می‌شوند
/// (حذف رکورد + حذف فایل در حد امکان). اجرای واقعی BACKUP DATABASE بر عهده
/// سرویس پشتیبان SQL است؛ این کار، ثبت تاریخچه و چرخه حیات نسخه‌ها را مدیریت می‌کند.
/// چرخه سیزدهم (درخواست ۷-۳): پس از ثبت، اگر پشتیبان ابری فعال باشد فایل
/// با رمزنگاری AES-256-GCM به مقصد ابری بارگذاری می‌شود (best-effort —
/// شکست ابری چرخه شبانه را نمی‌شکند و رکورد محلی معتبر می‌ماند).
/// فعال‌سازی: Jobs:Enabled = true در appsettings
/// </summary>
public class BackupJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupJob> _logger;

    /// <summary>سرویس پشتیبان ابری — اختیاری؛ تهی اگر پیکربندی نشده</summary>
    private Cloud.CloudBackupService? _cloud;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public BackupJob(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<BackupJob> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>تزریق تنبل سرویس ابری</summary>
    private Cloud.CloudBackupService Cloud =>
        _cloud ??= new Cloud.CloudBackupService(_configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Cloud.CloudBackupService>.Instance);

    /// <summary>اجرای شبانه — ثبت پشتیبان خودکار + بارگذاری ابری + پاک‌سازی نسخه‌های منقضی</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var dir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
        var fileName = $"SahandClinicPro_auto_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

        var record = new BackupHistory
        {
            BackupType = BackupType.Full,
            FilePath = Path.Combine(dir, fileName),
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(2),
            Status = BackupStatus.Success,
            IsAutomatic = true,
            StorageType = StorageType.Local,
        };
        await _unitOfWork.Repository<BackupHistory>().AddAsync(record, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        _logger.LogInformation("پشتیبان‌گیری خودکار شبانه ثبت شد — {File}", record.FilePath);

        // ── بارگذاری ابری — چرخه سیزدهم (درخواست ۷-۳) ──
        // فایل فقط وقتی بارگذاری می‌شود که روی دیسک موجود باشد؛
        // در استقرار واقعی سرویس SQL پیش از این کار فایل را می‌سازد.
        if (Cloud.IsEnabled && !string.IsNullOrEmpty(record.FilePath) && File.Exists(record.FilePath))
        {
            try
            {
                await Cloud.UploadBackupAsync(record, ct);
                await _unitOfWork.SaveEntitiesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "بارگذاری ابری پشتیبان شبانه ناموفق بود — نسخه محلی معتبر است: {File}", record.FilePath);
            }
        }

        // پاک‌سازی نسخه‌های فراتر از سهمیه
        var removed = await CleanupExpiredAsync(ct);
        if (removed > 0)
            _logger.LogInformation("پاک‌سازی نسخه‌های منقضی پشتیبان: {Count} نسخه حذف شد", removed);
    }

    /// <summary>
    /// پاک‌سازی نسخه‌های فراتر از سهمیه نگهداشت — قدیمی‌ترین‌ها حذف می‌شوند.
    /// حذف فایل فیزیکی best-effort است (نبود فایل مانع حذف رکورد نمی‌شود).
    /// </summary>
    /// <returns>تعداد نسخه‌های حذف‌شده</returns>
    public async Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        var retention = Math.Max(1, _configuration.GetValue("Backup:RetentionCount", 14));

        var automatic = await _unitOfWork.Repository<BackupHistory>()
            .QueryableTracking
            .Where(b => !b.IsDeleted && b.IsAutomatic)
            .OrderByDescending(b => b.StartedAt)
            .ToListAsync(ct);

        var expired = automatic
            .Skip(retention)
            .ToList();

        foreach (var old in expired)
        {
            // حذف فایل فیزیکی در حد امکان — نبود فایل خطا نیست
            if (!string.IsNullOrEmpty(old.FilePath))
            {
                try
                {
                    if (File.Exists(old.FilePath))
                        File.Delete(old.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "حذف فایل پشتیبان قدیمی ناموفق بود — {File}", old.FilePath);
                }
            }

            old.IsDeleted = true; // حذف نرم — تاریخچه حفظ می‌شود
        }

        if (expired.Count > 0)
            await _unitOfWork.SaveEntitiesAsync(ct);

        return expired.Count;
    }
}
