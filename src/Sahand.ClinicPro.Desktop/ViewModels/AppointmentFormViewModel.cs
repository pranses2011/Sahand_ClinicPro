using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه بیمار در نتیجه جستجو</summary>
public record PatientOption(long Id, string DisplayName, string Detail);

/// <summary>
/// ViewModel فرم ثبت نوبت — جستجوی بیمار + انتخاب پزشک + تاریخ و ساعت
/// </summary>
public partial class AppointmentFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجوی بیمار</summary>
    [ObservableProperty]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientOption? _selectedPatient;

    /// <summary>پزشکان فعال</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>نمایه پزشک انتخاب‌شده</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>تاریخ شمسی نوبت</summary>
    [ObservableProperty]
    private string _dateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow);

    /// <summary>ساعت نوبت — قالب HH:mm</summary>
    [ObservableProperty]
    private string _time = "09:00";

    /// <summary>نمایه نوع نوبت — ۰ اولیه، ۱ پیگیری، ۲ اورژانسی</summary>
    [ObservableProperty]
    private int _typeIndex;

    /// <summary>نوبت‌گیری نخستین مراجعه است</summary>
    [ObservableProperty]
    private bool _isFirstVisit;

    /// <summary>یادداشت</summary>
    [ObservableProperty]
    private string? _notes;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده با کلاینت API و فهرست پزشکان موجود</summary>
    public AppointmentFormViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors)
    {
        _api = api;
        foreach (var d in doctors)
            Doctors.Add(d);
    }

    /// <summary>جستجوی بیمار — حداقل ۲ نویسه</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(PatientSearchTerm) || PatientSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<PatientsSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearchTerm.Trim())}");

        PatientResults = response?.Data is not null
            ? new ObservableCollection<PatientOption>(response.Data.Select(p =>
                new PatientOption(p.Id,
                    $"{p.FirstName} {p.LastName} — پرونده {p.MedicalRecordNo}",
                    $"کد ملی {p.NationalCode} — موبایل {p.Mobile}")))
            : new ObservableCollection<PatientOption>();
    }

    /// <summary>ذخیره نوبت</summary>
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
        if (string.IsNullOrWhiteSpace(DateShamsi) || DateShamsi.Trim().Length != 10)
        {
            ErrorMessage = "تاریخ شمسی معتبر وارد کنید (مثال 1404/01/15)";
            return;
        }
        if (!TimeOnly.TryParse(Time, out _))
        {
            ErrorMessage = "ساعت را با قالب HH:mm وارد کنید (مثال 09:30)";
            return;
        }

        IsBusy = true;
        var body = new
        {
            patientId = SelectedPatient.Id,
            doctorId = Doctors[DoctorIndex].Id,
            dateShamsi = DateShamsi.Trim(),
            time = Time.Trim(),
            type = TypeIndex + 1,
            source = 1,
            isFirstVisit = IsFirstVisit,
            notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        var ok = await _api.PostAsync("appointments", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت نوبت ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاسخ جستجوی بیمار</summary>
    public sealed class PatientsSearchResponse
    {
        public bool IsSuccess { get; set; }
        public List<PatientSearchDto>? Data { get; set; }
    }

    /// <summary>DTO جستجوی بیمار</summary>
    public sealed class PatientSearchDto
    {
        public long Id { get; set; }
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }
}