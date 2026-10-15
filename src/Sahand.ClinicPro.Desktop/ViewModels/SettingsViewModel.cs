using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel تنظیمات سیستم — گام U15 طبق فرم ۲۸:
/// سایدبار منو + بخش‌ها: اطلاعات درمانگاه / ماژول‌ها / سامانه‌های دولتی / چاپگر / تم / امنیت
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly IThemeService _theme;

    /// <summary>
    /// پرچم همگام‌سازی مقدارهای اولیه — چرخه بیستم: هنگام مقداردهی اولیهٔ سازنده،
    /// گرداننده‌های تغییر (On*Changed) نباید ذخیرهٔ تکراری به سرور بفرستند
    /// </summary>
    /// <remarks>چرخه بیست‌وسوم: کامنت میانی با «//» قالب XML را می‌شکست (CS1570/CS1587)</remarks>
    private bool _initializing = true;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "تنظیمات";

    /// <summary>بخش فعال سایدبار — ۰ اطلاعات، ۱ ماژول‌ها، ۲ دولتی، ۳ چاپگر، ۴ تم، ۵ امنیت</summary>
    [ObservableProperty]
    private int _sectionIndex;

    /* ─── بخش اطلاعات درمانگاه ─── */

    /// <summary>نام درمانگاه</summary>
    [ObservableProperty]
    private string? _clinicName;

    /// <summary>شماره مجوز</summary>
    [ObservableProperty]
    private string? _licenseNumber;

    /// <summary>کد اقتصادی</summary>
    [ObservableProperty]
    private string? _economicCode;

    /// <summary>شناسه ملی</summary>
    [ObservableProperty]
    private string? _nationalId;

    /// <summary>نام مدیر</summary>
    [ObservableProperty]
    private string? _managerName;

    /// <summary>تلفن</summary>
    [ObservableProperty]
    private string? _phone;

    /// <summary>شهر — چرخهٔ بیست‌وهفتم: از پنل لایسنس همگام می‌شود و نمایش داده می‌شود</summary>
    [ObservableProperty]
    private string? _city;

    /// <summary>ایمیل</summary>
    [ObservableProperty]
    private string? _email;

    /// <summary>آدرس</summary>
    [ObservableProperty]
    private string? _address;

    /* ─── بخش ماژول‌ها ─── */

    /// <summary>ردیف ماژول با وضعیت لایسنس و فعال بودن</summary>
    public ObservableCollection<ModuleStatusRow> Modules { get; } = new();

    /// <summary>ماژول انتخاب‌شده</summary>
    [ObservableProperty]
    private ModuleStatusRow? _selectedModule;

    /* ─── بخش سامانه‌های دولتی ─── */

    /// <summary>ردیف سامانه دولتی</summary>
    public ObservableCollection<GovSystemCard> GovSystems { get; } = new();

    /// <summary>سامانه انتخاب‌شده</summary>
    [ObservableProperty]
    private GovSystemCard? _selectedGovSystem;

    /* ─── بخش چاپگر ─── */

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — «فیلدهای چاپگرها خالی هستند و
    // نمی‌شود چاپگر انتخاب کرد» ═══ فهرست چاپگرهای نصب‌شدهٔ ویندوز از
    // LocalPrintServer خوانده و در ComboBoxهای بخش چاپگر عرضه می‌شود؛
    // «چاپگر نوبت‌دهی» هم برای قبض‌های نوبت اضافه شد (چاپ مستقیم بدون دیالوگ).

    /// <summary>چاپگرهای نصب‌شدهٔ این ویندوز — برای ComboBoxهای بخش چاپگر</summary>
    public ObservableCollection<string> PrinterNames { get; } = new();

    /// <summary>چاپگر پیش‌فرض</summary>
    [ObservableProperty]
    private string? _defaultPrinter;

    /// <summary>چاپگر فاکتور</summary>
    [ObservableProperty]
    private string? _invoicePrinter;

    /// <summary>چاپگر نسخه</summary>
    [ObservableProperty]
    private string? _prescriptionPrinter;

    /// <summary>چاپگر نوبت‌دهی — قبض‌های نوبت با این چاپ می‌شوند (چرخه ۲۴)</summary>
    [ObservableProperty]
    private string? _ticketPrinter;

    /// <summary>چاپ خودکار قبض نوبت پس از ثبت موفق — چرخه ۲۴</summary>
    [ObservableProperty]
    private bool _autoPrintTicket;

    /// <summary>سایز کاغذ فاکتور</summary>
    [ObservableProperty]
    private string? _invoicePaperSize = "A5";

    /// <summary>گزینه‌های سایز کاغذ</summary>
    public List<string> PaperSizes { get; } = new() { "A4", "A5", "حرارنی ۸۰mm", "حرارنی ۵۸mm" };

    /// <summary>نمایش پیش‌نمایش قبل چاپ</summary>
    [ObservableProperty]
    private bool _showPreview = true;

    /// <summary>چاپ خودکار بعد ثبت فاکتور</summary>
    [ObservableProperty]
    private bool _autoPrintInvoice;

    /// <summary>تعداد نسخه چاپ</summary>
    [ObservableProperty]
    private int _printCopies = 1;

    /* ─── بخش تم ─── */

    /// <summary>تم تاریک فعال</summary>
    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>فهرست تم‌های رنگی — چرخه ۱۹ + گوشه/چگالی چرخه ۲۰</summary>
    public ObservableCollection<ThemeOptionRow> ThemeOptions { get; } = new();

    /// <summary>کلید تم رنگی انتخاب‌شده — با تغییر، فوری اعمال و ذخیره می‌شود</summary>
    [ObservableProperty]
    private string _selectedThemeKey = "Blue";

    /// <summary>
    /// فونت انتخابی — چرخه بیستم (گزارش چهاردهم مالک): با تغییر، بی‌درنگ روی
    /// همهٔ پنجره‌ها اعمال و ذخیره می‌شود؛ پیش‌تر ذخیره می‌شد ولی اثری نداشت.
    /// Vazirmatn داخل exe جاسازی شده؛ بقیه فونت‌های استاندارد ویندوز هستند.
    /// </summary>
    [ObservableProperty]
    private string? _selectedFont = "Vazirmatn";

    /// <summary>گزینه‌های فونت — وزیرمتن جاسازی‌شده + فونت‌های فارسی‌سازگار ویندوز</summary>
    /// <remarks>
    /// FontOptions — فهرست فونت‌ها در بخش سفارشی‌سازی تفکیکی (چرخه بیست‌ویکم)
    /// با فونت‌های نصب‌شدهٔ ویندوز ساخته می‌شود؛ تعریف قدیمی سه‌فونتی حذف شد.
    /// چرخه بیست‌وسوم: این توضیح قبلاً کامنت «//» بود و روی عنصر زبانی معتبری
    /// نبود (CS1587) — به remarks همان سند تبدیل شد.
    /// </remarks>

    /// <summary>
    /// سایز فونت پایه — چرخه بیستم: بی‌درنگ اعمال می‌شود (عناصر بدون اندازهٔ صریح
    /// ارث می‌برند — کادرها/جداول/فهرست‌ها) و در اجرای بعدی هم برقرار می‌ماند
    /// </summary>
    [ObservableProperty]
    private double _baseFontSize = 13;

    /// <summary>انیمیشن‌ها فعال</summary>
    [ObservableProperty]
    private bool _animationsEnabled = true;

    /// <summary>
    /// نمایهٔ واحد پول نمایشی — چرخه بیستم (گزارش چهاردهم مالک):
    /// ۰ = ریال (پیش‌فرض)، ۱ = تومان. تعویض فقط نمایشی است؛ محاسبات و
    /// ذخیره‌سازی همه‌جا ریالی می‌مانند (کمکی Money تبدیل نمایشی را انجام می‌دهد).
    /// </summary>
    [ObservableProperty]
    private int _currencyUnitIndex;

    /// <summary>برچسب‌های واحد پول برای ComboBox</summary>
    public List<string> CurrencyUnitOptions { get; } = new() { "ریال", "تومان" };

    /* ─── سفارشی‌سازی تفکیکی عناصر — چرخه بیست‌ویکم (گزارش پانزدهم مالک) ─── */

    /// <summary>
    /// فهرست فونت‌های قابل انتخاب — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «بتوانیم از فونت‌های نصب‌شده روی ویندوز هم برای برنامه انتخاب کنیم» —
    /// وزیرمتن جاسازی‌شدهٔ خود برنامه در صدر + همهٔ فونت‌های نصب‌شدهٔ سیستم.
    /// </summary>
    public List<string> FontOptions { get; } = BuildFontOptions();

    /// <summary>برچسب نمایشی → نام خام فونت برای ThemeService (چرخه ۲۴)</summary>
    private static string FontKey(string? display)
        => display == "وزیرمتن (فونت برنامه)" ? "Vazirmatn" : (display ?? "Vazirmatn");

    /// <summary>نام خام فونت → برچسب نمایشی در فهرست (چرخه ۲۴)</summary>
    private static string FontDisplay(string? key)
        => key == "Vazirmatn" ? "وزیرمتن (فونت برنامه)" : (key ?? "وزیرمتن (فونت برنامه)");

    /// <summary>
    /// ساخت فهرست فونت‌ها — چرخه ۲۴: فونت‌های «درون برنامه» با برچسب ویژه و
    /// متمایز از فونت‌های ویندوز عرضه می‌شوند (گزارش هفدهم مالک: «فونت‌های
    /// درون برنامه از فونت‌های ویندوزی متمایز باشند تا قابل تشخیص باشند»)؛
    /// مقدار ارسالی به ThemeService همان نام خام (Vazirmatn) است — برچسب
    /// نمایشی فقط برای چشم کاربر است.
    /// </summary>
    private static List<string> BuildFontOptions()
    {
        var list = new List<string> { "وزیرمتن (فونت برنامه)" };
        try
        {
            // همهٔ خانواده‌های فونت نصب‌شدهٔ سیستم — Source نام خانواده است
            var systemFonts = System.Windows.Media.Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
            list.AddRange(systemFonts);
        }
        catch
        {
            // در نبود دسترسی به فونت‌های سیستم، فقط وزیرمتن باقی می‌ماند
        }
        return list;
    }

    /// <summary>
    /// گزینه‌های رنگ منوی کناری — صفر یعنی «همگام با تم» (چرخه بیست‌ویکم):
    /// «تغییر رنگ، رنگ منوی کناری را عوض نمی‌کند» + سفارشی‌سازی مستقل.
    /// چرخهٔ بیست‌ونهم (گزارش هجدهم مالک): گزینهٔ آخر «رنگ دلخواه…»
    /// انتخاب‌گر رنگ حرفه‌ای را باز می‌کند — هر رنگی، نه فقط پالت ثابت.
    /// </summary>
    public List<string> SidebarColorOptions { get; } = new()
    {
        "همگام با تم", "آبی تیره رسمی", "سبز تیره", "خاکستری گرافیت",
        "زرشکی تیره", "فیروزه‌ای تیره", "بنفش تیره", "قهوه‌ای گرم", "مشکی مات",
        "🎨 رنگ دلخواه…"
    };

    /// <summary>رنگ‌های متناظر گزینه‌های منو — ایندکس ۰ (همگام) مقدار ندارد</summary>
    public static readonly string?[] SidebarColorHexes =
    {
        null, "#16355E", "#1B3A24", "#37474F", "#5C1A22",
        "#0E4A4A", "#3B1E5C", "#4E342E", "#111111", null
    };

    /// <summary>نمایهٔ رنگ منو — صفر = همگام با تم</summary>
    [ObservableProperty]
    private int _sidebarColorIndex;

    /// <summary>
    /// کد رنگ دلخواه منوی کناری — چرخهٔ بیست‌ونهم: وقتی گزینهٔ «رنگ دلخواه…»
    /// انتخاب شده باشد این مقدار به‌جای پالت ثابت اعمال/ذخیره می‌شود.
    /// </summary>
    [ObservableProperty]
    private string? _sidebarCustomHex;

    /// <summary>ایندکس گزینهٔ «رنگ دلخواه…» منو — آخرین آیتم فهرست</summary>
    private int SidebarCustomIndex => SidebarColorOptions.Count - 1;

    /// <summary>رنگ مؤثر منو — گزینهٔ دلخواه یا پالت ثابت یا تهی (همگام با تم)</summary>
    private string? EffectiveSidebarHex => SidebarColorIndex == SidebarCustomIndex
        ? SidebarCustomHex
        : (SidebarColorIndex > 0 ? SidebarColorHexes[SidebarColorIndex] : null);

    /// <summary>نمونهٔ رنگ مؤثر منو — نمایش زندهٔ کنار کشو (چرخهٔ ۲۹)</summary>
    public string? SidebarSwatchHex => EffectiveSidebarHex;

    /// <summary>
    /// تازه‌سازی نمونهٔ رنگ منو — با هر تغییر گزینه یا رنگ دلخواه
    /// </summary>
    partial void OnSidebarCustomHexChanged(string? value)
        => OnPropertyChanged(nameof(SidebarSwatchHex));

    /// <summary>گزینه‌های چگالی عناصر — صفر یعنی از تم</summary>
    public List<string> DensityOptions { get; } = new()
    {
        "همگام با تم", "استاندارد", "فشرده"
    };

    /// <summary>نمایهٔ چگالی — صفر = از تم</summary>
    [ObservableProperty]
    private int _densityIndex;

    /// <summary>
    /// شعاع گوشهٔ مستقل کارت‌ها — چرخه بیست‌ویکم؛ صفر مقدار معتبرِ «گوشه‌های تیز»
    /// است و برای «همگام با تم» از -1 استفاده می‌شود.
    /// </summary>
    [ObservableProperty]
    private int _customCardRadius = -1;

    /// <summary>گوشه مستقل فعال است؟ (برای چک‌باکس فعال‌سازی)</summary>
    [ObservableProperty]
    private bool _useCustomRadius;

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — شخصی‌سازی حرفه‌ای تم ═══
    // «بتوانیم ظاهر همهٔ عناصر را جداگانه تغییر دهیم و هر چیزی برای تغییر تم
    // هست بگذار تا شخصی‌سازی در حد حرفه‌ای شود» + «تنظیمات شخصی را بتوان
    // ذخیره و بعداً استفاده کرد»: رنگ اصلی سفارشی، حالت آیکون رنگی/یکدست و
    // سیستم پریست (ذخیره/بازیابی/حذف ترکیب دلخواه) افزوده شد.

    /// <summary>ایندکس حالت رنگ آیکون‌ها — ۰ یکدست، ۱ رنگی</summary>
    [ObservableProperty]
    private int _iconColorModeIndex;

    /// <summary>
    /// ایندکس مجموعهٔ آیکون‌های منو — ۰ کلاسیک، ۱ مدرن (خط‌نازک)، ۲ مینیمال
    /// چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک): «تعویض یکجای آیکون‌ها نه فقط رنگ»
    /// </summary>
    [ObservableProperty]
    private int _iconSetIndex;

    /// <summary>مقدارهای مجموعهٔ آیکون — هم‌اندیس کشوی تنظیمات</summary>
    public static readonly string[] IconSetKeys = { "Classic", "Modern", "Minimal" };

    /// <summary>ایندکس رنگ اصلی سفارشی — ۰ یعنی همگام با تم</summary>
    [ObservableProperty]
    private int _customPrimaryIndex;

    /// <summary>گزینه‌های رنگ اصلی سفارشی — [۰] همگام با تم + پالت + دلخواه</summary>
    public List<string> CustomPrimaryOptions { get; } = new()
        { "همگام با تم", "قرمز روشن", "صورتی", "بنفش", "سرمه‌ای", "آبی تیره", "فیروزه‌ای تیره", "سبز تیره", "زیتونی", "کهربایی", "قهوه‌ای", "🎨 رنگ دلخواه…" };

    /// <summary>مقدارهای هگز گزینه‌های رنگ اصلی — هم‌اندیس فهرست بالا</summary>
    public static readonly string?[] CustomPrimaryHexes =
        { null, "#E53935", "#D81B60", "#8E24AA", "#1A237E", "#0D47A1", "#00695C", "#1B5E20", "#827717", "#E65100", "#4E342E", null };

    /// <summary>
    /// کد رنگ دلخواه اصلی — چرخهٔ بیست‌ونهم: وقتی گزینهٔ «رنگ دلخواه…»
    /// انتخاب شده باشد این مقدار به‌جای پالت ثابت اعمال/ذخیره می‌شود.
    /// </summary>
    [ObservableProperty]
    private string? _primaryCustomHex;

    /// <summary>ایندکس گزینهٔ «رنگ دلخواه…» رنگ اصلی — آخرین آیتم فهرست</summary>
    private int PrimaryCustomIndex => CustomPrimaryOptions.Count - 1;

    /// <summary>رنگ مؤثر اصلی — گزینهٔ دلخواه یا پالت ثابت یا تهی (همگام با تم)</summary>
    private string? EffectivePrimaryHex => CustomPrimaryIndex == PrimaryCustomIndex
        ? PrimaryCustomHex
        : (CustomPrimaryIndex > 0 ? CustomPrimaryHexes[CustomPrimaryIndex] : null);

    /// <summary>نمونهٔ رنگ مؤثر اصلی — نمایش زندهٔ کنار کشو (چرخهٔ ۲۹)</summary>
    public string? PrimarySwatchHex => EffectivePrimaryHex;

    /// <summary>تازه‌سازی نمونهٔ رنگ اصلی — با هر تغییر گزینه یا رنگ دلخواه</summary>
    partial void OnPrimaryCustomHexChanged(string? value)
        => OnPropertyChanged(nameof(PrimarySwatchHex));

    /// <summary>نام پریست در حال ذخیره — ورودی کاربر</summary>
    [ObservableProperty]
    private string? _presetName;

    /// <summary>پریست‌های ذخیره‌شدهٔ کاربر — نام‌ها</summary>
    public ObservableCollection<string> ThemePresets { get; } = new();

    /// <summary>پریست انتخاب‌شده برای اعمال/حذف</summary>
    [ObservableProperty]
    private string? _selectedThemePreset;

    /// <summary>تغییر حالت رنگ آیکون‌ها — اعمال بی‌درنگ (چرخه ۲۴)</summary>
    partial void OnIconColorModeIndexChanged(int value)
    {
        if (_initializing) return;
        ApplyAppearanceNow();
    }

    /// <summary>تغییر مجموعهٔ آیکون‌های منو — اعمال بی‌درنگ روی شکل همهٔ آیکون‌ها
    /// (چرخهٔ بیست‌وهفتم — تعویض یکجای مجموعهٔ آیکون)</summary>
    partial void OnIconSetIndexChanged(int value)
    {
        if (_initializing) return;
        ApplyAppearanceNow();
    }

    /// <summary>
    /// تغییر رنگ اصلی سفارشی — اعمال بی‌درنگ روی کل پوسته (چرخه ۲۴).
    /// چرخهٔ بیست‌ونهم: گزینهٔ «🎨 رنگ دلخواه…» انتخاب‌گر حرفه‌ای را باز
    /// می‌کند؛ انصراف به گزینهٔ قبلی برمی‌گردد.
    /// </summary>
    partial void OnCustomPrimaryIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PrimarySwatchHex));
        if (_initializing) return;

        if (value == PrimaryCustomIndex)
        {
            var hex = Views.ColorPickerWindow.Pick(System.Windows.Application.Current?.MainWindow,
                "رنگ اصلی دکمه‌ها و هایلایت‌ها", PrimaryCustomHex ?? "#1565C0");
            if (hex is null)
            {
                _initializing = true;
                CustomPrimaryIndex = _lastPrimaryIndex;
                _initializing = false;
                return;
            }
            PrimaryCustomHex = hex;
        }
        else if (value >= 0 && value < PrimaryCustomIndex)
        {
            _lastPrimaryIndex = value;
        }

        ApplyAppearanceNow();
        // رنگ اصلی پالت متریال هم باید بازاعمال شود (دکمه‌ها/هایلایت‌ها)
        _theme.ApplyTheme(_theme.CurrentThemeKey, _theme.IsDarkTheme);
    }

    /// <summary>آخرین گزینهٔ غیردلخواه رنگ اصلی — برای بازگشت پس از انصراف</summary>
    private int _lastPrimaryIndex;

    /// <summary>
    /// گشودن مجدد انتخاب‌گر رنگ اصلی — دکمهٔ 🎨 کنار کشو؛ رنگ دلخواهِ
    /// ذخیره‌شده مقدار اولیه است.
    /// </summary>
    [RelayCommand]
    private void PickPrimaryColor()
    {
        var hex = Views.ColorPickerWindow.Pick(System.Windows.Application.Current?.MainWindow,
            "رنگ اصلی دکمه‌ها و هایلایت‌ها", PrimaryCustomHex ?? "#1565C0");
        if (hex is null) return;
        PrimaryCustomHex = hex;
        _initializing = true;
        CustomPrimaryIndex = PrimaryCustomIndex;
        _initializing = false;
        ApplyAppearanceNow();
        _theme.ApplyTheme(_theme.CurrentThemeKey, _theme.IsDarkTheme);
        _snackbar.Success($"رنگ اصلی دلخواه ({hex}) اعمال شد");
    }

    /// <summary>
    /// تایمر ضدبمباران ذخیرهٔ سروری — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «فونت و اندازه اعمال نمی‌شود و خطای سرور کد ۴۲۹ می‌دهد» — ریشه: هر حرکت
    /// اسلایدر فوراً ۶ درخواست PUT می‌فرستاد و سرور Rate-Limit (429) می‌کرد.
    /// اکنون اعمال ظاهری محلی و بی‌درنگ است و ذخیرهٔ سروری فقط ۸۰۰ میلی‌ثانیه
    /// پس از آخرین تغییر و یک‌باره انجام می‌شود.
    /// </summary>
    private System.Windows.Threading.DispatcherTimer? _themeSaveDebouncer;

    /* ─── بخش امنیت ─── */

    /// <summary>حداقل طول رمز</summary>
    [ObservableProperty]
    private int _minPasswordLength = 8;

    /// <summary>الزام حروف بزرگ و کوچک</summary>
    [ObservableProperty]
    private bool _requireMixedCase = true;

    /// <summary>الزام عدد</summary>
    [ObservableProperty]
    private bool _requireDigit = true;

    /// <summary>الزام کاراکتر خاص</summary>
    [ObservableProperty]
    private bool _requireSpecialChar = true;

    /// <summary>تعداد تلاش ناموفق تا قفل</summary>
    [ObservableProperty]
    private int _maxFailedAttempts = 5;

    /// <summary>مدت قفل (دقیقه)</summary>
    [ObservableProperty]
    private int _lockoutMinutes = 30;

    /// <summary>مدت انقضای رمز (روز) — صفر یعنی غیرفعال</summary>
    [ObservableProperty]
    private int _passwordExpiryDays = 90;

    /// <summary>مدت نشست (دقیقه)</summary>
    [ObservableProperty]
    private int _sessionTimeoutMinutes = 20;

    /// <summary>ثبت Audit Log</summary>
    [ObservableProperty]
    private bool _auditLogEnabled = true;

    /// <summary>سازنده</summary>
    public SettingsViewModel(IApiClient api, ISnackbarService snackbar, IThemeService theme)
    {
        _api = api;
        _snackbar = snackbar;
        _theme = theme;
        foreach (var th in _theme.Themes)
            ThemeOptions.Add(new ThemeOptionRow { Theme = th });
        SelectedThemeKey = _theme.CurrentThemeKey;

        // چرخه بیستم: مقدار اولیهٔ تنظیمات ظاهری از سرویس تم (اجرای فعلی/ذخیره‌شده)
        IsDarkTheme = _theme.IsDarkTheme;
        SelectedFont = FontDisplay(_theme.Appearance.FontFamilyName);
        BaseFontSize = _theme.Appearance.BaseFontSize;
        AnimationsEnabled = _theme.Appearance.AnimationsEnabled;
        CurrencyUnitIndex = _theme.Appearance.CurrencyUnit == "Toman" ? 1 : 0;

        // چرخه بیست‌ویکم: مقدار اولیهٔ سفارشی‌سازی‌های تفکیکی از تم
        // چرخهٔ بیست‌ونهم: رنگ خارج از پالت ثابت = رنگ دلخواهِ ذخیره‌شده
        var appearance = _theme.Appearance;
        var initSidebarIdx = Array.IndexOf(SidebarColorHexes, appearance.SidebarHex);
        if (initSidebarIdx >= 0) SidebarColorIndex = initSidebarIdx;
        else if (!string.IsNullOrWhiteSpace(appearance.SidebarHex))
        {
            SidebarCustomHex = appearance.SidebarHex;
            SidebarColorIndex = SidebarCustomIndex;
        }
        else SidebarColorIndex = 0;
        _lastSidebarIndex = SidebarColorIndex == SidebarCustomIndex ? 0 : SidebarColorIndex;
        DensityIndex = appearance.DensityOverride switch
        {
            "Compact" => 2,
            "Normal" => 1,
            _ => 0
        };
        UseCustomRadius = appearance.CardRadiusOverride.HasValue;
        CustomCardRadius = appearance.CardRadiusOverride ?? 8;
        // چرخه ۲۴: رنگ اصلی سفارشی + حالت رنگ آیکون‌ها + پریست‌ها
        var initPrimaryIdx = Array.IndexOf(CustomPrimaryHexes, appearance.CustomPrimaryHex);
        if (initPrimaryIdx >= 0) CustomPrimaryIndex = initPrimaryIdx;
        else if (!string.IsNullOrWhiteSpace(appearance.CustomPrimaryHex))
        {
            PrimaryCustomHex = appearance.CustomPrimaryHex;
            CustomPrimaryIndex = PrimaryCustomIndex;
        }
        else CustomPrimaryIndex = 0;
        _lastPrimaryIndex = CustomPrimaryIndex == PrimaryCustomIndex ? 0 : CustomPrimaryIndex;
        IconColorModeIndex = appearance.IsColorfulIcons ? 1 : 0;
        // چرخهٔ بیست‌وهفتم — مجموعهٔ آیکون‌های منو (کلاسیک/مدرن/مینیمال)
        IconSetIndex = Math.Max(0, Array.IndexOf(IconSetKeys, appearance.IconSetName ?? "Classic"));
        LoadThemePresets();

        RefreshThemeSelection();
        _initializing = false; // از این‌جا به بعد تغییر‌ها کاربرمبنا و ذخیره‌پذیرند

        // مقداردهی اولیه بخش‌ها — داده ماژول/دولتی از سرور در انتخاب بخش
        InitializeModules();
        InitializeGovSystems();
        // چرخهٔ سی‌ودوم: اطلاعات درمانگاه پس از تازه‌سازی لایسنس (توضیح متد بالا)
        _ = RefreshThenLoadClinicInfoAsync();
        // چرخه ۲۴: چاپگرهای نصب‌شده + تنظیمات چاپگر ذخیره‌شده
        _ = LoadPrinterSettingsAsync();
    }

    /// <summary>بارگذاری بخش فعال</summary>
    partial void OnSectionIndexChanged(int value)
    {
        if (value == 0) _ = RefreshThenLoadClinicInfoAsync();
        else if (value == 1) _ = RefreshThenLoadModulesAsync();
        else if (value == 2) _ = LoadGovSystemsAsync();
    }

    /// <summary>
    /// تازه‌سازی لایسنس از سرور لایسنس — چرخهٔ سی‌ودوم (گزارش مالک):
    /// «ماژول جدید در پنل لایسنس فعال کردم ولی هنوز غیرفعال نشان می‌دهد» —
    /// قبل از نمایش بخش‌های «اطلاعات درمانگاه» و «ماژول‌ها»، تپ قلب لحظه‌ای
    /// از طریق اندپوینت تازهٔ license/refresh اجرا می‌شود: ماژول‌ها/گنجایش‌ها
    /// از سرور لایسنس تازه و اطلاعات درمانگاه روی دستهٔ ClinicInfo همگام
    /// می‌شود؛ سپس داده‌ها خوانده و نمایش داده می‌شوند. شکست (قطعی شبکه)
    /// بی‌صدا نادیده گرفته می‌شود و مقادیر محلی فعلی نمایش می‌یابند.
    /// </summary>
    private async Task RefreshLicenseFromServerAsync()
    {
        try
        {
            // اندپوینت ناشناس هم کار می‌کند؛ خروجی مهم نیست — همگام‌سازی سمت سرور
            // روی دسته‌های ClinicInfo و لایسنس محلی انجام می‌شود و بارگذاری
            // بعدی همین صفحه مقادیر تازه را می‌خواند
            await _api.PostAsync("license/refresh", new { });
        }
        catch
        {
            // قطعی شبکه/سرور — بی‌صدا؛ مقادیر محلی مرجع نمایش می‌مانند
        }
    }

    /// <summary>تازه‌سازی + بارگذاری اطلاعات درمانگاه (ورود به بخش ۰)</summary>
    private async Task RefreshThenLoadClinicInfoAsync()
    {
        IsBusy = true;
        await RefreshLicenseFromServerAsync();
        await LoadClinicInfoAsync();
        IsBusy = false;
    }

    /// <summary>تازه‌سازی + بارگذاری وضعیت ماژول‌ها (ورود به بخش ۱)</summary>
    private async Task RefreshThenLoadModulesAsync()
    {
        IsBusy = true;
        await RefreshLicenseFromServerAsync();
        await LoadModulesStatusAsync();
        IsBusy = false;
    }

    /* ─── اطلاعات درمانگاه ─── */

    /// <summary>بارگذاری اطلاعات درمانگاه از تنظیمات — چرخهٔ بیست‌وهفتم: شهر هم
    /// خوانده و نمایش داده می‌شود (پنل لایسنس فیلد شهر جدا دارد و همگام‌ساز
    /// آن را در کلید City ذخیره می‌کند ولی فرم نمایشش نمی‌داد)</summary>
    private async Task LoadClinicInfoAsync()
    {
        var response = await _api.GetAsync<SettingsResponse>("settings?category=ClinicInfo");
        var map = response?.Data ?? new List<SettingDto>();
        ClinicName = Value(map, "ClinicName");
        LicenseNumber = Value(map, "LicenseNumber");
        EconomicCode = Value(map, "EconomicCode");
        NationalId = Value(map, "NationalId");
        ManagerName = Value(map, "ManagerName");
        Phone = Value(map, "Phone");
        Email = Value(map, "Email");
        City = Value(map, "City");
        Address = Value(map, "Address");
    }

    // ═══ چرخهٔ بیست‌وهفتم — حذف SaveClinicInfoAsync ═══
    // «اطلاعات درمانگاه فقط از سرور لایسنس قابل تغییر باشد»: دکمهٔ ذخیره در
    // چرخه ۲۴ حذف شد ولی متد ذخیره‌ساز باقی مانده بود؛ برای اینکه هیچ مسیر
    // نوشتنی از داخل برنامه روی ClinicInfo نماند، متد هم حذف شد — تنها مرجع
    // تغییر، همگام‌ساز خودکار از پنل لایسنس است (SyncClinicInfoFromLicenseAsync).

    /* ─── ماژول‌ها ─── */

    /// <summary>مقداردهی اولیه فهرست ماژول‌ها — وضعیت لایسنس از سرور تازه می‌شود</summary>
    private void InitializeModules()
    {
        var catalog = new (string Title, string Icon)[]
        {
            ("بیماران", "👤"), ("نوبت‌دهی", "📅"), ("پذیرش", "🩺"), ("نسخه‌نویسی", "💊"),
            ("صندوق و فاکتور", "💰"), ("حسابداری", "📊"), ("بیمه", "🏦"), ("آزمایشگاه", "🧪"),
            ("داروخانه", "🏥"), ("پیامک", "📱"), ("تصویربرداری", "📷"), ("دندانپزشکی", "🦷"),
            ("چشم‌پزشکی", "👁"), ("فیزیوتراپی", "🏃"), ("سپاس", "🏛"), ("نسخه الکترونیک", "📤")
        };
        foreach (var (title, icon) in catalog)
            Modules.Add(new ModuleStatusRow { Title = title, Icon = icon, IsCore = title is "بیماران" or "نوبت‌دهی" or "پذیرش" });
    }

    /// <summary>بارگذاری وضعیت لایسنس ماژول‌ها از سرور</summary>
    private async Task LoadModulesStatusAsync()
    {
        var response = await _api.GetAsync<LicenseStatusResponse>("license/status");
        var enabled = response?.Data?.EnabledModules ?? new List<string>();
        foreach (var m in Modules)
        {
            var isLicensed = enabled.Contains(m.Title, StringComparer.OrdinalIgnoreCase) ||
                             enabled.Contains(ToEnglishKey(m.Title), StringComparer.OrdinalIgnoreCase);
            m.LicenseText = isLicensed ? "✅ مجاز" : "❌ ندارد";
            m.IsToggleEnabled = isLicensed && !m.IsCore;
            m.IsEnabled = isLicensed; // مجاز و پیش‌فرض فعال — Core همیشه فعال
            m.IsLocked = !isLicensed;
        }
    }

    /// <summary>تبدیل عنوان فارسی به کلید انگلیسی ماژول</summary>
    private static string ToEnglishKey(string title) => title switch
    {
        "بیماران" => "Patients", "نوبت‌دهی" => "Appointments", "پذیرش" => "Receptions",
        "نسخه‌نویسی" => "Prescriptions", "صندوق و فاکتور" => "Billing", "حسابداری" => "Accounting",
        "بیمه" => "Insurance", "آزمایشگاه" => "Lab", "داروخانه" => "Pharmacy", "پیامک" => "Sms",
        "تصویربرداری" => "Imaging", "دندانپزشکی" => "Dental", "چشم‌پزشکی" => "Eye",
        "فیزیوتراپی" => "Physiotherapy", "سپاس" => "Government", "نسخه الکترونیک" => "Government",
        _ => title
    };

    /* ─── سامانه‌های دولتی ─── */

    /// <summary>مقداردهی کارت‌های سامانه دولتی — طبق فرم ۲۸</summary>
    private void InitializeGovSystems()
    {
        GovSystems.Add(new GovSystemCard { Name = "سامانه سپاس", Code = "sepas", ApiUrl = "https://api.sepas.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه سیب", Code = "sib", ApiUrl = "https://sib.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه ناب", Code = "nab", ApiUrl = "https://nab.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه سینا", Code = "sina", ApiUrl = "https://sina.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "نسخه الکترونیک", Code = "erx", ApiUrl = "https://erx.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "بیمه سلامت", Code = "health-insurance", ApiUrl = "", IsConnected = false });
    }

    /// <summary>بارگذاری وضعیت اتصال سامانه‌های دولتی از تنظیمات</summary>
    private async Task LoadGovSystemsAsync()
    {
        var response = await _api.GetAsync<SettingsResponse>("settings?category=Government");
        var map = response?.Data ?? new List<SettingDto>();
        foreach (var g in GovSystems)
        {
            g.Username = Value(map, $"{g.Code}.username");
            g.IsEnabled = Value(map, $"{g.Code}.enabled") == "true";
        }
    }

    /// <summary>ذخیره سامانه دولتی — از دکمه کارت</summary>
    [RelayCommand]
    private async Task SaveGovSystemAsync(GovSystemCard? g)
    {
        if (g is null) return;
        await SaveSettingsBatchAsync("Government", new Dictionary<string, string?>
        {
            [$"{g.Code}.apiUrl"] = g.ApiUrl,
            [$"{g.Code}.username"] = g.Username,
            [$"{g.Code}.enabled"] = g.IsEnabled ? "true" : "false"
        });
    }

    /* ─── چاپگر ─── */

    /// <summary>ذخیره تنظیمات چاپگر — شامل چاپگر نوبت‌دهی و چاپ خودکار قبض.
    /// چرخه ۲۴: مقادیر کلیدی (چاپگر نوبت/پرچم خودکار/نام درمانگاه) علاوه بر سرور
    /// در ذخیره‌گاه محلی هم آینه می‌شوند تا لحظهٔ ثبت نوبت بدون درخواست شبکه
    /// در دسترس باشند.</summary>
    [RelayCommand]
    private async Task SavePrinterSettingsAsync()
    {
        await SaveSettingsBatchAsync("Printer", new Dictionary<string, string?>
        {
            ["DefaultPrinter"] = DefaultPrinter,
            ["InvoicePrinter"] = InvoicePrinter,
            ["PrescriptionPrinter"] = PrescriptionPrinter,
            ["TicketPrinter"] = TicketPrinter,
            ["InvoicePaperSize"] = InvoicePaperSize,
            ["ShowPreview"] = ShowPreview ? "true" : "false",
            ["AutoPrintInvoice"] = AutoPrintInvoice ? "true" : "false",
            ["AutoPrintTicket"] = AutoPrintTicket ? "true" : "false",
            ["PrintCopies"] = PrintCopies.ToString()
        });

        // آینهٔ محلی — دسترسی بی‌درنگ هنگام ثبت نوبت (بدون رفت‌وبرگشت شبکه)
        try
        {
            LocalSettingsStore.Set("Printer.TicketPrinter", TicketPrinter ?? string.Empty);
            LocalSettingsStore.Set("Printer.AutoPrintTicket", AutoPrintTicket ? "true" : "false");
            LocalSettingsStore.Set("Printer.ClinicName", ClinicName ?? string.Empty);
            // ═══ چرخهٔ ۶۷ — آینهٔ پرچم پیش‌نمایش برای دروازهٔ PrintFlow ═══
            // همهٔ چاپ‌های عمومی (قبض/رسید/کارت/فیش/آزمایش) از PrintFlow
            // می‌گذرند و بی‌درنگ (بدون درخواست شبکه) این مقدار را می‌خوانند.
            LocalSettingsStore.Set("Printer.ShowPreview", ShowPreview ? "true" : "false");
        }
        catch
        {
            // ذخیرهٔ محلی هرگز نباید جریان را متوقف کند
        }

        _snackbar.Success("تنظیمات چاپگر ذخیره شد");
    }

    /// <summary>بارگذاری تنظیمات چاپگر ذخیره‌شده + کش فهرست چاپگرهای محلی —
    /// چرخه ۲۴: مقادیر ذخیره‌شده در بخش چاپگر نمایش داده شوند و فهرست
    /// چاپگرهای ویندوز هم برای انتخاب در دسترس باشد</summary>
    private async Task LoadPrinterSettingsAsync()
    {
        // فهرست چاپگرهای نصب‌شدهٔ ویندوز — از سرویس چاپ ویندوز (بدون کرش در نبود چاپگر)
        try
        {
            var server = new System.Printing.LocalPrintServer();
            foreach (var queue in server.GetPrintQueues()
                         .OrderBy(q => q.FullName, StringComparer.CurrentCultureIgnoreCase))
                PrinterNames.Add(queue.FullName);
        }
        catch
        {
            // محیط بدون سرویس چاپ — فهرست خالی می‌ماند و ComboBox ویرایش‌پذیر است
        }

        // مقادیر ذخیره‌شدهٔ تنظیمات چاپگر از سرور
        var response = await _api.GetAsync<SettingsResponse>("settings?category=Printer");
        var map = response?.Data ?? new List<SettingDto>();
        DefaultPrinter = Value(map, "DefaultPrinter");
        InvoicePrinter = Value(map, "InvoicePrinter");
        PrescriptionPrinter = Value(map, "PrescriptionPrinter");
        TicketPrinter = Value(map, "TicketPrinter");
        InvoicePaperSize = string.IsNullOrWhiteSpace(Value(map, "InvoicePaperSize")) ? "A5" : Value(map, "InvoicePaperSize");
        ShowPreview = Value(map, "ShowPreview") != "false";
        AutoPrintInvoice = Value(map, "AutoPrintInvoice") == "true";
        AutoPrintTicket = Value(map, "AutoPrintTicket") == "true";
        var copies = Value(map, "PrintCopies");
        if (int.TryParse(copies, out var n) && n is >= 1 and <= 10) PrintCopies = n;
    }

    /// <summary>تست چاپگر — ارسال صفحه آزمایشی به چاپگر پیش‌فرض ویندوز</summary>
    [RelayCommand]
    private void TestPrint()
    {
        try
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
                _snackbar.Success($"تست چاپ به «{printDialog.PrintQueue.FullName}\" ارسال شد");
        }
        catch (Exception)
        {
            _snackbar.Error("دسترسی به چاپگر ممکن نشد");
        }
    }

    /* ─── تم ─── */

    /// <summary>تغییر تم روشن/تاریک — اعمال فوری روی پوسته</summary>
    [RelayCommand]
    private void ApplyTheme() => _theme.SetTheme(IsDarkTheme);

    /// <summary>
    /// چرخهٔ ۷۹ (بازطراحی صفحهٔ تنظیمات) — کلید سریع «روشن/تاریک» در سربرگ
    /// قهرمان تنظیمات: فقط مقدار IsDarkTheme را برمی‌گرداند؛ partial
    /// OnIsDarkThemeChanged اعمالِ پوسته و ذخیرهٔ سروری را انجام می‌دهد.
    /// </summary>
    [RelayCommand]
    private void ToggleDarkMode() => IsDarkTheme = !IsDarkTheme;

    /// <summary>انتخاب تم رنگی از کاتالوگ — اعمال فوری + ذخیره (چرخه ۱۹)</summary>
    [RelayCommand]
    private void SelectTheme(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        SelectedThemeKey = key;
    }

    /// <summary>با تغییر کلید تم: اعمال روی پوسته + تازه‌سازی نشانگر انتخاب + ذخیره</summary>
    partial void OnSelectedThemeKeyChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || _initializing)
            return;
        _theme.ApplyTheme(value, IsDarkTheme);
        RefreshThemeSelection();
        _ = SaveThemeSettingsAsync();
    }

    /// <summary>تغییر حالت روشن/تیره از دکمه‌های تنظیمات — اعمال بی‌درنگ</summary>
    partial void OnIsDarkThemeChanged(bool value)
    {
        if (_initializing)
            return;
        _theme.ApplyTheme(SelectedThemeKey, value);
        _ = SaveThemeSettingsAsync();
    }

    /// <summary>
    /// تغییر فونت — چرخه بیستم: بی‌درنگ از طریق منبع پویای App.FontFamily روی
    /// همهٔ پنجره‌ها اعمال می‌شود؛ ذخیرهٔ سروری با توقف ۸۰۰ms (چرخه بیست‌ویکم)
    /// </summary>
    partial void OnSelectedFontChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || _initializing)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>تغییر سایز فونت پایه — بی‌درنگ اعمال + ذخیرهٔ با توقف (چرخه بیست‌ویکم)</summary>
    partial void OnBaseFontSizeChanged(double value)
    {
        if (value is < 11 or > 18 || _initializing)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>تغییر انیمیشن‌ها — ذخیره و اعمال (چرخه بیست‌ویکم)</summary>
    partial void OnAnimationsEnabledChanged(bool value)
    {
        if (_initializing)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>
    /// تغییر رنگ منوی کناری — چرخه بیست‌ویکم: اعمال بی‌درنگ مستقل از تم.
    /// چرخهٔ بیست‌ونهم: انتخاب گزینهٔ «🎨 رنگ دلخواه…» انتخاب‌گر حرفه‌ای را
    /// باز می‌کند؛ انصراف به گزینهٔ قبلی برمی‌گردد (بدون رنگ بی‌حساب اعمال نمی‌شود).
    /// </summary>
    partial void OnSidebarColorIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SidebarSwatchHex));
        if (_initializing)
            return;

        // گزینهٔ دلخواه — انتخاب‌گر را باز کن؛ انصراف = بازگشت به گزینهٔ قبلی
        if (value == SidebarCustomIndex)
        {
            var hex = Views.ColorPickerWindow.Pick(System.Windows.Application.Current?.MainWindow,
                "رنگ منوی کناری", SidebarCustomHex ?? "#16355E");
            if (hex is null)
            {
                _initializing = true;
                SidebarColorIndex = _lastSidebarIndex;
                _initializing = false;
                return;
            }
            SidebarCustomHex = hex;
        }
        else if (value >= 0 && value < SidebarCustomIndex)
        {
            _lastSidebarIndex = value;
        }
        ApplyAppearanceNow();
    }

    /// <summary>آخرین گزینهٔ غیردلخواه منو — برای بازگشت پس از انصراف انتخاب‌گر</summary>
    private int _lastSidebarIndex;

    /// <summary>
    /// گشودن مجدد انتخاب‌گر رنگ منو — دکمهٔ 🎨 کنار کشو؛ رنگ دلخواهِ ذخیره‌شده
    /// مقدار اولیهٔ انتخاب‌گر است تا تنظیم دقیق قبلی حفظ شود.
    /// </summary>
    [RelayCommand]
    private void PickSidebarColor()
    {
        var hex = Views.ColorPickerWindow.Pick(System.Windows.Application.Current?.MainWindow,
            "رنگ منوی کناری", SidebarCustomHex ?? "#16355E");
        if (hex is null) return;
        SidebarCustomHex = hex;
        _initializing = true;
        SidebarColorIndex = SidebarCustomIndex;
        _initializing = false;
        ApplyAppearanceNow();
        _snackbar.Success($"رنگ دلخواه منو ({hex}) اعمال شد");
    }

    /// <summary>تغییر چگالی مستقل — چرخه بیست‌ویکم</summary>
    partial void OnDensityIndexChanged(int value)
    {
        if (_initializing)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>تغییر فعال‌سازی گوشهٔ مستقل — چرخه بیست‌ویکم</summary>
    partial void OnUseCustomRadiusChanged(bool value)
    {
        if (_initializing)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>تغییر مقدار گوشهٔ مستقل — چرخه بیست‌ویکم</summary>
    partial void OnCustomCardRadiusChanged(int value)
    {
        if (_initializing || !UseCustomRadius)
            return;
        ApplyAppearanceNow();
    }

    /// <summary>
    /// اعمال بی‌درنگ تنظیمات ظاهری فعلی (فونت/سایز/انیمیشن/واحد/رنگ منو/گوشه/چگالی)
    /// + زمان‌بندی ذخیرهٔ سروری با توقف ۸۰۰ms — چرخه بیست‌ویکم: اعمال محلی فوری
    /// است و سرور فقط یک‌بار پس از آرام‌شدن تغییرات خبردار می‌شود (رفع 429).
    /// </summary>
    private void ApplyAppearanceNow()
    {
        _theme.ApplyAppearance(new Services.AppearanceSettings(
            FontKey(SelectedFont),
            BaseFontSize,
            AnimationsEnabled,
            CurrencyUnitIndex == 1 ? "Toman" : "Rial",
            // رنگ منو — چرخهٔ بیست‌ونهم: گزینهٔ دلخواه از رنگ دلخواه می‌آید
            EffectiveSidebarHex,
            // گوشهٔ مستقل — فقط وقتی چک‌باکس فعال است
            UseCustomRadius ? Math.Clamp(CustomCardRadius, 0, 24) : null,
            // چگالی مستقل — ۰ از تم، ۱ استاندارد، ۲ فشرده
            DensityIndex switch { 1 => "Normal", 2 => "Compact", _ => null },
            // چرخه ۲۴ — رنگ اصلی سفارشی (۰ = همگام با تم) و حالت رنگ آیکون‌ها
            EffectivePrimaryHex,
            IconColorModeIndex == 1 ? "Colorful" : "Mono",
            // چرخهٔ بیست‌وهفتم — مجموعهٔ آیکون‌های منو
            IconSetIndex >= 0 && IconSetIndex < IconSetKeys.Length ? IconSetKeys[IconSetIndex] : "Classic"));

        ScheduleThemeSave();
    }

    /// <summary>
    /// زمان‌بندی ذخیرهٔ سروری با توقف — هر تغییر تایمر را ریست می‌کند؛ فقط ۸۰۰ms
    /// پس از آخرین تغییر، یک‌بار SaveThemeSettingsAsync اجرا می‌شود.
    /// </summary>
    private void ScheduleThemeSave()
    {
        if (_themeSaveDebouncer is null)
        {
            _themeSaveDebouncer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _themeSaveDebouncer.Tick += (_, _) =>
            {
                _themeSaveDebouncer!.Stop();
                _ = SaveThemeSettingsAsync();
            };
        }
        _themeSaveDebouncer.Stop();
        _themeSaveDebouncer.Start();
    }

    /// <summary>تغییر واحد پول نمایشی — چرخه بیست‌ویکم: اعمال بی‌درنگ + ذخیره با توقف</summary>
    partial void OnCurrencyUnitIndexChanged(int value)
    {
        if (_initializing)
            return;
        ApplyAppearanceNow();
        _snackbar.Info(value == 1
            ? "واحد نمایشی به «تومان» تغییر یافت — فقط نمایش؛ محاسبات ریالی می‌ماند"
            : "واحد نمایشی به «ریال» تغییر یافت");
    }

    /// <summary>هماهنگی نشانگر انتخاب‌شده با کلید فعلی</summary>
    private void RefreshThemeSelection()
    {
        foreach (var row in ThemeOptions)
            row.IsSelected = string.Equals(row.Theme.Key, SelectedThemeKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ذخیره تنظیمات تم — چرخه بیست‌ویکم: فونت/سایز/واحد + سه سفارشی‌سازی تفکیکی
    /// (رنگ منو/گوشه/چگالی) هم همراه می‌شوند
    /// </summary>
    [RelayCommand]
    private async Task SaveThemeSettingsAsync()
    {
        await SaveSettingsBatchAsync("Theme", new Dictionary<string, string?>
        {
            ["IsDark"] = IsDarkTheme ? "true" : "false",
            ["Name"] = SelectedThemeKey,
            ["Font"] = FontKey(SelectedFont),
            ["BaseFontSize"] = BaseFontSize.ToString("0"),
            ["Animations"] = AnimationsEnabled ? "true" : "false",
            ["CurrencyUnit"] = CurrencyUnitIndex == 1 ? "Toman" : "Rial",
            // چرخه بیست‌ویکم — سفارشی‌سازی تفکیکی عناصر (چرخهٔ ۲۹: دلخواه پشتیبانی)
            ["SidebarHex"] = EffectiveSidebarHex ?? "",
            ["CardRadiusOverride"] = UseCustomRadius ? Math.Clamp(CustomCardRadius, 0, 24).ToString() : "",
            ["DensityOverride"] = DensityIndex switch { 1 => "Normal", 2 => "Compact", _ => "" },
            // چرخه ۲۴ — رنگ اصلی سفارشی و حالت رنگ آیکون‌ها (چرخهٔ ۲۹: دلخواه)
            ["CustomPrimaryHex"] = EffectivePrimaryHex ?? "",
            ["IconColorMode"] = IconColorModeIndex == 1 ? "Colorful" : "Mono"
        });
    }

    // ═══ چرخه ۲۴ — پریست‌های شخصی تم (ذخیره/اعمال/حذف) ═══
    // «تنظیمات شخصی رو بتونیم ذخیره و بعداً استفاده بکنیم» — کل ترکیب فعلی
    // (تم رنگی + روشن/تیره + فونت/اندازه + همهٔ سفارشی‌سازی‌ها) به‌صورت JSON
    // در ذخیره‌گاه محلی نگهداری و با یک کلیک بازیابی می‌شود.

    /// <summary>بارگذاری فهرست پریست‌های ذخیره‌شده — از کلیدهای ذخیره‌گاه محلی</summary>
    private void LoadThemePresets()
    {
        try
        {
            ThemePresets.Clear();
            foreach (var key in LocalSettingsStore.Keys().Where(k => k.StartsWith("ThemePreset.", StringComparison.Ordinal)))
                ThemePresets.Add(key["ThemePreset.".Length..]);
        }
        catch
        {
            // ذخیره‌گاه محلی نباید جریان را متوقف کند
        }
    }

    /// <summary>ذخیرهٔ ترکیب فعلی به‌عنوان پریست با نام دلخواه</summary>
    [RelayCommand]
    private void SaveThemePreset()
    {
        var name = PresetName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _snackbar.Error("نام پریست را وارد کنید");
            return;
        }

        // JSON تمام وجوه ترکیب فعلی — ساده و قابل‌خواندن توسط انسان
        var json = System.Text.Json.JsonSerializer.Serialize(new ThemePresetData
        {
            ThemeKey = SelectedThemeKey,
            IsDark = IsDarkTheme,
            Font = SelectedFont,
            BaseFontSize = BaseFontSize,
            Animations = AnimationsEnabled,
            CurrencyToman = CurrencyUnitIndex == 1,
            SidebarHex = EffectiveSidebarHex,
            CardRadius = UseCustomRadius ? Math.Clamp(CustomCardRadius, 0, 24) : null,
            Density = DensityIndex switch { 1 => "Normal", 2 => "Compact", _ => null },
            CustomPrimaryHex = EffectivePrimaryHex,
            ColorfulIcons = IconColorModeIndex == 1
        });
        LocalSettingsStore.Set($"ThemePreset.{name}", json);
        LoadThemePresets();
        SelectedThemePreset = name;
        PresetName = string.Empty;
        _snackbar.Success($"پریست «{name}» ذخیره شد");
    }

    /// <summary>اعمال پریست انتخاب‌شده — بازگرداندن کل ترکیب ذخیره‌شده</summary>
    [RelayCommand]
    private void ApplyThemePreset()
    {
        var name = SelectedThemePreset;
        if (string.IsNullOrWhiteSpace(name))
        {
            _snackbar.Error("ابتدا یک پریست را از فهرست انتخاب کنید");
            return;
        }

        var json = LocalSettingsStore.Get($"ThemePreset.{name}");
        if (string.IsNullOrWhiteSpace(json))
        {
            _snackbar.Error("پریست یافت نشد");
            return;
        }

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<ThemePresetData>(json);
            if (data is null)
            {
                _snackbar.Error("محتوای پریست قابل خواندن نیست");
                return;
            }

            _initializing = true;
            SelectedThemeKey = data.ThemeKey;
            IsDarkTheme = data.IsDark;
            _theme.ApplyTheme(data.ThemeKey, data.IsDark);
            SelectedFont = data.Font ?? "Vazirmatn";
            BaseFontSize = data.BaseFontSize is >= 11 and <= 18 ? data.BaseFontSize.Value : 13;
            AnimationsEnabled = data.Animations;
            CurrencyUnitIndex = data.CurrencyToman ? 1 : 0;
            // چرخهٔ بیست‌ونهم: رنگ ذخیره‌شده اگر در پالت ثابت نبود، همان رنگ
            // دلخواه است — کد ذخیره می‌شود و گزینهٔ «دلخواه…» انتخاب می‌شود
            var sidebarIdx = Array.IndexOf(SidebarColorHexes, data.SidebarHex);
            if (sidebarIdx >= 0) SidebarColorIndex = sidebarIdx;
            else if (!string.IsNullOrWhiteSpace(data.SidebarHex))
            {
                SidebarCustomHex = data.SidebarHex;
                SidebarColorIndex = SidebarCustomIndex;
            }
            else SidebarColorIndex = 0;
            UseCustomRadius = data.CardRadius.HasValue;
            CustomCardRadius = data.CardRadius ?? 8;
            DensityIndex = data.Density switch { "Normal" => 1, "Compact" => 2, _ => 0 };
            var primaryIdx = Array.IndexOf(CustomPrimaryHexes, data.CustomPrimaryHex);
            if (primaryIdx >= 0) CustomPrimaryIndex = primaryIdx;
            else if (!string.IsNullOrWhiteSpace(data.CustomPrimaryHex))
            {
                PrimaryCustomHex = data.CustomPrimaryHex;
                CustomPrimaryIndex = PrimaryCustomIndex;
            }
            else CustomPrimaryIndex = 0;
            IconColorModeIndex = data.ColorfulIcons ? 1 : 0;
            _initializing = false;

            // اعمال یکجای کل ترکیب + ذخیره
            ApplyAppearanceNow();
            _theme.ApplyTheme(_theme.CurrentThemeKey, _theme.IsDarkTheme);
            _snackbar.Success($"پریست «{name}» اعمال شد");
        }
        catch
        {
            _initializing = false;
            _snackbar.Error("خواندن پریست ناموفق بود");
        }
    }

    /// <summary>حذف پریست انتخاب‌شده</summary>
    [RelayCommand]
    private void DeleteThemePreset()
    {
        var name = SelectedThemePreset;
        if (string.IsNullOrWhiteSpace(name))
        {
            _snackbar.Error("ابتدا یک پریست را از فهرست انتخاب کنید");
            return;
        }
        LocalSettingsStore.Remove($"ThemePreset.{name}");
        LoadThemePresets();
        SelectedThemePreset = ThemePresets.FirstOrDefault();
        _snackbar.Success($"پریست «{name}» حذف شد");
    }

    /// <summary>دادهٔ پریست تم — سازگار با JSON برای ذخیره‌سازی محلی</summary>
    private sealed class ThemePresetData
    {
        public string ThemeKey { get; set; } = "Blue";
        public bool IsDark { get; set; }
        public string? Font { get; set; }
        public double? BaseFontSize { get; set; }
        public bool Animations { get; set; } = true;
        public bool CurrencyToman { get; set; }
        public string? SidebarHex { get; set; }
        public int? CardRadius { get; set; }
        public string? Density { get; set; }
        public string? CustomPrimaryHex { get; set; }
        public bool ColorfulIcons { get; set; }
    }

    /* ─── امنیت ─── */

    /// <summary>ذخیره تنظیمات امنیت</summary>
    [RelayCommand]
    private async Task SaveSecuritySettingsAsync()
    {
        await SaveSettingsBatchAsync("Security", new Dictionary<string, string?>
        {
            ["MinPasswordLength"] = MinPasswordLength.ToString(),
            ["RequireMixedCase"] = RequireMixedCase ? "true" : "false",
            ["RequireDigit"] = RequireDigit ? "true" : "false",
            ["RequireSpecialChar"] = RequireSpecialChar ? "true" : "false",
            ["MaxFailedAttempts"] = MaxFailedAttempts.ToString(),
            ["LockoutMinutes"] = LockoutMinutes.ToString(),
            ["PasswordExpiryDays"] = PasswordExpiryDays.ToString(),
            ["SessionTimeoutMinutes"] = SessionTimeoutMinutes.ToString(),
            ["AuditLogEnabled"] = AuditLogEnabled ? "true" : "false"
        });
    }

    /* ─── کمکی ─── */

    /// <summary>خواندن مقدار کلید از فهرست تنظیمات</summary>
    private static string? Value(List<SettingDto> settings, string key)
        => settings.FirstOrDefault(s => s.Key == key)?.Value;

    /// <summary>ذخیره دسته تنظیمات با یک کلید دسته</summary>
    private async Task SaveSettingsBatchAsync(string category, Dictionary<string, string?> entries)
    {
        IsBusy = true;
        var allOk = true;
        foreach (var (key, value) in entries)
        {
            var ok = await _api.PutAsync("settings", new { key, value, category });
            if (!ok) allOk = false;
        }
        IsBusy = false;
        if (allOk) _snackbar.Success("تنظیمات ذخیره شد");
        else _snackbar.Error(_api.LastError ?? "ذخیره برخی تنظیمات ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    private sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Category { get; set; }
    }

    private sealed class LicenseStatusResponse
    {
        public bool IsSuccess { get; set; }
        public LicenseStatusData? Data { get; set; }
    }

    private sealed class LicenseStatusData
    {
        public bool IsValid { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDoctors { get; set; }
        public int MaxPatients { get; set; }
        public List<string>? EnabledModules { get; set; }
        public string? Message { get; set; }
    }
}

/// <summary>ردیف ماژول با وضعیت لایسنس — بخش ماژول‌ها</summary>
public partial class ModuleStatusRow : ObservableObject
{
    /// <summary>عنوان فارسی</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>آیکن</summary>
    public string Icon { get; init; } = "";

    /// <summary>ماژول هسته — غیرقابل غیرفعال‌سازی</summary>
    public bool IsCore { get; init; }

    /// <summary>متن وضعیت لایسنس</summary>
    [ObservableProperty]
    private string _licenseText = "—";

    /// <summary>فعال در سیستم</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>کلید لایسنس ندارد</summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary>سوییچ فعال/غیرفعال قابل تغییر است</summary>
    [ObservableProperty]
    private bool _isToggleEnabled;

    /// <summary>متن دکمه عملیات</summary>
    public string ActionText => IsCore ? "Core" : (IsLocked ? "خرید ماژول" : "⚙️");
}

/// <summary>کارت سامانه دولتی — بخش سامانه‌های دولتی</summary>
public partial class GovSystemCard : ObservableObject
{
    /// <summary>نام نمایشی</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>کد سرویس</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>آدرس API</summary>
    [ObservableProperty]
    private string? _apiUrl;

    /// <summary>نام کاربری</summary>
    [ObservableProperty]
    private string? _username;

    /// <summary>رمز عبور — فقط ورود جدید</summary>
    [ObservableProperty]
    private string? _password;

    /// <summary>فعال</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>وضعیت اتصال آخرین بررسی</summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>متن وضعیت اتصال</summary>
    public string ConnectedText => IsConnected ? "🟢 متصل" : "⚪ بررسی نشده";
}

/// <summary>
/// ردیف تم رنگی در فهرست تنظیمات — چرخه ۱۹:
/// نشانگر انتخاب + رنگ پیش‌نمایش به‌صورت برش‌پذیر برای XAML
/// </summary>
public partial class ThemeOptionRow : ObservableObject
{
    /// <summary>تعریف تم از کاتالوگ</summary>
    public required AppTheme Theme { get; init; }

    /// <summary>کلید</summary>
    public string Key => Theme.Key;

    /// <summary>نام فارسی</summary>
    public string Title => Theme.Title;

    /// <summary>آیکون تم</summary>
    public string Icon => Theme.Icon;

    /// <summary>رنگ پیش‌نمایش</summary>
    public System.Windows.Media.Color PrimaryColor => Theme.PrimaryColor;

    /// <summary>شعاع گوشهٔ تم به‌صورت CornerRadius — برای قالب کارت تم (چرخه بیستم)</summary>
    public System.Windows.CornerRadius Radius => new(Theme.CardRadius);

    /// <summary>متن مشخصات ظاهری — «گوشه‌گرد ۱۴ • استاندارد/فشرده» (چرخه بیستم)</summary>
    public string DensityText => $"⤴ گوشه {Theme.CardRadius} • {(Theme.IsCompact ? "فشرده" : "استاندارد")}";

    /// <summary>انتخاب‌شده است؟</summary>
    [ObservableProperty]
    private bool _isSelected;
}
