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
/// ═══════════════════════════════════════════════════════════════════
/// چرخهٔ ۷۴ (سه گزارش مالک — هر سه یک ریشه):
///   ۱) «با انتخاب تاریخ از تقویم بسته می‌شود ولی تاریخ فیلد بروز نمی‌شود»
///   ۲) «۱۳۶۳/۱۳/۳۶ مینویسم هم قبول می‌کند» (ماه و روز غلط)
/// ریشهٔ مشترک: اتصالِ OneWay متن جعبه، با اولین کاراکترِ تایپی کاربر در WPF
/// «کشته» می‌شود (مقدار محلی جای اتصال را می‌گیرد)؛ پس از آن هر به‌روزرسانیِ
/// برنامه‌ای (انتخاب از تقویم) در فیلتـر منعکس نمی‌شد و متنِ تایپی — معتبر یا
/// نامعتبر — تا ابد می‌ماند. رفع قطعی: همگام‌سازی متن از کد پشت
/// (SyncShamsiBox) که در برابر تایپ کاربر مصون است + اعتبارسنجی کاملِ
/// روز/ماه/سال با پیام فارسی دقیق + بازگرداندن متن نامعتبر به آخرین تاریخ
/// معتبر هنگام خروج از فیلد (هیچ تاریخ غلطی هرگز «پذیرفته» نمی‌شود).
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

    /// <summary>
    /// پرچم همگام‌سازی برنامه‌ای متن — وقتی کد خودمان ShamsiBox.Text را
    /// می‌نویسد نباید رویداد TextChanged حالت خطا را دست‌کاری کند
    /// </summary>
    private bool _suppressTextEvents;

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
        ShamsiBox.LostFocus += (_, _) => ApplyTypedText(revertInvalid: true);
        // با شروع ویرایش تازه، حالت خطای قبلی پاک می‌شود (بازخورد زنده)
        ShamsiBox.TextChanged += (_, _) =>
        {
            if (_suppressTextEvents) return;
            if (_typedTextInvalid)
                SetInvalidState(false, null);
        };

        // ═══ چرخهٔ ۷۴ — اعتبارسنجی زنده حین تایپ ═══
        // به‌محض کامل‌شدن قالب (طول ≥ ۸)، صحت تاریخ بررسی می‌شود تا کاربر
        // همان لحظه بازخورد بگیرد، نه بعد از Enter یا خروج از فیلد
        ShamsiBox.TextChanged += (_, _) =>
        {
            if (_suppressTextEvents) return;
            var text = (ShamsiBox.Text ?? string.Empty).Trim();
            if (text.Length is >= 8 and <= 10 && TryParseTyped(text, out _, out var error))
                SetInvalidState(false, null);
            else if (text.Length >= 8)
                SetInvalidState(true, error);
        };

        // بستن پاپ‌آپ با Escape — سازگاری صفحه‌کلید
        // (چرخهٔ ۷۴: هندلر ESC پنجره حالا اولویت را به همین کنترل می‌دهد)
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && CalendarPopup.IsOpen)
            {
                CalendarPopup.IsOpen = false;
                e.Handled = true;
            }
        };

        // ذخیرهٔ قاب تم + همگامی اولیه نمایش
        Loaded += (_, _) =>
        {
            _defaultBoxBorder = ShamsiBox.BorderBrush;
            SyncShamsiBox();
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
        if (e.Key == Key.Escape)
        {
            // چرخهٔ ۷۴: ESC داخل جعبهٔ متنِ در حال ویرایش، متن را به تاریخ
            // معتبر فعلی برمی‌گرداند (لغو ویرایش) — رفتار طبیعی ویرایشگرها
            if (_typedTextInvalid)
            {
                SyncShamsiBox();
                SetInvalidState(false, null);
                e.Handled = true;
            }
            return;
        }
        if (e.Key != Key.Enter)
            return;
        ApplyTypedText(revertInvalid: false);
        e.Handled = true;
    }

    /// <summary>
    /// تحلیل کامل متن تایپ‌شده — چرخهٔ ۷۴. برخلاف گذشته که فقط ToGregorian
    /// صدا زده می‌شد و خطای «روز ناموجود» به‌صورت عمومی رد می‌شد، اینجا
    /// اجزا جداگانه بررسی می‌شوند تا پیام خطا دقیق و راهنما باشد:
    /// ماه > ۱۲؟ روز > طول همان ماه؟ سال بی‌معنا؟
    /// </summary>
    /// <param name="text">متن تایپ‌شده</param>
    /// <param name="parsed">تاریخ میلادی معتبر (در صورت موفقیت)</param>
    /// <param name="error">پیام خطای فارسی دقیق (در صورت نامعتبری)</param>
    private static bool TryParseTyped(string text, out DateTime? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        var parts = text.Trim().Replace('-', '/').Replace('.', '/').Split('/');
        if (parts.Length != 3)
        {
            error = "قالب تاریخ نامعتبر است — نمونهٔ درست: ۱۴۰۴/۰۷/۲۰";
            return false;
        }

        if (!int.TryParse(PersianDateHelper.ToLatinDigits(parts[0].Trim()), out var y) ||
            !int.TryParse(PersianDateHelper.ToLatinDigits(parts[1].Trim()), out var m) ||
            !int.TryParse(PersianDateHelper.ToLatinDigits(parts[2].Trim()), out var d))
        {
            error = "اجزای تاریخ باید عدد باشند — نمونهٔ درست: ۱۴۰۴/۰۷/۲۰";
            return false;
        }

        // سال — بازهٔ منطقی پرونده‌های درمانگاه (تولد ۱۲۰۰ تا آیندهٔ نزدیک)
        if (y is < 1200 or > 1500)
        {
            error = $"سال {PersianDateHelper.ToPersianDigits(y.ToString())} خارج از بازهٔ منطقی (۱۲۰۰ تا ۱۵۰۰) است";
            return false;
        }

        // ماه — باید بین ۱ تا ۱۲ باشد (مثلاً ۱۳ که «ماه» ندارد)
        if (m is < 1 or > 12)
        {
            error = $"ماه {PersianDateHelper.ToPersianDigits(m.ToString())} وجود ندارد — ماه باید بین ۱ تا ۱۲ باشد";
            return false;
        }

        // روز — با طول واقعی همان ماه (۳۱/۳۰/۲۹/۳۰ کبیسه) — چرخهٔ ۷۴
        var monthLength = PersianDateHelper.GetJalaliMonthLength(y, m);
        if (d is < 1 or > monthLength)
        {
            error = $"روز {PersianDateHelper.ToPersianDigits(d.ToString())} در «{PersianDateHelper.MonthNames[m - 1]} {PersianDateHelper.ToPersianDigits(y.ToString())}» وجود ندارد — این ماه {PersianDateHelper.ToPersianDigits(monthLength.ToString())} روز است";
            return false;
        }

        parsed = PersianDateHelper.ToGregorian(y, m, d);
        return parsed is not null;
    }

    /// <summary>
    /// اعمال متن تایپ‌شدهٔ دستی — چرخهٔ ۷۲ + بازگردانی چرخهٔ ۷۴. سه پیامد:
    /// متن خالی → پاک‌شدن تاریخ (فرم‌های اختیاری)؛
    /// متن معتبر («۱۴۰۴/۰۷/۲۰» / «1404-07-20» / ارقام فارسی) → تاریخ + نرمال‌سازی
    /// نمایش به فرمت استاندارد فارسی‌رقم؛
    /// متن نامعتبر → قاب نارنجی + تولتیپ راهنما؛ تاریخ قبلی دست‌نخورده می‌ماند.
    /// چرخهٔ ۷۴ — <paramref name="revertInvalid"/>: هنگام «خروج از فیلد»، متنِ
    /// نامعتبر به آخرین تاریخ معتبر برمی‌گردد تا هیچ تاریخ غلطی در فرم نماند
    /// (Enter این‌طور نیست — کاربر شاید در حال اصلاح است).
    /// </summary>
    private void ApplyTypedText(bool revertInvalid)
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

        if (TryParseTyped(text, out var parsed, out var error))
        {
            SetInvalidState(false, null);
            if (parsed is { } date)
            {
                if (date != SelectedDate)
                    SelectedDate = date; // مسیر OnSelectedDateChanged → SyncShamsi + DateChanged
                else
                    SyncShamsiBox(); // نرمال‌سازی نمایشِ همین تاریخ
            }
        }
        else
        {
            // ═══ بازخورد خطای تایپی — بدون پرت‌کردن استثنا و بدون دست‌کاری تاریخ ═══
            SetInvalidState(true, error +
                ". برای انتخاب از تقویم روی آیکون تقویم کلیک کنید");
            if (revertInvalid)
            {
                // چرخهٔ ۷۴: خروج از فیلد با متن نامعتبر → بازگشت به تاریخ معتبرِ
                // فعلی؛ قاب نارنجی می‌ماند تا کاربر بداند ویرایشش پذیرفته نشد
                SyncShamsiBox();
            }
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
        SyncShamsiBox();
        DateChanged?.Invoke(this, SelectedDate);
    }

    /// <summary>
    /// نوشتن مستقیم متن شمسی در جعبهٔ متن — چرخهٔ ۷۴.
    /// جایگزینِ اتصالِ OneWayِ کشته‌شده با تایپ کاربر؛ این مسیر همیشه کار
    /// می‌کند (انتخاب از تقویم، مقداردهی فرم، پاک‌کردن فرم و…).
    /// </summary>
    private void SyncShamsiBox()
    {
        _suppressTextEvents = true;
        try
        {
            ShamsiBox.Text = SelectedShamsi;
        }
        finally
        {
            _suppressTextEvents = false;
        }
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
            DateChanged?.Invoke(this, date);
        }
        else
        {
            // ═══ چرخهٔ ۴۸ (گزارش مالک: «در اضافه کردن قلم بعدی، اگر تاریخ انقضا با
            //      قلم قبلی یکی باشد خطای "تاریخ انقضای شمسی الزامی است" می‌دهد») ═══
            // متن تهی/نامعتبر از بیرون آمده (مثلاً ViewModel فرمِ ورود کالا پس از
            // «افزودن به فهرست» فیلد را خالی می‌کند) اما پیش‌تر تاریخِ انتخابیِ قبلی
            // دست‌نخورده باقی می‌ماند. ریشه‌یابی‌شده: با پاک‌شدن متنِ متصل، تاریخ
            // انتخابی و نمایش هم باید هم‌زمان پاک شوند تا انتخاب بعدیِ هر تاریخی
            // (حتی تکراری) رویداد تغییر بسازد و به ViewModel برسد.
            SelectedDate = null;
            // رویداد DateChanged با تهی برای آگاهی فرم‌ها منتشر می‌شود
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
            if (!string.IsNullOrEmpty(DateShamsiText))
                DateShamsiText = string.Empty;
        }
    }
}
