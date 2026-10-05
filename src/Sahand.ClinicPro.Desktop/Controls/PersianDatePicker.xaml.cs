using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// انتخابگر تاریخ شمسی — جعبه متن فقط-خواندنی + تقویم بازشو
/// خروجی دوگانه: تاریخ میلادی (SelectedDate) + متن شمسی (SelectedShamsi)
/// </summary>
public partial class PersianDatePicker : UserControl
{
    /// <summary>سازنده — اتصال رویدادها</summary>
    public PersianDatePicker()
    {
        InitializeComponent();
        CalendarToggle.Checked += (_, _) => CalendarPopup.IsOpen = true;
        CalendarToggle.Unchecked += (_, _) => CalendarPopup.IsOpen = false;
        CalendarPopup.Closed += (_, _) => CalendarToggle.IsChecked = false;
        PopupCalendar.DateSelected += (_, _) => CalendarPopup.IsOpen = false;
        Loaded += (_, _) => SyncShamsi();
    }

    /* ─────────────  خصوصیت‌های وابستگی  ───────────── */

    /// <summary>تاریخ انتخاب‌شده (میلادی)</summary>
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?),
            typeof(PersianDatePicker),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((PersianDatePicker)d).OnSelectedDateChanged()));

    /// <summary>متن شمسی تاریخ — خروجی یک‌طرفه</summary>
    public static readonly DependencyProperty SelectedShamsiProperty =
        DependencyProperty.Register(nameof(SelectedShamsi), typeof(string),
            typeof(PersianDatePicker), new PropertyMetadata(string.Empty));

    /// <summary>راهنمای فیلد</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string),
            typeof(PersianDatePicker), new PropertyMetadata("تاریخ"));

    /// <summary>حد پایین مجاز</summary>
    public static readonly DependencyProperty MinDateProperty =
        DependencyProperty.Register(nameof(MinDate), typeof(DateTime?),
            typeof(PersianDatePicker), new PropertyMetadata(null));

    /// <summary>حد بالای مجاز</summary>
    public static readonly DependencyProperty MaxDateProperty =
        DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?),
            typeof(PersianDatePicker), new PropertyMetadata(null));

    /* ─────────────  رویدادها  ───────────── */

    /// <summary>رویداد تغییر تاریخ انتخاب‌شده</summary>
    public event EventHandler<DateTime?>? DateChanged;

    /* ─────────────  پوشش  ───────────── */

    /// <summary>تاریخ انتخاب‌شده</summary>
    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>متن شمسی</summary>
    public string SelectedShamsi
    {
        get => (string)GetValue(SelectedShamsiProperty);
        private set => SetValue(SelectedShamsiProperty, value);
    }

    /// <summary>راهنمای فیلد</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
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

    /// <summary>باز کردن تقویم با کلیک روی جعبه متن</summary>
    private void OpenPopup(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => CalendarPopup.IsOpen = true;

    /// <summary>هم‌گام‌سازی متن شمسی و جهت ماه نمایشی با تاریخ انتخاب‌شده</summary>
    private void OnSelectedDateChanged()
    {
        SyncShamsi();
        DateChanged?.Invoke(this, SelectedDate);
    }

    /// <summary>به‌روزرسانی متن شمسی و تقویم بازشو</summary>
    private void SyncShamsi()
    {
        if (SelectedDate is { } date)
        {
            SelectedShamsi = PersianDateHelper.ToShamsi(date);
            var (y, m, _) = PersianDateHelper.ToJalali(date);
            PopupCalendar.DisplayYear = y;
            PopupCalendar.DisplayMonth = m;
        }
        else
        {
            SelectedShamsi = string.Empty;
        }
    }
}
