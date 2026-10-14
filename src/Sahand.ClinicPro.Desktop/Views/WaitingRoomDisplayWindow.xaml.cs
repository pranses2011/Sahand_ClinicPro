using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
/// کارت بخش نمایشگری — عنوان + ردیف‌های زنده + ستونِ میزبان (چرخهٔ ۷۵:
/// کارت‌ها ستونی چیده می‌شوند؛ ستون میزبان برای رد کردن کارت هنگام تازه‌سازی)
/// </summary>
public class WaitingDisplaySection
{
    public string Title { get; init; } = string.Empty;
    public ObservableCollection<WaitingDisplayRow> Rows { get; } = new();
}

/// <summary>
/// ViewModel سبک نمایشگر — پرچم‌های نمایش ساعت/تاریخ/تبلیغ + کارت‌های بخش برای XAML
/// </summary>
public class WaitingDisplayVm
{
    public System.Windows.Visibility ShowAnalogClock { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowDigitalClock { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowLongDate { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowAdSubtitle { get; set; } = System.Windows.Visibility.Visible;
    public System.Windows.Visibility ShowAdFeatures { get; set; } = System.Windows.Visibility.Collapsed;
    public System.Windows.Visibility ShowAdVersion { get; set; } = System.Windows.Visibility.Collapsed;
    public ObservableCollection<WaitingDisplaySection> Sections { get; } = new();
}

/// <summary>
/// کد پشت نمایشگر سالن انتظار — چرخهٔ ۷۴ + بازآرایی چرخهٔ ۷۵.
/// کارهای چرخهٔ ۷۵ (گزارش مالک):
///   ۱) نوار بالا سه‌ستونه: راست = پشتهٔ ساعت / میانه = نام درمانگاه از سرور
///      لایسنس + اعلانِ قابل‌تنظیم / چپ = تبلیغ فقط با تصویر و نام محصول؛
///   ۲) ناحیهٔ پایین = کارت‌های صف‌ها با چیدمان «ستونی»: تعداد ستون از
///      تنظیمات (۲ تا ۴) و هر ستون می‌تواند دو بخش را روی هم جا دهد؛
///   ۳) در صف عمومی (بدون انتخاب پزشک) نام پزشک زیر نام بیمار می‌نشیند؛
///   ۴) تازه‌سازی سریع‌تر (۱۵ ثانیه) + رویداد بلادرنگ + تازه‌سازی با focus؛
///   ۵) نام درمانگاه: اولویت با نام همگام‌شده از سرور لایسنس (تنظیم ClinicInfo)
///      است — «منظورم از نام درمانگاه همون نام درمانگاهی است که از سرور
///      لایسنس میاد»؛ نام دستی تنظیمات فقط override صریح است.
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

        // پرچم‌های نمایش ساعت/تاریخ/تبلیغ + کارت‌های بخش به DataContext
        _vm.ShowAnalogClock = ToVis(_config.ShowAnalogClock);
        _vm.ShowDigitalClock = ToVis(_config.ShowDigitalClock);
        _vm.ShowLongDate = ToVis(_config.ShowLongDate);
        _vm.ShowAdSubtitle = ToVis(_config.ShowAdSubtitle);
        _vm.ShowAdFeatures = ToVis(_config.ShowAdFeatures);
        _vm.ShowAdVersion = ToVis(_config.ShowAdVersion);
        BuildSectionCards();
        DataContext = _vm;

        // نام درمانگاه — از سرور لایسنس (تنظیم ClinicInfo) به‌صورت پویا؛
        // تا رسیدن پاسخ، مقدار محلی فوری نشانت می‌ماند که صفحه خالی نماند
        ClinicNameText.Text = _config.EffectiveClinicName();
        _ = LoadClinicNameFromServerAsync();

        // اعلان زیر نام درمانگاه — از تنظیمات نمایشگر (خالی = پیش‌فرض)
        AnnouncementText.Text = string.IsNullOrWhiteSpace(_config.Announcement)
            ? "لطفاً در سالن انتظار حضور داشته باشید و با فراخوانی شمارهٔ نوبت به اتاق مربوط مراجعه کنید"
            : _config.Announcement.Trim();

        // تبلیغ — نسخهٔ محصول (فقط اگر سوییچ روشن باشد دیده می‌شود)
        AdVersionText.Text = Helpers.AppInfo.VersionText;

        // ساعت و تاریخ — هر ثانیه (دیجیتال ثانیه‌شمار دارد)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // تازه‌سازی بخش‌ها — چرخهٔ ۷۵: ۳۰ → ۱۵ ثانیه (گزارش مالک: «بعد از
        // پذیرش و نوبت‌دهی لیست‌ها بروز نمی‌شوند»؛ نصف‌کردن فاصله + سه محرک
        // دیگر یعنی در بدبینانهٔ حالت ۱۵ ثانیه تأخیر، نه «تا ری‌استارت»)
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
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
            // بدون بلادرنگ هم تایمر ۱۵ ثانیه کار را انجام می‌دهد
        }

        // تازه‌سازی با هر بار دیده‌شدن پنجره (بازگشت از تنظیمات/alt-tab اپراتور)
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                _ = LoadAllSectionsAsync();
        };

        Loaded += OnWindowLoaded;
    }

    /// <summary>تبدیل bool به Visibility برای اتصال XAML</summary>
    private static System.Windows.Visibility ToVis(bool value)
        => value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — نام درمانگاه از سرور لایسنس ═══
    /// «گفته بودم نام درمانگاه نام خود درمانگاه باشه نه نام نرم افزار و منظورم
    /// از نام درمانگاه همون نام درمانگاهی است که از سرور لایسنس میاد» —
    /// پنل لایسنس هنگام فعال‌سازی، نام درمانگاه را در تنظیم ClinicInfo ذخیره
    /// می‌کند (تنها مرجع تغییر — چرخهٔ ۲۷)؛ اینجا همان مقدار خوانده می‌شود.
    /// نام دستی تنظیمات نمایشگر همچنان اولویت نخستِ صریح است (override اپراتور).
    /// </summary>
    private async Task LoadClinicNameFromServerAsync()
    {
        try
        {
            var response = await _api.GetAsync<SettingsResponse>("settings?category=ClinicInfo");
            var serverName = response?.Data?.FirstOrDefault(x => x.Key == "ClinicName")?.Value;
            // فقط اگر اپراتور نام دستی ننوشته باشد، نام سرور لایسنس می‌نشیند
            if (string.IsNullOrWhiteSpace(_config.ClinicName) && !string.IsNullOrWhiteSpace(serverName))
                ClinicNameText.Text = serverName.Trim();
        }
        catch
        {
            // قطعی شبکه — نام محلیِ ازقبل‌نشانده می‌ماند
        }
    }

    /// <summary>
    /// پس از آماده‌شدن هندل پنجره — انتقال دقیق به مانیتور انتخابی.
    /// MoveWindow با مختصات پیکسلی واقعی کار می‌کند (مستقل از DPI)؛ پس از
    /// انتقال، پنجره Maximized می‌شود و روی همان مانیتور تمام‌صفحه می‌شود.
    /// </summary>
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var target = Helpers.Monitors.Find(_config.ScreenDeviceName);
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

    /// <summary>ساخت کارت‌های بخش مطابق پیکربندی + چیدمان ستونی — چرخهٔ ۷۵</summary>
    private void BuildSectionCards()
    {
        _vm.Sections.Clear();
        foreach (var section in _config.Sections)
            _vm.Sections.Add(new WaitingDisplaySection { Title = SectionTitle(section) });
        BuildColumnsLayout();
    }

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — چیدمان ستونی کارت‌های صف ═══
    /// «بصورت ستونی قرار بگیرن نه سطری و بشه در یک ستون ۲ مورد هم قرار داد
    /// (تعداد ستون‌ها هم از تنظیماتش قابل تنظیم باشه)»:
    ///   • تعداد ستون از پیکربندی (۲/۳/۴ — پیش‌فرض ۳)؛
    ///   • بخش‌ها به ترتیبِ Drag&amp;Drop تنظیمات، ستون‌پرشونده پر می‌شوند:
    ///     هر ستون حداکثر دو بخش می‌گیرد (روی هم، فاصلهٔ ۱۲px)؛
    ///   • هر بخش در گرید ستونش نصف ارتفاع (یا کامل اگر تنها است) می‌گیرد؛
    ///   • با تغییر اندازهٔ پنجره چیزی بازسازی نمی‌شود (گرید کشسان است).
    /// </summary>
    private void BuildColumnsLayout()
    {
        ColumnsHost.ColumnDefinitions.Clear();
        ColumnsHost.Children.Clear();

        var columnCount = Math.Clamp(_config.ColumnCount, 1, 4);
        for (var i = 0; i < columnCount; i++)
            ColumnsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // بخش‌ها ستون‌پرشونده توزیع می‌شوند — هر ستون حداکثر دو بخش
        const int maxPerColumn = 2;
        var cardIndex = 0;
        for (var col = 0; col < columnCount && cardIndex < _vm.Sections.Count; col++)
        {
            var columnGrid = new Grid { Margin = new Thickness(col == 0 ? 0 : 12, 0, 0, 0) };
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var placed = 0;
            while (placed < maxPerColumn && cardIndex < _vm.Sections.Count)
            {
                var card = CreateSectionCard(_vm.Sections[cardIndex], isAloneInColumn: false);
                Grid.SetColumn(columnGrid, col);
                Grid.SetRow(card, placed);
                columnGrid.Children.Add(card);
                placed++;
                cardIndex++;
            }

            ColumnsHost.Children.Add(columnGrid);
        }
    }

    /// <summary>
    /// ساخت کارت بصری یک بخش — عنوان رنگی + ردیف‌های بیمار (همان زبان بصری
    /// چرخهٔ ۷۴؛ جدا شده تا هم چیدمان ستونی جدید هم پیش‌نمایش از آن استفاده کنند).
    /// </summary>
    private static FrameworkElement CreateSectionCard(WaitingDisplaySection section, bool isAloneInColumn)
    {
        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(16, 14),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x23, 0x3A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5C)),
            BorderThickness = new Thickness(1.2),
        };

        var dock = new DockPanel();

        // عنوان کارت
        var header = new TextBlock
        {
            Text = section.Title,
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x7F, 0xC4, 0xFF)),
            Margin = new Thickness(2, 0, 2, 10),
        };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);

        // ردیف‌های بیمار — یک‌درمیان با فاصلهٔ نفس
        var rowsHost = new ItemsControl { ItemsSource = section.Rows };
        rowsHost.ItemTemplate = CreateRowTemplate();
        dock.Children.Add(rowsHost);

        card.Child = dock;
        return card;
    }

    /// <summary>قالب ردیف بیمار — شمارهٔ دایره‌ای رنگی + نام + زیرعنوان</summary>
    private static DataTemplate CreateRowTemplate()
    {
        // قالب در XAML (منبع مشترک پنجره) تعریف شده و اینجا فقط مرجع می‌شود؛
        // برای استقلال از منابع، قالب معادل با کد ساخته می‌شود
        var numberFactory = new FrameworkElementFactory(typeof(Border));
        numberFactory.SetValue(Border.WidthProperty, 44.0);
        numberFactory.SetValue(Border.HeightProperty, 44.0);
        numberFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(22));
        numberFactory.SetValue(Border.BackgroundProperty, new Binding("NumberBrush"));
        numberFactory.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Center);
        var numberText = new FrameworkElementFactory(typeof(TextBlock));
        numberText.SetValue(TextBlock.TextProperty, new Binding("Number"));
        numberText.SetValue(TextBlock.FontSizeProperty, 18.0);
        numberText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        numberText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        numberText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        numberText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        numberText.SetValue(TextBlock.FontFamilyProperty, new DynamicResourceExtension("App.CodeFont"));
        numberFactory.AppendChild(numberText);

        var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
        nameFactory.SetValue(TextBlock.TextProperty, new Binding("Name"));
        nameFactory.SetValue(TextBlock.FontSizeProperty, 19.0);
        nameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        nameFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)));

        var subtitleFactory = new FrameworkElementFactory(typeof(TextBlock));
        subtitleFactory.SetValue(TextBlock.TextProperty, new Binding("Subtitle"));
        subtitleFactory.SetValue(TextBlock.FontSizeProperty, 13.5);
        subtitleFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x9D, 0xB8, 0xCF)));
        subtitleFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 3, 0, 0));
        // زیرعنوان فقط وقتی مقدار دارد جا بگیرد (صف عمومی: نام پزشک)
        subtitleFactory.SetValue(TextBlock.VisibilityProperty,
            new Binding("Subtitle") { Converter = EmptyToCollapsedConverter.Instance });

        var patientStack = new FrameworkElementFactory(typeof(StackPanel));
        patientStack.SetValue(StackPanel.MarginProperty, new Thickness(14, 0, 0, 0));
        patientStack.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
        patientStack.AppendChild(nameFactory);
        patientStack.AppendChild(subtitleFactory);

        var rowDock = new FrameworkElementFactory(typeof(DockPanel));
        rowDock.AppendChild(numberFactory);
        rowDock.AppendChild(patientStack);

        var rowBorder = new FrameworkElementFactory(typeof(Border));
        rowBorder.SetValue(Border.BackgroundProperty, new Binding("RowBrush"));
        rowBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        rowBorder.SetValue(Border.PaddingProperty, new Thickness(13, 9, 13, 9));
        rowBorder.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 8));
        rowBorder.AppendChild(rowDock);

        return new DataTemplate { VisualTree = rowBorder };
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
    /// چرخهٔ ۷۵: در صف عمومی، نام پزشک زیر نام بیمار می‌نشیند (DoctorName جدید
    /// پاسخ سرور — فقط برای DoctorId تهی).
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
                                PersianDigits(current.QueueNumber.ToString()), current.Name,
                                string.IsNullOrWhiteSpace(current.DoctorName)
                                    ? "در حال ویزیت"
                                    : $"{current.DoctorName} — در حال ویزیت",
                                CurrentRowBrush, GreenNumber));
                        foreach (var waiting in queue.Waiting.Take(7))
                            card.Rows.Add(new WaitingDisplayRow(
                                PersianDigits(waiting.QueueNumber.ToString()), waiting.Name,
                                // چرخهٔ ۷۵ — صف عمومی: نام پزشک زیر نام بیمار؛
                                // صف یک پزشکِ مشخص: نوع ویزیت (همان رفتار قبل)
                                section.DoctorId is null && !string.IsNullOrWhiteSpace(waiting.DoctorName)
                                    ? waiting.DoctorName
                                    : waiting.Subtitle,
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

    /// <summary>ردیف خام صف ویزیت از JSON سرور — چرخهٔ ۷۵: DoctorName جدید</summary>
    private sealed record QueueItemJson(string Name, string Subtitle, int QueueNumber, string? DoctorName)
    {
        public static QueueItemJson From(JsonElement e) => new(
            e.TryGetProperty("patientName", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "",
            e.TryGetProperty("visitTypeText", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "",
            e.TryGetProperty("queueNumber", out var q) && q.ValueKind == JsonValueKind.Number ? q.GetInt32() : 0,
            e.TryGetProperty("doctorName", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null);
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

    /// <summary>پاسخ تنظیمات ClinicInfo — برای نام درمانگاه از سرور لایسنس</summary>
    private sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    /// <summary>ردیف تنظیم — کلید/مقدار</summary>
    private sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    #region Native — انتقال دقیق پیکسلی پنجره به مانیتور هدف

    /// <summary>جابه‌جایی پنجره به مستطیل پیکسلی مانیتور (مستقل از DPI)</summary>
    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    #endregion
}

/// <summary>
/// مبدل «زیرعنوان خالی = جمع‌شده» — ردیف‌های بدون زیرعنوان (مثلاً صف عمومی وقتی
/// پزشکی تعیین نشده و پاسخ هم نامی نداشته) ارتفاع اضافه نمی‌گیرند.
/// </summary>
public sealed class EmptyToCollapsedConverter : System.Windows.Data.IValueConverter
{
    public static readonly EmptyToCollapsedConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter,
        System.Globalization.CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string)
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter,
        System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
