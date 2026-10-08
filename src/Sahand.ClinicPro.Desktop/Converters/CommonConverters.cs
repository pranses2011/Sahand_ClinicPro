using System.Globalization;
using System.Windows;
using System.Windows.Data;
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
/// این مبدل یک‌ره رو (OneWay) است؛ ConvertBack مقدار را برمی‌گرداند چون
/// ورودی کاربر جداگانه (MoneyInput/پارس ارقام فارسی) هندل می‌شود.
/// </summary>
public sealed class PersianDigitsConverter : IValueConverter
{
    /// <summary>تبدیل ارقام لاتین متن به فارسی — تهی به تهی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? Money.ToPersianDigits(s) : (object?)value ?? string.Empty;

    /// <summary>برگشت بی‌اثر — ورودی‌ها جداگانه ارقام فارسی را می‌پذیرند</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
