# -*- coding: utf-8 -*-
"""گام U3 — مبدل‌های جدید + ثبت ناوبری و DI و DataTemplate"""
import io

ROOT = "/home/z/my-project/Sahand_ClinicPro"
DT = f"{ROOT}/src/Sahand.ClinicPro.Desktop"

# ═══ 1) مبدل‌های جدید ═══
CONVERTERS = '''using System.Globalization;
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
'''

with io.open(f"{DT}/Converters/UiConverters.cs", "w", encoding="utf-8-sig", newline="\n") as f:
    f.write(CONVERTERS)
print("OK UiConverters.cs (۵ مبدل)")

# ═══ 2) ثبت در App.xaml ═══
APP_XAML = f"{DT}/App.xaml"
with io.open(APP_XAML, encoding="utf-8-sig") as f:
    src = f.read()

if "Base64ToImage" not in src:
    # افزودن مبدل‌ها بعد از آخرین مبدل موجود
    anchor = '<converters:BoolToVisibilityConverter x:Key="BoolToVisibility" />'
    if anchor in src:
        add = anchor + '''
            <converters:Base64ToImageConverter x:Key="Base64ToImage" />
            <converters:BoolToTextConverter x:Key="BoolToText" />
            <converters:NullToBoolConverter x:Key="NullToBool" />
            <converters:ZeroToCollapsedConverter x:Key="ZeroToCollapsed" />
            <converters:StringToVisibilityConverter x:Key="StringToVisibility" />'''
        src = src.replace(anchor, add, 1)
        with io.open(APP_XAML, "w", encoding="utf-8-sig", newline="\n") as f:
            f.write(src)
        print("OK App.xaml +5 مبدل")
    else:
        print("!! لنگر App.xaml پیدا نشد")
else:
    print("SKIP App.xaml")

# ═══ 3) DI در App.xaml.cs ═══
APP_CS = f"{DT}/App.xaml.cs"
with io.open(APP_CS, encoding="utf-8-sig") as f:
    src = f.read()

if "DoctorsViewModel" not in src:
    anchor = "services.AddTransient<PatientsViewModel>();"
    if anchor in src:
        src = src.replace(anchor, anchor + "\n        services.AddTransient<DoctorsViewModel>();", 1)
        with io.open(APP_CS, "w", encoding="utf-8-sig", newline="\n") as f:
            f.write(src)
        print("OK App.xaml.cs DI")
    else:
        print("!! لنگر App.xaml.cs پیدا نشد")
else:
    print("SKIP App.xaml.cs")

# ═══ 4) MainWindowViewModel — صفحه و ناوبری ═══
MWVM = f"{DT}/ViewModels/MainWindowViewModel.cs"
with io.open(MWVM, encoding="utf-8-sig") as f:
    src = f.read()

if "DoctorsViewModel" not in src:
    # سازنده
    src = src.replace(
        "PatientsViewModel patients,",
        "PatientsViewModel patients,\n        DoctorsViewModel doctorsVm,", 1)
    # کارخانه صفحات
    src = src.replace(
        '["patients"] = () => patients,',
        '["patients"] = () => patients,\n            ["doctors"] = () => doctorsVm,', 1)
    # ناوبری — بعد از بیماران
    src = src.replace(
        'new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },',
        'new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },\n            new() { Title = "پزشکان", PageKey = "doctors", Icon = "Doctor" },', 1)
    with io.open(MWVM, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(src)
    print("OK MainWindowViewModel (صفحه + ناوبری)")
else:
    print("SKIP MainWindowViewModel")

# ═══ 5) MainWindow.xaml — DataTemplate ═══
MW = f"{DT}/MainWindow.xaml"
with io.open(MW, encoding="utf-8-sig") as f:
    src = f.read()

if "DoctorsView" not in src:
    anchor = """        <DataTemplate DataType="{x:Type vm:PatientsViewModel}">
            <views:PatientsView />
        </DataTemplate>"""
    if anchor in src:
        add = anchor + """
        <DataTemplate DataType="{x:Type vm:DoctorsViewModel}">
            <views:DoctorsView />
        </DataTemplate>"""
        src = src.replace(anchor, add, 1)
        with io.open(MW, "w", encoding="utf-8-sig", newline="\n") as f:
            f.write(src)
        print("OK MainWindow.xaml DataTemplate")
    else:
        print("!! لنگر MainWindow.xaml پیدا نشد")
else:
    print("SKIP MainWindow.xaml")
