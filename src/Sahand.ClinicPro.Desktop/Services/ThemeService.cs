using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس تم — تغییر پویا بین روشن و تیره با PaletteHelper
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    private bool _isDark;

    /// <summary>تم فعلی تیره است؟</summary>
    public bool IsDarkTheme => _isDark;

    /// <summary>تغییر تم روشن و تیره</summary>
    public void ToggleTheme()
    {
        _isDark = !_isDark;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(_isDark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }
}
