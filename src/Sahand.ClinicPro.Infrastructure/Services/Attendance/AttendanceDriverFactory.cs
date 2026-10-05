using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Attendance;

/// <summary>
/// کارخانه درایورها — نگاشت برند دستگاه به درایور پیاده‌سازی‌شده
/// درایورهای بی‌نمونه (Suprema/Anviz) به شبیه‌ساز هدایت نمی‌شوند؛
/// برای آنها سازنده پرچم «بدون درایور» برمی‌گرداند تا پیام خطا روشن باشد
/// </summary>
public class AttendanceDriverFactory : IAttendanceDriverFactory
{
    private readonly ZktecoPullDriver _zkteco;
    private readonly HikvisionIsapiDriver _hikvision;
    private readonly SimulatedDeviceDriver _simulator;

    /// <summary>سازنده با تزریق درایورهای ثبت‌شده</summary>
    public AttendanceDriverFactory(
        ZktecoPullDriver zkteco,
        HikvisionIsapiDriver hikvision,
        SimulatedDeviceDriver simulator)
    {
        _zkteco = zkteco;
        _hikvision = hikvision;
        _simulator = simulator;
    }

    /// <summary>ساخت درایور برای برند</summary>
    public IAttendanceDeviceDriver Create(DeviceBrand brand) => brand switch
    {
        DeviceBrand.ZKTeco => _zkteco,
        DeviceBrand.Hikvision => _hikvision,
        DeviceBrand.Generic => _simulator,
        _ => throw new NotSupportedException(
            $"برند «{brand}» هنوز درایور ندارد — از ZKTeco، Hikvision یا Generic استفاده کنید"),
    };

    /// <summary>درایور متناسب با دستگاه</summary>
    public IAttendanceDeviceDriver DriverFor(AttendanceDevice device) => Create(device.Brand);
}
