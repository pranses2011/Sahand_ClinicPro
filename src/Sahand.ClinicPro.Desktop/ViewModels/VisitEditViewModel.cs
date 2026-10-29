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
    private long? _visitId;
    private bool _creationResultUnknown;
    private string? _editVersion;
    private readonly bool _openedExisting;
    private bool _existingVisitLoaded;
    private bool _visitEditable = true;
    private bool _hasExistingVitals;
    private string? _soapSubjective, _soapObjective, _soapAssessment, _soapPlan;
    private string? _originalSecondaryDiagnoses, _initialSecondaryRepresentation;
    private System.Windows.Threading.DispatcherTimer? _clock;
    private long _patientId;
    private long _doctorId;
    private long? _receptionId;
    private System.Windows.Threading.DispatcherTimer? _autoSave;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private bool _suppressAutoSave;

    /// <summary>عنوان پنجره — «🩺 ویزیت بیمار: نام [🔴 در حال ضبط]»</summary>
    [ObservableProperty]
    private string _formTitle = "ثبت ویزیت جدید";

    /// <summary>
    /// ═══ چرخهٔ ۷۸ — ایندکس تب فعال فرم ویزیت ═══
    /// برای «هدایت هوشمند» به کاربر: وقتی پایان ویزیت بدون تشخیص اصلی زده می‌شود،
    /// فرم خودکار به تب «تشخیص» (ایندکس ۳) می‌پرد تا پزشک بلافاصله بفهمد چه چیزی
    /// کم است — به‌جای نمایش خطای خام سرور (گزارش مالک: «پایان ویزیت همیشه
    /// خطای تشخیص اصلی الزامی است می‌دهد»).
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>مدت ویزیت زنده — Stopwatch</summary>
    [ObservableProperty]
    private string _elapsedText = "۰۰:۰۰";

    /// <summary>هشدار آلرژی بالای فرم — قرمز دائمی</summary>
    [ObservableProperty]
    private string? _allergyWarning;

    /// <summary>
    /// ═══ چرخهٔ ۷۸ (بازطراحی فرم ویزیت) ═══ نام و نام خانوادگی بیمار — هویت
    /// اصلی کارت سربرگ و سایدبار؛ پیش‌تر کد ملی به‌جای نام در سایدبار نشسته
    /// بود و نام فقط داخل عنوان پنجره دیده می‌شد.
    /// </summary>
    [ObservableProperty]
    private string _patientName = "بیمار";

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
    [ObservableProperty] private string? _physicalExamText;
    [ObservableProperty] private bool _headNeckNormal;
    [ObservableProperty] private string? _headNeckFindings;
    [ObservableProperty] private bool _chestNormal;
    [ObservableProperty] private string? _chestFindings;
    [ObservableProperty] private bool _abdomenNormal;
    [ObservableProperty] private string? _abdomenFindings;
    [ObservableProperty] private bool _limbsNormal;
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
        _openedExisting = true;
        FormTitle = "ادامه ویزیت";
        _ = LoadAsync();
        // چرخهٔ ۴۸ — فهرست آزمایش‌های مرجع برای تب ارسال به آزمایشگاه
        _ = LoadLabTestsAsync();
    }

    /// <summary>سازنده — ویزیت جدید برای بیمار</summary>
    public VisitEditViewModel(IApiClient api, ISnackbarService snackbar, long patientId, long doctorId)
    {
        _api = api;
        _snackbar = snackbar;
        _patientId = patientId;
        _doctorId = doctorId;
        _ = LoadPatientContextAsync();
        // چرخهٔ ۴۸ — فهرست آزمایش‌های مرجع برای تب ارسال به آزمایشگاه
        _ = LoadLabTestsAsync();
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
        _editVersion = v.EditVersion;
        _visitEditable = v.Status is 1 or 2 or 4 && !string.IsNullOrWhiteSpace(v.EditVersion);
        _soapSubjective = v.SoapSubjective;
        _soapObjective = v.SoapObjective;
        _soapAssessment = v.SoapAssessment;
        _soapPlan = v.SoapPlan;
        PhysicalExamText = v.PhysicalExamination;
        _originalSecondaryDiagnoses = v.SecondaryDiagnoses;

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
        _initialSecondaryRepresentation = JsonSerializer.Serialize(SecondaryDiagnoses.ToList());
        if (v.FollowUpDateShamsi is not null) FollowUpDateShamsi = v.FollowUpDateShamsi;
        // ═══ چرخهٔ ۷۸ — ریشهٔ «پایان ویزیت همیشه خطا می‌دهد» ═══
        // پیش‌تر فقط Code خوانده می‌شد و جای‌نگهدارِ «Id=0» ساخته می‌شد؛ در ذخیره،
        // Id=0 به تهی ترجمه می‌شد و اعتبارسنجِ سرورِ «تشخیص اصلی الزامی است»
        // هر بار پایانِ ویزیتِ ویرایش‌شده را رد می‌کرد — هرچند تشخیص روی صفحه بود!
        // اکنون «شناسهٔ واقعی» از DTO خوانده می‌شود و عنوان کامل هم نمایش داده می‌شود.
        if (v.PrimaryDiagnosisId is { } diagId)
            PrimaryDiagnosis = new DiagnosisOption(diagId, v.PrimaryDiagnosisCode ?? "",
                v.PrimaryDiagnosisTitle ?? v.PrimaryDiagnosisCode ?? "تشخیص ثبت‌شده");
        if (v.VitalSigns is { } vit)
        {
            _hasExistingVitals = true;
            BpText = vit.BloodPressureSystolic.HasValue ? $"{vit.BloodPressureSystolic}/{vit.BloodPressureDiastolic}" : null;
            HrText = vit.HeartRate?.ToString();
            TempText = vit.Temperature?.ToString();
            Spo2Text = vit.OxygenSaturation?.ToString();
            WeightText = vit.Weight?.ToString();
            HeightText = vit.Height?.ToString();
            BsText = vit.BloodSugar?.ToString();
        }
        RebuildVitalCards();

        _existingVisitLoaded = true;
        await LoadPatientContextAsync();
        FormTitle = _visitEditable ? $"🩺 ویزیت بیمار: {v.Patient?.FullName ?? ""} [🔴 در حال ضبط]" : "ویزیت تکمیل‌شده — فقط خواندنی";
        PatientName = v.Patient?.FullName ?? PatientName;
        IsBusy = false;
        StartTimers();
    }

    /// <summary>بارگذاری زمینه بیمار + سوابق</summary>
    private async Task LoadPatientContextAsync()
    {
        var patient = await _api.GetAsync<PatientFormViewModel.SearchResponse>($"patients/search?term={_patientId}");
        // ═══ چرخهٔ ۴۸ — پاکت استاندارد ApiResponse باز شود (قانون هم‌کلاسی‌ها) ═══
        // نگاشت مستقیمِ پیشین یعنی همهٔ مشخصات بیمار همیشه پیش‌فرض — شمارهٔ
        // پرونده، موبایل، بیمه و مهم‌تر از همه «حساسیت‌های دارویی/بیماری‌های
        // مزمن» هرگز در فرم ویزیت دیده نمی‌شدند (پایش حساسیت بی‌اثر).
        var patientEnvelope = await _api.GetAsync<PatientFormViewModel.PatientDetailEnvelope>($"patients/{_patientId}");
        var detail = patientEnvelope?.Data;
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
            PatientName = $"{detail.FirstName} {detail.LastName}".Trim();
            FormTitle = $"🩺 ویزیت بیمار: {PatientName} [🔴 در حال ضبط]";
        }

        // سوابق ویزیت
        var history = await _api.GetAsync<HistoryResponse>($"visits/patient/{_patientId}");
        if (history?.Data is not null)
        {
            VisitHistory.Clear();
            foreach (var h in history.Data.Take(10))
                VisitHistory.Add(new HistoryRow(h.Id, h.DateShamsi, h.DoctorName,
                    h.ChiefComplaint ?? "—"));

            // چرخهٔ ۶۹ (فاز ۳ / F-15): ویزیت قبلیِ همین بیمار برای «کپی ویزیت قبلی»
            // شناسه‌اش نگه داشته می‌شود — ویزیت جاری (اگر ادامه است) از فهرست کنار
            // می‌رود تا «قبلی» واقعاً قبلیِ همین ویزیت باشد نه خودش.
            PreviousVisitId = history.Data
                .Where(h => h.Id != _visitId)
                .OrderByDescending(h => h.Id)
                .FirstOrDefault()?.Id;
            PreviousVisitDate = history.Data
                .Where(h => h.Id != _visitId)
                .OrderByDescending(h => h.Id)
                .FirstOrDefault()?.DateShamsi;
        }

        StartTimers();
    }

    /// <summary>
    /// شناسهٔ ویزیت قبلی همین بیمار — برای «کپی ویزیت قبلی» (چرخهٔ ۶۹ / F-15)؛
    /// تهی یعنی ویزیت قبلی وجود ندارد و دکمه مخفی می‌شود.
    /// </summary>
    [ObservableProperty]
    private long? _previousVisitId;

    /// <summary>تاریخ شمسی ویزیت قبلی — نمایش روی دکمهٔ کپی</summary>
    [ObservableProperty]
    private string? _previousVisitDate;

    /// <summary>
    /// کپی ویزیت قبلی — چرخهٔ ۶۹ (فاز ۳ / F-15 «کپی ویزیت قبلی»):
    /// شرح حال/سابقه/برنامهٔ درمانی/توصیه‌های پیگیری ویزیت قبلی همین بیمار در
    /// فرم جاری ریخته می‌شود تا پزشکِ ویزیت‌های تکراری (پیگیری مزمن) از صفر
    /// تایپ نکند. نسخه و علائم حیاتی عمداً کپی «نمی‌شوند» — خطر بالینی دارند.
    /// </summary>
    [RelayCommand]
    private async Task CopyPreviousVisitAsync()
    {
        if (PreviousVisitId is not { } prevId)
            return;

        // تأیید فارسی — بازنویسی نکردنی نیست ولی کاربر باید بداند فیلدها جایگزین می‌شوند
        if (!Views.AppDialog.Confirm("کپی از ویزیت قبلی",
                $"شرح حال، سابقه‌ها، برنامهٔ درمانی و توصیه‌های پیگیری ویزیت {PreviousVisitDate ?? "قبلی"} " +
                "در فرم جاری جایگزین می‌شود. ادامه می‌دهید؟"))
            return;

        var response = await _api.GetAsync<VisitDetailResponse>($"visits/{prevId}");
        var v = response?.Data;
        if (v is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت ویزیت قبلی ناموفق بود");
            return;
        }

        // فقط فیلدهای تکرارپذیر و کم‌خطر — نسخه/علائم حیاتی هرگز کپی نمی‌شوند
        ChiefComplaint = v.ChiefComplaint;
        Hpi = v.HistoryOfPresentIllness;
        Pmh = v.PastMedicalHistory;
        FamilyHistory = v.FamilyHistory;
        SocialHistory = v.SocialHistory;
        ReviewOfSystems = v.ReviewOfSystems;
        TreatmentPlan = v.TreatmentPlan;
        FollowUpInstructions = v.FollowUpInstructions;
        if (v.PrimaryDiagnosisCode is not null && v.PrimaryDiagnosisId is { } copyDiagId)
            PrimaryDiagnosis = new DiagnosisOption(copyDiagId, v.PrimaryDiagnosisCode,
                v.PrimaryDiagnosisTitle ?? v.PrimaryDiagnosisCode);
        FillTags(SecondaryDiagnoses, v.SecondaryDiagnoses);

        _snackbar.Success($"از ویزیت {PreviousVisitDate ?? "قبلی"} کپی شد — بازبینی و ویرایش کنید");
    }

    /// <summary>راه‌اندازی AutoSave و Stopwatch</summary>
    private void StartTimers()
    {
        if (_autoSave is not null || !_visitEditable) return;
        _stopwatch.Start();

        // نمایش مدت — هر ثانیه
        _clock = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => ElapsedText = _stopwatch.Elapsed.ToString(@"mm\:ss");
        _clock.Start();

        // AutoSave بی‌صدا هر ۶۰ ثانیه — طبق فرم ۱۱
        _autoSave = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoSave.Tick += async (_, _) => await SaveCoreAsync(silent: true);
        _autoSave.Start();
    }

    /// <summary>توقف ذخیره خودکار و ساعت پس از بسته شدن فرم.</summary>
    public void StopTimers()
    {
        _autoSave?.Stop();
        _clock?.Stop();
        _stopwatch.Stop();
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
        static VitalCard Card(string key, string title, string? value, string unit) =>
            new(key, title, value, unit, value is null ? "—" : "ثبت‌شده؛ نیازمند ارزیابی", "#546E7A");
        var cards = new List<VitalCard>();
        var bpParts = (BpText ?? "").Split('/');
        var validBp = bpParts.Length == 2 && TryInt(bpParts[0], out var systolic) && systolic is not null &&
                      TryInt(bpParts[1], out _);
        cards.Add(Card("BP", "فشار خون", validBp ? BpText : null, "mmHg"));
        cards.Add(Card("HR", "ضربان قلب", TryInt(HrText, out var hr) ? hr?.ToString() : null, "bpm"));
        cards.Add(Card("Temp", "دمای بدن", TryDecimal(TempText, out var temp) ? temp?.ToString() : null, "°C"));
        cards.Add(Card("SpO2", "اکسیژن خون", TryInt(Spo2Text, out var spo2) ? spo2?.ToString() : null, "%"));
        var hasWeight = TryDecimal(WeightText, out var weight);
        var hasHeight = TryDecimal(HeightText, out var height);
        cards.Add(Card("Weight", "وزن", hasWeight ? weight?.ToString() : null, "kg"));
        cards.Add(Card("Height", "قد", hasHeight ? height?.ToString() : null, "cm"));
        BmiDisplay = null;
        if (hasWeight && hasHeight && weight is > 0 and <= 999.99m && height is >= 0.01m and <= 999.99m)
        {
            var bmi = Math.Round(weight.Value / (height.Value / 100m) / (height.Value / 100m), 1);
            BmiDisplay = bmi.ToString("0.0");
        }
        cards.Add(Card("BMI", "شاخص توده", BmiDisplay, "kg/m²"));
        cards.Add(Card("BS", "قند خون", TryInt(BsText, out var sugar) ? sugar?.ToString() : null, "mg/dL"));
        VitalCards = new ObservableCollection<VitalCard>(cards);
    }

    private static bool TryDecimal(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!Sahand.ClinicPro.Desktop.Helpers.PlainNumber.TryParse(text, out var parsed)) return false;
        value = parsed;
        return true;
    }

    private static bool TryInt(string? text, out int? value)
    {
        value = null;
        if (!TryDecimal(text, out var parsed)) return false;
        if (parsed is null) return true;
        if (parsed != decimal.Truncate(parsed.Value) || parsed < int.MinValue || parsed > int.MaxValue) return false;
        value = (int)parsed.Value;
        return true;
    }

    /* ═══════════ تب ۴: جستجوی تشخیص ═══════════ */

    /// <summary>جستجوی ICD-10 با Debounce</summary>
    private int _diagnosisSearchVersion;
    partial void OnDiagnosisSearchChanged(string value)
    {
        var version = ++_diagnosisSearchVersion;
        DiagnosisResults.Clear();
        if (value.Length < 2) return;
        _ = SearchDiagnosesAsync(value, version);
    }

    /// <summary>جستجوی تشخیص از داده مرجع؛ پاسخ قدیمی نمایش داده نمی‌شود.</summary>
    private async Task SearchDiagnosesAsync(string term, int version)
    {
        try
        {
            var response = await _api.GetAsync<DiagnosesResponse>(
                $"lookups/diagnoses?search={Uri.EscapeDataString(term)}&take=12");
            if (version != _diagnosisSearchVersion) return;
            if (response?.Data is null) { _snackbar.Error("دریافت نتایج تشخیص ناموفق بود."); return; }
            foreach (var d in response.Data)
                DiagnosisResults.Add(new DiagnosisOption(d.Id, d.Code, d.Title));
        }
        catch (Exception) { if (version == _diagnosisSearchVersion) _snackbar.Error("دریافت نتایج تشخیص ناموفق بود."); }
    }

    /// <summary>انتخاب تشخیص اصلی</summary>
    [RelayCommand]
    private void SelectDiagnosis(object? param)
    {
        if (param is not DiagnosisOption option || !DiagnosisResults.Contains(option)) return;
        PrimaryDiagnosis = option;
        DiagnosisSearch = option.Code;
        _diagnosisSearchVersion++;
        DiagnosisResults.Clear();
    }

    /// <summary>افزودن تشخیص ثانویه</summary>
    [RelayCommand]
    private void AddSecondaryDiagnosis(object? param)
    {
        if (param is DiagnosisOption option && DiagnosisResults.Contains(option) && !SecondaryDiagnoses.Contains(option.Title))
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
        // ═══ چرخهٔ ۷۸ — گارد فارسی سمت کلاینت ═══ به‌جای خطای خام سرور
        // («اطلاعات ارسالی نامعتبر است — برای تکمیل ویزیت ثبت تشخیص اصلی الزامی
        // است»)، پزشک پیام راهنما می‌گیرد و فرم خودکار به تب تشخیص می‌پرد؛
        // تشخیصِ جای‌نگهدارِ بی‌شناسه (Id=0 — از داده‌های قدیمی) هم مثل «نبودن»
        // در نظر گرفته می‌شود تا دوباره انتخاب شود و شناسهٔ واقعی ارسال گردد
        if (PrimaryDiagnosis is null || PrimaryDiagnosis.Id == 0)
        {
            SelectedTabIndex = 3; // تب «🏥 تشخیص» — چهارمین تب
            _snackbar.Error("برای پایان ویزیت، ابتدا تشخیص اصلی را از تب «تشخیص» انتخاب کنید (جستجوی ICD-10)");
            return;
        }

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
        if (await SaveCoreAsync(silent: false)) CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /* ═════════════════════════════════════════════════════════════════
       چرخهٔ ۴۸ — تب‌های ارجاع عملیاتی (گزارش مالک):
       «این قابلیت‌ها که پزشک بیمارش را انتخاب کند و … ارسال به آزمایشگاه و
       ارسال به تصویربرداری و ارسال به فیزیوتراپی و ارسال به نسخه‌نویسی و
       تداخل‌سنجی خودکار داروها و پایش حساسیت‌های بیمار … پیاده‌سازی نشده».
       تب‌های ۵ تا ۷ پیش‌تر فقط متن هدایت بودند؛ اکنون هر تب یک پنل عملیاتی
       است که ارجاع را با زمینهٔ کامل همین ویزیت (بیمار/پزشک/ویزیت) ثبت می‌کند
       و در ماژول مقصد (آزمایشگاه/تصویربرداری/فیزیوتراپی/داروخانه) دیده می‌شود.
       تداخل‌سنجی و پایش حساسیت، در فرم نسخه‌نویسی که از تب ۵ باز می‌شود
       خودکار اجرا می‌شود (کش محلی + بررسی سرور با آلرژی/بارداری/سن).
       ═════════════════════════════════════════════════════════════════ */

    /* ─── تب ۵: ارسال به آزمایشگاه ─── */

    /// <summary>فیلتر زندهٔ نام آزمایش — پالایش سمت کلاینت روی فهرست بارگذاری‌شده</summary>
    [ObservableProperty]
    private string _labTestSearch = string.Empty;

    /// <summary>آزمایش‌های قابل انتخاب — همان ردیف‌های فرم آزمایشگاه (Id/Name/Category/IsSelected)</summary>
    public ObservableCollection<LabTestOptionRow> LabTests { get; } = new();

    /// <summary>گزینه‌های اولویت — ترتیب با enum سرور هم‌خوان است (۱ اورژانس، ۲ بالا، ۳ عادی)</summary>
    public string[] LabPriorities { get; } = { "اورژانس", "بالا", "عادی" };

    /// <summary>نمایهٔ اولویت درخواست آزمایش — پیش‌فرض «عادی»</summary>
    [ObservableProperty]
    private int _labPriorityIndex = 2;

    /// <summary>توضیح بالینی همراه درخواست آزمایش (اختیاری)</summary>
    [ObservableProperty]
    private string? _labClinicalInfo;

    /// <summary>پالایش زندهٔ فهرست آزمایش‌ها با جستجو</summary>
    partial void OnLabTestSearchChanged(string value)
        => OnPropertyChanged(nameof(VisibleLabTests));

    /// <summary>آزمایش‌های قابل دیدن پس از پالایش — فهرست صرفه‌جویانه با Any به‌جای شمارش کامل</summary>
    public IEnumerable<LabTestOptionRow> VisibleLabTests
    {
        get
        {
            var term = LabTestSearch?.Trim();
            return string.IsNullOrWhiteSpace(term)
                ? LabTests
                : LabTests.Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                    || (t.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    /// <summary>بارگذاری آزمایش‌های فعال مرجع — یک‌بار در عمر فرم (۲۰۰ سقف سرور)</summary>
    [RelayCommand]
    private async Task LoadLabTestsAsync()
    {
        if (LabTests.Count > 0) return; // قبلاً بارگذاری شده
        var response = await _api.GetAsync<LabOrderFormViewModel.LabTestsResponse>("lookups/lab-tests");
        if (response?.Data is null) return;
        foreach (var t in response.Data)
            LabTests.Add(new LabTestOptionRow { Id = t.Id, Name = t.Name, Category = t.Category });
        OnPropertyChanged(nameof(VisibleLabTests));
    }

    /// <summary>ارسال درخواست آزمایش با زمینهٔ همین ویزیت — در صف آزمایشگاه می‌نشیند</summary>
    [RelayCommand]
    private async Task SendToLabAsync()
    {
        var selected = LabTests.Where(t => t.IsSelected).Select(t => t.Id).ToList();
        if (selected.Count == 0)
        {
            _snackbar.Error("حداقل یک آزمایش را از فهرست انتخاب کنید");
            return;
        }
        if (_patientId <= 0)
        {
            _snackbar.Error("بیمار ویزیت مشخص نیست — ابتدا ویزیت را ذخیره کنید");
            return;
        }

        IsBusy = true;
        var ok = await _api.PostAsync("lab/orders", new
        {
            patientId = _patientId,
            doctorId = _doctorId > 0 ? _doctorId : (long?)null,
            visitId = _visitId,
            testIds = selected,
            priority = LabPriorityIndex + 1, // هم‌ترتیب با enum سرور: ۱ اورژانس، ۲ بالا، ۳ عادی
            clinicalInfo = string.IsNullOrWhiteSpace(LabClinicalInfo) ? null : LabClinicalInfo.Trim()
        });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success($"درخواست {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(selected.Count.ToString())} آزمایش برای آزمایشگاه ارسال شد");
            foreach (var t in LabTests.Where(t => t.IsSelected))
                t.IsSelected = false;
            LabClinicalInfo = null;
        }
        else
            _snackbar.Error(_api.LastError ?? "ارسال درخواست آزمایش ناموفق بود");
    }

    /* ─── تب ۶: ارسال به تصویربرداری ─── */

    /// <summary>مودالیته‌های تصویربرداری — ترتیب با enum سرور ImagingModality هم‌خوان است (۱ سونوگرافی، ۲ رادیوگرافی، ۳ CT، ۴ MRI)</summary>
    public string[] ImagingModalities { get; } =
        { "سونوگرافی", "رادیوگرافی (X-Ray)", "سی‌تی‌اسکن (CT)", "ام‌آر‌آی (MRI)" };

    /// <summary>نمایهٔ مودالیتهٔ منتخب</summary>
    [ObservableProperty]
    private int _imagingModalityIndex;

    /// <summary>نواحی بدن — همان گزینه‌های پیشنهادی صفحهٔ تصویربرداری</summary>
    public string[] ImagingBodyParts { get; } =
        { "سر و گردن", "قفسه سینه", "شکم و لگن", "ستون فقرات", "اندام فوقانی", "اندام تحتانی", "مفصل شانه", "مفصل زانو" };

    /// <summary>نمایهٔ ناحیهٔ بدن</summary>
    [ObservableProperty]
    private int _imagingBodyPartIndex;

    /// <summary>درخواست فوری است؟ — اولویت اورژانس به سرور می‌رود</summary>
    [ObservableProperty]
    private bool _imagingUrgent;

    /// <summary>توضیح بالینی (اختیاری)</summary>
    [ObservableProperty]
    private string? _imagingClinicalInfo;

    /// <summary>ارسال درخواست تصویربرداری با قرارداد درست سرور — با پذیرندهٔ پزشکِ همین ویزیت</summary>
    [RelayCommand]
    private async Task SendToImagingAsync()
    {
        if (_patientId <= 0)
        {
            _snackbar.Error("بیمار ویزیت مشخص نیست — ابتدا ویزیت را ذخیره کنید");
            return;
        }

        IsBusy = true;
        var ok = await _api.PostAsync("imaging/orders", new
        {
            patientId = _patientId,
            referringDoctorId = _doctorId > 0 ? _doctorId : (long?)null,
            modality = ImagingModalityIndex + 1,
            bodyPart = ImagingBodyParts[ImagingBodyPartIndex],
            priority = ImagingUrgent ? 1 : 3, // ۱ اورژانس، ۳ عادی
            clinicalInfo = string.IsNullOrWhiteSpace(ImagingClinicalInfo) ? null : ImagingClinicalInfo.Trim()
        });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success($"درخواست {ImagingModalities[ImagingModalityIndex]} ({ImagingBodyParts[ImagingBodyPartIndex]}) ارسال شد");
            ImagingClinicalInfo = null;
            ImagingUrgent = false;
        }
        else
            _snackbar.Error(_api.LastError ?? "ارسال درخواست تصویربرداری ناموفق بود");
    }

    /* ─── تب فیزیوتراپی: ارسال به فیزیوتراپی ─── */

    /// <summary>شرح ارجاع به فیزیوتراپی — شکایت/تشخیص/هدف درمان (اختیاری)</summary>
    [ObservableProperty]
    private string? _physioReferralNote;

    /// <summary>
    /// ارسال به فیزیوتراپی — چرخهٔ ۴۸: قرارداد فعلی سرور جلسه‌محور است
    /// (RecordPhysioSessionCommand)؛ ارجاع به‌صورت نخستین جلسهٔ «ارجاعی» با
    /// درمانگر تهی و برچسب واضح ثبت می‌شود تا در فهرست جلساتِ بخش فیزیوتراپی
    /// دیده شود و درمانگر جلسات بعدی را با شمارهٔ ۲ به بعد ادامه دهد.
    /// </summary>
    [RelayCommand]
    private async Task SendToPhysioAsync()
    {
        if (_patientId <= 0)
        {
            _snackbar.Error("بیمار ویزیت مشخص نیست — ابتدا ویزیت را ذخیره کنید");
            return;
        }

        IsBusy = true;
        var ok = await _api.PostAsync("physiotherapy/sessions", new
        {
            patientId = _patientId,
            therapistId = (long?)null, // درمانگر هنوز مشخص نیست — ارجاع است نه جلسهٔ درمان
            sessionNumber = 1,
            treatments = string.IsNullOrWhiteSpace(PhysioReferralNote)
                ? "ارجاع به فیزیوتراپی از ویزیت پزشک"
                : $"ارجاع به فیزیوتراپی از ویزیت پزشک — {PhysioReferralNote.Trim()}",
            duration = (int?)null,
            painScore = (byte?)null
        });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success("ارجاع به فیزیوتراپی ثبت شد — در فهرست جلسات بخش فیزیوتراپی دیده می‌شود");
            PhysioReferralNote = null;
        }
        else
            _snackbar.Error(_api.LastError ?? "ارسال به فیزیوتراپی ناموفق بود");
    }

    /* ─── تب ۵: گشودن نسخه‌نویسی با زمینهٔ ویزیت ─── */

    /// <summary>
    /// گشودن فرم نسخه‌نویسی برای بیمار همین ویزیت — چرخهٔ ۴۸. ابتدا ویزیت
    /// ذخیره می‌شود تا مرجع کامل باشد، سپس فرم نسخه با بیمارِ پیش‌پرشده باز
    /// می‌شود؛ تداخل‌سنجی خودکار داروها و پایش حساسیت‌های بیمار همان‌جا و
    /// با هر افزودن قلم بی‌درنگ اجرا می‌شود (کش محلی + بررسی سرور).
    /// </summary>
    [RelayCommand]
    private async Task OpenPrescriptionAsync()
    {
        if (_patientId <= 0)
        {
            _snackbar.Error("بیمار ویزیت مشخص نیست");
            return;
        }

        // ویزیتِ در جریان ذخیره شود تا نسخه در پرونده به زمینهٔ کامل وصل باشد
        if (!await SaveCoreAsync(silent: false)) return;

        // پزشکان برای انتخاب صادرکننده — همان الگوی صفحهٔ نسخه‌ها
        var doctors = new ObservableCollection<DoctorOption>();
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        var vm = new PrescriptionFormViewModel(_api, _snackbar, doctors);
        var window = new Views.PrescriptionFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        // پیش‌پرکردن بیمار پس از نمایش قالب — پنجره با زمینهٔ ویزیت باز می‌شود
        window.Loaded += async (_, _) => await vm.PrefillPatientAsync(_patientId);
        window.ShowDialog();
    }

    /// <summary>هسته ذخیره — ساخت DTO و POST</summary>
    private async Task<bool> SaveCoreAsync(bool silent, bool complete = false)
    {
        if (_suppressAutoSave)
        { if (!silent) _snackbar.Info("ذخیره قبلی هنوز در حال انجام است؛ پس از پایان دوباره تلاش کنید."); return false; }
        if (_creationResultUnknown)
        { if (!silent) _snackbar.Error("نتیجه ساخت ویزیت نامعلوم است؛ پیش از تکرار پرونده بیمار را بررسی کنید."); return false; }
        if (!_visitEditable || (_openedExisting && !_existingVisitLoaded))
        { if (!silent) _snackbar.Error("ویزیت کامل بارگذاری نشده یا تکمیل‌شده و فقط خواندنی است."); return false; }
        if (_patientId <= 0 || _doctorId <= 0)
        { if (!silent) _snackbar.Error("اطلاعات بیمار یا پزشک ویزیت آماده نیست؛ ذخیره انجام نشد."); return false; }
        if (!TryBuildVitals(out var vitals))
        {
            ErrorMessage = "علائم حیاتی نامعتبر است؛ اعداد و فشار خون را بررسی کنید.";
            SaveStateText = "⚠️ علائم حیاتی ذخیره نشد";
            if (!silent) _snackbar.Error(ErrorMessage);
            return false;
        }
        _suppressAutoSave = true;
        try
        {
            var body = new
            {
                id = _visitId,
                editVersion = _editVersion,
                patientId = _patientId,
                doctorId = _doctorId,
                receptionId = _receptionId,
                chiefComplaint = ChiefComplaint,
                historyOfPresentIllness = Hpi,
                pastMedicalHistory = Pmh,
                physicalExamination = BuildExamText(),
                soapSubjective = _existingVisitLoaded ? _soapSubjective : ChiefComplaint,
                soapObjective = _existingVisitLoaded ? _soapObjective : BuildExamText(),
                soapAssessment = _existingVisitLoaded ? _soapAssessment : PrimaryDiagnosis?.Title,
                soapPlan = _existingVisitLoaded ? _soapPlan : TreatmentPlan,
                primaryDiagnosisId = PrimaryDiagnosis?.Id == 0 ? null : PrimaryDiagnosis?.Id,
                treatmentPlan = TreatmentPlan,
                followUpDateShamsi = string.IsNullOrWhiteSpace(FollowUpDateShamsi) ? null : FollowUpDateShamsi.Trim(),
                complete,
                familyHistory = FamilyHistory,
                socialHistory = SocialHistory,
                reviewOfSystems = ReviewOfSystems,
                secondaryDiagnoses = _existingVisitLoaded && JsonSerializer.Serialize(SecondaryDiagnoses.ToList()) == _initialSecondaryRepresentation
                    ? _originalSecondaryDiagnoses : JsonSerializer.Serialize(SecondaryDiagnoses.ToList()),
                followUpInstructions = FollowUpInstructions,
                vitalSigns = vitals
            };

            var response = await _api.PostAsync<SaveVisitResponse>("visits/save-versioned", body);
            if (response?.Data is null || response.Data.Id <= 0 || string.IsNullOrWhiteSpace(response.Data.EditVersion))
            {
                if (_visitId is null) _creationResultUnknown = true;
                ErrorMessage = (_api.LastError ?? "پاسخ ذخیره ویزیت دریافت نشد") +
                    (_creationResultUnknown ? "؛ پیش از تکرار، وجود ویزیت را در پرونده بررسی کنید." : "؛ پیش از تکرار وضعیت ویزیت را بررسی کنید.");
                SaveStateText = "⚠️ نتیجه ذخیره نامعلوم یا ناموفق";
                _snackbar.Error(ErrorMessage);
                return false;
            }

            _visitId = response.Data.Id;
            _editVersion = response.Data.EditVersion;
            if (vitals is not null) _hasExistingVitals = true;
            SaveStateText = complete
                ? $"✅ تکمیل شد — {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToClockFa(IranClock.Now)}"
                : $"💾 ذخیره خودکار — {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToClockFa(IranClock.Now)}";
            if (!silent && !complete)
                _snackbar.Success("ویزیت ذخیره شد");
            return true;
        }
        catch (Exception)
        {
            if (_visitId is null) _creationResultUnknown = true;
            ErrorMessage = "پاسخ ذخیره ویزیت دریافت نشد؛ پیش از تکرار پرونده بیمار را بررسی کنید.";
            SaveStateText = "⚠️ نتیجه ذخیره نامعلوم";
            _snackbar.Error(ErrorMessage);
            return false;
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
        if (!string.IsNullOrWhiteSpace(PhysicalExamText)) parts.Add(PhysicalExamText);
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

    /// <summary>دریافت بدون گردکردن یا حذف خاموش نویسه‌ها.</summary>
    private bool TryBuildVitals(out Sahand.ClinicPro.Application.DTOs.Visits.VitalSignsInputDto? vitals)
    {
        vitals = null;
        int? sys = null, dia = null;
        if (!string.IsNullOrWhiteSpace(BpText))
        {
            var parts = BpText.Split('/');
            if (parts.Length != 2 || !TryInt(parts[0], out sys) || !TryInt(parts[1], out dia) || sys is null) return false;
        }
        if (!TryInt(HrText, out var hr) || !TryDecimal(TempText, out var temp) ||
            !TryInt(Spo2Text, out var spo2) || !TryDecimal(WeightText, out var weight) ||
            !TryDecimal(HeightText, out var height) || !TryInt(BsText, out var sugar)) return false;
        if (!_hasExistingVitals && sys is null && dia is null && hr is null && temp is null && spo2 is null && weight is null && height is null && sugar is null) return true;
        var result = new Sahand.ClinicPro.Application.DTOs.Visits.VitalSignsInputDto(sys, dia, hr, temp, spo2, weight, height, sugar);
        if (!Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit.VisitVitalsSafety.IsValid(result)) return false;
        vitals = result;
        return true;
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
        public int Status { get; set; }
        public string? EditVersion { get; set; }
        public string? PhysicalExamination { get; set; }
        public string? SoapSubjective { get; set; }
        public string? SoapObjective { get; set; }
        public string? SoapAssessment { get; set; }
        public string? SoapPlan { get; set; }
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
        // ═══ چرخهٔ ۷۸ — شناسه و عنوان تشخیص اصلی از سرور ═══ پیش‌تر فقط Code
        // می‌آمد و کلاینت جای‌نگهدار Id=0 می‌ساخت که در پایان ویزیت به تهی ترجمه
        // می‌شد و اعتبارسنج سرور رد می‌کرد («تشخیص اصلی الزامی است»)
        public long? PrimaryDiagnosisId { get; set; }
        public string? PrimaryDiagnosisTitle { get; set; }
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
        public SavedVisit? Data { get; set; }
    }
    public sealed class SavedVisit
    {
        public long Id { get; set; }
        public string? EditVersion { get; set; }
    }
}

/// <summary>ردیف سابقه ویزیت در سایدبار</summary>
public record HistoryRow(long Id, string DateShamsi, string DoctorName, string Complaint);
