using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.License;

/// <summary>
/// کار زمان‌بندی‌شده تپ قلب لایسنس — روزانه ساعت ۳ بامداد
/// اعلان حضور به سرور لایسنس و تازه‌سازی محدودیت‌ها و ماژول‌ها
/// </summary>
public class LicenseHeartbeatJob
{
    private readonly ILicenseService _license;
    private readonly ILogger<LicenseHeartbeatJob> _logger;

    /// <summary>سازنده با تزریق سرویس لایسنس و لاگر</summary>
    public LicenseHeartbeatJob(ILicenseService license, ILogger<LicenseHeartbeatJob> logger)
    {
        _license = license;
        _logger = logger;
    }

    /// <summary>اجرای تپ قلب — خطاها بلعیده می‌شوند تا کار روز بعد دست‌نخورده بماند</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            var report = await _license.HeartbeatAsync(ct);
            if (!report.IsValid)
                _logger.LogWarning("تپ قلب لایسنس ناموفق: {Message}", report.Message);
            else
                _logger.LogInformation("تپ قلب لایسنس موفق — انقضا: {Expiry}", report.ExpiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای غیرمنتظره در تپ قلب لایسنس");
        }
    }
}
