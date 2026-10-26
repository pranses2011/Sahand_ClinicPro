using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>آمار ظرفیت یک روز — نوبت‌های رزروشده از کل ظرفیت روز</summary>
public readonly record struct CalendarDayStat(int Booked, int Total);

/// <summary>آمار یک ماه برای نمای انتخاب ماه — رزروشده، کل ظرفیت و روزهای کاری</summary>
public readonly record struct CalendarMonthStat(int Booked, int Total, int WorkDays);

/// <summary>
/// کنترل تقویم شمسی نوبت‌دهی — طراحی تازه (تأیید مالک روی پیش‌نمایش تعاملی):
/// سربرگ گرادیانی برند، خانه‌های روز با نوار ظرفیت طیفی (سبزِ ظرفیت خالی
/// تا قرمزِ تکمیل، بر اساس درصد پرشدن)، شمار نوبت، حالت هاشور «تعطیل»،
/// برچسب «امروز»، انتخاب گرادیانی و نمای ماه‌ها با آمار گرافیکی هر ماه.
/// قرارداد داده‌ای پیشین (SelectedDate / DisplayMonth / DisplayYear /
/// MarkedDates / DisabledDates / MinDate / MaxDate) حفظ شده و ورودی‌های
/// DayStats و MonthStats برای ظرفیت و آمار افزوده شده‌اند.
/// فونت از فونت انتخابی کاربر در تنظیمات ظاهر به ارث می‌رسد؛ هیچ فونت
/// ثابتی در این کنترل نیست.
/// </summary>
public partial class PersianCalendarControl : UserControl
{
    /// <summary>تقویم فارسی دات‌نت — برای طول دقیق ماه‌های شمسی</summary>
    private static readonly System.Globalization.PersianCalendar Persian = new();

    /// <summary>نمای فعلی تقویم — روزها / انتخاب ماه / انتخاب سال</summary>
    private enum CalendarViewMode
    {
        /// <summary>شبکهٔ روزهای ماه</summary>
        Days,
        /// <summary>انتخاب ماه از میان ۱۲ ماه سال</summary>
        Months,
        /// <summary>انتخاب سال از گرید ۱۲ ساله</summary>
        Years,
    }

    /// <summary>نمای فعلی — پیش‌فرض روزها</summary>
    private CalendarViewMode _viewMode = CalendarViewMode.Days;

    /// <summary>سال مبنای گرید انتخاب سال (شروع بازهٔ ۱۲ ساله)</summary>
    private int _yearGridBase;

    /// <summary>سازنده — اتصال رویدادها و رندر اولیه</summary>
    public PersianCalendarControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
        PrevButton.MouseLeftButtonUp += (_, _) => Navigate(-1);
        NextButton.MouseLeftButtonUp += (_, _) => Navigate(+1);
        TitleButton.MouseLeftButtonUp += (_, _) => TitleClicked();
        GoTodayButton.MouseLeftButtonUp += (_, _) => GoToToday();
    }

    /* ─────────────  خصوصیت‌های وابستگی  ───────────── */

    /// <summary>تاریخ انتخاب‌شده (میلادی) — تهی یعنی انتخاب نشده</summary>
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?),
            typeof(PersianCalendarControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((PersianCalendarControl)d).Render()));

    /// <summary>ماه نمایشی (۱ تا ۱۲ شمسی)</summary>
    public static readonly DependencyProperty DisplayMonthProperty =
        DependencyProperty.Register(nameof(DisplayMonth), typeof(int),
            typeof(PersianCalendarControl), new PropertyMetadata(1, OnDisplayChanged));

    /// <summary>سال نمایشی شمسی</summary>
    public static readonly DependencyProperty DisplayYearProperty =
        DependencyProperty.Register(nameof(DisplayYear), typeof(int),
            typeof(PersianCalendarControl), new PropertyMetadata(IranClock.Now.Year - 621, OnDisplayChanged));

    /// <summary>خروجی: تاریخ شمسی انتخاب‌شده</summary>
    public static readonly DependencyProperty SelectedShamsiProperty =
        DependencyProperty.Register(nameof(SelectedShamsi), typeof(string),
            typeof(PersianCalendarControl), new PropertyMetadata(string.Empty));

    /// <summary>تاریخ‌های دارای نشانگر (سازگاری پیشین)</summary>
    public static readonly DependencyProperty MarkedDatesProperty =
        DependencyProperty.Register(nameof(MarkedDates), typeof(IDictionary<DateTime, int>),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>تاریخ‌های غیرفعال (تعطیلات و غیبت‌ها و روزهای تکمیل)</summary>
    public static readonly DependencyProperty DisabledDatesProperty =
        DependencyProperty.Register(nameof(DisabledDates), typeof(IEnumerable<DateTime>),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>آمار ظرفیت روزانه — تاریخ ← (رزروشده، کل ظرفیت)</summary>
    public static readonly DependencyProperty DayStatsProperty =
        DependencyProperty.Register(nameof(DayStats), typeof(IDictionary<DateTime, CalendarDayStat>),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>آمار ماه‌های سال نمایشی — شمارهٔ ماه ← آمار آن ماه</summary>
    public static readonly DependencyProperty MonthStatsProperty =
        DependencyProperty.Register(nameof(MonthStats), typeof(IDictionary<int, CalendarMonthStat>),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>حد پایین مجاز</summary>
    public static readonly DependencyProperty MinDateProperty =
        DependencyProperty.Register(nameof(MinDate), typeof(DateTime?),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>حد بالای مجاز</summary>
    public static readonly DependencyProperty MaxDateProperty =
        DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /* ─────────────  رویدادها  ───────────── */

    /// <summary>رویداد انتخاب تاریخ</summary>
    public event EventHandler<DateTime?>? DateSelected;

    /// <summary>رویداد تغییر ماه نمایشی</summary>
    public event EventHandler? MonthChanged;

    /* ─────────────  پوشش خصوصیت‌ها  ───────────── */

    /// <summary>تاریخ انتخاب‌شده</summary>
    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>ماه نمایشی</summary>
    public int DisplayMonth
    {
        get => (int)GetValue(DisplayMonthProperty);
        set => SetValue(DisplayMonthProperty, value);
    }

    /// <summary>سال نمایشی</summary>
    public int DisplayYear
    {
        get => (int)GetValue(DisplayYearProperty);
        set => SetValue(DisplayYearProperty, value);
    }

    /// <summary>متن شمسی انتخاب</summary>
    public string SelectedShamsi
    {
        get => (string)GetValue(SelectedShamsiProperty);
        private set => SetValue(SelectedShamsiProperty, value);
    }

    /// <summary>نقشه تاریخ‌های دارای نشانگر</summary>
    public IDictionary<DateTime, int>? MarkedDates
    {
        get => (IDictionary<DateTime, int>?)GetValue(MarkedDatesProperty);
        set => SetValue(MarkedDatesProperty, value);
    }

    /// <summary>تاریخ‌های غیرفعال</summary>
    public IEnumerable<DateTime>? DisabledDates
    {
        get => (IEnumerable<DateTime>?)GetValue(DisabledDatesProperty);
        set => SetValue(DisabledDatesProperty, value);
    }

    /// <summary>آمار ظرفیت روزانه</summary>
    public IDictionary<DateTime, CalendarDayStat>? DayStats
    {
        get => (IDictionary<DateTime, CalendarDayStat>?)GetValue(DayStatsProperty);
        set => SetValue(DayStatsProperty, value);
    }

    /// <summary>آمار ماه‌های سال نمایشی</summary>
    public IDictionary<int, CalendarMonthStat>? MonthStats
    {
        get => (IDictionary<int, CalendarMonthStat>?)GetValue(MonthStatsProperty);
        set => SetValue(MonthStatsProperty, value);
    }

    /// <summary>حد پایین</summary>
    public DateTime? MinDate
    {
        get => (DateTime?)GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    /// <summary>حد بالای</summary>
    public DateTime? MaxDate
    {
        get => (DateTime?)GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /* ─────────────  منطق  ───────────── */

    /// <summary>بازخورد تغییر نمایشی</summary>
    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PersianCalendarControl)d).Render();

    /// <summary>رنگ نوار ظرفیت بر اساس نسبت پرشدن — سبز (خالی) تا قرمز (تکمیل)</summary>
    internal static Color CapacityColor(double ratio)
    {
        var hue = 125d - Math.Clamp(ratio, 0d, 1d) * 125d;
        return HslToColor(hue, 0.72d, 0.40d);
    }

    /// <summary>تبدیل HSL به رنگ WPF</summary>
    private static Color HslToColor(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60d % 2 - 1));
        var m = lightness - chroma / 2;
        double r, g, b;
        if (hue < 60) { r = chroma; g = x; b = 0; }
        else if (hue < 120) { r = x; g = chroma; b = 0; }
        else if (hue < 180) { r = 0; g = chroma; b = x; }
        else if (hue < 240) { r = 0; g = x; b = chroma; }
        else if (hue < 300) { r = x; g = 0; b = chroma; }
        else { r = chroma; g = 0; b = x; }
        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    /// <summary>ناوبری فلش‌ها بر اساس نمای فعلی</summary>
    private void Navigate(int delta)
    {
        switch (_viewMode)
        {
            case CalendarViewMode.Days:
                ShiftMonth(delta);
                break;
            case CalendarViewMode.Months:
                DisplayYear += delta;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
                break;
            default:
                _yearGridBase += delta * 12;
                Render();
                break;
        }
    }

    /// <summary>کلیک عنوان — روزها ← ماه‌ها ← سال‌ها ← روزها</summary>
    private void TitleClicked()
    {
        _viewMode = _viewMode switch
        {
            CalendarViewMode.Days => CalendarViewMode.Months,
            CalendarViewMode.Months => CalendarViewMode.Years,
            _ => CalendarViewMode.Days,
        };
        if (_viewMode == CalendarViewMode.Years)
            _yearGridBase = DisplayYear - 6;
        Render();
    }

    /// <summary>پرچم اجرای «برو به امروز» — کلیک پیاپی/هم‌زمان دوباره وارد نشود</summary>
    private bool _goingToToday;

    /// <summary>
    /// رفتن به امروز — ماه جاری و انتخاب خود امروز.
    /// گزارش مالک: کلیک روی «امروز» از برنامه خارج می‌کرد. این مسیر اکنون
    /// محافظت‌شده است: پرچم ضدورود هم‌زمان، انتساب فقط در صورت تغییر، ترتیب
    /// امن (نخست نما، بعد ماه/سال، آخر انتخاب) و پوشش خطا؛ هیچ خطایی از این
    /// دکمه نباید به کاربر برسد یا برنامه را ببندد.
    /// </summary>
    private void GoToToday()
    {
        if (_goingToToday) return;
        _goingToToday = true;
        try
        {
            _viewMode = CalendarViewMode.Days;
            var (y, m, _) = PersianDateHelper.ToJalali(IranClock.Today);
            if (DisplayYear != y) DisplayYear = y;
            if (DisplayMonth != m) DisplayMonth = m;
            if (SelectedDate?.Date != IranClock.Today)
            {
                SelectedDate = IranClock.Today;
                SelectedShamsi = PersianDateHelper.ToShamsiFa(IranClock.Today);
                DateSelected?.Invoke(this, IranClock.Today);
            }
            Render();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GoToToday failed: {ex}");
        }
        finally
        {
            _goingToToday = false;
        }
    }

    /// <summary>جابه‌جایی ماه نمایشی</summary>
    private void ShiftMonth(int delta)
    {
        var m = DisplayMonth + delta;
        var y = DisplayYear;
        if (m < 1) { m = 12; y--; }
        if (m > 12) { m = 1; y++; }
        DisplayMonth = m;
        DisplayYear = y;
        MonthChanged?.Invoke(this, EventArgs.Empty);
        Render();
    }

    /// <summary>
    /// رندر بر اساس نمای فعلی — هرگز نباید استثنا به بالا پرتاب شود؛ خطای
    /// رندر تقویم (دادهٔ ناقص/نبود سرور) نباید برنامه را ببندد.
    /// </summary>
    private void Render()
    {
        try { RenderCore(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Calendar render failed: {ex}"); }
    }

    /// <summary>رندر واقعی بر اساس نمای فعلی</summary>
    private void RenderCore()
    {
        if (DaysGrid is null)
            return;
        if (DisplayMonth is < 1 or > 12)
            DisplayMonth = 1;

        TodayFullText.Text = TodayFullTextValue();
        switch (_viewMode)
        {
            case CalendarViewMode.Months:
                RenderMonthsView();
                return;
            case CalendarViewMode.Years:
                RenderYearsView();
                return;
            default:
                RenderDaysView();
                return;
        }
    }

    /// <summary>متن کامل امروز برای چیپ سربرگ</summary>
    private static string TodayFullTextValue()
    {
        var now = IranClock.Today;
        var (y, m, d) = PersianDateHelper.ToJalali(now);
        var weekday = PersianDateHelper.DayNames[((int)now.DayOfWeek + 1) % 7];
        return "امروز: " + PersianDateHelper.ToPersianDigits(
            $"{weekday} {d} {PersianDateHelper.MonthNames[m - 1]} {y}");
    }

    /// <summary>قلم‌موی هاشور روزهای تعطیل / غیبت</summary>
    private static Brush HatchBrush()
    {
        var drawing = new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF4)),
            Geometry = Geometry.Parse("M0,0 L8,0 L8,8 L0,8 Z"),
        };
        var lines = new GeometryDrawing
        {
            Pen = new Pen(new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC)), 1.6),
            Geometry = Geometry.Parse("M-2,2 L2,-2 M0,8 L8,0 M6,10 L10,6"),
        };
        return new DrawingBrush(new DrawingGroup { Children = { drawing, lines } })
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 8, 8),
            ViewportUnits = BrushMappingMode.Absolute,
        };
    }

    /// <summary>ساخت نوار ظرفیت با نسبت پرشدن و رنگ طیفی سبز تا قرمز</summary>
    private static Grid BuildCapacityBar(double ratio, bool onDark)
    {
        var clamped = Math.Clamp(ratio, 0d, 1d);
        var grid = new Grid { Margin = new Thickness(5, 3, 5, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(clamped, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - clamped, GridUnitType.Star) });
        var track = new Border
        {
            Height = 4.5,
            CornerRadius = new CornerRadius(2.25),
            Background = new SolidColorBrush(onDark
                ? Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)
                : Color.FromRgb(0xE3, 0xEA, 0xF2)),
        };
        Grid.SetColumnSpan(track, 2);
        var fill = new Border
        {
            Height = 4.5,
            CornerRadius = new CornerRadius(2.25),
            Background = new SolidColorBrush(CapacityColor(clamped)),
        };
        Grid.SetColumn(fill, 0);
        grid.Children.Add(track);
        grid.Children.Add(fill);
        return grid;
    }

    /// <summary>نمای انتخاب ماه — هر ماه با آمار گرافیکی (عدد نوبت + نوار ظرفیت + روزهای کاری)</summary>
    private void RenderMonthsView()
    {
        MonthTitleText.Text = $"سال {PersianDateHelper.ToPersianDigits(DisplayYear.ToString())}";
        WeekDaysRow.Visibility = Visibility.Collapsed;
        LegendPanel.Visibility = Visibility.Collapsed;
        SummaryPanel.Visibility = Visibility.Collapsed;
        DaysGrid.Columns = 3;
        DaysGrid.Rows = 4;
        DaysGrid.Children.Clear();

        for (var month = 1; month <= 12; month++)
        {
            var captured = month;
            var isCurrent = month == DisplayMonth;
            var stat = MonthStatFor(month);

            var name = new TextBlock
            {
                Text = PersianDateHelper.MonthNames[month - 1],
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x31, 0x51, 0x7A)),
            };
            var stack = new StackPanel { Margin = new Thickness(4, 7, 4, 6), Children = { name } };

            if (stat is { } s && s.Total > 0)
            {
                var ratio = (double)s.Booked / s.Total;
                stack.Children.Add(new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(s.Booked.ToString()),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x17, 0x2B, 0x4D)),
                    Margin = new Thickness(0, 2, 0, 0),
                });
                stack.Children.Add(new TextBlock
                {
                    Text = "نوبت",
                    FontSize = 8.5,
                    TextAlignment = TextAlignment.Center,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x62, 0x74, 0x8E)),
                });
                // نوار ظرفیت ماه با دو ستون ستاره‌ای (نسبت واقعی) و رنگ طیفی
                var barGrid = new Grid { Margin = new Thickness(8, 4, 8, 3) };
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Clamp(ratio, 0, 1), GridUnitType.Star) });
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - Math.Clamp(ratio, 0, 1), GridUnitType.Star) });
                var track = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(2.5),
                    Background = new SolidColorBrush(isCurrent
                        ? Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)
                        : Color.FromRgb(0xE1, 0xE8, 0xF0)),
                };
                Grid.SetColumnSpan(track, 2);
                var fill = new Border
                {
                    Height = 5,
                    CornerRadius = new CornerRadius(2.5),
                    Background = new SolidColorBrush(CapacityColor(ratio)),
                };
                Grid.SetColumn(fill, 0);
                barGrid.Children.Add(track);
                barGrid.Children.Add(fill);
                stack.Children.Add(barGrid);
                stack.Children.Add(new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(
                        $"{Math.Round(ratio * 100)}٪ ظرفیت • {s.WorkDays} روز کاری"),
                    FontSize = 8.5,
                    TextAlignment = TextAlignment.Center,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x62, 0x74, 0x8E)),
                });
            }
            else
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "—",
                    FontSize = 13,
                    TextAlignment = TextAlignment.Center,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x9A, 0xA7, 0xBB)),
                    Margin = new Thickness(0, 4, 0, 0),
                });
            }

            var cell = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1.4),
                BorderBrush = new SolidColorBrush(isCurrent
                    ? Color.FromRgb(0x00, 0x4D, 0x40)
                    : Color.FromRgb(0xD7, 0xE0, 0xEA)),
                Background = isCurrent
                    ? new LinearGradientBrush(Color.FromRgb(0x00, 0x89, 0x7B), Color.FromRgb(0x00, 0x4D, 0x40), 45)
                    : Brushes.White,
                Child = stack,
            };
            cell.MouseLeftButtonUp += (_, _) =>
            {
                DisplayMonth = captured;
                _viewMode = CalendarViewMode.Days;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
            };
            DaysGrid.Children.Add(cell);
        }
    }

    /// <summary>آمار یک ماه — از MonthStats؛ برای ماه نمایشی از DayStats هم قابل استخراج است</summary>
    private CalendarMonthStat? MonthStatFor(int month)
    {
        if (MonthStats is not null && MonthStats.TryGetValue(month, out var stat))
            return stat;
        if (month != DisplayMonth || DayStats is null)
            return null;
        var booked = 0; var total = 0; var workDays = 0;
        foreach (var (date, dayStat) in DayStats)
        {
            var (_, m, _) = PersianDateHelper.ToJalali(date);
            if (m != month) continue;
            booked += dayStat.Booked;
            total += dayStat.Total;
            if (dayStat.Total > 0) workDays++;
        }
        return total > 0 ? new CalendarMonthStat(booked, total, workDays) : null;
    }

    /// <summary>نمای انتخاب سال — گرید ۱۲ ساله با ناوبری بازه</summary>
    private void RenderYearsView()
    {
        var fromYear = _yearGridBase;
        var toYear = _yearGridBase + 11;
        MonthTitleText.Text =
            $"{PersianDateHelper.ToPersianDigits(fromYear.ToString())} — {PersianDateHelper.ToPersianDigits(toYear.ToString())}";
        WeekDaysRow.Visibility = Visibility.Collapsed;
        LegendPanel.Visibility = Visibility.Collapsed;
        SummaryPanel.Visibility = Visibility.Collapsed;
        DaysGrid.Columns = 3;
        DaysGrid.Rows = 4;
        DaysGrid.Children.Clear();

        for (var i = 0; i < 12; i++)
        {
            var year = _yearGridBase + i;
            var isCurrent = year == DisplayYear;
            var cell = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1.4),
                BorderBrush = new SolidColorBrush(isCurrent
                    ? Color.FromRgb(0x00, 0x4D, 0x40)
                    : Color.FromRgb(0xD7, 0xE0, 0xEA)),
                Background = isCurrent
                    ? new LinearGradientBrush(Color.FromRgb(0x00, 0x89, 0x7B), Color.FromRgb(0x00, 0x4D, 0x40), 45)
                    : Brushes.White,
                Child = new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(year.ToString()),
                    FontSize = 13.5,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x31, 0x51, 0x7A)),
                    Margin = new Thickness(0, 10, 0, 10),
                },
            };
            cell.MouseLeftButtonUp += (_, _) =>
            {
                DisplayYear = year;
                _viewMode = CalendarViewMode.Months;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
            };
            DaysGrid.Children.Add(cell);
        }
    }

    /// <summary>نمای روزهای ماه — خانه‌های طراحی تازه با نوار ظرفیت طیفی.
    /// هر خانه در try جدا ساخته می‌شود تا خطای یک روز کل ماه را خالی نکند.</summary>
    private void RenderDaysView()
    {
        WeekDaysRow.Visibility = Visibility.Visible;
        LegendPanel.Visibility = Visibility.Visible;
        SummaryPanel.Visibility = Visibility.Visible;
        DaysGrid.Columns = 7;
        DaysGrid.Rows = 6;
        // گارد تاریخ نامعتبر: نمای ماه/سال صفر یا بیرون از بازه یعنی «امروز»
        if (DisplayYear < 1300 || DisplayYear > 1500 || DisplayMonth is < 1 or > 12)
        {
            var (ty, tm, _) = PersianDateHelper.ToJalali(IranClock.Today);
            DisplayYear = ty;
            DisplayMonth = tm;
        }
        MonthTitleText.Text =
            $"{PersianDateHelper.MonthNames[DisplayMonth - 1]} {PersianDateHelper.ToPersianDigits(DisplayYear.ToString())}";
        DaysGrid.Children.Clear();

        var today = IranClock.Today;
        var disabled = DisabledDates?.Select(d => d.Date).ToHashSet() ?? new HashSet<DateTime>();
        var stats = DayStats ?? new Dictionary<DateTime, CalendarDayStat>();
        var marked = MarkedDates ?? new Dictionary<DateTime, int>();

        var firstOfMonth = Persian.ToDateTime(DisplayYear, DisplayMonth, 1, 0, 0, 0, 0);
        var daysInMonth = Persian.GetDaysInMonth(DisplayYear, DisplayMonth);
        var firstCol = ((int)firstOfMonth.DayOfWeek + 1) % 7;

        for (var i = 0; i < firstCol; i++)
            DaysGrid.Children.Add(new Border());

        var monthBooked = 0; var monthTotal = 0; var monthWorkDays = 0;
        for (var day = 1; day <= daysInMonth; day++)
        {
            try
            {
                var cell = BuildDayCell(day, today, disabled, stats, marked,
                    ref monthBooked, ref monthTotal, ref monthWorkDays);
                if (cell is not null) DaysGrid.Children.Add(cell);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calendar day cell {day} failed: {ex.Message}");
                // خانهٔ جایگزین حداقلی — عدد روز در هر شرایطی دیده شود و
                // کلیک انتخاب روز هم کار کند تا هیچ روزی قفل نماند
                var fallbackDate = Persian.ToDateTime(DisplayYear, DisplayMonth, day, 0, 0, 0, 0);
                var fallbackCell = new Border
                {
                    CornerRadius = new CornerRadius(13),
                    Margin = new Thickness(2.5),
                    MinHeight = 50,
                    Background = Brushes.White,
                    Child = new TextBlock
                    {
                        Text = PersianDateHelper.ToPersianDigits(day.ToString()),
                        FontSize = 13.5,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 0),
                    },
                };
                fallbackCell.MouseLeftButtonUp += (_, _) =>
                {
                    SelectedDate = fallbackDate;
                    SelectedShamsi = PersianDateHelper.ToShamsiFa(fallbackDate);
                    DateSelected?.Invoke(this, fallbackDate);
                    Render();
                };
                DaysGrid.Children.Add(fallbackCell);
            }
        }

        MonthBookedText.Text = PersianDateHelper.ToPersianDigits(monthBooked.ToString());
        MonthWorkDaysText.Text = PersianDateHelper.ToPersianDigits(monthWorkDays.ToString());
        MonthFreeText.Text = PersianDateHelper.ToPersianDigits(Math.Max(0, monthTotal - monthBooked).ToString());
    }

    /// <summary>ساخت خانهٔ یک روز — عدد، نوار ظرفیت، برچسب امروز و کلیک انتخاب</summary>
    private Border? BuildDayCell(int day, DateTime today, HashSet<DateTime> disabled,
        IDictionary<DateTime, CalendarDayStat> stats, IDictionary<DateTime, int> marked,
        ref int monthBooked, ref int monthTotal, ref int monthWorkDays)
    {
        var date = Persian.ToDateTime(DisplayYear, DisplayMonth, day, 0, 0, 0, 0);
        var isFriday = date.DayOfWeek == DayOfWeek.Friday;
        var isOff = disabled.Contains(date);
        var hasStat = stats.TryGetValue(date, out var stat);
        var isFull = hasStat && stat.Total > 0 && stat.Booked >= stat.Total;
        if (isFull) isOff = true;
        var outOfRange = (MinDate is { } min && date < min) || (MaxDate is { } max && date > max);
        if (outOfRange) isOff = true;
        var isSelected = SelectedDate?.Date == date;
        var isToday = today == date;
        var isPast = date < today;

        if (hasStat && stat.Total > 0 && !disabled.Contains(date))
        {
            monthBooked += stat.Booked;
            monthTotal += stat.Total;
            monthWorkDays++;
        }

        var number = new TextBlock
        {
            Text = PersianDateHelper.ToPersianDigits(day.ToString()),
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Foreground = isSelected
                ? Brushes.White
                : isOff
                    ? new SolidColorBrush(Color.FromRgb(0x9A, 0xA7, 0xBB))
                    : isFriday
                        ? new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35))
                        : new SolidColorBrush(Color.FromRgb(0x17, 0x2B, 0x4D)),
            Margin = new Thickness(0, 5, 0, 0),
        };
        var content = new StackPanel { Children = { number } };

        if (isOff && !isFull)
        {
            content.Children.Add(new TextBlock
            {
                Text = "تعطیل",
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xB0, 0x39, 0x2E)),
                Margin = new Thickness(0, 3, 0, 4),
            });
        }
        else if (hasStat && stat.Total > 0)
        {
            var ratio = (double)stat.Booked / stat.Total;
            content.Children.Add(BuildCapacityBar(ratio, isSelected));
            content.Children.Add(new TextBlock
            {
                Text = isFull
                    ? "تکمیل"
                    : PersianDateHelper.ToPersianDigits($"{stat.Booked} نوبت"),
                FontSize = 8,
                FontWeight = isFull ? FontWeights.Bold : FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Foreground = isSelected
                    ? Brushes.White
                    : isFull
                        ? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28))
                        : new SolidColorBrush(Color.FromRgb(0x5A, 0x6B, 0x84)),
                Margin = new Thickness(0, 2, 0, 4),
            });
        }
        else if (marked.TryGetValue(date, out var count))
        {
            content.Children.Add(new TextBlock
            {
                Text = PersianDateHelper.ToPersianDigits($"{count} نوبت"),
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Foreground = isSelected
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
                Margin = new Thickness(0, 3, 0, 4),
            });
        }
        else
        {
            content.Children.Add(new Border { Height = 15 });
        }

        var cellGrid = new Grid();
        cellGrid.Children.Add(content);
        if (isToday)
        {
            var badge = new Border
            {
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(isSelected
                    ? Color.FromRgb(0xFF, 0xC4, 0x00)
                    : Color.FromRgb(0x1E, 0x88, 0xE5)),
                Padding = new Thickness(5, 0.5, 5, 0.5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -6, 3, 0),
                Child = new TextBlock
                {
                    Text = "امروز",
                    FontSize = 7.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = isSelected
                        ? new SolidColorBrush(Color.FromRgb(0x4E, 0x34, 0x2E))
                        : Brushes.White,
                },
            };
            cellGrid.Children.Add(badge);
        }

        var cell = new Border
        {
            CornerRadius = new CornerRadius(13),
            Margin = new Thickness(2.5),
            MinHeight = 50,
            BorderThickness = new Thickness(isToday || isSelected ? 1.6 : 1.2),
            BorderBrush = new SolidColorBrush(isSelected
                ? Color.FromRgb(0x00, 0x4D, 0x40)
                : isToday
                    ? Color.FromRgb(0x1E, 0x88, 0xE5)
                    : Colors.Transparent),
            Background = isSelected
                ? new LinearGradientBrush(Color.FromRgb(0x00, 0x89, 0x7B), Color.FromRgb(0x00, 0x4D, 0x40), 45)
                : isOff && !isFull
                    ? HatchBrush()
                    : Brushes.White,
            Opacity = isPast && !isSelected ? 0.45 : 1,
            Cursor = Cursors.Hand,
            ToolTip = isFull
                ? $"{PersianDateHelper.ToShamsiFa(date)} — ظرفیت تکمیل است"
                : isOff
                    ? $"{PersianDateHelper.ToShamsiFa(date)} — تعطیل / عدم حضور"
                    : hasStat
                        ? $"{PersianDateHelper.ToShamsiFa(date)} — {PersianDateHelper.ToPersianDigits(stat.Booked.ToString())} نوبت از {PersianDateHelper.ToPersianDigits(stat.Total.ToString())}"
                        : PersianDateHelper.ToShamsiFa(date),
            Child = cellGrid,
        };
        // همهٔ روزها همیشه قابل انتخاب‌اند — حتی تعطیل یا تکمیل‌ظرفیت:
        // کلیک، روز را انتخاب می‌کند و پنل اسلات واقعیت همان روز را نشان
        // می‌دهد؛ قفل‌کردن روزها به دادهٔ خلاصهٔ ماه وابسته بود و با هر
        // نقص داده‌ای کل تقویم غیرقابل‌کلیک می‌شد (گزارش‌های مکرر مالک).
        cell.MouseLeftButtonUp += (_, _) =>
        {
            SelectedDate = date;
            SelectedShamsi = PersianDateHelper.ToShamsiFa(date);
            DateSelected?.Invoke(this, date);
            Render();
        };
        return cell;
    }

}
