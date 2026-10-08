using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل رنگ آیکون‌های منوی کناری — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
/// «در تنظیمات تم، آیکون‌ها عوض نمی‌شوند؛ می‌خواهم تمی باشد که آیکون‌های
/// زیبا و رنگی داشته باشد همانند آیکون‌های اسکرین‌شات‌ها».
/// ═══════════════════════════════════════════════════════════════════
/// • حالت «یکدست» (پیش‌فرض): همان رنگ زمینهٔ منو (سفید) — رفتار پیشین
/// • حالت «رنگی»: هر آیتم منو رنگ اختصاصی خودش را از پالت شاداب می‌گیرد؛
///   نگاشت رنگ بر اساس نام آیکون (Kind) پایدار است تا آیکون ثابت بماند
/// • حالت از منبع پویای App.IconColorMode خوانده می‌شود (ThemeService)؛
///   تعویض حالت در تنظیمات بی‌درنگ روی منو اعمال می‌شود.
/// </summary>
public sealed class IconTintConverter : IValueConverter
{
    /// <summary>
    /// پالت رنگی شاداب — ۱۲ رنگ متنوع با اشباع بالا برای آیکون‌های منو؛
    /// انتخاب رنگ بر اساس هش پایدار نام آیکون است (هر آیکون همیشه همان رنگ)
    /// </summary>
    private static readonly string[] Palette =
    {
        "#42A5F5", // آبی روشن
        "#EF5350", // قرمز مرجانی
        "#66BB6A", // سبز
        "#FFA726", // نارنجی
        "#AB47BC", // بنفش
        "#26C6DA", // فیروزه‌ای
        "#FFD54F", // زرد طلایی
        "#EC407A", // صورتی
        "#8D6E63", // قهوه‌ای
        "#7E57C2", // نیلی روشن
        "#26A69A", // سبزآبی
        "#5C6BC0", // نیلی
    };

    /// <summary>
    /// نام آیکون → رنگ؛ حالت رنگی خاموش بود؟ رنگ پیش‌فرض منو (سفید)
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // حالت رنگ آیکون‌ها از منابع برنامه (ThemeService مقدارش را می‌نویسد)؛
        // نبودِ مقدار = یکدست (پیش‌فرض امن)
        var mode = System.Windows.Application.Current?.TryFindResource("App.IconColorMode") as string ?? "Mono";
        var colorful = string.Equals(mode, "Colorful", StringComparison.OrdinalIgnoreCase);

        // در حالت یکدست، رنگ پایهٔ زمینهٔ منو (سفید) — parameter می‌تواند رنگ جایگزین بدهد
        var fallback = parameter as string ?? "#FFFFFF";
        if (!colorful)
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));

        // رنگ اختصاصی از پالت — هش پایدار نام آیکون؛ براق/فریز برای کارایی
        var key = value?.ToString() ?? string.Empty;
        var hash = 0;
        foreach (var ch in key)
            hash = (hash * 31 + ch) & 0x7FFFFFFF;
        var hex = Palette[hash % Palette.Length];
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    /// <summary>بدون پشتیبانی — فقط نمایشی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// نسخهٔ چندمقداری مبدل رنگ آیکون — چرخه ۲۴: مقدار نخست = نام آیکون (Kind)،
/// مقدار دوم = شمارندهٔ نوسازی (IconRefreshTick از MainWindowViewModel)؛ تغییر
/// شمارنده اتصال را دوباره ارزیابی می‌کند تا تعویض حالت رنگی/یکدست بی‌درنگ
/// روی منو بنشیند — بدون ری‌استارت برنامه.
/// </summary>
public sealed class IconTintMultiConverter : IMultiValueConverter
{
    /// <summary>مقدارهای [نام آیکون، شمارنده] → رنگ آیکون</summary>
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        => new IconTintConverter().Convert(
            values is { Length: > 0 } ? values[0] : null,
            typeof(Brush), parameter, culture);

    /// <summary>بدون پشتیبانی</summary>
    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => Array.Empty<object?>();
}
