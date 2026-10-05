using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>یک ضربه دریافتی از دستگاه — واحد انتقال بین درایور و پردازشگر</summary>
/// <param name="EnrollNumber">شناسه ثبت‌شده روی دستگاه</param>
/// <param name="PunchTime">زمان ضربه به UTC</param>
/// <param name="State">نوع ضربه</param>
/// <param name="VerifyMode">روش تأیید: ۱=اثر انگشت، ۲=کارت، ۱۵=چهره</param>
/// <param name="InOutPort">درگاه/شماره دستگاه فرعی</param>
public record DevicePunch(
    string EnrollNumber,
    DateTime PunchTime,
    PunchState State = PunchState.Undefined,
    byte VerifyMode = 1,
    byte? InOutPort = null);

/// <summary>
/// قرارداد درایور دستگاه حضور و غیاب — هر برند یک پیاده‌سازی در زیرساخت دارد
/// پیاده‌سازی‌ها باید stateless و thread-safe باشند؛ وضعیت اتصال هر نمونه دستگاه جدا نگه داشته می‌شود
/// </summary>
public interface IAttendanceDeviceDriver
{
    /// <summary>برند پشتیبانی‌شده توسط این درایور</summary>
    DeviceBrand Brand { get; }

    /// <summary>آزمون اتصال — با مهلت کوتاه، برای دکمه «تست» پنل</summary>
    Task<bool> TestConnectionAsync(AttendanceDevice device, CancellationToken ct = default);

    /// <summary>خواندن شماره سریال سخت‌افزار — برای تشخیص قطعی دستگاه جابه‌جاشده</summary>
    Task<string?> GetSerialAsync(AttendanceDevice device, CancellationToken ct = default);

    /// <summary>کشیدن ضربه‌های جدید پس از زمان داده‌شده (UTC)</summary>
    Task<IReadOnlyList<DevicePunch>> PullNewPunchesAsync(AttendanceDevice device, DateTime fromUtc, CancellationToken ct = default);

    /// <summary>پیشانی کردن فهرست کاربران روی دستگاه — نگاشت EnrollNumber به پرسنل</summary>
    Task<bool> SyncUsersAsync(AttendanceDevice device, IReadOnlyList<DeviceUserMapping> mappings, CancellationToken ct = default);
}
