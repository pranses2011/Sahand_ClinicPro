using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// انتخاب‌گر رنگ حرفه‌ای — چرخهٔ بیست‌ونهم (گزارش هجدهم مالک):
/// «توسعهٔ تنظیمات تم تک‌عنصری حرفه‌ای‌تر». به‌جای محدودیت پالت ثابتِ
/// کشویی برای رنگ منوی کناری و رنگ اصلی دکمه‌ها، هر رنگی با انتخاب‌گر
/// کامل انتخاب می‌شود: پالت آمادهٔ متریال (۱۹ خانواده × ۵ طیف) +
/// اسلایدرهای دقیق RGB + ورودی کد HEX با اعتبارسنجی برخط + پیش‌نمایش
/// زنده. ظاهر همان زبان طراحی ThemedDialog است (پوشش تیره + کارت تم).
/// </summary>
public partial class ColorPickerWindow : Window
{
    /// <summary>نتیجهٔ نهایی — کد هگز انتخاب‌شده؛ انصراف = تهی</summary>
    public string? SelectedHex { get; private set; }

    /// <summary>در حال همگام‌سازی کنترل‌ها از کد هستیم — رویدادها نادیده</summary>
    private bool _syncing;

    /// <summary>خانواده‌های رنگ پالت آماده — هر ردیف ۵ طیف روشن→تیره</summary>
    private static readonly string[][] Palette =
    {
        new[] { "#FFEBEE", "#EF5350", "#E53935", "#C62828", "#B71C1C" }, // قرمز
        new[] { "#FBE9E7", "#FF7043", "#F4511E", "#D84315", "#BF360C" }, // نارنجی عمیق
        new[] { "#FFF3E0", "#FFA726", "#FB8C00", "#EF6C00", "#E65100" }, // نارنجی
        new[] { "#FFFDE7", "#FDD835", "#F9A825", "#F57F17", "#E6A700" }, // زرد کهربایی
        new[] { "#F9FBE7", "#C0CA33", "#9E9D24", "#827717", "#827717" }, // زیتونی
        new[] { "#F1F8E9", "#7CB342", "#558B2F", "#33691E", "#1B5E20" }, // سبز روشن
        new[] { "#E8F5E9", "#66BB6A", "#43A047", "#2E7D32", "#1B5E20" }, // سبز
        new[] { "#E0F2F1", "#26A69A", "#00897B", "#00695C", "#004D40" }, // فیروزه‌ای
        new[] { "#E0F7FA", "#26C6DA", "#00ACC1", "#00838F", "#006064" }, // فیروزه‌ای روشن
        new[] { "#E3F2FD", "#42A5F5", "#1E88E5", "#1565C0", "#0D47A1" }, // آبی
        new[] { "#E8EAF6", "#5C6BC0", "#3949AB", "#283593", "#1A237E" }, // نیلی
        new[] { "#EDE7F6", "#7E57C2", "#5E35B1", "#4527A0", "#311B92" }, // بنفش عمیق
        new[] { "#F3E5F5", "#AB47BC", "#8E24AA", "#6A1B9A", "#4A148C" }, // بنفش
        new[] { "#FCE4EC", "#EC407A", "#D81B60", "#AD1457", "#880E4F" }, // صورتی
        new[] { "#EFEBE9", "#8D6E63", "#6D4C41", "#5D4037", "#3E2723" }, // قهوه‌ای
        new[] { "#FAFAFA", "#BDBDBD", "#757575", "#424242", "#212121" }, // خاکستری
        new[] { "#ECEFF1", "#78909C", "#546E7A", "#37474F", "#263238" }, // خاکستری آبی (گرافیت)
        new[] { "#E0E0E0", "#9E9E9E", "#616161", "#414141", "#111111" }, // مشکی مات
        new[] { "#FFFFFF", "#ECEFF1", "#CFD8DC", "#B0BEC5", "#90A4AE" }, // سفید نقره‌ای
    };

    /// <summary>سازنده — پالت ساخته و رنگ اولیه اعمال می‌شود</summary>
    public ColorPickerWindow(string? initialHex = null)
    {
        InitializeComponent();
        BuildPalette();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { SelectedHex = null; Close(); }
            else if (e.Key == Key.Enter) OnAcceptClick(this, e);
        };
        SetColor(initialHex is { Length: > 0 } && TryParseHex(initialHex, out var c) ? c : Color.FromRgb(0x15, 0x65, 0xC0));
    }

    /// <summary>
    /// نمایش مودال انتخاب‌گر — خروجی کد هگز انتخاب‌شده یا تهی برای انصراف.
    /// </summary>
    /// <param name="owner">پنجرهٔ مالک</param>
    /// <param name="title">عنوان — نام عنصرِ در حال تنظیم</param>
    /// <param name="initialHex">رنگ اولیه</param>
    public static string? Pick(Window? owner, string title, string? initialHex)
    {
        var dialog = new ColorPickerWindow(initialHex)
        {
            Owner = owner ?? (System.Windows.Application.Current?.Windows
                .OfType<Window>().FirstOrDefault(w => w.IsActive)),
        };
        dialog.TitleText.Text = title;
        dialog.ShowDialog();
        return dialog.SelectedHex;
    }

    /// <summary>ساخت پالت آماده — سطرهای افقی از مربع‌های کلیک‌خور</summary>
    private void BuildPalette()
    {
        foreach (var family in Palette)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            foreach (var hex in family)
            {
                var swatch = new Border
                {
                    Width = 52,
                    Height = 26,
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = Cursors.Hand,
                    Tag = hex,
                    ToolTip = hex,
                };
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                swatch.Background = brush;
                swatch.MouseLeftButtonDown += OnSwatchClick;
                row.Children.Add(swatch);
            }
            PaletteRows.Children.Add(row);
        }
    }

    /// <summary>کلیک روی مربع پالت — همان رنگ انتخاب و همگام می‌شود</summary>
    private void OnSwatchClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: string hex } && TryParseHex(hex, out var color))
            SetColor(color);
    }

    /// <summary>حرکت اسلایدرهای RGB — همگام‌سازی پیش‌نمایش و کد</summary>
    private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing) return;
        var color = Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
        ApplyColor(color, updateSliders: false);
        HexErrorText.Visibility = Visibility.Collapsed; // اسلایدر همیشه مقدار معتبر می‌سازد
    }

    /// <summary>تغییر متن کد HEX — اعتبارسنجی برخط و اعمال در صورت معتبر بودن</summary>
    private void HexInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        var text = HexInput.Text.Trim();
        if (TryParseHex(text, out var color))
        {
            HexErrorText.Visibility = Visibility.Collapsed;
            ApplyColor(color, updateSliders: true, updateHexInput: false);
        }
        else
        {
            HexErrorText.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>اعمال کامل یک رنگ روی همهٔ کنترل‌ها</summary>
    private void SetColor(Color color) => ApplyColor(color, updateSliders: true, updateHexInput: true);

    /// <summary>
    /// اعمال رنگ روی پیش‌نمایش/اسلایدرها/ورودی — بسته به منبع تغییر، بازنویسی
    /// همان کنترلی که رویداد را فراخوانده لازم نیست (مثلاً ورودی HEX نباید
    /// هنگام تایپ خودش بازنویسی شود که مکان‌نما بپرد).
    /// </summary>
    private void ApplyColor(Color color, bool updateSliders, bool updateHexInput = true)
    {
        _syncing = true;
        try
        {
            // پیش‌نمایش — پس‌زمینه + متن کد (سایه و متن اصلی)
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            PreviewBorder.Background = brush;
            var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            PreviewHexText.Text = hex;
            PreviewHexShadow.Text = hex;
            // متن پیش‌نمایش روی رنگ‌های روشن تیره و روی تیره روشن — کنتراست تضمین‌شده
            var luminance = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
            var textBrush = new SolidColorBrush(luminance > 140 ? Colors.Black : Colors.White);
            textBrush.Freeze();
            PreviewHexText.Foreground = textBrush;
            PreviewHexShadow.Foreground = textBrush;

            if (updateSliders)
            {
                RedSlider.Value = color.R;
                GreenSlider.Value = color.G;
                BlueSlider.Value = color.B;
            }
            RedValueText.Text = ((int)color.R).ToString();
            GreenValueText.Text = ((int)color.G).ToString();
            BlueValueText.Text = ((int)color.B).ToString();
            if (updateHexInput)
                HexInput.Text = hex;
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>تأیید — رنگ فعلی پیش‌نمایش نتیجه است (کد ورودی نامعتبر پذیرفته نمی‌شود)</summary>
    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        SelectedHex = PreviewHexText.Text;
        Close();
    }

    /// <summary>انصراف</summary>
    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        SelectedHex = null;
        Close();
    }

    /// <summary>کلیک روی پوشش تیره = انصراف (همان رفتار دیالوگ‌های استاندارد)</summary>
    private void OnDimClick(object sender, MouseButtonEventArgs e)
    {
        SelectedHex = null;
        Close();
    }

    /// <summary>تجزیهٔ کد HEX — با یا بدون #، ۶ رقمی</summary>
    private static bool TryParseHex(string text, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim().TrimStart('#');
        if (s.Length != 6) return false;
        try
        {
            color = (Color)ColorConverter.ConvertFromString("#" + s);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
