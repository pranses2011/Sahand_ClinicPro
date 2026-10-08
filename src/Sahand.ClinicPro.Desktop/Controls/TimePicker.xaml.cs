using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// انتخابگر ساعت طرح Material — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «کنترل انتخاب ساعت رو مثل این عکس بکن» — مرجع: Time Picker متریال با
/// ساعت عقربه‌ای + نمایش دیجیتال + صبح/عصر + تأیید/انصراف + ورود تایپی.
/// ─────────────────────────────────────────────────────────────────────
/// صفحهٔ عقربه‌ای از کد پشت رندر می‌شود (Canvas):
///  ● حالت ساعت: ۱۲ خانهٔ ۱ تا ۱۲ — انتخاب، خودکار به حالت دقیقه می‌رود
///  ● حالت دقیقه: ۱۲ خانهٔ ۰۰ تا ۵۵ (گام ۵)
///  ● عقربه + نقطهٔ انتخابِ توپرِ رنگ اصلی با عدد سفید (مطابق تصویر مرجع)
///  ● کلیک روی هر نقطه از صفحه (نه لزوماً روی عدد) → نزدیک‌ترین زاویه
/// قرارداد خروجی مانند قبل: TimeText دوطرفه با قالب «HH:mm» لاتین؛
/// همهٔ نمایش‌های رابط کاربری با ارقام فارسی است.
/// </summary>
public partial class TimePicker : UserControl
{
    /* ───────────── ثابت‌های هندسهٔ صفحهٔ ساعت ───────────── */

    /// <summary>ضلع صفحهٔ ساعت (پیکسل) — باید با Width/Height کانوس XAML هم‌اندازه باشد</summary>
    private const double FaceSize = 250;

    /// <summary>مرکز صفحهٔ ساعت</summary>
    private const double Center = FaceSize / 2;

    /// <summary>شعاع حلقهٔ اعداد از مرکز</summary>
    private const double NumbersRadius = 98;

    /// <summary>قطر دکمهٔ عددی</summary>
    private const double NumberDotSize = 34;

    /// <summary>قطر نقطهٔ انتخاب توپر (کمی بزرگ‌تر برای برجستگی — مطابق مرجع)</summary>
    private const double SelectionDotSize = 40;

    /// <summary>طول عقربه از مرکز تا نقطهٔ انتخاب</summary>
    private const double HandLength = 88;

    /* ───────────── وضعیت داخلی ───────────── */

    /// <summary>حالت جاری صفحه: true = انتخاب ساعت، false = انتخاب دقیقه</summary>
    private bool _isHourMode = true;

    /// <summary>ساعت ۲۴ ساعتهٔ جاری (۰-۲۳)</summary>
    private int _hour24;

    /// <summary>دقیقهٔ جاری (۰-۵۹)</summary>
    private int _minute;

    /// <summary>مقدار هنگام گشودن پاپ‌آپ — برای انصراف (بازگشت به وضعیت پیش از تغییر)</summary>
    private string? _valueAtOpen;

    /// <summary>پرچم جلوگیری از حلقهٔ بازخورد هنگام هم‌گام‌سازی برنامه‌ای</summary>
    private bool _suppressWrite;

    /// <summary>سازنده</summary>
    public TimePicker()
    {
        InitializeComponent();
        // شروع: ساعت عقربه‌ای در حالت ساعت + دیجیتال خوانده از TimeText
        Loaded += (_, _) =>
        {
            SyncFromTimeText();
            RenderClock();
        };
    }

    /* ─────────────  خصوصیت وابستگی  ───────────── */

    /// <summary>
    /// متن ساعت قالب‌بندی‌شدهٔ «HH:mm» لاتین — اتصال دوطرفه به ViewModel
    /// (قرارداد قدیمی حفظ شده تا همهٔ فرم‌های مصرف‌کننده بی‌تغییر کار کنند)
    /// </summary>
    public static readonly DependencyProperty TimeTextProperty =
        DependencyProperty.Register(nameof(TimeText), typeof(string),
            typeof(TimePicker),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((TimePicker)d).OnTimeTextChanged()));

    /// <summary>متن ساعت لاتین — «08:30»</summary>
    public string TimeText
    {
        get => (string)GetValue(TimeTextProperty);
        set => SetValue(TimeTextProperty, value);
    }

    /// <summary>
    /// نمایش فقط-خواندنی با ارقام فارسی — چرخه بیست‌وسوم (قانون «همهٔ اعداد فارسی»):
    /// دکمهٔ تاگل این متن را نشان می‌دهد تا کاربر هرگز رقم لاتین نبیند
    /// </summary>
    private static readonly DependencyPropertyKey TimeTextFaPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(TimeTextFa), typeof(string),
            typeof(TimePicker), new PropertyMetadata(string.Empty));

    /// <summary>متن فارسی برای نمایش — «۰۸:۳۰» یا خط تیره در نبود مقدار</summary>
    public static readonly DependencyProperty TimeTextFaProperty = TimeTextFaPropertyKey.DependencyProperty;

    /// <summary>متن نمایشی فارسی</summary>
    public string TimeTextFa => (string)GetValue(TimeTextFaProperty);

    /// <summary>به‌روزرسانی متن نمایشی فارسی از مقدار لاتین</summary>
    private void RefreshTimeTextFa()
    {
        var text = string.IsNullOrWhiteSpace(TimeText) ? "--:--" : TimeText;
        SetValue(TimeTextFaPropertyKey, PersianDateHelper.ToPersianDigits(text));
    }

    /* ───────────── هم‌گام‌سازی مقدار ───────────── */

    /// <summary>ورود مقدار از ViewModel → وضعیت داخلی + رندر صفحه و دیجیتال</summary>
    private void OnTimeTextChanged()
    {
        RefreshTimeTextFa();
        SyncFromTimeText();
        if (PickerPopup is not null)
            RenderClock();
    }

    /// <summary>خواندن TimeText به وضعیت داخلی — مقدار نامعتبر نادیده گرفته می‌شود</summary>
    private void SyncFromTimeText()
    {
        var (h, m) = Parse(TimeText);
        if (h >= 0 && m >= 0)
        {
            _hour24 = h;
            _minute = m;
        }
        RefreshDigital();
    }

    /// <summary>تحلیل امن متن ساعت → (ساعت، دقیقه)؛ نامعتبر → (-1,-1)؛ ارقام فارسی هم پذیرفته است</summary>
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

    /// <summary>نوشتن وضعیت داخلی به TimeText (لاتین HH:mm) — بدون حلقهٔ بازخورد</summary>
    private void WriteToTimeText()
    {
        if (_suppressWrite)
            return;
        var text = $"{_hour24:00}:{_minute:00}";
        if (!string.Equals(TimeText, text, StringComparison.Ordinal))
        {
            _suppressWrite = true;
            try
            {
                TimeText = text;
            }
            finally
            {
                _suppressWrite = false;
            }
        }
        RefreshTimeTextFa();
    }

    /* ───────────── نمایش دیجیتال ───────────── */

    /// <summary>
    /// به‌روزرسانی خانه‌های دیجیتال (ساعت ۱۲ ساعتهٔ فارسی + دقیقه) و حالت صبح/عصر
    /// و هایلایت خانهٔ فعال — مطابق مرجع: خانهٔ فعال با رنگ اصلی، غیرفعال خاکستری
    /// </summary>
    private void RefreshDigital()
    {
        if (HourText is null)
            return;

        // تبدیل به نمایش ۱۲ ساعتهٔ مرجع متریال (۱۲/۱/…/۱۱ + صبح/عصر)
        var h12 = _hour24 % 12;
        if (h12 == 0) h12 = 12;

        HourText.Text = PersianDateHelper.ToPersianDigits(h12.ToString("00"));
        MinuteText.Text = PersianDateHelper.ToPersianDigits(_minute.ToString("00"));

        // رنگ اصلی تم — برای هایلایت خانهٔ فعال و کلید صبح/عصر
        var primary = TryFindResource("MaterialDesign.Brush.Primary") as SolidColorBrush
                      ?? new SolidColorBrush(Color.FromRgb(0x6E, 0x3F, 0xA6));
        var primaryBrush = (Brush)primary;
        // نسخهٔ ملایمِ روشن رنگ اصلی برای پس‌زمینهٔ هایلایت (کم‌شفاف)
        var pc = primary.Color;
        var soft = new SolidColorBrush(Color.FromArgb(0x40, pc.R, pc.G, pc.B));
        soft.Freeze();

        HighlightBox(HourBox, HourText, _isHourMode, primaryBrush, soft);
        HighlightBox(MinuteBox, MinuteText, !_isHourMode, primaryBrush, soft);

        // کلید صبح/عصر — انتخاب‌شده با پس‌زمینهٔ ملایم رنگ اصلی
        ApplyAmPm(AmBox, _hour24 < 12, primaryBrush, soft);
        ApplyAmPm(PmBox, _hour24 >= 12, primaryBrush, soft);
    }

    /// <summary>هایلایت یک خانهٔ دیجیتال بر اساس فعال/غیرفعال بودن</summary>
    private static void HighlightBox(Button? box, TextBlock? text, bool isActive, Brush primary, Brush? activeBg)
    {
        if (box?.Template?.FindName("Bd", box) is not Border bd) return;
        var idleBg = box.TryFindResource("MaterialDesign.Brush.TextBox.HoverBackground") as Brush;
        bd.Background = isActive ? activeBg ?? Brushes.Transparent : idleBg ?? Brushes.Transparent;
        if (text is not null)
        {
            var fg = box.TryFindResource("MaterialDesign.Brush.Foreground") as Brush;
            text.Foreground = isActive ? primary : fg ?? Brushes.Black;
        }
    }

    /// <summary>رنگ‌آمیزی کلید صبح/عصر — انتخاب‌شده با پس‌زمینهٔ ملایم رنگ اصلی</summary>
    private static void ApplyAmPm(Button? box, bool isSelected, Brush primary, Brush soft)
    {
        if (box is null) return;
        box.Background = isSelected ? soft : Brushes.Transparent;
        var fg = box.TryFindResource("MaterialDesign.Brush.Foreground") as Brush;
        box.Foreground = isSelected ? primary : fg ?? Brushes.Black;
    }

    /* ───────────── رندر صفحهٔ ساعت عقربه‌ای ───────────── */

    /// <summary>
    /// رندر کامل صفحهٔ عقربه‌ای از نو — اعداد دور دایره، عقربه، نقطهٔ انتخاب و
    /// مرکز. در حالت ساعت: ۱-۱۲؛ در حالت دقیقه: ۰۰-۵۵ (گام ۵) مطابق مرجع.
    /// </summary>
    private void RenderClock()
    {
        if (ClockFace is null)
            return;
        ClockFace.Children.Clear();

        var primary = TryFindResource("MaterialDesign.Brush.Primary") as Brush
                      ?? new SolidColorBrush(Color.FromRgb(0x6E, 0x3F, 0xA6));
        var foreground = TryFindResource("MaterialDesign.Brush.Foreground") as Brush ?? Brushes.Black;

        // مقدار انتخاب‌شدهٔ جاری در این حالت
        var selectedValue = _isHourMode ? (_hour24 % 12 == 0 ? 12 : _hour24 % 12) : _minute;

        // ۱۲ خانهٔ عددی — زاویهٔ خانهٔ i از بالای دایره در جهت عقربه‌های ساعت
        for (var i = 0; i < 12; i++)
        {
            var value = _isHourMode ? i + 1 : i * 5;
            var angleDeg = i * 30 - 90; // −۹۰ یعنی شروع از بالا
            var (x, y) = PointOnCircle(angleDeg, NumbersRadius);

            var isSelected = value == selectedValue;
            var dotSize = isSelected ? SelectionDotSize : NumberDotSize;

            // دکمهٔ عددی — توپرِ رنگ اصلی برای انتخاب‌شده (مطابق مرجع: عدد سفید روی دایرهٔ توپر)
            var b = new Button
            {
                Width = dotSize,
                Height = dotSize,
                Content = PersianDateHelper.ToPersianDigits(
                    _isHourMode ? value.ToString() : value.ToString("00")),
                FontSize = isSelected ? 15 : 12.5,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = value,
                Focusable = false,
            };
            b.Click += OnNumberClick;

            // قالب دایره‌ای بدون حاشیه — انتخاب‌شده توپرِ رنگ اصلی با متن سفید
            b.Template = MakeRoundTemplate(isSelected, primary, foreground);
            // رنگ متن: انتخاب‌شده سفید (مطابق مرجع)، بقیه رنگ متن تم
            b.Foreground = isSelected ? Brushes.White : foreground;

            Canvas.SetLeft(b, Center + x - dotSize / 2);
            Canvas.SetTop(b, Center + y - dotSize / 2);
            ClockFace.Children.Add(b);
        }

        // عقربه — خط از مرکز به سمت زاویهٔ مقدار جاری + نقطهٔ مرکز کوچک
        var handAngle = (_isHourMode
                ? selectedValue % 12
                : selectedValue / 5) * 30 - 90;
        var (hx, hy) = PointOnCircle(handAngle, HandLength);
        ClockFace.Children.Add(new Line
        {
            X1 = Center, Y1 = Center,
            X2 = Center + hx, Y2 = Center + hy,
            Stroke = primary,
            StrokeThickness = 2.2,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        });
        ClockFace.Children.Add(new Ellipse
        {
            Width = 7, Height = 7,
            Fill = primary,
        });
        Canvas.SetLeft(ClockFace.Children[^1], Center - 3.5);
        Canvas.SetTop(ClockFace.Children[^1], Center - 3.5);

        RefreshDigital();
    }

    /// <summary>
    /// قالب دایره‌ای دکمهٔ عددی — ساخته‌شده با کارخانهٔ عنصر (بدون XamlReader):
    /// گرید = دایرهٔ پس‌زمینه + ارائه‌دهندهٔ محتوا در مرکز.
    /// انتخاب‌شده: دایرهٔ توپر رنگ اصلی (عدد سفید روی آن — مطابق مرجع).
    /// غیر انتخاب: پس‌زمینهٔ بی‌رنگ، عدد رنگ متن تم.
    /// </summary>
    private static ControlTemplate MakeRoundTemplate(bool isSelected, Brush primary, Brush foreground)
    {
        var template = new ControlTemplate(typeof(Button));

        // گرید ریشه — دایره + متن مرکز‌چین
        var grid = new FrameworkElementFactory(typeof(Grid));

        // دایرهٔ پس‌زمینه
        var ellipse = new FrameworkElementFactory(typeof(Ellipse));
        var fill = isSelected
            ? primary
            : new SolidColorBrush(Color.FromArgb(0x0F, 0x00, 0x00, 0x00));
        if (fill is SolidColorBrush fs && !fs.IsFrozen) fs.Freeze();
        ellipse.SetValue(Shape.FillProperty, fill);
        grid.AppendChild(ellipse);

        // متن عدد در مرکز دایره
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Bold);
        grid.AppendChild(presenter);

        template.VisualTree = grid;
        return template;
    }

    /// <summary>نقطهٔ روی دایره از زاویهٔ درجه (۰ = بالا، مثبت = عقربه‌های ساعت)</summary>
    private static (double X, double Y) PointOnCircle(double angleDeg, double radius)
    {
        var rad = angleDeg * Math.PI / 180.0;
        return (radius * Math.Cos(rad), radius * Math.Sin(rad));
    }
    /* ───────────── رویدادهای صفحهٔ ساعت ───────────── */

    /// <summary>کلیک روی عدد — مقدار می‌گیرد؛ در حالت ساعت، پس از انتخاب خودکار به دقیقه می‌رود</summary>
    private void OnNumberClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int value })
            ApplyValue(value);
    }

    /// <summary>کلیک روی هرنقطهٔ صفحه — نزدیک‌ترین زاویه (رفتار مرجع متریال)</summary>
    private void OnClockFaceMouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(ClockFace);
        // زاویه از مرکز — تبدیل به شاخص ۱۲گانهٔ نزدیک‌ترین خانه
        var dx = pos.X - Center;
        var dy = pos.Y - Center;
        if (dx == 0 && dy == 0)
            return;
        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI; // −۱۸۰..۱۸۰ (۰ = راست)
        var index = (int)Math.Round(angle / 30.0 + 3) % 12; // جابه‌جایی صفر از راست به بالا
        if (index < 0) index += 12;
        var value = _isHourMode ? index + 1 : index * 5;
        ApplyValue(value);
    }

    /// <summary>اعمال مقدار انتخاب‌شده + رفتار خودکار حالت‌ها + نوشتن به TimeText</summary>
    private void ApplyValue(int value)
    {
        if (_isHourMode)
        {
            var isPm = _hour24 >= 12;
            var h12 = value == 12 ? 0 : value; // ۱۲ → ۰ (۱۲ صبح/ظهر)
            _hour24 = h12 + (isPm ? 12 : 0);
            _hour24 %= 24;
        }
        else
        {
            _minute = Math.Clamp(value, 0, 59);
        }

        WriteToTimeText();

        // رفتار مرجع متریال: پس از انتخاب ساعت، خودکار به حالت دقیقه برو؛
        // پس از انتخاب دقیقه، در حالت دقیقه بماند تا کاربر دقیقه را بپذیرد
        if (_isHourMode)
        {
            _isHourMode = false;
            // رندر با تأخیر صفر — صف نما برای انیمیشن نرم (در حد امکان)
        }
        RenderClock();
    }

    /* ───────────── رویدادهای نمایش دیجیتال ───────────── */

    /// <summary>کلیک خانهٔ ساعت → صفحه به حالت ساعت می‌رود</summary>
    private void OnHourBoxClick(object sender, RoutedEventArgs e)
    {
        _isHourMode = true;
        RenderClock();
    }

    /// <summary>کلیک خانهٔ دقیقه → صفحه به حالت دقیقه می‌رود</summary>
    private void OnMinuteBoxClick(object sender, RoutedEventArgs e)
    {
        _isHourMode = false;
        RenderClock();
    }

    /// <summary>کلید صبح — ساعت به بازهٔ ۰ تا ۱۱ منتقل می‌شود (مقدار ۱۲ ساعته حفظ)</summary>
    private void OnAmClick(object sender, RoutedEventArgs e)
    {
        var h12 = _hour24 % 12;
        _hour24 = h12;
        WriteToTimeText();
        RenderClock();
    }

    /// <summary>کلید عصر — ساعت به بازهٔ ۱۲ تا ۲۳ منتقل می‌شود</summary>
    private void OnPmClick(object sender, RoutedEventArgs e)
    {
        var h12 = _hour24 % 12;
        _hour24 = h12 + 12;
        WriteToTimeText();
        RenderClock();
    }

    /* ───────────── ورود تایپی ───────────── */

    /// <summary>کلید صفحه‌کلید — نمایش/پنهان‌سازی پنل ورود تایپی مستقیم</summary>
    private void OnKeyboardToggleClick(object sender, RoutedEventArgs e)
    {
        var show = KeyboardPanel.Visibility != Visibility.Visible;
        KeyboardPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            ManualInput.Text = $"{_hour24:00}:{_minute:00}";
            // تمرکز پس از نمایان‌شدن پنل
            Dispatcher.BeginInvoke(() => { ManualInput.Focus(); ManualInput.SelectAll(); },
                DispatcherPriority.Render);
        }
    }

    /// <summary>پذیرش ورود تایپی با Enter — مقدار معتبر نوشته می‌شود، نامعتبر نادیده</summary>
    private void OnManualKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        var (h, m) = Parse(ManualInput.Text);
        if (h >= 0 && m >= 0)
        {
            _hour24 = h;
            _minute = m;
            WriteToTimeText();
            RenderClock();
        }
        e.Handled = true;
    }

    /* ───────────── باز/بستن پاپ‌آپ و پاورقی ───────────── */

    /// <summary>گشودن پاپ‌آپ — ذخیرهٔ مقدار فعلی برای انصراف + شروع در حالت ساعت</summary>
    private void OnToggleChecked(object sender, RoutedEventArgs e)
    {
        _valueAtOpen = TimeText;
        _isHourMode = true;
        KeyboardPanel.Visibility = Visibility.Collapsed;
        SyncFromTimeText();
        RenderClock();
        PickerPopup.IsOpen = true;
    }

    private void OnToggleUnchecked(object sender, RoutedEventArgs e) => PickerPopup.IsOpen = false;

    /// <summary>تأیید — مقدار در لحظهٔ انتخاب نوشته شده؛ فقط بستن پاپ‌آپ</summary>
    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        _valueAtOpen = TimeText;
        PickerPopup.IsOpen = false;
        Toggle.IsChecked = false;
    }

    /// <summary>انصراف — بازگشت به مقدار هنگام گشودن پاپ‌آپ (مطابق مرجع CANCEL)</summary>
    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (_valueAtOpen is not null)
        {
            _suppressWrite = true;
            try { TimeText = _valueAtOpen; }
            finally { _suppressWrite = false; }
        }
        SyncFromTimeText();
        RefreshTimeTextFa();
        PickerPopup.IsOpen = false;
        Toggle.IsChecked = false;
    }

    /// <summary>پاک‌کردن — تهی‌سازی ساعت (حذف استراحت اختیاری و مانند آن)</summary>
    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _hour24 = 0;
        _minute = 0;
        _suppressWrite = true;
        try { TimeText = string.Empty; }
        finally { _suppressWrite = false; }
        RefreshTimeTextFa();
        PickerPopup.IsOpen = false;
        Toggle.IsChecked = false;
    }
}
