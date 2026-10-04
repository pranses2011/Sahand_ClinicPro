using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف ویزیت — منطبق با VisitListDto سرور</summary>
public record VisitRow(
    long Id,
    string? Code,
    string PatientName,
    string DoctorName,
    string Date,
    string StatusText,
    string? ChiefComplaint);

/// <summary>
/// ViewModel ویزیت‌ها — جستجوی بیمار و تاریخچه ویزیت‌ها
/// </summary>
public partial class VisitsViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "ویزیت و معاینه";

    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجو</summary>
    [ObservableProperty]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientOption? _selectedPatient;

    /// <summary>ویزیت‌های بیمار</summary>
    [ObservableProperty]
    private ObservableCollection<VisitRow> _visits = new();

    /// <summary>ویزیت انتخاب‌شده</summary>
    [ObservableProperty]
    private VisitRow? _selectedVisit;

    /// <summary>پزشکان — برای شروع ویزیت جدید</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public VisitsViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadDoctorsAsync();
    }

    /// <summary>بارگذاری پزشکان</summary>
    private async Task LoadDoctorsAsync()
    {
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName));
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

    /// <summary>با انتخاب بیمار — تاریخچه ویزیت‌ها بارگذاری می‌شود</summary>
    partial void OnSelectedPatientChanged(PatientOption? value)
    {
        if (value is not null)
            _ = LoadVisitsAsync();
    }

    /// <summary>بارگذاری ویزیت‌های بیمار انتخاب‌شده</summary>
    [RelayCommand]
    public async Task LoadVisitsAsync()
    {
        if (SelectedPatient is null) return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        var response = await _api.GetAsync<VisitsResponse>(
            $"visits/patient/{SelectedPatient.Id}");

        if (response?.Data is not null)
        {
            Visits = new ObservableCollection<VisitRow>(response.Data.Select(v => new VisitRow(
                v.Id, v.VisitCode, v.PatientName, v.DoctorName, v.DateShamsi,
                StatusTextOf(v.Status), v.ChiefComplaint)));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت ویزیت‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>متن فارسی وضعیت ویزیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "شروع‌شده",
        2 => "در جریان",
        3 => "تکمیل‌شده",
        4 => "پیش‌نویس",
        _ => "نامشخص"
    };

    /// <summary>شروع ویزیت جدید برای بیمار انتخاب‌شده</summary>
    [RelayCommand]
    private void NewVisit()
    {
        if (SelectedPatient is null) return;

        var vm = new VisitEditViewModel(_api, Doctors, SelectedPatient.Id,
            SelectedPatient.DisplayName);
        var window = new VisitWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadVisitsAsync();
    }

    /// <summary>بازکردن ویزیت انتخاب‌شده برای ادامه ثبت</summary>
    [RelayCommand]
    private void OpenVisit()
    {
        if (SelectedVisit is null) return;

        var vm = new VisitEditViewModel(_api, Doctors, SelectedVisit.Id);
        var window = new VisitWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadVisitsAsync();
    }

    /// <summary>پاسخ فهرست ویزیت‌ها</summary>
    public sealed class VisitsResponse
    {
        public bool IsSuccess { get; set; }
        public List<VisitDto>? Data { get; set; }
    }

    /// <summary>DTO ویزیت منطبق بر سرور</summary>
    public sealed class VisitDto
    {
        public long Id { get; set; }
        public string? VisitCode { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? ChiefComplaint { get; set; }
    }
}