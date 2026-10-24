using System.Globalization;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>یک مناسبت روز — عنوان + برچسب تقویم (شمسی/قمری/میلادی)</summary>
public sealed record DayOccasion(string Title, string CalendarLabel);

/// <summary>
/// مناسبت‌های روز جاری در سه تقویم شمسی، قمری و میلادی — برای کارت
/// «ساعت و تاریخ امروز» داشبورد. فهرست‌ها ثابت و درون‌برنامه‌ای‌اند
/// (مناسبت‌های رسمی و شناخته‌شدهٔ هر تقویم)؛ این‌که کدام تقویم‌ها نمایش
/// داده شوند از تنظیمات سیستم قابل انتخاب است.
/// </summary>
public static class DayOccasions
{
    /// <summary>نام ماه‌های قمری</summary>
    private static readonly string[] HijriMonthNames =
    {
        "محرم", "صفر", "ربیع‌الاول", "ربیع‌الثانی", "جمادی‌الاول", "جمادی‌الثانی",
        "رجب", "شعبان", "رمضان", "شوال", "ذی‌القعده", "ذی‌الحجه"
    };

    /// <summary>نام ماه‌های میلادی به فارسی</summary>
    private static readonly string[] GregorianMonthNamesFa =
    {
        "ژانویه", "فوریه", "مارس", "آوریل", "مه", "ژوئن",
        "ژوئیه", "اوت", "سپتامبر", "اکتبر", "نوامبر", "دسامبر"
    };

    /// <summary>مناسبت‌های شمسی — کلید: (ماه، روز)</summary>
    private static readonly Dictionary<(int, int), string[]> Jalali = new()
    {
        [(1, 1)] = new[] { "آغاز نوروز و سال نو" },
        [(1, 2)] = new[] { "عید نوروز" },
        [(1, 3)] = new[] { "عید نوروز" },
        [(1, 4)] = new[] { "عید نوروز" },
        [(1, 12)] = new[] { "روز جمهوری اسلامی ایران" },
        [(1, 13)] = new[] { "روز طبیعت (سیزده‌بدر)" },
        [(1, 25)] = new[] { "بزرگداشت عطار نیشابوری" },
        [(2, 1)] = new[] { "بزرگداشت سعدی" },
        [(2, 3)] = new[] { "بزرگداشت شیخ بهایی — روز ملی فناوری فضایی" },
        [(2, 12)] = new[] { "روز معلم (شهادت استاد مطهری)" },
        [(3, 14)] = new[] { "رحلت امام خمینی (ره)" },
        [(3, 15)] = new[] { "قیام خونین ۱۵ خرداد" },
        [(4, 7)] = new[] { "شهادت دکتر بهشتی و ۷۲ تن (روز قوهٔ قضاییه)" },
        [(5, 17)] = new[] { "روز خبرنگار" },
        [(6, 1)] = new[] { "روز پزشک (بزرگداشت ابوعلی سینا)" },
        [(6, 4)] = new[] { "روز کارمند" },
        [(6, 31)] = new[] { "آغاز هفتهٔ دفاع مقدس" },
        [(7, 8)] = new[] { "روز جهانی کودک" },
        [(8, 13)] = new[] { "روز دانش‌آموز و تسخیر لانهٔ جاسوسی" },
        [(9, 16)] = new[] { "روز دانشجو" },
        [(9, 30)] = new[] { "شب یلدا (شب چله)" },
        [(10, 9)] = new[] { "حماسهٔ ۹ دی — روز بصیرت" },
        [(11, 22)] = new[] { "پیروزی انقلاب اسلامی ایران" },
        [(12, 29)] = new[] { "روز ملی شدن صنعت نفت ایران" },
    };

    /// <summary>مناسبت‌های قمری — کلید: (ماه، روز)</summary>
    private static readonly Dictionary<(int, int), string[]> Hijri = new()
    {
        [(1, 9)] = new[] { "تاسوعای حسینی" },
        [(1, 10)] = new[] { "عاشورای حسینی" },
        [(2, 20)] = new[] { "اربعین حسینی" },
        [(2, 28)] = new[] { "رحلت پیامبر اکرم (ص) و شهادت امام حسن مجتبی (ع)" },
        [(2, 30)] = new[] { "شهادت امام رضا (ع)" },
        [(3, 8)] = new[] { "شهادت امام حسن عسکری (ع) و آغاز امامت حضرت مهدی (عج)" },
        [(3, 17)] = new[] { "میلاد پیامبر اکرم (ص) و امام جعفر صادق (ع)" },
        [(6, 3)] = new[] { "شهادت حضرت فاطمهٔ زهرا (س)" },
        [(6, 20)] = new[] { "میلاد حضرت فاطمهٔ زهرا (س) — روز زن و مادر" },
        [(7, 13)] = new[] { "میلاد امام علی (ع) — روز پدر" },
        [(7, 27)] = new[] { "مبعث پیامبر اکرم (ص)" },
        [(8, 15)] = new[] { "میلاد حضرت مهدی (عج) — نیمهٔ شعبان" },
        [(9, 19)] = new[] { "شب قدر" },
        [(9, 21)] = new[] { "شهادت امام علی (ع) — شب قدر" },
        [(9, 23)] = new[] { "شب قدر" },
        [(10, 1)] = new[] { "عید سعید فطر" },
        [(10, 2)] = new[] { "تعطیل به مناسبت عید سعید فطر" },
        [(11, 11)] = new[] { "میلاد امام رضا (ع)" },
        [(12, 9)] = new[] { "روز عرفه" },
        [(12, 10)] = new[] { "عید سعید قربان" },
        [(12, 18)] = new[] { "عید سعید غدیر خم" },
    };

    /// <summary>مناسبت‌های میلادی — کلید: (ماه، روز)</summary>
    private static readonly Dictionary<(int, int), string[]> Gregorian = new()
    {
        [(1, 1)] = new[] { "آغاز سال نو میلادی" },
        [(12, 25)] = new[] { "میلاد حضرت عیسی مسیح (ع) — کریسمس" },
        [(3, 8)] = new[] { "روز جهانی زن" },
        [(4, 7)] = new[] { "روز جهانی بهداشت" },
        [(5, 1)] = new[] { "روز جهانی کار و کارگر" },
        [(5, 12)] = new[] { "روز جهانی پرستار" },
        [(9, 27)] = new[] { "روز جهانی گردشگری" },
        [(10, 24)] = new[] { "روز ملل متحد" },
        [(12, 10)] = new[] { "روز جهانی حقوق بشر" },
    };

    /// <summary>تاریخ بلند شمسی — مانند «شنبه ۲۴ آبان ۱۴۰۵»</summary>
    public static string JalaliLong(DateTime now)
    {
        var (y, m, d) = PersianDateHelper.ToJalali(now);
        return PersianDateHelper.ToPersianDigits(
            $"{WeekdayFa(now)} {d} {PersianDateHelper.MonthNames[m - 1]} {y}");
    }

    /// <summary>تاریخ بلند میلادی به فارسی — مانند «شنبه ۱۴ نوامبر ۲۰۲۶»</summary>
    public static string GregorianLong(DateTime now)
        => PersianDateHelper.ToPersianDigits(
            $"{WeekdayFa(now)} {now.Day} {GregorianMonthNamesFa[now.Month - 1]} {now.Year}");

    /// <summary>تاریخ بلند قمری — مانند «۳ جمادی‌الثانی ۱۴۴۸»</summary>
    public static string HijriLong(DateTime now)
    {
        var hc = new HijriCalendar();
        return PersianDateHelper.ToPersianDigits(
            $"{hc.GetDayOfMonth(now)} {HijriMonthNames[hc.GetMonth(now) - 1]} {hc.GetYear(now)} هجری قمری");
    }

    /// <summary>نام فارسی روز هفته از روی تاریخ میلادی</summary>
    private static string WeekdayFa(DateTime now)
        => PersianDateHelper.DayNames[((int)now.DayOfWeek + 1) % 7];

    /// <summary>
    /// مناسبت‌های امروز بر اساس تقویم‌های فعال در تنظیمات؛ هر مناسبت
    /// برچسب تقویم خودش را دارد تا منشأ آن معلوم باشد.
    /// </summary>
    public static IReadOnlyList<DayOccasion> ForToday(
        DateTime now, bool jalali, bool hijri, bool gregorian)
    {
        var list = new List<DayOccasion>();
        if (jalali)
        {
            var (_, m, d) = PersianDateHelper.ToJalali(now);
            if (Jalali.TryGetValue((m, d), out var titles))
                list.AddRange(titles.Select(t => new DayOccasion(t, "شمسی")));
        }
        if (hijri)
        {
            var hc = new HijriCalendar();
            if (Hijri.TryGetValue((hc.GetMonth(now), hc.GetDayOfMonth(now)), out var titles))
                list.AddRange(titles.Select(t => new DayOccasion(t, "قمری")));
        }
        if (gregorian && Gregorian.TryGetValue((now.Month, now.Day), out var gtitles))
            list.AddRange(gtitles.Select(t => new DayOccasion(t, "میلادی")));
        return list;
    }
}
