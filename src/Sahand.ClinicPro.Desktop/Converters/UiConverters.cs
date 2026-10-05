using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل رشته Base64 به تصویر — برای امضا و مهر پزشک (گام U3)
/// </summary>
public sealed class Base64ToImageConverter : IValueConverter
{
    /// <summary>تبدیل Base64 به BitmapImage — تهی یعنی بدون تصویر</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string base64 || string.IsNullOrWhiteSpace(base64))
            return null;
        try
        {
            var bytes = System.Convert.FromBase64String(base64);
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل بولی به متن فارسی فعال/غیرفعال — برای ستون وضعیت
/// </summary>
public sealed class BoolToTextConverter : IValueConverter
{
    /// <summary>تبدیل</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value is true;
        if (parameter as string == "YesNo")
            return state ? "بله" : "خیر";
        return state ? "فعال" : "غیرفعال";
    }

    /// <summary>تبدیل معکوس متن فارسی به بولی</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s is "فعال" or "بله";
}

/// <summary>
/// مبدل تهی‌نبودن به بولی — برای فعال‌سازی کنترل وابسته به مقدار
/// </summary>
public sealed class NullToBoolConverter : IValueConverter
{
    /// <summary>مقدار → صحیح؛ تهی/خالی → نادرست</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && !(value is string s && string.IsNullOrWhiteSpace(s));

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل عدد صفر به مخفی — برای Badge های شرطی مثل نوبت‌های درگیر
/// </summary>
public sealed class ZeroToCollapsedConverter : IValueConverter
{
    /// <summary>صفر → مخفی؛ غیرصفر → نمایان</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل متن غیرخالی به دیدپذیری — برای پیام خطای فرم
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    /// <summary>متن → نمایان؛ خالی → مخفی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل معکوس‌کننده مقدار منطقی — برای غیرفعال‌سازی فیلدها در حالت ویرایش (گام U13)
/// </summary>
public sealed class InvertBoolConverter : IValueConverter
{
    /// <summary>تبدیل true به false و بالعکس</summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    /// <summary>تبدیل معکوس</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
