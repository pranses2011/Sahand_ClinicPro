using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services;

/// <summary>
/// سرویس زمان — زمان فعلی جهانی و به‌وقت ایران
/// </summary>
public class DateTimeService : IDateTimeService
{
    /// <summary>منطقه زمانی رسمی ایران</summary>
    private static readonly TimeZoneInfo IranZone =
        TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

    /// <summary>زمان فعلی به وقت جهانی</summary>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>زمان فعلی به وقت ایران</summary>
    public DateTime NowIran => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranZone);

    /// <summary>امروز به وقت ایران</summary>
    public DateOnly TodayIran => DateOnly.FromDateTime(NowIran);
}
