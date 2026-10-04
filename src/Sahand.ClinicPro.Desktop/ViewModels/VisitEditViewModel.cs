using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه تشخیص ICD-10 در نتایج جستجو</summary>
public record DiagnosisOption(long Id, string Code, string Title);

/// <summary>
/// ViewModel ویرایشگر ویزیت — الگوی SOAP چهاربخشی + تشخیص ICD-10
/// دو حالت: ویزیت جدید (patientId داده شده) یا ادامه ویزیت موجود (visitId)
/// </summary>
public partial class VisitEditViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long? _existingVisitId;
    private long _patientId;
    private long _doctorId;
    private long? _receptionId;

    /// <summary>حالت ویزیت جدید است</summary>
    public bool IsNewMode => _existingVisitId is null;

    /// <summary>عنوان پنجره</summary>
    [ObservableProperty]
    private string _formTitle = "ثبت ویزیت جدید";

    /// <summary>سرصفحه بیمار</summary>
    [ObservableProperty]
    private string _patientHeader = string.Empty;

    /// <summary>نمایه پزشک — فقط حالت جدید</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>نمایه نوع ویزیت</summary>
    [ObservableProperty]
    private int _typeIndex;

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    // ── بخش‌های SOAP ──
    /// <summary>شکایت اصلی</summary>
    [ObservableProperty]
    private string? _chiefComplaint;

    /// <summary>تاریخچه بیماری فعلی — ذهنی</summary>
    [ObservableProperty]
    private string? _soapSubjective;

    /// <summary>معاینه و یافته‌ها — عینی</summary>
    [ObservableProperty]
    private string? _soapObjective;

    /// <summary>ارزیابی و تشخیص</summary>
    [ObservableProperty]
    private string? _soapAssessment;

    /// <summary>برنامه درمان</summary>
    [ObservableProperty]
    private string? _soapPlan;

    /// <summary>سابقه پزشکی گذشته</summary>
    [ObservableProperty]
    private string? _pastMedicalHistory;

    /// <summary>معاینه فیزیکی</summary>
    [ObservableProperty]
    private string? _physicalExamination;

    /// <summary>برنامه درمانی کلی</summary>
    [ObservableProperty]
    private string? _treatmentPlan;

    /// <summary>تاریخ مراجعه بعدی شمسی</summary>
    [ObservableProperty]
    private string? _followUpDateShamsi;

    // ── تشخیص ICD-10 ──
    /// <summary>عبارت جستجوی تشخیص</summary>
    [ObservableProperty]
    private string _diagnosisSearchTerm = string.Empty;

    /// <summary>نتایج جستجوی تشخیص</summary>
    [ObservableProperty]
    private ObservableCollection<DiagnosisOption> _diagnosisResults = new();

    /// <summary>تشخیص انتخاب‌شده</summary>
    [ObservableProperty]
    private DiagnosisOption? _selectedDiagnosis;

    /// <summary>شناسه تشخیص اصلی</summary>
    [ObservableProperty]
    private long? _primaryDiagnosisId;

    /// <summary>پیام وضعیت (ذخیره موفق)</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد تکمیل موفق ویزیت</summary>
    public event EventHandler? Completed;

    /// <summary>
    /// سازنده حالت ویزیت جدید — با بیمار و پزشکان
    /// </summary>
    public VisitEditViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors,
        long patientId, string patientDisplayName)
    {
        _api = api;
        _patientId = patientId;
        PatientHeader = patientDisplayName;
        foreach (var d in doctors)
            Doctors.Add(d);
    }

    /// <summary>
    /// سازنده حالت ادامه ویزیت موجود — با شناسه ویزیت
    /// </summary>
    public VisitEditViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors,
        long visitId)
    {
        _api = api;
        _existingVisitId = visitId;
        FormTitle = "ادامه ثبت ویزیت";
        _ = LoadExistingAsync();
    }

    /// <summary>بارگذاری ویزیت موجود و پرکردن فیلدها</summary>
    private async Task LoadExistingAsync()
    {
        IsBusy = true;
        var detail = await _api.GetAsync<VisitDetail>($"visits/{_existingVisitId}");

        if (detail is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت ویزیت ناموفق بود";
            IsBusy = false;
            return;
        }

        _patientId = detail.PatientId;
        _doctorId = detail.DoctorId;
        _receptionId = null;
        PatientHeader = $"{detail.PatientId} — {detail.DateShamsi} ({detail.VisitCode ?? "بدون کد"})";
        ChiefComplaint = detail.ChiefComplaint;
        SoapSubjective = detail.SoapSubjective;
        SoapObjective = detail.SoapObjective;
        SoapAssessment = detail.SoapAssessment;
        SoapPlan = detail.SoapPlan;
        PastMedicalHistory = detail.PastMedicalHistory;
        PhysicalExamination = detail.PhysicalExamination;
        TreatmentPlan = detail.TreatmentPlan;
        FollowUpDateShamsi = detail.FollowUpDateShamsi;
        PrimaryDiagnosisId = detail.PrimaryDiagnosisId;
        if (detail.PrimaryDiagnosisId.HasValue && !string.IsNullOrEmpty(detail.PrimaryDiagnosisCode))
        {
            SelectedDiagnosis = new DiagnosisOption(detail.PrimaryDiagnosisId.Value,
                detail.PrimaryDiagnosisCode ?? string.Empty,
                detail.PrimaryDiagnosisCode ?? string.Empty);
            DiagnosisResults.Add(SelectedDiagnosis);
        }

        IsBusy = false;
    }

    /// <summary>جستجوی تشخیص ICD-10</summary>
    [RelayCommand]
    private async Task SearchDiagnosisAsync()
    {
        if (string.IsNullOrWhiteSpace(DiagnosisSearchTerm) || DiagnosisSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<DiagnosisResponse>(
            $"lookups/diagnoses?search={Uri.EscapeDataString(DiagnosisSearchTerm.Trim())}&take=25");

        DiagnosisResults = response?.Data is not null
            ? new ObservableCollection<DiagnosisOption>(response.Data.Select(d =>
                new DiagnosisOption(d.Id, d.Code, d.Title)))
            : new ObservableCollection<DiagnosisOption>();
    }

    /// <summary>با انتخاب تشخیص — شناسه آن ثبت می‌شود</summary>
    partial void OnSelectedDiagnosisChanged(DiagnosisOption? value)
    {
        PrimaryDiagnosisId = value?.Id;
    }

    /// <summary>ذخیره پیش‌نویس — بدون الزام تشخیص</summary>
    [RelayCommand]
    private Task SaveDraftAsync() => SaveAsync(complete: false);

    /// <summary>تکمیل ویزیت — تشخیص اصلی الزامی است</summary>
    [RelayCommand]
    private Task CompleteVisitAsync() => SaveAsync(complete: true);

    /// <summary>ارسال ذخیره به سرور</summary>
    private async Task SaveAsync(bool complete)
    {
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        if (_patientId <= 0)
        {
            ErrorMessage = "بیمار مشخص نیست";
            return;
        }
        if (IsNewMode && (DoctorIndex < 0 || DoctorIndex >= Doctors.Count))
        {
            ErrorMessage = "انتخاب پزشک الزامی است";
            return;
        }
        if (complete && PrimaryDiagnosisId is null)
        {
            ErrorMessage = "برای تکمیل ویزیت انتخاب تشخیص اصلی (ICD-10) الزامی است";
            return;
        }
        if (!string.IsNullOrWhiteSpace(FollowUpDateShamsi) && FollowUpDateShamsi.Trim().Length != 10)
        {
            ErrorMessage = "تاریخ مراجعه بعدی را با قالب 1404/01/15 وارد کنید";
            return;
        }

        var doctorId = IsNewMode ? Doctors[DoctorIndex].Id : _doctorId;

        IsBusy = true;
        var body = new
        {
            id = _existingVisitId,
            patientId = _patientId,
            doctorId,
            receptionId = _receptionId,
            type = TypeIndex + 1,
            chiefComplaint = NullIfEmpty(ChiefComplaint),
            historyOfPresentIllness = NullIfEmpty(SoapSubjective),
            pastMedicalHistory = NullIfEmpty(PastMedicalHistory),
            physicalExamination = NullIfEmpty(PhysicalExamination),
            soapSubjective = NullIfEmpty(SoapSubjective),
            soapObjective = NullIfEmpty(SoapObjective),
            soapAssessment = NullIfEmpty(SoapAssessment),
            soapPlan = NullIfEmpty(SoapPlan),
            primaryDiagnosisId = PrimaryDiagnosisId,
            treatmentPlan = NullIfEmpty(TreatmentPlan),
            followUpDateShamsi = NullIfEmpty(FollowUpDateShamsi),
            complete
        };

        var ok = await _api.PostAsync("visits", body);
        IsBusy = false;

        if (!ok)
        {
            ErrorMessage = _api.LastError ?? "ذخیره ویزیت ناموفق بود";
            return;
        }

        if (complete)
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            StatusMessage = "پیش‌نویس ویزیت ذخیره شد";
            if (IsNewMode)
                FormTitle = "ادامه ثبت ویزیت";
        }
    }

    /// <summary>تبدیل رشته خالی به null برای بدنه درخواست</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>پاسخ جستجوی تشخیص</summary>
    public sealed class DiagnosisResponse
    {
        public bool IsSuccess { get; set; }
        public List<DiagnosisDto>? Data { get; set; }
    }

    /// <summary>DTO تشخیص</summary>
    public sealed class DiagnosisDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>جزئیات ویزیت — منطبق بر VisitDetailDto سرور</summary>
    public sealed class VisitDetail
    {
        public long Id { get; set; }
        public string? VisitCode { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Status { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? HistoryOfPresentIllness { get; set; }
        public string? PastMedicalHistory { get; set; }
        public string? PhysicalExamination { get; set; }
        public string? SoapSubjective { get; set; }
        public string? SoapObjective { get; set; }
        public string? SoapAssessment { get; set; }
        public string? SoapPlan { get; set; }
        public long? PrimaryDiagnosisId { get; set; }
        public string? PrimaryDiagnosisCode { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? FollowUpDateShamsi { get; set; }
    }
}