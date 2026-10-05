using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Attendance;

/// <summary>
/// شبیه‌ساز دستگاه حضور و غیاب — برای دمو، آموزش و تست یکپارچگی
/// ضربه‌های مصنوعی ولی منطقی تولید می‌کند: ورود نزدیک شروع شیفتِ کد پرسنل
/// شناسه پرسنل از EnrollNumber مستقیماً خوانده می‌شود (پرسنل شماره N → شناسه N)
/// </summary>
public class SimulatedDeviceDriver : IAttendanceDeviceDriver
{
    private readonly ILogger<SimulatedDeviceDriver> _logger;
    private readonly Random _random = new(42);

    /// <summary>برند درایور</summary>
    public DeviceBrand Brand => DeviceBrand.Generic;

    /// <summary>سازنده با لاگر</summary>
    public SimulatedDeviceDriver(ILogger<SimulatedDeviceDriver> logger) => _logger = logger;

    /// <summary>آزمون اتصال — همیشه موفق</summary>
    public Task<bool> TestConnectionAsync(AttendanceDevice device, CancellationToken ct = default)
    {
        _logger.LogDebug("شبیه‌ساز {Name} پاسخ آزمون اتصال مثبت داد", device.Name);
        return Task.FromResult(true);
    }

    /// <summary>سریال ثابت شبیه‌ساز</summary>
    public Task<string?> GetSerialAsync(AttendanceDevice device, CancellationToken ct = default)
        => Task.FromResult<string?>($"SIM-{device.Id:0000}");

    /// <summary>
    /// تولید ضربه‌های مصنوعی — برای هر شناسه ۱ تا ۲۹ یک ورود و یک خروج منطقی
    /// ورود: ۷:۴۵ تا ۸:۲۰ — خروج: ۱۴:۱۰ تا ۱۶:۴۰ (محلی ایران)
    /// </summary>
    public Task<IReadOnlyList<DevicePunch>> PullNewPunchesAsync(
        AttendanceDevice device, DateTime fromUtc, CancellationToken ct = default)
    {
        var punches = new List<DevicePunch>();

        var todayLocal = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranTimeZone));

        // فقط اگر «امروز» در پنجره کشیدن است، ضربه تازه تولید کن
        if (todayLocal.ToDateTime(TimeOnly.MinValue).ToUniversalTime() <= fromUtc)
        {
            for (var enroll = 1; enroll <= 29; enroll++)
            {
                var checkIn = todayLocal.ToDateTime(new TimeOnly(7, 45))
                    .AddMinutes(_random.Next(0, 36));
                var checkOut = todayLocal.ToDateTime(new TimeOnly(14, 10))
                    .AddMinutes(_random.Next(0, 151));

                var utcIn = ToUtc(checkIn);
                var utcOut = ToUtc(checkOut);

                if (utcIn >= fromUtc)
                    punches.Add(new DevicePunch(enroll.ToString(), utcIn, PunchState.CheckIn, 1));
                if (utcOut >= fromUtc)
                    punches.Add(new DevicePunch(enroll.ToString(), utcOut, PunchState.CheckOut, 2));
            }
        }

        _logger.LogInformation("شبیه‌ساز {Name} تعداد {Count} ضربه تولید کرد", device.Name, punches.Count);
        return Task.FromResult<IReadOnlyList<DevicePunch>>(punches);
    }

    /// <summary>پیشانی کاربران — شبیه‌ساز همیشه موفق</summary>
    public Task<bool> SyncUsersAsync(AttendanceDevice device, IReadOnlyList<DeviceUserMapping> mappings, CancellationToken ct = default)
        => Task.FromResult(true);

    private static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), IranTimeZone);

    private static readonly TimeZoneInfo IranTimeZone = CreateIranTz();

    private static TimeZoneInfo CreateIranTz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }
}
