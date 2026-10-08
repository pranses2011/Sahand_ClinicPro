using System.Windows;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس تم — چرخه نوزدهم + بازآرایی چرخه بیستم (گزارش چهاردهم مالک).
/// وظایف:
///   ۱) روشن/تیره + هشت پالت رنگی (چرخه ۱۹) — بی‌درنگ روی کل برنامه
///   ۲) ظاهر عناصر هر تم: شعاع گوشهٔ کارت‌ها و چگالی فشرده/استاندارد
///      از طریق منابع پویای App.CornerRadius و App.CardPadding
///   ۳) تنظیمات ظاهری سراسری (فونت/اندازهٔ پایه/انیمیشن/واحد پول) —
///      قبلاً این تنظیمات فقط در دیتابیس ذخیره می‌شدند و هیچ اثری
///      نداشتند؛ اکنون با منابع پویای App.FontFamily و App.BaseFontSize
///      روی همهٔ پنجره‌ها (سبک ضمنی Window) اعمال می‌شوند.
/// انتخاب‌ها هم به‌صورت محلی (LocalSettingsStore — برای اجرای بعدی
/// پیش از ورود) و هم در سرور (دستهٔ Theme/Currency تنظیمات) ذخیره می‌شوند.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    private bool _isDark;
    private string _currentThemeKey = Blue.Key;
    private AppearanceSettings _appearance = new();

    /// <summary>تم پیش‌فرض محصول — آبی</summary>
    public static readonly AppTheme Blue = new("Blue", "آبی آسمانی", "💙", "#1565C0", "#00838F", 8, "Normal");

    /// <summary>
    /// کاتالوگ تم‌ها — چرخه بیستم: هر تم علاوه بر رنگ، گوشه و چگالی خودش را دارد
    /// تا «تعویض تم» واقعاً چهرهٔ عناصر برنامه را عوض کند، نه فقط رنگ را.
    /// ترتیب نمایش در تنظیمات همین ترتیب است.
    /// </summary>
    public static readonly IReadOnlyList<AppTheme> Catalog = new[]
    {
        Blue,
        new AppTheme("Teal", "فیروزه‌ای", "🩵", "#00695C", "#26A69A", 14, "Normal"),
        new AppTheme("Green", "سبز جنگلی", "💚", "#2E7D32", "#7CB342", 6, "Compact"),
        new AppTheme("Purple", "بنفش سلطنتی", "💜", "#6A1B9A", "#8E24AA", 18, "Normal"),
        new AppTheme("Indigo", "نیلی شبانه", "🫐", "#283593", "#5C6BC0", 10, "Compact"),
        new AppTheme("Crimson", "زرشکی", "❤️", "#C62828", "#EF5350", 4, "Normal"),
        new AppTheme("Orange", "نارنجی گرم", "🧡", "#E65100", "#FB8C00", 12, "Normal"),
        new AppTheme("Pink", "صورتی مهتابی", "💗", "#AD1457", "#EC407A", 16, "Compact"),
    };

    /// <summary>تم فعلی تیره است؟</summary>
    public bool IsDarkTheme => _isDark;

    /// <summary>کلید تم رنگی فعلی</summary>
    public string CurrentThemeKey => _currentThemeKey;

    /// <summary>تنظیمات ظاهری فعلی — فونت/اندازه/انیمیشن/واحد پول</summary>
    public AppearanceSettings Appearance => _appearance;

    /// <summary>کاتالوگ عمومی</summary>
    public IReadOnlyList<AppTheme> Themes => Catalog;

    /// <summary>رویداد تغییر تم رنگی/روشن-تیره</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>رویداد تغییر تنظیمات ظاهری (فونت/اندازه/واحد پول)</summary>
    public event EventHandler? AppearanceChanged;

    /// <summary>تغییر تم روشن و تیره — پالت رنگی حفظ می‌شود</summary>
    public void ToggleTheme() => ApplyTheme(_currentThemeKey, !_isDark);

    /// <summary>اعمال تم روشن/تاریک — پالت رنگی حفظ می‌شود</summary>
    public void SetTheme(bool dark) => ApplyTheme(_currentThemeKey, dark);

    /// <summary>
    /// اعمال تم رنگی مشخص + حالت روشن/تیره — رنگ‌های MaterialDesign،
    /// گوشه‌ها و چگالی کارت‌ها و ذخیرهٔ محلی برای اجرای بعدی.
    /// چرخه بیست‌ویکم (گزارش پانزدهم مالک): ۱) رنگ منوی کناری هم از رنگ تم
    /// ساخته می‌شود (پیش‌تر گرادیان هاردکد بود و تعویض تم منو را عوض نمی‌کرد)؛
    /// ۲) گوشه/چگالی اگر کاربر مقدار مستقل گذاشته باشد، مقدار کاربر مقدم است.
    /// </summary>
    public void ApplyTheme(string themeKey, bool dark)
    {
        var theme = _paletteHelper.GetTheme();

        // حالت پایه — روشن یا تیره
        theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);

        // پالت رنگی — کلید نامعتبر به پیش‌فرض آبی برمی‌گردد
        var picked = Catalog.FirstOrDefault(t =>
            string.Equals(t.Key, themeKey, StringComparison.OrdinalIgnoreCase)) ?? Blue;
        theme.SetPrimaryColor(picked.PrimaryColor);
        theme.SetSecondaryColor((System.Windows.Media.Color)System.Windows.Media.ColorConverter
            .ConvertFromString(picked.SecondaryHex));

        _paletteHelper.SetTheme(theme);
        _isDark = dark;
        _currentThemeKey = picked.Key;

        // ═══ ظاهر عناصر تم — چرخه بیست + overrideهای مستقل چرخه بیست‌ویکم ═══
        var resources = System.Windows.Application.Current.Resources;

        // شعاع گوشه: مقدار مستقل کاربر مقدم است؛ وگرنه از تم
        var radius = _appearance.CardRadiusOverride ?? picked.CardRadius;
        resources["App.CardRadius"] = new CornerRadius(radius);

        // چگالی: مقدار مستقل کاربر مقدم است؛ وگرنه از تم
        var compact = _appearance.DensityOverride is not null
            ? string.Equals(_appearance.DensityOverride, "Compact", StringComparison.OrdinalIgnoreCase)
            : picked.IsCompact;
        resources["App.CardPadding"] = new Thickness(
            compact ? 12 : 20, compact ? 9 : 20, compact ? 12 : 20, compact ? 9 : 20);

        // ═══ رنگ منوی کناری — چرخه بیست‌ویکم ═══
        ApplySidebarColors(resources, picked);

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

    /// <summary>
    /// اعمال رنگ منوی کناری — منبع پویای App.SidebarBrush (چرخه بیست‌وسوم).
    /// اگر کاربر رنگ مستقل گذاشته باشد همان؛ وگرنه گرادیانی تیره از رنگ اصلی
    /// تم ساخته می‌شود تا منو همیشه با تم هماهنگ باشد (گزارش پانزدهم مالک:
    /// «تغییر رنگ، رنگ منوی کناری را عوض نمی‌کند»).
    /// نکتهٔ فنی (رفع ریشهٔ گزارش شانزدهم: «رنگ منو تا ری‌استارت عوض نمی‌شود»):
    /// قلم‌های تعریف‌شده در XAML توسط بهینه‌ساز BAML فریز (IsFrozen) می‌شوند؛
    /// تغییر رنگِ نمونهٔ فریزشده ناممکن و جایگزینی منبع هم با StaticResource
    /// دیده نمی‌شد. اکنون هر بار یک قلم گرادیانیِ نو (دو GradientStop از
    /// رنگ پایه) ساخته و «کل منبع» جایگزین می‌شود؛ سایدبار با DynamicResource
    /// بی‌درنگ تازه می‌شود — بدون ری‌استارت.
    /// </summary>
    private void ApplySidebarColors(
        System.Windows.ResourceDictionary resources, AppTheme picked)
    {
        System.Windows.Media.Color baseColor;
        var customHex = _appearance.SidebarHex;
        if (!string.IsNullOrWhiteSpace(customHex))
        {
            // رنگ سفارشی کاربر — مستقل از تم (رشتهٔ نامعتبر → بازگشت به رنگ تم)
            try
            {
                baseColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString(customHex);
            }
            catch
            {
                baseColor = picked.PrimaryColor;
            }
        }
        else
        {
            // همگام با تم — تیره‌شدهٔ رنگ اصلی
            baseColor = picked.PrimaryColor;
        }
        var top = Darken(baseColor, 0.30);   // بالای گرادیان — تیره‌تر
        var bottom = Darken(baseColor, 0.08); // پایین گرادیان — نزدیک رنگ تم

        // قلم گرادیانی نو — جایگزینی کامل منبع تا DynamicResource فوراً بگیردش
        // زاویهٔ ۹۰ درجه = عمودی از بالا (تیره‌تر) به پایین (روشن‌تر)
        var brush = new System.Windows.Media.LinearGradientBrush(top, bottom, 90d);
        resources["App.SidebarBrush"] = brush;
    }

    /// <summary>
    /// تیره‌سازی رنگ برای گرادیان منو — رنگ ورودی با ضریب داده‌شده به سمت سیاه
    /// می‌رود (factor بین ۰ و ۱)؛ کانال آلفا دست‌نخورده می‌ماند.
    /// </summary>
    private static System.Windows.Media.Color Darken(System.Windows.Media.Color c, double factor)
    {
        factor = Math.Clamp(factor, 0, 0.9);
        return System.Windows.Media.Color.FromRgb(
            (byte)(c.R * (1 - factor)),
            (byte)(c.G * (1 - factor)),
            (byte)(c.B * (1 - factor)));
    }

    /// <summary>
    /// اعمال تنظیمات ظاهری — چرخه بیستم (گزارش چهاردهم مالک):
    /// فونت و اندازهٔ پایه از طریق منابع پویا روی سبک ضمنی Window و همهٔ
    /// فرزندانش اثر می‌گذارد (نماهای Root فونت هاردکدشده ندارند)؛
    /// واحد پول نمایشی در سرویس Money خوانده می‌شود.
    /// چرخه بیست‌ویکم: رنگ منو/گوشه/چگالی مستقل هم همین‌جا اعمال می‌شوند تا
    /// تغییرشان بدون تعویض تم بی‌درنگ روی پوسته اثر کند.
    /// </summary>
    public void ApplyAppearance(AppearanceSettings settings)
    {
        _appearance = settings ?? new AppearanceSettings();

        var resources = System.Windows.Application.Current.Resources;

        // فونت — نام‌های شناخته‌شدهٔ ویندوز مستقیم؛ Vazirmatn جاسازی‌شده با pack URI
        resources["App.FontFamily"] = ResolveFontFamily(_appearance.FontFamilyName);

        // اندازهٔ پایهٔ متن — عناصر بدون اندازهٔ صریح ارث می‌برند
        resources["App.BaseFontSize"] = _appearance.BaseFontSize;

        // انیمیشن‌ها — منبع سراسری برای رفتارهای رابط
        resources["App.AnimationsEnabled"] = _appearance.AnimationsEnabled;

        // واحد پول نمایشی — سرویس Money در هر قالب‌بندی مبلغ می‌خواندش؛
        // برچسب متن هم برای هدرها/جمع‌ها منبع پویا می‌شود
        resources["App.CurrencyLabel"] = _appearance.CurrencyLabel;
        Helpers.Money.CurrencyUnit = _appearance.CurrencyUnit;

        // ═══ سفارشی‌سازی تفکیکی عناصر — چرخه بیست‌ویکم ═══
        // گوشه و چگالی مستقل — اگر تهی باشند مقدار تم فعلی باقی می‌ماند
        if (_appearance.CardRadiusOverride is { } radius)
            resources["App.CardRadius"] = new CornerRadius(radius);
        if (_appearance.DensityOverride is not null)
        {
            var compact = string.Equals(_appearance.DensityOverride, "Compact", StringComparison.OrdinalIgnoreCase);
            resources["App.CardPadding"] = new Thickness(
                compact ? 12 : 20, compact ? 9 : 20, compact ? 12 : 20, compact ? 9 : 20);
        }
        // رنگ منوی کناری — بازاعمال بی‌درنگ (سفارشی یا همگام با تم)
        var picked = Catalog.FirstOrDefault(t =>
            string.Equals(t.Key, _currentThemeKey, StringComparison.OrdinalIgnoreCase)) ?? Blue;
        ApplySidebarColors(resources, picked);

        try
        {
            LocalSettingsStore.Set("AppFont", _appearance.FontFamilyName);
            LocalSettingsStore.Set("AppBaseFontSize", _appearance.BaseFontSize.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
            LocalSettingsStore.Set("AppAnimations", _appearance.AnimationsEnabled ? "true" : "false");
            LocalSettingsStore.Set("CurrencyUnit", _appearance.CurrencyUnit);
            // چرخه بیست‌ویکم: سه تنظیم مستقل جدید
            LocalSettingsStore.Set("SidebarHex", _appearance.SidebarHex ?? "");
            LocalSettingsStore.Set("CardRadiusOverride",
                _appearance.CardRadiusOverride is { } r2 ? r2.ToString() : "");
            LocalSettingsStore.Set("DensityOverride", _appearance.DensityOverride ?? "");
        }
        catch
        {
            // ذخیرهٔ محلی نباید جریان برنامه را متوقف کند
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// خواندن تنظیمات ظاهری ذخیره‌شدهٔ محلی — برای اعمال در راه‌اندازی
    /// برنامه پیش از ساخت پنجرهٔ اصلی؛ در نبود مقدار، پیش‌فرض محصول.
    /// چرخه بیست‌ویکم: سه تنظیم مستقل (رنگ منو/گوشه/چگالی) هم خوانده می‌شوند.
    /// </summary>
    public static AppearanceSettings LoadSavedAppearance()
    {
        try
        {
            var font = LocalSettingsStore.Get("AppFont");
            var size = LocalSettingsStore.Get("AppBaseFontSize");
            var anim = LocalSettingsStore.Get("AppAnimations");
            var currency = LocalSettingsStore.Get("CurrencyUnit");
            var sidebarHex = LocalSettingsStore.Get("SidebarHex");
            var radiusOvr = LocalSettingsStore.Get("CardRadiusOverride");
            var densityOvr = LocalSettingsStore.Get("DensityOverride");
            return new AppearanceSettings(
                string.IsNullOrWhiteSpace(font) ? "Vazirmatn" : font,
                double.TryParse(size, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var s) ? Math.Clamp(s, 11, 18) : 13,
                anim != "false",
                string.Equals(currency, "Toman", StringComparison.OrdinalIgnoreCase) ? "Toman" : "Rial",
                // رشتهٔ خالی یعنی «همگام با تم»
                string.IsNullOrWhiteSpace(sidebarHex) ? null : sidebarHex,
                int.TryParse(radiusOvr, out var r) && r is >= 0 and <= 24 ? r : null,
                string.IsNullOrWhiteSpace(densityOvr) ? null : densityOvr);
        }
        catch
        {
            return new AppearanceSettings();
        }
    }

    /// <summary>
    /// تبدیل نام فونت به FontFamily — Vazirmatn جاسازی‌شده در exe است؛
    /// بقیهٔ نام‌ها فونت‌های نصب‌شدهٔ ویندوز (Tahoma/Segoe UI/…)
    /// </summary>
    private static System.Windows.Media.FontFamily ResolveFontFamily(string? name)
        => string.IsNullOrWhiteSpace(name) || name == "Vazirmatn"
            ? new System.Windows.Media.FontFamily("pack://application:,,,/Assets/Fonts/#Vazirmatn")
            : new System.Windows.Media.FontFamily(name);
}
