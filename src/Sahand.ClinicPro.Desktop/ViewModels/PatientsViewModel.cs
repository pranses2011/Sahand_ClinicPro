using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
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
            p.Age, p.LastVisitDateShamsi, false);

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

    /// <summary>ویرایش بیمار — پارامتر ردیف از دکمهٔ سطر؛ در نبودش بیمار انتخاب‌شده — چرخه بیستم</summary>
    [RelayCommand]
    private void EditPatient(object? parameter)
    {
        // دکمهٔ داخل سطر جدول، ردیف را به‌عنوان پارامتر می‌فرستد — کلیک روی دکمه
        // در WPF لزوماً ردیف را انتخاب نمی‌کند؛ پارامتر همان ردیف مقصود است
        var row = parameter as PatientRow ?? SelectedPatient;
        if (row is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }
        OpenForm(new PatientFormViewModel(_api, row.Id, afterSave: null));
    }

    /// <summary>مشاهده جزئیات — پنجره پرونده بیمار — پارامتر ردیف از دکمهٔ سطر — چرخه بیستم</summary>
    [RelayCommand]
    private void ViewPatient(object? parameter)
    {
        var row = parameter as PatientRow ?? SelectedPatient;
        if (row is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }
        var window = new PatientDetailWindow(new PatientDetailViewModel(_api, row.Id))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /// <summary>رزرو نوبت برای بیمار — مشخصات بیمار به فرم منتقل و پزشکان خودکار بارگذاری می‌شوند — چرخه بیستم</summary>
    [RelayCommand]
    private void BookAppointment(object? parameter)
    {
        var row = parameter as PatientRow ?? SelectedPatient;
        if (row is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }

        // چرخه بیستم (گزارش چهاردهم مالک): پیش‌تر فرم با فهرست پزشکان خالی باز
        // می‌شد و کاربر باید کد ملی را از نو جستجو می‌کرد. اکنون:
        //   ۱) مشخصات بیمار از همان ردیف به فرم تزریق می‌شود (بیمار از پیش انتخاب‌شده)
        //   ۲) فرم نوبت خودش فهرست پزشکان را از سرور بارگذاری می‌کند
        var form = new AppointmentFormViewModel(
            _api,
            new ObservableCollection<DoctorOption>(),
            preselectedPatient: new PatientOption(
                row.Id,
                $"{row.FullName} — پرونده {row.MedicalRecordNo}",
                $"کد ملی {row.NationalCode} — موبایل {row.Mobile}"));
        var window = new AppointmentFormWindow(form) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
    }

    /// <summary>حذف بیمار — پارامتر ردیف از دکمهٔ سطر؛ در نبودش انتخاب‌شده — چرخه بیستم</summary>
    [RelayCommand]
    private async Task DeletePatientAsync(object? parameter)
    {
        var row = parameter as PatientRow ?? SelectedPatient;
        if (row is null)
        {
            _snackbar.Error("ابتدا یک بیمار انتخاب کنید");
            return;
        }

        var name = row.FullName;
        var patientId = row.Id;
        var confirmed = Views.AppDialog.Confirm("تأیید حذف بیمار", $"آیا از حذف بیمار «{name}» مطمئن هستید؟\nاین عملیات حذف نرم است و قابل بازیابی توسط مدیر سیستم است.", Views.AppDialogIcon.Warning) == true;

        if (!confirmed) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"patients/{patientId}");
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

    /// <summary>ساختار پاسخ صفحه بیماران — منطبق بر PagedResponse از PatientListDto</summary>
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

        /// <summary>سن محاسبه‌شده از سمت سرور — چرخه بیست‌وسوم: ستون سن فهرست بیماران</summary>
        public int? Age { get; set; }
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

    /// <summary>فهرست استان‌ها — چرخه ۱۹: انتخاب از کشویی با دادهٔ پیش‌فرض داخل برنامه</summary>
    public List<string> ProvinceOptions { get; } =
        IranGeoData.Provinces.Keys.OrderBy(k => k, StringComparer.CurrentCulture).ToList();

    /// <summary>شهرهای استان منتخب — با تغییر استان تازه می‌شود</summary>
    public ObservableCollection<string> CityOptions { get; } = new();

    /* ── تب ۲: اطلاعات بیمه ── */
    [ObservableProperty] private long? _insuranceCompanyId;
    [ObservableProperty] private string? _insuranceNumber;
    [ObservableProperty] private string? _insuranceTypeName;

    /* ── تب ۴: تماس اضطراری ── */
    [ObservableProperty] private string? _emergencyContactName;
    [ObservableProperty] private string? _emergencyContactPhone;
    [ObservableProperty] private string? _emergencyContactRelation;

    /* ── عمومی ── */
    /// <summary>متن خطای فرم</summary>
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _formTitle = "ثبت بیمار جدید";
    [ObservableProperty] private bool _isNationalCodeValid;

    /// <summary>
    /// متن اعتبارسنجی زندهٔ کد ملی — چرخه بیستم (گزارش چهاردهم مالک):
    /// «اعتبارسنجی کد ملی زنده باشد و هنگام نوشتن زیرش با متن نشان داده شود»
    /// </summary>
    [ObservableProperty] private string _nationalCodeValidationText = string.Empty;

    /// <summary>رنگ متن اعتبارسنجی — سبز معتبر / قرمز نامعتبر / زرد ناقص</summary>
    [ObservableProperty] private string _nationalCodeValidationColor = "#C62828";

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
            // چرخه بیستم (گزارش چهاردهم مالک): پاسخ در پاکت ApiResponse پیچیده
            // می‌شود؛ پیش‌تر مستقیم روی PatientDetail نگاشت می‌شد و چون فیلد Data
            // وجود نداشت، همهٔ مشخصات خالی می‌ماند و «فرم ویرایش خالی» باز می‌شد
            var envelope = await _api.GetAsync<PatientDetailEnvelope>($"patients/{_patientId}");
            var patient = envelope?.Data;
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
                // چرخه بیست‌ویکم (گزارش پانزدهم مالک): فیلدهای پیشین‌جا‌افتاده —
                // کد پستی، شغل، تأهل، تماس اضطراری و نوع بیمه نیز در ویرایش بار می‌شوند
                PostalCode = patient.PostalCode;
                Occupation = patient.Occupation;
                MaritalStatus = patient.MaritalStatusFa;
                EmergencyContactName = patient.EmergencyContactName;
                EmergencyContactPhone = patient.EmergencyContactPhone;
                EmergencyContactRelation = patient.EmergencyContactRelation;
                InsuranceTypeName = patient.InsuranceType;
                Phone = patient.Phone;
                Email = patient.Email;
                Province = patient.Province;
                City = patient.City;
                Address = patient.Address;

                // چرخه ۱۹: اگر مقدار ذخیره‌شده در فهرست پیش‌فرض نبود، به فهرست اضافه شود
                // تا هنگام ویرایش، مقدار قبلی بیمار در کشویی دیده و حفظ شود
                if (!string.IsNullOrWhiteSpace(Province)
                    && !IranGeoData.Provinces.ContainsKey(Province)
                    && !ProvinceOptions.Contains(Province))
                    ProvinceOptions.Insert(0, Province);
                if (!string.IsNullOrWhiteSpace(City) && !CityOptions.Contains(City))
                    CityOptions.Add(City);
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

    /// <summary>بارگذاری شهرهای استان منتخب — چرخه ۱۹ (گزارش سیزدهم مالک)</summary>
    partial void OnProvinceChanged(string? value)
    {
        CityOptions.Clear();
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (IranGeoData.Provinces.TryGetValue(value, out var cities))
            foreach (var c in cities)
                CityOptions.Add(c);
    }

    /// <summary>اعتبارسنجی کد ملی — از اعتبارسنج مشترک (چرخه ۱۹)</summary>
    private static bool IsValidNationalCode(string code)
        => Validators.IsValidNationalCode(code);

    /// <summary>بررسی خودکار یکتایی کد ملی بعد از ۱۰ رقم — طبق فرم ۴ پرامپت + بازخورد زندهٔ زیر فیلد (چرخه بیستم)</summary>
    partial void OnNationalCodeChanged(string value)
    {
        IsNationalCodeValid = IsValidNationalCode(value);

        // ═══ بازخورد زندهٔ زیر فیلد — چرخه بیستم (گزارش چهاردهم مالک) ═══
        var code = (value ?? string.Empty).Trim();
        var latin = Money.PersianDigitsToLatin(code);
        if (latin.Length == 0)
        {
            NationalCodeValidationText = string.Empty;
        }
        else if (latin.Any(ch => !char.IsDigit(ch)))
        {
            NationalCodeValidationColor = "#C62828";
            NationalCodeValidationText = "❌ کد ملی فقط باید رقم باشد";
        }
        else if (latin.Length < 10)
        {
            NationalCodeValidationColor = "#F9A825";
            NationalCodeValidationText = $"⏳ {10 - latin.Length} رقم دیگر وارد کنید ({latin.Length} از ۱۰)";
        }
        else if (IsNationalCodeValid)
        {
            NationalCodeValidationColor = "#2E7D32";
            NationalCodeValidationText = "✅ کد ملی معتبر است (رقم کنترلی درست)";
        }
        else
        {
            NationalCodeValidationColor = "#C62828";
            NationalCodeValidationText = "❌ رقم کنترلی کد ملی درست نیست — کد را بازبینی کنید";
        }

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
            var view = Views.AppDialog.Confirm("کد ملی تکراری", $"بیماری با این کد ملی قبلاً ثبت شده است:\n\n{found.FirstName} {found.LastName} — پرونده {found.MedicalRecordNo}\n\nمشاهده پرونده می‌کنید؟", Views.AppDialogIcon.Info);
            if (view == true)
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
        if (!Validators.IsValidMobile(Mobile)) { ErrorMessage = "فرمت موبایل: ۰۹XXXXXXXXX"; return; }

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
            // چرخه بیست‌ویکم: فیلدهای پیشین ارسال‌نشدنی — حالا ذخیره واقعی می‌شوند
            maritalStatus = MaritalStatus,
            postalCode = PostalCode,
            occupation = Occupation,
            emergencyContactName = EmergencyContactName,
            emergencyContactPhone = EmergencyContactPhone,
            emergencyContactRelation = EmergencyContactRelation,
            insuranceType = InsuranceTypeName,
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
            var confirm = Views.AppDialog.Confirm("تغییرات ذخیره نشده", "تغییرات ذخیره نشده است. خارج می‌شوید؟", Views.AppDialogIcon.Warning);
            if (confirm != true) return;
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

    /// <summary>پاکت استاندارد پاسخ جزئیات بیمار — چرخه بیستم</summary>
    public sealed class PatientDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public PatientDetail? Data { get; set; }
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
        // چرخه بیست‌ویکم: فیلدهای تازه‌افزوده برای نمایش کامل در فرم ویرایش
        public string? PostalCode { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatusFa { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? InsuranceType { get; set; }
        public long? InsuranceCompanyId { get; set; }
        public string? InsuranceName { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
    }
}
