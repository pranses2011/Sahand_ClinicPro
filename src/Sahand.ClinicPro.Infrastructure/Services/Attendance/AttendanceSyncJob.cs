using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Features.Attendance;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Attendance;

/// <summary>
/// کار زمان‌بندی‌شده همگام‌سازی حضور و غیاب — هر دقیقه اجرا می‌شود
/// فقط دستگاه‌هایی که موعد بازه همگام‌سازی‌شان رسیده همگام می‌شوند
/// </summary>
public class AttendanceSyncJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttendanceSynchronizer _synchronizer;
    private readonly ILogger<AttendanceSyncJob> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public AttendanceSyncJob(
        IUnitOfWork unitOfWork,
        IAttendanceSynchronizer synchronizer,
        ILogger<AttendanceSyncJob> logger)
    {
        _unitOfWork = unitOfWork;
        _synchronizer = synchronizer;
        _logger = logger;
    }

    /// <summary>
    /// اجرای دوره‌ای — دستگاه‌های فعالِ موعدرسیده سنجیده و همگام می‌شوند
    /// خطای هر دستگاه مستقل بلعیده می‌شود تا بقیه دست‌نخورده بمانند
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;

            var dueDevices = await _unitOfWork.Repository<AttendanceDevice>().QueryableNoTracking
                .Where(d => d.IsActive && !d.IsDeleted
                         && (d.LastSyncAt == null
                             || d.LastSyncAt.Value.AddMinutes(d.SyncIntervalMinutes) <= now))
                .ToListAsync(ct);

            if (dueDevices.Count == 0)
                return;

            _logger.LogInformation("همگام‌سازی دوره‌ای حضور و غیاب — {Count} دستگاه موعدرسیده", dueDevices.Count);

            foreach (var device in dueDevices)
            {
                try
                {
                    var result = await _synchronizer.SyncDeviceAsync(device, ct);
                    if (!result.Success)
                        _logger.LogWarning("همگام‌سازی دستگاه {Name} ناموفق: {Message}",
                            result.DeviceName, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطای غیرمنتظره در همگام‌سازی دستگاه {Name}", device.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای کلی کار همگام‌سازی حضور و غیاب");
        }
    }
}
