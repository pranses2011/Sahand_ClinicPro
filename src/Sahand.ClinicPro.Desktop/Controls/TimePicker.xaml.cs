using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// انتخابگر ساعت — چرخه بیست‌ویکم (گزارش پانزدهم مالک)
/// به‌جای تایپ دستی «08:30»، با کلیک پاپ‌آپ دوستونی ساعت/دقیقه باز می‌شود
/// (شبیه انتخابگر ساعت ویندوز ۱۰)؛ خروجی متن قالب‌بندی‌شدهٔ HH:mm است تا
/// خطای قالب ساعت هنگام ذخیرهٔ برنامهٔ کاری رخ ندهد.
/// </summary>
public partial class TimePicker : UserControl
{
    /// <summary>سازنده — پر کردن لیست ساعت ۰-۲۳ و دقیقه‌های ۵ در ۵</summary>
    public TimePicker()
    {
        InitializeComponent();

        // ساعت‌های ۰۰ تا ۲۳ با نمایش دو-رقمی
        for (var h = 0; h <= 23; h++)
            HoursList.Items.Add(h.ToString("00"));
        // دقیقه‌های ۰۰/۰۵/۱۰/…/۵۵ — دقت کافی برای شیفت درمانگاه
        for (var m = 0; m <= 59; m += 5)
            MinutesList.Items.Add(m.ToString("00"));
    }

    /* ─────────────  خصوصیت وابستگی  ───────────── */

    /// <summary>
    /// متن ساعت قالب‌بندی‌شده «HH:mm» — اتصال دوطرفه به ViewModel؛
    /// مقدارهای خارج از قالب (خالی یا نامعتبر) پذیرفته و در پاپ‌آپ انتخاب نمی‌شوند.
    /// </summary>
    public static readonly DependencyProperty TimeTextProperty =
        DependencyProperty.Register(nameof(TimeText), typeof(string),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((TimePicker)d).OnTimeTextChanged()));

    /// <summary>متن ساعت — «08:30»</summary>
    public string TimeText
    {
        get => (string)GetValue(TimeTextProperty);
        set => SetValue(TimeTextProperty, value);
    }

    /* ─────────────  منطق  ───────────── */

    /// <summary>ورود مقدار از ViewModel — هم‌گام‌سازی انتخاب لیست‌ها بدون حلقه</summary>
    private void OnTimeTextChanged()
    {
        var (h, m) = Parse(TimeText);
        // -1 یعنی نامعتبر؛ در این حالت هیچ آیتمی انتخاب نمی‌شود
        if (h >= 0 && HoursList.SelectedIndex != h)
            HoursList.SelectedIndex = h;
        if (m >= 0 && MinutesList.SelectedIndex != m / 5)
            MinutesList.SelectedIndex = m / 5;
    }

    /// <summary>تحلیل امن متن ساعت → (ساعت، دقیقه)؛ نامعتبر → (-1,-1)</summary>
    private static (int H, int M) Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (-1, -1);
        var parts = text.Trim().Split(':', '،', '-');
        if (parts.Length != 2)
            return (-1, -1);
        // ارقام فارسی هم پذیرفته شود
        var hh = Money.PersianDigitsToLatin(parts[0].Trim());
        var mm = Money.PersianDigitsToLatin(parts[1].Trim());
        if (!int.TryParse(hh, out var h) || !int.TryParse(mm, out var m))
            return (-1, -1);
        if (h < 0 || h > 23 || m < 0 || m > 59)
            return (-1, -1);
        return (h, m);
    }

    /// <summary>باز/بسته‌شدن پاپ‌آپ با تاگل</summary>
    private void OnToggleChecked(object sender, RoutedEventArgs e) => PickerPopup.IsOpen = true;
    private void OnToggleUnchecked(object sender, RoutedEventArgs e) => PickerPopup.IsOpen = false;

    /// <summary>انتخاب ساعت — نوشتن فوری به TimeText (بدون بستن پاپ‌آپ)</summary>
    private void OnHoursChanged(object sender, SelectionChangedEventArgs e)
        => SyncFromLists();

    /// <summary>انتخاب دقیقه — نوشتن فوری به TimeText</summary>
    private void OnMinutesChanged(object sender, SelectionChangedEventArgs e)
        => SyncFromLists();

    /// <summary>ساخت متن HH:mm از انتخاب جاری لیست‌ها</summary>
    private void SyncFromLists()
    {
        var h = HoursList.SelectedIndex;
        var m5 = MinutesList.SelectedIndex;
        if (h < 0 || m5 < 0)
            return; // یکی از دوها هنوز انتخاب نشده — مقدار قبلی حفظ شود
        // جلوگیری از بازنویسی بی‌مورد (همان مقدار)
        var text = $"{h:00}:{m5 * 5:00}";
        if (!string.Equals(TimeText, text, StringComparison.Ordinal))
            TimeText = text;
    }

    /// <summary>دکمهٔ تأیید — بستن پاپ‌آپ (مقدار هنگام انتخاب نوشته شده است)</summary>
    private void OnAcceptClick(object sender, RoutedEventArgs e)
        => PickerPopup.IsOpen = false;

    /// <summary>دکمهٔ پاک‌کردن — تهی‌سازی ساعت (مثلاً حذف استراحت اختیاری)</summary>
    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        HoursList.SelectedIndex = -1;
        MinutesList.SelectedIndex = -1;
        TimeText = string.Empty;
        PickerPopup.IsOpen = false;
    }
}
