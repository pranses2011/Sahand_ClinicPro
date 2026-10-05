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
    public void ToggleTheme() => SetTheme(!_isDark);

    /// <summary>اعمال تم مشخص — گام U15 بخش ظاهر</summary>
    public void SetTheme(bool dark)
    {
        _isDark = dark;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }
}
