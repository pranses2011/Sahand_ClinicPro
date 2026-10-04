namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد ناوبری بین صفحات داخل پنجره اصلی
/// </summary>
public interface INavigationService
{
    /// <summary>رفتن به صفحه مقصد با پارامتر اختیاری</summary>
    void NavigateTo(string pageKey, object? parameter = null);

    /// <summary>رویداد درخواست ناوبری</summary>
    event EventHandler<NavigationEventArgs> NavigationRequested;
}

/// <summary>
/// آرگومان رویداد ناوبری
/// </summary>
public record NavigationEventArgs(string PageKey, object? Parameter);
