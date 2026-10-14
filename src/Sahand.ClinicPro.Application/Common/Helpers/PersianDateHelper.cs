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
    /// تبدیل تاریخ میلادی به رشته شمسی با ارقام فارسی — فرمت ۱۴۰۳/۰۷/۱۲.
    /// چرخه هجدهم (گزارش دوازدهم مالک): ساعت/تاریخ داشبورد و نوار بالا
    /// با ارقام انگلیسی نمایش داده می‌شد؛ این خروجیِ فارسی‌رقم جایگزین
    /// نمایش‌ها شد. ورودیِ پارس‌شونده (ToGregorian) همچنان ToShamsi است.
    /// </summary>
    public static string ToShamsiFa(DateTime gregorianDate)
        => ToPersianDigits(ToShamsi(gregorianDate));

    /// <summary>
    /// ساعت لحظه‌ای با ارقام فارسی — فرمت ۱۴:۳۵:۰۵ یا ۱۴:۳۵ (ثانیه‌دار وقتی ثانیه صفر نباشد).
    /// ساعت به وقت رسمی ایران تبدیل می‌شود تا با تقویم شمسی هماهنگ باشد.
    /// </summary>
    public static string ToClockFa(DateTime gregorianDate, bool withSeconds = true)
    {
        var iran = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            gregorianDate, "Iran Standard Time");
        var clock = withSeconds ? $"{iran.Hour:D2}:{iran.Minute:D2}:{iran.Second:D2}"
                                : $"{iran.Hour:D2}:{iran.Minute:D2}";
        return ToPersianDigits(clock);
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
    /// تبدیل تاریخ شمسی به میلادی — چرخهٔ ۷۴: اعتبارسنجی کامل روزِ ماه
    /// گزارش مالک: «الان 1363/13/36 مینویسم هم قبول میکنه در حالی که هم ماه رو
    /// اشتباه زدم و هم روز رو». پیش‌تر فقط «روز ≤ ۳۱» بررسی می‌شد و خطاهای ظریف
    /// مثل «۳۱ مهر» یا «۳۰ اسفندِ سال غیرکبیسه» بی‌صدا به روزِ ماه بعد
    /// سرریز می‌شدند (تاریخ جابه‌جا می‌شد!). اکنون طول دقیق ماه با تقویم مرجع
    /// .NET کنترل می‌شود و روزِ ناموجود به‌جای سرریز، خطا می‌دهد.
    /// </summary>
    public static DateTime ToGregorian(int jalaliYear, int jalaliMonth, int jalaliDay)
    {
        if (jalaliMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(jalaliMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد");

        // ═══ چرخهٔ ۷۴ — روزِ ماه با طول واقعی همان ماه (کبیسه‌آگاه) کنترل می‌شود ═══
        var monthLength = GetJalaliMonthLength(jalaliYear, jalaliMonth);
        if (jalaliDay < 1 || jalaliDay > monthLength)
            throw new ArgumentOutOfRangeException(nameof(jalaliDay),
                $"روز شمسی باید بین ۱ تا {monthLength} باشد (ماه {MonthNames[jalaliMonth - 1]} سال " +
                $"{jalaliYear} فقط {monthLength} روز دارد)");

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
    /// طول ماه شمسی با تقویم مرجع .NET — کبیسه‌آگاه (اسفند ۲۹ یا ۳۰ روز).
    /// چرخهٔ ۷۴: منبع واحد اعتبارسنجی «روزِ ماه» در انتخابگر تاریخ و همهٔ
    /// فرم‌ها؛ تقویم System.Globalization.PersianCalendar همان الگوریتم
    /// حسابی ۳۳ سالهٔ این هلپتر است و برای سال‌های کاری (۱۳۰۰ تا ۱۵۰۰)
    /// همیشه هم‌نتیجه است.
    /// </summary>
    /// <param name="jalaliYear">سال شمسی</param>
    /// <param name="jalaliMonth">ماه شمسی ۱ تا ۱۲</param>
    public static int GetJalaliMonthLength(int jalaliYear, int jalaliMonth)
    {
        if (jalaliMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(jalaliMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد");
        return PersianCalendarReference.GetDaysInMonth(jalaliYear, jalaliMonth);
    }

    /// <summary>تقویم مرجع .NET — نمونهٔ اشتراکی فقط‌خواندنی (بدون وضعیت)</summary>
    private static readonly System.Globalization.PersianCalendar PersianCalendarReference = new();

    /// <summary>
    /// آیا سال شمسی کبیسه است؟ (اسفند ۳۰ روزه) — چرخهٔ ۷۴.
    /// «طول اسفند = ۳۰» همان تعریف کبیسه است؛ IsLeapDay(سال، ۱۲، ۳۰) در سال
    /// غیرکبیسه به‌خاطر ناموجودبودنِ روز استثنا می‌دهد و اینجا به‌کار نمی‌رود.
    /// </summary>
    public static bool IsJalaliLeapYear(int jalaliYear)
        => PersianCalendarReference.GetDaysInMonth(jalaliYear, 12) == 30;

    /// <summary>
    /// تبدیل رشته شمسی به میلادی — فرمت قابل قبول: 1403/07/12 یا 1403-07-12
    /// چرخه نوزدهم: ارقام فارسی (۱۴۰۳/۰۷/۱۲) هم پذیرفته می‌شوند
    /// </summary>
    public static DateTime? ToGregorian(string? shamsiDate)
    {
        if (string.IsNullOrWhiteSpace(shamsiDate)) return null;
        var parts = shamsiDate.Trim().Replace('-', '/').Split('/');
        if (parts.Length != 3) return null;
        if (!int.TryParse(ToLatinDigits(parts[0]), out var y) ||
            !int.TryParse(ToLatinDigits(parts[1]), out var m) ||
            !int.TryParse(ToLatinDigits(parts[2]), out var d)) return null;
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
    /// تبدیل ارقام فارسی/عربی به لاتین برای پارس — چرخه نوزدهم
    /// </summary>
    public static string ToLatinDigits(string input)
    {
        const string fa = "۰۱۲۳۴۵۶۷۸۹";
        const string ar = "٠١٢٣٤٥٦٧٨٩";
        var chars = input.Select(c =>
        {
            var fi = fa.IndexOf(c);
            if (fi >= 0) return (char)('0' + fi);
            var ai = ar.IndexOf(c);
            if (ai >= 0) return (char)('0' + ai);
            return c;
        });
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
