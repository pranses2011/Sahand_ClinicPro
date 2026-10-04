using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم ثبت پذیرش — حضوری یا مرتبط با نوبت
/// </summary>
public partial class ReceptionFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجو</summary>
    [ObservableProperty]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientOption? _selectedPatient;

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>نمایه پزشک</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>بیمه‌ها</summary>
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>نمایه بیمه</summary>
    [ObservableProperty]
    private int _insuranceIndex = -1;

    /// <summary>شناسه نوبت مرتبط — اختیاری</summary>
    [ObservableProperty]
    private string? _appointmentId;

    /// <summary>کد معرفی‌نامه</summary>
    [ObservableProperty]
    private string? _referralCode;

    /// <summary>یادداشت</summary>
    [ObservableProperty]
    private string? _notes;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده با کلاینت API و پزشکان موجود</summary>
    public ReceptionFormViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors)
    {
        _api = api;
        foreach (var d in doctors)
            Doctors.Add(d);
        _ = LoadInsuranceAsync();
    }

    /// <summary>بارگذاری بیمه‌ها از داده مرجع</summary>
    private async Task LoadInsuranceAsync()
    {
        var response = await _api.GetAsync<InsuranceResponse>("lookups/insurance-companies");
        if (response?.Data is not null)
            foreach (var c in response.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));
    }

    /// <summary>جستجوی بیمار</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(PatientSearchTerm) || PatientSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<AppointmentFormViewModel.PatientsSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearchTerm.Trim())}");

        PatientResults = response?.Data is not null
            ? new ObservableCollection<PatientOption>(response.Data.Select(p =>
                new PatientOption(p.Id,
                    $"{p.FirstName} {p.LastName} — پرونده {p.MedicalRecordNo}",
                    $"کد ملی {p.NationalCode} — موبایل {p.Mobile}")))
            : new ObservableCollection<PatientOption>();
    }

    /// <summary>ذخیره پذیرش</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedPatient is null)
        {
            ErrorMessage = "انتخاب بیمار الزامی است";
            return;
        }
        if (DoctorIndex < 0 || DoctorIndex >= Doctors.Count)
        {
            ErrorMessage = "انتخاب پزشک الزامی است";
            return;
        }

        long? appointmentId = null;
        if (!string.IsNullOrWhiteSpace(AppointmentId)
            && (!long.TryParse(AppointmentId.Trim(), out var parsed) || parsed <= 0))
        {
            ErrorMessage = "شناسه نوبت باید عدد باشد";
            return;
        }
        else if (!string.IsNullOrWhiteSpace(AppointmentId))
        {
            appointmentId = long.Parse(AppointmentId.Trim());
        }

        IsBusy = true;
        var body = new
        {
            patientId = SelectedPatient.Id,
            doctorId = Doctors[DoctorIndex].Id,
            appointmentId,
            insuranceCompanyId = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count
                ? InsuranceCompanies[InsuranceIndex].Id
                : (long?)null,
            referralCode = string.IsNullOrWhiteSpace(ReferralCode) ? null : ReferralCode.Trim(),
            notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        var ok = await _api.PostAsync("receptions", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت پذیرش ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاسخ بیمه‌های مرجع</summary>
    public sealed class InsuranceResponse
    {
        public bool IsSuccess { get; set; }
        public List<CompanyDto>? Data { get; set; }
    }

    /// <summary>DTO شرکت بیمه</summary>
    public sealed class CompanyDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}