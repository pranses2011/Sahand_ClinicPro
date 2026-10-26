namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// ساعت رسمی ایران برای کل کلاینت — همهٔ نمایش‌های ساعت/تاریخ و مُهرهای
/// زمانی سمت کاربر از همین‌جا خوانده می‌شوند تا حتی اگر ساعت ویندوز روی
/// منطقهٔ دیگری باشد، برنامه همیشه وقت ایران را نشان دهد (نه فقط پذیرش).
/// مبنا: تبدیل UTC با منطقهٔ «Iran Standard Time» (و در نبودش Asia/Tehran).
/// </summary>
public static class IranClock
{
    private static readonly TimeZoneInfo IranZone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* منطقهٔ بعدی */ }
        }
        return TimeZoneInfo.Local;
    }

    /// <summary>اکنون به وقت رسمی ایران</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranZone);

    /// <summary>امروز (نیمه‌شب) به وقت رسمی ایران</summary>
    public static DateTime Today => Now.Date;
}
