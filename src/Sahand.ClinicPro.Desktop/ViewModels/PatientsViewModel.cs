using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف بیمار در فهرست — گام U2: ۹ ستون طبق پرامپت UI بخش ۱ (فرم ۳)
/// شامل سن و آخرین مراجعه شمسی و آیتم انتخاب چندتایی
/// </summary>
public record PatientRow(
    long Id,
    string MedicalRecordNo,
    string FullName,
    string NationalCode,
    string Mobile,
    string? InsuranceName,
    int? Age,
    string? LastVisitDateShamsi,
    bool IsSelected);

/// <summary>
/// مدل کمکی بیمه در ComboBox
/// </summary>
public record InsuranceOption(long Id, string Name);

/// <summary>
/// ViewModel فهرست بیماران — گام U2: نوار ابزار کامل + Badge تعداد +
/// انتخاب چندتایی + منوی راست‌کلیک + حذف با تأیید + صفحه‌بندی با PaginationBar
/// </summary>
public partial class PatientsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly ITokenManager _tokens;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مدیریت بیماران";

    /// <summary>فهرست بیماران صفحه فعلی</summary>
    [ObservableProperty]
    private ObservableCollection<PatientRow> _patients = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private PatientRow? _selectedPatient;

    /// <summary>عبارت جستجو — با Debounce سیستمی Enter یا دکمه</summary>
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    /// <summary>شماره صفحه فعلی</summary>
    [ObservableProperty]
    private int _pageNumber = 1;

    /// <summary>تعداد کل صفحات</summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>تعداد کل بیماران — برای Badge «کل: N نفر»</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>تعداد رکورد در صفحه — قابل تغییر از PaginationBar</summary>
    [ObservableProperty]
    private int _pageSize = 20;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public PatientsViewModel(IApiClient api, ISnackbarService snackbar, ITokenManager tokens)
    {
        _api = api;
        _snackbar = snackbar;
        _tokens = tokens;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری صفحه فعلی از سرور</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = $"patients?page={PageNumber}&pageSize={PageSize}" +
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
        new(p.Id, p.MedicalRecordNo,
            $"{p.FirstName} {p.LastName}".Trim(),
            p.NationalCode, p.Mobile, p.InsuranceName,
            null, p.LastVisitDateShamsi, false);

    /// <summary>جستجو — بازگشت به صفحه اول (میانبر: Enter یا دکمه)</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>بارگذاری مجدد (F5)</summary>
    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    /// <summary>تغییر صفحه از PaginationBar — پارامتر شماره صفحه</summary>
    [RelayCommand]
    private async Task PageChangedAsync(object? page)
    {
        if (page is int p && p >= 1)
            PageNumber = p;
        await LoadAsync();
    }

    /// <summary>بازکردن فرم ثبت بیمار جدید (میانبر F3)</summary>
    [RelayCommand]
    private void AddPatient()
        => OpenForm(new PatientFormViewModel(_api, null, afterSave: null));

    /// <summary>ویرایش بیمار انتخاب‌شده</summary>
    [RelayCommand]
    private void EditPatient()
    {
        if (SelectedPatient is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }
        OpenForm(new PatientFormViewModel(_api, SelectedPatient.Id, afterSave: null));
    }

    /// <summary>مشاهده جزئیات — پنجره پرونده بیمار</summary>
    [RelayCommand]
    private void ViewPatient()
    {
        if (SelectedPatient is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }
        var window = new PatientDetailWindow(new PatientDetailViewModel(_api, SelectedPatient.Id))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>رزرو نوبت برای بیمار انتخاب‌شده</summary>
    [RelayCommand]
    private async Task BookAppointmentAsync()
    {
        if (SelectedPatient is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }

        // نوبت‌دهی از طریق صفحه نوبت‌ها با جستجوی کد ملی
        var form = new AppointmentFormViewModel(_api, new ObservableCollection<DoctorOption>());
        var window = new AppointmentFormWindow(form) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _snackbar.Info($"برای «{SelectedPatient.FullName}» نوبت را با جستجوی کد ملی ثبت کنید");
    }

    /// <summary>حذف بیمار انتخاب‌شده — با دیالوگ تأیید طبق قوانین کلی</summary>
    [RelayCommand]
    private async Task DeletePatientAsync()
    {
        if (SelectedPatient is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }

        var name = SelectedPatient.FullName;
        var confirmed = System.Windows.MessageBox.Show(
            $"آیا از حذف بیمار «{name}» مطمئن هستید؟\nاین عملیات حذف نرم است و قابل بازیابی توسط مدیر سیستم است.",
            "تأیید حذف بیمار",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;

        if (!confirmed) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"patients/{SelectedPatient.Id}");
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success($"بیمار «{name}» حذف شد");
            await LoadAsync();
        }
        else
        {
            _snackbar.Error(_api.LastError ?? "حذف بیمار ناموفق بود");
        }
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
        public string? LastVisitDateShamsi { get; set; }
    }
}

/// <summary>
/// ViewModel فرم ثبت و ویرایش بیمار — گام U2: ۵ تب کامل +
/// بررسی خودکار یکتایی کد ملی + TagInput سوابق پزشکی + سه دکمه ذخیره
/// </summary>
public partial class PatientFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long? _patientId;
    private readonly Action? _afterSave;
    private bool _nationalCodeChecked;

    /* ── تب ۱: اطلاعات اصلی ── */
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _nationalCode = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string? _fatherName;
    [ObservableProperty] private string? _birthDateShamsi;
    [ObservableProperty] private int _genderIndex;
    [ObservableProperty] private string? _bloodType;
    [ObservableProperty] private string? _maritalStatus;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _province;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _postalCode;
    [ObservableProperty] private string? _occupation;
    [ObservableProperty] private string? _address;

    /* ── تب ۲: اطلاعات بیمه ── */
    [ObservableProperty] private long? _insuranceCompanyId;
    [ObservableProperty] private string? _insuranceNumber;
    [ObservableProperty] private string? _insuranceTypeName;

    /* ── تب ۴: تماس اضطراری ── */
    [ObservableProperty] private string? _emergencyContactName;
    [ObservableProperty] private string? _emergencyContactPhone;
    [ObservableProperty] private string? _emergencyContactRelation;

    /* ── عمومی ── */
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _formTitle = "ثبت بیمار جدید";
    [ObservableProperty] private bool _isNationalCodeValid;

    /// <summary>گزینه‌های بیمه برای ComboBox</summary>
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>گزینه‌های گروه خونی</summary>
    public List<string> BloodTypes { get; } = new() { "نامشخص", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    /// <summary>گزینه‌های وضعیت تأهل</summary>
    public List<string> MaritalStates { get; } = new() { "مجرد", "متأهل", "بیوه", "مطلقه" };

    /// <summary>گزینه‌های نوع بیمه</summary>
    public List<string> InsuranceTypes { get; } = new() { "اصلی", "تبعی ۱", "تبعی ۲" };

    /// <summary>تگ‌های آلرژی — قرمز</summary>
    public ObservableCollection<string> Allergies { get; } = new();

    /// <summary>تگ‌های بیماری مزمن — نارنجی</summary>
    public ObservableCollection<string> ChronicDiseases { get; } = new();

    /// <summary>تگ‌های داروهای فعلی — آبی</summary>
    public ObservableCollection<string> CurrentMedications { get; } = new();

    /// <summary>رویداد درخواست بستن پنجره</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده — با شناسه بیمار برای ویرایش و تهی برای ثبت</summary>
    public PatientFormViewModel(IApiClient api, long? patientId, Action? afterSave = null)
    {
        _api = api;
        _patientId = patientId;
        _afterSave = afterSave;
        if (patientId.HasValue) FormTitle = "ویرایش اطلاعات بیمار";
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری بیمه‌ها و در ویرایش داده بیمار</summary>
    private async Task InitializeAsync()
    {
        // فهرست بیمه‌ها از داده مرجع — گام V2
        var companies = await _api.GetAsync<LookupCompanies>("lookups/insurance-companies");
        if (companies?.Data is not null)
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));
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
                BloodType = patient.BloodType;
                Phone = patient.Phone;
                Email = patient.Email;
                Province = patient.Province;
                City = patient.City;
                Address = patient.Address;
                InsuranceCompanyId = patient.InsuranceCompanyId;
                InsuranceNumber = patient.InsuranceNumber;
                FormTitle = $"ویرایش بیمار: {patient.FirstName} {patient.LastName}";

                // سوابق پزشکی از JSON
                FillTags(Allergies, patient.Allergies);
                FillTags(ChronicDiseases, patient.ChronicDiseases);
                FillTags(CurrentMedications, patient.CurrentMedications);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "دریافت اطلاعات بیمار ناموفق بود";
            }
        }
    }

    /// <summary>پر کردن مجموعه تگ از رشته JSON آرایه</summary>
    private static void FillTags(ObservableCollection<string> target, string? json)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(json);
            if (items is not null)
                foreach (var item in items)
                    target.Add(item);
        }
        catch
        {
            // مقدار غیر JSON — به‌عنوان یک تگ واحد
            target.Add(json);
        }
    }

    /// <summary>اعتبارسنجی کد ملی — رقم کنترلی طبق الگوریتم ملی</summary>
    private static bool IsValidNationalCode(string code)
    {
        if (code.Length != 10 || !code.All(char.IsDigit) || code.Distinct().Count() == 1)
            return false;
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);
        var remainder = sum % 11;
        var check = code[9] - '0';
        return (remainder < 2 && check == remainder) || (remainder >= 2 && check == 11 - remainder);
    }

    /// <summary>بررسی خودکار یکتایی کد ملی بعد از ۱۰ رقم — طبق فرم ۴ پرامپت</summary>
    partial void OnNationalCodeChanged(string value)
    {
        IsNationalCodeValid = IsValidNationalCode(value);
        if (value?.Length == 10 && !_nationalCodeChecked && IsNationalCodeValid)
        {
            _nationalCodeChecked = true;
            _ = CheckNationalCodeUniquenessAsync();
        }
        if (string.IsNullOrEmpty(value))
            _nationalCodeChecked = false;
    }

    /// <summary>جستجوی کد ملی در سرور — اگر موجود بود پیشنهاد مشاهده بده</summary>
    private async Task CheckNationalCodeUniquenessAsync()
    {
        var result = await _api.GetAsync<SearchResponse>($"patients/search?term={NationalCode}");
        var found = result?.Data?.FirstOrDefault(p => p.NationalCode == NationalCode);
        if (found is not null && found.Id != _patientId)
        {
            var view = System.Windows.MessageBox.Show(
                $"بیماری با این کد ملی قبلاً ثبت شده است:\n\n{found.FirstName} {found.LastName} — پرونده {found.MedicalRecordNo}\n\nمشاهده پرونده می‌کنید؟",
                "کد ملی تکراری",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            if (view == System.Windows.MessageBoxResult.Yes)
                CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>ذخیره — ثبت یا ویرایش بر اساس حالت فرم</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // اعتبارسنجی طبق فرم ۴ — پیام‌های فارسی
        if (string.IsNullOrWhiteSpace(FirstName)) { ErrorMessage = "نام الزامی است"; return; }
        if (string.IsNullOrWhiteSpace(LastName)) { ErrorMessage = "نام خانوادگی الزامی است"; return; }
        if (string.IsNullOrWhiteSpace(NationalCode)) { ErrorMessage = "کد ملی الزامی است"; return; }
        if (NationalCode.Length != 10) { ErrorMessage = "کد ملی باید ۱۰ رقم باشد"; return; }
        if (!IsNationalCodeValid) { ErrorMessage = "کد ملی معتبر نیست (رقم کنترلی اشتباه)"; return; }
        if (string.IsNullOrWhiteSpace(Mobile)) { ErrorMessage = "شماره موبایل الزامی است"; return; }
        if (!System.Text.RegularExpressions.Regex.IsMatch(Mobile, @"^09\d{9}$"))
        { ErrorMessage = "فرمت موبایل: ۰۹XXXXXXXXX"; return; }

        var body = new
        {
            firstName = FirstName.Trim(),
            lastName = LastName.Trim(),
            nationalCode = NationalCode.Trim(),
            mobile = Mobile.Trim(),
            fatherName = FatherName,
            birthDateShamsi = BirthDateShamsi,
            gender = GenderIndex + 1,
            bloodType = BloodType == "نامشخص" ? null : BloodType,
            phone = Phone,
            email = Email,
            province = Province,
            city = City,
            address = Address,
            insuranceCompanyId = InsuranceCompanyId,
            insuranceNumber = InsuranceNumber,
            allergies = JsonSerializer.Serialize(Allergies.ToList()),
            chronicDiseases = JsonSerializer.Serialize(ChronicDiseases.ToList()),
            currentMedications = JsonSerializer.Serialize(CurrentMedications.ToList()),
        };

        IsBusy = true;
        var ok = _patientId.HasValue
            ? await _api.PutAsync($"patients/{_patientId}", body)
            : await _api.PostAsync("patients", body);
        IsBusy = false;

        if (ok)
        {
            _afterSave?.Invoke();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
        }
    }

    /// <summary>انصراف — اگر تغییری شده هشدار بده</summary>
    [RelayCommand]
    private void Cancel()
    {
        var dirty = !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(NationalCode);
        if (dirty && _patientId is null)
        {
            var confirm = System.Windows.MessageBox.Show(
                "تغییرات ذخیره نشده است. خارج می‌شوید؟",
                "تغییرات ذخیره نشده",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>پاسخ بیمه‌های مرجع</summary>
    public sealed class LookupCompanies
    {
        public bool IsSuccess { get; set; }
        public List<LookupCompany>? Data { get; set; }
    }

    /// <summary>DTO شرکت بیمه مرجع</summary>
    public sealed class LookupCompany
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>پاسخ جستجوی بیمار</summary>
    public sealed class SearchResponse
    {
        public bool IsSuccess { get; set; }
        public List<PatientDetail>? Data { get; set; }
    }

    /// <summary>DTO جزئیات بیمار — منطبق بر سرور</summary>
    public sealed class PatientDetail
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? BirthDateShamsi { get; set; }
        public int? Age { get; set; }
        public string? BloodType { get; set; }
        public int Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public long? InsuranceCompanyId { get; set; }
        public string? InsuranceName { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
    }
}
