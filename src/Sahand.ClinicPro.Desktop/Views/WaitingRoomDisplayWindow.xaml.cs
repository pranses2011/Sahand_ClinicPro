using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// ردیف نمایشی یک بیمار در کارت بخش — شمارهٔ صف + نام + زیرعنوان
/// </summary>
public record WaitingDisplayRow(
    string Number,
    string Name,
    string Subtitle,
    Brush RowBrush,
    Brush NumberBrush);

/// <summary>
/// کارت بخش نمایشگری — عنوان + ردیف‌های زنده
/// </summary>
public class WaitingDisplaySection
{
    public string Title { get; init; } = string.Empty;
    public ObservableCollection<WaitingDisplayRow> Rows { get; } = new();
}

/// <summary>
/// ViewModel سبک نمایشگر — پرچم‌های نمایش ساعت/تاریخ + کارت‌های بخش برای XAML
/// </summary>
public class WaitingDisplayVm
{
    public System.Windows.Visibility ShowAnalogClock { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowDigitalClock { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowLongDate { get; set; } = System.Windows.Visibility.Visible;
    public ObservableCollection<WaitingDisplaySection> Sections { get; } = new();
}

/// <summary>
/// کد پشت نمایشگر سالن انتظار — بازطراحی کامل چرخهٔ ۷۴.
/// کارها:
///   ۱) چیدمان سه‌ستونهٔ سفارشی مالک: راست = پشتهٔ ساعت (آنالوگ شکیل +
///      دیجیتال کوچک + تاریخ بلند) / میانه = نام واقعی درمانگاه + کارت‌های
///      بخش / چپ = تبلیغ نرم‌افزار (عضو ثابت)؛
///   ۲) باز شدن روی مانیتور/تلویزیون انتخابی از تنظیمات (جابه‌جایی دقیق
///      پیکسلی با MoveWindow + تمام‌صفحه) تا نرم‌افزار روی مانیتور اصلی
///      قابل استفاده بماند؛
///   ۳) بارگذاری زندهٔ بخش‌ها بر اساس نوع: صف پزشک (visits/queue) /
///      صف خدمت (receptions/today با فیلتر ServiceId) / نوبت‌های امروز؛
///   ۴) تازه‌سازی: هر ۳۰ ثانیه + رویداد بلادرنگ صف؛ خطای شبکه فهرست
///      قبلی را نگه می‌دارد (نمایشگر عمومی هرگز خالی نمی‌شود).
/// خروج: Escape — تنظیمات: راست‌کلیک (مخفی از دید مراجع).
/// </summary>
public partial class WaitingRoomDisplayWindow : Window
{
    private readonly IApiClient _api;
    private readonly IRealtimeClient? _realtime;
    private readonly WaitingRoomConfig _config;
    private readonly WaitingDisplayVm _vm = new();
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _refreshTimer;

    // ═══ قلم‌های رنگ ثابت ردیف‌ها (فریز برای رندر پایدار کیوسک) ═══
    private static readonly Brush WaitingRowBrush = Freeze(Color.FromRgb(0x14, 0x2E, 0x4A));
    private static readonly Brush CurrentRowBrush = Freeze(Color.FromRgb(0x11, 0x3B, 0x2D));
    private static readonly Brush AppointmentRowBrush = Freeze(Color.FromRgb(0x12, 0x2B, 0x3E));
    private static readonly Brush BlueNumber = Freeze(Color.FromRgb(0x1E, 0x5B, 0xA8));
    private static readonly Brush GreenNumber = Freeze(Color.FromRgb(0x1B, 0x6E, 0x4A));

    /// <summary>ساخت قلم فریزشده — رندر سردِ بدون تخصیص در هر فریم</summary>
    private static Brush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    /// <summary>سازنده — پیکربندی خوانده و چیدمان/ساعت‌ها راه می‌افتد</summary>
    public WaitingRoomDisplayWindow()
    {
        InitializeComponent();
        _api = App.Services.GetRequiredService<IApiClient>();
        _config = WaitingRoomConfig.Load();

        // پرچم‌های نمایش ساعت/تاریخ + کارت‌های بخش به DataContext
        _vm.ShowAnalogClock = ToVis(_config.ShowAnalogClock);
        _vm.ShowDigitalClock = ToVis(_config.ShowDigitalClock);
        _vm.ShowLongDate = ToVis(_config.ShowLongDate);
        BuildSectionCards();
        DataContext = _vm;

        // نام واقعی درمانگاه — مستقل از تنظیمات یا نام چاپی
        ClinicNameText.Text = _config.EffectiveClinicName();
        VersionText.Text = $"نسخهٔ {Helpers.AppInfo.VersionText}";

        // ساعت و تاریخ — هر ثانیه (دیجیتال ثانیه‌شمار دارد)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // تازه‌سازی بخش‌ها — هر ۳۰ ثانیه
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += (_, _) => _ = LoadAllSectionsAsync();
        _refreshTimer.Start();

        // بلادرنگ با رویداد صف (فراخوان بیمار/ثبت نوبت/پذیرش) — best-effort
        try
        {
            _realtime = App.Services.GetService(typeof(IRealtimeClient)) as IRealtimeClient;
            if (_realtime is not null)
                _realtime.QueueChanged += (_, _) => Dispatcher.BeginInvoke(() => _ = LoadAllSectionsAsync());
        }
        catch
        {
            // بدون بلادرنگ هم تایمر ۳۰ ثانیه کار را انجام می‌دهد
        }

        Loaded += OnWindowLoaded;
    }

    /// <summary>تبدیل bool به Visibility برای اتصال XAML</summary>
    private static System.Windows.Visibility ToVis(bool value)
        => value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>
    /// پس از آماده‌شدن هندل پنجره — انتقال دقیق به مانیتور انتخابی.
    /// MoveWindow با مختصات پیکسلی واقعی کار می‌کند (مستقل از DPI)؛ پس از
    /// انتقال، پنجره Maximized می‌شود و روی همان مانیتور تمام‌صفحه می‌شود.
    /// </summary>
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var target = FindTargetScreen();
            if (target is not null)
            {
                var handle = new WindowInteropHelper(this).Handle;
                MoveWindow(handle, target.X, target.Y, target.Width, target.Height, true);
            }
        }
        catch
        {
            // هر خطای مانیتور — نمایشگر روی مانیتور فعلی تمام‌صفحه می‌ماند
        }
        WindowState = WindowState.Maximized;
        _ = LoadAllSectionsAsync();
    }

    /// <summary>
    /// یافتن مانیتور هدف بر اساس نام دستگاه ذخیره‌شده — تهی = مانیتور اصلی.
    /// شمارش با P/Invoke سبک (Helpers/Monitors) — مستقل از DPI، مختصات پیکسلی.
    /// </summary>
    private static Helpers.MonitorInfo? FindTargetScreen()
        => Helpers.Monitors.Find(WaitingRoomConfig.Load().ScreenDeviceName);

    /// <summary>ساخت کارت‌های بخش مطابق پیکربندی — هم‌ترتیب با کشیدن‌ورهاکردنِ تنظیمات</summary>
    private void BuildSectionCards()
    {
        _vm.Sections.Clear();
        foreach (var section in _config.Sections)
            _vm.Sections.Add(new WaitingDisplaySection { Title = SectionTitle(section) });
    }

    /// <summary>عنوان مؤثر بخش — عنوان دلخواه یا پیش‌فرضِ نوع‌محور</summary>
    private static string SectionTitle(WaitingRoomSection s) => !string.IsNullOrWhiteSpace(s.Title)
        ? s.Title
        : s.Type switch
        {
            WaitingRoomSectionType.DoctorQueue => s.DoctorId is null
                ? "صف انتظار ویزیت"
                : $"صف {s.DoctorName ?? "پزشک"}",
            WaitingRoomSectionType.ServiceQueue => $"صف {s.ServiceName ?? "خدمت"}",
            _ => "نوبت‌های امروز",
        };

    /// <summary>به‌روزرسانی ساعت دیجیتال و تاریخ بلند شمسی</summary>
    private void UpdateClock()
    {
        var now = DateTime.Now;
        DigitalClockText.Text = PersianDateHelper.ToPersianDigits(now.ToString("HH:mm:ss"));
        // تاریخ بلند — «تاریخ امروز : ۲۳ مهر ماه ۱۴۰۵»
        var (y, m, d) = PersianDateHelper.ToJalali(now);
        LongDateText.Text =
            $"تاریخ امروز : {PersianDateHelper.ToPersianDigits(d.ToString())} " +
            $"{PersianDateHelper.MonthNames[m - 1]} ماه {PersianDateHelper.ToPersianDigits(y.ToString())}";
    }

    /// <summary>
    /// بارگذاری دادهٔ همهٔ بخش‌ها — هر «کلید یکتا» فقط یک‌بار از سرور خوانده
    /// می‌شود (هر پزشک یک GET، خدمات یک GET، نوبت‌ها یک GET) و میان کارت‌های
    /// هم‌نوع تقسیم می‌شود؛ سپس همهٔ کارت‌ها با هم تازه می‌شوند (بدون چشمک).
    /// </summary>
    private async Task LoadAllSectionsAsync()
    {
        try
        {
            // ── ۱) صف ویزیت — یک GET به‌ازای هر پزشکِ یکتا (تهی = همه) ──
            var queueByDoctor = new Dictionary<long, (QueueItemJson? Current, List<QueueItemJson> Waiting)>();
            foreach (var doctorId in _config.Sections
                         .Where(s => s.Type == WaitingRoomSectionType.DoctorQueue)
                         .Select(s => s.DoctorId ?? 0).Distinct())
            {
                var url = "visits/queue" + (doctorId > 0 ? $"?doctorId={doctorId}" : string.Empty);
                var response = await _api.GetAsync<JsonElement>(url);
                var data = UnwrapData(response);
                if (data.ValueKind != JsonValueKind.Object)
                    continue;
                QueueItemJson? current = null;
                if (data.TryGetProperty("current", out var c) && c.ValueKind == JsonValueKind.Object)
                    current = QueueItemJson.From(c);
                var waiting = new List<QueueItemJson>();
                if (data.TryGetProperty("waiting", out var w) && w.ValueKind == JsonValueKind.Array)
                    waiting.AddRange(w.EnumerateArray().Select(QueueItemJson.From));
                queueByDoctor[doctorId] = (current, waiting);
            }

            // ── ۲) صف خدمات — یک GET از پذیرش‌های امروز + فیلتر سمت کلاینت ──
            var serviceRows = new Dictionary<long, List<WaitingDisplayRow>>();
            var hasServiceSections = _config.Sections.Any(s =>
                s.Type == WaitingRoomSectionType.ServiceQueue && s.ServiceId is not null);
            if (hasServiceSections)
            {
                var response = await _api.GetAsync<JsonElement>("receptions/today");
                var data = UnwrapData(response);
                if (data.ValueKind == JsonValueKind.Array)
                {
                    var all = data.EnumerateArray().ToList();
                    foreach (var section in _config.Sections.Where(s =>
                                 s.Type == WaitingRoomSectionType.ServiceQueue && s.ServiceId is not null))
                    {
                        serviceRows[section.ServiceId!.Value] = all
                            .Where(r => r.TryGetProperty("serviceId", out var sid)
                                        && sid.ValueKind == JsonValueKind.Number
                                        && sid.GetInt64() == section.ServiceId
                                        && r.TryGetProperty("status", out var st)
                                        && st.ValueKind == JsonValueKind.Number
                                        && st.GetInt32() is 1 or 2) // پذیرش‌شده/درصف
                            .Take(7)
                            .Select(r => new WaitingDisplayRow(
                                PersianDigits(((r.TryGetProperty("id", out var rid)
                                    && rid.ValueKind == JsonValueKind.Number ? rid.GetInt64() : 0) % 100).ToString()),
                                r.TryGetProperty("patientName", out var pn) ? pn.GetString() ?? "" : "",
                                r.TryGetProperty("doctorName", out var dn) ? dn.GetString() ?? "" : "",
                                WaitingRowBrush, BlueNumber))
                            .ToList();
                    }
                }
            }

            // ── ۳) نوبت‌های امروز — یک GET مشترک ──
            List<WaitingDisplayRow>? appointmentRows = null;
            if (_config.Sections.Any(s => s.Type == WaitingRoomSectionType.TodayAppointments))
            {
                var response = await _api.GetAsync<JsonElement>("dashboard/appointments-today?limit=12");
                var data = UnwrapData(response);
                if (data.ValueKind == JsonValueKind.Array)
                {
                    appointmentRows = data.EnumerateArray().Take(8).Select(a => new WaitingDisplayRow(
                        a.TryGetProperty("time", out var t) && t.ValueKind == JsonValueKind.String
                            ? PersianDigits(t.GetString() ?? "—")
                            : "—",
                        a.TryGetProperty("patientName", out var p) ? p.GetString() ?? "" : "",
                        a.TryGetProperty("doctorName", out var doc) && doc.ValueKind == JsonValueKind.String
                            ? $"پزشک: {doc.GetString()}"
                            : "",
                        AppointmentRowBrush, BlueNumber)).ToList();
                }
            }

            // ── ۴) تازه‌سازی همهٔ کارت‌ها — کارت i متناظر بخش i است ──
            for (var i = 0; i < _config.Sections.Count && i < _vm.Sections.Count; i++)
            {
                var section = _config.Sections[i];
                var card = _vm.Sections[i];
                switch (section.Type)
                {
                    case WaitingRoomSectionType.DoctorQueue
                        when queueByDoctor.TryGetValue(section.DoctorId ?? 0, out var queue):
                    {
                        card.Rows.Clear();
                        if (queue.Current is { } current)
                            card.Rows.Add(new WaitingDisplayRow(
                                PersianDigits(current.QueueNumber.ToString()), current.Name, "در حال ویزیت",
                                CurrentRowBrush, GreenNumber));
                        foreach (var waiting in queue.Waiting.Take(7))
                            card.Rows.Add(new WaitingDisplayRow(
                                PersianDigits(waiting.QueueNumber.ToString()), waiting.Name, waiting.Subtitle,
                                WaitingRowBrush, BlueNumber));
                        break;
                    }
                    case WaitingRoomSectionType.ServiceQueue
                        when section.ServiceId is not null && serviceRows.TryGetValue(section.ServiceId.Value, out var rows):
                        ApplyRows(card, rows, append: false);
                        break;
                    case WaitingRoomSectionType.TodayAppointments when appointmentRows is not null:
                        ApplyRows(card, appointmentRows, append: false);
                        break;
                }
            }
        }
        catch
        {
            // خطای شبکه — کارت‌های قبلی می‌مانند؛ تلاش بعدی با تایمر/رویداد صف
        }
    }

    /// <summary>نوشتن ردیف‌های آماده روی کارت (خدمات/نوبت‌ها) — append یعنی به ردیف‌های موجود اضافه کن</summary>
    private static void ApplyRows(WaitingDisplaySection card, IEnumerable<WaitingDisplayRow> rows, bool append)
    {
        if (!append)
            card.Rows.Clear();
        foreach (var row in rows)
            card.Rows.Add(row);
    }

    /// <summary>بازکردن پاکت استاندارد ApiResponse — ریشه یا .data</summary>
    private static JsonElement UnwrapData(JsonElement root)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)
            ? (d.ValueKind == JsonValueKind.Null ? default : d)
            : root;

    /// <summary>ردیف خام صف ویزیت از JSON سرور</summary>
    private sealed record QueueItemJson(string Name, string Subtitle, int QueueNumber)
    {
        public static QueueItemJson From(JsonElement e) => new(
            e.TryGetProperty("patientName", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "",
            e.TryGetProperty("visitTypeText", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "",
            e.TryGetProperty("queueNumber", out var q) && q.ValueKind == JsonValueKind.Number ? q.GetInt32() : 0);
    }

    /// <summary>خروج با Escape — تنها راه بستن (بدون دکمهٔ قابل‌کلیک برای مراجع)</summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    /// <summary>راست‌کلیک = تنظیمات نمایشگر — مخفی از دید عمومی (Cursor=None است)</summary>
    private void Window_RightClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var settings = new WaitingRoomSettingsWindow();
        settings.ShowDialog();
        // پس از بستن تنظیمات، نمایشگر با پیکربندی تازه بازسازی می‌شود
        Close();
        var fresh = new WaitingRoomDisplayWindow();
        fresh.Show();
    }

    /// <summary>تبدیل ارقام لاتین به فارسی — نمایش عمومی کیوسک</summary>
    private static string PersianDigits(string value)
    {
        var digits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (char.IsAsciiDigit(chars[i]))
                chars[i] = digits[chars[i] - '0'];
        return new string(chars);
    }

    #region Native — انتقال دقیق پیکسلی پنجره به مانیتور هدف

    /// <summary>جابه‌جایی پنجره به مستطیل پیکسلی مانیتور (مستقل از DPI)</summary>
    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    #endregion
}
