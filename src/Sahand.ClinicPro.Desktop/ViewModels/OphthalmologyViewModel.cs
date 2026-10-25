using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف سابقهٔ معاینهٔ چشم — نمایش خوانا در فهرست</summary>
public record EyeExamRow(string DateShamsi, string OdSummary, string OsSummary, string? Findings);

/// <summary>
/// ViewModel صفحهٔ مستقل «چشم‌پزشکی» — چرخهٔ چهل‌وسوم (گزارش مالک)
/// </summary>
/// <remarks>
/// «برای هر کدوم یک صفحه مستقل طراحی بکن تا با همدیگه تداخلی نداشته باشن» —
/// معاینه OD/OS، سابقه و پروندهٔ بیمار این تب از پاراکلینیکِ تب‌دار جدا و به
/// صفحهٔ اختصاصی منتقل شد؛ نام پراپرتی‌ها عیناً حفظ شد تا قالب XAML بدون
/// تغییر بایند شود. جستجوی بیمار از هلپر مشترک PatientQuickSearch.
/// </remarks>
public partial class OphthalmologyViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "چشم‌پزشکی";

    /* ── اندازه‌های چشم راست (OD) ── */
    [ObservableProperty] private string? _odSphere;
    [ObservableProperty] private string? _odCylinder;
    [ObservableProperty] private string? _odAxis;
    [ObservableProperty] private string? _odAdd;
    [ObservableProperty] private int _odVisionIndex = 2;
    [ObservableProperty] private string? _odIop;

    /* ── اندازه‌های چشم چپ (OS) ── */
    [ObservableProperty] private string? _osSphere;
    [ObservableProperty] private string? _osCylinder;
    [ObservableProperty] private string? _osAxis;
    [ObservableProperty] private string? _osAdd;
    [ObservableProperty] private int _osVisionIndex = 2;
    [ObservableProperty] private string? _osIop;

    /// <summary>فاصلهٔ مردمک‌چشمی (PD)</summary>
    [ObservableProperty] private string? _pd;

    /// <summary>نمایهٔ دید رنگی</summary>
    [ObservableProperty] private int _colorVisionIndex;

    /// <summary>یافته‌های بالینی</summary>
    [ObservableProperty] private string? _eyeFindings;

    /* ── ماژول حرفه‌ای چشم‌پزشکی (طرح تأییدشدهٔ نسخهٔ ۲) ── */

    /// <summary>معاینهٔ اسلیت‌لمپ و فوندوس چشم راست (ساخت‌یافته)</summary>
    [ObservableProperty] private string? _slitOd;
    /// <summary>معاینهٔ اسلیت‌لمپ و فوندوس چشم چپ (ساخت‌یافته)</summary>
    [ObservableProperty] private string? _slitOs;
    /// <summary>ضخامت قرنیه (پاچیمتری) راست — میکرون</summary>
    [ObservableProperty] private string? _pachymetryOd;
    /// <summary>ضخامت قرنیه (پاچیمتری) چپ — میکرون</summary>
    [ObservableProperty] private string? _pachymetryOs;
    /// <summary>میدان بینایی (پریمتری) — الگو و MD</summary>
    [ObservableProperty] private string? _visualFieldText;
    /// <summary>تست‌های تخصصی (آمادگی/نتیجهٔ آمسلر، شیرمر، کاور تست، استریوپسیس)</summary>
    [ObservableProperty] private string? _specialTestsText;
    /// <summary>تصویربرداری چشم (OCT، عکس فوندوس، بیومتری) پیوندخورده به این معاینه</summary>
    [ObservableProperty] private string? _imagingNote;
    /// <summary>تشخیص‌های این معاینه</summary>
    [ObservableProperty] private string? _diagnosisText;
    /// <summary>برنامهٔ درمان/جراحی/ارجاع</summary>
    [ObservableProperty] private string? _carePlanText;
    /// <summary>نسخهٔ دارویی چشمی</summary>
    [ObservableProperty] private string? _drugPrescriptionText;
    /// <summary>برنامهٔ پیگیری و یادآوری</summary>
    [ObservableProperty] private string? _followUpText;

    /// <summary>نسخهٔ عینک زنده — از عددهای رفرکشن و PD همان لحظه ساخته می‌شود</summary>
    public string GlassesPrescriptionText =>
        $"OD: SPH {OdSphere ?? "—"} · CYL {OdCylinder ?? "—"} · AXIS {OdAxis ?? "—"} · ADD {OdAdd ?? "—"}" + Environment.NewLine +
        $"OS: SPH {OsSphere ?? "—"} · CYL {OsCylinder ?? "—"} · AXIS {OsAxis ?? "—"} · ADD {OsAdd ?? "—"}" + Environment.NewLine +
        $"PD: {Pd ?? "—"} mm";

    /// <summary>پهنای گیج فشار چشم راست (از ۴۰ مقیاس، ۱۸۰ پیکسل)</summary>
    public double OdIopGaugeWidth => int.TryParse(OdIop, out var v) ? Math.Clamp(v, 0, 40) / 40d * 180 : 0;
    /// <summary>پهنای گیج فشار چشم چپ</summary>
    public double OsIopGaugeWidth => int.TryParse(OsIop, out var v) ? Math.Clamp(v, 0, 40) / 40d * 180 : 0;

    /// <summary>روند فشار و نمره در سابقه — مقایسهٔ تازه‌ترین با قبلی</summary>
    [ObservableProperty] private string _historyTrendText = string.Empty;

    partial void OnOdSphereChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOdCylinderChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOdAxisChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOdAddChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOsSphereChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOsCylinderChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOsAxisChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnOsAddChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));
    partial void OnPdChanged(string? value) => OnPropertyChanged(nameof(GlassesPrescriptionText));

    /// <summary>هشدار گلوکوم چشم راست</summary>
    public string? OdIopWarning => int.TryParse(OdIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    /// <summary>هشدار گلوکوم چشم چپ</summary>
    public string? OsIopWarning => int.TryParse(OsIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    partial void OnOdIopChanged(string? value) { OnPropertyChanged(nameof(OdIopWarning)); OnPropertyChanged(nameof(OdIopGaugeWidth)); }
    partial void OnOsIopChanged(string? value) { OnPropertyChanged(nameof(OsIopWarning)); OnPropertyChanged(nameof(OsIopGaugeWidth)); }

    /// <summary>گزینه‌های حدت بینایی</summary>
    public string[] VisionOptions { get; } =
        { "20/10", "20/15", "20/20", "20/25", "20/30", "20/40", "20/50", "20/60", "20/80", "20/100", "20/200", "CF", "HM", "LP", "NLP" };

    /// <summary>گزینه‌های دید رنگی</summary>
    public string[] ColorVisionOptions { get; } = { "طبیعی", "کاهش نسبی", "کانونی", "آتروفی عصبی" };

    /* ── بیمار و سابقه (چرخهٔ ۲۸) ── */

    /// <summary>عبارت جستجوی بیمار چشم</summary>
    [ObservableProperty] private string _eyePatientSearch = string.Empty;

    /// <summary>بیمار منتخب</summary>
    [ObservableProperty] private QuickPatient? _eyePatient;

    /// <summary>سابقهٔ معاینه‌های چشم بیمار منتخب — ۲۰ مورد آخر</summary>
    public ObservableCollection<EyeExamRow> EyeHistory { get; } = new();

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public OphthalmologyViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
    }

    /// <summary>
    /// جستجوی بیمار چشم — چرخهٔ ۲۸: با انتخاب بیمار، آخرین معاینه + سابقه
    /// خودکار بارگذاری می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task SearchEyePatientAsync()
    {
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, EyePatientSearch);
        if (patient is null) return;
        EyePatient = patient;
        _ = LoadEyeHistoryAsync();
    }

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

        // روند: مقایسهٔ تازه‌ترین معاینه با قبلی (فشار چشم و نمرهٔ عدسی راست)
        if (exams.Count >= 2)
        {
            var (a, b) = (exams[0], exams[1]);
            static string D(int? x, int? y) => x is null || y is null ? "—" : x > y ? $"📈 از {y} به {x}" : x < y ? $"📉 از {y} به {x}" : $"{x} (ثابت)";
            HistoryTrendText = $"روند IOP راست: {D(a.Od?.Iop, b.Od?.Iop)} · IOP چپ: {D(a.Os?.Iop, b.Os?.Iop)} · عدسی راست: {a.Od?.Sphere} (قبل: {b.Od?.Sphere})";
        }
        else HistoryTrendText = string.Empty;

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
    /// ذخیرهٔ معاینه چشم — چرخهٔ ۲۸: بیمار همین صفحه + ارسال همهٔ اندازه‌ها
    /// شامل IOP و PD و حدت بینایی و دید رنگی.
    /// </summary>
    [RelayCommand]
    private async Task SaveEyeExamAsync()
    {
        if (EyePatient is null)
        { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }

        // پارس امن IOP و PD — ورودی خالی = تهی (ثبت نمی‌شود)
        int? TryInt(string? s) => int.TryParse(s?.Trim(), out var v) ? v : null;
        decimal? TryDec(string? s) => decimal.TryParse(s?.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

        // ═══ چرخهٔ ۴۸ — سازگاری کامل با قرارداد سرور SaveEyeExamCommand ═══
        // نام‌های قبلی odSphere/osCylinder/… با هیچ ویژگی‌ای از DTO سرور
        // (rightSphere/leftCylinder/…) بایند نمی‌شدند ⇒ عدسی/استوانه/محور هر دو
        // چشم بی‌صدا دور ریخته می‌شد و رکورد معاینهٔ چشم «ثبت‌شده» بدون نسخهٔ
        // عینک ذخیره می‌شد (گزارش مالک: قابلیت‌ها پیاده نشده‌اند).
        var ok = await _api.PostAsync("eye/exams", new
        {
            patientId = EyePatient.Id,
            visitId = (long?)null,
            rightSphere = TryDec(OdSphere), rightCylinder = TryDec(OdCylinder), rightAxis = TryInt(OdAxis),
            leftSphere = TryDec(OsSphere), leftCylinder = TryDec(OsCylinder), leftAxis = TryInt(OsAxis),
            rightIop = TryInt(OdIop), leftIop = TryInt(OsIop),
            rightVisualAcuity = VisionOptions.ElementAtOrDefault(OdVisionIndex),
            leftVisualAcuity = VisionOptions.ElementAtOrDefault(OsVisionIndex),
            pupilDistance = TryDec(Pd),
            colorVision = ColorVisionOptions.ElementAtOrDefault(ColorVisionIndex),
            visualField = string.IsNullOrWhiteSpace(VisualFieldText) ? null : VisualFieldText.Trim(),
            findings = ComposeFindings(),
            // تجویز عینک — نسخهٔ زندهٔ ساخته‌شده از رفرکشن و PD
            eyewearPrescription = GlassesPrescriptionText
        });
        if (ok) _snackbar.Success($"معاینهٔ چشم برای «{EyePatient.FullName}» ثبت شد");
        else _snackbar.Error(_api.LastError ?? "ثبت معاینه ناموفق بود");
    }

    /// <summary>
    /// ترکیب یافته‌های بالینی با بخش‌های ساخت‌یافتهٔ ماژول حرفه‌ای — همه در
    /// همان ستون یافته‌های رکورد معاینه ذخیره می‌شوند تا بدون تغییر دیتابیس،
    /// در سابقهٔ بیمار هم خوانا بمانند.
    /// </summary>
    private string? ComposeFindings()
    {
        var parts = new List<string>();
        void Add(string title, string? text) { if (!string.IsNullOrWhiteSpace(text)) parts.Add($"{title}: {text.Trim()}"); }
        Add("یافته‌ها", EyeFindings);
        Add("اسلیت‌لمپ/فوندوس راست", SlitOd);
        Add("اسلیت‌لمپ/فوندوس چپ", SlitOs);
        if (!string.IsNullOrWhiteSpace(PachymetryOd) || !string.IsNullOrWhiteSpace(PachymetryOs))
            parts.Add($"پاچیمتری: راست {PachymetryOd ?? "—"} · چپ {PachymetryOs ?? "—"} میکرون");
        Add("تست‌های تخصصی", SpecialTestsText);
        Add("تصویربرداری چشم", ImagingNote);
        Add("تشخیص", DiagnosisText);
        Add("برنامهٔ درمان", CarePlanText);
        Add("نسخهٔ دارویی", DrugPrescriptionText);
        Add("پیگیری", FollowUpText);
        return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
    }

    // ── مدل‌های پاسخ معاینهٔ چشم — چرخهٔ ۲۸ ──

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
