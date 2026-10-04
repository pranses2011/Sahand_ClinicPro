using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// آیتم ناوبری ستون کنار — آیکون و عنوان و کلید صفحه
/// </summary>
public partial class NavItem : ObservableObject
{
    /// <summary>عنوان فارسی صفحه</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>کلید صفحه برای ناوبری</summary>
    public string PageKey { get; init; } = string.Empty;

    /// <summary>نام آیکون Material Design</summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>آیا این صفحه فعال است (در گام‌های بعدی ماژول فعال می‌شود)</summary>
    public bool Enabled { get; init; } = true;
}
