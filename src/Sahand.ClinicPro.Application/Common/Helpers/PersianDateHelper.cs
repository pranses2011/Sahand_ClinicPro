namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// راهنمای تقویم شمسی جلالی — تبدیل دوطرفه میلادی و شمسی با الگوریتم استاندارد
/// هیچ وابستگی خارجی ندارد
/// </summary>
public static class PersianDateHelper
{
    /// <summary>تعداد روزهای ماه‌های شمسی — شش ماه اول ۳۱ روزه</summary>
    private static readonly int[] JalaliMonthDays = { 31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29 };

    /// <summary>نام ماه‌های شمسی</summary>
    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    /// <summary>نام روزهای هفته شمسی — شنبه اول</summary>
    public static readonly string[] DayNames =
    {
        "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه"
    };

    /// <summary>
    /// تبدیل تاریخ میلادی به رشته شمسی با فرمت 1403/07/12
    /// </summary>
    public static string ToShamsi(DateTime gregorianDate)
    {
        var (year, month, day) = ToJalali(gregorianDate);
        return $"{year:D4}/{month:D2}/{day:D2}";
    }

    /// <summary>
    /// تبدیل تاریخ میلادی به رشته شمسی با ساعت — فرمت 1403/07/12 14:35
    /// </summary>
    public static string ToShamsiWithTime(DateTime gregorianDate)
    {
        var iranTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            gregorianDate, "Iran Standard Time");
        return $"{ToShamsi(iranTime)} {iranTime.Hour:D2}:{iranTime.Minute:D2}";
    }

    /// <summary>
    /// تبدیل تاریخ میلادی به شمسی — خروجی سه‌تایی سال و ماه و روز
    /// </summary>
    public static (int Year, int Month, int Day) ToJalali(DateTime gregorianDate)
    {
        var gy = gregorianDate.Year;
        var gm = gregorianDate.Month;
        var gd = gregorianDate.Day;

        var gdm = new[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        var jy = gy <= 1600 ? 0 : 979;
        gy -= gy <= 1600 ? 621 : 1600;
        var gy2 = gm > 2 ? gy + 1 : gy;

        var days = 365 * gy
                   + (gy2 + 3) / 4 - (gy2 + 99) / 100 + (gy2 + 399) / 400 - 80
                   + gd + gdm[gm - 1];

        jy += 33 * (days / 12053);
        days %= 12053;
        jy += 4 * (days / 1461);
        days %= 1461;

        if (days > 365)
        {
            jy += (days - 1) / 365;
            days = (days - 1) % 365;
        }

        int jm, jd;
        if (days < 186)
        {
            jm = 1 + days / 31;
            jd = 1 + days % 31;
        }
        else
        {
            jm = 7 + (days - 186) / 30;
            jd = 1 + (days - 186) % 30;
        }

        return (jy, jm, jd);
    }

    /// <summary>
    /// تبدیل تاریخ شمسی به میلادی
    /// </summary>
    public static DateTime ToGregorian(int jalaliYear, int jalaliMonth, int jalaliDay)
    {
        if (jalaliMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(jalaliMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد");
        if (jalaliDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(jalaliDay), "روز شمسی باید بین ۱ تا ۳۱ باشد");

        var gy = jalaliYear <= 979 ? 621 : 1600;
        var jy = jalaliYear - (jalaliYear <= 979 ? 0 : 979);

        var days = 365 * jy + (jy / 33) * 8 + ((jy % 33) + 3) / 4
                   + 78 + jalaliDay
                   + (jalaliMonth < 7 ? (jalaliMonth - 1) * 31 : (jalaliMonth - 7) * 30 + 186);

        gy += 400 * (days / 146097);
        days %= 146097;
        if (days > 36524)
        {
            gy += 100 * (--days / 36524);
            days %= 36524;
            if (days >= 365) days++;
        }
        gy += 4 * (days / 1461);
        days %= 1461;
        if (days > 365)
        {
            gy += (days - 1) / 365;
            days = (days - 1) % 365;
        }

        var gd = days + 1;
        var leap = (gy % 4 == 0 && gy % 100 != 0) || gy % 400 == 0;
        var salA = new[] { 0, 31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        var gm = 0;
        while (gm < 13 && gd > salA[gm]) gd -= salA[gm++];

        return new DateTime(gy, gm, gd);
    }

    /// <summary>
    /// تبدیل رشته شمسی به میلادی — فرمت قابل قبول: 1403/07/12 یا 1403-07-12
    /// </summary>
    public static DateTime? ToGregorian(string? shamsiDate)
    {
        if (string.IsNullOrWhiteSpace(shamsiDate)) return null;
        var parts = shamsiDate.Trim().Replace('-', '/').Split('/');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d)) return null;
        try
        {
            return ToGregorian(y, m, d);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// محاسبه سن از تاریخ تولد شمسی
    /// </summary>
    public static int CalculateAge(DateTime birthDate)
    {
        var (jy, jm, jd) = ToJalali(DateTime.UtcNow);
        var (by, bm, bd) = ToJalali(birthDate);
        var age = jy - by;
        if (jm < bm || (jm == bm && jd < bd)) age--;
        return age < 0 ? 0 : age;
    }

    /// <summary>
    /// تبدیل ارقام لاتین به فارسی برای نمایش
    /// </summary>
    public static string ToPersianDigits(string input)
    {
        var persian = "۰۱۲۳۴۵۶۷۸۹";
        var chars = input.Select(c => char.IsDigit(c) ? persian[c - '0'] : c);
        return new string(chars.ToArray());
    }

    /// <summary>
    /// بازه تقویمی میلادیِ یک ماه شمسی — [از، تا) برای فیلتر کوئری‌های دوره‌ای
    /// (تولید حقوق، حضوروغیاب، گزارش سود و حقوق درصدی و ...)
    /// تقویم شمسی: فروردین از حدود ۲۱ مارس شروع می‌شود؛ تقریب ابتدای ماه میلادی کافی است.
    /// </summary>
    /// <param name="jalaliYear">سال شمسی دوره</param>
    /// <param name="jalaliMonth">ماه شمسی دوره — ۱ تا ۱۲</param>
    public static (DateOnly From, DateOnly To) MonthRange(int jalaliYear, int jalaliMonth)
    {
        if (jalaliYear is < 1300 or > 1600)
            throw new ArgumentOutOfRangeException(nameof(jalaliYear), "سال شمسی باید بین ۱۳۰۰ تا ۱۶۰۰ باشد");
        if (jalaliMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(jalaliMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد");

        // نگاشت ماه شمسی به ماه میلادی آغازین: فروردین→مارس(3) ... بهمن→ژانویه(1) اسفند→فوریه(2)
        var gregorianMonth = jalaliMonth switch
        {
            1 => 3, 2 => 4, 3 => 5, 4 => 6, 5 => 7, 6 => 8,
            7 => 9, 8 => 10, 9 => 11, 10 => 12, 11 => 1, _ => 2
        };

        // ⚠️ رفع باگ چرخه نهم: فرمول قبلی «سال − ۶۲۲» سال‌های بی‌معنی (مثلاً ۷۸۲)
        // تولید می‌کرد و بازه دوره هیچ‌وقت به فاکتورهای واقعی نمی‌رسید.
        // رابطه درست: ۱ فروردین ۱ شمسی = ۲۲ مارس ۶۲۲ میلادی →
        // میلادی = شمسی + ۶۲۱ ؛ بهمن/اسفند به سال میلادی بعدی تعلق دارند (+۶۲۲)
        var gregorianYear = jalaliYear + 621;
        if (jalaliMonth is 11 or 12) gregorianYear++;

        var from = new DateOnly(gregorianYear, gregorianMonth, 1);
        return (from, from.AddMonths(1));
    }
}
