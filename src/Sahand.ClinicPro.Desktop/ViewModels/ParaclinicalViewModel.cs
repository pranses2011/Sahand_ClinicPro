using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>دندان چارت FDI</summary>
public partial class ToothVM : ObservableObject
{
    public int Number { get; init; }
    public string Quadrant => Number / 10 == 1 || Number / 10 == 2 ? "بالا" : "پایین";
    public string ToolTip => $"دندان {Number} — فک {Quadrant} — {ConditionText}";
    [ObservableProperty] private int _condition; // 0 سالم 1 پوسیدگی 2 ترمیم 3 درمان 4 روکش 5 کشیده 6 ایمپلنت

    // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع «رنگ‌های وضعیت برای فک
    // بالا و پایین درست نشان داده نمی‌شوند و همگی سفید می‌مانند» ═══
    // «Color» و «ToolTip» و «ConditionText» پراپرتی‌های محاسبه‌ای‌اند و با
    // تغییر Condition هیچ اعلانی برایشان صادر نمی‌شد → اتصال Background در
    // قالب دکمهٔ دندان فقط مقدار لحظهٔ بارگذاری (سالم = سفید) را می‌دید و
    // هرگز تازه نمی‌شد. اکنون تغییر Condition، اعلان تغییر همهٔ موارد
    // وابسته را هم صادر می‌کند تا رنگ دندان بی‌درنگ عوض شود.
    partial void OnConditionChanged(int value)
    {
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(ConditionText));
    }

    /// <summary>این دندان همان دندان انتخاب‌شدهٔ چارت است؟ — چرخه بیست‌وچهارم:
    /// مقدار با انتخاب کاربر توسط OnSelectedToothChanged به‌روز می‌شود تا
    /// قالب دکمه با DataTrigger ساده (مقدار ثابت True) هایلایتش کند</summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>شمارهٔ دندان با ارقام فارسی — «همهٔ اعداد بدون استثنا فارسی» (گزارش هفدهم)</summary>
    public string NumberFa => Helpers.Money.ToPersianDigits(Number.ToString());

    /// <summary>متن فارسی وضعیت — برای تولتیپ و نمایش</summary>
    public string ConditionText => Condition switch
    {
        1 => "پوسیدگی", 2 => "ترمیم‌شده", 3 => "درمان‌شده", 4 => "روکش", 5 => "کشیده‌شده", 6 => "ایمپلنت",
        _ => "سالم"
    };

    /// <summary>
    /// رنگ وضعیت — عیناً همان رنگ چیپ‌های راهنمای چارت در نمای پاراکلینیک:
    /// سالم #FFFFFF / پوسیدگی #E53935 / ترمیم #FDD835 / درمان #43A047 /
    /// روکش #1E88E5 / کشیده #37474F / ایمپلنت #8E24AA (چرخهٔ ۳۳: راهنمای
    /// نما به‌جای ایموجیِ رندرنشده، چیپ رنگی واقعی با همین کدهاست).
    /// با تغییر Condition اعلان تازه‌شدن هم صادر می‌شود (بالا) تا اتصال
    /// پس‌زمینهٔ دکمه بی‌درنگ رنگ را بگیرد
    /// </summary>
    public string Color => Condition switch
    {
        1 => "#E53935", 2 => "#FDD835", 3 => "#43A047", 4 => "#1E88E5", 5 => "#37474F", 6 => "#8E24AA",
        _ => "#FFFFFF"
    };
}

/// <summary>ردیف طرح درمان دندان</summary>
public record DentalPlanRow(int Tooth, string Procedure, decimal Price, string StatusText, string StatusColor);

/// <summary>
/// ViewModel پاراکلینیک — گام U12 طبق فرم‌های ۲۰ تا ۲۳:
/// ۴ تب: دندانپزشکی چارت FDI + چشم OD/OS + فیزیوتراپی + تصویربرداری
/// </summary>
public partial class ParaclinicalViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان</summary>
    public override string Title => "پاراکلینیک تخصصی";

    /* ── تب دندانپزشکی ── */
    public ObservableCollection<ToothVM> UpperTeeth { get; } = new();
    public ObservableCollection<ToothVM> LowerTeeth { get; } = new();
    [ObservableProperty] private ToothVM? _selectedTooth;
    [ObservableProperty] private int _toothConditionIndex;
    [ObservableProperty] private string? _dentalNotes;
    public ObservableCollection<DentalPlanRow> DentalPlan { get; } = new();

    /// <summary>برچسب وضعیت دندان</summary>
    public string[] ToothConditions { get; } = { "سالم", "پوسیدگی", "ترمیم‌شده", "درمان‌شده", "روکش", "کشیده‌شده", "ایمپلنت" };

    /* ── تب چشم ── */
    [ObservableProperty] private string? _odSphere;
    [ObservableProperty] private string? _odCylinder;
    [ObservableProperty] private string? _odAxis;
    [ObservableProperty] private string? _odAdd;
    [ObservableProperty] private int _odVisionIndex = 2;
    [ObservableProperty] private string? _odIop;
    [ObservableProperty] private string? _osSphere;
    [ObservableProperty] private string? _osCylinder;
    [ObservableProperty] private string? _osAxis;
    [ObservableProperty] private string? _osAdd;
    [ObservableProperty] private int _osVisionIndex = 2;
    [ObservableProperty] private string? _osIop;
    [ObservableProperty] private string? _pd;
    [ObservableProperty] private int _colorVisionIndex;
    [ObservableProperty] private string? _eyeFindings;

    /// <summary>هشدار گلوکوم چشم راست</summary>
    public string? OdIopWarning => int.TryParse(OdIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;
    /// <summary>هشدار گلوکوم چشم چپ</summary>
    public string? OsIopWarning => int.TryParse(OsIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    partial void OnOdIopChanged(string? value) => OnPropertyChanged(nameof(OdIopWarning));
    partial void OnOsIopChanged(string? value) => OnPropertyChanged(nameof(OsIopWarning));

    /// <summary>گزینه‌های دید</summary>
    public string[] VisionOptions { get; } =
        { "20/10", "20/15", "20/20", "20/25", "20/30", "20/40", "20/50", "20/60", "20/80", "20/100", "20/200", "CF", "HM", "LP", "NLP" };

    /// <summary>گزینه‌های دید رنگی</summary>
    public string[] ColorVisionOptions { get; } = { "طبیعی", "کاهش نسبی", "کانونی", "آتروفی عصبی" };

    /* ── تب دندانپزشکی — بیمار و ماندگاری چارت (چرخهٔ ۲۸) ── */
    [ObservableProperty] private string _dentalPatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _dentalPatient;

    /// <summary>اطلاعات آخرین ذخیرهٔ چارت — «آخرین ذخیره: ۱۴۰۴/۰۷/۱۲» یا راهنما</summary>
    [ObservableProperty]
    private string _dentalChartInfo = "بیمار را جستجو کنید تا چارت ذخیره‌شدهٔ او بارگذاری شود";

    /* ── تب چشم — بیمار و سابقه (چرخهٔ ۲۸) ── */
    [ObservableProperty] private string _eyePatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _eyePatient;

    /// <summary>سابقهٔ معاینه‌های چشم بیمار منتخب — ۲۰ مورد آخر</summary>
    public ObservableCollection<EyeExamRow> EyeHistory { get; } = new();

    /// <summary>ردیف سابقهٔ معاینهٔ چشم — نمایش خوانا در فهرست</summary>
    public record EyeExamRow(string DateShamsi, string OdSummary, string OsSummary, string? Findings);

    /* ── تب فیزیوتراپی ── */
    public ObservableCollection<string> Techniques { get; } = new()
        { "الکتروتراپی (TENS)", "اولتراسوند", "لیزر درمانی", "تمرینات درمانی", "ماساژ", "هیدروتراپی", "طب سوزنی" };
    [ObservableProperty] private string? _physioOtherTechnique;
    [ObservableProperty] private int _painScore;
    [ObservableProperty] private string? _physioNotes;
    [ObservableProperty] private string _physioPatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _physioPatient;
    public ObservableCollection<string> SelectedTechniques { get; } = new();

    /// <summary>رنگ نمره درد</summary>
    public string PainColor => PainScore <= 3 ? "#43A047" : PainScore <= 6 ? "#FDD835" : "#E53935";

    partial void OnPainScoreChanged(int value) => OnPropertyChanged(nameof(PainColor));

    /* ── تب تصویربرداری ── */
    public ObservableCollection<string> ImagingTypes { get; } =
        new() { "سونوگرافی", "رادیوگرافی", "MRI", "CT Scan", "ماموگرافی", "آنژیوگرافی" };
    [ObservableProperty] private int _imagingTypeIndex;
    [ObservableProperty] private string? _imagingRegion;
    [ObservableProperty] private string? _imagingClinicalInfo;
    [ObservableProperty] private bool _imagingUrgent;
    [ObservableProperty] private string _imagingPatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _imagingPatient;

    /// <summary>ناحیه‌های بدن</summary>
    public string[] BodyRegions { get; } =
        { "سر و گردن", "قفسه سینه", "شکم و لگن", "ستون فقرات", "اندام فوقانی", "اندام تحتانی", "مفصل شانه", "مفصل زانو", "تروماتیک" };

    /// <summary>سازنده</summary>
    public ParaclinicalViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        // چارت FDI — فک بالا ۱۱-۱۸ و ۲۱-۲۸ / فک پایین ۳۱-۳۸ و ۴۱-۴۸
        foreach (var n in new[] { 18, 17, 16, 15, 14, 13, 12, 11 })
            UpperTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 21, 22, 23, 24, 25, 26, 27, 28 })
            UpperTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 48, 47, 46, 45, 44, 43, 42, 41 })
            LowerTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 31, 32, 33, 34, 35, 36, 37, 38 })
            LowerTeeth.Add(new ToothVM { Number = n });
    }

    /// <summary>انتخاب دندان از چارت</summary>
    [RelayCommand]
    private void SelectTooth(ToothVM tooth)
    {
        SelectedTooth = tooth;
        ToothConditionIndex = tooth.Condition;
    }

    /// <summary>همگام‌سازی پرچم IsSelected دندان‌ها با انتخاب فعلی — چرخه
    /// بیست‌وچهارم (گزارش هفدهم مالک): هایلایت دندان انتخابی پیش‌تر با
    /// «Value={Binding …}» روی DataTrigger پیاده شده بود؛ Value در
    /// DataTrigger وابستگی DependencyProperty نیست و Binding روی آن
    /// XamlParseException می‌اندازد و کل تب تصویربرداری/دندانپزشکی را
    /// می‌بست. اکنون فقط این پرچم ساده به‌روز می‌شود و قالب با مقدار ثابت
    /// True آن را می‌خواند — بی‌هیچ استثنایی.</summary>
    partial void OnSelectedToothChanged(ToothVM? oldValue, ToothVM? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    /// <summary>اعمال وضعیت روی دندان انتخابی</summary>
    partial void OnToothConditionIndexChanged(int value)
    {
        if (SelectedTooth is not null)
            SelectedTooth.Condition = value;
    }

    /// <summary>افزودن به طرح درمان</summary>
    [RelayCommand]
    private void AddDentalPlan()
    {
        if (SelectedTooth is null) { _snackbar.Error("ابتدا دندان را از چارت انتخاب کنید"); return; }
        var procedures = new[] { "معاینه", "ترمیم", "عصب‌کشی", "روکش", "کشیدن ساده", "کشیدن جراحی" };
        var price = SelectedTooth.Condition switch
        {
            1 => 1_500_000m, 3 => 5_000_000m, 4 => 8_000_000m, 5 => 2_000_000m, _ => 1_000_000m
        };
        DentalPlan.Add(new DentalPlanRow(SelectedTooth.Number,
            procedures[Math.Clamp(SelectedTooth.Condition, 0, 5)],
            price, "پیشنهاد", "#FFF3E0"));
    }

    /// <summary>
    /// جستجوی بیمار تب دندانپزشکی — چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک):
    /// «در دندانپزشکی نمی‌شود بیمار را انتخاب کرد و مشخص نیست اطلاعات برای
    /// چه کسی ذخیره می‌شود». با انتخاب بیمار، آخرین چارت ذخیره‌شدهٔ او
    /// خودکار بارگذاری می‌شود تا ادامهٔ همان پرونده باشد.
    /// </summary>
    [RelayCommand]
    private async Task SearchDentalPatientAsync()
        => await SearchPatientGenericAsync(t =>
        {
            DentalPatient = t;
            _ = LoadDentalChartAsync();
        }, DentalPatientSearch);

    /// <summary>
    /// بازیابی آخرین چارت دندان و طرح درمان بیمار — GET dental/chart
    /// </summary>
    private async Task LoadDentalChartAsync()
    {
        DentalPlan.Clear();
        if (DentalPatient is null) return;
        var response = await _api.GetAsync<DentalChartResponse>(
            $"dental/chart?patientId={DentalPatient.Id}");
        var chart = response?.Data;
        if (chart is null)
        {
            DentalChartInfo = "بازیابی چارت ناموفق بود — می‌توانید چارت تازه ذخیره کنید";
            return;
        }

        // اعمال وضعیت دندان‌ها از نقشهٔ ذخیره‌شده — هر ۳۲ دندان
        foreach (var tooth in UpperTeeth.Concat(LowerTeeth))
            if (chart.Teeth is not null && chart.Teeth.TryGetValue(tooth.Number.ToString(), out var cond))
                tooth.Condition = cond;

        // ردیف‌های طرح درمان ذخیره‌شده
        if (chart.Rows is not null)
            foreach (var r in chart.Rows)
                DentalPlan.Add(new DentalPlanRow(r.Tooth, r.Procedure ?? "—", r.Price,
                    r.Status ?? "پیشنهاد", "#FFF3E0"));

        DentalChartInfo = string.IsNullOrWhiteSpace(chart.CreatedDateShamsi)
            ? "چارتی برای این بیمار ثبت نشده — نخستین ثبت"
            : $"آخرین ذخیرهٔ چارت: {chart.CreatedDateShamsi}";
    }

    /// <summary>
    /// ذخیرهٔ چارت کامل + طرح درمان برای بیمار منتخب — POST dental/plans؛
    /// وضعیت هر ۳۲ دندان و ردیف‌های طرح به‌صورت JSON در PlanDetails طرح
    /// درمان ذخیره می‌شود و با هر بار بازیابی، چارت همان وضعیت را برمی‌گرداند.
    /// </summary>
    [RelayCommand]
    private async Task SaveDentalChartAsync()
    {
        if (DentalPatient is null)
        { _snackbar.Error("ابتدا بیمار را جستجو کنید — چارت باید به پروندهٔ یک بیمار وصل باشد"); return; }

        // نقشهٔ وضعیت همهٔ دندان‌ها — اعداد کلیدی؛ وضعیت صفر هم ذخیره می‌شود تا
        // بازیابی، تصویر کامل چارت (و نه فقط تغییرات) را بازسازی کند
        var teeth = UpperTeeth.Concat(LowerTeeth)
            .ToDictionary(t => t.Number.ToString(), t => t.Condition);

        // ردیف‌های طرح درمان — متن/قیمت/وضعیت
        var rows = DentalPlan.Select(r => new { tooth = r.Tooth, procedure = r.Procedure, price = r.Price, status = r.StatusText })
            .ToList();

        var planDetails = System.Text.Json.JsonSerializer.Serialize(new { teeth, rows });
        var ok = await _api.PostAsync("dental/plans", new
        {
            patientId = DentalPatient.Id,
            dentistId = (long?)null,
            totalCost = DentalPlan.Sum(r => r.Price),
            estimatedSessions = DentalPlan.Count,
            planDetails
        });
        if (ok)
        {
            _snackbar.Success($"چارت و طرح درمان برای «{DentalPatient.FullName}» ذخیره شد");
            DentalChartInfo = $"آخرین ذخیرهٔ چارت: {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(System.DateTime.Now)}";
        }
        else _snackbar.Error(_api.LastError ?? "ذخیرهٔ چارت ناموفق بود");
    }

    /// <summary>پاک‌کردن بیمار دندانپزشکی — چارت تازه برای بیمار بعدی</summary>
    [RelayCommand]
    private void ClearDentalPatient()
    {
        DentalPatient = null;
        DentalPatientSearch = string.Empty;
        DentalPlan.Clear();
        foreach (var t in UpperTeeth.Concat(LowerTeeth)) t.Condition = 0;
        SelectedTooth = null;
        DentalChartInfo = "بیمار را جستجو کنید تا چارت ذخیره‌شدهٔ او بارگذاری شود";
    }

    /// <summary>جستجوی بیمار فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SearchPhysioPatientAsync() => await SearchPatientGenericAsync(t => PhysioPatient = t, PhysioPatientSearch);

    /// <summary>جستجوی بیمار تصویربرداری</summary>
    [RelayCommand]
    private async Task SearchImagingPatientAsync() => await SearchPatientGenericAsync(t => ImagingPatient = t, ImagingPatientSearch);

    /// <summary>جستجوی مشترک بیمار</summary>
    private async Task SearchPatientGenericAsync(Action<QuickPatient?> assign, string term)
    {
        if (term.Trim().Length < 4) { _snackbar.Error("کد ملی یا موبایل را وارد کنید"); return; }
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term.Trim())}");
        var p = response?.Data?.FirstOrDefault();
        if (p is null) { _snackbar.Error("بیمار یافت نشد"); return; }
        assign(new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile, p.InsuranceName, p.Allergies, null));
        _snackbar.Success($"بیمار: {p.FirstName} {p.LastName}");
    }

    /// <summary>ذخیره جلسه فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SavePhysioSessionAsync()
    {
        if (PhysioPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        var ok = await _api.PostAsync("physiotherapy/sessions", new
        {
            patientId = PhysioPatient.Id,
            sessionNumber = 1,
            sessionDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(System.DateTime.Now),
            techniques = string.Join("، ", SelectedTechniques.Concat(string.IsNullOrWhiteSpace(PhysioOtherTechnique) ? System.Linq.Enumerable.Empty<string>() : new[] { PhysioOtherTechnique.Trim() })),
            painScore = PainScore,
            notes = PhysioNotes
        });
        if (ok)
        {
            _snackbar.Success("جلسه فیزیوتراپی ثبت شد");
            PhysioNotes = null;
            PhysioOtherTechnique = null;
            PainScore = 0;
        }
        else _snackbar.Error(_api.LastError ?? "ثبت جلسه ناموفق بود");
    }

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [RelayCommand]
    private async Task SaveImagingOrderAsync()
    {
        if (ImagingPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        var ok = await _api.PostAsync("imaging/orders", new
        {
            patientId = ImagingPatient.Id,
            modalityType = ImagingTypeIndex + 1,
            region = BodyRegions[Math.Clamp(System.Array.IndexOf(BodyRegions, ImagingRegion ?? "سر و گردن"), 0, 8)],
            clinicalInformation = ImagingClinicalInfo,
            isUrgent = ImagingUrgent
        });
        if (ok)
        {
            _snackbar.Success("درخواست تصویربرداری ثبت شد");
            ImagingClinicalInfo = null;
            ImagingUrgent = false;
        }
        else _snackbar.Error(_api.LastError ?? "ثبت درخواست ناموفق بود");
    }

    /// <summary>
    /// جستجوی بیمار تب چشم — چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک): پیش‌تر
    /// فرم چشم بیمارِ خودش را نداشت و باید بیمار در تب فیزیوتراپی/تصویربرداری
    /// جستجو می‌شد! اکنون تب چشم بیمار مستقل خودش را دارد و با انتخاب،
    /// آخرین معاینه + سابقه در فرم بارگذاری می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task SearchEyePatientAsync()
        => await SearchPatientGenericAsync(t =>
        {
            EyePatient = t;
            _ = LoadEyeHistoryAsync();
        }, EyePatientSearch);

    /// <summary>بازیابی آخرین معاینهٔ چشم بیمار + سابقه — GET eye/exams</summary>
    private async Task LoadEyeHistoryAsync()
    {
        EyeHistory.Clear();
        if (EyePatient is null) return;
        var response = await _api.GetAsync<EyeExamsResponse>($"eye/exams?patientId={EyePatient.Id}");
        var exams = response?.Data;
        if (exams is null || exams.Count == 0) return;

        // آخرین معاینه → پر کردن فرم برای ادامهٔ پرونده
        var last = exams[0];
        OdSphere = last.Od?.Sphere?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OdCylinder = last.Od?.Cylinder?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OdAxis = last.Od?.Axis?.ToString();
        OsSphere = last.Os?.Sphere?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OsCylinder = last.Os?.Cylinder?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        OsAxis = last.Os?.Axis?.ToString();
        OdIop = last.Od?.Iop?.ToString();
        OsIop = last.Os?.Iop?.ToString();
        Pd = last.Pd?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        EyeFindings = last.Findings;
        if (last.Od?.Acuity is { } odAcuity && VisionOptions.Contains(odAcuity))
            OdVisionIndex = Array.IndexOf(VisionOptions, odAcuity);
        if (last.Os?.Acuity is { } osAcuity && VisionOptions.Contains(osAcuity))
            OsVisionIndex = Array.IndexOf(VisionOptions, osAcuity);
        if (last.ColorVision is { } cv && ColorVisionOptions.Contains(cv))
            ColorVisionIndex = Array.IndexOf(ColorVisionOptions, cv);

        // سابقه — نمایش خوانا برای مرور روند درمان
        foreach (var e in exams)
            EyeHistory.Add(new EyeExamRow(
                e.DateShamsi ?? "—",
                e.Od is null ? "—" : $"S {e.Od.Sphere} / C {e.Od.Cylinder} / IOP {e.Od.Iop?.ToString() ?? "—"}",
                e.Os is null ? "—" : $"S {e.Os.Sphere} / C {e.Os.Cylinder} / IOP {e.Os.Iop?.ToString() ?? "—"}",
                e.Findings));
    }

    /// <summary>پاک‌کردن بیمار چشم و سابقه — بیمار بعدی</summary>
    [RelayCommand]
    private void ClearEyePatient()
    {
        EyePatient = null;
        EyePatientSearch = string.Empty;
        EyeHistory.Clear();
    }

    /// <summary>
    /// ذخیرهٔ معاینه چشم — چرخهٔ بیست‌وهشتم: بیمار تب چشم (یا به‌عنوان
    /// جایگزین، بیمار تب‌های دیگر — سازگاری با رفتار پیشین) + ارسال همهٔ
    /// اندازه‌ها شامل IOP و PD و حدت بینایی و دید رنگی که پیش‌تر در binding
    /// سرور دور ریخته می‌شدند.
    /// </summary>
    [RelayCommand]
    private async Task SaveEyeExamAsync()
    {
        var patient = EyePatient ?? PhysioPatient ?? ImagingPatient;
        if (patient is null)
        { _snackbar.Error("ابتدا بیمار را در همین تب جستجو کنید"); return; }

        // پارس امن IOP و PD — ورودی خالی = تهی (ثبت نمی‌شود)
        int? TryInt(string? s) => int.TryParse(s?.Trim(), out var v) ? v : null;
        decimal? TryDec(string? s) => decimal.TryParse(s?.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

        var ok = await _api.PostAsync("eye/exams", new
        {
            patientId = patient.Id,
            odSphere = TryDec(OdSphere), odCylinder = TryDec(OdCylinder), odAxis = TryInt(OdAxis),
            osSphere = TryDec(OsSphere), osCylinder = TryDec(OsCylinder), osAxis = TryInt(OsAxis),
            rightIop = TryInt(OdIop), leftIop = TryInt(OsIop),
            rightVisualAcuity = VisionOptions.ElementAtOrDefault(OdVisionIndex),
            leftVisualAcuity = VisionOptions.ElementAtOrDefault(OsVisionIndex),
            pupilDistance = TryDec(Pd),
            colorVision = ColorVisionOptions.ElementAtOrDefault(ColorVisionIndex),
            findings = EyeFindings,
            // تجویز عینک — از مقادیر Add (نزدیک‌بین) ساخته می‌شود
            eyewearPrescription = (string.IsNullOrWhiteSpace(OdAdd) && string.IsNullOrWhiteSpace(OsAdd))
                ? null
                : $"Add OD {OdAdd} / OS {OsAdd} — PD {Pd}"
        });
        if (ok) _snackbar.Success($"معاینهٔ چشم برای «{patient.FullName}» ثبت شد");
        else _snackbar.Error(_api.LastError ?? "ثبت معاینه ناموفق بود");
    }

    /* ── بخش فایل‌های DICOM — گام R4 ── */

    /// <summary>ردیف درخواست تصویر امروز</summary>
    public sealed class ImagingOrderRow
    {
        /// <summary>سازنده</summary>
        public ImagingOrderRow(long id, string patientName, string modality, string? bodyPart)
        {
            Id = id; PatientName = patientName; Modality = modality; BodyPart = bodyPart;
        }

        /// <summary>شناسه درخواست</summary>
        public long Id { get; }

        /// <summary>عنوان نمایشی</summary>
        public string Display => $"#{Id} — {PatientName} ({Modality})";

        /// <summary>نام بیمار</summary>
        public string PatientName { get; }

        /// <summary>مودالیته</summary>
        public string Modality { get; }

        /// <summary>ناحیه بدن</summary>
        public string? BodyPart { get; }
    }

    /// <summary>ردیف فایل تصویر</summary>
    public sealed class ImagingFileRow
    {
        /// <summary>سازنده</summary>
        public ImagingFileRow(long id, string? fileName, bool isDicom, long? fileSize, DateTime? uploadedAt)
        {
            Id = id; FileName = fileName ?? $"فایل {id}"; IsDicom = isDicom;
            FileSize = fileSize ?? 0; UploadedAt = uploadedAt;
        }

        /// <summary>شناسه</summary>
        public long Id { get; }

        /// <summary>نام فایل</summary>
        public string FileName { get; }

        /// <summary>DICOM است؟</summary>
        public bool IsDicom { get; }

        /// <summary>حجم بایت</summary>
        public long FileSize { get; }

        /// <summary>حجم خوانا</summary>
        public string SizeText => FileSize >= 1_048_576
            ? $"{FileSize / 1_048_576.0:0.#} MB" : $"{FileSize / 1024.0:0.#} KB";

        /// <summary>نوع با برچسب</summary>
        public string KindText => IsDicom ? "DICOM" : "عادی";

        /// <summary>زمان آپلود</summary>
        public DateTime? UploadedAt { get; }
    }

    /// <summary>درخواست‌های تصویر امروز — برای اتصال فایل</summary>
    [ObservableProperty]
    private ObservableCollection<ImagingOrderRow> _imagingOrders = new();

    /// <summary>درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedImagingOrder))]
    private ImagingOrderRow? _selectedImagingOrder;

    /// <summary>آیا درخواستی انتخاب شده</summary>
    public bool HasSelectedImagingOrder => SelectedImagingOrder is not null;

    /// <summary>فایل‌های درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<ImagingFileRow> _imagingFiles = new();

    /// <summary>فایل انتخاب‌شده</summary>
    [ObservableProperty]
    private ImagingFileRow? _selectedImagingFile;

    /// <summary>بارگذاری فهرست درخواست‌های تصویر امروز</summary>
    [RelayCommand]
    private async Task LoadImagingOrdersAsync()
    {
        var response = await _api.GetAsync<ImagingOrdersResponse>("imaging/orders");
        ImagingOrders = new ObservableCollection<ImagingOrderRow>((response?.Data ?? new List<ImagingOrderDto>())
            .Select(o => new ImagingOrderRow(o.Id, o.PatientName, o.Modality, o.BodyPart)));
        if (ImagingOrders.Count == 0)
            _snackbar.Info("درخواست تصویربرداری برای امروز ثبت نشده است");
    }

    /// <summary>بازخورد انتخاب درخواست — فهرست فایل‌های آن</summary>
    partial void OnSelectedImagingOrderChanged(ImagingOrderRow? value)
    {
        if (value is null) return;
        _ = LoadImagingFilesAsync();
    }

    /// <summary>بارگذاری فایل‌های درخواست انتخاب‌شده</summary>
    private async Task LoadImagingFilesAsync()
    {
        if (SelectedImagingOrder is null) return;
        var response = await _api.GetAsync<ImagingFilesResponse>(
            $"imaging/files?orderId={SelectedImagingOrder.Id}");
        ImagingFiles = new ObservableCollection<ImagingFileRow>((response?.Data ?? new List<ImagingFileDto>())
            .Select(f => new ImagingFileRow(f.Id, f.FileName, f.IsDicom, f.FileSize, f.UploadedAt)));
    }

    /// <summary>آپلود فایل تصویر برای درخواست انتخاب‌شده — انتخاب از دیسک</summary>
    [RelayCommand]
    private async Task UploadImagingFileAsync()
    {
        if (SelectedImagingOrder is null)
        {
            _snackbar.Error("ابتدا یک درخواست تصویربرداری از فهرست انتخاب کنید");
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "انتخاب فایل تصویر یا DICOM",
            Filter = "فایل‌های تصویری (*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp)|*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp|همه فایل‌ها (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var bytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
        var uploadResponse = await _api.PostFileAsync<UploadAck>("imaging/files",
            System.IO.Path.GetFileName(dialog.FileName), bytes,
            new Dictionary<string, string> { ["orderId"] = SelectedImagingOrder.Id.ToString() });

        if (uploadResponse?.IsSuccess == true)
        {
            _snackbar.Success(uploadResponse.Message ?? "فایل بارگذاری شد");
            await LoadImagingFilesAsync();
        }
        else
        {
            _snackbar.Error(_api.LastError ?? "بارگذاری ناموفق بود");
        }
    }

    /// <summary>گشودن فایل انتخاب‌شده در نمایشگر DICOM</summary>
    [RelayCommand]
    private async Task OpenDicomViewerAsync()
    {
        if (SelectedImagingFile is null)
        {
            _snackbar.Error("ابتدا یک فایل از فهرست انتخاب کنید");
            return;
        }

        var bytes = await _api.DownloadFileAsync($"imaging/files/{SelectedImagingFile.Id}/content");
        if (bytes is null || bytes.Length == 0)
        {
            _snackbar.Error(_api.LastError ?? "دریافت فایل ناموفق بود");
            return;
        }

        // ذخیره در پوشه موقت و گشودن در نمایشگر
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"sahand-dicom-{SelectedImagingFile.Id}.dcm");
        await System.IO.File.WriteAllBytesAsync(tempPath, bytes);

        var viewer = new Views.DicomViewerWindow(tempPath)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        viewer.Show();
    }

    /// <summary>حذف فایل انتخاب‌شده</summary>
    [RelayCommand]
    private async Task DeleteImagingFileAsync(ImagingFileRow? row)
    {
        if (row is null) return;
        if (await _api.DeleteAsync($"imaging/files/{row.Id}"))
        {
            _snackbar.Success("فایل حذف شد");
            await LoadImagingFilesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    // ── مدل‌های پاسخ DICOM/فایل ──

    private sealed class ImagingOrdersResponse
    {
        public List<ImagingOrderDto>? Data { get; set; }
    }

    private sealed class ImagingOrderDto
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string? BodyPart { get; set; }
    }

    private sealed class ImagingFilesResponse
    {
        public List<ImagingFileDto>? Data { get; set; }
    }

    private sealed class ImagingFileDto
    {
        public long Id { get; set; }
        public string? FileName { get; set; }
        public bool IsDicom { get; set; }
        public long? FileSize { get; set; }
        public DateTime? UploadedAt { get; set; }
    }

    private sealed class UploadAck
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public UploadAckData? Data { get; set; }
    }

    private sealed class UploadAckData
    {
        public long FileId { get; set; }
        public bool IsDicom { get; set; }
    }

    // ── مدل‌های پاسخ چارت دندان و معاینهٔ چشم — چرخهٔ بیست‌وهشتم ──

    /// <summary>پاسخ GET dental/chart — وضعیت دندان‌ها + ردیف‌های طرح</summary>
    private sealed class DentalChartResponse
    {
        public DentalChartData? Data { get; set; }
    }

    private sealed class DentalChartData
    {
        public long? PlanId { get; set; }
        public string? CreatedDateShamsi { get; set; }
        /// <summary>نقشهٔ شمارهٔ دندان → وضعیت (۰ سالم تا ۶ ایمپلنت)</summary>
        public Dictionary<string, int>? Teeth { get; set; }
        public List<DentalPlanRowDto>? Rows { get; set; }
        public string? Note { get; set; }
    }

    private sealed class DentalPlanRowDto
    {
        public int Tooth { get; set; }
        public string? Procedure { get; set; }
        public decimal Price { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>پاسخ GET eye/exams — سابقهٔ معاینه‌های چشم بیمار</summary>
    private sealed class EyeExamsResponse
    {
        public List<EyeExamDto>? Data { get; set; }
    }

    private sealed class EyeExamDto
    {
        public long Id { get; set; }
        public string? DateShamsi { get; set; }
        public long? VisitId { get; set; }
        public EyeSideDto? Od { get; set; }
        public EyeSideDto? Os { get; set; }
        public decimal? Pd { get; set; }
        public string? ColorVision { get; set; }
        public string? Findings { get; set; }
        public string? EyewearPrescription { get; set; }
    }

    /// <summary>اندازه‌های یک چشم (OD راست / OS چپ)</summary>
    private sealed class EyeSideDto
    {
        public decimal? Sphere { get; set; }
        public decimal? Cylinder { get; set; }
        public int? Axis { get; set; }
        public string? Acuity { get; set; }
        public int? Iop { get; set; }
    }
}
