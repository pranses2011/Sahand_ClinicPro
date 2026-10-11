using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    // ═══ چرخهٔ چهل‌ودوم (گزارش مالک) — پراپرتی Breadcrumb حذف شد ═══
    // «گفته بودم نام صفحه و بج ساعت و زمان رو وسط‌چین بکن ولی تو Breadcrumb
    // رو هم وسط‌چین کردی. اونو پاکش بکن و فقط نام صفحه و بج ساعت وسط باشن» —
    // مسیر «خانه > عنوان» از نوار بالا برداشته شد؛ تنها مصرف‌کننده همان
    // TextBlock گروه میانهٔ MainWindow.xaml بود و با آن یکی شد.

    /// <summary>ساعت شمسی لحظه‌ای — «۱۴۰۴/۰۴/۰۱ - ۱۰:۳۰:۲۵»</summary>
    [ObservableProperty]
    private string _clockText = string.Empty;

    /// <summary>نام کاربر واردشده — نام کامل در صورت وجود وگرنه نام کاربری (چرخهٔ ۳۸)</summary>
    [ObservableProperty]
    private string _currentUser = string.Empty;

    /// <summary>
    /// تصویر کاربر واردشده (بندانگشتی پرسنل متصل) — تهی یعنی عکسی ثبت نشده؛
    /// سایدبار و نوار بالا در نبودش آدمک جای‌گزین می‌کشند. چرخهٔ ۳۸ (گزارش
    /// مالک: «تصویر هر کاربر هم نشون داده بشه») — همان کش/دانلودِ
    /// PhotoTools لیست پزشکان/پرسنل (چرخهٔ ۳۷) این‌جا هم به کار می‌رود.
    /// </summary>
    [ObservableProperty]
    private ImageSource? _currentUserPhoto;

    /// <summary>
    /// نقشهٔ کلید صفحه ← دسترسی لازم — چرخهٔ سی‌وهشتم (گزارش مالک: «فقط منوها
    /// و فرم‌هایی که کاربر دسترسی دارد نمایش داده شود»). کلیدها همان
    /// گیت‌های HasPermission خود کنترلرها هستند تا آنچه منو می‌بیند با آنچه
    /// سرور اجرا می‌کند یکی باشد؛ صفحاتی که سرورشان آزاد است (لایسنس/آموزش)
    /// اینجا هم آزادند.
    /// </summary>
    private static readonly Dictionary<string, string> PagePermissionMap =
        // چرخهٔ ۳۹ — منبع یگانه شد: کلید/مجوزها از PageCatalog می‌آیند (کاتالوگ
        // مشترک منوی کناری + میانبرهای داشبورد)؛ صفحات آزاد (لایسنس/آموزش/
        // اعلان‌ها) مجوز تهی دارند و عمداً بیرون نقشه می‌مانند — همان قرارداد
        // پیشین: «نبودن در نقشه = آزاد». رفتار HasPageFilter یک بایت هم عوض نشده.
        new Dictionary<string, string>(Helpers.PageCatalog.PermissionMap, StringComparer.Ordinal);

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

    /// <summary>
    /// فهرست پیام‌های شناور فعال — چرخهٔ سی‌وهشتم (گزارش مالک: ظاهر توست‌ها
    /// مطابق تصویر نمونه با رنگبندی نوع). میزبان پوسته این مجموعه را با
    /// کپسول‌های گوشه‌گرد نمایش می‌دهد؛ هر پیام پس از زمانی کوتاه خودکار
    /// (یا با کلیک کاربر) کنده می‌شود. جایگزین SnackbarMessageQueue متریال شد.
    /// </summary>
    public ObservableCollection<ToastMessage> Toasts { get; } = new();

    /// <summary>
    /// زمان‌سنج‌های حذف خودکار هر توست — توست رکوردِ مقداری است و کلیدِ همان
    /// در دیکشنری نگه داشته می‌شود تا پس از تیک یا کلیک، هم تایمر بایستد و هم
    /// توست از فهرست کنده شود (نشت تایمر = نشت حافظه).
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<ToastMessage, DispatcherTimer> _toastTimers = new();

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
        // چرخهٔ ۴۱ — صفحهٔ مستقل لوازم مصرفی (بدون نیاز به ماژول داروخانه)
        ConsumablesViewModel consumablesVm,
        VisitsViewModel visitsVm,
        // چرخهٔ بیست‌وهفتم — میزکار پزشک (صفحهٔ ویزیت پزشک)
        DoctorVisitViewModel doctorVisitVm,
        PrescriptionsViewModel prescriptionsVm,
        BillingViewModel billingVm,
        AccountingViewModel accountingVm,
        InsuranceViewModel insuranceVm,
        DentistryViewModel dentistryVm,
        OphthalmologyViewModel ophthalmologyVm,
        PhysiotherapyViewModel physiotherapyVm,
        ImagingViewModel imagingVm,
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
        // چرخهٔ سی‌ونه‌م — صفحهٔ منوی کناری «مدیریت اتاق‌ها» (گزارش مالک)
        RoomsManagerViewModel roomsVm,
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
        // چرخهٔ ۴۴ — مرجع داشبورد برای نوسازی میانبرها/چیدمان در شروع نشست
        _dashboardVm = dashboard;

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

        CurrentUser = _tokens.DisplayName ?? _tokens.Username ?? "کاربر";
        // چرخه هجدهم (گزارش دوازدهم): تاریخ و ساعت نوار بالا با ارقام فارسی
        CurrentDate = PersianDateHelper.ToShamsiFa(DateTime.Now);

        // ساعت لحظه‌ای — هر ثانیه به‌روزرسانی شمسی و ساعت (ارقام فارسی)
        _clock.Tick += (_, _) =>
            ClockText = $"{PersianDateHelper.ToShamsiFa(DateTime.Now)} - {PersianDateHelper.ToClockFa(DateTime.Now)}";
        _clock.Start();
        ClockText = $"{PersianDateHelper.ToShamsiFa(DateTime.Now)} - {PersianDateHelper.ToClockFa(DateTime.Now)}";

        // اتصال سرویس پیام شناور به میزبان کپسول‌های پوسته — چرخهٔ ۳۸
        _snackbar.MessagePosted += EnqueueToast;

        // ═══ چرخهٔ ۴۳ (گزارش مالک: «در داشبورد، کلیدهای میانبر عمل نمی‌کنند و
        // هیچ گزینه‌ای رو باز نمی‌کنند») — ریشه: دکمهٔ مرده برای بار سوم! ═══
        // کاشی‌های میانبر داشبورد (و «رفتن به پذیرش» نوبت‌دهی) از INavigationService
        // استفاده می‌کنند؛ سرویس فقط رویداد NavigationRequested را منتشر می‌کند
        // اما «هیچ‌کس» آن را گوش نمی‌داد — یعنی NavigateTo بی‌اثر بود و کلیک
        // کاشی هیچ نمی‌کرد (الگوی دکمهٔ مردهٔ چرخهٔ ۳۸/۴۱). اکنون پوستهٔ اصلی
        // مشترک این رویداد است و همان ناوبری منوی کناری (NavigateToPage با
        // هایلایت منو) اجرا می‌شود؛ صفحهٔ ناموجود هم پیام سازگار خود را می‌دهد.
        _navigation.NavigationRequested += (_, e) => NavigateToPage(e.PageKey);

        // کارخانه صفحات — صفحات بعدی در گام‌های ۱۳ به بعد اضافه می‌شوند
        _pages = new Dictionary<string, Func<ViewModelBase>>
        {
            ["dashboard"] = () => dashboard,
            ["patients"] = () => patients,
            ["doctors"] = () => doctorsVm,
            ["appointments"] = () => appointments,
            ["receptions"] = () => receptions,
            // چرخهٔ ۴۱ — لوازم مصرفی درمانگاه (صفحهٔ منوی کناری؛ مستقل از داروخانه)
            ["consumables"] = () => consumablesVm,
            ["visits"] = () => visitsVm,
            // چرخهٔ بیست‌وهفتم — میزکار پزشک (گزارش هجدهم مالک)
            ["doctor-visit"] = () => doctorVisitVm,
            ["prescriptions"] = () => prescriptionsVm,
            ["billing"] = () => billingVm,
            ["accounting"] = () => accountingVm,
            ["insurance"] = () => insuranceVm,
            // چرخهٔ ۴۳ — تفکیک پاراکلینیک به چهار صفحهٔ مستقل (گزارش مالک)
            ["dentistry"] = () => dentistryVm,
            ["ophthalmology"] = () => ophthalmologyVm,
            ["physiotherapy"] = () => physiotherapyVm,
            ["imaging"] = () => imagingVm,
            // نگاشت سازگاری: میانبرهای ذخیره‌شدهٔ قدیمی با کلید paraclinical
            // به صفحهٔ تصویربرداری می‌روند (نام قدیمی همان تصویربرداری و پاراکلینیک بود)
            ["paraclinical"] = () => imagingVm,
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
            // چرخهٔ سی‌ونه‌م — صفحهٔ منوی کناری اتاق‌ها
            ["rooms"] = () => roomsVm,
            ["sepas"] = () => sepasVm,
            ["erx"] = () => erxVm,
            ["license"] = () => licenseVm,
            ["complementary"] = () => complementaryVm,
            ["emr-advanced"] = () => advancedEmrVm,
            ["payroll"] = () => payrollVm,
            ["attendance"] = () => attendanceVm,
            ["training"] = () => trainingVm
        };



        // ═══ چرخهٔ چهل‌وچهارم — تفکیک «زیرساخت یک‌باره» از «شروع نشست» ═══
        // گزارش مالک: «با کاربری دیگری وارد میشوم، همچنان کاربر قبلی نشان
        // داده می‌شود» — ریشه: ViewModel تک‌نمونه است و کل راه‌اندازی نشست
        // در سازنده بود که فقط یک‌بار اجرا می‌شود؛ پنجرهٔ اصلیِ جدید همان
        // نمونهٔ کهنه را می‌گرفت و هیچ‌چیز برای کاربر تازه نو نمی‌شد.
        // اکنون سازنده فقط «زیرساختِ عمرِ برنامه» را می‌سازد (اشتراک
        // رویدادها و تایمرها — تکرارشان با هر ورود یعنی انباشت/نشت) و هر
        // چیزی وابسته به «کاربر جاری» در StartSessionAsync است که
        // App.ShowMainWindow با هر نمایش پنجرهٔ اصلی فراخوانی‌اش می‌کند.

        // منو — ساخت خام بدون پالایش؛ پالایش دسترسی در شروع نشست
        RebuildNavItems();

        // اتصال رویداد انقضای نشست — بازگشت به صفحه ورود (یک‌بار برای عمر VM)
        _api.SessionExpired += (_, _) => OnSessionExpired();

        // ═══ اتصال بلادرنگ SignalR — گام R1 (مشترکین یک‌باره) ═══
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

        // اعلان جدید → شمارنده زنگ + پیام شناور خبری (آبی)
        _realtime.NotificationReceived += (_, args) =>
        {
            UnreadCount++;
            _snackbar.Info(args.Title);
        };

        // هشدار سراسری → پیام شناور هشدار (کهربایی — نوع ساخت‌یافتهٔ چرخهٔ ۳۸)
        _realtime.BroadcastAlert += (_, args) =>
            _snackbar.Warning($"{args.Title}: {args.Message}");

        // وضعیت اتصال بلادرنگ در StatusBar
        _realtime.ConnectionStateChanged += (_, _) =>
            ConnectionStatus = _realtime.IsConnected ? "متصل • بلادرنگ فعال" : "متصل";

        // تایمر تازه‌سازی وضعیت لایسنس (۳۰ دقیقه) — یک‌بار ساخته می‌شود؛
        // فراخوانی نخست و بقیهٔ بارگذاری‌های نشست در StartSessionAsync است
        _licenseTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(30)
        };
        _licenseTimer.Tick += (_, _) => _ = LoadLicenseStatusAsync();
        _licenseTimer.Start();
    }

    /// <summary>مرجع داشبورد — برای نوسازی میانبرها/چیدمان در شروع نشست</summary>
    private readonly DashboardViewModel _dashboardVm;

    /// <summary>گارد هم‌زمانی شروع نشست — دو فراخوانی روی هم نمی‌افتند</summary>
    private int _sessionStarting;

    /// <summary>
    /// شروع/نوسازی نشست کاربر جاری — چرخهٔ چهل‌وچهارم (گزارش مالک).
    /// با «هر» نمایش پنجرهٔ اصلی فراخوانی می‌شود (نخستین ورود و هر ورودِ
    /// پس از خروج از حساب)؛ هرچیز وابسته به کاربر جاری اینجا نو می‌شود:
    ///   ۱) نام نمایشی بی‌درنگ از توکن (پیش از رسیدن users/me) + پاک‌شدن
    ///      عکس/شمارندهٔ اعلان کاربر قبلی و توست‌های مانده
    ///   ۲) بازسازی کامل منو از تعریف واحد + پالایش دسترسی کاربر تازه
    ///      (پالایشِ دورهٔ قبل حذفِ برگشت‌ناپذیر بود — بازسازی، وارونش است)
    ///   ۳) ناوبری به نخستین صفحهٔ مجاز (داشبورد یا جایگزین)
    ///   ۴) پروفایل زنده (users/me)، شعبه‌ها و شمار اعلان‌های خوانده‌نشده
    ///   ۵) میانبرها و چیدمان داشبوردِ کاربر تازه (DashboardViewModel
    ///      هم تک‌نمونه است و میانبرهای کاربر قبلی را در خود داشت)
    ///   ۶) اتصال بلادرنگ با توکن تازه (لاگ‌اوت قبلی قطعش کرده بود)
    ///   ۷) وضعیت لایسنس + مارکر تورِ per-user
    /// گارد Interlocked جلوی هم‌زمانی دو شروع را می‌گیرد (مثلاً ShowMainWindow
    /// پشت‌سرهم در گذار گیت لایسنس/ورود).
    /// </summary>
    public async Task StartSessionAsync()
    {
        // شروع دیگری در جریان است — دادهٔ همین فراخوانی در آن ادغام شده است
        if (System.Threading.Interlocked.CompareExchange(ref _sessionStarting, 1, 0) != 0)
            return;
        try
        {
            // ۱) هویت نمایشی تازه بی‌درنگ — کاربر قبلی همین حالا پاک می‌شود؛
            //    عکس/اعلان/توست‌های او هم (پیش از بارگذاری‌های تازه)
            CurrentUser = _tokens.DisplayName ?? _tokens.Username ?? "کاربر";
            CurrentUserPhoto = null;
            UnreadCount = 0;
            foreach (var toast in Toasts.ToList())
                RemoveToast(toast);

            // ۲) منو از تعریف واحد + پالایش دسترسی کاربر جاری
            RebuildNavItems();
            ApplyPermissionFilter();

            // ۳) نخستین صفحهٔ مجاز — کاربر هرگز روی صفحهٔ ممنوع گیر نمی‌کند
            NavigateToPage(HasPageAccess("dashboard") ? "dashboard" : FirstAllowedPageKey());

            // ۴) بارگذاری‌های نشست — شکست هیچ‌کدام جریان را نمی‌شکند
            _ = LoadCurrentUserAsync();
            _ = LoadBranchesAsync();
            _ = RefreshUnreadCountAsync();

            // ۵) داشبورد کاربر تازه — میانبرها (دیتابیس/کش محلی) و چیدمان
            //    کارت‌ها؛ تک‌نمونه بودن DashboardViewModel یعنی بدون این
            //    نوسازی، میانبرهای کارور قبلی نمایش داده می‌شد
            await _dashboardVm.OnSessionStartedAsync();

            // ۶) اتصال بلادرنگ با توکن تازه — لاگ‌اوت قبلی StopAsync کرده بود
            _ = _realtime.StartAsync();

            // ۷) وضعیت لایسنس + مارکر تور per-user (نخستین ورود هر کاربر)
            _ = LoadLicenseStatusAsync();
            Views.TourMarker.CurrentUser = _tokens.Username;
            _ = TryShowGuidedTourAsync();
        }
        finally
        {
            System.Threading.Volatile.Write(ref _sessionStarting, 0);
        }
    }


    /// <summary>
    /// ساخت تعریف کامل منوی کناری — چرخهٔ چهل‌وچهارم.
    /// پیش‌تر این تعریف «درون سازنده» بود و چون ViewModel تک‌نمونه است، فقط
    /// یک‌بار برای نخستین کاربر ساخته می‌شد؛ ApplyPermissionFilter آیتم‌های
    /// بی‌دسترسیِ همان کاربر را «حذف» می‌کرد و پس از ورود کاربر دوم هیچ
    /// راهی برای بازگشت آیتم‌ها نبود (حذفِ انجام‌شده برگشت‌ناپذیر است).
    /// اکنون هر «شروع نشست» منو را از این تعریف پاک می‌سازد و بعد پالایش
    /// دسترسیِ کاربر جاری را اعمال می‌کند — تعریف واحد، سازنده و نشست‌ها.
    /// </summary>
    private ObservableCollection<NavItem> BuildNavMenu()
    {
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

        return new ObservableCollection<NavItem>
        {
            new() { Title = "داشبورد", PageKey = "dashboard", Icon = "ViewDashboard" },

            // ─── جریان اصلی درمان از ورود تا خروج ───
            Group("پذیرش و درمان", "Hospital",
                new() { Title = "بیماران", PageKey = "patients", Icon = "AccountGroup" },
                new() { Title = "نوبت‌دهی", PageKey = "appointments", Icon = "CalendarClock" },
                new() { Title = "پذیرش", PageKey = "receptions", Icon = "ClipboardAccount" },
                // چرخهٔ ۴۱ (گزارش مالک) — «درمانگاه بدون ماژول داروخانه هم باید
                // بتواند لوازم مصرفی را تعریف و استفاده کند»: صفحهٔ مستقل مصرفی‌ها
                // در گروه پذیرش و درمان (نه نسخه و دارو) تا جایگاهش در جریان درمان روشن باشد
                new() { Title = "لوازم مصرفی", PageKey = "consumables", Icon = "Syringe" },
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
                new() { Title = "دندانپزشکی", PageKey = "dentistry", Icon = "ToothOutline" },
                new() { Title = "چشم‌پزشکی", PageKey = "ophthalmology", Icon = "EyeCircle" },
                new() { Title = "فیزیوتراپی", PageKey = "physiotherapy", Icon = "DirectionsRun" },
                new() { Title = "تصویربرداری", PageKey = "imaging", Icon = "CameraBurst" }),

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
                // چرخهٔ سی‌ونه‌م (گزارش مالک): «مدیریت اتاق ها رو در منوی کناری
                // هم قرار بده» — پیش‌تر فقط از تب برنامهٔ کاری فرم پزشک باز می‌شد
                new() { Title = "مدیریت اتاق‌ها", PageKey = "rooms", Icon = "Door" },
                new() { Title = "پشتیبان‌گیری و بازیابی", PageKey = "backup", Icon = "DatabaseSync" },
                new() { Title = "لاگ عملیات", PageKey = "audit", Icon = "FileDocumentEditOutline" },
                new() { Title = "لایسنس", PageKey = "license", Icon = "KeyChain" },
                new() { Title = "قالب چاپ و نظرسنجی", PageKey = "complementary", Icon = "TuneVertical" }),

            // ─── آموزش ───
            new() { Title = "🎓 مرکز آموزش", PageKey = "training", Icon = "School" }
        };
    }

    /// <summary>
    /// بازسازی درون‌جای منو — چرخهٔ ۴۴: نمونهٔ مجموعه دست‌نخورده می‌ماند
    /// (بایندینگ ItemsSource منوی کناری به همین نمونه وصل است) و فقط
    /// محتوایش از تعریف واحد تازه می‌شود؛ سپس پالایش دسترسی کاربر جاری.
    /// </summary>
    private void RebuildNavItems()
    {
        NavItems.Clear();
        foreach (var item in BuildNavMenu())
            NavItems.Add(item);
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
                _snackbar.Info($"سرور لایسنس: {check.Message}");
        }
        catch
        {
            // قطعی لحظه‌ای سرور — نوار وضعیت دست‌نخورده می‌ماند (fail-open)
        }
    }

    /// <summary>
    /// فهرست آیتم‌های ناوبری — نمونهٔ ثابتِ درون‌خطی (چرخهٔ ۴۴): بایندینگ
    /// ItemsSource منوی کناری به همین نمونه وصل است و RebuildNavItems محتوایش
    /// را درون‌جای پاک/بازسازی می‌کند (تعویض نمونه لازم نیست).
    /// </summary>
    public ObservableCollection<NavItem> NavItems { get; } = new();

    /// <summary>
    /// آیا کاربر جاری به این صفحه دسترسی دارد؟ — ادمین همه‌چیز؛ صفحهٔ بدون
    /// گیت آزاد؛ در غیر این صورت دسترسی اتمی متناظر در فهرست نقش‌های توکن
    /// جست‌وجو می‌شود (چرخهٔ ۳۸).
    /// </summary>
    private bool HasPageAccess(string pageKey)
    {
        if (_tokens.IsAdmin) return true;
        if (!PagePermissionMap.TryGetValue(pageKey, out var permission)) return true;
        return _tokens.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// پالایش منو با سطح دسترسی — چرخهٔ ۳۸: آیتم‌های تکی بدون دسترسی حذف
    /// می‌شوند؛ از فرزندان گروه‌ها فقط بی‌دسترسی‌ها کنده می‌شود و گروهِ خالی‌شده
    /// کل حذف می‌شود. برای ادمین هیچ کاری انجام نمی‌شود (همه‌چیز مجاز است).
    /// متد خودتوان (idempotent) است — فراخوانی دوباره پس از به‌روزشدن
    /// دسترسی‌ها از users/me بی‌ضرر است چون آیتم حذف‌شده دیگر در فهرست نیست.
    /// </summary>
    private void ApplyPermissionFilter()
    {
        if (_tokens.IsAdmin) return;

        var toRemove = new List<NavItem>();
        foreach (var nav in NavItems)
        {
            if (nav.HasChildren)
            {
                var blockedChildren = nav.Children
                    .Where(c => !HasPageAccess(c.PageKey))
                    .ToList();
                foreach (var child in blockedChildren)
                    nav.Children.Remove(child);
                if (nav.Children.Count == 0)
                    toRemove.Add(nav); // گروهی که همهٔ فرزندانش بی‌دسترسی بودند
            }
            else if (!HasPageAccess(nav.PageKey))
            {
                toRemove.Add(nav);
            }
        }
        foreach (var item in toRemove)
            NavItems.Remove(item);
    }

    /// <summary>
    /// نخستین کلید صفحهٔ مجاز برای ناوبری اولیه — وقتی داشبورد در دسترس
    /// نیست (کاربر نیمه‌مجاز). مرکز آموزش همیشه آزاد است پس هرگز نتیجه‌ای
    /// خالی نمی‌ماند؛ خروجی تهی هم برای اطمینان به داشبورد برمی‌گردد.
    /// </summary>
    private string FirstAllowedPageKey()
    {
        foreach (var nav in NavItems)
        {
            if (nav.HasChildren)
            {
                var child = nav.Children.FirstOrDefault(c => HasPageAccess(c.PageKey));
                if (child is not null) return child.PageKey;
            }
            else if (HasPageAccess(nav.PageKey))
            {
                return nav.PageKey;
            }
        }
        return "dashboard"; // فهرست خالی/بی‌دسترسی — همان رفتار پیشین
    }

    /// <summary>
    /// بارگذاری پروفایل زندهٔ کاربر — چرخهٔ ۳۸ (گزارش مالک):
    /// «اگر نام کامل وجود داشته باشد همان نوشته شود و در صورت نبودش نام
    /// کاربری» + «تصویر هر کاربر هم نمایش داده شود».
    /// از users/me: نام نمایشی و نقش/دسترسی‌های تازه (اگر مدیر نقش‌ها را
    /// عوض کرده باشد بی‌درنگ منو هم‌قدم می‌شود) + عکس پرسنل متصل با همان
    /// زیرساخت کش بندانگشتی چرخهٔ ۳۷. هر خطایی بی‌صرفه ادامه می‌یابد و
    /// دادهٔ ذخیره‌شدهٔ لحظهٔ ورود می‌ماند.
    /// </summary>
    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var response = await _api.GetAsync<CurrentUserResponse>("users/me");
            var me = response?.Data;
            if (me is not null)
            {
                // نام کامل در صورت وجود — وگرنه نام کاربری (قانون مالک)
                CurrentUser = !string.IsNullOrWhiteSpace(me.DisplayName)
                    ? me.DisplayName!.Trim()
                    : (string.IsNullOrWhiteSpace(me.Username) ? CurrentUser : me.Username!);

                // همگام‌سازی دسترسی‌های زندهٔ نشست + بازپالایش منو
                _tokens.UpdateProfile(me.DisplayName, me.Roles, me.Permissions);
                ApplyPermissionFilter();

                // تصویر کاربر — بندانگشتی پرسنل متصل (کش مشترک با لیست‌ها)
                if (me.EmployeeId is { } employeeId)
                {
                    var cacheKey = PhotoTools.EmployeeThumbnailKey(employeeId);
                    if (PhotoTools.GetCachedThumbnail(cacheKey) is { } cached)
                    {
                        CurrentUserPhoto = cached;
                        return;
                    }
                    var bytes = await PhotoTools.DownloadEmployeePhotoAsync(_api, employeeId);
                    var thumb = PhotoTools.ToThumbnail(bytes, 72);
                    if (thumb is not null)
                    {
                        PhotoTools.CacheThumbnail(cacheKey, thumb);
                        CurrentUserPhoto = thumb;
                    }
                }
            }
            else
            {
                // پاسخ ناموفق — دادهٔ لحظهٔ ورود محلی (بهتر از هیچ)
                CurrentUser = _tokens.DisplayName ?? _tokens.Username ?? CurrentUser;
            }
        }
        catch
        {
            // قطعی گذرا — وضعیت موجود می‌ماند؛ پروفایل جانبی است
        }
    }

    /// <summary>پاسخ اندپوینت users/me — پاکت استاندارد</summary>
    private sealed class CurrentUserResponse
    {
        public bool IsSuccess { get; set; }
        public CurrentUserDto? Data { get; set; }
    }

    /// <summary>داده‌های کاربر جاری — نام نمایشی/پرسنل متصل/نقش‌ها/دسترسی‌ها</summary>
    private sealed class CurrentUserDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public long? EmployeeId { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? Permissions { get; set; }
    }

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
        // چرخهٔ ۴۲: Breadcrumb («خانه > عنوان») به درخواست مالک از نوار بالا
        // حذف شد — وسط نوار فقط نام صفحه و بج ساعت است؛ مقداردهی اضافه حذف شد
    }

    /// <summary>گشودن مرکز آموزش — دکمه 🎓 نوار بالا و آیتم منو</summary>
    [RelayCommand]
    private void OpenTraining() => NavigateToPage("training");

    /* ═══ میزبان پیام‌های شناور — چرخهٔ سی‌وهشتم (گزارش مالک: ظاهر مطابق
       تصویر نمونهٔ ToastNotif.png با رنگبندی نوع) ═══ */

    /// <summary>
    /// افزودن توست به فهرست نمایش — حداکثر ۳ توست هم‌زمان (قدیمی‌ترین
    /// کنده می‌شود تا پشت‌هم انبار نشوند) + زمان‌سنج حذف خودکار:
    /// موفقیت/خبری ۴ ثانیه؛ خطا/هشدار ۶٫۵ ثانیه (خواندن پیام بلندتر).
    /// </summary>
    private void EnqueueToast(ToastMessage toast)
    {
        // سقف هم‌زمانی — قدیمی‌ترین بی‌سروصدا حذف می‌شود
        while (Toasts.Count >= 3)
            RemoveToast(Toasts[0]);

        Toasts.Add(toast);

        // زمان‌سنج حذف خودکار — خطا/هشدار بیشتر می‌مانند (فرصت خواندن)
        var duration = toast.Kind is ToastKind.Error or ToastKind.Warning
            ? TimeSpan.FromSeconds(6.5)
            : TimeSpan.FromSeconds(4);
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) => RemoveToast(toast);
        timer.Start();
        _toastTimers[toast] = timer;
    }

    /// <summary>
    /// بستن توست با کلیک کاربر — تایمرش هم بایستد تا نشت نشود؛ رکورد مقداری
    /// است پس کلید همان نمونهٔ اصلی است.
    /// </summary>
    [RelayCommand]
    private void DismissToast(ToastMessage? toast)
    {
        if (toast is not null)
            RemoveToast(toast);
    }

    /// <summary>کنده‌شدن توست از فهرست + توقف تایمرش</summary>
    private void RemoveToast(ToastMessage toast)
    {
        if (_toastTimers.Remove(toast, out var timer))
            timer.Stop();
        Toasts.Remove(toast);
    }

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

    /// <summary>
    /// بازگشت به صفحه ورود — بستن پنجره فعلی و باز کردن ورود
    /// ═══ چرخهٔ چهل‌ودوم (گزارش مالک) ═══
    /// گزارش: خطای «خروج از حساب» — InvalidOperationException («Cannot call
    /// Show… after a Window has closed»). ریشه: LoginWindow در DI تک‌نمونه بود
    /// و پس از لاگین موفق Close واقعی می‌شد؛ اینجا همان نمونهٔ بسته Show می‌شد.
    /// رفع: نمایش ورود از App.ShowLoginWindow می‌گذرد — نخست پنجرهٔ زندهٔ موجود
    /// را می‌یابد (تعویض نشست دوباره‌کاری نمی‌سازد) و فقط در نبودش نمونهٔ نو
    /// از DI (اکنون Transient) می‌سازد؛ نمونهٔ بسته هرگز دیگر Show نمی‌شود.
    /// </summary>
    private void OnSessionExpired()
    {
        // قطع اتصال بلادرنگ — نشست دیگر معتبر نیست (گام R1)
        _ = _realtime.StopAsync();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // پنجرهٔ فعلی را «پیش از» نمایش ورود برمی‌داریم چون ShowLoginWindow
            // خودش MainWindow را روی پنجرهٔ ورود ست می‌کند و مرجع قبلی از دست می‌رود
            var oldWindow = System.Windows.Application.Current.MainWindow;
            App.ShowLoginWindow();
            // بستن پنجرهٔ قدیمی بدون تأییدیهٔ خروج — این مسیر «خروج از حساب»
            // است نه خروج از برنامه؛ چرخهٔ ۲۸ (توضیح کامل در MainWindow.CloseForLoginSwitch)
            if (oldWindow is MainWindow mainWindow)
                mainWindow.CloseForLoginSwitch();
            else
                oldWindow?.Close();
        });
    }
}
