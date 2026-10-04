using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه آزمایش قابل انتخاب</summary>
public partial class LabTestOptionRow : ObservableObject
{
    /// <summary>شناسه آزمایش</summary>
    public long Id { get; init; }

    /// <summary>نام</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>دسته</summary>
    public string? Category { get; init; }

    /// <summary>انتخاب‌شده</summary>
    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// ViewModel فرم درخواست آزمایش — بیمار + پزشک + انتخاب تست‌ها
/// </summary>
public partial class LabOrderFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>جستجوی بیمار</summary>
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

    /// <summary>نمایه اولویت</summary>
    [ObservableProperty]
    private int _priorityIndex = 2;

    /// <summary>اطلاعات بالینی</summary>
    [ObservableProperty]
    private string? _clinicalInfo;

    /// <summary>فیلتر نام آزمایش</summary>
    [ObservableProperty]
    private string _testSearchTerm = string.Empty;

    /// <summary>آزمایش‌های قابل انتخاب</summary>
    public ObservableCollection<LabTestOptionRow> Tests { get; } = new();

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

    /// <summary>سازنده با کلاینت API و پزشکان</summary>
    public LabOrderFormViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors)
    {
        _api = api;
        foreach (var d in doctors)
            Doctors.Add(d);
        _ = LoadTestsAsync();
    }

    /// <summary>بارگذاری آزمایش‌های فعال</summary>
    private async Task LoadTestsAsync()
    {
        var response = await _api.GetAsync<LabTestsResponse>("lookups/lab-tests");
        if (response?.Data is not null)
            foreach (var t in response.Data)
                Tests.Add(new LabTestOptionRow
                {
                    Id = t.Id, Name = t.Name, Category = t.Category
                });
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

    /// <summary>ثبت درخواست آزمایش</summary>
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

        var selectedIds = Tests.Where(t => t.IsSelected).Select(t => t.Id).ToList();
        if (selectedIds.Count == 0)
        {
            ErrorMessage = "انتخاب حداقل یک آزمایش الزامی است";
            return;
        }

        IsBusy = true;
        var body = new
        {
            patientId = SelectedPatient.Id,
            doctorId = Doctors[DoctorIndex].Id,
            visitId = (long?)null,
            testIds = selectedIds,
            priority = PriorityIndex + 1,
            clinicalInfo = string.IsNullOrWhiteSpace(ClinicalInfo) ? null : ClinicalInfo.Trim()
        };

        var ok = await _api.PostAsync("lab/orders", body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت درخواست ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاسخ آزمایش‌های مرجع</summary>
    public sealed class LabTestsResponse
    {
        public bool IsSuccess { get; set; }
        public List<LabTestDto>? Data { get; set; }
    }

    /// <summary>DTO آزمایش</summary>
    public sealed class LabTestDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
    }
}