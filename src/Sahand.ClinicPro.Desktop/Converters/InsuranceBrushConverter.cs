using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل رنگ ویژهٔ هر بیمه — چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک):
/// «در لیست بیماران، رنگ بیمه را درست نشان نمی‌دهد؛ همهٔ بیمه‌ها را با یک
/// رنگ نشان می‌دهد و پیدا کردن سخت می‌شود».
/// پیش‌تر تنها چهار بیمهٔ شناخته‌شدهٔ پیش‌فرض رنگ ثابت DataTrigger داشتند و
/// هر بیمهٔ تعریف‌شدهٔ جدیدِ درمانگاه (و آزاد) همه به یک خاکستر یکسان
/// می‌افتادند. اکنون «هر» بیمه — چه پیش‌فرض چه سفارشی — از پالت پایدار
/// رنگ می‌گیرد؛ رنگ بر اساس هشِ نام بیمه ثابت می‌ماند تا در نگاه اول
/// بیمارهای یک بیمه از هم تشخیص داده شوند (همان الگوی IconTintConverter
/// چرخه ۲۴).
/// </summary>
public class InsuranceBrushConverter : IValueConverter
{
    /// <summary>
    /// پالت ملایم پس‌زمینهٔ نشان (Badge) — ۱۲ رنگ متنوع با اشباع ملایم تا
    /// متن تیره روی همهٔ آن‌ها خوانا بماند؛ رنگ‌ها چرخه‌ای تکرار می‌شوند
    /// </summary>
    private static readonly string[] Palette =
    {
        "#BBDEFB", // آبی روشن — تامین اجتماعی
        "#C8E6C9", // سبز روشن — سلامت
        "#E1BEE7", // بنفش روشن — نظام وظیفه
        "#FFCCBC", // نارنجی روشن — نیروهای مسلح
        "#FFF9C4", // زرد روشن
        "#B2DFDB", // فیروزه‌ای روشن
        "#D1C4E9", // نیلی روشن
        "#FFCDD2", // صورتی روشن
        "#C5E1A5", // سبز لیمویی
        "#B0BEC5", // خاکستری آبی — آزاد/بدون بیمه
        "#FFE0B2", // کهربایی روشن
        "#DCEDC8", // سبز ملایم
    };

    /// <summary>
    /// نگاشت ثابت بیمه‌های شناخته‌شدهٔ سامانه به خانهٔ پالت — تا بیمه‌های
    /// پرتکرار همیشه همان رنگ شناخته‌شدهٔ قبلی را بگیرند (پایداری بصری)
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, int> Known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["تامین اجتماعی"] = 0,
            ["تأمین اجتماعی"] = 0,
            ["سلامت"] = 1,
            ["نظام وظیفه"] = 2,
            ["نیروهای مسلح"] = 3,
            ["آزاد"] = 9,
            ["آزاد (بدون بیمه)"] = 9,
        };

    /// <summary>نام بیمه → پس‌زمینهٔ رنگی متمایز</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            name = "آزاد"; // نبود بیمه = آزاد — همان رنگ خاکستری متمایز آزاد

        int index;
        if (!Known.TryGetValue(name.Trim(), out index))
        {
            // بیمهٔ سفارشی — هش پایدار نام → خانهٔ پالت؛ هرگز با بیمهٔ دیگر
            // تصادفی هم‌رنگ نمی‌شود مگر هش‌ها همان خانه را بگیرند
            var hash = 0;
            foreach (var ch in name)
                hash = (hash * 31 + ch) & 0x7FFFFFFF;
            index = 4 + hash % (Palette.Length - 4); // خانه‌های پس از بیمه‌های شناخته‌شده
        }

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Palette[index]));
        brush.Freeze();
        return brush;
    }

    /// <summary>بدون پشتیبانی — فقط نمایشی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
