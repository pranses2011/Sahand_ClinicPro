using System.Globalization;
using System.Windows;
using System.Windows.Data;
// ═══ چرخهٔ سی‌وششم (گزارش مالک — رفع خطای ساخت CS0103/CS0246) ═══
// مبدل HexBrushConverter (چرخهٔ سی‌وپنجم، رنگ کادر پیام‌های سرور لایسنس) از
// انواع Brushes / Color / ColorConverter / SolidColorBrush استفاده می‌کند که
// همگی عضو فضای‌نام System.Windows.Media هستند؛ این using در آن چرخه جا
// افتاده بود. این فایل تا پیش از چرخهٔ سی‌وپنجم هیچ نوعِ رسانه‌ای مصرف
// نمی‌کرد و به همین دلیل خطا تا مرحلهٔ Rebuild مالک آشکار نشد — نبودِ آن
// پروژهٔ دسکتاپ را می‌شکند و در زنجیره‌اش پروژهٔ تست هم (CS0006) از آب می‌افتد.
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل تاریخ میلادی به شمسی برای نمایش در جدول‌ها — «۱۴۰۴/۰۴/۰۱»
/// ورودی پذیرفته‌شده: DateTime یا DateTime? یا رشته تاریخ
/// </summary>
public sealed class ShamsiDateConverter : IValueConverter
{
    /// <summary>تبدیل به شمسی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateTime dt => PersianDateHelper.ToShamsi(dt),
            DateTimeOffset dto => PersianDateHelper.ToShamsi(dto.DateTime),
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var parsed)
                => PersianDateHelper.ToShamsi(parsed),
            _ => "—",
        };
    }

    /// <summary>تبدیل معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل تاریخ + ساعت شمسی — «۱۴۰۴/۰۴/۰۱ ۱۰:۳۰»
/// </summary>
public sealed class ShamsiDateTimeConverter : IValueConverter
{
    /// <summary>تبدیل به شمسی با ساعت</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dt ? PersianDateHelper.ToShamsiWithTime(dt) : "—";

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل تهی بودن به دیدپذیری — برای EmptyState و عناصر اختیاری
/// پارامتر «Inverse» حالت را وارونه می‌کند
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    /// <summary>تهی یا صفر یا خالی → مخفی؛ مقدار → نمایان (یا وارونه با پارامتر)</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null
                     || (value is string s && string.IsNullOrWhiteSpace(s))
                     || (value is System.Collections.ICollection c && c.Count == 0)
                     || (value is int i && i == 0);
        var inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);
        var visible = inverse ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل بولی به دیدپذیری — False → Collapsed
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>True → نمایان؛ False → مخفی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل مبلغ ریالی با جداکنندهٔ هزارگان — چرخه بیستم (گزارش چهاردهم مالک):
/// حالا از کمکی Money می‌گذرد تا (۱) جداکنندهٔ ۳ رقمی همه‌جا یکدست باشد و
/// (۲) واحد پول نمایشی (ریال/تومان) از تنظیمات اعمال شود — صرفاً نمایش.
/// </summary>
public sealed class CurrencyRialConverter : IValueConverter
{
    /// <summary>قالب‌بندی مبلغ — decimal/long/int/double</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal m => Sahand.ClinicPro.Desktop.Helpers.Money.Format(m),
            long l => Sahand.ClinicPro.Desktop.Helpers.Money.Format(l),
            int i => Sahand.ClinicPro.Desktop.Helpers.Money.Format(i),
            double d => Sahand.ClinicPro.Desktop.Helpers.Money.Format(System.Convert.ToDecimal(d)),
            _ => "0",
        };

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل مبلغ همراه واحد پول نمایشی — «۱,۲۳۴,۵۶۷ ریال» — چرخه بیستم
/// </summary>
public sealed class MoneyWithUnitConverter : IValueConverter
{
    /// <summary>قالب‌بندی مبلغ + واحد پول</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal m => Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(m),
            long l => Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(l),
            int i => Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(i),
            double d => Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(System.Convert.ToDecimal(d)),
            _ => "0",
        };

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل معکوس‌کننده منطقی به دیدپذیری — True → مخفی، False → نمایان (گام U14 برای ارسال تکی/دسته‌ای)
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <summary>معکوس تبدیل منطقی به دیدپذیری</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>بدون پشتیبانی</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل شاخص بخش به دیدپذیری — پارامتر شماره بخش (گام U15 سایدبار تنظیمات)
/// </summary>
public sealed class SectionToVisibilityConverter : IValueConverter
{
    /// <summary>مقایسه شاخص با پارامتر</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index && parameter is string str && int.TryParse(str, out var target) && index == target
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <summary>بدون پشتیبانی</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل سراسری ارقام فارسی — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «تمامی اعداد داخل برنامه بدون استثنا اعداد فارسی باشند».
/// هر متنی که از این مبدل بگذرد ارقام لاتینش به فارسی تبدیل می‌شود؛
/// نویسه‌های غیررقمی (حروف، جداکننده‌ها، خط تیره و …) دست‌نخورده می‌مانند،
/// از این‌رو اعمالش روی ستون‌های متنی هم بی‌ضرر است.
/// ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) — دوسویه شد ═══
/// «در فیلد ظرفیت [برنامهٔ کاری] هر مقداری که مینویسم اعمال نمیشه و با ترک
/// سلول، مقدار پاک میشه»: ConvertBack پیشتر همیشه Binding.DoNothing بود و
/// ویرایش ستون‌های دوسویهٔ دارای این مبدل (ظرفیت برنامهٔ کاری پزشک و
/// بدهکار/بستانکار/شرح سطرهای سند حسابداری) هرگز به منبع نمی‌رسید.
/// اکنون برگشت، ارقام فارسی/عربی را به لاتین برمی‌گرداند تا موتور اتصال
/// مقدار را به نوع مقصد (int?/decimal/رشته) تبدیل کند.
/// </summary>
public sealed class PersianDigitsConverter : IValueConverter
{
    /// <summary>
    /// تبدیل ارقام لاتین متن به فارسی — چرخهٔ چهل‌وهفتم (گزارش مالک):
    /// «در فیلد ظرفیت [برنامهٔ کاری پزشک] هر مقداری که مینویسم اعمال نمیشه و
    /// با ترک سلول، مقدار پاک میشه».
    /// ریشه: Convert فقط «رشته» را می‌پذیرفت؛ مقدار عددی (مثل int? MaxPatients)
    /// از مسیر value ?? string.Empty خام رد می‌شد و مهم‌تر از آن ConvertBack
    /// همیشه Binding.DoNothing برمی‌گرداند ⇒ هر ویرایشی در ستون‌های دوسویهٔ
    /// دارای این مبدل (ظرفیت برنامهٔ کاری، بدهکار/بستانکار/شرح سطرهای سند
    /// حسابداری) هرگز به منبع نمی‌رسید و سلول پس از ویرایش به مقدار پیشین
    /// (تهی) برمی‌گشت — دقیقاً «پاک شدن با ترک سلول».
    /// اکنون: ① نمایش — هر مقدار غیرتهی به متن تبدیل و ارقامش فارسی می‌شود؛
    /// ② برگشت — ارقام فارسی/عربیِ تایپ‌شده به لاتین برگردانده می‌شود تا
    /// موتور اتصال WPF آن را به نوع مقصد (int?/decimal/رشته) تبدیل کند.
    /// تهی و رشتهٔ خالی عيناً عبور داده می‌شوند تا TargetNullValue کار کند.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null
            ? string.Empty
            : Money.ToPersianDigits(value.ToString() ?? string.Empty);

    /// <summary>
    /// برگشت دوسویه — ارقام فارسی/عربی به لاتین (با Money.PersianDigitsToLatin)
    /// تا ویرایش سلول‌ها واقعاً در منبع ثبت شود؛ خالی = تهی می‌ماند.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s)
            return value ?? Binding.DoNothing;
        return string.IsNullOrWhiteSpace(s) ? s : Money.PersianDigitsToLatin(s);
    }
}

/// <summary>
/// مبدل bool → متن فارسی «فعال / غیرفعال» — چرخه بیست‌وسوم (گزارش شانزدهم
/// مالک): ستون وضعیت جدول‌های بیمه/تعرفهٔ صفحهٔ بیمه؛ برچسب‌ها فارسی.
/// </summary>
public sealed class BoolToFaTextConverter : IValueConverter
{
    /// <summary>true → فعال | false → غیرفعال | null → —</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            true => "فعال",
            false => "غیرفعال",
            _ => "—"
        };

    /// <summary>برگشت بی‌اثر — ستون فقط‌خواندنی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// ═══ چرخهٔ سی‌وپنجم (گزارش مالک) — مبدل رشتهٔ هگز رنگ به براش WPF ═══
/// «رنگ کادر پیام‌های سرور لایسنس با رنگ انتخابی در سرور فرق دارد»:
/// پیام‌های پشتیبانی داشبورد رنگشان از پنل لایسنس می‌آید (رشته‌هایی مثل
/// «#E53935» یا «E53935» یا رنگ‌های نامی «OrangeRed»). این مبدل رشته را به
/// SolidColorBrush تبدیل می‌کند و با پارامتر می‌توان حالت «ملایم» گرفت:
///   ConverterParameter=soft  → همان رنگ با آلفای ~۱۴٪ برای پس‌زمینهٔ کادر
///   (پیش‌فرض بدون پارامتر)   → رنگ توپر برای حاشیه/نوار
/// رشتهٔ نامعتبر → براش خنثی (بدون کرش، بدون رنگ عجیب).
/// قالب‌بندی یخ‌زدن (Freeze) انجام می‌شود تا رندر سبک‌تر باشد.
/// </summary>
public sealed class HexBrushConverter : IValueConverter
{
    /// <summary>رشتهٔ رنگ → براش؛ پارامتر «soft» رنگ را برای پس‌زمینه ملایم می‌کند</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var raw = value as string;
        if (string.IsNullOrWhiteSpace(raw))
            return Brushes.Transparent;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(raw.Trim());
            // حالت ملایم — پس‌زمینهٔ کادر: همان رنگ ولی تقریباً شفاف (۱۴٪ آلفا)
            if (string.Equals(parameter as string, "soft", StringComparison.OrdinalIgnoreCase))
                color.A = 36; // ~۱۴٪ شفافیت
            // ═══ چرخهٔ سی‌وهفتم (گزارش مالک — «کادر پیام تیره است») ═══
            // حالت «tint»: ترکیبِ کدر به‌سفید (۱۳٪ رنگ + ۸۷٪ سفید) — برخلاف
            // «soft» که آلفا دارد و رنگش به پس‌زمینهٔ پشتش وابسته است (در تم
            // تیره/پس‌زمینهٔ رنگی تیره دیده می‌شد)، tint همیشه و در هر جایی
            // «روشن» است چون رنگ نهایی از پیش با سفید مخلوط شده است.
            else if (string.Equals(parameter as string, "tint", StringComparison.OrdinalIgnoreCase))
            {
                const double K = 0.13; // سهم رنگ — بقیه سفید
                color = Color.FromRgb(
                    (byte)Math.Round(255 + (color.R - 255) * K),
                    (byte)Math.Round(255 + (color.G - 255) * K),
                    (byte)Math.Round(255 + (color.B - 255) * K));
            }
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            // نام/قالب رنگ نامعتبر — کادر بی‌رنگ می‌ماند تا پیام در هر حالت خوانا بماند
            return Brushes.Transparent;
        }
    }

    /// <summary>برگشت بی‌اثر — رنگ فقط از سمت سرور می‌آید</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

// ═══ چرخهٔ سی‌ونه‌م (گزارش مالک — لوازم مصرفی) ═══ مبدل «فهرست خالی» برای
// راهنماهای حالت خالی: اگر مقدارِ همبسته یک مجموعه (ObservableCollection) با
// صفر عنصر باشد، نمایان می‌شود — مثلاً راهنمای «هیچ قلم مصرفی‌ای موجودی
// ندارد…» در فرم پذیرش فقط وقتی انبار مصرفی خالی است دیده می‌شود.
/// <summary>
/// مبدل فهرست خالی به دیدپذیری — مجموعهٔ بدون عنصر = نمایان (راهنمای حالت خالی)
/// </summary>
public sealed class EmptyListToVisibilityConverter : IValueConverter
{
    /// <summary>تبدیل — شمار عناصر مجموعه (یا طول رشته) صفر باشد نمایان</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            System.Collections.ICollection col => col.Count,
            // چرخهٔ چهلم — رفع CS8510: بازوی «string s => s.Length» حذف شد؛ رشته
            // خودش System.Collections.IEnumerable را پیاده می‌کند و همین بازوی
            // زیر آن را می‌بلعد، پس بازوی رشته‌ای هرگز دسترس‌پذیر نبود (شمارش
            // کاراکترهای رشته از همین مسیر همان Length را می‌دهد — رفتار یکسان).
            System.Collections.IEnumerable en => en.Cast<object?>().Count(),
            null => 0,
            _ => 1
        };
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>برگشت — در اتصالهای یک‌طرفه استفاده نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
