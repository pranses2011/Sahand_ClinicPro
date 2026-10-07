namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// تعریف یک تم رنگی برنامه — چرخه نوزدهم (گزارش سیزدهم مالک):
/// چند تم با رنگ‌ها و آیکون‌های متفاوت که از تنظیمات قابل انتخاب است
/// </summary>
/// <param name="Key">کلید ذخیره‌سازی</param>
/// <param name="Title">نام فارسی تم</param>
/// <param name="Icon">آیکون نمایشی</param>
/// <param name="PrimaryHex">رنگ اصلی</param>
/// <param name="SecondaryHex">رنگ ثانویه (تأکیدی)</param>
public sealed record AppTheme(string Key, string Title, string Icon, string PrimaryHex, string SecondaryHex);

/// <summary>
/// قرارداد سرویس تم — تغییر پویا بین روشن و تیره + پالت‌های رنگی متفاوت
/// </summary>
public interface IThemeService
{
    /// <summary>تم فعلی تیره است؟</summary>
    bool IsDarkTheme { get; }

    /// <summary>کلید تم رنگی فعلی — مثلاً Blue</summary>
    string CurrentThemeKey { get; }

    /// <summary>کاتالوگ تم‌های قابل انتخاب</summary>
    IReadOnlyList<AppTheme> Themes { get; }

    /// <summary>رویداد تغییر تم — برای ذخیره‌سازی و هماهنگی پوسته</summary>
    event EventHandler? ThemeChanged;

    /// <summary>تغییر تم روشن و تیره (با حفظ پالت رنگی فعلی)</summary>
    void ToggleTheme();

    /// <summary>اعمال تم روشن/تیره (با حفظ پالت رنگی فعلی)</summary>
    void SetTheme(bool dark);

    /// <summary>اعمال تم رنگی مشخص + حالت روشن/تیره</summary>
    void ApplyTheme(string themeKey, bool dark);
}
