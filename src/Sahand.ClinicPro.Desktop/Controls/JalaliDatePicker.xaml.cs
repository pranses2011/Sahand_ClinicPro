using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// انتخابگر تاریخ شمسی با معادل میلادیِ هم‌زمان — سه نمای روز / ماه / سال
/// </summary>
/// <remarks>
/// <para>
/// ═══ چرخهٔ ۸۱ — بازطراحی کامل (گزارش مالک + نمونهٔ کار jalali-datepicker.html) ═══
/// ظاهر و رفتار از روی نمونهٔ HTML بازسازی شده است:
/// </para>
/// <list type="bullet">
///   <item>فیلد ورودی قابل تایپ با ارقام فارسی/عربی/لاتین و جداکنندهٔ / یا - یا .،
///   همراه با معادل میلادی زیر آن («Oct 23, 2026 (2026-10-23)») و آیکون تقویم برداری.</item>
///   <item>نمای روز: هفته از شنبه و کاملاً راست‌به‌چپ؛ هر خانه «عدد شمسی» + «عدد میلادی»؛
///   فقط روزهای همان ماه (خانه‌های ابتدا/انتها خالی)؛ ستون جمعه قرمز؛
///   انتخاب‌شده پر آبی و امروز فقط قاب آبی؛ سربرگ دوخطی با بازهٔ میلادی و عدد ماه در پرانتز.</item>
///   <item>نمای ماه: ۳ ستون × ۴ سطر — هر سطر یک فصل، با زیرنویس میلادی هر ماه.</item>
///   <item>نمای سال: ۴ ستون × ۳ سطر — بازهٔ ثابت ۱۲ساله
///   (<c>سال − ((سال − ۱۴۰۰) mod 12)</c>) با بازهٔ میلادی هر سال.</item>
/// </list>
/// <para>
/// ═══ سازگاری با بایندینگ‌های موجود (قانون طلایی) ═══
/// مقدار کنترل همان <see cref="SelectedDate"/> میلادی (<c>DateTime?</c>) است و
/// <see cref="DateShamsiText"/>، <see cref="SelectedShamsi"/>، <see cref="Placeholder"/>،
/// <see cref="MinDate"/>، <see cref="MaxDate"/> و رویداد <see cref="DateChanged"/>
/// عیناً مثل کنترل پیشین کار می‌کنند؛ تبدیل شمسی فقط در لایهٔ نمایش انجام می‌شود.
/// (کنترل پیشین <c>PersianDatePicker</c> بازنشسته شد و همهٔ ۱۸ محل استفاده — ۱۰ نما/فرم —
/// به این کنترل مهاجرت کردند؛ نام خصوصیت‌ها و رویدادها عوض نشد.)
/// </para>
/// </remarks>
public partial class JalaliDatePicker : UserControl
{
    /// <summary>نمای فعال تقویم — روز / ماه / سال</summary>
    private enum CalendarView
    {
        /// <summary>شبکهٔ روزهای یک ماه شمسی</summary>
        Days,
        /// <summary>انتخاب ماه (۳×۴ فصلی)</summary>
        Months,
        /// <summary>انتخاب سال (۴×۳ — بازهٔ ۱۲ساله)</summary>
        Years
    }

    /// <summary>نتیجهٔ تحلیل متن تایپ‌شده</summary>
    private enum ParseState
    {
        /// <summary>متن خالی — یعنی «بدون تاریخ»</summary>
        Empty,
        /// <summary>در حال تایپ (هنوز کامل نیست) — بدون بازخورد خطا</summary>
        Incomplete,
        /// <summary>نامعتبر — قاب قرمز و پیام</summary>
        Invalid,
        /// <summary>معتبر</summary>
        Valid
    }

    /// <summary>الگوی متنِ هنوز-در-حال-تایپ (پیش‌وندِ یک تاریخ کامل)</summary>
    private static readonly Regex PartialPattern = new(@"^\d{0,4}([\/\-.٫]\d{0,2}){0,2}$", RegexOptions.Compiled);

    /// <summary>الگوی متنِ کامل تاریخ — سه جزء عددی با جداکنندهٔ / یا - یا .</summary>
    private static readonly Regex FullPattern = new(@"^\s*(\d{1,4})\s*[\/\-.٫]\s*(\d{1,2})\s*[\/\-.٫]\s*(\d{1,2})\s*$",
        RegexOptions.Compiled);

    /// <summary>نمای جاری</summary>
    private CalendarView _view = CalendarView.Days;

    /// <summary>سالِ ماهِ نمایشی در نمای روز</summary>
    private int _viewYear;

    /// <summary>ماهِ نمایشی در نمای روز (۱ تا ۱۲)</summary>
    private int _viewMonth = 1;

    /// <summary>سالِ نمایشی در نمای ماه</summary>
    private int _monthViewYear;

    /// <summary>شروع بازهٔ ۱۲سالهٔ نمای سال</summary>
    private int _decadeStart;

    /// <summary>در حال نوشتن برنامه‌ای متن — رویداد TextChanged نادیده گرفته شود</summary>
    private bool _syncingText;

    /// <summary>هنگام تایپ کاربر: متن جعبه بازنویسی نشود (مکان‌نما نپرد)</summary>
    private bool _suppressBoxSync;

    /// <summary>جلوگیری از بازشدن پاپ‌آپ در اثر بازگشت فوکوس پس از بستن آن</summary>
    private bool _suppressFocusOpen;

    /// <summary>ابزارهای رنگ — یک‌بار در Loaded کش می‌شوند</summary>
    private Brush _accentBrush = Brushes.DodgerBlue;
    private Brush _fridayBrush = Brushes.IndianRed;

    /// <summary>
    /// لحظهٔ فشردن دکمهٔ آیکون تقویم: پاپ‌آپ باز بود؟ — همان الگوی اثبات‌شدهٔ
    /// چرخهٔ ۲۳؛ بدون این گارد، StaysOpen=False پاپ‌آپ را در MouseDown می‌بندد
    /// و Click بلافاصله دوباره بازش می‌کند، پس دکمه هیچ‌وقت «بستن» نمی‌داد
    /// </summary>
    private bool _popupWasOpenOnMouseDown;

    /// <summary>
    /// لحظهٔ آخرین بسته‌شدن پاپ‌آپ — چون StaysOpen=False در MouseDown (پیش از
    /// رویداد Click) پاپ‌آپ را می‌بندد، این مُهر زمانی تنها راه مطمئن تشخیص
    /// «کلیکِ بستن» از «کلیکِ باز کردن» روی آیکون تقویم است
    /// </summary>
    private DateTime _popupClosedAt = DateTime.MinValue;
    /// <summary>
    /// پنجرهٔ میزبان — چرخهٔ ۸۲: برای بستن تقویم با کلیک بیرون از فیلد و
    /// با غیرفعال‌شدن پنجره (Alt+Tab). پاپ‌آپ در پنجرهٔ جدا رندر می‌شود، پس
    /// کلیک بیرون از فیلد هیچ رویدادی در خود کنترل تولید نمی‌کند.
    /// </summary>
    private Window? _hostWindow;

    /// <summary>هندلر ذخیره‌شده — برای RemoveHandler دقیق در Unloaded</summary>
    private MouseButtonEventHandler? _hostMouseDownHandler;

    /// <summary>سازنده — اتصال رویدادها</summary>
    public JalaliDatePicker()
    {
        InitializeComponent();

        var today = JalaliCalendar.ToJalali(DateTime.Today);
        _viewYear = today.Year;
        _viewMonth = today.Month;
        _monthViewYear = today.Year;
        _decadeStart = JalaliCalendar.DecadeStart(today.Year);

        Loaded += OnLoaded;

        // ── باز/بستن پاپ‌آپ ──
        CalendarButton.PreviewMouseDown += (_, _) => _popupWasOpenOnMouseDown = CalendarPopup.IsOpen;
        // ═══ چرخهٔ ۸۲ — ریشهٔ «کلیک روی کنترل فوکوس نمی‌دهد و تایپ نمی‌شود» ═══
        // پیش‌تر همین‌جا پاپ‌آپ باز می‌شد و چون Popup با StaysOpen=False ماوس را
        // capture می‌کرد، MouseDown به TextBox نمی‌رسید: نه مکان‌نما می‌نشست و نه
        // فوکوس صفحه‌کلید به جعبه می‌رفت. اکنون کلیک فقط فوکوس را به جعبه
        // می‌دهد؛ بازشدن تقویم در GotKeyboardFocus (با تأخیر یک تیک) انجام
        // می‌شود تا کلیک کامل شود و کاربر بتواند همان لحظه تایپ کند.
        FieldBorder.PreviewMouseLeftButtonDown += (_, _) =>
        {
            if (!ShamsiBox.IsKeyboardFocusWithin)
                ShamsiBox.Focus();
        };
        CalendarButton.Click += (_, _) => TogglePopup();
        CalendarPopup.Closed += OnPopupClosed;
        // Esc وقتی فوکوس داخل پاپ‌آپ است (تقویم پنجرهٔ جدا دارد و رویداد کلید
        // ممکن است به کنترل نرسد) — مسیر مستقل بستن
        PopupSurface.PreviewKeyDown += OnPopupPreviewKeyDown;

        // ── تایپ دستی: تحلیل زنده، Enter، خروج از فیلد ──
        ShamsiBox.TextChanged += OnTypedTextChanged;
        ShamsiBox.PreviewKeyDown += OnBoxPreviewKeyDown;
        ShamsiBox.LostKeyboardFocus += OnBoxLostFocus;
        ShamsiBox.GotKeyboardFocus += OnBoxGotFocus;

        // ── سربرگ و ناوبری ──
        PrevButton.Click += (_, _) => Shift(-1);
        NextButton.Click += (_, _) => Shift(+1);
        TitleButton.Click += (_, _) => GoUpOneView();

        // Esc در هر جای کنترل: بستن پاپ‌آپ (و لغو ویرایش نامعتبر)
        PreviewKeyDown += OnControlPreviewKeyDown;
    }

    /* ═══════════════ خصوصیت‌های وابستگی (سازگار با کنترل پیشین) ═══════════════ */

    /// <summary>تاریخ انتخاب‌شده — میلادی (منبع حقیقت؛ تبدیل شمسی فقط نمایشی است)</summary>
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(JalaliDatePicker),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, e) => ((JalaliDatePicker)d).OnSelectedDateChanged(e.NewValue as DateTime?)));

    /// <summary>متن شمسی تاریخ انتخاب‌شده با ارقام فارسی — خروجی یک‌طرفه</summary>
    public static readonly DependencyProperty SelectedShamsiProperty =
        DependencyProperty.Register(nameof(SelectedShamsi), typeof(string), typeof(JalaliDatePicker),
            new PropertyMetadata(string.Empty));

    /// <summary>راهنمای فیلد در حالت خالی</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(JalaliDatePicker),
            new PropertyMetadata("تاریخ را تایپ کنید؛ مثلاً ۱۴۰۵/۰۸/۰۱"));

    /// <summary>حد پایین مجاز — روزهای پیش از آن غیرفعال‌اند</summary>
    public static readonly DependencyProperty MinDateProperty =
        DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(JalaliDatePicker),
            new PropertyMetadata(null, (d, _) => ((JalaliDatePicker)d).RenderIfOpen()));

    /// <summary>حد بالای مجاز — روزهای پس از آن غیرفعال‌اند</summary>
    public static readonly DependencyProperty MaxDateProperty =
        DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(JalaliDatePicker),
            new PropertyMetadata(null, (d, _) => ((JalaliDatePicker)d).RenderIfOpen()));

    /// <summary>متن شمسی لاتین‌رقم دوطرفه — «1405/08/01» برای فرم‌هایی که رشته ذخیره می‌کنند</summary>
    public static readonly DependencyProperty DateShamsiTextProperty =
        DependencyProperty.Register(nameof(DateShamsiText), typeof(string), typeof(JalaliDatePicker),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((JalaliDatePicker)d).OnDateShamsiTextChanged()));

    /* ═══════════════ رویدادها ═══════════════ */

    /// <summary>رویداد تغییر تاریخ انتخاب‌شده (سازگار با کنترل پیشین)</summary>
    public event EventHandler<DateTime?>? DateChanged;

    /// <summary>همان رویداد با نام رایج‌تر WPF — برای مصرف‌کننده‌های تازه</summary>
    public event EventHandler<DateTime?>? SelectedDateChanged;

    /* ═══════════════ پوشش خصوصیت‌ها ═══════════════ */

    /// <summary>تاریخ انتخاب‌شده (میلادی)</summary>
    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>متن شمسی با ارقام فارسی</summary>
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

    /// <summary>حد پایین مجاز</summary>
    public DateTime? MinDate
    {
        get => (DateTime?)GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    /// <summary>حد بالای مجاز</summary>
    public DateTime? MaxDate
    {
        get => (DateTime?)GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /// <summary>متن شمسی لاتین‌رقم دوطرفه</summary>
    public string DateShamsiText
    {
        get => (string)GetValue(DateShamsiTextProperty);
        set => SetValue(DateShamsiTextProperty, value);
    }

    /// <summary>آیا پاپ‌آپ تقویم باز است؟ (برای تست/میزبان)</summary>
    public bool IsPopupOpen => CalendarPopup.IsOpen;

    /* ═══════════════ آماده‌سازی ═══════════════ */

    /// <summary>کش رنگ‌ها و همگام‌سازی اولیهٔ نمایش</summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _accentBrush = (Brush)FindResource("Jdp.Accent");
        _fridayBrush = (Brush)FindResource("Jdp.Friday");

        SyncFromSelectedDate(rewriteBox: true);

        // چرخهٔ ۸۲ — بستن تقویم با کلیک بیرون / غیرفعال‌شدن پنجرهٔ میزبان
        HookHostWindow();
    }

    /// <summary>
    /// اتصال به پنجرهٔ میزبان — PreviewMouseDown سراسری پنجره (با
    /// handledEventsToo) تا کلیک روی هر کنترل دیگری تقویم را ببندد، و
    /// Deactivated تا با رفتن به برنامهٔ دیگر هم تقویم باز نماند.
    /// </summary>
    private void HookHostWindow()
    {
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow is null)
            return;

        _hostMouseDownHandler = OnHostPreviewMouseDown;
        _hostWindow.AddHandler(PreviewMouseDownEvent, _hostMouseDownHandler, handledEventsToo: true);
        _hostWindow.Deactivated += OnHostDeactivated;

        Unloaded += (_, _) =>
        {
            if (_hostWindow is null)
                return;

            if (_hostMouseDownHandler is not null)
                _hostWindow.RemoveHandler(PreviewMouseDownEvent, _hostMouseDownHandler);
            _hostWindow.Deactivated -= OnHostDeactivated;
            _hostWindow = null;
            _hostMouseDownHandler = null;
        };
    }

    /// <summary>کلیک بیرونِ فیلد و تقویم ⇒ بستن تقویم (چرخهٔ ۸۲)</summary>
    private void OnHostPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!CalendarPopup.IsOpen)
            return;

        if (e.OriginalSource is not DependencyObject src)
            return;

        if (IsWithin(src, FieldBorder) || IsWithin(src, PopupSurface))
            return;

        ClosePopup();
    }

    /// <summary>غیرفعال‌شدن پنجرهٔ میزبان (Alt+Tab) ⇒ بستن تقویم</summary>
    private void OnHostDeactivated(object? sender, EventArgs e) => ClosePopup();

    /// <summary>آیا گره در زیرشاخهٔ ریشه است؟ (خودِ ریشه هم شامل)</summary>
    private static bool IsWithin(DependencyObject node, DependencyObject root)
    {
        // پیمایش والدهای بصری/منطقی — IsAncestorOf روی DependencyObject در دسترس نیست
        for (DependencyObject? current = node; current is not null;)
        {
            if (ReferenceEquals(current, root)) return true;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                      ?? LogicalTreeHelper.GetParent(current);
        }
        return false;
    }

    /* ═══════════════ باز/بستن پاپ‌آپ ═══════════════ */

    /// <summary>باز/بسته کردن پاپ‌آپ با دکمهٔ آیکون تقویم</summary>
    private void TogglePopup()
    {
        var closedByThisClick = _popupWasOpenOnMouseDown;
        _popupWasOpenOnMouseDown = false;

        // اگر پاپ‌آپ همان لحظهٔ فشردن این دکمه باز بود، سیستم پیش از Click
        // بسته‌اش کرده؛ این کلیک باید «بستن» بماند نه باز کردن دوباره
        if (CalendarPopup.IsOpen)
        {
            ClosePopup();
            return;
        }

        if (closedByThisClick || WasPopupClosedJustNow())
            return;

        OpenPopup();
    }

    /// <summary>آیا پاپ‌آپ همین چند صدم‌ثانیهٔ پیش بسته شد؟ (کلیک بیرون/آیکون)</summary>
    private bool WasPopupClosedJustNow() =>
        _popupClosedAt != DateTime.MinValue
        && DateTime.UtcNow - _popupClosedAt < TimeSpan.FromMilliseconds(250);

    /// <summary>Esc داخل خود پاپ‌آپ — بستن بدون تغییر تاریخ</summary>
    private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        ClosePopup();
        e.Handled = true;
    }

    /// <summary>
    /// باز کردن تقویم با «ماه هوشمند»: اگر تاریخ معتبر داریم روی همان ماه و با
    /// همان روز انتخاب‌شده، وگرنه روی ماه جاری (امروز فقط قاب آبی).
    /// </summary>
    private void OpenPopup()
    {
        var state = ParseTyped(ShamsiBox.Text, out var jy, out var jm, out _, out _);
        if (state == ParseState.Valid)
        {
            _viewYear = jy;
            _viewMonth = jm;
        }
        else if (SelectedDate is { } selected)
        {
            // متن جعبه ناقص/نامعتبر است ولی انتخاب معتبر داریم — روی همان ماه
            var (sy, sm, _) = JalaliCalendar.ToJalali(selected);
            _viewYear = sy;
            _viewMonth = sm;
        }
        else
        {
            var today = JalaliCalendar.ToJalali(DateTime.Today);
            _viewYear = today.Year;
            _viewMonth = today.Month;
        }

        _monthViewYear = _viewYear;
        _decadeStart = JalaliCalendar.DecadeStart(_viewYear);
        _view = CalendarView.Days;
        RenderCurrentView();
        CalendarPopup.IsOpen = true;
    }

    /// <summary>بستن پاپ‌آپ — با گارد بازگشت فوکوس تا دوباره باز نشود</summary>
    private void ClosePopup()
    {
        if (!CalendarPopup.IsOpen)
            return;

        _suppressFocusOpen = true;
        CalendarPopup.IsOpen = false;
        // فوکوس پس از بستن به جعبهٔ متن برمی‌گردد؛ بدون این گارد پاپ‌آپ بلافاصله
        // دوباره باز می‌شد (چرخهٔ ۸۱ — GotKeyboardFocus هم تقویم را باز می‌کند).
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            new Action(() => _suppressFocusOpen = false));
    }

    /// <summary>پس از بستن پاپ‌آپ: متن معتبر به قالب استاندارد نرمال شود</summary>
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _popupClosedAt = DateTime.UtcNow;

        if (ParseTyped(ShamsiBox.Text, out _, out _, out _, out _) == ParseState.Valid)
        {
            NormalizeBoxText();
        }
    }

    /// <summary>فوکوس روی فیلد → باز شدن تقویم (مطابق نمونهٔ HTML)</summary>
    private void OnBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // سه گارد: پرچم بسته‌شدن برنامه‌ای، باز بودن فعلی، و بازگشت فوکوس
        // اندکی پس از بستن (بازگردانی فوکوس ویندوز می‌تواند دیرتر از
        // ApplicationIdle رخ دهد و بی‌این‌گارد تقویم را دوباره باز می‌کرد)
        if (_suppressFocusOpen || CalendarPopup.IsOpen || WasPopupClosedJustNow())
            return;

        // ═══ چرخهٔ ۸۲ ═══ بازکردن در پایان چرخهٔ ورودی: اگر همین لحظه (وسط
        // MouseDown) باز شود، چیدمان فیلد جابه‌جا می‌شود و کلیکِ در جریان،
        // فوکوس/مکان‌نمای جعبه را از دست می‌دهد. با Background، اول کلیک و
        // نشاندن مکان‌نما کامل می‌شود، بعد تقویم باز می‌شود.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (_suppressFocusOpen || CalendarPopup.IsOpen)
                return;

            // فوکوس را در جعبه نگه دار تا تایپ بلافاصله ممکن باشد
            ShamsiBox.Focus();
            OpenPopup();
            ShamsiBox.CaretIndex = ShamsiBox.Text.Length;
        }));
    }

    /* ═══════════════ تایپ دستی ═══════════════ */

    /// <summary>تحلیل زندهٔ متن در حال تایپ — اعتبارسنجی واقعی + پرش زندهٔ تقویم</summary>
    private void OnTypedTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncingText)
            return;

        var state = ParseTyped(ShamsiBox.Text, out var jy, out var jm, out var jd, out var error);

        switch (state)
        {
            case ParseState.Empty:
                ClearError();
                ShowGregorianCaption(null);
                if (SelectedDate is not null)
                {
                    _suppressBoxSync = true;
                    try { SelectedDate = null; }
                    finally { _suppressBoxSync = false; }
                }
                RenderIfOpen();
                return;

            case ParseState.Incomplete:
                // وسطِ تایپ — سکوت (بدون فلش قرمز مزاحم)
                ClearError();
                ShowGregorianCaption(null);
                return;

            case ParseState.Invalid:
                SetError(error);
                return;
        }

        // ── معتبر: تاریخ را همان لحظه انتخاب کن و تقویم را به همان ماه ببر ──
        ClearError();
        var date = JalaliCalendar.ToGregorian(jy, jm, jd);
        ShowGregorianCaption(date);

        _viewYear = jy;
        _viewMonth = jm;
        _monthViewYear = jy;
        _decadeStart = JalaliCalendar.DecadeStart(jy);

        _suppressBoxSync = true; // متنِ در حال تایپ بازنویسی نشود (مکان‌نما نپرد)
        try
        {
            if (SelectedDate?.Date != date)
                SelectedDate = date;
        }
        finally
        {
            _suppressBoxSync = false;
        }

        if (CalendarPopup.IsOpen)
        {
            _view = CalendarView.Days;
            RenderCurrentView(); // پرش زنده به همان ماه/سالِ تایپ‌شده
        }
    }

    /// <summary>Enter = ثبت و بستن؛ Esc = بستن/لغو ویرایش نامعتبر</summary>
    private void OnBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (CalendarPopup.IsOpen)
                ClosePopup();
            else if (ParseTyped(ShamsiBox.Text, out _, out _, out _, out _) is ParseState.Invalid)
                SyncFromSelectedDate(rewriteBox: true); // بازگشت به آخرین تاریخ معتبر

            ClearError();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        // Enter: تاریخ معتبر ثبت و نرمال می‌شود؛ متن خالی = پاک‌کردن تاریخ
        var state = ParseTyped(ShamsiBox.Text, out _, out _, out _, out _);
        if (state == ParseState.Valid)
        {
            NormalizeBoxText();
            ClosePopup();
            e.Handled = true;
        }
        else if (state == ParseState.Empty)
        {
            ClosePopup();
            e.Handled = true;
        }
        else
        {
            var typed = ParseTyped(ShamsiBox.Text, out _, out _, out _, out var error);
            SetError(typed == ParseState.Invalid ? error : "تاریخ کامل نیست — نمونه: ۱۴۰۵/۰۸/۰۱");
            e.Handled = true;
        }
    }

    /// <summary>خروج از فیلد: متن معتبر به قالب استاندارد نرمال می‌شود</summary>
    private void OnBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // اگر فوکوس به داخل خود پاپ‌آپ رفته، فیلد را نرمال نکن (کلیک روی روز/ماه/سال)
        if (IsFocusInsidePopup())
            return;

        if (ParseTyped(ShamsiBox.Text, out _, out _, out _, out _) == ParseState.Valid)
            NormalizeBoxText();

        // ═══ چرخهٔ ۸۲ — «با خروج فوکوس، تقویم بسته نمی‌شد» ═══
        // پیش‌تر StaysOpen=False فقط با کلیکِ ماوس بیرون می‌بست؛ Tab/Shift+Tab یا
        // رفتن فوکوس به کنترل دیگر (بدون کلیک) تقویم را باز می‌گذاشت. اکنون
        // اگر فوکوس جدید نه در جعبه و نه در تقویم باشد، پاپ‌آپ صریحاً بسته
        // می‌شود.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (!CalendarPopup.IsOpen)
                return;

            if (ShamsiBox.IsKeyboardFocusWithin || IsFocusInsidePopup())
                return;

            ClosePopup();
        }));
    }

    /// <summary>آیا فوکوس صفحه‌کلید داخل پاپ‌آپ است؟ (کلیک روی روز/ماه/سال)</summary>
    private bool IsFocusInsidePopup()
    {
        if (!CalendarPopup.IsOpen || Keyboard.FocusedElement is not DependencyObject focused)
            return false;

        return ReferenceEquals(focused, PopupSurface) || PopupSurface.IsAncestorOf(focused);
    }

    /// <summary>
    /// تحلیل متن تایپ‌شده — ارقام فارسی/عربی/لاتین و جداکنندهٔ / یا - یا .؛
    /// اعتبار روز با طول واقعی همان ماه از تقویم و رعایت MinDate/MaxDate.
    /// </summary>
    private ParseState ParseTyped(string? rawText, out int jy, out int jm, out int jd, out string error)
    {
        jy = jm = jd = 0;
        error = string.Empty;

        var text = (rawText ?? string.Empty).Trim();
        if (text.Length == 0)
            return ParseState.Empty;

        var latin = PersianDateHelper.ToLatinDigits(text);
        if (!FullPattern.IsMatch(latin))
        {
            // «در حال تایپ» یا «نامعتبر»؟ پیش‌وندِ یک تاریخ کامل = سکوت بی‌خطا
            if (PartialPattern.IsMatch(latin))
                return ParseState.Incomplete;

            error = "قالب تاریخ نامعتبر است — نمونهٔ درست: ۱۴۰۵/۰۸/۰۱";
            return ParseState.Invalid;
        }

        var m = FullPattern.Match(latin);
        jy = int.Parse(m.Groups[1].Value);
        jm = int.Parse(m.Groups[2].Value);
        jd = int.Parse(m.Groups[3].Value);

        if (jy is < 1200 or > 1500)
        {
            error = $"سال {PersianDateHelper.ToPersianDigits(jy.ToString())} خارج از بازهٔ منطقی (۱۲۰۰ تا ۱۵۰۰) است";
            return ParseState.Invalid;
        }

        if (jm is < 1 or > 12)
        {
            error = $"ماه {PersianDateHelper.ToPersianDigits(jm.ToString())} وجود ندارد — ماه باید بین ۱ تا ۱۲ باشد";
            return ParseState.Invalid;
        }

        var monthLength = JalaliCalendar.GetMonthLength(jy, jm);
        if (jd < 1 || jd > monthLength)
        {
            error = $"روز {PersianDateHelper.ToPersianDigits(jd.ToString())} در " +
                    $"«{JalaliCalendar.MonthNames[jm - 1]} {PersianDateHelper.ToPersianDigits(jy.ToString())}» " +
                    $"وجود ندارد — این ماه {PersianDateHelper.ToPersianDigits(monthLength.ToString())} روز است";
            return ParseState.Invalid;
        }

        var date = JalaliCalendar.ToGregorian(jy, jm, jd);
        if (MinDate is { } min && date < min.Date)
        {
            error = $"تاریخ نباید پیش از {JalaliCalendar.ToShamsiFa(min)} باشد";
            return ParseState.Invalid;
        }

        if (MaxDate is { } max && date > max.Date)
        {
            error = $"تاریخ نباید پس از {JalaliCalendar.ToShamsiFa(max)} باشد";
            return ParseState.Invalid;
        }

        return ParseState.Valid;
    }

    /// <summary>نمایش متن به قالب استاندارد «۱۴۰۵/۰۸/۰۱» با ارقام فارسی</summary>
    private void NormalizeBoxText()
    {
        if (SelectedDate is not { } date)
            return;

        WriteBoxText(JalaliCalendar.ToShamsiFa(date));
    }

    /// <summary>نوشتن برنامه‌ای متن جعبه (بدون شعله‌ور شدن تحلیل و بدون بازنویسی حلقه‌ای)</summary>
    private void WriteBoxText(string text)
    {
        if (string.Equals(ShamsiBox.Text, text, StringComparison.Ordinal))
            return;

        _syncingText = true;
        try
        {
            ShamsiBox.Text = text;
            ShamsiBox.CaretIndex = text.Length;
        }
        finally
        {
            _syncingText = false;
        }
    }

    /* ═══════════════ حالت خطا / معادل میلادی ═══════════════ */

    /// <summary>نمایش خطای ورودی — قاب قرمز + پیام کوچک زیر فیلد</summary>
    private void SetError(string message)
    {
        // قاب قرمز با تریگر استایل (Tag=err) — مقدار محلی نمی‌گذاریم تا
        // تریگر فوکوس/خطا در XAML بتواند اثر کند
        FieldBorder.Tag = "err";
        GregorianCaption.Text = message;
        GregorianCaption.Foreground = _fridayBrush;
        GregorianCaption.Opacity = 1;
        GregorianCaption.FlowDirection = FlowDirection.RightToLeft;
        GregorianCaption.ToolTip = message;
    }

    /// <summary>بازگرداندن ظاهر عادی فیلد</summary>
    private void ClearError()
    {
        FieldBorder.Tag = null;
        GregorianCaption.ClearValue(TextBlock.ForegroundProperty);
        GregorianCaption.Opacity = 0.65;
        GregorianCaption.FlowDirection = FlowDirection.LeftToRight;
    }

    /// <summary>نمایش معادل میلادی زیر فیلد — «Oct 23, 2026  (2026-10-23)»</summary>
    private void ShowGregorianCaption(DateTime? date)
    {
        GregorianCaption.Text = date is { } d ? JalaliCalendar.GregorianCaption(d) : string.Empty;
        // فیلدهای باریک متن را با «…» می‌برند — تولتیپ معادل کامل را نشان می‌دهد
        GregorianCaption.ToolTip = string.IsNullOrEmpty(GregorianCaption.Text) ? null : GregorianCaption.Text;
    }

    /* ═══════════════ هم‌گام‌سازی با مقدار انتخاب‌شده ═══════════════ */

    /// <summary>تغییر مقدار SelectedDate — از بایندینگ/کد یا از تایپ و کلیک</summary>
    private void OnSelectedDateChanged(DateTime? newValue)
    {
        SyncFromSelectedDate(rewriteBox: !_suppressBoxSync);
        DateChanged?.Invoke(this, newValue);
        SelectedDateChanged?.Invoke(this, newValue);
    }

    /// <summary>
    /// هم‌گام‌سازی نمایش با تاریخ انتخاب‌شده: متن جعبه، معادل میلادی، خروجی‌های
    /// متنی و ماهِ نمایشی تقویم.
    /// </summary>
    /// <param name="rewriteBox">متن جعبه بازنویسی شود؟ (هنگام تایپ: نه)</param>
    private void SyncFromSelectedDate(bool rewriteBox)
    {
        if (SelectedDate is { } date)
        {
            var (jy, jm, _) = JalaliCalendar.ToJalali(date);
            _viewYear = jy;
            _viewMonth = jm;
            _monthViewYear = jy;
            _decadeStart = JalaliCalendar.DecadeStart(jy);

            SelectedShamsi = JalaliCalendar.ToShamsiFa(date);
            var latin = JalaliCalendar.ToShamsi(date); // همان منبع تقویم کنترل
            if (!string.Equals(DateShamsiText, latin, StringComparison.Ordinal))
                DateShamsiText = latin;

            if (rewriteBox)
                WriteBoxText(SelectedShamsi);

            ShowGregorianCaption(date);
            ClearError();
        }
        else
        {
            SelectedShamsi = string.Empty;
            if (!string.IsNullOrEmpty(DateShamsiText))
                DateShamsiText = string.Empty;

            if (rewriteBox)
                WriteBoxText(string.Empty);

            ClearError();
            ShowGregorianCaption(null);
        }

        RenderIfOpen();
    }

    /// <summary>ورود متن شمسی از بیرون (بایندینگ DateShamsiText)</summary>
    private void OnDateShamsiTextChanged()
    {
        var parsed = PersianDateHelper.ToGregorian(DateShamsiText);
        if (parsed?.Date == SelectedDate?.Date)
            return;

        // متن تهی/نامعتبر از بیرون (مثلاً پاک‌کردن فرم) = پاک‌شدن انتخاب؛
        // همان رفتار چرخهٔ ۴۸ که باگ «تاریخ انقضای تکراری» را رفع کرد.
        SelectedDate = parsed?.Date;
    }

    /* ═══════════════ رسم نماها ═══════════════ */

    /// <summary>رسم دوباره تنها اگر پاپ‌آپ باز است (بازدهی هنگام بسته‌بودن)</summary>
    private void RenderIfOpen()
    {
        if (CalendarPopup.IsOpen)
            RenderCurrentView();
    }

    /// <summary>رسم نمای جاری (روز/ماه/سال) + سربرگ آن</summary>
    private void RenderCurrentView()
    {
        if (CellsGrid is null)
            return;

        switch (_view)
        {
            case CalendarView.Months:
                RenderMonthsView();
                break;
            case CalendarView.Years:
                RenderYearsView();
                break;
            default:
                RenderDaysView();
                break;
        }
    }

    /// <summary>
    /// نمای روز — هفته از شنبه (راست‌ترین ستون)، هر خانه «شمسی + میلادی»،
    /// ستون جمعه قرمز و خانه‌های ماه قبل/بعد کاملاً خالی.
    /// </summary>
    private void RenderDaysView()
    {
        WeekDaysRow.Visibility = Visibility.Visible;
        CellsGrid.Columns = 7;
        CellsGrid.Rows = 0; // سطر خودکار (۵ یا ۶ ردیف بسته به ماه)
        CellsGrid.Children.Clear();

        TitleMainText.Text =
            $"{JalaliCalendar.MonthNames[_viewMonth - 1]} {PersianDateHelper.ToPersianDigits(_viewYear.ToString())}";
        TitleSubText.Text = JalaliCalendar.MonthRangeLabel(_viewYear, _viewMonth);

        var today = DateTime.Today;
        var monthLength = JalaliCalendar.GetMonthLength(_viewYear, _viewMonth);
        var firstOfMonth = JalaliCalendar.ToGregorian(_viewYear, _viewMonth, 1);
        var lead = JalaliCalendar.WeekColumn(firstOfMonth);

        // خانه‌های خالی ابتدای ماه — هیچ عدد کم‌رنگی از ماه قبل/بعد نمایش داده نمی‌شود
        for (var i = 0; i < lead; i++)
            CellsGrid.Children.Add(new Border { Margin = new Thickness(2) });

        for (var day = 1; day <= monthLength; day++)
        {
            var jy = _viewYear;
            var jm = _viewMonth;
            var jd = day;
            var date = JalaliCalendar.ToGregorian(jy, jm, jd);

            var isSelected = SelectedDate?.Date == date;
            var isToday = today == date;
            var isDisabled = (MinDate is { } min && date < min.Date)
                             || (MaxDate is { } max && date > max.Date);

            var cell = CreateCell(
                mainText: PersianDateHelper.ToPersianDigits(day.ToString()),
                subText: date.Day.ToString(), // معادل میلادیِ همان روز (ارقام لاتین)
                isFriday: JalaliCalendar.IsFriday(date),
                isSelected: isSelected,
                isToday: isToday,
                isDisabled: isDisabled,
                mainFontSize: 15,
                subFontSize: 9.5,
                minHeight: 44,
                onClick: () => SelectDay(jy, jm, jd));

            CellsGrid.Children.Add(cell);
        }
    }

    /// <summary>
    /// نمای ماه — ۳ ستون × ۴ سطر؛ هر سطر یک فصل (بهار/تابستان/پاییز/زمستان)
    /// با زیرنویس بازهٔ میلادی و عدد ماه در پرانتز.
    /// </summary>
    private void RenderMonthsView()
    {
        WeekDaysRow.Visibility = Visibility.Collapsed;
        CellsGrid.Columns = 3;
        CellsGrid.Rows = 4;
        CellsGrid.Children.Clear();

        TitleMainText.Text = PersianDateHelper.ToPersianDigits(_monthViewYear.ToString());
        TitleSubText.Text = JalaliCalendar.YearRangeLabel(_monthViewYear);

        var today = JalaliCalendar.ToJalali(DateTime.Today);

        for (var month = 1; month <= 12; month++)
        {
            var m = month;
            var isSelected = SelectedDate is { } sel
                             && JalaliCalendar.ToJalali(sel).Year == _monthViewYear
                             && JalaliCalendar.ToJalali(sel).Month == m;
            var isToday = today.Year == _monthViewYear && today.Month == m;

            var cell = CreateCell(
                mainText: JalaliCalendar.MonthNames[m - 1],
                subText: JalaliCalendar.MonthGregorianSubtitles[m - 1],
                isFriday: false,
                isSelected: isSelected,
                isToday: isToday,
                isDisabled: false,
                mainFontSize: 14,
                subFontSize: 9.5,
                minHeight: 58,
                onClick: () =>
                {
                    _viewYear = _monthViewYear;
                    _viewMonth = m;
                    _view = CalendarView.Days;
                    RenderCurrentView();
                });

            CellsGrid.Children.Add(cell);
        }
    }

    /// <summary>
    /// نمای سال — ۴ ستون × ۳ سطر (۱۲ سال)؛ بازهٔ ثابت ۱۲ساله
    /// (<c>سال − ((سال − ۱۴۰۰) mod 12)</c>) و زیرنویس بازهٔ میلادی هر سال.
    /// </summary>
    private void RenderYearsView()
    {
        WeekDaysRow.Visibility = Visibility.Collapsed;
        CellsGrid.Columns = 4;
        CellsGrid.Rows = 3;
        CellsGrid.Children.Clear();

        var from = _decadeStart;
        var to = _decadeStart + 11;
        TitleMainText.Text =
            $"{PersianDateHelper.ToPersianDigits(from.ToString())} – {PersianDateHelper.ToPersianDigits(to.ToString())}";
        TitleSubText.Text = JalaliCalendar.DecadeRangeLabel(from);

        var today = JalaliCalendar.ToJalali(DateTime.Today);

        for (var year = from; year <= to; year++)
        {
            var y = year;
            var isSelected = SelectedDate is { } sel && JalaliCalendar.ToJalali(sel).Year == y;
            var isToday = today.Year == y;
            var gregorian = JalaliCalendar.ToGregorian(y, 1, 1).Year;

            var cell = CreateCell(
                mainText: PersianDateHelper.ToPersianDigits(y.ToString()),
                subText: $"{gregorian} – {gregorian + 1}",
                isFriday: false,
                isSelected: isSelected,
                isToday: isToday,
                isDisabled: false,
                mainFontSize: 15,
                subFontSize: 9.5,
                minHeight: 58,
                onClick: () =>
                {
                    _monthViewYear = y;
                    _view = CalendarView.Months;
                    RenderCurrentView();
                });

            CellsGrid.Children.Add(cell);
        }
    }

    /// <summary>
    /// ساخت یک خانهٔ شبکه (روز/ماه/سال) — دو خط: متن اصلی و زیرنویس میلادی.
    /// رنگ‌ها: انتخاب‌شده پر آبی/سفید، امروز فقط قاب آبی، جمعه قرمز، خارج از بازه کم‌رنگ.
    /// </summary>
    private Button CreateCell(
        string mainText,
        string subText,
        bool isFriday,
        bool isSelected,
        bool isToday,
        bool isDisabled,
        double mainFontSize,
        double subFontSize,
        double minHeight,
        Action onClick)
    {
        var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        // رنگ متن: انتخاب‌شده سفید؛ جمعه قرمز؛ بقیه از تم
        Brush? mainForeground = null;
        if (isSelected)
            mainForeground = Brushes.White;
        else if (isFriday)
            mainForeground = _fridayBrush;

        var mainBlock = new TextBlock
        {
            Text = mainText,
            FontSize = mainFontSize,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = Helpers.FontAssets.Vazirmatn
        };
        if (mainForeground is not null)
            mainBlock.Foreground = mainForeground;

        var subBlock = new TextBlock
        {
            Text = subText,
            FontSize = subFontSize,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FlowDirection = FlowDirection.LeftToRight,
            Opacity = isSelected ? 1 : 0.85,
            FontFamily = Helpers.FontAssets.Vazirmatn
        };
        if (isSelected)
            subBlock.Foreground = Brushes.White;
        else if (isFriday)
            subBlock.Foreground = _fridayBrush;

        content.Children.Add(mainBlock);
        content.Children.Add(subBlock);

        var cell = new Button
        {
            Style = (Style)FindResource("Jdp.CellButton"),
            Content = content,
            MinHeight = minHeight,
            IsEnabled = !isDisabled,
            ToolTip = isDisabled ? "خارج از بازهٔ مجاز" : null
        };

        if (isSelected)
        {
            // Tag فقط برای تریگر قالب است — رنگ آبی واقعی را تریگرها می‌گذارند
            cell.Tag = "selected";
        }
        else if (isToday)
        {
            // امروز (بدون انتخاب): فقط قاب آبی
            cell.BorderBrush = _accentBrush;
        }

        if (isDisabled)
            cell.Opacity = 0.35;

        cell.Click += (_, _) => onClick();
        return cell;
    }

    /* ═══════════════ ناوبری و پرش نما ═══════════════ */

    /// <summary>یک گام جلو/عقب در نمای جاری (ماه / سال / بازهٔ ۱۲ساله)</summary>
    private void Shift(int delta)
    {
        switch (_view)
        {
            case CalendarView.Days:
                var month = _viewMonth + delta;
                var year = _viewYear;
                if (month > 12) { month = 1; year++; }
                else if (month < 1) { month = 12; year--; }
                _viewMonth = month;
                // کلمپ: پیمایش بی‌پایان هرگز به سالی نرسد که تقویم مرجع برایش
                // استثنا می‌دهد (پایداری — چرخهٔ ۸۱)
                _viewYear = JalaliCalendar.ClampYear(year);
                break;

            case CalendarView.Months:
                _monthViewYear = JalaliCalendar.ClampYear(_monthViewYear + delta);
                break;

            default:
                var start = JalaliCalendar.DecadeStart(JalaliCalendar.MinYear);
                var end = JalaliCalendar.DecadeStart(JalaliCalendar.MaxYear);
                _decadeStart = Math.Clamp(_decadeStart + delta * 12, start, end);
                break;
        }

        RenderCurrentView();
    }

    /// <summary>کلیک روی عنوان سربرگ — فقط صعودی: روز → ماه → سال</summary>
    private void GoUpOneView()
    {
        switch (_view)
        {
            case CalendarView.Days:
                _monthViewYear = _viewYear;
                _view = CalendarView.Months;
                break;
            case CalendarView.Months:
                _decadeStart = JalaliCalendar.DecadeStart(_monthViewYear);
                _view = CalendarView.Years;
                break;
            default:
                return; // در نمای سال، عنوان کاری نمی‌کند
        }

        RenderCurrentView();
    }

    /// <summary>انتخاب یک روز از شبکه — ثبت، نرمال‌سازی متن و بستن پاپ‌آپ</summary>
    private void SelectDay(int jy, int jm, int jd)
    {
        SelectedDate = JalaliCalendar.ToGregorian(jy, jm, jd);
        WriteBoxText(JalaliCalendar.ToShamsiFa(SelectedDate.Value));
        ShowGregorianCaption(SelectedDate);
        ClearError();
        ClosePopup();
    }

    /// <summary>Esc سراسری کنترل — بستن پاپ‌آپ</summary>
    private void OnControlPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && CalendarPopup.IsOpen)
        {
            ClosePopup();
            e.Handled = true;
        }
    }
}
