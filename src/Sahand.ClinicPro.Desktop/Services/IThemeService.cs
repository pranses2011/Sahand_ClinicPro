using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// تعریف یک تم برنامه — چرخه نوزدهم + گسترش چرخه بیستم (گزارش چهاردهم مالک).
/// چند تم با رنگ‌ها و آیکون‌های متفاوت که از تنظیمات قابل انتخاب است؛
/// چرخه بیستم: هر تم علاوه بر پالت رنگ، «ظاهر عناصر» را هم تعیین می‌کند —
/// شعاع گوشهٔ کارت‌ها و چگالی (Compact/Normal) — تا تعویض تم فقط تعویض
/// رنگ نباشد و چهرهٔ همهٔ عناصر برنامه عوض شود.
/// </summary>
/// <param name="Key">کلید ذخیره‌سازی</param>
/// <param name="Title">نام فارسی تم</param>
/// <param name="Icon">آیکون نمایشی</param>
/// <param name="PrimaryHex">رنگ اصلی</param>
/// <param name="SecondaryHex">رنگ ثانویه (تأکیدی)</param>
/// <param name="CardRadius">شعاع گوشهٔ کارت‌ها و پنل‌ها — پیکسل</param>
/// <param name="Density">چگالی عناصر — Compact (فشرده) یا Normal (استاندارد)</param>
public sealed record AppTheme(
    string Key,
    string Title,
    string Icon,
    string PrimaryHex,
    string SecondaryHex,
    int CardRadius = 8,
    string Density = "Normal")
{
    /// <summary>چگالی فشرده است؟ — برای پاس‌دادن به XAML بدون مبدل</summary>
    public bool IsCompact => string.Equals(Density, "Compact", StringComparison.OrdinalIgnoreCase);

    /// <summary>رنگ اصلی به‌صورت Color — برای پیش‌نمایش کارت تم در تنظیمات</summary>
    public System.Windows.Media.Color PrimaryColor =>
        (System.Windows.Media.Color)ColorConverter.ConvertFromString(PrimaryHex);
}

/// <summary>
/// تنظیمات ظاهری مستقل از پالت رنگ — چرخه بیستم (گزارش چهاردهم مالک):
/// فونت و اندازهٔ پایه و واحد پول نمایشی. قبلاً این تنظیمات فقط ذخیره
/// می‌شدند و هیچ اثری روی برنامه نداشتند؛ اکنون از طریق منابع پویا
/// (DynamicResource) روی همهٔ پنجره‌ها بی‌درنگ اعمال می‌شوند.
/// ═══ چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
/// «هر کدام از عناصر را بتوانیم به‌صورت جداگانه تغییر دهیم، نه اینکه با
/// انتخاب یک گزینه همه عوض شود» — سه تنظیم مستقلِ جدا از تم افزوده شد:
/// رنگ منوی کناری، شعاع گوشه و چگالی؛ مقدار تهی یعنی «همگام با تم».
/// </summary>
/// <param name="FontFamilyName">نام خانوادهٔ فونت — Vazirmatn (جاسازی‌شده) یا فونت ویندوز</param>
/// <param name="BaseFontSize">اندازهٔ پایهٔ متن — بین ۱۱ تا ۱۸</param>
/// <param name="AnimationsEnabled">انیمیشن‌های رابط فعال باشند؟</param>
/// <param name="CurrencyUnit">واحد پول نمایشی — Rial یا Toman (فقط نمایش، بدون اثر بر محاسبات)</param>
/// <param name="SidebarHex">رنگ منوی کناری — تهی یعنی همگام با رنگ تم (چرخه بیست‌ویکم)</param>
/// <param name="CardRadiusOverride">شعاع گوشهٔ مستقل — تهی یعنی از تم (چرخه بیست‌ویکم)</param>
/// <param name="DensityOverride">چگالی مستقل — تهی یعنی از تم؛ Compact یا Normal (چرخه بیست‌ویکم)</param>
/// <param name="CustomPrimaryHex">رنگ اصلی سفارشی — تهی یعنی از تم (چرخه ۲۴)</param>
/// <param name="IconColorMode">حالت رنگ آیکون‌ها — Mono (یکدست) یا Colorful (رنگی) — چرخه ۲۴</param>
/// <param name="IconSetName">مجموعهٔ آیکون‌های منو — Classic یا Modern یا Minimal (چرخهٔ بیست‌وهفتم:
/// تعویض یکجای شکل آیکون‌ها، هم‌عرضِ «تعویض آیکون‌ها» گزارش هجدهم مالک)</param>
public sealed record AppearanceSettings(
    string FontFamilyName = "Vazirmatn",
    double BaseFontSize = 13,
    bool AnimationsEnabled = true,
    string CurrencyUnit = "Rial",
    string? SidebarHex = null,
    int? CardRadiusOverride = null,
    string? DensityOverride = null,
    string? CustomPrimaryHex = null,
    string IconColorMode = "Mono",
    string IconSetName = "Classic")
{
    /// <summary>متن واحد پول — «ریال» یا «تومان»</summary>
    public string CurrencyLabel => CurrencyUnit == "Toman" ? "تومان" : "ریال";

    /// <summary>حالت رنگی آیکون‌ها فعال است؟ — چرخه ۲۴</summary>
    public bool IsColorfulIcons =>
        string.Equals(IconColorMode, "Colorful", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// قرارداد سرویس تم — تغییر پویا بین روشن و تیره + پالت‌های رنگی متفاوت +
/// تنظیمات ظاهری سراسری (فونت/اندازه/انیمیشن/واحد پول) — چرخه بیستم
/// </summary>
public interface IThemeService
{
    /// <summary>تم فعلی تیره است؟</summary>
    bool IsDarkTheme { get; }

    /// <summary>کلید تم رنگی فعلی — مثلاً Blue</summary>
    string CurrentThemeKey { get; }

    /// <summary>تنظیمات ظاهری فعلی (فونت/اندازه/انیمیشن/واحد پول)</summary>
    AppearanceSettings Appearance { get; }

    /// <summary>کاتالوگ تم‌های قابل انتخاب</summary>
    IReadOnlyList<AppTheme> Themes { get; }

    /// <summary>رویداد تغییر تم — برای ذخیره‌سازی و هماهنگی پوسته</summary>
    event EventHandler? ThemeChanged;

    /// <summary>رویداد تغییر تنظیمات ظاهری (فونت/اندازه/واحد پول)</summary>
    event EventHandler? AppearanceChanged;

    /// <summary>تغییر تم روشن و تیره (با حفظ پالت رنگی فعلی)</summary>
    void ToggleTheme();

    /// <summary>اعمال تم روشن/تیره (با حفظ پالت رنگی فعلی)</summary>
    void SetTheme(bool dark);

    /// <summary>اعمال تم رنگی مشخص + حالت روشن/تیره — رنگ + گوشه‌ها + چگالی</summary>
    void ApplyTheme(string themeKey, bool dark);

    /// <summary>
    /// اعمال تنظیمات ظاهری — فونت و اندازهٔ پایه و انیمیشن و واحد پول؛
    /// بی‌درنگ روی همهٔ پنجره‌های باز اعمال و به‌صورت محلی ذخیره می‌شود
    /// </summary>
    void ApplyAppearance(AppearanceSettings settings);
}
