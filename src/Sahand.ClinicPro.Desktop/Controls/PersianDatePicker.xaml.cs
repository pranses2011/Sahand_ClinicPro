using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// انتخابگر تاریخ شمسی — چرخهٔ ۷۲: ورودِ دوگانه
/// (تایپ دستی در جعبهٔ متن + تقویم بازشو فقط از آیکون تقویم)
/// خروجی چندگانه: تاریخ میلادی (SelectedDate) + متن شمسی (SelectedShamsi)
/// + رشتهٔ شمسی لاتین‌رقم دوطرفه (DateShamsiText) برای فرم‌های رشته‌ای
/// </summary>
public partial class PersianDatePicker : UserControl
{
    /// <summary>
    /// قاب پیش‌فرض جعبهٔ متن (از تم متریال) — برای بازگردانی پس از حالت خطا
    /// در Load اولین بار ذخیره می‌شود
    /// </summary>
    private Brush _defaultBoxBorder = Brushes.Transparent;

    /// <summary>
    /// آیا متنِ تایپ‌شدهٔ فعلی نامعتبر است؟ — برای رنگ‌آمیزی قاب و تولتیپ راهنما
    /// و پاک‌سازی درست هنگام خروج از فیلد
    /// </summary>
    private bool _typedTextInvalid;

    /// <summary>سازنده — اتصال رویدادها</summary>
    public PersianDatePicker()
    {
        InitializeComponent();

        // ── تقویم بازشو: باز/بسته با آیکون تقویم (چرخهٔ ۷۲ — یگانه راه گشودن) ──
        CalendarToggle.Checked += (_, _) => CalendarPopup.IsOpen = true;
        CalendarToggle.Unchecked += (_, _) => CalendarPopup.IsOpen = false;
        CalendarPopup.Closed += (_, _) => CalendarToggle.IsChecked = false;
        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): «با کلیک روی تاریخ‌ها بسته شود» —
        // بستن پاپ‌آپ به محض انتخاب روز از تقویم
        PopupCalendar.DateSelected += (_, _) => CalendarPopup.IsOpen = false;

        // ═══ چرخهٔ ۷۲ — ورود تایپی دستی (گزارش مالک: «بصورت دستی هم بشه نوشت») ═══
        // Enter = اعمال فوری متن تایپ‌شده؛ خروج از فیلد = اعمالِ مؤدبانه با بازخورد
        ShamsiBox.PreviewKeyDown += OnShamsiBoxKeyDown;
        ShamsiBox.LostFocus += (_, _) => ApplyTypedText();
        // با شروع ویرایش تازه، حالت خطای قبلی پاک می‌شود (بازخورد زنده)
        ShamsiBox.TextChanged += (_, _) =>
        {
            if (_typedTextInvalid)
                SetInvalidState(false, null);
        };
        // اگر کاربر حین ویرایشِ متن، از تقویم تاریخی برگزیند، متن در حال ویرایش
        // با نمایش استاندارد هم‌گام می‌شود (مسیر SelectedShamsi ← Binding یک‌طرفه)

        // بستن پاپ‌آپ با Escape — سازگاری صفحه‌کلید
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && CalendarPopup.IsOpen)
            {
                CalendarPopup.IsOpen = false;
                e.Handled = true;
            }
        };

        // ذخیرهٔ قاب تم + هم‌گامی اولیه نمایش
        Loaded += (_, _) =>
        {
            _defaultBoxBorder = ShamsiBox.BorderBrush;
            SyncShamsi();
        };
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

    /// <summary>متن شمسی قابل اتصال دوطرفه — «۱۴۰۴/۰۴/۰۱» (برای فرم‌هایی که رشته شمسی ذخیره می‌کنند)</summary>
    public static readonly DependencyProperty DateShamsiTextProperty =
        DependencyProperty.Register(nameof(DateShamsiText), typeof(string),
            typeof(PersianDatePicker),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((PersianDatePicker)d).OnDateShamsiTextChanged()));

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

    /// <summary>متن شمسی دوطرفه</summary>
    public string DateShamsiText
    {
        get => (string)GetValue(DateShamsiTextProperty);
        set => SetValue(DateShamsiTextProperty, value);
    }

    /* ─────────────  منطق  ───────────── */

    /// <summary>
    /// کلیدهای ویژهٔ جعبهٔ متن — چرخهٔ ۷۲:
    /// Enter متن تایپ‌شده را بلافاصله اعمال می‌کند (بدون صبر تا خروج از فیلد)
    /// تا کاربر سریع «۱۴۰۴/۰۷/۲۰ + Enter» بزند و به فیلد بعدی برود
    /// </summary>
    private void OnShamsiBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        ApplyTypedText();
        e.Handled = true;
    }

    /// <summary>
    /// اعمال متن تایپ‌شدهٔ دستی — چرخهٔ ۷۲. سه پیامد:
    /// متن خالی → پاک‌شدن تاریخ (فرم‌های اختیاری)؛
    /// متن معتبر («۱۴۰۴/۰۷/۲۰» / «1404-07-20» / ارقام فارسی) → تاریخ + نرمال‌سازی
    /// نمایش به فرمت استاندارد فارسی‌رقم؛
    /// متن نامعتبر → قاب نارنجی + تولتیپ راهنما؛ تاریخ قبلی دست‌نخورده می‌ماند
    /// (اعتبارسنجی الزامیِ فرم‌ها با همان تاریخ قبلی تصمیم می‌گیرد، نه با متن خام)
    /// </summary>
    private void ApplyTypedText()
    {
        var text = (ShamsiBox.Text ?? string.Empty).Trim();

        // متن تهی = پاک‌کردن آگاهانهٔ تاریخ
        if (text.Length == 0)
        {
            SetInvalidState(false, null);
            if (SelectedDate is not null)
                SelectedDate = null; // مسیر OnSelectedDateChanged نمایش را هم پاک می‌کند
            return;
        }

        var parsed = PersianDateHelper.ToGregorian(text);
        if (parsed is { } date)
        {
            SetInvalidState(false, null);
            if (date != SelectedDate)
                SelectedDate = date; // مسیر OnSelectedDateChanged → SyncShamsi + DateChanged
            else
                ShamsiBox.Text = PersianDateHelper.ToShamsiFa(date); // نرمال‌سازی نمایشِ همین تاریخ
        }
        else
        {
            // ═══ بازخورد خطای تایپی — بدون پرت‌کردن استثنا و بدون دست‌کاری تاریخ ═══
            SetInvalidState(true,
                "قالب تاریخ نامعتبر است — نمونهٔ درست: ۱۴۰۴/۰۷/۲۰ (یا 1404-07-20). " +
                "برای انتخاب از تقویم روی آیکون تقویم کلیک کنید");
        }
    }

    /// <summary>
    /// رنگ‌آمیزی قاب جعبهٔ متن بر اساس اعتبار/بطلان متن تایپی + تولتیپ راهنما.
    /// قاب تم در نخستین Load ذخیره می‌شود تا بازگردانی دقیق باشد
    /// </summary>
    private void SetInvalidState(bool invalid, string? message)
    {
        _typedTextInvalid = invalid;
        ShamsiBox.BorderBrush = invalid
            ? (TryFindResource("MaterialDesign.Brush.ValidationError") as Brush ?? Brushes.OrangeRed)
            : _defaultBoxBorder;
        // ToolTip سراسری (استایل App.xaml) هنگام hover نمایش داده می‌شود
        ShamsiBox.ToolTip = invalid ? message : null;
    }

    /// <summary>هم‌گام‌سازی متن شمسی و جهت ماه نمایشی با تاریخ انتخاب‌شده</summary>
    private void OnSelectedDateChanged()
    {
        SyncShamsi();
        DateChanged?.Invoke(this, SelectedDate);
    }

    /// <summary>ورود متن شمسی از بیرون — تحلیل و تبدیل به تاریخ</summary>
    private void OnDateShamsiTextChanged()
    {
        // چرخه ۱۹: مقایسه بر پایهٔ تاریخ معادل انجام می‌شود نه رشته —
        // تا نمایش فارسی‌رقم و مقدار لاتینِ ViewModel بدون حلقه با هم هم‌گام بمانند
        var parsed = PersianDateHelper.ToGregorian(DateShamsiText);
        if (parsed == SelectedDate)
            return;

        if (parsed is { } date)
        {
            SelectedDate = date;
            SyncShamsi();
            DateChanged?.Invoke(this, date);
        }
        else
        {
            // ═══ چرخهٔ ۴۸ (گزارش مالک: «در اضافه کردن قلم بعدی، اگر تاریخ انقضا با
            //      قلم قبلی یکی باشد خطای "تاریخ انقضای شمسی الزامی است" می‌دهد») ═══
            // متن تهی/نامعتبر از بیرون آمده (مثلاً ViewModel فرمِ ورود کالا پس از
            // «افزودن به فهرست» فیلد را خالی می‌کند) اما پیش‌تر تاریخِ انتخابیِ قبلی
            // دست‌نخورده باقی می‌ماند. نتیجهٔ زنجیره‌وار:
            //   ۱) نمایش جعبهٔ متن همچنان تاریخ قبلی را نشان می‌داد؛
            //   ۲) انتخاب «همان تاریخ» قبلی از تقویم، چون مقدار خاصیت وابستگی
            //      تکراری است، هیچ رویداد تغییر نمی‌سازد؛
            //   ۳) DateShamsiText هرگز به‌روز نمی‌شد و ViewModel تهی می‌ماند
            //      ⇒ اعتبارسنجی «الزامی» به‌کرات خطا می‌داد.
            // ریشه‌یابی‌شده: با پاک‌شدن متنِ متصل، تاریخ انتخابی و نمایش هم باید
            // هم‌زمان پاک شوند تا انتخاب بعدیِ هر تاریخی (حتی تکراری) رویداد
            // تغییر بسازد و به ViewModel برسد.
            SelectedDate = null;
            // SyncShamsi از مسیر OnSelectedDateChanged صدا زده می‌شود و نمایش را
            // پاک می‌کند؛ رویداد DateChanged با تهی برای آگاهی فرم‌ها منتشر می‌شود
            DateChanged?.Invoke(this, null);
        }
    }

    /// <summary>به‌روزرسانی متن شمسی و تقویم بازشو</summary>
    private void SyncShamsi()
    {
        if (SelectedDate is { } date)
        {
            // نمایش با ارقام فارسی (چرخه ۱۹) — مقدار قابل‌پارس لاتین جداگانه نگه داشته می‌شود
            SelectedShamsi = PersianDateHelper.ToShamsiFa(date);
            if (!string.Equals(DateShamsiText, PersianDateHelper.ToShamsi(date), StringComparison.Ordinal))
                DateShamsiText = PersianDateHelper.ToShamsi(date);
            var (y, m, _) = PersianDateHelper.ToJalali(date);
            PopupCalendar.DisplayYear = y;
            PopupCalendar.DisplayMonth = m;
        }
        else
        {
            SelectedShamsi = string.Empty;
            // ═══ چرخهٔ ۴۸ — هم‌گامی دوطرفهٔ کامل هنگام پاک‌شدن تاریخ ═══
            // اگر تاریخ انتخابی تهی شود (پاک‌کردن فرم، مقداردهی اولیه) متنِ
            // متصلِ DateShamsiText هم باید خالی شود؛ وگرنه ViewModel رشتهٔ کهنه
            // را نگه می‌دارد و اعتبارسنجی «الزامی بودن» با نمایش ناسازگار می‌شود.
            // تنظیم رشتهٔ تهی → بازگشتِ همان‌مسیر parse تهی با گارد «parsed ==
            // SelectedDate» فوراً متوقف می‌شود ⇒ بدون حلقهٔ بی‌نهایت.
            if (!string.IsNullOrEmpty(DateShamsiText))
                DateShamsiText = string.Empty;
        }
    }
}
