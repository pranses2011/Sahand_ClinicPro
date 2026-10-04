namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس ناوبری — انتشار رویداد برای پوسته اصلی
/// </summary>
public class NavigationService : INavigationService
{
    /// <summary>رویداد درخواست ناوبری</summary>
    public event EventHandler<NavigationEventArgs>? NavigationRequested;

    /// <summary>درخواست رفتن به صفحه</summary>
    public void NavigateTo(string pageKey, object? parameter = null)
    {
        NavigationRequested?.Invoke(this, new NavigationEventArgs(pageKey, parameter));
    }
}
