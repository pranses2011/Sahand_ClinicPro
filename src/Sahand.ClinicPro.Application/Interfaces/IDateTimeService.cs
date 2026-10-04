namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس زمان — انتزاع زمان برای تست‌پذیری
/// </summary>
public interface IDateTimeService
{
    /// <summary>زمان فعلی به وقت جهانی</summary>
    DateTime UtcNow { get; }

    /// <summary>امروز به وقت محلی ایران</summary>
    DateOnly TodayIran { get; }

    /// <summary>زمان فعلی به وقت ایران</summary>
    DateTime NowIran { get; }
}
