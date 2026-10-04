using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// مدل نمایش بیمار در فهرست — منطبق با PatientListDto سرور
/// </summary>
public record PatientRow(
    long Id,
    string MedicalRecordNo,
    string FirstName,
    string LastName,
    string NationalCode,
    string Mobile,
    string? InsuranceName);

/// <summary>
/// مدل کمکی بیمه در ComboBox
/// </summary>
public record InsuranceOption(long Id, string Name);

/// <summary>
/// ViewModel فهرست بیماران — جستجو و صفحه‌بندی سمت سرور
/// </summary>
public partial class PatientsViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مدیریت بیماران";

    /// <summary>فهرست بیماران صفحه فعلی</summary>
    [ObservableProperty]
    private ObservableCollection<PatientRow> _patients = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientRow? _selectedPatient;

    /// <summary>عبارت جستجو</summary>
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    /// <summary>شماره صفحه فعلی</summary>
    [ObservableProperty]
    private int _pageNumber = 1;

    /// <summary>تعداد کل صفحات</summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>تعداد کل بیماران</summary>
    [ObservableProperty]
    private int _totalCount;

    private int _pageSize = 20;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public PatientsViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری صفحه فعلی از سرور</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = $"patients?page={PageNumber}&pageSize={_pageSize}" +
                       (!string.IsNullOrWhiteSpace(SearchTerm)
                           ? $"&search={Uri.EscapeDataString(SearchTerm.Trim())}"
                           : string.Empty);

        var response = await _api.GetAsync<PatientsPage>(endpoint);

        if (response is not null)
        {
            Patients = new ObservableCollection<PatientRow>(
                response.Data?.Select(MapRow) ?? Enumerable.Empty<PatientRow>());
            PageNumber = response.PageNumber;
            TotalPages = response.TotalPages;
            TotalCount = response.TotalCount;
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت فهرست بیماران ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف پاسخ سرور به مدل نمایش</summary>
    private static PatientRow MapRow(PatientDto p) =>
        new(p.Id, p.MedicalRecordNo, p.FirstName, p.LastName,
            p.NationalCode, p.Mobile, p.InsuranceName);

    /// <summary>جستجو — بازگشت به صفحه اول</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>صفحه بعد</summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber < TotalPages)
        {
            PageNumber++;
            await LoadAsync();
        }
    }

    /// <summary>صفحه قبل</summary>
    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
            await LoadAsync();
        }
    }

    /// <summary>بازکردن فرم ثبت بیمار جدید</summary>
    [RelayCommand]
    private void AddPatient()
    {
        OpenForm(new PatientFormViewModel(_api, null));
    }

    /// <summary>بازکردن فرم ویرایش بیمار انتخاب‌شده</summary>
    [RelayCommand]
    private void EditPatient()
    {
        if (SelectedPatient is null) return;
        OpenForm(new PatientFormViewModel(_api, SelectedPatient.Id));
    }

    /// <summary>مشاهده جزئیات — فعلاً همان فرم ویرایش</summary>
    [RelayCommand]
    private void ViewPatient()
    {
        if (SelectedPatient is null) return;
        OpenForm(new PatientFormViewModel(_api, SelectedPatient.Id));
    }

    /// <summary>بازکردن پنجره فرم به‌صورت مودال و بارگذاری مجدد پس از بستن</summary>
    private void OpenForm(PatientFormViewModel formVm)
    {
        var window = new PatientFormWindow(formVm) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadAsync(); // بارگذاری مجدد پس از ذخیره احتمالی
    }

    /// <summary>ساختار پاسخ صفحه بیماران — منطبق بر PagedResponse<PatientListDto></summary>
    public sealed class PatientsPage
    {
        public bool IsSuccess { get; set; }
        public List<PatientDto>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }

    /// <summary>DTO بیمار منطبق بر سرور</summary>
    public sealed class PatientDto
    {
        public long Id { get; set; }
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? InsuranceName { get; set; }
    }
}

/// <summary>
/// ViewModel فرم ثبت و ویرایش بیمار
/// </summary>
public partial class PatientFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long? _patientId;

    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _nationalCode = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string? _fatherName;
    [ObservableProperty] private string? _birthDateShamsi;
    [ObservableProperty] private int _genderIndex = 0;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private long? _insuranceCompanyId;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _formTitle = "ثبت بیمار جدید";

    /// <summary>گزینه‌های بیمه برای ComboBox</summary>
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>رویداد درخواست بستن پنجره</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده — با شناسه بیمار برای ویرایش و تهی برای ثبت</summary>
    public PatientFormViewModel(IApiClient api, long? patientId)
    {
        _api = api;
        _patientId = patientId;
        if (patientId.HasValue) FormTitle = "ویرایش اطلاعات بیمار";
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری بیمه‌ها و در ویرایش داده بیمار</summary>
    private async Task InitializeAsync()
    {
        // فهرست بیمه‌ها — endpoint عمومی در گام‌های بعد؛ فعلاً خالی می‌ماند
        if (_patientId.HasValue)
        {
            var patient = await _api.GetAsync<PatientDetail>($"patients/{_patientId}");
            if (patient is not null)
            {
                FirstName = patient.FirstName;
                LastName = patient.LastName;
                NationalCode = patient.NationalCode;
                Mobile = patient.Mobile;
                FatherName = patient.FatherName;
                BirthDateShamsi = patient.BirthDateShamsi;
                GenderIndex = patient.Gender == 2 ? 1 : 0;
                Address = patient.Address;
                City = patient.City;
                InsuranceCompanyId = patient.InsuranceCompanyId;
            }
            else
            {
                ErrorMessage = _api.LastError ?? "دریافت اطلاعات بیمار ناموفق بود";
            }
        }
    }

    /// <summary>ذخیره — ثبت یا ویرایش بر اساس حالت فرم</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
            || string.IsNullOrWhiteSpace(NationalCode) || string.IsNullOrWhiteSpace(Mobile))
        {
            ErrorMessage = "نام و نام خانوادگی و کد ملی و موبایل الزامی است";
            return;
        }

        IsBusy = true;

        var body = new
        {
            firstName = FirstName.Trim(),
            lastName = LastName.Trim(),
            nationalCode = NationalCode.Trim(),
            mobile = Mobile.Trim(),
            fatherName = FatherName,
            birthDateShamsi = BirthDateShamsi,
            gender = GenderIndex + 1,
            address = Address,
            city = City,
            insuranceCompanyId = InsuranceCompanyId
        };

        var ok = _patientId.HasValue
            ? await _api.PutAsync($"patients/{_patientId}", body)
            : await _api.PostAsync("patients", body);

        IsBusy = false;

        if (ok)
        {
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
        }
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>DTO جزئیات بیمار — منطبق بر سرور</summary>
    public sealed class PatientDetail
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? BirthDateShamsi { get; set; }
        public int Gender { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public long? InsuranceCompanyId { get; set; }
    }
}
