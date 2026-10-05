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
    private readonly IRealtimeClient _realtime;

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
        DoctorsViewModel doctorsVm,
        AppointmentsViewModel appointments,
        ReceptionsViewModel receptions,
        VisitsViewModel visitsVm,
        PrescriptionsViewModel prescriptionsVm,
        BillingViewModel billingVm,
        AccountingViewModel accountingVm,
        InsuranceViewModel insuranceVm,
        ParaclinicalViewModel paraclinicalVm,
        LabViewModel labVm,
        PharmacyViewModel pharmacyVm,
        StocktakingViewModel stocktakingVm,
        ReportsViewModel reportsVm,
        ReportBrowserViewModel reportBrowserVm,
        AdminViewModel adminVm,
        UsersViewModel usersVm,
        RolesViewModel rolesVm,
        SmsViewModel smsVm,
        NotificationsViewModel notificationsVm,
        SettingsViewModel settingsVm,
        BackupViewModel backupVm,
        AuditLogViewModel auditVm,
        BranchesViewModel branchesVm,
        SepasViewModel sepasVm,
        EPrescriptionViewModel erxVm,
        LicenseStatusViewModel licenseVm,
        ComplementaryViewModel complementaryVm,
        AdvancedEmrViewModel advancedEmrVm,
        PayrollViewModel payrollVm,
        AttendanceViewModel attendanceVm,
        IRealtimeClient realtime)
    {
        _api = api;
        _tokens = tokens;
        _theme = theme;
        _navigation = navigation;
        _snackbar = snackbar;
        _realtime = realtime;

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
            ["doctors"] = () => doctorsVm,
            ["appointments"] = () => appointments,
            ["receptions"] = () => receptions,
            ["visits"] = () => visitsVm,
            ["prescriptions"] = () => prescriptionsVm,
            ["billing"] = () => billingVm,
            ["accounting"] = () => accountingVm,
            ["insurance"] = () => insuranceVm,
            ["paraclinical"] = () => paraclinicalVm,
            ["lab"] = () => labVm,
            ["pharmacy"] = () => pharmacyVm,
            ["stocktaking"] = () => stocktakingVm,
            ["reports"] = () => reportBrowserVm,
            ["reports-legacy"] = () => reportsVm,
            ["settings"] = () => settingsVm,
            ["personnel"] = () => adminVm,
            ["users"] = () => usersVm,
            ["roles"] = () => rolesVm,
            ["sms"] = () => smsVm,
            ["notifications"] = () => notificationsVm,
            ["settings-full"] = () => settingsVm,
            ["backup"] = () => backupVm,
            ["audit"] = () => auditVm,
            ["branches"] = () => branchesVm,
            ["sepas"] = () => sepasVm,
            ["erx"] = () => erxVm,
            ["license"] = () => licenseVm,
            ["complementary"] = () => complementaryVm,
            ["emr-advanced"] = () => advancedEmrVm,
            ["payroll"] = () => payrollVm,
            ["attendance"] = () => attendanceVm
        };

        // آیتم‌های ناوبری — فعال بودن از لایسنس کنترل می‌شود
        NavItems = new ObservableCollection<NavItem>
        {
            new() { Title = "داشبورد", PageKey = "dashboard", Icon = "ViewDashboard" },
            new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },
            new() { Title = "پزشکان", PageKey = "doctors", Icon = "Doctor" },
            new() { Title = "نوبت‌دهی", PageKey = "appointments", Icon = "CalendarClock" },
            new() { Title = "پذیرش", PageKey = "receptions", Icon = "Reception" },
            new() { Title = "ویزیت و معاینه", PageKey = "visits", Icon = "Stethoscope" },
            new() { Title = "نسخه‌نویسی", PageKey = "prescriptions", Icon = "Prescription" },
            new() { Title = "پرونده بالینی پیشرفته", PageKey = "emr-advanced", Icon = "HeartPulse" },
            new() { Title = "مالی و صندوق", PageKey = "billing", Icon = "CashMultiple" },
            new() { Title = "حسابداری", PageKey = "accounting", Icon = "BookOpenOutline" },
            new() { Title = "فیش حقوقی", PageKey = "payroll", Icon = "CashCheck" },
            new() { Title = "حضور و غیاب", PageKey = "attendance", Icon = "Fingerprint" },
            new() { Title = "بیمه", PageKey = "insurance", Icon = "ShieldCheckOutline" },
            new() { Title = "پاراکلینیک", PageKey = "paraclinical", Icon = "MedicalBag" },
            new() { Title = "آزمایشگاه", PageKey = "lab", Icon = "TestTube" },
            new() { Title = "داروخانه", PageKey = "pharmacy", Icon = "Pill" },
            new() { Title = "انبارگردانی", PageKey = "stocktaking", Icon = "ClipboardCheckOutline" },
            new() { Title = "گزارش‌ها", PageKey = "reports", Icon = "FileChart" },
            new() { Title = "کاربران", PageKey = "users", Icon = "AccountKey" },
            new() { Title = "نقش‌ها", PageKey = "roles", Icon = "ShieldAccount" },
            new() { Title = "پیامک", PageKey = "sms", Icon = "MessageTextOutline" },
            new() { Title = "پرسنل", PageKey = "personnel", Icon = "AccountTie" },
            new() { Title = "تنظیمات", PageKey = "settings", Icon = "Cog" },
            new() { Title = "سپاس", PageKey = "sepas", Icon = "BankTransfer" },
            new() { Title = "نسخه الکترونیک", PageKey = "erx", Icon = "CloudUploadOutline" },
            new() { Title = "شعبه‌ها", PageKey = "branches", Icon = "HomeGroup" },
            new() { Title = "بکاپ و بازیابی", PageKey = "backup", Icon = "DatabaseBackup" },
            new() { Title = "لاگ عملیات", PageKey = "audit", Icon = "FileDocumentEditOutline" },
            new() { Title = "لایسنس", PageKey = "license", Icon = "KeyChain" },
            new() { Title = "قالب چاپ و نظرسنجی", PageKey = "complementary", Icon = "TuneVertical" }
        };

        NavigateToPage("dashboard");

        // بارگذاری شعبه‌ها برای انتخابگر TopBar — گام U17
        _ = LoadBranchesAsync();

        // اتصال رویداد انقضای نشست — بازگشت به صفحه ورود
        _api.SessionExpired += (_, _) => OnSessionExpired();

        // ═══ اتصال بلادرنگ SignalR — گام R1 ═══
        // تغییر صف → تازه‌سازی فوری صفحات صف‌دار (پذیرش/ویزیت)
        _realtime.QueueChanged += async (_, args) =>
        {
            var page = CurrentPage;
            if (page is ReceptionsViewModel receptions)
                await receptions.LoadAsync();
            else if (page is VisitsViewModel visits)
                await visits.LoadAsync();
        };

        // اعلان جدید → شمارنده زنگ + پیام شناور
        _realtime.NotificationReceived += (_, args) =>
        {
            UnreadCount++;
            _snackbar.Info($"🔔 {args.Title}");
        };

        // هشدار سراسری → پیام شناور هشدار
        _realtime.BroadcastAlert += (_, args) =>
            _snackbar.Info($"⚠ {args.Title}: {args.Message}");

        // وضعیت اتصال بلادرنگ در StatusBar
        _realtime.ConnectionStateChanged += (_, _) =>
            ConnectionStatus = _realtime.IsConnected ? "متصل • بلادرنگ فعال" : "متصل";

        // شروع اتصال پس از ورود — MainWindow فقط با توکن معتبر باز می‌شود
        _ = _realtime.StartAsync();
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

    /// <summary>مشاهده مرکز اعلان‌ها — ناوبری به صفحه اعلان‌ها و تازه‌سازی شمارنده</summary>
    [RelayCommand]
    private Task ShowNotificationsAsync()
    {
        NavigateToPage("notifications");
        // تازه‌سازی شمارنده خوانده‌نشده از سرور
        return RefreshUnreadCountAsync();
    }

    /// <summary>تازه‌سازی شمارنده اعلان خوانده‌نشده از سرور</summary>
    private async Task RefreshUnreadCountAsync()
    {
        var unread = await _api.GetAsync<UnreadResponse>("notifications/unread-count");
        UnreadCount = unread?.Data?.Count ?? 0;
    }

    /// <summary>پاسخ شمارنده خوانده‌نشده</summary>
    private sealed class UnreadResponse
    {
        public bool IsSuccess { get; set; }
        public UnreadData? Data { get; set; }
    }

    /// <summary>داده شمارنده</summary>
    private sealed class UnreadData
    {
        public int Count { get; set; }
    }

    /// <summary>ردیف شعبه برای انتخابگر TopBar</summary>
    public sealed class BranchPickerRow
    {
        /// <summary>شناسه</summary>
        public long Id { get; init; }

        /// <summary>نام</summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>فهرست شعبه‌های فعال برای انتخابگر TopBar</summary>
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<BranchPickerRow> _branches = new();

    /// <summary>شعبه انتخاب‌شده — تنظیم هدر X-Branch-Id روی همه درخواست‌ها</summary>
    [ObservableProperty]
    private BranchPickerRow? _selectedBranch;

    /// <summary>آیا بیش از یک شعبه فعال است — نمایش انتخابگر</summary>
    public bool HasMultipleBranches => Branches.Count > 1;

    /// <summary>بارگذاری فهرست شعبه‌ها از سرور</summary>
    private async Task LoadBranchesAsync()
    {
        var response = await _api.GetAsync<BranchesResponse>("branches");
        var rows = (response?.Data ?? new List<BranchDto>())
            .Select(b => new BranchPickerRow { Id = b.Id, Name = b.Name })
            .ToList();
        Branches = new System.Collections.ObjectModel.ObservableCollection<BranchPickerRow>(rows);
        SelectedBranch = Branches.FirstOrDefault();
        OnPropertyChanged(nameof(HasMultipleBranches));
    }

    /// <summary>بازخورد انتخاب شعبه — اعمال روی کلاینت API</summary>
    partial void OnSelectedBranchChanged(BranchPickerRow? value)
    {
        if (_api is ApiClient concrete)
            concrete.BranchId = value?.Id;
        _snackbar.Info(value is null ? "بدون فیلتر شعبه" : $"شعبه فعال: {value.Name}");
    }

    /// <summary>پاسخ شعبه‌ها</summary>
    private sealed class BranchesResponse
    {
        public bool IsSuccess { get; set; }
        public List<BranchDto>? Data { get; set; }
    }

    /// <summary>ردیف شعبه</summary>
    private sealed class BranchDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>خروج از حساب — ابطال توکن، قطع بلادرنگ و بازگشت به ورود</summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _realtime.StopAsync();
        await _api.LogoutAsync();
        OnSessionExpired();
    }

    /// <summary>تغییر تم روشن و تیره</summary>
    [RelayCommand]
    private void ToggleTheme() => _theme.ToggleTheme();

    /// <summary>بازگشت به صفحه ورود — بستن پنجره فعلی و باز کردن ورود</summary>
    private void OnSessionExpired()
    {
        // قطع اتصال بلادرنگ — نشست دیگر معتبر نیست (گام R1)
        _ = _realtime.StopAsync();
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
