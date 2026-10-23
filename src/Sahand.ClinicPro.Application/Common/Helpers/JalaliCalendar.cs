namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// کمکی نمایش تقویم شمسی برای انتخابگر تاریخ سه‌نمایی (روز / ماه / سال)
/// </summary>
/// <remarks>
/// <para>
/// این کلاس «منبع واحد» چیزهایی است که انتخابگر تاریخ شمسی برای رسم
/// ظاهرِ «شمسی + میلادی هم‌زمان» لازم دارد: نام ماه‌ها، بازهٔ میلادی هر ماه،
/// برچسب بازهٔ ۱۲سالهٔ سال‌ها، ستون هفته (شنبه = ۰) و قالب‌بندی معادل میلادی.
/// همهٔ محاسبات تقویمی از <see cref="System.Globalization.PersianCalendar"/>
/// خوانده می‌شود — هیچ طول ماه یا سال کبیسه‌ای هاردکد نیست.
/// </para>
/// <para>
/// کاربرد: کنترل <c>JalaliDatePicker</c> در لایهٔ دسکتاپ و آزمون‌های واحد
/// همین لایهٔ کاربرد (بدون وابستگی به WPF).
/// </para>
/// </remarks>
public static class JalaliCalendar
{
    /// <summary>تقویم فارسی .NET — مرجع طول ماه و کبیسه</summary>
    private static readonly System.Globalization.PersianCalendar Calendar = new();

    /// <summary>
    /// کمینهٔ سال قابل پیمایش/تایپ در انتخابگر — سال‌های پیش از آن در تقویم
    /// کاری درمانگاه معنا ندارد و برای بیرون‌ماندن از استثنای تقویم مرجع است
    /// </summary>
    public const int MinYear = 1200;

    /// <summary>
    /// بیشینهٔ سال قابل پیمایش/تایپ در انتخابگر — مانع رسیدن به سال‌هایی
    /// که تقویم مرجع برایشان استثنا می‌دهد (چرخهٔ ۸۱ — پایداری ناوبری)
    /// </summary>
    public const int MaxYear = 1500;

    /// <summary>نام ماه‌های شمسی — فروردین تا اسفند</summary>
    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    /// <summary>
    /// حروف مخفف روزهای هفته — ترتیب ستون‌ها در نمای راست‌به‌چپ:
    /// شنبه، یک‌شنبه، دوشنبه، سه‌شنبه، چهارشنبه، پنج‌شنبه، جمعه
    /// </summary>
    public static readonly string[] WeekDayShortNames = { "ش", "ی", "د", "س", "چ", "پ", "ج" };

    /// <summary>
    /// زیرنویس میلادیِ هر ماه شمسی — «با عدد ماه داخل پرانتز»
    /// (جدول ثابتِ مشخصات طراحی؛ ترتیب = فروردین تا اسفند)
    /// </summary>
    public static readonly string[] MonthGregorianSubtitles =
    {
        "Mar (3)–Apr (4)", "Apr (4)–May (5)", "May (5)–Jun (6)",
        "Jun (6)–Jul (7)", "Jul (7)–Aug (8)", "Aug (8)–Sep (9)",
        "Sep (9)–Oct (10)", "Oct (10)–Nov (11)", "Nov (11)–Dec (12)",
        "Dec (12)–Jan (1)", "Jan (1)–Feb (2)", "Feb (2)–Mar (3)"
    };

    /// <summary>مخفف ماه‌های میلادی — نمایش لاتین با ارقام لاتین</summary>
    public static readonly string[] GregorianMonthAbbreviations =
    {
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    };

    /// <summary>محدودکردن سال شمسی به بازهٔ مجاز کنترل (بدون استثنای تقویم مرجع)</summary>
    public static int ClampYear(int jalaliYear) => Math.Clamp(jalaliYear, MinYear, MaxYear);

    /// <summary>ستون هفته برای تقویم راست‌به‌چپ — شنبه = ۰ … جمعه = ۶</summary>
    public static int WeekColumn(DateTime date) => ((int)date.DayOfWeek + 1) % 7;

    /// <summary>آیا این روز جمعه است؟ (ستون جمعه در تقویم شمسی تعطیل و قرمز است)</summary>
    public static bool IsFriday(DateTime date) => date.DayOfWeek == DayOfWeek.Friday;

    /// <summary>طول ماه شمسی از تقویم مرجع — اسفند ۲۹/۳۰ و ۳۱روزه‌ها بدون هاردکد</summary>
    public static int GetMonthLength(int jalaliYear, int jalaliMonth)
        => Calendar.GetDaysInMonth(jalaliYear, jalaliMonth);

    /// <summary>آیا سال شمسی کبیسه است؟ (اسفند ۳۰ روزه)</summary>
    public static bool IsLeapYear(int jalaliYear) => Calendar.IsLeapYear(jalaliYear);

    /// <summary>تاریخ میلادیِ یک روز شمسی — برای نمایش ستون «میلادیِ هر روز»</summary>
    public static DateTime ToGregorian(int jalaliYear, int jalaliMonth, int jalaliDay)
        => Calendar.ToDateTime(jalaliYear, jalaliMonth, jalaliDay, 0, 0, 0, 0).Date;

    /// <summary>اجزای شمسی یک تاریخ میلادی</summary>
    public static (int Year, int Month, int Day) ToJalali(DateTime gregorianDate)
        => (Calendar.GetYear(gregorianDate), Calendar.GetMonth(gregorianDate), Calendar.GetDayOfMonth(gregorianDate));

    /// <summary>
    /// شروع بازهٔ ۱۲سالهٔ نمای «سال» — طبق مشخصات: <c>سال − ((سال − ۱۴۰۰) mod 12)</c>
    /// (۱۴۰۰ تا ۱۴۱۱ یک بازه، ۱۴۱۲ تا ۱۴۲۳ بازهٔ بعد و …)
    /// </summary>
    public static int DecadeStart(int jalaliYear)
    {
        var offset = ((jalaliYear - 1400) % 12 + 12) % 12;
        return jalaliYear - offset;
    }

    /// <summary>
    /// عنوان زیرنویس سربرگ نمای «روز» — بازهٔ میلادی همان ماه شمسی با عدد ماه در پرانتز:
    /// «Oct (10) – Nov (11) 2026» و در گذر از سال میلادی
    /// «Dec (12) 2026 – Jan (1) 2027».
    /// </summary>
    public static string MonthRangeLabel(int jalaliYear, int jalaliMonth)
    {
        var first = ToGregorian(jalaliYear, jalaliMonth, 1);
        var last = ToGregorian(jalaliYear, jalaliMonth, GetMonthLength(jalaliYear, jalaliMonth));
        var firstPart = $"{GregorianMonthAbbreviations[first.Month - 1]} ({first.Month})";
        var lastPart = $"{GregorianMonthAbbreviations[last.Month - 1]} ({last.Month})";
        return first.Year == last.Year
            ? $"{firstPart} – {lastPart} {first.Year}"
            : $"{firstPart} {first.Year} – {lastPart} {last.Year}";
    }

    /// <summary>بازهٔ میلادی یک سال شمسی — «2026 – 2027» (مبنای زیرنویس نمای ماه/سال)</summary>
    public static string YearRangeLabel(int jalaliYear)
    {
        var first = ToGregorian(jalaliYear, 1, 1);
        var last = ToGregorian(jalaliYear, 12, GetMonthLength(jalaliYear, 12));
        return $"{first.Year} – {last.Year}";
    }

    /// <summary>بازهٔ میلادی یک بازهٔ ۱۲سالهٔ شمسی — «2021 – 2033»</summary>
    public static string DecadeRangeLabel(int decadeStartYear)
    {
        var first = ToGregorian(decadeStartYear, 1, 1);
        var last = ToGregorian(decadeStartYear + 11, 12, GetMonthLength(decadeStartYear + 11, 12));
        return $"{first.Year} – {last.Year}";
    }

    /// <summary>
    /// معادل میلادی تاریخ به شکل کوچک خاکستری فیلد:
    /// «Oct 23, 2026  (2026-10-23)» — بخش دوم پرانتزی و لاتین
    /// </summary>
    public static string GregorianCaption(DateTime gregorianDate)
        => $"{GregorianMonthAbbreviations[gregorianDate.Month - 1]} {gregorianDate.Day:D2}, {gregorianDate.Year}" +
           $"  ({gregorianDate.Year:D4}-{gregorianDate.Month:D2}-{gregorianDate.Day:D2})";

    /// <summary>نمایش شمسی استاندارد با ارقام لاتین — «1405/08/01» (مبنای DateShamsiText)</summary>
    public static string ToShamsi(DateTime gregorianDate)
        => $"{Calendar.GetYear(gregorianDate):D4}/{Calendar.GetMonth(gregorianDate):D2}/{Calendar.GetDayOfMonth(gregorianDate):D2}";

    /// <summary>نمایش شمسی استاندارد با ارقام فارسی — «۱۴۰۵/۰۸/۰۱»</summary>
    public static string ToShamsiFa(DateTime gregorianDate)
        => PersianDateHelper.ToPersianDigits(ToShamsi(gregorianDate));
}
