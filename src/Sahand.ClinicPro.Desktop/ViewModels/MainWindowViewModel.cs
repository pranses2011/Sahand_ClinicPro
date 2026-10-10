using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
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

    /// <summary>کلید صفحهٔ جاری — برای بازسازی صفحه پس از تغییر ظاهر/واحد پول (چرخه بیست‌وسوم)</summary>
    private string _currentPageKey = string.Empty;

    /// <summary>
    /// شمارندهٔ نوسازی رنگ آیکون‌های منو — چرخه ۲۴ (گزارش هفدهم مالک): با هر تغییر
    /// تنظیمات ظاهری (به‌ویژه حالت آیکون رنگی/یکدست) این شمارنده زیاد می‌شود و
    /// MultiBindingهای رنگ آیکون دوباره ارزیابی می‌شوند؛ نتیجه: تعویض حالت
    /// آیکون بدون ری‌استارت بی‌درنگ روی منو می‌نشیند.
    /// </summary>
    [ObservableProperty]
    private int _iconRefreshTick;

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

    /// <summary>وضعیت لایسنس در StatusBar — چرخهٔ بیست‌وهفتم: از سرور تازه می‌شود</summary>
    [ObservableProperty]
    private string _licenseStatus = "لایسنس فعال";

    /// <summary>تایمر تازه‌سازی وضعیت لایسنس — هر ۳۰ دقیقه (چرخهٔ بیست‌وهفتم)</summary>
    private readonly System.Windows.Threading.DispatcherTimer? _licenseTimer;

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
        // چرخهٔ بیست‌وهفتم — میزکار پزشک (صفحهٔ ویزیت پزشک)
        DoctorVisitViewModel doctorVisitVm,
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
        PersonnelViewModel personnelVm,
        UsersViewModel usersVm,
        RolesViewModel rolesVm,
        SmsViewModel smsVm,
        SurveyViewModel surveyVm,
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
        TrainingCenterViewModel trainingVm,
        IRealtimeClient realtime)
    {
        _api = api;
        _tokens = tokens;
        _theme = theme;
        _navigation = navigation;
        _snackbar = snackbar;
        _realtime = realtime;

        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): «واحد پولی که عوض می‌کنیم در تمام
        // برنامه اعمال نشود» — مبدل‌های مبلغ (CurrencyRial/MoneyWithUnit) مقدار
        // را هنگام تغییر «منبع» قالب‌بندی می‌کنند؛ تغییر واحد پول استاتیک است و
        // اتصال‌های موجود را وادار به نوسازی نمی‌کند. با هر تغییر ظاهر، صفحهٔ جاری
        // از نو ساخته می‌شود تا همهٔ مبالغ با واحد تازه (و ارقام فارسی) دوباره
        // رندر شوند — بی‌نیاز از ری‌استارت برنامه.
        _theme.AppearanceChanged += (_, _) =>
        {
            // چرخه ۲۴: نوسازی رنگ آیکون‌های منو — شمارنده با هر تغییر ظاهری زیاد
            // می‌شود و MultiBindingهای رنگ آیکون دوباره ارزیابی می‌شوند (حالت
            // آیکون رنگی/یکدست بی‌درنگ عوض می‌شود — بدون ری‌استارت)
            IconRefreshTick++;

            if (string.IsNullOrEmpty(_currentPageKey))
                return;
            NavigateToPage(_currentPageKey);

            // نوسازی داده‌های صفحهٔ جاری — رشته‌های قالب‌بندی‌شده در سطح ViewModel
            // (مثل جمع‌ها و ردیف‌های مبلغ) با بازسازی نما نو نمی‌شوند؛ اگر صفحه
            // فرمان استاندارد بارگذاری/نوسازی داشته باشد فراخوانده می‌شود.
            try
            {
                var type = CurrentPage?.GetType();
                var commandProp = type?.GetProperty("RefreshCommand")
                                  ?? type?.GetProperty("LoadCommand");
                if (commandProp?.GetValue(CurrentPage) is System.Windows.Input.ICommand command
                    && command.CanExecute(null))
                    command.Execute(null);
            }
            catch
            {
                // نوسازی داده اختیاری است — خطایش جریان را متوقف نمی‌کند
            }
        };

        CurrentUser = tokens.Username ?? "کاربر";
        // چرخه هجدهم (گزارش دوازدهم): تاریخ و ساعت نوار بالا با ارقام فارسی
        CurrentDate = PersianDateHelper.ToShamsiFa(DateTime.Now);

        // ساعت لحظه‌ای — هر ثانیه به‌روزرسانی شمسی و ساعت (ارقام فارسی)
        _clock.Tick += (_, _) =>
            ClockText = $"{PersianDateHelper.ToShamsiFa(DateTime.Now)} - {PersianDateHelper.ToClockFa(DateTime.Now)}";
        _clock.Start();
        ClockText = $"{PersianDateHelper.ToShamsiFa(DateTime.Now)} - {PersianDateHelper.ToClockFa(DateTime.Now)}";

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
            // چرخهٔ بیست‌وهفتم — میزکار پزشک (گزارش هجدهم مالک)
            ["doctor-visit"] = () => doctorVisitVm,
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
            // چرخه ۲۴: صفحهٔ «مدیریت سامانه» (AdminView تب‌دار: تنظیمات کلینیک/
            // لاگ حسابرسی/پشتیبان‌گیری/آرشیو سالانه) — با جدا شدن پرسنل و
            // تنظیماتِ بخش‌بندی‌شده، این صفحه از منو حذف شده و تک‌افتاده بود؛
            // دوباره از منوی مدیریت سامانه در دسترس شد (گزارش هفدهم مالک)
            ["admin"] = () => adminVm,
            // چرخه بیستم (گزارش چهاردهم مالک): پرسنل صفحهٔ خودش را می‌گیرد —
            // پیش‌تر به adminVm (تنظیمات) نگاشت شده بود و زبانهٔ تنظیمات باز می‌شد
            ["personnel"] = () => personnelVm,
            ["users"] = () => usersVm,
            ["roles"] = () => rolesVm,
            ["sms"] = () => smsVm,
            ["surveys"] = () => surveyVm,
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
            ["attendance"] = () => attendanceVm,
            ["training"] = () => trainingVm
        };

        // ═══ آیتم‌های ناوبری — چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
        // «گزینه‌های لیست کناری مرتب نیستند؛ بر اساس کاربردشان مرتب بکن و شلوغی را
        // با زیر منو کم کن» — ۳۰ آیتم تخت به ۹ گروهِ عملکردی + ۳ آیتم تکی بازسازماندهی شد.
        // گروه‌ها: پذیرش و درمان | نسخه و دارو | پاراکلینیک | پزشکان و پرسنل |
        // مالی و بیمه | ارتباط با بیمار | مدیریت سامانه
        static NavItem Group(string title, string icon, params NavItem[] children)
        {
            var group = new NavItem { Title = title, Icon = icon };
            foreach (var child in children)
                group.Children.Add(child);
            return group;
        }

        NavItems = new ObservableCollection<NavItem>
        {
            new() { Title = "داشبورد", PageKey = "dashboard", Icon = "ViewDashboard" },

            // ─── جریان اصلی درمان از ورود تا خروج ───
            Group("پذیرش و درمان", "Hospital",
                new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },
                new() { Title = "نوبت‌دهی", PageKey = "appointments", Icon = "CalendarClock" },
                new() { Title = "پذیرش", PageKey = "receptions", Icon = "ClipboardAccount" },
                new() { Title = "ویزیت و معاینه", PageKey = "visits", Icon = "Stethoscope" },
                // چرخهٔ بیست‌وهفتم — میزکار شخصی پزشک (صف و فراخوان و آمار خودش)
                new() { Title = "میزکار پزشک", PageKey = "doctor-visit", Icon = "Doctor" },
                new() { Title = "پرونده بالینی پیشرفته", PageKey = "emr-advanced", Icon = "HeartPulse" }),

            // ─── تجویز و دارو ───
            Group("نسخه و دارو", "Prescription",
                new() { Title = "نسخه‌نویسی", PageKey = "prescriptions", Icon = "FileDocumentEdit" },
                new() { Title = "داروخانه", PageKey = "pharmacy", Icon = "Pill" },
                new() { Title = "انبارگردانی", PageKey = "stocktaking", Icon = "ClipboardCheckOutline" },
                new() { Title = "نسخه الکترونیک", PageKey = "erx", Icon = "CloudUploadOutline" }),

            // ─── تشخیص‌های یاری ───
            Group("پاراکلینیک", "TestTube",
                new() { Title = "آزمایشگاه", PageKey = "lab", Icon = "TestTube" },
                new() { Title = "تصویربرداری و پاراکلینیک", PageKey = "paraclinical", Icon = "MedicalBag" }),

            // ─── کادر درمان و پرسنل ───
            Group("پزشکان و پرسنل", "AccountTie",
                new() { Title = "پزشکان", PageKey = "doctors", Icon = "Doctor" },
                new() { Title = "پرسنل", PageKey = "personnel", Icon = "AccountTie" },
                new() { Title = "فیش حقوقی", PageKey = "payroll", Icon = "CashCheck" },
                new() { Title = "حضور و غیاب", PageKey = "attendance", Icon = "Fingerprint" }),

            // ─── امور مالی ───
            Group("مالی و بیمه", "CashMultiple",
                new() { Title = "مالی و صندوق", PageKey = "billing", Icon = "CashMultiple" },
                new() { Title = "حسابداری", PageKey = "accounting", Icon = "BookOpenOutline" },
                new() { Title = "بیمه", PageKey = "insurance", Icon = "ShieldCheckOutline" },
                new() { Title = "سپاس", PageKey = "sepas", Icon = "BankTransfer" }),

            // ─── ارتباط با بیمار ───
            Group("ارتباط با بیمار", "MessageTextOutline",
                new() { Title = "پیامک", PageKey = "sms", Icon = "MessageTextOutline" },
                new() { Title = "نظرسنجی بیماران", PageKey = "surveys", Icon = "CommentQuoteOutline" }),

            // ─── گزارش‌ها ───
            new() { Title = "گزارش‌ها", PageKey = "reports", Icon = "FileChart" },

            // ─── مدیریت و تنظیمات سامانه ───
            Group("مدیریت سامانه", "Cog",
                new() { Title = "مدیریت سامانه (تب‌بندی کامل)", PageKey = "admin", Icon = "ViewDashboardOutline" },
                new() { Title = "کاربران", PageKey = "users", Icon = "AccountKey" },
                new() { Title = "نقش‌ها", PageKey = "roles", Icon = "ShieldAccount" },
                new() { Title = "تنظیمات", PageKey = "settings", Icon = "Cog" },
                new() { Title = "شعبه‌ها", PageKey = "branches", Icon = "HomeGroup" },
                new() { Title = "پشتیبان‌گیری و بازیابی", PageKey = "backup", Icon = "DatabaseSync" },
                new() { Title = "لاگ عملیات", PageKey = "audit", Icon = "FileDocumentEditOutline" },
                new() { Title = "لایسنس", PageKey = "license", Icon = "KeyChain" },
                new() { Title = "قالب چاپ و نظرسنجی", PageKey = "complementary", Icon = "TuneVertical" }),

            // ─── آموزش ───
            new() { Title = "🎓 مرکز آموزش", PageKey = "training", Icon = "School" }
        };

        NavigateToPage("dashboard");

        // بارگذاری شعبه‌ها برای انتخابگر TopBar — گام U17
        _ = LoadBranchesAsync();

        // اتصال رویداد انقضای نشست — بازگشت به صفحه ورود
        _api.SessionExpired += (_, _) => OnSessionExpired();

        // ═══ تور هدایت‌شده اولین اجرا — چرخه سیزدهم (درخواست ۸-۶) ═══
        // نام کاربر برای مارکر محلی + نمایش خودکار در نخستین ورود این ایستگاه
        Views.TourMarker.CurrentUser = tokens.Username;
        _ = TryShowGuidedTourAsync();

        // ═══ اتصال بلادرنگ SignalR — گام R1 ═══
        // تغییر صف → تازه‌سازی فوری صفحات صف‌دار (پذیرش/ویزیت)
        // ═══ چرخهٔ بیست‌وهشتم: میزکار پزشک هم به همین جریان وصل شد — «وقتی
        // بیماری پذیرش می‌شود باید فوری در لیست نشان داده شود در حالی که چند
        // ثانیه طول می‌کشد تا با تپ قلب بعدی بیاید»؛ تازه‌ساز ۳۰ ثانیه‌ای
        // میزکار هنوز هست ولی تغییر صف حالا بی‌درنگ همان لحظه می‌نشیند ═══
        _realtime.QueueChanged += async (_, args) =>
        {
            var page = CurrentPage;
            if (page is ReceptionsViewModel receptions)
                await receptions.LoadAsync();
            else if (page is VisitsViewModel visits)
                await visits.LoadAsync();
            else if (page is DoctorVisitViewModel doctorVisit)
                await doctorVisit.LoadAsync();
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

        // ═══ چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک) — پیام‌های سرور لایسنس ═══
        // «پیام‌های سرور لایسنس در برنامه نمایش داده شود»: نوار وضعیت از متن
        // ثابتِ «لایسنس فعال» به وضعیت واقعی + روزهای باقیمانده تبدیل می‌شود و
        // پیام پنل (هشدار انقضا/اطلاعیهٔ فروشنده) با اعلان شناور سراسری به
        // کاربر می‌رسد. هر ۳۰ دقیقه هم تازه می‌شود (هم‌قدم با تپ‌قلب سرور).
        _ = LoadLicenseStatusAsync();
        _licenseTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(30)
        };
        _licenseTimer.Tick += (_, _) => _ = LoadLicenseStatusAsync();
        _licenseTimer.Start();
    }

    /// <summary>
    /// بارگذاری وضعیت لایسنس در نوار وضعیت + نمایش پیام سرور — چرخهٔ بیست‌وهفتم.
    /// خطا هرگز جریان برنامه را متوقف نمی‌کند (متن پیش‌فرض باقی می‌ماند).
    /// </summary>
    private async Task LoadLicenseStatusAsync()
    {
        try
        {
            var check = await LicenseGate.CheckDetailedAsync(_api);

            // متن نوار وضعیت — وضعیت + روزهای باقیمانده (فقط وقتی کم است)
            LicenseStatus = check.Verdict switch
            {
                LicenseGate.Verdict.Valid => check.DaysRemaining is { } days and > 0 && days <= 30
                    ? $"لایسنس فعال — {PersianDateHelper.ToPersianDigits(days.ToString())} روز باقیمانده"
                    : "لایسنس فعال",
                LicenseGate.Verdict.Invalid => "❌ لایسنس نامعتبر",
                _ => "لایسنس — وضعیت نامعلوم"
            };

            // پیام سرور لایسنس → اعلان شناور سراسری (یادآوری تک‌باره در هر تازه‌سازی)
            if (!string.IsNullOrWhiteSpace(check.Message))
                _snackbar.Info($"🏛 سرور لایسنس: {check.Message}");
        }
        catch
        {
            // قطعی لحظه‌ای سرور — نوار وضعیت دست‌نخورده می‌ماند (fail-open)
        }
    }

    /// <summary>فهرست آیتم‌های ناوبری</summary>
    public ObservableCollection<NavItem> NavItems { get; }

    /// <summary>انتخاب آیتم ناوبری → رفتن به صفحه (آیتم گروه فقط باز/بسته می‌شود)</summary>
    partial void OnSelectedNavChanged(NavItem? value)
    {
        if (value is null) return;
        // چرخه بیست‌ویکم: آیتم‌های گروه‌دار صفحه ندارند — فقط زیرمنو باز/بسته می‌شود
        if (value.HasChildren)
        {
            value.IsExpanded = !value.IsExpanded;
            return;
        }
        NavigateToPage(value.PageKey);
    }

    /// <summary>
    /// دستور کلیک روی آیتم منو — چرخه بیست‌ویکم: برای آیتم‌های زیرمنو که در
    /// ListBox تو در تو قرار دارند (بدون SelectedItem والد)؛ آیتم گروه را هم
    /// هندل می‌کند (باز/بسته کردن).
    /// </summary>
    [RelayCommand]
    private void Navigate(NavItem item)
    {
        if (item.HasChildren)
        {
            item.IsExpanded = !item.IsExpanded;
            return;
        }
        NavigateToPage(item.PageKey);
    }

    /// <summary>
    /// رفتن به صفحه با کلید — چرخه بیست‌ویکم: هایلایت صفحهٔ فعال روی منو
    /// (IsCurrent روی آیتم تکی یا فرزند + باز ماندن گروه والد) نیز تنظیم می‌شود.
    /// چرخه بیست‌وسوم: کلید صفحهٔ جاری نگه داشته می‌شود تا با تغییر ظاهر/واحد پول،
    /// صفحه با اتصال‌های تازه بازسازی شود.
    /// </summary>
    private void NavigateToPage(string pageKey)
    {
        _currentPageKey = pageKey;
        // بازنشانی هایلایت همهٔ آیتم‌ها (تکی و فرزندان گروه‌ها)
        foreach (var nav in NavItems)
        {
            nav.IsCurrent = false;
            foreach (var child in nav.Children)
                child.IsCurrent = false;
        }
        // فعال‌سازی آیتم متناظر + باز ماندن گروهش
        foreach (var nav in NavItems)
        {
            if (!nav.HasChildren)
            {
                if (nav.PageKey == pageKey)
                {
                    nav.IsCurrent = true;
                    break;
                }
                continue;
            }
            var match = nav.Children.FirstOrDefault(c => c.PageKey == pageKey);
            if (match is not null)
            {
                match.IsCurrent = true;
                nav.IsExpanded = true; // گروه والد باز بماند تا محل فعال دیده شود
                break;
            }
        }

        if (!_pages.TryGetValue(pageKey, out var factory))
        {
            // صفحات گام‌های بعد — پیام سازگار
            CurrentTitle = "این ماژول هنوز فعال نشده است";
            return;
        }

        var fresh = factory();
        // چرخه بیست‌وسوم: کارخانه‌ها نمونه‌های تک‌الگو (singleton) برمی‌گردانند؛
        // اگر همان نمونهٔ فعلی باشد، تخصیص مستقیم هیچ نوسازی نمی‌کند — تهی‌گذاری
        // موقت، بازسازی کامل نمای صفحه را وادار می‌کند تا مبالغ با واحد پول/تم
        // تازه دوباره قالب‌بندی شوند (تغییر واحد پول بی‌ری‌استارت اعمال شود)
        if (ReferenceEquals(fresh, CurrentPage))
            CurrentPage = null;
        CurrentPage = fresh;
        CurrentTitle = CurrentPage.Title;
        Breadcrumb = $"خانه > {CurrentTitle}";
    }

    /// <summary>گشودن مرکز آموزش — دکمه 🎓 نوار بالا و آیتم منو</summary>
    [RelayCommand]
    private void OpenTraining() => NavigateToPage("training");

    /// <summary>
    /// نمایش خودکار تور هدایت‌شده در نخستین ورود کاربر روی این ایستگاه —
    /// مارکر محلی per-user بررسی می‌شود؛ نبود مارکر یعنی اولین اجراست.
    /// </summary>
    [RelayCommand]
    private void TryShowGuidedTour()
    {
        try
        {
            var user = _tokens.Username ?? "user";
            var marker = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SahandClinicPro", $"tour-done-{user}.marker");
            if (File.Exists(marker)) return; // پیش‌تر دیده است
        }
        catch
        {
            return; // خطای فایل — تور خودکار نمایش نده
        }

        var owner = System.Windows.Application.Current.MainWindow;
        if (owner is not MainWindow mainWindow) return;
        // چرخهٔ سی‌وهشتم: تور درون‌پنجره‌ای — به‌جای پنجرهٔ همپوشان جدا،
        // در لایهٔ TourLayer خود پنجرهٔ اصلی مستقر می‌شود تا اندازه‌گیری
        // هایلایت در همان درخت بصری انجام شود (رفع ریشهٔ آینه‌شدن RTL)
        var tour = new Views.GuidedTourWindow(owner);
        mainWindow.ShowTourOverlay(tour);
    }

    /// <summary>نسخه غیرهمگام امن برای فراخوانی از سازنده</summary>
    private async Task TryShowGuidedTourAsync()
    {
        // تأخیر کوتاه تا پنجره اصلی و ناوبری کاملاً رندر شوند
        await Task.Delay(900);
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(TryShowGuidedTour);
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

    /// <summary>
    /// تأیید خروج از برنامه + پشتیبان‌گیری اختیاری — چرخهٔ بیست‌وهشتم
    /// (گزارش هجدهم مالک): «برای خروج از برنامه تاییدیه بگیره و در اون
    /// تأییدیه چک باکس پشتیبان‌گیری قبل از خروج هم باشه که اگه فعال کنیم
    /// قبل از خروج پشتیبان بگیره و بعد خارج بشه».
    /// دیالوگ فارسی خود برنامه (قانون مالک — نه کادر ویندوز) نمایش داده
    /// می‌شود؛ اگر چک‌باکس روشن باشد، پیش از بستن، پشتیبان کامل ZIP از
    /// سرور گرفته می‌شود. شکست پشتیبان خروج را نمی‌بندد — انتخاب ادامه
    /// با کاربر است (داده‌ها سمت سرور امن‌اند).
    /// </summary>
    /// <returns>true = ادامهٔ خروج مجاز است</returns>
    public async Task<bool> ConfirmExitAsync()
    {
        var (confirmed, backupRequested) = Views.ThemedDialog.ShowExitConfirm(
            null,
            "خروج از برنامه",
            "آیا از خروج کامل از سامانه اطمینان دارید؟\n" +
            "برای امنیت داده‌ها می‌توانید پیش از خروج یک نسخهٔ پشتیبان کامل تهیه کنید.",
            "پیش از خروج، پشتیبان‌گیری کامل تهیه شود");

        if (confirmed != true)
            return false;

        if (backupRequested)
        {
            // اجرای پشتیبان کامل روی سرور — همان دکمهٔ صفحهٔ پشتیبان‌گیری
            var ok = await _api.PostAsync("backup/run", new { });
            if (ok)
            {
                Views.AppDialog.Success(
                    "پشتیبان‌گیری کامل شد",
                    "نسخهٔ پشتیبان کامل (ZIP) با نام شمسی در پوشهٔ پشتیبان ذخیره شد.\nخروج ادامه می‌یابد — خداحافظ!");
            }
            else
            {
                // شکست پشتیبان (مثلاً نبود مجوز) — ادامه با انتخاب کاربر
                var proceed = Views.AppDialog.Confirm(
                    "پشتیبان‌گیری ناموفق بود",
                    $"پشتیبان‌گیری انجام نشد: {_api.LastError ?? "خطای ناشناخته"}\n\nادامهٔ خروج بدون پشتیبان‌گیری؟",
                    Views.AppDialogIcon.Warning);
                if (proceed != true)
                    return false;
            }
        }

        // قطع اتصال بلادرنگ پیش از خروج — نشست تمیز بسته می‌شود
        await _realtime.StopAsync();
        return true;
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
            // بستن پنجرهٔ قدیمی بدون تأییدیهٔ خروج — این مسیر «خروج از حساب»
            // است نه خروج از برنامه؛ چرخهٔ ۲۸ (توضیح کامل در MainWindow.CloseForLoginSwitch)
            if (oldWindow is MainWindow mainWindow)
                mainWindow.CloseForLoginSwitch();
            else
                oldWindow?.Close();
        });
    }
}
