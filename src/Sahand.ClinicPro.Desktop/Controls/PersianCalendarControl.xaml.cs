using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// کنترل تقویم شمسی — مشخصات پرامپت UI بخش ۲:
/// نمایش ماهانه با دکمه‌های روز؛ شنبه تا جمعه؛ جمعه قرمز؛ امروز متمایز؛
/// MarkedDates برای نشانگر تعداد نوبت و DisabledDates برای تعطیلات و غیبت‌ها.
/// ═══ چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
/// «در کنترل انتخابگر تاریخ بتوان ماه و سال را هم انتخاب کرد همانند کنترل
/// انتخاب تاریخ ویندوز ۱۰» — سه نمای سلسله‌مراتبی:
///   نمای روزها —کلیک روی عنوان ماه→ نمای انتخاب ماه (۱۲ ماه)
///   —کلیک روی عنوان سال→ نمای انتخاب سال (گرید ۱۲ ساله با ناوبری دهه)
/// فلش‌های چپ/راست در نمای روز ماه را، در نمای ماه سال را و در نمای سال دهه را جابه‌جا می‌کنند.
/// </summary>
public partial class PersianCalendarControl : UserControl
{
    /// <summary>تقویم فارسی دات‌نت — برای طول دقیق ماه‌های شمسی</summary>
    private static readonly System.Globalization.PersianCalendar Persian = new();

    /// <summary>نمای فعلی تقویم — روزها / انتخاب ماه / انتخاب سال (چرخه بیست‌ویکم)</summary>
    private enum CalendarViewMode
    {
        /// <summary>شبکهٔ روزهای ماه — نمای عادی</summary>
        Days,
        /// <summary>انتخاب ماه از میان ۱۲ ماه سال</summary>
        Months,
        /// <summary>انتخاب سال از گرید ۱۲ ساله</summary>
        Years,
    }

    /// <summary>نمای فعلی — پیش‌فرض روزها</summary>
    private CalendarViewMode _viewMode = CalendarViewMode.Days;

    /// <summary>سال مبنای گرید انتخاب سال (شروع بازهٔ ۱۲ ساله) — چرخه بیست‌ویکم</summary>
    private int _yearGridBase;

    /// <summary>سازنده — اتصال رویدادها و رندر اولیه</summary>
    public PersianCalendarControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
        PrevMonthButton.Click += (_, _) => Navigate(-1);
        NextMonthButton.Click += (_, _) => Navigate(+1);
        // چرخه بیست‌ویکم: عنوان ماه → نمای ماه → نمای سال → بازگشت به روزها
        MonthTitleButton.Click += (_, _) => TitleClicked();
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
            typeof(PersianCalendarControl), new PropertyMetadata(DateTime.Now.Year - 621, OnDisplayChanged));

    /// <summary>خروجی: تاریخ شمسی انتخاب‌شده</summary>
    public static readonly DependencyProperty SelectedShamsiProperty =
        DependencyProperty.Register(nameof(SelectedShamsi), typeof(string),
            typeof(PersianCalendarControl), new PropertyMetadata(string.Empty));

    /// <summary>تاریخ‌های دارای ناپ (کلید میلادی نیم‌روز، مقدار = تعداد)</summary>
    public static readonly DependencyProperty MarkedDatesProperty =
        DependencyProperty.Register(nameof(MarkedDates), typeof(IDictionary<DateTime, int>),
            typeof(PersianCalendarControl), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>تاریخ‌های غیرفعال (تعطیلات و غیبت‌ها)</summary>
    public static readonly DependencyProperty DisabledDatesProperty =
        DependencyProperty.Register(nameof(DisabledDates), typeof(IEnumerable<DateTime>),
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

    /// <summary>بازخورد تغییر ماه یا سال نمایشی</summary>
    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PersianCalendarControl)d).Render();

    /// <summary>
    /// ناوبری فلش‌های چپ/راست بر اساس نمای فعلی — چرخه بیست‌ویکم:
    /// روزها → ماه قبل/بعد؛ ماه‌ها → سال قبل/بعد؛ سال‌ها → دهه قبل/بعد.
    /// </summary>
    private void Navigate(int delta)
    {
        switch (_viewMode)
        {
            case CalendarViewMode.Days:
                ShiftMonth(delta);
                break;
            case CalendarViewMode.Months:
                // جابه‌جایی سال در نمای انتخاب ماه
                DisplayYear += delta;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
                break;
            case CalendarViewMode.Years:
                // جابه‌جایی بازهٔ ۱۲ ساله در نمای انتخاب سال
                _yearGridBase += delta * 12;
                Render();
                break;
        }
    }

    /// <summary>
    /// کلیک روی عنوان — حرکت سلسله‌مراتبی نماها (چرخه بیست‌ویکم):
    /// روزها → ماه‌ها → سال‌ها → روزها.
    /// </summary>
    private void TitleClicked()
    {
        _viewMode = _viewMode switch
        {
            CalendarViewMode.Days => CalendarViewMode.Months,
            CalendarViewMode.Months => CalendarViewMode.Years,
            _ => CalendarViewMode.Days,
        };
        if (_viewMode == CalendarViewMode.Years)
        {
            // مبنای گرید سال: از ۶ سال قبل شروع شود تا سال فعلی وسط گرید باشد
            _yearGridBase = DisplayYear - 6;
        }
        Render();
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

    /// <summary>رندر بر اساس نمای فعلی — روزها/ماه‌ها/سال‌ها (چرخه بیست‌ویکم)</summary>
    private void Render()
    {
        if (DaysGrid is null)
            return;

        // نرمال‌سازی ماه نمایشی
        if (DisplayMonth is < 1 or > 12)
            DisplayMonth = 1;

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

    /// <summary>نمای انتخاب ماه — ۱۲ دکمهٔ ماه با عنوان سال (چرخه بیست‌ویکم)</summary>
    private void RenderMonthsView()
    {
        // عنوان: سال نمایشی — کلیک بعدی → نمای سال
        MonthTitleText.Text = $"سال {PersianDateHelper.ToPersianDigits(DisplayYear.ToString())}";
        // مخفی‌کردن ردیف نام روزها و تغییر شبکه به ۳ ستون × ۴ ردیف
        WeekDaysRow.Visibility = Visibility.Collapsed;
        DaysGrid.Columns = 3;
        DaysGrid.Rows = 4;
        DaysGrid.Children.Clear();

        for (var month = 1; month <= 12; month++)
        {
            var captured = month;
            var isCurrent = month == DisplayMonth;
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = PersianDateHelper.MonthNames[month - 1],
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                },
                Margin = new Thickness(2),
                MinHeight = 38,
                Padding = new Thickness(0),
                Cursor = Cursors.Hand,
                Style = (Style)FindResource("MaterialDesignFlatLightButton"),
            };
            if (isCurrent)
            {
                btn.Style = (Style)FindResource("MaterialDesignRaisedLightButton");
            }
            btn.Click += (_, _) =>
            {
                // انتخاب ماه → بازگشت به نمای روزهای همان ماه
                DisplayMonth = captured;
                _viewMode = CalendarViewMode.Days;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
            };
            DaysGrid.Children.Add(btn);
        }
    }

    /// <summary>نمای انتخاب سال — گرید ۱۲ ساله با ناوبری بازه (چرخه بیست‌ویکم)</summary>
    private void RenderYearsView()
    {
        // عنوان بازهٔ ۱۲ سالهٔ فعلی
        var fromYear = _yearGridBase;
        var toYear = _yearGridBase + 11;
        MonthTitleText.Text =
            $"{PersianDateHelper.ToPersianDigits(fromYear.ToString())} — {PersianDateHelper.ToPersianDigits(toYear.ToString())}";
        WeekDaysRow.Visibility = Visibility.Collapsed;
        DaysGrid.Columns = 3;
        DaysGrid.Rows = 4;
        DaysGrid.Children.Clear();

        for (var i = 0; i < 12; i++)
        {
            var year = _yearGridBase + i;
            var isCurrent = year == DisplayYear;
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(year.ToString()),
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12.5,
                    // چرخهٔ ۲۶: فونت هاردکد حذف — سال شمسی با ارقام فارسی باید با
                    // فونت وزیرمتن برنامه رسم شود نه فونت لاتین بدون ارقام فارسی
                    FontFamily = Sahand.ClinicPro.Desktop.Helpers.FontAssets.Vazirmatn,
                },
                Margin = new Thickness(2),
                MinHeight = 38,
                Padding = new Thickness(0),
                Cursor = Cursors.Hand,
                Style = (Style)FindResource("MaterialDesignFlatLightButton"),
            };
            if (isCurrent)
            {
                btn.Style = (Style)FindResource("MaterialDesignRaisedLightButton");
            }
            btn.Click += (_, _) =>
            {
                // انتخاب سال → نمای انتخاب ماه همان سال
                DisplayYear = year;
                _viewMode = CalendarViewMode.Months;
                MonthChanged?.Invoke(this, EventArgs.Empty);
                Render();
            };
            DaysGrid.Children.Add(btn);
        }
    }

    /// <summary>نمای اصلی روزهای ماه — رفتار پیشین، چرخه بیست‌ویکم از Render تفکیک شد</summary>
    private void RenderDaysView()
    {
        // بازگشت چیدمان روزها: نمایش ردیف هفته + شبکهٔ ۷×۶
        WeekDaysRow.Visibility = Visibility.Visible;
        DaysGrid.Columns = 7;
        DaysGrid.Rows = 6;

        // چرخه ۱۹ (گزارش سیزدهم مالک): اعداد تقویم با ارقام فارسی نمایش داده می‌شوند
        MonthTitleText.Text =
            $"{PersianDateHelper.MonthNames[DisplayMonth - 1]} {PersianDateHelper.ToPersianDigits(DisplayYear.ToString())}";
        DaysGrid.Children.Clear();

        var today = DateTime.Today;
        var disabled = DisabledDates?
            .Select(d => d.Date)
            .ToHashSet() ?? new HashSet<DateTime>();
        var marked = MarkedDates ?? new Dictionary<DateTime, int>();

        var firstOfMonth = PersianDateHelper.ToGregorian(DisplayYear, DisplayMonth, 1);
        // طول ماه شمسی — نه میلادی! (۶ ماه اول ۳۱، ۵ ماه بعد ۳۰، اسفند ۲۹ یا ۳۰)
        var daysInMonth = Persian.GetDaysInMonth(DisplayYear, DisplayMonth);

        // جابه‌جایی ستون: شنبه = ۰ … جمعه = ۶
        var firstCol = ((int)firstOfMonth.DayOfWeek + 1) % 7;

        // خانه‌های خالی ابتدای ماه
        for (var i = 0; i < firstCol; i++)
            DaysGrid.Children.Add(new Border());

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = PersianDateHelper.ToGregorian(DisplayYear, DisplayMonth, day);
            var isFriday = date.DayOfWeek == DayOfWeek.Friday;
            var isDisabled = disabled.Contains(date)
                             || (MinDate is { } min && date < min)
                             || (MaxDate is { } max && date > max);
            var isSelected = SelectedDate?.Date == date;
            var isToday = today == date;

            var btn = new Button
            {
                // چرخه ۱۹: روز با ارقام فارسی + عرض/مرکزگیری صریح سلول تا روزهای دو‌رقمی کامل دیده شوند
                Content = new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(day.ToString()),
                    TextAlignment = TextAlignment.Center,
                },
                FontSize = 12,
                Margin = new Thickness(1),
                MinHeight = 30,
                MinWidth = 30,
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = isDisabled ? Cursors.No : Cursors.Hand,
                IsEnabled = !isDisabled,
                Tag = date,
            };

            // سبک‌ها — اولویت: انتخاب > امروز > جمعه > عادی > غیرفعال
            if (isSelected)
            {
                btn.Style = (Style)FindResource("MaterialDesignRaisedLightButton");
            }
            else if (isToday)
            {
                btn.Style = (Style)FindResource("MaterialDesignOutlinedButton");
                btn.BorderThickness = new Thickness(2);
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5));
            }
            else
            {
                btn.Style = (Style)FindResource("MaterialDesignFlatLightButton");
                if (isFriday)
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
            }

            if (isDisabled)
                btn.Opacity = 0.35;

            // نشانگر تعداد نوبت + راهنمای ماوس
            if (marked.TryGetValue(date, out var count))
            {
                btn.ToolTip = $"{PersianDateHelper.ToShamsiFa(date)} — {PersianDateHelper.ToPersianDigits(count.ToString())} نوبت";
                btn.Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = PersianDateHelper.ToPersianDigits(day.ToString()), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center },
                        new Border
                        {
                            CornerRadius = new CornerRadius(5),
                            Background = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)),
                            Padding = new Thickness(4, 0, 4, 0),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 1, 0, 0),
                            Child = new TextBlock
                            {
                                Text = PersianDateHelper.ToPersianDigits(count.ToString()),
                                FontSize = 8,
                                Foreground = Brushes.White,
                            },
                        },
                    },
                };
            }
            else if (!isDisabled)
            {
                btn.ToolTip = PersianDateHelper.ToShamsiFa(date);
            }

            btn.Click += (_, _) =>
            {
                SelectedDate = date;
                SelectedShamsi = PersianDateHelper.ToShamsiFa(date);
                DateSelected?.Invoke(this, date);
                Render();
            };

            DaysGrid.Children.Add(btn);
        }
    }
}
