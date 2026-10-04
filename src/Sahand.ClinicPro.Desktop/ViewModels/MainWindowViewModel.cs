using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel پوسته اصلی — مدیریت ناوبری و تم و کاربر و خروج
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ITokenManager _tokens;
    private readonly IThemeService _theme;
    private readonly INavigationService _navigation;

    /// <summary>صفحه فعلی به‌عنوان ViewModel</summary>
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>آیتم ناوبری انتخاب‌شده</summary>
    [ObservableProperty]
    private NavItem? _selectedNav;

    /// <summary>عنوان هدر صفحه فعلی</summary>
    [ObservableProperty]
    private string _currentTitle = "داشبورد";

    /// <summary>نام کاربر واردشده</summary>
    [ObservableProperty]
    private string _currentUser = string.Empty;

    /// <summary>تاریخ امروز شمسی</summary>
    [ObservableProperty]
    private string _currentDate = string.Empty;

    /// <summary>کارخانه صفحات — کلید به ViewModel</summary>
    private readonly Dictionary<string, Func<ViewModelBase>> _pages;

    /// <summary>سازنده با تزریق سرویس‌ها و صفحات</summary>
    public MainWindowViewModel(
        IApiClient api,
        ITokenManager tokens,
        IThemeService theme,
        INavigationService navigation,
        DashboardViewModel dashboard,
        PatientsViewModel patients,
        AppointmentsViewModel appointments,
        ReceptionsViewModel receptions,
        VisitsViewModel visitsVm)
    {
        _api = api;
        _tokens = tokens;
        _theme = theme;
        _navigation = navigation;

        CurrentUser = tokens.Username ?? "کاربر";
        CurrentDate = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
            .ToShamsi(DateTime.UtcNow);

        // کارخانه صفحات — صفحات بعدی در گام‌های ۱۳ به بعد اضافه می‌شوند
        _pages = new Dictionary<string, Func<ViewModelBase>>
        {
            ["dashboard"] = () => dashboard,
            ["patients"] = () => patients,
            ["appointments"] = () => appointments,
            ["receptions"] = () => receptions,
            ["visits"] = () => visitsVm
        };

        // آیتم‌های ناوبری — فعال بودن از لایسنس کنترل می‌شود
        NavItems = new ObservableCollection<NavItem>
        {
            new() { Title = "داشبورد", PageKey = "dashboard", Icon = "ViewDashboard" },
            new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },
            new() { Title = "نوبت‌دهی", PageKey = "appointments", Icon = "CalendarClock" },
            new() { Title = "پذیرش", PageKey = "receptions", Icon = "Reception" },
            new() { Title = "ویزیت و معاینه", PageKey = "visits", Icon = "Stethoscope" },
            new() { Title = "نسخه‌نویسی", PageKey = "prescriptions", Icon = "Prescription", Enabled = false },
            new() { Title = "مالی و صندوق", PageKey = "billing", Icon = "CashMultiple", Enabled = false },
            new() { Title = "آزمایشگاه", PageKey = "lab", Icon = "TestTube", Enabled = false },
            new() { Title = "داروخانه", PageKey = "pharmacy", Icon = "Pill", Enabled = false },
            new() { Title = "گزارش‌ها", PageKey = "reports", Icon = "FileChart", Enabled = false },
            new() { Title = "تنظیمات", PageKey = "settings", Icon = "Cog", Enabled = false }
        };

        NavigateToPage("dashboard");

        // اتصال رویداد انقضای نشست — بازگشت به صفحه ورود
        _api.SessionExpired += (_, _) => OnSessionExpired();
    }

    /// <summary>فهرست آیتم‌های ناوبری</summary>
    public ObservableCollection<NavItem> NavItems { get; }

    /// <summary>انتخاب آیتم ناوبری → رفتن به صفحه</summary>
    partial void OnSelectedNavChanged(NavItem? value)
    {
        if (value is not null)
            NavigateToPage(value.PageKey);
    }

    /// <summary>رفتن به صفحه با کلید</summary>
    private void NavigateToPage(string pageKey)
    {
        if (!_pages.TryGetValue(pageKey, out var factory))
        {
            // صفحات گام‌های بعد — پیام سازگار
            CurrentTitle = "این ماژول هنوز فعال نشده است";
            return;
        }

        CurrentPage = factory();
        CurrentTitle = CurrentPage.Title;
    }

    /// <summary>خروج از حساب — ابطال توکن و بازگشت به ورود</summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _api.LogoutAsync();
        OnSessionExpired();
    }

    /// <summary>تغییر تم روشن و تیره</summary>
    [RelayCommand]
    private void ToggleTheme() => _theme.ToggleTheme();

    /// <summary>بازگشت به صفحه ورود — بستن پنجره فعلی و باز کردن ورود</summary>
    private void OnSessionExpired()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var login = App.Services.GetRequiredService(typeof(LoginWindow)) as LoginWindow;
            if (login is null) return;

            var oldWindow = System.Windows.Application.Current.MainWindow;
            System.Windows.Application.Current.MainWindow = login;
            login.Show();
            oldWindow?.Close();
        });
    }
}
