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

    /* ── آمار فرعی (ردیف دوم) ── */
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _todayPrescriptions;
    [ObservableProperty] private int _pendingLabOrders;

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

    /// <summary>بازگشت به چیدمان پیش‌فرض</summary>
    [RelayCommand]
    private void ResetLayout()
    {
        foreach (var c in MainCards) c.Visible = true;
        ShowSecondaryCards = true;
        ShowRevenueChart = true;
        ShowUpcoming = true;
        ShowActivities = true;
        // ترتیب پیش‌فرض
        var ordered = MainCards.OrderBy(c => Array.IndexOf(DefaultOrder, c.Key)).ToList();
        MainCards.Clear();
        foreach (var c in ordered) MainCards.Add(c);
        _ = SaveLayoutAsync();
        _snackbar.Success("چیدمان به حالت پیش‌فرض بازگشت");
    }

    /// <summary>ترتیب پیش‌فرض کارت‌ها</summary>
    private static readonly string[] DefaultOrder = { "patients", "visits", "waiting", "revenue" };

    partial void OnShowSecondaryCardsChanged(bool value) => _ = SaveLayoutAsync();
    partial void OnShowRevenueChartChanged(bool value) => _ = SaveLayoutAsync();
    partial void OnShowUpcomingChanged(bool value) => _ = SaveLayoutAsync();
    partial void OnShowActivitiesChanged(bool value) => _ = SaveLayoutAsync();

    /// <summary>راه‌اندازی کارت‌های داینامیک</summary>
    private void InitializeCards()
    {
        MainCards.Clear();
        MainCards.Add(new DashboardCardVM
        {
            Key = "patients", IconKind = "AccountGroup", Label = "بیماران امروز",
            CompareText = "مجموع نوبت‌های ثبت‌شده امروز", Accent = "#1E88E5"
        });
        MainCards.Add(new DashboardCardVM
        {
            Key = "visits", IconKind = "Stethoscope", Label = "ویزیت‌شده",
            CompareText = "ویزیت‌های انجام‌شده امروز", Accent = "#43A047"
        });
        MainCards.Add(new DashboardCardVM
        {
            Key = "waiting", IconKind = "ClockOutline", Label = "در انتظار",
            CompareText = "در صف انتظار باقی‌مانده", Accent = "#FB8C00"
        });
        MainCards.Add(new DashboardCardVM
        {
            // چرخهٔ بیست‌وهفتم: واحد پول پویا از تنظیمات (ریال/تومان) — حذف هاردکد
            Key = "revenue", IconKind = "CashMultiple", Label = $"درآمد امروز ({Money.UnitLabel})",
            CompareText = "مجموع پرداخت‌های ثبت‌شده", Accent = "#8E24AA"
        });
        foreach (var c in MainCards)
            c.VisibleChanged += (_, _) => _ = SaveLayoutAsync();
    }

    /// <summary>به‌روزرسانی مقادیر کارت‌ها از آمار</summary>
    private void UpdateCardValues()
    {
        foreach (var c in MainCards)
            c.Value = c.Key switch
            {
                "patients" => TodayPatients.ToString(),
                "visits" => TodayVisits.ToString(),
                "waiting" => WaitingCount.ToString(),
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
        _ = SaveLayoutAsync();
    }

    /// <summary>جابه‌جایی کارت به پایین</summary>
    [RelayCommand]
    private void MoveCardDown(DashboardCardVM? card)
    {
        if (card is null) return;
        var idx = MainCards.IndexOf(card);
        if (idx < 0 || idx >= MainCards.Count - 1) return;
        MainCards.Move(idx, idx + 1);
        _ = SaveLayoutAsync();
    }

    /// <summary>ذخیره چیدمان در سرور</summary>
    private async Task SaveLayoutAsync()
    {
        try
        {
            var order = MainCards.Select(c => c.Key).ToList();
            var layout = System.Text.Json.JsonSerializer.Serialize(new
            {
                order,
                hidden = MainCards.Where(c => !c.Visible).Select(c => c.Key).ToList(),
                showSecondary = ShowSecondaryCards,
                showChart = ShowRevenueChart,
                showUpcoming = ShowUpcoming,
                showActivities = ShowActivities
            });
            await _api.PutAsync("dashboard/layout", new { layout });
        }
        catch
        {
            // ذخیره چیدمان بهترین‌حالتی است — خطا داشبورد را نمی‌شکند
        }
    }

    /// <summary>بارگذاری چیدمان ذخیره‌شده</summary>
    private async Task LoadLayoutAsync()
    {
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
        }
        catch
        {
            // بدون چیدمان ذخیره‌شده — پیش‌فرض
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

    /// <summary>آیا میانبری ثبت شده؟ — برای حالت خالی پنل</summary>
    public bool HasShortcuts => Shortcuts.Count > 0;

    /// <summary>تغییر حالت ویرایش → ذخیره در خاموشی حالت (تجربهٔ ایمن)</summary>
    partial void OnIsEditingShortcutsChanged(bool value)
    {
        if (!value) SaveShortcuts();
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
            var revenue = stats.TryGetProperty("todayRevenue", out var rev) && rev.ValueKind != JsonValueKind.Null
                ? rev.GetDecimal() : 0;
            TodayRevenueText = Money.Format(revenue);
            ConnectionStatus = "آمار به‌روز شد";
        }

        // ۲) هشدارها (موجودی کم و آزمایش در انتظار)
        var alerts = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/alerts"));
        if (alerts.ValueKind == JsonValueKind.Object)
        {
            LowStockCount = alerts.TryGetProperty("lowStock", out var low) ? low.GetInt32() : 0;
            PendingLabOrders = alerts.TryGetProperty("pendingLabOrders", out var lab) ? lab.GetInt32() : 0;
        }

        // به‌روزرسانی کارت‌های داینامیک چرخه ۱۳
        UpdateCardValues();

        // ۳) نمودار درآمد ۷ روز — چرخه یازدهم: میله‌ای سطح‌خورده بود؛
        // برای «روند در طول زمان» نوع درست نمودار، سطحی/خطی است
        var chart = UnwrapData(await _api.GetAsync<JsonElement>("dashboard/revenue-chart?days=7"));
        RevenueChart.Clear();
        if (chart.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in chart.EnumerateArray())
            {
                var label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                var amount = item.TryGetProperty("amount", out var a) && a.ValueKind != JsonValueKind.Null
                    ? (double)a.GetDecimal() : 0;
                // چرخهٔ بیست‌وهفتم: قالب‌بندی مبلغ با واحد پویا + جداکنندهٔ فارسی
                // (Money.FormatWithUnit همزمان تقسیم ۱۰ِ حالت تومان را هم اعمال می‌کند)
                RevenueChart.Add(new ChartPointData(label, amount,
                    $"{label}: {Money.FormatWithUnit((decimal)amount)}"));
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
