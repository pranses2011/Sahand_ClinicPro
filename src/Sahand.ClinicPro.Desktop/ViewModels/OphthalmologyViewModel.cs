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

    /// <summary>هشدار گلوکوم چشم راست</summary>
    public string? OdIopWarning => int.TryParse(OdIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    /// <summary>هشدار گلوکوم چشم چپ</summary>
    public string? OsIopWarning => int.TryParse(OsIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    partial void OnOdIopChanged(string? value) => OnPropertyChanged(nameof(OdIopWarning));
    partial void OnOsIopChanged(string? value) => OnPropertyChanged(nameof(OsIopWarning));

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

        var ok = await _api.PostAsync("eye/exams", new
        {
            patientId = EyePatient.Id,
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
        if (ok) _snackbar.Success($"معاینهٔ چشم برای «{EyePatient.FullName}» ثبت شد");
        else _snackbar.Error(_api.LastError ?? "ثبت معاینه ناموفق بود");
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
