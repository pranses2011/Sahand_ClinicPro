using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه تشخیص ICD-10 در نتایج جستجو</summary>
public record DiagnosisOption(long Id, string Code, string Title);

/// <summary>کارت علائم حیاتی برای نمایش وضعیت</summary>
public record VitalCard(
    string Title,
    string PersianTitle,
    string? Value,
    string Unit,
    string StatusText,
    string StatusColor);

/// <summary>
/// ViewModel فرم ویزیت — گام U6 طبق پرامپت UI بخش ۲ (فرم ۱۱):
/// سایدبار اطلاعات بیمار + ۸ تب (SOAP/حیاتی/معاینه/تشخیص/نسخه/آزمایش/تصویر/فایل) +
/// AutoSave ۶۰ ثانیه + Stopwatch + بیمار بعدی + پایان ویزیت Ctrl+Enter
/// </summary>
public partial class VisitEditViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly long? _visitId;
    private long _patientId;
    private long _doctorId;
    private long? _receptionId;
    private System.Windows.Threading.DispatcherTimer? _autoSave;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private bool _suppressAutoSave;

    /// <summary>عنوان پنجره — «🩺 ویزیت بیمار: نام [🔴 در حال ضبط]»</summary>
    [ObservableProperty]
    private string _formTitle = "ثبت ویزیت جدید";

    /// <summary>مدت ویزیت زنده — Stopwatch</summary>
    [ObservableProperty]
    private string _elapsedText = "۰۰:۰۰";

    /// <summary>هشدار آلرژی بالای فرم — قرمز دائمی</summary>
    [ObservableProperty]
    private string? _allergyWarning;

    /// <summary>پرونده بیمار — MRN</summary>
    [ObservableProperty]
    private string _patientMrn = string.Empty;

    /// <summary>کد ملی</summary>
    [ObservableProperty]
    private string _patientNationalCode = string.Empty;

    /// <summary>سن و جنسیت</summary>
    [ObservableProperty]
    private string _patientAgeGender = string.Empty;

    /// <summary>گروه خونی</summary>
    [ObservableProperty]
    private string? _patientBloodType;

    /// <summary>موبایل</summary>
    [ObservableProperty]
    private string _patientMobile = string.Empty;

    /// <summary>بیمه</summary>
    [ObservableProperty]
    private string _patientInsurance = "آزاد";

    /// <summary>تگ‌های آلرژی — قرمز</summary>
    public ObservableCollection<string> AllergyTags { get; } = new();

    /// <summary>تگ‌های بیماری مزمن — نارنجی</summary>
    public ObservableCollection<string> ChronicTags { get; } = new();

    /// <summary>تگ‌های داروهای فعلی — آبی</summary>
    public ObservableCollection<string> MedicationTags { get; } = new();

    /// <summary>سوابق ویزیت‌های اخیر سایدبار</summary>
    public ObservableCollection<HistoryRow> VisitHistory { get; } = new();

    /// <summary>سوابق نسخه</summary>
    [ObservableProperty]
    private int _prescriptionCount;

    /// <summary>سوابق آزمایش</summary>
    [ObservableProperty]
    private int _labCount;

    /* ═══════════ تب ۱: SOAP ═══════════ */
    [ObservableProperty] private string? _chiefComplaint;
    [ObservableProperty] private string? _hpi;
    [ObservableProperty] private string? _pmh;
    [ObservableProperty] private string? _familyHistory;
    [ObservableProperty] private string? _socialHistory;
    [ObservableProperty] private string? _reviewOfSystems;

    /* ═══════════ تب ۲: علائم حیاتی ═══════════ */
    [ObservableProperty] private string? _bpText;
    [ObservableProperty] private string? _hrText;
    [ObservableProperty] private string? _tempText;
    [ObservableProperty] private string? _spo2Text;
    [ObservableProperty] private string? _weightText;
    [ObservableProperty] private string? _heightText;
    [ObservableProperty] private string? _bmiDisplay;
    [ObservableProperty] private string? _bsText;
    [ObservableProperty] private ObservableCollection<VitalCard> _vitalCards = new();

    /// <summary>هشدارهای علائم حیاتی</summary>
    public ObservableCollection<string> VitalWarnings { get; } = new();

    /* ═══════════ تب ۳: معاینه فیزیکی ═══════════ */
    [ObservableProperty] private bool _headNeckNormal = true;
    [ObservableProperty] private string? _headNeckFindings;
    [ObservableProperty] private bool _chestNormal = true;
    [ObservableProperty] private string? _chestFindings;
    [ObservableProperty] private bool _abdomenNormal = true;
    [ObservableProperty] private string? _abdomenFindings;
    [ObservableProperty] private bool _limbsNormal = true;
    [ObservableProperty] private string? _limbsFindings;

    /* ═══════════ تب ۴: تشخیص ═══════════ */
    /// <summary>متن جستجوی تشخیص اصلی</summary>
    [ObservableProperty]
    private string _diagnosisSearch = string.Empty;

    /// <summary>نتایج جستجوی ICD-10</summary>
    [ObservableProperty]
    private ObservableCollection<DiagnosisOption> _diagnosisResults = new();

    /// <summary>تشخیص اصلی انتخاب‌شده</summary>
    [ObservableProperty]
    private DiagnosisOption? _primaryDiagnosis;

    /// <summary>تشخیص‌های ثانویه</summary>
    public ObservableCollection<string> SecondaryDiagnoses { get; } = new();

    /// <summary>برنامه درمانی</summary>
    [ObservableProperty]
    private string? _treatmentPlan;

    /// <summary>تاریخ مراجعه بعدی شمسی</summary>
    [ObservableProperty]
    private string? _followUpDateShamsi;

    /// <summary>رزرو خودکار نوبت پیگیری</summary>
    [ObservableProperty]
    private bool _autoBookFollowUp;

    /// <summary>دستورات پیگیری</summary>
    [ObservableProperty]
    private string? _followUpInstructions;

    /* ═══════════ عمومی ═══════════ */
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _saveStateText = string.Empty;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده — ادامه ویزیت موجود با شناسه</summary>
    public VisitEditViewModel(IApiClient api, ISnackbarService snackbar, long visitId)
    {
        _api = api;
        _snackbar = snackbar;
        _visitId = visitId;
        FormTitle = "ادامه ویزیت";
        _ = LoadAsync();
    }

    /// <summary>سازنده — ویزیت جدید برای بیمار</summary>
    public VisitEditViewModel(IApiClient api, ISnackbarService snackbar, long patientId, long doctorId)
    {
        _api = api;
        _snackbar = snackbar;
        _patientId = patientId;
        _doctorId = doctorId;
        _ = LoadPatientContextAsync();
    }

    /// <summary>بارگذاری ویزیت موجود</summary>
    private async Task LoadAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<VisitDetailResponse>($"visits/{_visitId}");
        if (response?.Data is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت ویزیت ناموفق بود";
            IsBusy = false;
            return;
        }

        var v = response.Data;
        _patientId = v.PatientId;
        _doctorId = v.DoctorId;
        _receptionId = v.ReceptionId;

        // فیلدهای SOAP
        ChiefComplaint = v.ChiefComplaint;
        Hpi = v.HistoryOfPresentIllness;
        Pmh = v.PastMedicalHistory;
        FamilyHistory = v.FamilyHistory;
        SocialHistory = v.SocialHistory;
        ReviewOfSystems = v.ReviewOfSystems;
        TreatmentPlan = v.TreatmentPlan;
        FollowUpInstructions = v.FollowUpInstructions;
        FillTags(SecondaryDiagnoses, v.SecondaryDiagnoses);
        if (v.FollowUpDateShamsi is not null) FollowUpDateShamsi = v.FollowUpDateShamsi;
        if (v.PrimaryDiagnosisCode is not null)
            PrimaryDiagnosis = new DiagnosisOption(0, v.PrimaryDiagnosisCode, v.PrimaryDiagnosisCode);

        // علائم حیاتی
        if (v.VitalSigns is { } vit)
        {
            BpText = vit.BloodPressureSystolic.HasValue ? $"{vit.BloodPressureSystolic}/{vit.BloodPressureDiastolic}" : null;
            HrText = vit.HeartRate?.ToString();
            TempText = vit.Temperature?.ToString();
            Spo2Text = vit.OxygenSaturation?.ToString();
            WeightText = vit.Weight?.ToString();
            HeightText = vit.Height?.ToString();
            BsText = vit.BloodSugar?.ToString();
        }
        RebuildVitalCards();

        await LoadPatientContextAsync();
        FormTitle = $"🩺 ویزیت بیمار: {v.Patient?.FullName ?? ""} [🔴 در حال ضبط]";
        IsBusy = false;
        StartTimers();
    }

    /// <summary>بارگذاری زمینه بیمار + سوابق</summary>
    private async Task LoadPatientContextAsync()
    {
        var patient = await _api.GetAsync<PatientFormViewModel.SearchResponse>($"patients/search?term={_patientId}");
        // زمینه کامل از ویزیت فعلی یا جستجو
        var detail = await _api.GetAsync<PatientFormViewModel.PatientDetail>($"patients/{_patientId}");
        if (detail is not null && _visitId is null)
        {
            PatientMrn = detail.MedicalRecordNo;
            PatientNationalCode = detail.NationalCode;
            PatientMobile = detail.Mobile;
            PatientBloodType = detail.BloodType;
            PatientAgeGender = $"{detail.Age?.ToString() ?? "—"} سال";
            PatientInsurance = detail.InsuranceName ?? "آزاد";
            FillTags(AllergyTags, detail.Allergies);
            FillTags(ChronicTags, detail.ChronicDiseases);
            FillTags(MedicationTags, detail.CurrentMedications);
            AllergyWarning = AllergyTags.Count > 0
                ? $"⚠️ بیمار به {string.Join("، ", AllergyTags)} آلرژی دارد"
                : null;
            FormTitle = $"🩺 ویزیت بیمار: {detail.FirstName} {detail.LastName} [🔴 در حال ضبط]";
        }

        // سوابق ویزیت
        var history = await _api.GetAsync<HistoryResponse>($"visits/patient/{_patientId}");
        if (history?.Data is not null)
        {
            VisitHistory.Clear();
            foreach (var h in history.Data.Take(10))
                VisitHistory.Add(new HistoryRow(h.Id, h.DateShamsi, h.DoctorName,
                    h.ChiefComplaint ?? "—"));
        }

        StartTimers();
    }

    /// <summary>راه‌اندازی AutoSave و Stopwatch</summary>
    private void StartTimers()
    {
        if (_autoSave is not null) return;
        _stopwatch.Start();

        // نمایش مدت — هر ثانیه
        var clock = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clock.Tick += (_, _) => ElapsedText = _stopwatch.Elapsed.ToString(@"mm\:ss");
        clock.Start();

        // AutoSave بی‌صدا هر ۶۰ ثانیه — طبق فرم ۱۱
        _autoSave = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoSave.Tick += async (_, _) => await SaveCoreAsync(silent: true);
        _autoSave.Start();
    }

    /* ═══════════ تب ۲: محاسبه کارت‌های حیاتی ═══════════ */

    /// <summary>بازسازی کارت‌ها با وضعیت رنگی</summary>
    partial void OnBpTextChanged(string? value) => RebuildVitalCards();
    partial void OnHrTextChanged(string? value) => RebuildVitalCards();
    partial void OnTempTextChanged(string? value) => RebuildVitalCards();
    partial void OnSpo2TextChanged(string? value) => RebuildVitalCards();
    partial void OnWeightTextChanged(string? value) => RebuildVitalCards();
    partial void OnHeightTextChanged(string? value) => RebuildVitalCards();
    partial void OnBsTextChanged(string? value) => RebuildVitalCards();

    /// <summary>ساخت ۸ کارت حیاتی با Badge وضعیت — طبق فرم ۱۱ تب ۲</summary>
    private void RebuildVitalCards()
    {
        VitalWarnings.Clear();
        var cards = new List<VitalCard>();

        // فشار خون
        var (bp, bpColor) = ("—", "#78909C");
        if (BpText is not null && BpText.Contains('/'))
        {
            var sys = ParseInt(BpText.Split('/')[0]);
            if (sys > 0)
            {
                var (t, c) = sys < 90 ? ("🔴 پایین", "#C62828") :
                    sys <= 120 ? ("🟢 طبیعی", "#2E7D32") :
                    sys <= 130 ? ("🟡 بالا", "#F57C00") : ("🔴 خطر", "#C62828");
                bp = BpText; bpColor = c;
                if (sys > 140) VitalWarnings.Add("⚠️ هشدار: فشار خون بالاتر از حد نرمال");
            }
        }
        cards.Add(new VitalCard("BP", "فشار خون", bp == "—" ? null : bp, "mmHg", t0(bpColor), bpColor));

        // ضربان
        var hr = ParseInt(HrText);
        var (hrS, hrC) = hr == 0 ? ("—", "#78909C") : hr is < 60 or > 100 ? ("🟡 غیرطبیعی", "#F57C00") : ("🟢 طبیعی", "#2E7D32");
        cards.Add(new VitalCard("HR", "ضربان قلب", hr == 0 ? null : hr.ToString(), "bpm", hrS, hrC));

        // دما
        var temp = ParseDec(TempText);
        var (tS, tC) = temp == 0 ? ("—", "#78909C") : temp > 37.5m ? ("🟡 تب", "#F57C00") : temp < 35m ? ("🟡 پایین", "#F57C00") : ("🟢 طبیعی", "#2E7D32");
        if (temp > 38) { tS = "🔴 تب بالا"; tC = "#C62828"; VitalWarnings.Add("⚠️ هشدار: تب بالاتر از حد نرمال"); }
        cards.Add(new VitalCard("Temp", "دمای بدن", temp == 0 ? null : temp.ToString(), "°C", tS, tC));

        // اکسیژن
        var spo2 = ParseInt(Spo2Text);
        var (oS, oC) = spo2 == 0 ? ("—", "#78909C") : spo2 < 92 ? ("🔴 خطر", "#C62828") : spo2 < 95 ? ("🟡 پایین", "#F57C00") : ("🟢 طبیعی", "#2E7D32");
        if (spo2 is > 0 and < 92) VitalWarnings.Add("⚠️ هشدار: اشباع اکسیژن پایین‌تر از حد نرمال");
        cards.Add(new VitalCard("SpO2", "اکسیژن خون", spo2 == 0 ? null : spo2.ToString(), "%", oS, oC));

        // وزن و قد و BMI
        var w = ParseDec(WeightText);
        var h = ParseDec(HeightText);
        cards.Add(new VitalCard("Weight", "وزن", w == 0 ? null : w.ToString(), "kg", "", "#78909C"));
        cards.Add(new VitalCard("Height", "قد", h == 0 ? null : h.ToString(), "cm", "", "#78909C"));

        if (w > 0 && h > 0)
        {
            var bmi = Math.Round(w / (h / 100m * (h / 100m)), 1);
            BmiDisplay = bmi.ToString("0.0");
            var (bS, bC) = bmi < 18.5m ? ("🟡 کمبود وزن", "#F57C00") :
                bmi < 25 ? ("🟢 طبیعی", "#2E7D32") :
                bmi < 30 ? ("🟡 اضافه وزن", "#F57C00") : ("🔴 چاقی", "#C62828");
            if (bmi >= 25) VitalWarnings.Add("⚠️ هشدار: BMI در محدوده اضافه وزن یا بالاتر");
            cards.Add(new VitalCard("BMI", "شاخص توده", BmiDisplay, "kg/m²", bS, bC));
        }
        else
        {
            BmiDisplay = null;
            cards.Add(new VitalCard("BMI", "شاخص توده", null, "kg/m²", "—", "#78909C"));
        }

        // قند خون
        var bs = ParseInt(BsText);
        var (sS, sC) = bs == 0 ? ("—", "#78909C") : bs is >= 70 and <= 110 ? ("🟢 نرمال", "#2E7D32") : bs is > 110 and <= 140 ? ("🟡 بالا", "#F57C00") : ("🔴 خطر", "#C62828");
        if (bs > 140) VitalWarnings.Add("⚠️ هشدار: قند خون بالاتر از حد نرمال");
        cards.Add(new VitalCard("BS", "قند خون", bs == 0 ? null : bs.ToString(), "mg/dL", sS, sC));

        VitalCards = new ObservableCollection<VitalCard>(cards);
    }

    private static string t0(string color) => color switch
    {
        "#2E7D32" => "🟢 طبیعی", "#F57C00" => "🟡 هشدار", "#C62828" => "🔴 خطر", _ => "—"
    };

    private static int ParseInt(string? text) =>
        int.TryParse(new string((text ?? "").Where(char.IsDigit).ToArray()), out var v) ? v : 0;

    private static decimal ParseDec(string? text) =>
        decimal.TryParse(new string((text ?? "").Where(c => char.IsDigit(c) || c == '.').ToArray()), out var v) ? v : 0;

    /* ═══════════ تب ۴: جستجوی تشخیص ═══════════ */

    /// <summary>جستجوی ICD-10 با Debounce</summary>
    partial void OnDiagnosisSearchChanged(string value)
    {
        if (value.Length < 2) { DiagnosisResults.Clear(); return; }
        _ = SearchDiagnosesAsync(value);
    }

    /// <summary>جستجوی تشخیص از داده مرجع</summary>
    private async Task SearchDiagnosesAsync(string term)
    {
        var response = await _api.GetAsync<DiagnosesResponse>(
            $"lookups/diagnoses?search={Uri.EscapeDataString(term)}&take=12");
        DiagnosisResults.Clear();
        if (response?.Data is null) return;
        foreach (var d in response.Data)
            DiagnosisResults.Add(new DiagnosisOption(d.Id, d.Code, d.Title));
    }

    /// <summary>انتخاب تشخیص اصلی</summary>
    [RelayCommand]
    private void SelectDiagnosis(object? param)
    {
        if (param is not DiagnosisOption option) return;
        PrimaryDiagnosis = option;
        DiagnosisSearch = option.Code;
        DiagnosisResults.Clear();
    }

    /// <summary>افزودن تشخیص ثانویه</summary>
    [RelayCommand]
    private void AddSecondaryDiagnosis(object? param)
    {
        if (param is DiagnosisOption option && !SecondaryDiagnoses.Contains(option.Title))
            SecondaryDiagnoses.Add(option.Title);
    }

    /* ═══════════ ذخیره ═══════════ */

    /// <summary>ذخیره موقت — Ctrl+S</summary>
    [RelayCommand]
    private async Task SaveDraftAsync() => await SaveCoreAsync(silent: false);

    /// <summary>پایان ویزیت — Ctrl+Enter</summary>
    [RelayCommand]
    private async Task CompleteVisitAsync()
    {
        var ok = await SaveCoreAsync(silent: false, complete: true);
        if (ok)
        {
            _snackbar.Success("ویزیت تکمیل شد");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>بیمار بعدی — ذخیره فعلی و بستن برای فراخوان بعدی</summary>
    [RelayCommand]
    private async Task NextPatientAsync()
    {
        await SaveCoreAsync(silent: false);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>هسته ذخیره — ساخت DTO و POST</summary>
    private async Task<bool> SaveCoreAsync(bool silent, bool complete = false)
    {
        if (_suppressAutoSave) return true;
        _suppressAutoSave = true;
        try
        {
            var body = new
            {
                id = _visitId,
                patientId = _patientId,
                doctorId = _doctorId,
                receptionId = _receptionId,
                chiefComplaint = ChiefComplaint,
                historyOfPresentIllness = Hpi,
                pastMedicalHistory = Pmh,
                physicalExamination = BuildExamText(),
                soapSubjective = ChiefComplaint,
                soapObjective = BuildExamText(),
                soapAssessment = PrimaryDiagnosis?.Title,
                soapPlan = TreatmentPlan,
                primaryDiagnosisId = PrimaryDiagnosis?.Id == 0 ? null : PrimaryDiagnosis?.Id,
                treatmentPlan = TreatmentPlan,
                followUpDateShamsi = string.IsNullOrWhiteSpace(FollowUpDateShamsi) ? null : FollowUpDateShamsi.Trim(),
                complete,
                familyHistory = FamilyHistory,
                socialHistory = SocialHistory,
                reviewOfSystems = ReviewOfSystems,
                secondaryDiagnoses = JsonSerializer.Serialize(SecondaryDiagnoses.ToList()),
                followUpInstructions = FollowUpInstructions,
                vitalSigns = BuildVitals()
            };

            var response = await _api.PostAsync<SaveVisitResponse>("visits", body);
            if (response?.Data is null or 0)
            {
                if (!silent)
                {
                    ErrorMessage = _api.LastError ?? "ذخیره ویزیت ناموفق بود";
                    _snackbar.Error(ErrorMessage);
                }
                return false;
            }

            SaveStateText = complete
                ? $"✅ تکمیل شد — {System.DateTime.Now:HH:mm}"
                : $"💾 ذخیره خودکار — {System.DateTime.Now:HH:mm}";
            if (!silent && !complete)
                _snackbar.Success("ویزیت ذخیره شد");
            return true;
        }
        finally
        {
            _suppressAutoSave = false;
        }
    }

    /// <summary>متن معاینه از چهار ناحیه بدن</summary>
    private string? BuildExamText()
    {
        var parts = new List<string>();
        if (!HeadNeckNormal && !string.IsNullOrWhiteSpace(HeadNeckFindings)) parts.Add($"سر و گردن: {HeadNeckFindings}");
        else if (HeadNeckNormal) parts.Add("سر و گردن: نرمال");
        if (!ChestNormal && !string.IsNullOrWhiteSpace(ChestFindings)) parts.Add($"قفسه سینه: {ChestFindings}");
        else if (ChestNormal) parts.Add("قفسه سینه: نرمال");
        if (!AbdomenNormal && !string.IsNullOrWhiteSpace(AbdomenFindings)) parts.Add($"شکم: {AbdomenFindings}");
        else if (AbdomenNormal) parts.Add("شکم: نرمال");
        if (!LimbsNormal && !string.IsNullOrWhiteSpace(LimbsFindings)) parts.Add($"اندام‌ها: {LimbsFindings}");
        else if (LimbsNormal) parts.Add("اندام‌ها: نرمال");
        return parts.Count == 0 ? null : string.Join("؛ ", parts);
    }

    /// <summary>ساخت DTO علائم حیاتی</summary>
    private object? BuildVitals()
    {
        var hasAny = !string.IsNullOrWhiteSpace(BpText) || !string.IsNullOrWhiteSpace(HrText) ||
                     !string.IsNullOrWhiteSpace(TempText) || !string.IsNullOrWhiteSpace(Spo2Text) ||
                     !string.IsNullOrWhiteSpace(WeightText) || !string.IsNullOrWhiteSpace(BsText);
        if (!hasAny) return null;

        int? sys = null, dia = null;
        if (BpText is not null && BpText.Contains('/'))
        {
            sys = ParseInt(BpText.Split('/')[0]);
            var d = ParseInt(BpText.Split('/')[1]);
            dia = d > 0 ? d : null;
            if (sys == 0) sys = null;
        }
        return new
        {
            bloodPressureSystolic = sys,
            bloodPressureDiastolic = dia,
            heartRate = ParseNullable(HrText),
            temperature = ParseNullableDec(TempText),
            oxygenSaturation = ParseNullable(Spo2Text),
            weight = ParseNullableDec(WeightText),
            height = ParseNullableDec(HeightText),
            bloodSugar = ParseNullable(BsText)
        };
    }

    private static int? ParseNullable(string? text)
    {
        var v = ParseInt(text);
        return v == 0 ? null : v;
    }

    private static decimal? ParseNullableDec(string? text)
    {
        var v = ParseDec(text);
        return v == 0 ? null : v;
    }

    /// <summary>پر کردن مجموعه تگ از JSON</summary>
    private static void FillTags(ObservableCollection<string> target, string? json)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json);
            if (items is not null)
                foreach (var item in items) target.Add(item);
        }
        catch
        {
            target.Add(json);
        }
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class VisitDetailResponse
    {
        public bool IsSuccess { get; set; }
        public VisitDetail? Data { get; set; }
    }

    public sealed class VisitDetail
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
        public long? ReceptionId { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public string? PastMedicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? SocialHistory { get; set; }
        public string? ReviewOfSystems { get; set; }
        public string? SecondaryDiagnoses { get; set; }
        public string? FollowUpInstructions { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? FollowUpDateShamsi { get; set; }
        public string? PrimaryDiagnosisCode { get; set; }
        public PatientContext? Patient { get; set; }
        public VitalSignsView? VitalSigns { get; set; }
    }

    public sealed class PatientContext
    {
        public string FullName { get; set; } = string.Empty;
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string GenderText { get; set; } = string.Empty;
        public string? BloodType { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
    }

    public sealed class VitalSignsView
    {
        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }
        public int? HeartRate { get; set; }
        public decimal? Temperature { get; set; }
        public int? OxygenSaturation { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? Bmi { get; set; }
        public int? BloodSugar { get; set; }
    }

    public sealed class DiagnosesResponse
    {
        public bool IsSuccess { get; set; }
        public List<DiagnosisItem>? Data { get; set; }
    }

    public sealed class DiagnosisItem
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public sealed class HistoryResponse
    {
        public bool IsSuccess { get; set; }
        public List<HistoryItem>? Data { get; set; }
    }

    public sealed class HistoryItem
    {
        public long Id { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
    }

    public sealed class SaveVisitResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }
}

/// <summary>ردیف سابقه ویزیت در سایدبار</summary>
public record HistoryRow(long Id, string DateShamsi, string DoctorName, string Complaint);
