namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد سرویس تم — تغییر تم روشن و تیره
/// </summary>
public interface IThemeService
{
    /// <summary>تم فعلی تیره است؟</summary>
    bool IsDarkTheme { get; }

    /// <summary>تغییر تم</summary>
    void ToggleTheme();

    /// <summary>اعمال تم مشخص — گام U15 بخش ظاهر</summary>
    void SetTheme(bool dark);
}
