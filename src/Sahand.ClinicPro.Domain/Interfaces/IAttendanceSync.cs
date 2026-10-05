using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>خروجی یک نوبت همگام‌سازی دستگاه</summary>
/// <param name="DeviceId">شناسه دستگاه</param>
/// <param name="DeviceName">نام دستگاه</param>
/// <param name="Success">آیا همگام‌سازی موفق بود</param>
/// <param name="PulledCount">تعداد لاگ تازه دریافتی</param>
/// <param name="ProcessedCount">تعداد لاگ تبدیل‌شده به حضور</param>
/// <param name="ErrorCount">تعداد لاگ خطادار (بدون نگاشت پرسنل)</param>
/// <param name="Message">پیام فارسی برای نمایش</param>
public record DeviceSyncResult(
    long DeviceId,
    string DeviceName,
    bool Success,
    int PulledCount,
    int ProcessedCount,
    int ErrorCount,
    string Message);

/// <summary>
/// کارخانه درایورها — بر اساس برند دستگاه، درایور متناسب می‌سازد
/// پیاده‌سازی در زیرساخت با DI ثبت می‌شود
/// </summary>
public interface IAttendanceDriverFactory
{
    /// <summary>ساخت درایور برای برند داده‌شده</summary>
    IAttendanceDeviceDriver Create(DeviceBrand brand);

    /// <summary>درایور متناسب با دستگاه — میان‌بر بر اساس فیلد Brand</summary>
    IAttendanceDeviceDriver DriverFor(AttendanceDevice device);
}

/// <summary>
/// همگام‌ساز حضور و غیاب — چرخه کامل «کشیدن لاگ از دستگاه تا رکورد حضور»
/// هم سرویس پس‌زمینه (زیرساخت) و هم فرمان‌های دستی اپراتور از آن استفاده می‌کنند
/// </summary>
public interface IAttendanceSynchronizer
{
    /// <summary>همگام‌سازی یک دستگاه — Pull و Persist و Process</summary>
    Task<DeviceSyncResult> SyncDeviceAsync(AttendanceDevice device, CancellationToken ct = default);

    /// <summary>همگام‌سازی همه دستگاه‌های فعال — برای سرویس دوره‌ای</summary>
    Task<IReadOnlyList<DeviceSyncResult>> SyncAllActiveDevicesAsync(CancellationToken ct = default);

    /// <summary>پردازش لاگ‌های در انتظار — تبدیل به رکورد حضور با محاسباتگر</summary>
    Task<int> ProcessPendingLogsAsync(DateOnly? date = null, long? employeeId = null, CancellationToken ct = default);
}
