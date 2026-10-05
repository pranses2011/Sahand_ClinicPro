using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel پوسته اصلی — گام U0 (ارتقای طبق قوانین کلی UI):
/// ناوبری و تم و کاربر و خروج + مسیر صفحه + ساعت شمسی لحظه‌ای +
/// زنگ اعلان با شمارنده + وضعیت اتصال و لایسنس در StatusBar
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ITokenManager _tokens;
    private readonly IThemeService _theme;
    private readonly INavigationService _navigation;
    private readonly ISnackbarService _snackbar;

    /// <summary>تایمر ساعت لحظه‌ای — هر ثانیه به‌روزرسانی</summary>
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>صفحه فعلی به‌عنوان ViewModel</summary>
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>آیتم ناوبری انتخاب‌شده</summary>
    [ObservableProperty]
    private NavItem? _selectedNav;

    /// <summary>عنوان هدر صفحه فعلی</summary>
    [ObservableProperty]
    private string _currentTitle = "داشبورد";

    /// <summary>مسیر صفحه فعلی (Breadcrumb) — «خانه > داشبورد»</summary>
    [ObservableProperty]
    private string _breadcrumb = "خانه > داشبورد";

    /// <summary>ساعت شمسی لحظه‌ای — «۱۴۰۴/۰۴/۰۱ - ۱۰:۳۰:۲۵»</summary>
    [ObservableProperty]
    private string _clockText = string.Empty;

    /// <summary>نام کاربر واردشده</summary>
    [ObservableProperty]
    private string _currentUser = string.Empty;

    /// <summary>تاریخ امروز شمسی</summary>
    [ObservableProperty]
    private string _currentDate = string.Empty;

    /// <summary>وضعیت اتصال در StatusBar</summary>
    [ObservableProperty]
    private string _connectionStatus = "متصل";

    /// <summary>وضعیت لایسنس در StatusBar</summary>
    [ObservableProperty]
    private string _licenseStatus = "لایسنس فعال";

    /// <summary>تعداد اعلان‌های خوانده‌نشده</summary>
    [ObservableProperty]
    private int _unreadCount;

    /// <summary>صف ارسال پیام شناور — برای Snackbar سراسری</summary>
    public SnackbarMessageQueue SnackbarQueue { get; } = new(TimeSpan.FromSeconds(3));

    /// <summary>کارخانه صفحات — کلید به ViewModel</summary>
    private readonly Dictionary<string, Func<ViewModelBase>> _pages;

    /// <summary>سازنده با تزریق سرویس‌ها و صفحات</summary>
    public MainWindowViewModel(
        IApiClient api,
        ITokenManager tokens,
        IThemeService theme,
        INavigationService navigation,
        ISnackbarService snackbar,
        DashboardViewModel dashboard,
        PatientsViewModel patients,
        AppointmentsViewModel appointments,
        ReceptionsViewModel receptions,
        VisitsViewModel visitsVm,
        PrescriptionsViewModel prescriptionsVm,
        BillingViewModel billingVm,
        LabViewModel labVm,
        PharmacyViewModel pharmacyVm,
        ReportsViewModel reportsVm,
        AdminViewModel adminVm)
    {
        _api = api;
        _tokens = tokens;
        _theme = theme;
        _navigation = navigation;
        _snackbar = snackbar;

        CurrentUser = tokens.Username ?? "کاربر";
        CurrentDate = PersianDateHelper.ToShamsi(DateTime.Now);

        // ساعت لحظه‌ای — هر ثانیه به‌روزرسانی شمسی و ساعت
        _clock.Tick += (_, _) =>
            ClockText = $"{PersianDateHelper.ToShamsi(DateTime.Now)} - {DateTime.Now:HH:mm:ss}";
        _clock.Start();
        ClockText = $"{PersianDateHelper.ToShamsi(DateTime.Now)} - {DateTime.Now:HH:mm:ss}";

        // اتصال سرویس پیام شناور به صف نمایش
        _snackbar.MessagePosted += (message, _) => SnackbarQueue.Enqueue(message);

        // کارخانه صفحات — صفحات بعدی در گام‌های ۱۳ به بعد اضافه می‌شوند
        _pages = new Dictionary<string, Func<ViewModelBase>>
        {
            ["dashboard"] = () => dashboard,
            ["patients"] = () => patients,
            ["appointments"] = () => appointments,
            ["receptions"] = () => receptions,
            ["visits"] = () => visitsVm,
            ["prescriptions"] = () => prescriptionsVm,
            ["billing"] = () => billingVm,
            ["lab"] = () => labVm,
            ["pharmacy"] = () => pharmacyVm,
            ["reports"] = () => reportsVm,
            ["settings"] = () => adminVm
        };

        // آیتم‌های ناوبری — فعال بودن از لایسنس کنترل می‌شود
        NavItems = new ObservableCollection<NavItem>
        {
            new() { Title = "داشبورد", PageKey = "dashboard", Icon = "ViewDashboard" },
            new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },
            new() { Title = "نوبت‌دهی", PageKey = "appointments", Icon = "CalendarClock" },
            new() { Title = "پذیرش", PageKey = "receptions", Icon = "Reception" },
            new() { Title = "ویزیت و معاینه", PageKey = "visits", Icon = "Stethoscope" },
            new() { Title = "نسخه‌نویسی", PageKey = "prescriptions", Icon = "Prescription" },
            new() { Title = "مالی و صندوق", PageKey = "billing", Icon = "CashMultiple" },
            new() { Title = "آزمایشگاه", PageKey = "lab", Icon = "TestTube" },
            new() { Title = "داروخانه", PageKey = "pharmacy", Icon = "Pill" },
            new() { Title = "گزارش‌ها", PageKey = "reports", Icon = "FileChart" },
            new() { Title = "تنظیمات", PageKey = "settings", Icon = "Cog" }
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
        Breadcrumb = $"خانه > {CurrentTitle}";
    }

    /// <summary>آیا اعلان خوانده‌نشده وجود دارد — برای نمایش نشانگر زنگ</summary>
    public bool HasUnreadNotifications => UnreadCount > 0;

    /// <summary>بازخورد تغییر شمارنده — تازه‌سازی وضعیت نمایش نشانگر</summary>
    partial void OnUnreadCountChanged(int value)
        => OnPropertyChanged(nameof(HasUnreadNotifications));

    /// <summary>مشاهده مرکز اعلان‌ها — گام U14 به صفحه واقعی متصل می‌شود</summary>
    [RelayCommand]
    private void ShowNotifications()
    {
        _snackbar.Info("مرکز اعلان‌ها در گام‌های بعدی متصل می‌شود");
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
