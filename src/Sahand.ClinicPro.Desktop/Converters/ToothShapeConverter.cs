using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل «شکل دندان» — چرخهٔ ۴۸ (گزارش مالک: «نشانگرهای دندان که داخلش شماره
/// دندان نوشته شده و با وضعیتش با رنگ مشخص می‌شود را از حالت کادر مربع تبدیلش
/// کن به شکل دندان باشن که توش شماره باشه و رنگ داخلش وضعیتش رو نشون بده»).
/// شمارهٔ FDI دندان را به هندسهٔ وکتوریِ «جنس واقعی همان دندان» نگاشت می‌کند تا
/// هر نشانگر در چارت به‌جای مربع، خودِ دندان باشد: تاج پهن دو‌ریشه برای آسیاها،
/// تاج دوقلّهٔ تک‌ریشه برای آسیای کوچک، تاج نوک‌تیزِ ریشه‌بلند برای نیش‌ها و تاج
/// تختِ باریک برای پیش‌دندان‌ها — همان نموداری که در نرم‌افزارهای تخصصی
/// دندانپزشکی دیده می‌شود.
/// </summary>
/// <remarks>
/// قاعدهٔ نگاشت از «موقعیت دندان در ربع» (رقم یکان شمارهٔ FDI):
///   ۱ و ۲ → پیش‌دندان (مرکزی/کناری) — تاج تخت و باریک، یک ریشه؛
///   ۳ → نیش (کانین) — تاج نوک‌تیز، یک ریشهٔ بلند؛
///   ۴ و ۵ → آسیای کوچک (پرمولر) — تاج دوبرجستگی، یک ریشه؛
///   ۶ و ۷ و ۸ → آسیا (مولر) — تاج پهن، دو ریشه.
/// هندسه‌ها در دستگاه مختصات محلی ۴۰×۵۲ رسم شده‌اند و با Stretch=Uniform در
/// اندازهٔ دکمهٔ نشانگر جا می‌گیرند؛ برای فک بالا کل شکل با چرخش ۱۸۰ درجه
/// (تریگر XAML در DentistryView) ریشه‌ها را به بالا می‌برد — چیدمان استاندارد
/// اودنتوگرام: دندان‌های فک بالا ریشه به بالا و فک پایین ریشه به پایین.
/// </remarks>
public sealed class ToothShapeConverter : IValueConverter
{
    /// <summary>آسیا (مولر) — تاج پهن و گرد با دو ریشهٔ جدا؛ طرح پایه ۴۰×۵۲</summary>
    private static readonly Geometry Molar = Geometry.Parse(
        "M20,2 C12,2 6,4 5,9 C4,13 5,18 5,22 C5,25 6,26.5 8,27.5 " +
        "L6.5,33 C5.5,39 5,44 6.5,47.5 C7.5,50 11,50 11.5,47 " +
        "C12.5,43 11.5,37.5 12.5,33.5 C13.5,31 16,30.5 20,30.5 " +
        "C24,30.5 26.5,31 27.5,33.5 C28.5,37.5 27.5,43 28.5,47 " +
        "C29,50 32.5,50 33.5,47.5 C35,44 34.5,39 33.5,33 " +
        "L32,27.5 C34,26.5 35,25 35,22 C35,18 36,13 35,9 C34,4 28,2 20,2 Z");

    /// <summary>آسیای کوچک (پرمولر) — تاج دوقلّهٔ متوسط با یک ریشهٔ مرکزی</summary>
    private static readonly Geometry Premolar = Geometry.Parse(
        "M11,3 C7,3 5,6 5,10 L4.5,21 C4,25 6,27.5 8.5,28.3 L14,28.8 " +
        "C14.6,29 15,30 15.1,31.5 L15.8,44 C16,48.5 18,50 20,50 " +
        "C22,50 24,48.5 24.2,44 L24.9,31.5 C25,30 25.4,29 26,28.8 " +
        "L31.5,28.3 C34,27.5 35.5,25 35,21 L35,10 C35,6 33,3 29,3 " +
        "L26.5,5.5 L23,3.5 L20,6 L17,3.5 L13.5,5.5 Z");

    /// <summary>آسیای کوچک دوشاخه — همان تاج آسیای کوچک، با دو ریشهٔ جدا (آسیای کوچک اول فک بالا)</summary>
    private static readonly Geometry PremolarTwoRoot = Geometry.Parse(
        "M11,3 C7,3 5,6 5,10 L4.5,21 C4,25 6,27.5 8.5,28.3 L13,28.8 " +
        "C13.6,29 14,30 14.1,31.5 L13.4,42 C13.1,46.2 14.4,48.4 16,47.8 " +
        "C17.6,47.2 18.1,43 18.3,38.2 C18.5,35.6 19,34 20,34 " +
        "C21,34 21.5,35.6 21.7,38.2 C21.9,43 22.4,47.2 24,47.8 " +
        "C25.6,48.4 26.9,46.2 26.6,42 L25.9,31.5 C26,30 26.4,29 27,28.8 " +
        "L31.5,28.3 C34,27.5 35.5,25 35,21 L35,10 C35,6 33,3 29,3 " +
        "L26.5,5.5 L23,3.5 L20,6 L17,3.5 L13.5,5.5 Z");

    /// <summary>آسیای سه‌ریشه — همان تاج پهن آسیا، با سه ریشهٔ جدا (آسیاهای فک بالا)</summary>
    private static readonly Geometry MolarThreeRoot = Geometry.Parse(
        "M20,2 C12,2 6,4 5,9 C4,13 5,18 5,22 C5,25 6,26.5 8,27.5 " +
        "L6.5,33 C5.5,39 5,44 6.5,47.5 C7.5,50 10.6,50 11.1,47 " +
        "C11.9,43.4 11.1,38 12.1,34.6 C12.7,32.9 14.6,32.2 16.1,33.2 " +
        "L17.1,40.5 C17.5,44.8 18.2,48.5 20,48.5 C21.8,48.5 22.5,44.8 22.9,40.5 " +
        "L23.9,33.2 C25.4,32.2 27.3,32.9 27.9,34.6 C28.9,38 28.1,43.4 28.9,47 " +
        "C29.4,50 32.5,50 33.5,47.5 C35,44 34.5,39 33.5,33 " +
        "L32,27.5 C34,26.5 35,25 35,22 C35,18 36,13 35,9 C34,4 28,2 20,2 Z");

    /// <summary>نیش (کانین) — تاج نوک‌دار و یک ریشهٔ بلند مرکزی</summary>
    private static readonly Geometry Canine = Geometry.Parse(
        "M20,2.5 C17,3 13,5 11,9 L5,22 C4,25.5 6,27.5 8.5,28 " +
        "L13,28.5 C13.5,28.5 14,29.5 14,31 L15,46 C15.2,49.5 18,51 20,51 " +
        "C22,51 24.8,49.5 25,46 L26,31 C26,29.5 26.5,28.5 27,28.5 " +
        "L31.5,28 C34,27.5 36,25.5 35,22 L29,9 C27,5 23,3 20,2.5 Z");

    /// <summary>پیش‌دندان (مرکزی/کناری) — تاج تخت و باریک با یک ریشه</summary>
    private static readonly Geometry Incisor = Geometry.Parse(
        "M20,2.5 C14,2.5 10.5,4 9.5,8 L7,21 C6.5,25 8,27.5 10.5,28.3 " +
        "L14,28.6 C14.6,28.8 15,29.8 15.1,31.5 L15.8,45 C16,49 18,50.5 20,50.5 " +
        "C22,50.5 24,49 24.2,45 L24.9,31.5 C25,29.8 25.4,28.8 26,28.6 " +
        "L29.5,28.3 C32,27.5 33.5,25 33,21 L30.5,8 C29.5,4 26,2.5 20,2.5 Z");

    /// <summary>
    /// شمارهٔ FDI → هندسهٔ جنس دندان؛ ورودی نامعتبر (صفر/تهی) پیش‌دندان را
    /// برمی‌گرداند تا هیچ سلولی بی‌شکل نماند.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = value is int n ? n : 0;
        var position = number % 10;
        // تعداد ریشهٔ واقعی: آسیاهای بالا سه‌ریشه، آسیاهای پایین دوری‌شه،
        // آسیای کوچک اول بالا دوشاخه و بقیه تک‌ریشه‌اند — فرم دندان از روی
        // همین تعداد انتخاب می‌شود تا شکل، آناتومی واقعی را نشان دهد.
        var roots = RootCount(number);
        return position switch
        {
            1 or 2 => Incisor,
            3 => Canine,
            4 or 5 => roots == 2 ? PremolarTwoRoot : Premolar,
            _ => roots >= 3 ? MolarThreeRoot : Molar
        };
    }

    /// <summary>تعداد ریشهٔ استاندارد هر دندان FDI (ربع ۱ و ۲ = فک بالا)</summary>
    internal static int RootCount(int number)
    {
        var position = number % 10;
        var upper = number / 10 is 1 or 2;
        return position switch
        {
            >= 6 => upper ? 3 : 2, // آسیاها: بالا سه‌ریشه، پایین دوری‌شه
            4 => upper ? 2 : 1,    // آسیای کوچک اول بالا دوشاخه است
            _ => 1
        };
    }

    /// <summary>
    /// تعداد کانال استاندارد هر دندان FDI — در درمان ریشه راهنمای دندانپزشک:
    /// آسیای بزرگ اول بالا ۴ کانال (مزیوباکال دوکاناله)، آسیای کوچک دوم بالا
    /// و آسیاهای پایین (به‌جز عقل) ریشهٔ کمتر ولی کانال بیشتر از ریشه دارند.
    /// </summary>
    internal static int CanalCount(int number)
    {
        var position = number % 10;
        var upper = number / 10 is 1 or 2;
        return position switch
        {
            6 => upper ? 4 : 3,
            7 => 3,
            8 => upper ? 3 : 2,
            4 => upper ? 2 : 1,
            5 => upper ? 2 : 1,
            _ => 1
        };
    }

    /// <summary>بدون پشتیبانی — مبدل یک‌طرفهٔ نمایشی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل «رنگ خوانا برای شمارهٔ دندان» — چرخهٔ ۴۸: رنگ متن شماره بر اساس رنگ
/// وضعیت دندان انتخاب می‌شود تا روی هر هفت رنگ وضعیت (سفیدِ سالم تا سرمه‌ایِ
/// کشیده‌شده) خوانا بماند: روی رنگ‌های روشن (سالم/ترمیم) متن تیرهٔ سرمه‌ای و
/// روی بقیهٔ رنگ‌ها متن سفید. ورودی همان رشتهٔ هگز ToothVM.Color است.
/// </summary>
public sealed class ToothNumberBrushConverter : IValueConverter
{
    /// <summary>رنگ‌های وضعیتِ روشن — متنِ تیره روی آن‌ها خواناست</summary>
    private static readonly HashSet<string> LightFills =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "#FFFFFF", // سالم
            "#FDD835", // ترمیم‌شده — زرد روشن
        };

    /// <summary>قلم تیرهٔ سرمه‌ای برای زمینه‌های روشن — یکتا و منجمد</summary>
    private static readonly Brush DarkBrush = Frozen("#263238");

    /// <summary>قلم سفید برای زمینه‌های تیره — یکتا و منجمد</summary>
    private static readonly Brush LightBrush = Frozen("#FFFFFF");

    /// <summary>هگز وضعیت → قلم خوانا (تیرهٔ سرمه‌ای یا سفید)</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value as string;
        return LightFills.Contains(hex?.Trim() ?? string.Empty) ? DarkBrush : LightBrush;
    }

    /// <summary>بدون پشتیبانی — مبدل یک‌طرفهٔ نمایشی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    /// <summary>ساخت SolidColorBrush منجمد از هگز — قلم ایستای سبک و بی‌نشتی</summary>
    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
