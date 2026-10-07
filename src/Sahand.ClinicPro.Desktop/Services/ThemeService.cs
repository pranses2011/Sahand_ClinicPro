using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس تم — چرخه نوزدهم (گزارش سیزدهم مالک):
/// علاوه بر روشن/تیره، هشت پالت رنگی با آیکون‌های متفاوت از تنظیمات
/// قابل انتخاب و بلافاصله اعمال می‌شود؛ انتخاب در سیستم و به‌صورت محلی
/// (برای اعمال پیش از ورود) ذخیره می‌شود.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    private bool _isDark;
    private string _currentThemeKey = Blue.Key;

    /// <summary>تم پیش‌فرض محصول — آبی</summary>
    public static readonly AppTheme Blue = new("Blue", "آبی آسمانی", "💙", "#1565C0", "#00838F");

    /// <summary>کاتالوگ تم‌ها — ترتیب نمایش در تنظیمات</summary>
    public static readonly IReadOnlyList<AppTheme> Catalog = new[]
    {
        Blue,
        new AppTheme("Teal", "فیروزه‌ای", "🩵", "#00695C", "#26A69A"),
        new AppTheme("Green", "سبز", "💚", "#2E7D32", "#7CB342"),
        new AppTheme("Purple", "بنفش", "💜", "#6A1B9A", "#8E24AA"),
        new AppTheme("Indigo", "نیلی", "🫐", "#283593", "#5C6BC0"),
        new AppTheme("Crimson", "زرشکی", "❤️", "#C62828", "#EF5350"),
        new AppTheme("Orange", "نارنجی", "🧡", "#E65100", "#FB8C00"),
        new AppTheme("Pink", "صورتی", "💗", "#AD1457", "#EC407A"),
    };

    /// <summary>تم فعلی تیره است؟</summary>
    public bool IsDarkTheme => _isDark;

    /// <summary>کلید تم رنگی فعلی</summary>
    public string CurrentThemeKey => _currentThemeKey;

    /// <summary>کاتالوگ عمومی</summary>
    public IReadOnlyList<AppTheme> Themes => Catalog;

    /// <summary>رویداد تغییر تم</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>تغییر تم روشن و تیره — پالت رنگی حفظ می‌شود</summary>
    public void ToggleTheme() => ApplyTheme(_currentThemeKey, !_isDark);

    /// <summary>اعمال تم روشن/تیره — پالت رنگی حفظ می‌شود</summary>
    public void SetTheme(bool dark) => ApplyTheme(_currentThemeKey, dark);

    /// <summary>اعمال تم رنگی مشخص + حالت روشن/تیره</summary>
    public void ApplyTheme(string themeKey, bool dark)
    {
        var theme = _paletteHelper.GetTheme();

        // حالت پایه — روشن یا تیره
        theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);

        // پالت رنگی — کلید نامعتبر به پیش‌فرض آبی برمی‌گردد
        var picked = Catalog.FirstOrDefault(t =>
            string.Equals(t.Key, themeKey, StringComparison.OrdinalIgnoreCase)) ?? Blue;
        theme.SetPrimaryColor((Color)ColorConverter.ConvertFromString(picked.PrimaryHex));
        theme.SetSecondaryColor((Color)ColorConverter.ConvertFromString(picked.SecondaryHex));

        _paletteHelper.SetTheme(theme);
        _isDark = dark;
        _currentThemeKey = picked.Key;

        // ذخیرهٔ محلی — برای اعمال تم در اجرای بعدی پیش از ورود کاربر
        try
        {
            LocalSettingsStore.Set("ThemeKey", picked.Key);
            LocalSettingsStore.Set("ThemeIsDark", dark ? "true" : "false");
        }
        catch
        {
            // ذخیرهٔ محلی نباید جریان برنامه را متوقف کند
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
