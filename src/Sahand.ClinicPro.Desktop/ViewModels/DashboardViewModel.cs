using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف لیست نوبت‌های پیش‌رو</summary>
public record UpcomingAppointmentRow(
    long Id, int? QueueNumber, string Time, string PatientName,
    string DoctorName, string StatusText, string Status);

/// <summary>ردیف جدول فعالیت‌های اخیر</summary>
public record RecentActivityRow(string TimeShamsi, string User, string Action, string Entity);

/// <summary>
/// ردیف مرکز هشدار انبار — بچ در حال انقضا (چرخهٔ ۶۹ / فاز ۳ F-08)
/// پلهٔ رنگی بر اساس روزهای مانده در ViewModel محاسبه و روی UI اعمال می‌شود.
/// </summary>
public record InventoryAlertRow(
    string MedicationName, string? BatchNumber, string ExpiryDateShamsi,
    int DaysLeft, string QuantityText, string TierBrush, string TierText);

/// <summary>ردیف مرکز هشدار — قلم زیر حداقل موجودی (چرخهٔ ۶۹ / F-08)</summary>
public record LowStockAlertRow(
    string MedicationName, string? BatchNumber, string QuantityText, string MinStockText);

/// <summary>کارت آمار داینامیک داشبورد — قابل چیدمان (چرخه ۱۳، درخواست ۴-۲)</summary>
public partial class DashboardCardVM : ObservableObject
{
    /// <summary>کلید کارت — مبنای ذخیره ترتیب</summary>
    public string Key { get; init; } = "";

    /// <summary>آیکن PackIcon</summary>
    public string IconKind { get; init; } = "AccountGroup";

    /// <summary>مقدار نمایشی</summary>
    [ObservableProperty]
    private string _value = "…";

    /// <summary>عنوان</summary>
    public string Label { get; init; } = "";

    /// <summary>متن مقایسه</summary>
    public string CompareText { get; init; } = "";

    /// <summary>رنگ تأکیدی</summary>
    public string Accent { get; init; } = "#1E88E5";

    /// <summary>نمایش کارت</summary>
    [ObservableProperty]
    private bool _visible = true;

    partial void OnVisibleChanged(bool value) => VisibleChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>رویداد تغییر نمایش — برای ذخیره خودکار چیدمان</summary>
    public event EventHandler? VisibleChanged;
}

/// <summary>
/// بخش قابل‌چیدمان داشبورد — چرخهٔ ۷۴ (گزارش مالک: «کنترل کامل و حرفه‌ای
/// با کشیدن‌ورهاکردن») — یک بلوک کامل داشبورد (نمودارها/ویزیت‌ها/فعالیت‌ها/
/// میانبرها) با کلید قالب، عنوان برای دستگیرهٔ درگ و پرچم نمایش.
/// </summary>
public partial class DashboardSectionItem : ObservableObject
{
    /// <summary>سازنده</summary>
    public DashboardSectionItem(string key, string title, string icon)
    {
        Key = key;
        Title = title;
        Icon = icon;
    }

    /// <summary>کلید بخش — انتخاب قالب و مبنای ذخیره ترتیب</summary>
    public string Key { get; }

    /// <summary>عنوان روی دستگیرهٔ درگ</summary>
    public string Title { get; }

    /// <summary>آیکن PackIcon دستگیره</summary>
    public string Icon { get; }

    /// <summary>نمایش بخش</summary>
    [ObservableProperty]
    private bool _visible = true;

    partial void OnVisibleChanged(bool value) => VisibleChanged?.Invoke();

    /// <summary>رویداد تغییر نمایش — برای ذخیرهٔ خودکار چیدمان (بدون آرگومان؛ سبک)</summary>
    public event Action? VisibleChanged;
}

/// <summary>
/// ویومدل داشبورد — گام U1 طبق پرامپت UI بخش ۱ (فرم ۲):
/// ۴ کارت آمار اصلی + ۳ کارت فرعی + نمودار درآمد + نوبت‌های پیش‌رو + فعالیت‌های اخیر
/// به‌روزرسانی خودکار هر ۶۰ ثانیه (Timer)
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — میانبرهای شخصی داشبورد ═══
    // «میخوام در داشبورد… دکمه‌های میانبر برای قسمت‌های مختلف… با کلیک مستقیم
    // اون فرم باز بشه… برای هر کاربر مختص خودش ذخیره بشه» — ناوبری از
    // INavigationService؛ چرخهٔ ۴۱ (گزارش مالک: «در دیتابیس ذخیره بشه تا
    // مشکلی براش پیش نیاد»): حقیقت مرجع به جدول UserShortcuts سرور منتقل شد
    // (اندپوینت‌های users/me/shortcuts) تا با تعویض ایستگاه/پروفایل ویندوز
    // از بین نرود و در پشتیبان سرور هم پوشش داده شود؛ فایل محلی فقط کشِ
    // آفلاین + منبع مهاجرتِ یک‌بارهٔ چیدمان‌های قدیمی است.
    private readonly ITokenManager _tokens;
    private readonly INavigationService _navigation;

    /// <summary>
    /// آیا ذخیرهٔ کاربر بر بارگذاریِ در حال پرواز از سرور پیشی گرفته؟ — گاردِ
    /// مسابقه: PUT تازه‌تر از GET است و پاسخ کهنه نباید چیدمانِ همین لحظهٔ
    /// کاربر را بازنویسی کند (چرخهٔ ۴۱).
    /// </summary>
    private bool _shortcutsSaveWonRace;

    /// <summary>
    /// مسیر فایل کش محلی میانبرهای این کاربر — پوشهٔ %AppData%/SahandClinicPro؛
    /// از چرخهٔ ۴۱ فقط نقش «کش آفلاین + منبع مهاجرت یک‌باره» را دارد و مرجعِ
    /// اصلی رکورد دیتابیسی همین کاربر روی سرور است.
    /// </summary>
    private string ShortcutsFilePath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sahand_ClinicPro",
        $"shortcuts.{_tokens.Username ?? "user"}.json");
    private readonly System.Windows.Threading.DispatcherTimer _refresh =
        new() { Interval = TimeSpan.FromSeconds(60) };

    /// <summary>عنوان صفحه در هدر</summary>
    public override string Title => "داشبورد";

    /* ── آمار اصلی (ردیف اول) ── */
    [ObservableProperty] private int _todayPatients;
    [ObservableProperty] private int _todayVisits;
    [ObservableProperty] private int _waitingCount;
    [ObservableProperty] private string _todayRevenueText = "0";

    /* ── چرخهٔ ۷۲ — بازطراحی داشبورد: بیمار جدید امروز (داده‌ای که سرور
       از قبل می‌فرستاد ولی کارتی نداشت) ── */
    [ObservableProperty] private int _newPatientsToday;

    /* ── چرخهٔ ۷۲ — نمودار ویزیت روزانه (ستونی) و ترکیب وضعیت نوبت‌ها (دونات) ── */

    /// <summary>نمودار ستونی ویزیت روزانه — از dashboard/visits-chart</summary>
    public ObservableCollection<ChartPointData> VisitsChart { get; } = new();

    /// <summary>نمودار دونات ترکیب وضعیت نوبت‌های امروز — از dashboard/status-mix</summary>
    public ObservableCollection<ChartPointData> StatusMix { get; } = new();

    /// <summary>متن مرکز دونات — جمع نوبت‌های امروز</summary>
    [ObservableProperty]
    private string _statusMixTotalText = "۰";

    /// <summary>زیرنویس مرکز دونات</summary>
    [ObservableProperty]
    private string _statusMixSubText = "نوبت امروز";

    /// <summary>گزینه‌های بازهٔ نمودار درآمد — چرخهٔ ۷۲</summary>
    public int[] RevenueDaysOptions { get; } = { 7, 14, 30 };

    /// <summary>بازهٔ منتخب نمودار درآمد — با تغییر، نمودار تازه‌سازی می‌شود</summary>
    [ObservableProperty]
    private int _selectedRevenueDays = 7;

    /* ── سوییچ‌های چیپ بازهٔ نمودار — RadioButton دوطرفه روی سه بولین مشتق ── */

    /// <summary>بازهٔ ۷ روزه فعال است؟</summary>
    public bool IsRevenue7
    {
        get => SelectedRevenueDays == 7;
        set { if (value) SelectedRevenueDays = 7; }
    }

    /// <summary>بازهٔ ۱۴ روزه فعال است؟</summary>
    public bool IsRevenue14
    {
        get => SelectedRevenueDays == 14;
        set { if (value) SelectedRevenueDays = 14; }
    }

    /// <summary>بازهٔ ۳۰ روزه فعال است؟</summary>
    public bool IsRevenue30
    {
        get => SelectedRevenueDays == 30;
        set { if (value) SelectedRevenueDays = 30; }
    }

    /// <summary>تغییر بازهٔ نمودار درآمد → اعلان سوییچ‌ها + بارگذاری مجدد نمودار</summary>
    partial void OnSelectedRevenueDaysChanged(int value)
    {
        OnPropertyChanged(nameof(IsRevenue7));
        OnPropertyChanged(nameof(IsRevenue14));
        OnPropertyChanged(nameof(IsRevenue30));
        _ = LoadRevenueChartAsync();
    }

    /* ── آمار فرعی (ردیف دوم) ── */
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _todayPrescriptions;
    [ObservableProperty] private int _pendingLabOrders;

    /* ── مرکز هشدار انقضای پلکانی — چرخهٔ ۶۹ (فاز ۳ / F-08) ──
       چهار پلهٔ ۹۰/۶۰/۳۰/۷ روز + موجودی کم؛ پله‌ها جمع تجمعی هستند
       (expiring30 شامل expiring7 هم هست) ولی «قد» هر پله جدا نمایش داده
       می‌شود تا اولویت عملیاتی از رنگ پیداباشد: قرمز=۷، نارنجی=۳۰،
       کهربایی=۶۰، زرد=۹۰. */
    [ObservableProperty] private int _expiring7Count;
    [ObservableProperty] private int _expiring30Count;
    [ObservableProperty] private int _expiring60Count;
    [ObservableProperty] private int _expiring90Count;

    /// <summary>مرکز هشدار باز است؟ (پرش انیمیشنی فهرست اقلام)</summary>
    [ObservableProperty] private bool _isAlertCenterOpen;

    /// <summary>هیچ هشدار انباری نیست؟ (کارت جمع می‌شود)</summary>
    [ObservableProperty] private bool _hasNoInventoryAlerts = true;

    /// <summary>
    /// هیچ هشداری برای مرکز هشدار نیست؟ — چرخهٔ ۷۲: «آزمایش در انتظار» هم
    /// وضعیت هشداری است؛ کارت وقتی «نه انباری و نه آزمایشِ در انتظاری»
    /// نباشد جمع می‌شود.
    /// </summary>
    public bool HasNoAlertsAtAll => HasNoInventoryAlerts && PendingLabOrders == 0;

    /// <summary>اعلان تغییر وضعیت مرکز هشدار با تغییر منابع انبار</summary>
    partial void OnHasNoInventoryAlertsChanged(bool value) => OnPropertyChanged(nameof(HasNoAlertsAtAll));

    /// <summary>اعلان تغییر وضعیت مرکز هشدار با تغییر آزمایش‌های در انتظار</summary>
    partial void OnPendingLabOrdersChanged(int value) => OnPropertyChanged(nameof(HasNoAlertsAtAll));

    /// <summary>اقلام در حال انقضا — نزدیک‌ترین اول</summary>
    public ObservableCollection<InventoryAlertRow> ExpiringItems { get; } = new();

    /// <summary>اقلام زیر حداقل موجودی — بدترین وضعیت اول</summary>
    public ObservableCollection<LowStockAlertRow> LowStockItems { get; } = new();

    /* ── عمومی ── */
    [ObservableProperty] private string _welcomeMessage = string.Empty;
    [ObservableProperty] private string _todayDateShamsi = string.Empty;
    [ObservableProperty] private string _connectionStatus = "در حال بارگذاری…";
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>داده نمودار درآمد ۷ روز — نمودار سطحی (نوع درست برای روند زمانی)</summary>
    public ObservableCollection<ChartPointData> RevenueChart { get; } = new();

    /// <summary>نوبت‌های پیش‌رو امروز</summary>
    public ObservableCollection<UpcomingAppointmentRow> UpcomingAppointments { get; } = new();

    /// <summary>آخرین فعالیت‌ها</summary>
    public ObservableCollection<RecentActivityRow> RecentActivities { get; } = new();

    /* ── چرخه دوازدهم: پیام‌های مستقیم پشتیبانی — با تپ‌قلب از سرور لایسنس
       تحویل و در SystemSettings ذخیره می‌شوند؛ اینجا خوانده و در کارت نمایش می‌یابند ── */

    /// <summary>آیا پیام پشتیبانی برای نمایش هست؟</summary>
    [ObservableProperty]
    private bool _hasSupportMessages;

    /// <summary>پیام‌های پشتیبانی دریافتی از پنل لایسنس — جدیدترین اول</summary>
    public ObservableCollection<SupportMessageDto> SupportMessages { get; } = new();

    /* ── چرخه ۱۳ (درخواست ۴-۲): داشبورد شخصی‌سازی‌شده ── */

    /// <summary>کارت‌های اصلی داینامیک — ترتیب قابل جابه‌جایی</summary>
    public ObservableCollection<DashboardCardVM> MainCards { get; } = new();

    /// <summary>حالت شخصی‌سازی فعال است؟</summary>
    [ObservableProperty]
    private bool _isCustomizing;

    /// <summary>نمایش ردیف کارت‌های فرعی</summary>
    [ObservableProperty]
    private bool _showSecondaryCards = true;

    /// <summary>نمایش نمودار درآمد</summary>
    [ObservableProperty]
    private bool _showRevenueChart = true;

    /// <summary>نمایش کارت نوبت‌های پیش‌رو</summary>
    [ObservableProperty]
    private bool _showUpcoming = true;

    /// <summary>نمایش فعالیت‌های اخیر</summary>
    [ObservableProperty]
    private bool _showActivities = true;

    // ═══ چرخهٔ ۷۴ — بخش‌های قابل‌چیدمان داشبورد (گزارش مالک) ═══
    // «مدیریت چیدمان داشبورد خیلی سطحی و ناقص هست و فقط چند کارت رو مخفی و
    // یا آشکار میکنه. میخوام طوری پیاده سازیش بکنی که بصورت Drag & Drop باشه
    // و بتونم کنترل کامل و حرفه‌ای روی داشبورد داشته باشم و همه چیزش رو کنترل
    // بکنم» — چهار بلوک اصلی داشبورد (نمودارها / ویزیت‌ها و نوبت‌ها / فعالیت‌ها
    // / میانبرها) به‌صورت آیتم‌های قابل‌کشیدن مدل شدند؛ ترتیب و نمایش هر بخش
    // در همان JSON چیدمان per-user ذخیره می‌شود. کارت‌های KPI هم با درگ
    // جابه‌جا می‌شوند (CollectionChanged → ذخیرهٔ خودکار).

    /// <summary>بخش‌های قابل‌چیدمان — ترتیب همین ترتیبِ درگ‌شده است</summary>
    public ObservableCollection<DashboardSectionItem> Sections { get; } = new();

    // ═══ چرخهٔ ۷۶ — مدیریت درست ذخیرهٔ چیدمان ═══
    // گزارش مالک: PUTهای پشت‌سرهم /api/v1/dashboard/layout روی سرور
    // DbUpdateConcurrencyException می‌دادند. دو عامل سمت کلاینت:
    // ۱) بارگذاری چیدمان (Clear/Add/Visible) خودش ده‌ها رویداد «ذخیرهٔ خودکار»
    //    شعله‌ور می‌کرد — گارد _suppressLayoutSave آن را خاموش می‌کند؛
    // ۲) هر تغییر جزئی یک PUT فوری بود — زمان‌سنج ادغام (Debounce) همهٔ
    //    تغییرات ۸۰۰ میلی‌ثانیه‌ای را در «یک PUT آخرین‌وضعیت» ادغام می‌کند و
    //    ذخیره‌ها صف‌بندی می‌شوند تا دو PUT هم‌پوشان هرگز در پرواز نباشند.
    /// <summary>گارد خاموش‌کردن ذخیرهٔ خودکار هنگام اعمال برنامه‌ای چیدمان (بارگذاری)</summary>
    private bool _suppressLayoutSave;

    /// <summary>زمان‌سنج ادغام ذخیره‌های چیدمان — هر تغییر تازه تایمر را ریست می‌کند</summary>
    private System.Threading.CancellationTokenSource? _layoutSaveDebounce;

    /// <summary>پرچم «ذخیره‌ای در پرواز است» — صف‌بندی PUTهای پیاپی</summary>
    private volatile bool _layoutSaveInFlight;

    /// <summary>ترتیب پیش‌فرض بخش‌ها</summary>
    private static readonly string[] DefaultSectionOrder =
        { "shortcuts", "charts", "visits", "activities" };

    /// <summary>ساخت بخش‌های قابل‌چیدمان — ترتیب و وضعیت از چیدمان ذخیره‌شده اعمال می‌شود</summary>
    private void InitializeSections()
    {
        Sections.Clear();
        Sections.Add(new DashboardSectionItem("shortcuts", "میانبرهای سریع", "LightningBolt"));
        Sections.Add(new DashboardSectionItem("charts", "نمودارها", "ChartAreaspline"));
        Sections.Add(new DashboardSectionItem("visits", "ویزیت‌ها و نوبت‌های پیش‌رو", "ChartBar"));
        Sections.Add(new DashboardSectionItem("activities", "فعالیت‌های اخیر", "History"));
        // ذخیرهٔ خودکار پس از هر جابه‌جایی درگ/دکمه
        Sections.CollectionChanged += (_, _) => ScheduleSaveLayout();
        // ذخیرهٔ خودکار تغییر نمایش هر بخش
        foreach (var section in Sections)
            section.VisibleChanged += () => ScheduleSaveLayout();
    }

    /// <summary>بازگشت به چیدمان پیش‌فرض</summary>
    [RelayCommand]
    private void ResetLayout()
    {
        foreach (var c in MainCards) c.Visible = true;
        ShowSecondaryCards = true;
        ShowRevenueChart = true;
        ShowUpcoming = true;
        ShowActivities = true;
        // ترتیب پیش‌فرض کارت‌ها
        var ordered = MainCards.OrderBy(c => Array.IndexOf(DefaultOrder, c.Key)).ToList();
        MainCards.Clear();
        foreach (var c in ordered) MainCards.Add(c);
        // ترتیب و نمایش پیش‌فرض بخش‌ها — چرخهٔ ۷۴
        foreach (var s in Sections) s.Visible = true;
        var orderedSections = Sections
            .OrderBy(s => Array.IndexOf(DefaultSectionOrder, s.Key)).ToList();
        Sections.Clear();
        foreach (var s in orderedSections) Sections.Add(s);
        ScheduleSaveLayout();
        _snackbar.Success("چیدمان به حالت پیش‌فرض بازگشت");
    }

    /// <summary>ترتیب پیش‌فرض کارت‌ها — چرخهٔ ۷۲: بیمار جدید بین بیماران امروز و ویزیت‌ها</summary>
    private static readonly string[] DefaultOrder =
        { "patients", "newPatients", "visits", "waiting", "revenue" };

    partial void OnShowSecondaryCardsChanged(bool value) => ScheduleSaveLayout();
    partial void OnShowRevenueChartChanged(bool value) => ScheduleSaveLayout();
    partial void OnShowUpcomingChanged(bool value) => ScheduleSaveLayout();
    partial void OnShowActivitiesChanged(bool value) => ScheduleSaveLayout();

    /// <summary>راه‌اندازی کارت‌های داینامیک — چرخهٔ ۷۲: پنج کارت با پالت هماهنگ</summary>
    private void InitializeCards()
    {
        MainCards.Clear();
        MainCards.Add(new DashboardCardVM
        {
            Key = "patients", IconKind = "AccountGroup", Label = "نوبت‌های امروز",
            CompareText = "مجموع نوبت‌های ثبت‌شده امروز", Accent = "#1565C0"
        });
        MainCards.Add(new DashboardCardVM
        {
            Key = "newPatients", IconKind = "AccountPlus", Label = "بیمار جدید",
            CompareText = "پرونده‌های ثبت‌شده امروز", Accent = "#00897B"
        });
        MainCards.Add(new DashboardCardVM
        {
            Key = "visits", IconKind = "Stethoscope", Label = "ویزیت‌شده",
            CompareText = "ویزیت‌های انجام‌شده امروز", Accent = "#2E7D32"
        });
        MainCards.Add(new DashboardCardVM
        {
            Key = "waiting", IconKind = "ClockOutline", Label = "در انتظار",
            CompareText = "در صف انتظار باقی‌مانده", Accent = "#E65100"
        });
        MainCards.Add(new DashboardCardVM
        {
            // چرخهٔ بیست‌وهفتم: واحد پول پویا از تنظیمات (ریال/تومان) — حذف هاردکد
            Key = "revenue", IconKind = "CashMultiple", Label = $"درآمد امروز ({Money.UnitLabel})",
            CompareText = "مجموع پرداخت‌های ثبت‌شده", Accent = "#6A1B9A"
        });
        foreach (var c in MainCards)
            c.VisibleChanged += (_, _) => ScheduleSaveLayout();
        // ═══ چرخهٔ ۷۴ — ذخیرهٔ خودکار پس از جابه‌جایی درگ‌و‌دراپی کارت‌ها ═══
        MainCards.CollectionChanged += (_, _) => ScheduleSaveLayout();
    }

    /// <summary>
    /// پنهان/آشکار کردن یک بخش — دکمهٔ چشم روی دستگیرهٔ درگ (چرخهٔ ۷۴)
    /// </summary>
    [RelayCommand]
    private void ToggleSectionVisibility(DashboardSectionItem? section)
    {
        if (section is null) return;
        section.Visible = !section.Visible; // مسیر رویداد → ذخیرهٔ خودکار
    }

    /// <summary>به‌روزرسانی مقادیر کارت‌ها از آمار — چرخهٔ ۷۲: ارقام فارسی + کارت پنجم «بیمار جدید»</summary>
    private void UpdateCardValues()
    {
        foreach (var c in MainCards)
            c.Value = c.Key switch
            {
                "patients" => Helpers.Money.ToPersianDigits(TodayPatients.ToString()),
                "newPatients" => Helpers.Money.ToPersianDigits(NewPatientsToday.ToString()),
                "visits" => Helpers.Money.ToPersianDigits(TodayVisits.ToString()),
                "waiting" => Helpers.Money.ToPersianDigits(WaitingCount.ToString()),
                "revenue" => TodayRevenueText,
                _ => "—"
            };
    }

    /// <summary>جابه‌جایی کارت به بالا</summary>
    [RelayCommand]
    private void MoveCardUp(DashboardCardVM? card)
    {
        if (card is null) return;
        var idx = MainCards.IndexOf(card);
        if (idx <= 0) return;
        MainCards.Move(idx, idx - 1);
        ScheduleSaveLayout();
    }

    /// <summary>جابه‌جایی کارت به پایین</summary>
    [RelayCommand]
    private void MoveCardDown(DashboardCardVM? card)
    {
        if (card is null) return;
        var idx = MainCards.IndexOf(card);
        if (idx < 0 || idx >= MainCards.Count - 1) return;
        MainCards.Move(idx, idx + 1);
        ScheduleSaveLayout();
    }

    /// <summary>ساخت JSON چیدمان روی نخ رابط کاربری — مصون از تغییر هم‌زمان مجموعه‌ها (چرخهٔ ۷۶)</summary>
    private string BuildLayoutJson() => JsonSerializer.Serialize(new
    {
        order = MainCards.Select(c => c.Key).ToList(),
        hidden = MainCards.Where(c => !c.Visible).Select(c => c.Key).ToList(),
        showSecondary = ShowSecondaryCards,
        showChart = ShowRevenueChart,
        showUpcoming = ShowUpcoming,
        showActivities = ShowActivities,
        // ═══ چرخهٔ ۷۴ — ترتیب و نمایش بخش‌های قابل‌چیدمان ═══
        sectionsOrder = Sections.Select(s => s.Key).ToList(),
        sectionsHidden = Sections.Where(s => !s.Visible).Select(s => s.Key).ToList()
    });

    /// <summary>
    /// زمان‌بندی ذخیرهٔ چیدمان با ادغام (Debounce) — چرخهٔ ۷۶.
    /// جابه‌جایی/مخفی‌سازی پیاپی کارت‌ها در حالت شخصی‌سازی، ده‌ها رویداد در
    /// چند صد میلی‌ثانیه تولید می‌کرد و هر رویداد یک PUT مستقل می‌فرستاد؛
    /// PUTهای هم‌پوشان روی سرور برخورد هم‌زمانی می‌ساختند. اکنون تغییرات
    /// ۸۰۰ میلی‌ثانیه‌ای ادغام می‌شوند، فقط «آخرین وضعیت» یک‌بار ارسال می‌شود
    /// و ذخیره‌های پیاپی صف‌بندی می‌شوند تا هرگز دو PUT هم‌پوشان در پرواز نباشد.
    /// </summary>
    private void ScheduleSaveLayout()
    {
        if (_suppressLayoutSave)
            return;

        // JSON روی همین نخِ رابط کاربری ساخته می‌شود — مجموعه‌ها اینجا هم‌زمان تغییر نمی‌کنند
        string layout;
        try
        {
            layout = BuildLayoutJson();
        }
        catch
        {
            return; // ذخیرهٔ چیدمان بهترین‌حالتی است
        }

        _layoutSaveDebounce?.Cancel();
        _layoutSaveDebounce = new System.Threading.CancellationTokenSource();
        var token = _layoutSaveDebounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token); // ادغام تغییرات پیاپی در یک ذخیره

                // فقط یک PUT هم‌زمان در پرواز — صف‌بندی تا خلاصیِ قبلی
                while (_layoutSaveInFlight && !token.IsCancellationRequested)
                    await Task.Delay(120, token);
                if (token.IsCancellationRequested)
                    return;

                _layoutSaveInFlight = true;
                try
                {
                    await _api.PutAsync("dashboard/layout", new { layout });
                }
                finally
                {
                    _layoutSaveInFlight = false;
                }
            }
            catch (OperationCanceledException)
            {
                // تغییر تازه‌تری رسیده و این ذخیره باطل شده — دنبالهٔ تازه خودش می‌آید
            }
            catch
            {
                // ذخیرهٔ چیدمان بهترین‌حالتی است — خطا داشبورد را نمی‌شکند
            }
        });
    }

    /// <summary>بارگذاری چیدمان ذخیره‌شده</summary>
    private async Task LoadLayoutAsync()
    {
        // چرخهٔ ۷۶ — اعمال برنامه‌ای چیدمان نباید «ذخیرهٔ خودکار» شعله‌ور کند؛
        // پیش‌تر هر Clear/Add/Visible=false هنگام بارگذاری یک PUT مستقل می‌فرستاد.
        _suppressLayoutSave = true;
        try
        {
            var response = await _api.GetAsync<LayoutEnvelope>("dashboard/layout");
            var json = response?.Data?.Layout;
            if (string.IsNullOrWhiteSpace(json)) return;
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("order", out var order) && order.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var ordered = new List<DashboardCardVM>();
                foreach (var keyEl in order.EnumerateArray())
                {
                    var key = keyEl.GetString();
                    var card = MainCards.FirstOrDefault(c => c.Key == key);
                    if (card is not null && !ordered.Contains(card)) ordered.Add(card);
                }
                foreach (var c in MainCards)
                    if (!ordered.Contains(c)) ordered.Add(c);
                MainCards.Clear();
                foreach (var c in ordered) MainCards.Add(c);
            }
            if (root.TryGetProperty("hidden", out var hidden) && hidden.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var keyEl in hidden.EnumerateArray())
                {
                    var card = MainCards.FirstOrDefault(c => c.Key == keyEl.GetString());
                    if (card is not null) card.Visible = false;
                }
            if (root.TryGetProperty("showSecondary", out var sec)) ShowSecondaryCards = sec.GetBoolean();
            if (root.TryGetProperty("showChart", out var chart)) ShowRevenueChart = chart.GetBoolean();
            if (root.TryGetProperty("showUpcoming", out var up)) ShowUpcoming = up.GetBoolean();
            if (root.TryGetProperty("showActivities", out var act)) ShowActivities = act.GetBoolean();

            // ═══ چرخهٔ ۷۴ — ترتیب و نمایش بخش‌های قابل‌چیدمان ═══
            if (root.TryGetProperty("sectionsOrder", out var sOrder) &&
                sOrder.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var ordered = new List<DashboardSectionItem>();
                foreach (var keyEl in sOrder.EnumerateArray())
                {
                    var section = Sections.FirstOrDefault(s => s.Key == keyEl.GetString());
                    if (section is not null && !ordered.Contains(section)) ordered.Add(section);
                }
                foreach (var s in Sections)
                    if (!ordered.Contains(s)) ordered.Add(s);
                var shortcutsSection = ordered.FirstOrDefault(s => s.Key == "shortcuts");
                if (shortcutsSection is not null)
                {
                    ordered.Remove(shortcutsSection);
                    ordered.Insert(0, shortcutsSection);
                }
                Sections.Clear();
                foreach (var s in ordered) Sections.Add(s);
            }
            if (root.TryGetProperty("sectionsHidden", out var sHidden) &&
                sHidden.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var keyEl in sHidden.EnumerateArray())
                {
                    var section = Sections.FirstOrDefault(s => s.Key == keyEl.GetString());
                    if (section is not null) section.Visible = false;
                }
            var shortcutsAtTop = Sections.FirstOrDefault(s => s.Key == "shortcuts");
            if (shortcutsAtTop is not null && Sections.IndexOf(shortcutsAtTop) > 0)
                Sections.Move(Sections.IndexOf(shortcutsAtTop), 0);
        }
        catch
        {
            // بدون چیدمان ذخیره‌شده — پیش‌فرض
        }
        finally
        {
            _suppressLayoutSave = false;
        }
    }

    /// <summary>پاسخ چیدمان داشبورد</summary>
    public sealed class LayoutEnvelope
    {
        public bool IsSuccess { get; set; }
        public LayoutData? Data { get; set; }
    }

    /// <summary>داده چیدمان</summary>
    public sealed class LayoutData
    {
        public string? Layout { get; set; }
    }

    // ═══ چرخهٔ ۴۱ — پاسخ GET users/me/shortcuts ═══

    /// <summary>
    /// پوشش پاسخ استاندارد — Data.Keys تهی یعنی هنوز رکوردی در دیتابیس نیست
    /// (کلاینت به کش محلی/پیش‌فرض می‌رود)؛ لیست خالی یعنی انتخاب آگاهانهٔ
    /// کاربر (هیچ میانبری).
    /// </summary>
    public sealed class ShortcutsResponse
    {
        public bool IsSuccess { get; set; }
        public ShortcutsData? Data { get; set; }
    }

    /// <summary>بارِ میانبرها — همان UserShortcutsDto سرور</summary>
    public sealed class ShortcutsData
    {
        public List<string>? Keys { get; set; }
    }

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public DashboardViewModel(IApiClient api, ISnackbarService snackbar,
        ITokenManager tokens, INavigationService navigation)
    {
        _api = api;
        _snackbar = snackbar;
        // چرخهٔ ۳۹ — میانبرهای شخصی
        _tokens = tokens;
        _navigation = navigation;
        InitializeCards();
        InitializeSections(); // چرخهٔ ۷۴ — بخش‌های قابل‌چیدمان (پیش از بارگذاری ترتیب)
        _ = LoadLayoutAsync();
        // چرخهٔ ۴۱ — میانبرها از دیتابیس (با مهاجرت کش محلی و جانشینی آفلاین)
        _ = LoadShortcutsAsync();
        _refresh.Tick += async (_, _) => await LoadStatsAsync();
        _refresh.Start();
        _ = LoadStatsAsync();
    }

    // ═════════════════ چرخهٔ ۳۹ — میانبرهای شخصی داشبورد ═════════════════

    /// <summary>کاشی میانبر — کلید صفحه + عنوان + آیکون + رنگ تأکیدی + دستهٔ منو</summary>
    public sealed class ShortcutTile
    {
        /// <summary>کلید صفحهٔ مقصد</summary>
        public string Key { get; init; } = "";

        /// <summary>عنوان فارسی — از کاتالوگ</summary>
        public string Title { get; init; } = "";

        /// <summary>آیکون PackIcon — از کاتالوگ</summary>
        public string Icon { get; init; } = "ViewDashboard";

        /// <summary>رنگ تأکیدی کاشی — چرخهٔ رنگی هماهنگ با کارت‌های آمار</summary>
        public string Accent { get; init; } = "#1E88E5";

        /// <summary>
        /// نام فارسی دستهٔ منو — چرخهٔ ۴۲ (گزارش مالک: «در فرم انتخاب میانبرهای
        /// داشبورد ظاهر بدی داره و زیباتر و حرفه‌ای‌ترش بکن»): گروه‌بندی بصری
        /// انتخابگر میانبرها بر همین فیلد انجام می‌شود.
        /// </summary>
        public string Group { get; init; } = "مدیریت سامانه";

        /// <summary>
        /// رنگ دستهٔ منو — چرخهٔ ۴۲: لبه/دایرهٔ آیکون/چیپ کارت در انتخابگر از
        /// همین رنگ استفاده می‌کند (رنگِ کاشی داشبورد همچنان از Accent تکراری است).
        /// </summary>
        public string GroupColor { get; init; } = "#616161";
    }

    /// <summary>میانبرهای این کاربر</summary>
    public ObservableCollection<ShortcutTile> Shortcuts { get; } = new();

    /// <summary>حالت ویرایش میانبرها — دکمه‌های حذف/افزودن نمایان می‌شوند</summary>
    [ObservableProperty]
    private bool _isEditingShortcuts;

    /// <summary>
    /// خروجِ پس از تعویض نشست را «بدون ذخیره» رد کن — چرخهٔ ۴۴.
    /// اگر کاربر قبلی هنگام خروج از حساب در حالت ویرایش میانبرها بود،
    /// پایین‌آوردنِ سادهٔ پرچم یعنی SaveShortcuts با «مجموعهٔ میانبرهای
    /// کاربر قبلی» روی مسیر/رکورد «کاربر تازه» — خرابی داده است. این
    /// پرچم یک‌بارهٔ مصرفی آن ذخیره را قورت می‌دهد.
    /// </summary>
    private bool _suppressEditExitSave;

    /// <summary>آیا میانبری ثبت شده؟ — برای حالت خالی پنل</summary>
    public bool HasShortcuts => Shortcuts.Count > 0;

    /// <summary>
    /// شروع/نوسازی نشست — چرخهٔ چهل‌وچهارم (گزارش مالک: «با کاربری دیگری
    /// وارد میشوم همچنان کاربر قبلی رو نشون میده»).
    /// DashboardViewModel هم تک‌نمونه است؛ بدون این نوسازی، میانبرها و
    /// چیدمانِ ذخیره‌شدهٔ «کاربر قبلی» برای کاربر تازه نمایش داده می‌شد
    /// (سازنده فقط یک‌بار و برای نخستین کاربر اجرا می‌شود). MainWindowViewModel
    /// در آغاز هر نشست این متد را فراخوانی می‌کند.
    /// </summary>
    public async Task OnSessionStartedAsync()
    {
        // خروج از حالت ویرایشِ احتمالیِ کاربر قبلی — بی‌ذخیره (توضیح پرچم بالا)
        if (IsEditingShortcuts)
        {
            _suppressEditExitSave = true;
            IsEditingShortcuts = false;
        }

        // پاک‌سازی میانبرهای کاربر قبلی پیش از بارگذاریِ کاربر تازه
        Shortcuts.Clear();
        OnPropertyChanged(nameof(HasShortcuts));

        // پرچم مسابقهٔ دورهٔ قبل معتبر نیست — GET تازه باید بی‌دردسر بنشیند
        _shortcutsSaveWonRace = false;

        // کارت‌ها و چیدمانِ ذخیره‌شدهٔ کاربر تازه + میانبرهای او
        // (InitializeCards علاوه بر ساخت خام، رویداد ذخیرهٔ خودکار چیدمان
        //  هر کارت را هم وصل می‌کند — الگوی همان سازنده)
        InitializeCards();
        InitializeSections(); // چرخهٔ ۷۴ — بازسازی بخش‌ها برای کاربر تازه
        await LoadLayoutAsync();
        await LoadShortcutsAsync();

        // آمار داشبورد نیز برای کاربر تازه بی‌درنگ تازه شود (نه منتظر تیک ۶۰ ثانیه)
        _ = LoadStatsAsync();
    }

    /// <summary>تغییر حالت ویرایش → ذخیره در خاموشی حالت (تجربهٔ ایمن)</summary>
    partial void OnIsEditingShortcutsChanged(bool value)
    {
        if (!value && _suppressEditExitSave)
        {
            // چرخهٔ ۴۴ — خروجِ ناشی از تعویض نشست؛ ذخیره‌ای در کار نیست
            _suppressEditExitSave = false;
        }
        else if (!value)
        {
            SaveShortcuts();
        }
        OnPropertyChanged(nameof(HasShortcuts));
    }

    /// <summary>چرخهٔ رنگ کاشی‌ها — هماهنگ با کارت‌های آمار داشبورد</summary>
    private static readonly string[] ShortcutAccents =
    {
        "#1E88E5", "#43A047", "#F57C00", "#00897B", "#5E35B1", "#C2185B", "#5D4037", "#0277BD"
    };

    /// <summary>
    /// میانبرهای پیش‌فرض ادمین — چرخهٔ ۴۱ (گزارش مالک: «میانبرهای پیش‌فرض برای
    /// ادمین بزار»): پرمصرف‌ترین صفحات جریان روزانهٔ درمانگاه؛ فقط وقتی دیده
    /// می‌شوند که هیچ رکورد سروری و هیچ کش محلی در کار نباشد (انتخاب آگاهانهٔ
    /// کاربر همیشه بر پیش‌فرض مقدم است) و عمداً ذخیره نمی‌شوند تا نخستین
    /// ویرایشِ ادمین همان مبنای رکورد دیتابیس شود.
    /// </summary>
    private static readonly string[] DefaultAdminShortcutKeys =
    {
        "receptions",     // پذیرش — پرتکرارترین کار روزانه
        "appointments",   // نوبت‌دهی
        "patients",       // پرونده بیماران
        "visits",         // ویزیت و معاینه
        "billing",        // مالی و صندوق
        "reports"         // گزارش‌ها — پایش روزانهٔ مدیر
    };

    /// <summary>
    /// بارگذاری میانبرها — چرخهٔ ۴۱: اولویت با رکورد دیتابیسی است (GET
    /// users/me/shortcuts)؛ اگر رکوردی نبود، کش محلی یک‌باره به سرور مهاجرت
    /// داده می‌شود؛ اگر کش هم نبود و کاربر ادمین بود، میانبرهای پیش‌فرض
    /// نمایش داده می‌شود؛ اگر سرور در دسترس نبود، کش محلی (رفتار پیش از
    /// چرخهٔ ۴۱) جانشین می‌شود تا قطعیِ لحظه‌ای پنل را خالی نکند.
    /// </summary>
    private async Task LoadShortcutsAsync()
    {
        ShortcutsResponse? response = null;
        try
        {
            response = await _api.GetAsync<ShortcutsResponse>("users/me/shortcuts");
        }
        catch
        {
            // قطعی لحظه‌ای شبکه — پایین با کش محلی ادامه می‌دهیم
        }

        // گارد مسابقه: اگر کاربر پیش از رسیدن پاسخ، چیزی ذخیره کرد، همان
        // PUT تازه حقیقت است و پاسخ کهنهٔ این GET کنار می‌رود
        if (_shortcutsSaveWonRace) return;

        if (response?.Data is not null)
        {
            if (response.Data.Keys is not null)
            {
                // رکورد سرور موجود — مبنای حقیقت؛ حتی لیست خالی یعنی کاربر
                // آگاهانه همهٔ میانبرها را برداشته و پیش‌فرض نباید بازگردد
                ApplyShortcutKeys(response.Data.Keys);
                return;
            }

            // رکوردی در دیتابیس نیست — مهاجرت کش محلی قدیمی (چرخهٔ ۳۹) یا پیش‌فرض
            var local = ReadLocalShortcutKeys();
            if (local is not null)
            {
                ApplyShortcutKeys(local);
                // مهاجرت بی‌صدا به دیتابیس — از این پس همهٔ ایستگاه‌ها همان را می‌بینند
                _ = SaveShortcutsToServerAsync();
            }
            else
            {
                ApplyDefaultShortcuts();
            }
            return;
        }

        // سرور پاسخ نداد — کش محلی (رفتار کاملاً مشتری‌محورِ پیش از چرخهٔ ۴۱)
        var cached = ReadLocalShortcutKeys();
        if (cached is not null)
            ApplyShortcutKeys(cached);
        // نه سرور، نه کش: پنل خالی می‌ماند تا نخستین بارگذاری موفق
    }

    /// <summary>
    /// اعمال فهرست کلیدها روی پنل — همان پالایشِ چرخهٔ ۳۹: کلید ناشناخته
    /// (صفحهٔ حذف‌شده) و صفحهٔ بی‌مجوزِ کاربر از فهرست کنده می‌شود تا کاشی
    /// مرده/ممنوع نماند و چرخهٔ رنگ کاشی‌ها به ترتیب نمایش می‌چسبد.
    /// </summary>
    /// <param name="keys">کلید صفحات به ترتیب چیدمان کاربر</param>
    private void ApplyShortcutKeys(IEnumerable<string> keys)
    {
        Shortcuts.Clear();
        var index = 0;
        foreach (var key in keys)
        {
            var entry = Helpers.PageCatalog.Find(key);
            if (entry is null || !entry.ShowInPicker) continue;
            // مجوز → همان قاعدهٔ منو: ادمین همه‌چیز؛ صفحهٔ آزاد همیشه
            if (!HasShortcutAccess(key)) continue;
            Shortcuts.Add(new ShortcutTile
            {
                Key = entry.Key,
                Title = entry.Title,
                Icon = entry.Icon,
                Accent = ShortcutAccents[index % ShortcutAccents.Length],
                Group = entry.Group,
                GroupColor = Helpers.PageCatalog.ColorOfGroup(entry.Group)
            });
            index++;
        }
        OnPropertyChanged(nameof(HasShortcuts));
    }

    /// <summary>
    /// میانبرهای پیش‌فرض فقط برای ادمین — چرخهٔ ۴۱؛ کاربر عادی پنل خالی می‌گیرد
    /// (انتخاب شخصی خودش)؛ ادمین شش میانبر پرکاربرد می‌بیند و عمداً چیزی
    /// ذخیره نمی‌شود تا نخستین ویرایش، مبنای رکورد دیتابیس شود.
    /// </summary>
    private void ApplyDefaultShortcuts()
    {
        if (_tokens.IsAdmin)
            ApplyShortcutKeys(DefaultAdminShortcutKeys);
        else
            OnPropertyChanged(nameof(HasShortcuts));
    }

    /// <summary>
    /// خواندن کش محلی — تهی یعنی فایل نیست/خراب است؛ تنها مصرف‌کنندهٔ
    /// مهاجرتِ یک‌بارهٔ چرخهٔ ۴۱ و حالت قطعی سرور است.
    /// </summary>
    private List<string>? ReadLocalShortcutKeys()
    {
        try
        {
            if (System.IO.File.Exists(ShortcutsFilePath))
                return JsonSerializer.Deserialize<List<string>>(
                    System.IO.File.ReadAllText(ShortcutsFilePath));
        }
        catch
        {
            // فایل قفل‌شده/خراب — تهی برمی‌گردد و مسیر پیش‌فرض ادامه می‌یابد
        }
        return null;
    }

    /// <summary>
    /// ذخیرهٔ میانبرها — چرخهٔ ۴۱: کش محلی بلافاصله نوشته می‌شود (رفتار
    /// پیشین + جانشینی آفلاین) و رکورد مرجع دیتابیس به‌صورت ناهمگام تازه
    /// می‌شود؛ پرچم مسابقه هم اینجا برافراشته می‌شود تا GETِ در حال پروازِ
    /// همان لحظه، چیدمان تازه را بازنویسی نکند.
    /// </summary>
    private void SaveShortcuts()
    {
        SaveLocalShortcuts();
        _shortcutsSaveWonRace = true;
        _ = SaveShortcutsToServerAsync();
    }

    /// <summary>
    /// نوشتن کش محلی — همان منطق چرخهٔ ۳۹ (فقط کلیدها، با تورفتگی خوانا)؛
    /// شکست دیسک بی‌صدا است چون رکورد مرجع در دیتابیس ذخیره می‌شود.
    /// </summary>
    private void SaveLocalShortcuts()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(ShortcutsFilePath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(ShortcutsFilePath,
                JsonSerializer.Serialize(Shortcuts.Select(s => s.Key).ToList(),
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // دیسک فقط‌خواندنی/دسترسی ناکافی — بی‌صدا؛ رکورد دیتابیسی مرجع است
        }
    }

    /// <summary>
    /// ذخیرهٔ مرجع در دیتابیس — PUT users/me/shortcuts؛ شکست بی‌صدا است چون
    /// کش محلی نوشته شده و تلاش بعدی (بارگذاری/ذخیرهٔ بعدی) دوباره می‌فرستد.
    /// </summary>
    private async Task SaveShortcutsToServerAsync()
    {
        try
        {
            await _api.PutAsync("users/me/shortcuts",
                new { keys = Shortcuts.Select(s => s.Key).ToList() });
        }
        catch
        {
            // قطعی لحظه‌ای — کش محلی جانشین است تا اتصال بعدی
        }
    }

    /// <summary>دسترسی این کاربر به کلید صفحه — همان قاعدهٔ منوی کناری</summary>
    private bool HasShortcutAccess(string key)
    {
        if (_tokens.IsAdmin) return true;
        var permission = Helpers.PageCatalog.PermissionMap.TryGetValue(key, out var p) ? p : null;
        return permission is null
            || _tokens.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>بازکردن صفحهٔ مقصد با کلیک روی کاشی</summary>
    /// <param name="tile">کاشی کلیک‌شده — از CommandParameter</param>
    [RelayCommand]
    private void OpenShortcut(ShortcutTile? tile)
    {
        if (tile is null) return;
        _navigation.NavigateTo(tile.Key);
    }

    /// <summary>
    /// افزودن میانبر — انتخابگر صفحات (فقط صفحات مجازِ هنوز‌پین‌نشده) باز می‌شود.
    /// چرخهٔ ۴۲ (گزارش مالک: «در فرم انتخاب میانبرهای داشبود، ظاهر بدی داره و
    /// زیباتر و حرفه‌ای‌ترش بکن»): انتخابگر بازطراحی شد — گروه‌بندی دسته‌ای با
    /// رنگِ هر دسته، جستجوی زنده با شمار نتیجه و «انتخاب چندگانه» (تیک روی
    /// چند کارت و افزودن همه با هم) به‌جای افزودنِ تک‌به‌تکِ قبلی.
    /// </summary>
    [RelayCommand]
    private void AddShortcut()
    {
        var already = Shortcuts.Select(s => s.Key).ToHashSet();
        var available = Helpers.PageCatalog.All
            .Where(p => p.ShowInPicker && !already.Contains(p.Key) && HasShortcutAccess(p.Key))
            .Select(p => new ShortcutTile
            {
                Key = p.Key,
                Title = p.Title,
                Icon = p.Icon,
                Group = p.Group,
                GroupColor = Helpers.PageCatalog.ColorOfGroup(p.Group)
            })
            .ToList();

        if (available.Count == 0)
        {
            _snackbar.Info("صفحهٔ دیگری برای افزودن نیست — همهٔ صفحات مجاز شما پین شده‌اند");
            return;
        }

        var picker = new Views.ShortcutPickerWindow(available)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        picker.ShowDialog();

        // چرخهٔ ۴۲: افزودن جمعی — هر تیک‌خورده یک کاشی می‌شود؛ یک‌بار ذخیره
        // و یک پیام با شمار واقعی (منطبق با قانون پیام فارسی/توست مالک)
        if (picker.SelectedKeys is not { Count: > 0 } keys) return;

        foreach (var key in keys)
        {
            var entry = Helpers.PageCatalog.Find(key);
            if (entry is null) continue;
            Shortcuts.Add(new ShortcutTile
            {
                Key = entry.Key,
                Title = entry.Title,
                Icon = entry.Icon,
                Accent = ShortcutAccents[Shortcuts.Count % ShortcutAccents.Length],
                Group = entry.Group,
                GroupColor = Helpers.PageCatalog.ColorOfGroup(entry.Group)
            });
        }
        OnPropertyChanged(nameof(HasShortcuts));
        SaveShortcuts();
        _snackbar.Success(keys.Count == 1
            ? $"میانبر «{Helpers.PageCatalog.Find(keys[0])?.Title ?? keys[0]}» به داشبورد اضافه شد"
            : $"{keys.Count} میانبر به داشبورد اضافه شد");
    }

    /// <summary>حذف میانبر — در حالت ویرایش</summary>
    /// <param name="tile">کاشی قابل حذف — از CommandParameter</param>
    [RelayCommand]
    private void RemoveShortcut(ShortcutTile? tile)
    {
        if (tile is null) return;
        Shortcuts.Remove(tile);
        OnPropertyChanged(nameof(HasShortcuts));
        SaveShortcuts();
    }

    /// <summary>بارگذاری کامل همه بخش‌ها — طبق LoadAsync پرامپت</summary>
    public async Task LoadStatsAsync()
    {
        // چرخه هجدهم (گزارش دوازدهم): ساعت و تاریخ داشبورد با ارقام فارسی
        WelcomeMessage = $"خوش آمدید! امروز {Application.Common.Helpers.PersianDateHelper.ToClockFa(DateTime.Now, withSeconds: false)} — وضعیت درمانگاه به‌روزرسانی شد.";
        TodayDateShamsi = Application.Common.Helpers.PersianDateHelper.ToShamsiFa(DateTime.Now);
        ErrorMessage = string.Empty;

        // ۰) چرخه دوازدهم — پیام‌های پشتیبانی دریافتی از سرور لایسنس (best-effort)
        await LoadSupportMessagesAsync();

        // ═══ چرخهٔ سی‌ودوم (گزارش مالک — قانون هم‌کلاسی‌ها) ═══ همهٔ اندپوینت‌های
        // داشبورد پاسخ را در پاکت استاندارد ApiResponse ({isSuccess, data}) می‌فرستند
        // ولی کد قدقی ریشهٔ JSON را مستقیم «شیء آمار» یا «آرایهٔ نوبت‌ها» فرض
        // می‌کرد؛ در نتیجه ValueKind آرایه‌ها هرگز برقرار نمی‌شد و آمار هم فقط
        // صفر می‌خواند — نمودار درآمد، نوبت‌های امروز، فعالیت‌های اخیر و
        // هشدارها همگی همیشه خالی بودند. اکنون هر پنج پاسخ نخست از پاکت باز
        // می‌شوند (UnwrapData) و سپس خوانده می‌شوند.

        // ۱) آمار خلاصه — payload داخل data است
        var stats = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/stats"));
        if (stats.ValueKind == JsonValueKind.Object)
        {
            TodayPatients = stats.TryGetProperty("todayAppointments", out var appts) ? appts.GetInt32() : 0;
            TodayVisits = stats.TryGetProperty("todayVisits", out var visits) ? visits.GetInt32() : 0;
            WaitingCount = Math.Max(0, TodayPatients - TodayVisits);
            // چرخهٔ ۷۲ — بیمار جدید امروز؛ کارت پنجم داشبورد جدید
            NewPatientsToday = stats.TryGetProperty("newPatients", out var np) && np.ValueKind != JsonValueKind.Null
                ? np.GetInt32() : 0;
            var revenue = stats.TryGetProperty("todayRevenue", out var rev) && rev.ValueKind != JsonValueKind.Null
                ? rev.GetDecimal() : 0;
            TodayRevenueText = Money.Format(revenue);
            ConnectionStatus = "آمار به‌روز شد";
        }

        // ۲) هشدارها (موجودی کم، انقضای پلکانی و آزمایش در انتظار) — چرخهٔ ۶۹: پله‌های ۹۰/۶۰/۳۰/۷
        var alerts = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/alerts"));
        if (alerts.ValueKind == JsonValueKind.Object)
        {
            LowStockCount = alerts.TryGetProperty("lowStock", out var low) ? low.GetInt32() : 0;
            PendingLabOrders = alerts.TryGetProperty("pendingLabOrders", out var lab) ? lab.GetInt32() : 0;
            Expiring7Count = alerts.TryGetProperty("expiring7", out var e7) ? e7.GetInt32() : 0;
            Expiring30Count = alerts.TryGetProperty("expiring30", out var e30) ? e30.GetInt32() : 0;
            Expiring60Count = alerts.TryGetProperty("expiring60", out var e60) ? e60.GetInt32() : 0;
            Expiring90Count = alerts.TryGetProperty("expiring90", out var e90) ? e90.GetInt32() : 0;
            HasNoInventoryAlerts = LowStockCount == 0 && Expiring90Count == 0;
        }

        // به‌روزرسانی کارت‌های داینامیک چرخه ۱۳
        UpdateCardValues();

        // ۳) نمودار درآمد — چرخهٔ ۷۲: بازهٔ قابل انتخاب (۷/۱۴/۳۰) و بارگذاری مستقل
        await LoadRevenueChartAsync();

        // ═══ چرخهٔ ۷۲ — داشبورد جدید: نمودار ویزیت + ترکیب وضعیت ═══

        // ۳-ب) نمودار ستونی ویزیت روزانه
        var visitsChart = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/visits-chart?days=7"));
        VisitsChart.Clear();
        if (visitsChart.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in visitsChart.EnumerateArray())
            {
                var label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                var count = item.TryGetProperty("count", out var c) && c.ValueKind != JsonValueKind.Null
                    ? (double)c.GetInt32() : 0;
                VisitsChart.Add(new ChartPointData(label, count,
                    $"{label}: {Helpers.Money.ToPersianDigits(((int)count).ToString())} ویزیت"));
            }
        }

        // ۳-پ) ترکیب وضعیت نوبت‌های امروز — دونات + جمع مرکز
        var statusMix = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/status-mix"));
        StatusMix.Clear();
        if (statusMix.ValueKind == JsonValueKind.Object)
        {
            if (statusMix.TryGetProperty("total", out var total) && total.ValueKind != JsonValueKind.Null)
                StatusMixTotalText = Helpers.Money.ToPersianDigits(total.GetInt32().ToString());

            if (statusMix.TryGetProperty("mix", out var mix) && mix.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in mix.EnumerateArray())
                {
                    var label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                    var count = item.TryGetProperty("count", out var c) && c.ValueKind != JsonValueKind.Null
                        ? (double)c.GetInt32() : 0;
                    if (count <= 0) continue; // سهم صفر رسم نمی‌شود
                    StatusMix.Add(new ChartPointData(label, count,
                        $"{label}: {Helpers.Money.ToPersianDigits(((int)count).ToString())} نوبت"));
                }
            }
        }

        // ۴) نوبت‌های امروز
        var appointments = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/appointments-today?limit=8"));
        UpcomingAppointments.Clear();
        if (appointments.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in appointments.EnumerateArray())
            {
                UpcomingAppointments.Add(new UpcomingAppointmentRow(
                    a.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                    a.TryGetProperty("queueNumber", out var q) && q.ValueKind != JsonValueKind.Null ? q.GetInt32() : null,
                    a.TryGetProperty("time", out var t) ? t.GetString() ?? "" : "",
                    a.TryGetProperty("patientName", out var p) ? p.GetString() ?? "" : "",
                    a.TryGetProperty("doctorName", out var d) ? d.GetString() ?? "" : "",
                    a.TryGetProperty("statusText", out var s) ? s.GetString() ?? "" : "",
                    a.TryGetProperty("status", out var st) ? st.GetString() ?? "" : ""));
            }
        }

        // ۵) فعالیت‌های اخیر
        var activities = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/recent-activities?limit=10"));
        RecentActivities.Clear();
        if (activities.ValueKind == JsonValueKind.Array)
        {
            foreach (var act in activities.EnumerateArray())
            {
                RecentActivities.Add(new RecentActivityRow(
                    act.TryGetProperty("timeShamsi", out var ts) ? ts.GetString() ?? "" : "",
                    act.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "",
                    act.TryGetProperty("action", out var ac) ? ac.GetString() ?? "" : "",
                    act.TryGetProperty("entity", out var en) ? en.GetString() ?? "" : ""));
            }
        }
    }

    /// <summary>
    /// بارگذاری نمودار درآمد با بازهٔ منتخب — چرخهٔ ۷۲: از LoadStats جدا شد تا
    /// تعویض بازهٔ ۷/۱۴/۳۰ فقط همین درخواست را بشورد نه کل داشبورد را.
    /// </summary>
    private async Task LoadRevenueChartAsync()
    {
        var chart = UnwrapData(await _api.GetAsync<JsonElement>(
            $"dashboard/revenue-chart?days={SelectedRevenueDays}"));
        RevenueChart.Clear();
        if (chart.ValueKind != JsonValueKind.Array) return;

        foreach (var item in chart.EnumerateArray())
        {
            var label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
            var amount = item.TryGetProperty("amount", out var a) && a.ValueKind != JsonValueKind.Null
                ? (double)a.GetDecimal() : 0;
            // چرخهٔ بیست‌وهفتم: قالب‌بندی مبلغ با واحد پویا + جداکنندهٔ فارسی
            RevenueChart.Add(new ChartPointData(label, amount,
                $"{label}: {Money.FormatWithUnit((decimal)amount)}"));
        }
    }

    /// <summary>
    /// بازکردن پاکت استاندارد ApiResponse — چرخهٔ سی‌ودوم: همهٔ اندپوینت‌های
    /// API دادهٔ واقعی را در ویژگی «data» می‌فرستند؛ این کمکی اگر پاکت باشد
    /// آن را باز می‌کند و در نبود پاکت، خود ریشه را برمی‌گرداند (سازگار با
    /// پاسخ‌های بدون پاکت). درخواست ناموفق → JsonElement پیش‌فرض (Undefined)
    /// که شرط‌های ValueKind بعدی آن را نادیده می‌گیرند.
    /// </summary>
    private static JsonElement UnwrapData(JsonElement root)
        => root.ValueKind == JsonValueKind.Object &&
           root.TryGetProperty("data", out var data) &&
           (data.ValueKind == JsonValueKind.Object || data.ValueKind == JsonValueKind.Array)
            ? data
            : root;

    /// <summary>بارگذاری مجدد دستی (F5)</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadStatsAsync();
        _snackbar.Success("داشبورد به‌روزرسانی شد");
    }

    /// <summary>
    /// گشودن نمایشگر سالن انتظار — چرخهٔ ۶۹ (فاز ۳ / F-14 «حالت کیوسک»):
    /// پنجرهٔ تمام‌صفحهٔ تیره برای نمایشگر سالن (ساعت/تاریخ شمسی درشت +
    /// «در حال ویزیت» و «نوبت‌های پیش‌رو» با تازه‌سازی بلادرنگ صف).
    /// خروج از نمایشگر فقط با Escape است.
    /// </summary>
    [RelayCommand]
    private void OpenWaitingRoomDisplay()
    {
        var window = new Views.WaitingRoomDisplayWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.Show(); // بدون ShowDialog — پشت آن کار ادامه دارد؛ سالن مستقل است
        _snackbar.Info("نمایشگر سالن انتظار باز شد — خروج با کلید Esc");
    }

    /// <summary>
    /// تنظیمات نمایشگر سالن انتظار — چرخهٔ ۷۴ (گزارش مالک):
    /// بخش‌های نمایشی (صف پزشک/خدمت/نوبت‌ها) با ترتیب Drag &amp; Drop +
    /// پیش‌نمایش زنده + ساعت آنالوگ/دیجیتال/تاریخ + نام درمانگاه + مانیتور هدف.
    /// </summary>
    [RelayCommand]
    private void OpenWaitingRoomSettings()
    {
        var window = new Views.WaitingRoomSettingsWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>
    /// باز/بستن مرکز هشدار انبار — چرخهٔ ۶۹ (فاز ۳ / F-08)
    /// با نخستین باز شدن، اقلام جزئیات از endpoint ویژه خوانده می‌شوند؛
    /// خطا فقط پیام فارسی می‌دهد و پنل باز می‌ماند (تلاش مجدد با کلیک بعدی).
    /// </summary>
    [RelayCommand]
    private async Task ToggleAlertCenterAsync()
    {
        // بستن — فقط پرچم برمی‌گردد؛ داده برای باز شدن بعدی می‌ماند
        if (IsAlertCenterOpen)
        {
            IsAlertCenterOpen = false;
            return;
        }

        IsAlertCenterOpen = true;
        try
        {
            var detail = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/alerts/inventory"));

            ExpiringItems.Clear();
            if (detail.ValueKind == JsonValueKind.Object &&
                detail.TryGetProperty("expiring", out var exp) && exp.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in exp.EnumerateArray())
                {
                    var daysLeft = i.TryGetProperty("daysLeft", out var dl) && dl.ValueKind == JsonValueKind.Number
                        ? dl.GetInt32() : 0;
                    var medication = i.TryGetProperty("medicationName", out var mn) ? mn.GetString() ?? "" : "";
                    var batch = i.TryGetProperty("batchNumber", out var bn) && bn.ValueKind != JsonValueKind.Null
                        ? bn.GetString() : null;
                    var expiryShamsi = i.TryGetProperty("expiryDate", out var ed) && ed.ValueKind != JsonValueKind.Null
                        ? Application.Common.Helpers.PersianDateHelper.ToShamsiFa(DateTime.Parse(ed.GetString()!)) : "—";
                    var qty = i.TryGetProperty("quantity", out var q) && q.ValueKind != JsonValueKind.Null
                        ? q.GetDecimal() : 0;

                    // پلهٔ رنگی — همان سیاست چهارپله‌ای سرور: قرمز/نارنجی/کهربایی/زرد
                    var (brush, tier) = daysLeft <= 7 ? ("#E53935", "بحرانی")
                        : daysLeft <= 30 ? ("#FB8C00", "فوری")
                        : daysLeft <= 60 ? ("#F9A825", "هشدار")
                        : ("#FDD835", "پایش");

                    ExpiringItems.Add(new InventoryAlertRow(
                        medication, batch, expiryShamsi, daysLeft,
                        Money.Format(qty), brush, tier));
                }
            }

            LowStockItems.Clear();
            if (detail.ValueKind == JsonValueKind.Object &&
                detail.TryGetProperty("lowStockItems", out var ls) && ls.ValueKind == JsonValueKind.Array)
            {
                foreach (var i in ls.EnumerateArray())
                {
                    LowStockItems.Add(new LowStockAlertRow(
                        i.TryGetProperty("medicationName", out var mn) ? mn.GetString() ?? "" : "",
                        i.TryGetProperty("batchNumber", out var bn) && bn.ValueKind != JsonValueKind.Null
                            ? bn.GetString() : null,
                        Money.Format(i.TryGetProperty("quantity", out var q) && q.ValueKind != JsonValueKind.Null
                            ? q.GetDecimal() : 0),
                        Money.Format(i.TryGetProperty("minStock", out var ms) && ms.ValueKind != JsonValueKind.Null
                            ? ms.GetDecimal() : 0)));
                }
            }
        }
        catch
        {
            // خطای شبکه — مرکز هشدار باز می‌ماند با فهرست خالی؛ تلاش بعدی با کلیک دوباره
            _snackbar.Error("بارگذاری اقلام هشدار انبار ناموفق بود — دوباره تلاش کنید");
        }
    }

    /// <summary>
    /// چرخه دوازدهم — بارگذاری پیام‌های پشتیبانی دریافتی از سرور لایسنس.
    /// جریان داده: پنل PHP → تپ‌قلب LicenseService در API → SystemSettings
    /// → endpoint برون‌گرانه license/messages → اینجا → کارت داشبورد. best-effort:
    /// هر خطایی فقط کارت را مخفی می‌کند و بقیه داشبورد سالم می‌ماند.
    /// ═══ چرخهٔ سی‌ودوم (گزارش مالک) — «پیام‌های ارسالی از سرور لایسنس هنوز
    /// هم در برنامه نمایش داده نمی‌شوند» ═══ ریشه: پاسخ endpoint در پاکت
    /// استاندارد ApiResponse است ({isSuccess, data:[…]}) ولی کد قدیمی ریشهٔ
    /// JSON را مستقیماً «آرایه» فرض می‌کرد؛ ValueKind همیشه Object بود، شرط
    /// هرگز برقرار نمی‌شد و کارت هرگز چیزی نشان نمی‌داد — حتی وقتی پیام‌ها
    /// در دیتابیس موجود بودند. اکنون نخست پاکت باز و آرایهٔ data خوانده می‌شود.
    /// </summary>
    private async Task LoadSupportMessagesAsync()
    {
        try
        {
            SupportMessages.Clear();
            var response = await _api.GetAsync<JsonElement>("license/messages");

            // ── بازکردن پاکت استاندارد: دادهٔ واقعی در ویژگی «data» است ──
            if (response.ValueKind != JsonValueKind.Object ||
                !response.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                HasSupportMessages = false;
                return;
            }

            foreach (var m in data.EnumerateArray().Take(5))
            {
                // ═══ چرخهٔ سی‌وپنجم (گزارش مالک) ═══
                // «رنگ کادر پیام با رنگ انتخابی در سرور لایسنس فرق دارد» — فیلد
                // color فروشنده خوانده می‌شود (تهی مجاز — EffectiveBrush خودش به
                // رنگ‌بندی سطح اهمیت برمی‌گردد). تاریخ هم از این پس با DateFa
                // شمسیِ ارقام‌فارسی نمایش داده می‌شود (تبدیل در خود DTO مشترک).
                SupportMessages.Add(new SupportMessageDto(
                    m.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt64() : 0,
                    m.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    m.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "",
                    m.TryGetProperty("level", out var lv) ? lv.GetString() ?? "info" : "info",
                    m.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "",
                    m.TryGetProperty("color", out var c) ? c.GetString() : null));
            }
            HasSupportMessages = SupportMessages.Count > 0;
        }
        catch
        {
            // خطای شبکه یا نبود لایسنس فعال — کارت مخفی می‌شود
            HasSupportMessages = false;
        }
    }
}
