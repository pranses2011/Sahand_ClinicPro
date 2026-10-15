using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه بیمار در نتیجه جستجو — چرخهٔ ۴۴: شناسه بیمهٔ اصلی پرونده
/// هم برای انتخاب خودکار نوع بیمه در فرم پذیرش حمل می‌شود</summary>
public record PatientOption(long Id, string DisplayName, string Detail, long? InsuranceCompanyId = null);

/// <summary>
/// ViewModel فرم ثبت نوبت — جستجوی بیمار + انتخاب پزشک + تاریخ و ساعت.
/// چرخه بیستم (گزارش چهاردهم مالک): فرم فهرست پزشکان را خودش از سرور
/// بارگذاری می‌کند (پیش‌تر فقط از سازنده می‌گرفت و از صفحهٔ بیماران با
/// فهرست خالی باز می‌شد) و می‌تواند با بیمارِ از پیش انتخاب‌شده باز شود.
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

    // ═══ چرخهٔ ۷۹ (گزارش مالک: «اولویت نوبت‌ها اشتباه است — همشون معمولی
    // ثبت می‌شوند») — فرم کلاسیک ثبت نوبت (از صفحهٔ بیماران) هیچ انتخابگر
    // اولویتی نداشت و بدنهٔ POST هم priority نمی‌فرستاد ⇒ سرور همیشه پیش‌فرض
    // «عادی» ذخیره می‌کرد. اکنون همان سه گزینهٔ ویزارد نوبت‌دهی این‌جا هم
    // هست و مقدار انتخابی واقعاً به سرور می‌رسد (واژگان یکدست «عادی/بالا/اورژانس»). ═══
    /// <summary>گزینه‌های اولویت — دقیقاً هم‌سانِ ویزارد نوبت‌دهی</summary>
    public string[] Priorities { get; } = { "عادی", "بالا", "اورژانس" };

    /// <summary>نمایه اولویت — ۰ عادی، ۱ بالا، ۲ اورژانس (پیش‌فرض عادی)</summary>
    [ObservableProperty]
    private int _priorityIndex;

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
        : this(api, doctors, null)
    {
    }

    /// <summary>
    /// سازنده کامل — چرخه بیستم: بیمار از پیش انتخاب‌شده (از صفحهٔ بیماران)
    /// و بارگذاری خودکار فهرست پزشکان از سرور وقتی فهرست داده‌نشده/خالی است
    /// </summary>
    /// <param name="api">کلاینت API</param>
    /// <param name="doctors">فهرست پزشکان — خالی مجاز است، خودکار پر می‌شود</param>
    /// <param name="preselectedPatient">بیمار از پیش انتخاب‌شده یا null</param>
    public AppointmentFormViewModel(IApiClient api, ObservableCollection<DoctorOption> doctors,
        PatientOption? preselectedPatient)
    {
        _api = api;
        foreach (var d in doctors)
            Doctors.Add(d);

        // بیمار تزریقی از صفحهٔ بیماران — فرم بدون جستجو باز می‌شود
        if (preselectedPatient is not null)
            SelectedPatient = preselectedPatient;

        // بارگذاری خودکار پزشکان — فقط وقتی فهرست داده‌نشده یا خالی است
        if (Doctors.Count == 0)
            _ = LoadDoctorsAsync();
    }

    /// <summary>
    /// بارگذاری پزشکان فعال از سرور — چرخه بیستم (گزارش چهاردهم مالک):
    /// «لیست پزشکانی که تعریف شده هیچ‌جا نمی‌آید تا انتخاب کنیم».
    /// چرخه بیست‌وسوم (گزارش شانزدهم مالک): «کادر ثبت نوبت بدون هیچ اطلاعی باز
    /// می‌شود» — شکست بارگذاری پزشکان پیش‌تر بی‌صدا قورت داده می‌شد و کشوی
    /// پزشک خالی می‌ماند بی‌آنکه کاربر بداند چرا؛ اکنون علت در پیام خطای فرم دیده می‌شود.
    /// </summary>
    private async Task LoadDoctorsAsync()
    {
        try
        {
            var response = await _api.GetAsync<DoctorsPage>("doctors?pageSize=200");
            if (response?.Data is null)
            {
                // پاسخ تهی = خطای سرور/شبکه — پیام فارسی کلاینت در LastError است
                ErrorMessage = $"بارگذاری فهرست پزشکان ناموفق بود: {_api.LastError ?? "پاسخ سرور تهی بود"}";
                return;
            }

            foreach (var d in response.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

            // اگر هیچ پزشکی فعال نیست، کاربر باید بداند (شکاف تعریف پزشک)
            if (Doctors.Count == 0)
                ErrorMessage = "هیچ پزشکی ثبت نشده است — ابتدا از صفحهٔ پزشکان، پزشک تعریف کنید";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"بارگذاری فهرست پزشکان ناموفق بود: {ex.Message}";
        }
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
            // چرخهٔ ۷۹: اولویت واقعی انتخاب کاربر — نمایه ۰/۱/۲ به enum سرور
            // (۳ عادی / ۲ بالا / ۱ اورژانس) نگاشت می‌شود؛ پیش‌تر اصلاً ارسال نمی‌شد
            priority = PriorityIndex switch { 2 => 1, 1 => 2, _ => 3 },
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

    /// <summary>DTO جستجوی بیمار — چرخهٔ ۴۴: بیمهٔ اصلی بیمار هم برای انتخاب خودکار در پذیرش</summary>
    public sealed class PatientSearchDto
    {
        public long Id { get; set; }
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        /// <summary>شناسه بیمهٔ اصلی پروندهٔ بیمار — چرخهٔ ۴۴</summary>
        public long? InsuranceCompanyId { get; set; }
        /// <summary>نام بیمهٔ اصلی پرونده — چرخهٔ ۴۴ (نمایش در جزئیات)</summary>
        public string? InsuranceName { get; set; }
    }

    /// <summary>پاسخ صفحهٔ فهرست پزشکان — چرخه بیستم</summary>
    public sealed class DoctorsPage
    {
        public bool IsSuccess { get; set; }
        public List<DoctorDto>? Data { get; set; }
    }

    /// <summary>ردیف پزشک — چرخه بیستم</summary>
    public sealed class DoctorDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? SpecialtyName { get; set; }

        /// <summary>حق ویزیت به ریال — چرخه بیست‌ویکم: برای پذیرش و نوبت</summary>
        public decimal ConsultationFee { get; set; }
    }
}