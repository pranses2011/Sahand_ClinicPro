using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ═══ ViewModel ویرایش کاتالوگ آزمایش — چرخهٔ ۷۲ ═══
/// گزارش مالک: «بازه‌های مرجع و حدود بحرانی هر آزمایش رو بطور کامل و دقیق
/// تکمیل کن… در صورت نیاز بتونم اصلاحشون بکنم». این فرم ابزارِ اصلاح مالک
/// است: همهٔ ۹ ستون ساخت‌یافتهٔ بازه (کف/سقف × مرد/زن/کودک + سقف سن کودک)
/// + حدود بحرانی + واحد/قیمت/زمان جواب + متن‌های نمایشی قدیمی.
///
/// قواعد رابط (قانون پروژه):
///  ۱) همهٔ فیلدهای عددی «متن‌محور» هستند تا ارقام فارسی پذیرفته و نمایش
///     داده شوند؛ پارس با ParseDecimal (ارقام فارسی/عربی → لاتین، ممیز
///     فارسی «٫» و عربی «٬/٫» نرمال می‌شوند).
///  ۲) فیلد پولی قیمت با رفتار MoneyInput (جداکنندهٔ سه‌رقمی زنده) — قانون
///     «فیلدهای پولی جداکنندهٔ سه‌رقمی دارند».
///  ۳) فیلد خالی یعنی «بدون محدودیت» برای آن سمت (مثلاً HDL فقط کف دارد) —
///     نه صفر! صفر معنای بالینی متفاوت دارد.
///  ۴) اعتبارسنجی سمت کلاینت همان قواعد سرور است: کف ≤ سقف هر بازه و
///     حدود بحرانی بیرونِ بازهٔ مرجع — پیام‌های فارسی و شفاف.
/// </summary>
public partial class LabTestEditViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly long _testId;

    /// <summary>درخواست بستن پنجره — بدون ذخیره</summary>
    public event EventHandler? CloseRequested;

    /// <summary>ذخیرهٔ موفق — پنجره بسته و کاتالوگ تازه‌سازی می‌شود</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>
    /// آیا ذخیره با موفقیت انجام شد؟ — پس از بسته‌شدن Dialog، فرم کاتالوگ
    /// از همین پرچم می‌فهمد که باید لیست را تازه‌سازی کند (الگوی متداول
    /// ShowDialog + بررسی نتیجه، بدون نیاز به رویداد سرریز‌شونده).
    /// </summary>
    public bool WasSaved { get; private set; }

    /// <summary>عنوان پنجره — نام آزمایش در حال ویرایش</summary>
    public override string Title => $"ویرایش آزمایش: {TestName}";

    /* ─── هویت آزمایش (فقط-خواندنی — نمایشی) ─── */

    /// <summary>نام فارسی آزمایش</summary>
    [ObservableProperty]
    private string _testName = string.Empty;

    /// <summary>نام انگلیسی آزمایش</summary>
    [ObservableProperty]
    private string? _testNameEn;

    /// <summary>کد آزمایش</summary>
    [ObservableProperty]
    private string? _testCode;

    /// <summary>دستهٔ آزمایش (هماتولوژی/بیوشیمی/…) — نمایشی</summary>
    [ObservableProperty]
    private string? _category;

    /// <summary>واحد اندازه‌شده در آزمایشگاه — قابل ویرایش</summary>
    [ObservableProperty]
    private string? _unit;

    /* ─── متن‌های نمایشی قدیمی (برگهٔ نتیجهٔ عمومی) ─── */

    /// <summary>متن حد طبیعی نمایشی مردان — مثل «۱۳.۵-۱۷.۵»</summary>
    [ObservableProperty]
    private string? _normalRangeMale;

    /// <summary>متن حد طبیعی نمایشی زنان</summary>
    [ObservableProperty]
    private string? _normalRangeFemale;

    /* ─── قیمت و زمان جواب ─── */

    /// <summary>قیمت (تومان/ریال) — متن با جداکنندهٔ سه‌رقمی زنده (MoneyInput)</summary>
    [ObservableProperty]
    private string _priceText = "۰";

    /// <summary>زمان جواب به ساعت — متن عددی فارسی</summary>
    [ObservableProperty]
    private string _tatText = "۲۴";

    /* ─── بازهٔ مرجع بزرگسال مرد (ساخت‌یافته) ─── */

    /// <summary>کف بازهٔ مردان — متن عددی فارسی؛ خالی = بدون کف</summary>
    [ObservableProperty]
    private string _minMale = string.Empty;

    /// <summary>سقف بازهٔ مردان — خالی = بدون سقف (مثل HDL)</summary>
    [ObservableProperty]
    private string _maxMale = string.Empty;

    /* ─── بازهٔ مرجع بزرگسال زن ─── */

    /// <summary>کف بازهٔ زنان</summary>
    [ObservableProperty]
    private string _minFemale = string.Empty;

    /// <summary>سقف بازهٔ زنان</summary>
    [ObservableProperty]
    private string _maxFemale = string.Empty;

    /* ─── بازهٔ مرجع کودک ─── */

    /// <summary>کف بازهٔ کودک</summary>
    [ObservableProperty]
    private string _minChild = string.Empty;

    /// <summary>سقف بازهٔ کودک</summary>
    [ObservableProperty]
    private string _maxChild = string.Empty;

    /// <summary>سقف سن کودک (سال) — پیش از این سن بازهٔ کودک اعمال می‌شود</summary>
    [ObservableProperty]
    private string _childAgeText = string.Empty;

    /* ─── حدود بحرانی — هشدار فوری پزشک ─── */

    /// <summary>کف مقدار بحرانی — برابر یا کمتر = هشدار فوری</summary>
    [ObservableProperty]
    private string _criticalLow = string.Empty;

    /// <summary>سقف مقدار بحرانی — برابر یا بیشتر = هشدار فوری</summary>
    [ObservableProperty]
    private string _criticalHigh = string.Empty;

    /// <summary>
    /// سازنده — ردیف کاتالوگ از سرور (LabTestCatalogDto چرخهٔ ۷۲) به فیلدهای
    /// متنی «ارقام فارسی» نگاشت می‌شود؛ اعداد تهی رشتهٔ خالی می‌مانند (نه «۰»).
    /// </summary>
    /// <param name="api">کلاینت API</param>
    /// <param name="row">ردیف کاتالوگ آزمایش (LabViewModel.TestCatalogRow — رکورد کامل چرخهٔ ۷۲)</param>
    public LabTestEditViewModel(IApiClient api, LabViewModel.TestCatalogRow row)
    {
        _api = api;
        _testId = row.Id;

        TestName = row.Name;
        TestNameEn = row.TestNameEn;
        TestCode = row.TestCode;
        Category = row.Category;
        Unit = row.Unit;

        NormalRangeMale = row.NormalRangeMale;
        NormalRangeFemale = row.NormalRangeFemale;

        // قیمت با جداکنندهٔ سه‌رقمی فارسی — قانون نمایش مبالغ
        PriceText = Money.ToPersianDigits(row.Price.ToString("N0"));
        TatText = Money.ToPersianDigits(row.TurnAroundTime?.ToString() ?? "24");

        MinMale = Fa(row.RefMinAdultMale);
        MaxMale = Fa(row.RefMaxAdultMale);
        MinFemale = Fa(row.RefMinAdultFemale);
        MaxFemale = Fa(row.RefMaxAdultFemale);
        MinChild = Fa(row.RefMinChild);
        MaxChild = Fa(row.RefMaxChild);
        ChildAgeText = Fa(row.ChildAgeMaxYears);
        CriticalLow = Fa(row.CriticalLow);
        CriticalHigh = Fa(row.CriticalHigh);
    }

    /// <summary>نمایش فارسی یک عدد تهی‌پذیر — تهی → رشتهٔ خالی (بدون محدودیت)</summary>
    private static string Fa(decimal? value)
        => value is { } v ? Money.ToPersianDigits(v.ToString("0.####")) : string.Empty;

    /// <summary>
    /// پارس متن عددی «شکیبای فارسی» — ارقام فارسی/عربی به لاتین، ممیز فارسی
    /// «٫» و عربی «٬» به نقطه، فاصله و جداکنندهٔ هزارگان حذف؛ تهی/خالی → null.
    /// خروجی: موفق بودن پارس + مقدار.
    /// </summary>
    private static bool TryParse(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true; // خالی = بدون محدودیت — ورودی معتبر

        // نرمال‌سازی ارقام و علائم
        var sb = new System.Text.StringBuilder(text.Trim().Length);
        foreach (var ch in text.Trim())
        {
            var c = ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + ch - '۰'),            // ارقام فارسی
                >= '٠' and <= '٩' => (char)('0' + ch - '٠'),            // ارقام عربی
                '٫' or '،' or ',' => '.',                                // ممیز فارسی/عربی
                _ => ch
            };
            if (c == ' ' || c == '٬' || c == '_')
                continue; // جداکنندهٔ هزارگان/فاصله — حذف
            sb.Append(c);
        }

        var normalized = sb.ToString();
        if (string.IsNullOrEmpty(normalized))
        {
            return true; // فقط جداکننده بود — مثل خالی
        }

        if (decimal.TryParse(normalized,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }
        return false; // متن غیرعددی — خطای ورودی
    }

    /// <summary>
    /// اعتبارسنجی سمت کلاینت — همان قواعد سرور با پیام فارسی؛
    /// اولین خطا در ErrorMessage می‌نشیند و فرم باز می‌ماند.
    /// </summary>
    private string? Validate()
    {
        // پارس همهٔ فیلدها — هر ناپارس‌شدنی خطای شفاف می‌دهد
        var fields = new (string Label, string Text, decimal? Value)[]
        {
            ("کف بازهٔ مردان", MinMale, null), ("سقف بازهٔ مردان", MaxMale, null),
            ("کف بازهٔ زنان", MinFemale, null), ("سقف بازهٔ زنان", MaxFemale, null),
            ("کف بازهٔ کودک", MinChild, null), ("سقف بازهٔ کودک", MaxChild, null),
            ("سقف سن کودک", ChildAgeText, null),
            ("حد بحرانی پایین", CriticalLow, null), ("حد بحرانی بالا", CriticalHigh, null),
        };

        decimal? minM = null, maxM = null, minF = null, maxF = null, minC = null, maxC = null;
        decimal? childAge = null, critLow = null, critHigh = null;
        foreach (var (label, text, _) in fields)
        {
            if (!TryParse(text, out var parsed))
                return $"مقدار «{label}» عدد معتبر نیست — فقط عدد (ارقام فارسی یا لاتین) وارد کنید";

            // جای‌گذاری مقدار پارس‌شده در متغیر درست
            if (label == "کف بازهٔ مردان") minM = parsed;
            else if (label == "سقف بازهٔ مردان") maxM = parsed;
            else if (label == "کف بازهٔ زنان") minF = parsed;
            else if (label == "سقف بازهٔ زنان") maxF = parsed;
            else if (label == "کف بازهٔ کودک") minC = parsed;
            else if (label == "سقف بازهٔ کودک") maxC = parsed;
            else if (label == "سقف سن کودک") childAge = parsed;
            else if (label == "حد بحرانی پایین") critLow = parsed;
            else if (label == "حد بحرانی بالا") critHigh = parsed;
        }

        // ─── کف ≤ سقف در هر بازه ───
        if (minM.HasValue && maxM.HasValue && minM > maxM)
            return "کف بازهٔ مردان نمی‌تواند از سقف آن بیشتر باشد";
        if (minF.HasValue && maxF.HasValue && minF > maxF)
            return "کف بازهٔ زنان نمی‌تواند از سقف آن بیشتر باشد";
        if (minC.HasValue && maxC.HasValue && minC > maxC)
            return "کف بازهٔ کودک نمی‌تواند از سقف آن بیشتر باشد";

        // ─── سقف سن کودک در بازهٔ منطقی ───
        if (childAge.HasValue && (childAge < 1 || childAge > 18))
            return "سقف سن کودک باید بین ۱ تا ۱۸ سال باشد";

        // ─── حدود بحرانی بیرونِ بازهٔ مرجع ───
        if (critHigh.HasValue && maxM.HasValue && critHigh < maxM)
            return "حد بحرانی بالا نباید از سقف بازهٔ مردان کمتر باشد — مقدار بحرانی باید بیرون بازهٔ طبیعی باشد";
        if (critHigh.HasValue && maxF.HasValue && critHigh < maxF)
            return "حد بحرانی بالا نباید از سقف بازهٔ زنان کمتر باشد";
        if (critLow.HasValue && minM.HasValue && critLow > minM)
            return "حد بحرانی پایین نباید از کف بازهٔ مردان بیشتر باشد";
        if (critLow.HasValue && minF.HasValue && critLow > minF)
            return "حد بحرانی پایین نباید از کف بازهٔ زنان بیشتر باشد";

        return null; // همه‌چیز سالم
    }

    /// <summary>ذخیرهٔ ویرایش — PUT lab/tests/{id}</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // اعتبارسنجی محلی — خطا همان‌جا نمایش داده می‌شود (بدون رفت‌وبرگشت شبکه)
        var validationError = Validate();
        if (validationError is not null)
        {
            ErrorMessage = validationError;
            return;
        }

        // قیمت از رفتار MoneyInput می‌آید — متن قالب‌بندی‌شده را پارس می‌کنیم
        if (!TryParse(PriceText, out var price) || price is null || price < 0)
        {
            ErrorMessage = "قیمت عدد معتبر و نامنفی باشد";
            return;
        }
        if (!TryParse(TatText, out var tat) || tat is < 1 or > 720)
        {
            ErrorMessage = "زمان جواب باید عددی بین ۱ تا ۷۲۰ ساعت باشد";
            return;
        }

        // مقادیر ساخت‌یافته — دوباره پارس (اعتبارسنجی موفق بوده)
        TryParse(MinMale, out var minM);
        TryParse(MaxMale, out var maxM);
        TryParse(MinFemale, out var minF);
        TryParse(MaxFemale, out var maxF);
        TryParse(MinChild, out var minC);
        TryParse(MaxChild, out var maxC);
        TryParse(ChildAgeText, out var childAge);
        TryParse(CriticalLow, out var critLow);
        TryParse(CriticalHigh, out var critHigh);

        IsBusy = true;
        var body = new
        {
            unit = string.IsNullOrWhiteSpace(Unit) ? null : Unit.Trim(),
            price = price.Value,
            turnAroundTime = (int?)tat,
            normalRangeMale = string.IsNullOrWhiteSpace(NormalRangeMale) ? null : NormalRangeMale.Trim(),
            normalRangeFemale = string.IsNullOrWhiteSpace(NormalRangeFemale) ? null : NormalRangeFemale.Trim(),
            refMinAdultMale = minM,
            refMaxAdultMale = maxM,
            refMinAdultFemale = minF,
            refMaxAdultFemale = maxF,
            refMinChild = minC,
            refMaxChild = maxC,
            childAgeMaxYears = (int?)childAge,
            criticalLow = critLow,
            criticalHigh = critHigh
        };

        var ok = await _api.PutAsync($"lab/tests/{_testId}", body);
        IsBusy = false;

        if (ok)
        {
            WasSaved = true;
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
            ErrorMessage = _api.LastError ?? "ذخیرهٔ تغییرات ناموفق بود";
    }

    /// <summary>انصراف — بدون ذخیره</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
