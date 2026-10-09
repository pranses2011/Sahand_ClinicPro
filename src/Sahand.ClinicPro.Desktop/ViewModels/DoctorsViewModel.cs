using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف پزشک در فهرست — گام U3 طبق پرامپت UI بخش ۱ (فرم ۶)
/// ۱۰ ستون: شماره نظام/نام/تخصص/فوق‌تخصص/حق ویزیت/مدت/درصد سهم/وضعیت/امتیاز/عملیات
/// </summary>
public record DoctorRow(
    long Id,
    string? MedicalLicenseNo,
    string FullName,
    string? SpecialtyName,
    string? SubSpecialtyName,
    decimal ConsultationFee,
    string FeeDisplay,
    int VisitDuration,
    decimal DoctorPercentage,
    bool IsActive,
    decimal Rating,
    string RatingDisplay,
    long? EmployeeId = null,
    bool HasPhoto = false); // چرخه هجدهم: عکس از پرسنل مرتبط

/// <summary>
/// ViewModel فهرست پزشکان — جستجو و صفحه‌بندی + دکمه‌های برنامه کاری/آمار/کارانه
/// </summary>
public partial class DoctorsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مدیریت پزشکان";

    /// <summary>فهرست پزشکان صفحه فعلی</summary>
    [ObservableProperty]
    private ObservableCollection<DoctorRow> _doctors = new();

    /// <summary>پزشک انتخاب‌شده</summary>
    [ObservableProperty]
    private DoctorRow? _selectedDoctor;

    /// <summary>عبارت جستجو</summary>
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    /// <summary>شماره صفحه</summary>
    [ObservableProperty]
    private int _pageNumber = 1;

    /// <summary>تعداد کل صفحات</summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>تعداد کل</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>اندازه صفحه</summary>
    [ObservableProperty]
    private int _pageSize = 20;

    /// <summary>سازنده</summary>
    public DoctorsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری از سرور</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        var endpoint = $"doctors?page={PageNumber}&pageSize={PageSize}&onlyActive=false" +
                       (!string.IsNullOrWhiteSpace(SearchTerm)
                           ? $"&page.search={Uri.EscapeDataString(SearchTerm.Trim())}"
                           : string.Empty);
        var response = await _api.GetAsync<DoctorsPage>(endpoint);
        if (response is not null)
        {
            Doctors = new ObservableCollection<DoctorRow>(
                response.Data?.Select(MapRow) ?? Enumerable.Empty<DoctorRow>());
            PageNumber = response.PageNumber;
            TotalPages = response.TotalPages;
            TotalCount = response.TotalCount;
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت فهرست پزشکان ناموفق بود";
        }
        IsBusy = false;
    }

    /// <summary>نگاشت ردیف با جداکننده هزارگان ریال</summary>
    private static DoctorRow MapRow(DoctorDto d) => new(
        d.Id, d.MedicalLicenseNo,
        d.FullName,
        d.SpecialtyName, d.SubSpecialtyName,
        d.ConsultationFee, Money.Format(d.ConsultationFee),
        d.VisitDuration, d.DoctorPercentage,
        d.IsActive, d.Rating,
        d.Rating > 0 ? $"{d.Rating:0.0} ★" : "—",
        d.EmployeeId, d.HasPhoto);

    /// <summary>جستجو</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>بارگذاری مجدد</summary>
    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    /// <summary>تغییر صفحه</summary>
    [RelayCommand]
    private async Task PageChangedAsync(object? page)
    {
        if (page is int p && p >= 1) PageNumber = p;
        await LoadAsync();
    }

    /// <summary>ثبت پزشک جدید</summary>
    [RelayCommand]
    private void AddDoctor()
        => OpenForm(new DoctorFormViewModel(_api, _snackbar, null));

    /// <summary>ویرایش پزشک انتخاب‌شده</summary>
    [RelayCommand]
    private void EditDoctor(object? parameter)
    {
        // چرخه بیستم: دکمهٔ سطر، ردیف را به‌عنوان پارامتر می‌فرستد — کلیک روی دکمه
        // در WPF لزوماً ردیف را انتخاب نمی‌کند
        var row = parameter as DoctorRow ?? SelectedDoctor;
        if (row is null) { _snackbar.Error("ابتدا یک پزشک انتخاب کنید"); return; }
        OpenForm(new DoctorFormViewModel(_api, _snackbar, row.Id));
    }

    /// <summary>حذف پزشک با تأیید</summary>
    [RelayCommand]
    private async Task DeleteDoctorAsync(object? parameter)
    {
        var row = parameter as DoctorRow ?? SelectedDoctor; // چرخه بیستم — پارامتر ردیف
        if (row is null) { _snackbar.Error("ابتدا یک پزشک انتخاب کنید"); return; }
        var name = row.FullName;
        var confirmed = Views.AppDialog.Confirm("تأیید حذف پزشک", $"آیا از حذف پزشک «{name}» مطمئن هستید؟\nسوابق ویزیت‌ها حفظ می‌شود اما پزشک غیرفعال خواهد شد.", Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"doctors/{row.Id}"); // چرخه بیستم: حذف همان ردیف پارامتر
        IsBusy = false;
        if (ok) { _snackbar.Success($"پزشک «{name}» حذف شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /// <summary>مشاهده آمار عملکرد — دکمه آمار طبق فرم ۶</summary>
    [RelayCommand]
    private void ShowPerformance(object? parameter)
    {
        var row = parameter as DoctorRow ?? SelectedDoctor; // چرخه بیستم — پارامتر ردیف
        if (row is null) { _snackbar.Error("ابتدا یک پزشک انتخاب کنید"); return; }
        var window = new DoctorPerformanceWindow(_api, row.Id, row.FullName)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    /* ─── مشخصات کاربری پزشک — چرخهٔ بیست‌ونهم (گزارش هجدهم مالک) ─── */

    /// <summary>
    /// مشخصات کاربری پزشک — کلید «مشخصات کاربری» در لیست پزشکان:
    /// حسابِ ورودِ متصل به پرسنلِ همین پزشک را ویرایش می‌کند یا حسابِ
    /// جدیدِ متصل به همان رکورد پرسنل می‌سازد؛ پزشکِ بدون پرسنل مرتبط
    /// ابتدا باید از فرم ویرایش به پرسنل متصل شود (همان قالب عکس پزشک).
    /// </summary>
    [RelayCommand]
    private async Task ManageAccountAsync(DoctorRow? row)
    {
        var doctor = row ?? SelectedDoctor;
        if (doctor is null) { _snackbar.Error("ابتدا یک پزشک انتخاب کنید"); return; }
        if (doctor.EmployeeId is null)
        {
            _snackbar.Error("این پزشک به رکورد پرسنل متصل نیست — ابتدا از «ویرایش»، پرسنل مرتبط را انتخاب کنید");
            return;
        }

        IsBusy = true;
        await Services.AccountTools.OpenForEmployeeAsync(
            _api, _snackbar, doctor.EmployeeId.Value, doctor.FullName, mobile: null);
        IsBusy = false;
    }

    /* ─── عکس پزشکان — چرخه هجدهم (گزارش دوازدهم مالک) ─── */

    /// <summary>نمایش بزرگ عکس پزشک — از پرسنل مرتبط با پزشک</summary>
    [RelayCommand]
    private async Task ViewDoctorPhotoAsync(DoctorRow? row)
    {
        if (row is null) return;
        var bytes = await Services.PhotoTools.DownloadDoctorPhotoAsync(_api, row.Id);
        Services.PhotoTools.ShowLarge(bytes, $"عکس: {row.FullName}");
    }

    /// <summary>
    /// انتخاب/تعویض عکس پزشک — عکس روی پرسنل مرتبط ذخیره می‌شود؛
    /// پزشکِ بدون پرسنل مرتبط ابتدا باید از فرم ویرایش به پرسنل متصل شود.
    /// </summary>
    [RelayCommand]
    private async Task ChangeDoctorPhotoAsync(DoctorRow? row)
    {
        if (row is null) return;
        if (row.EmployeeId is null)
        {
            _snackbar.Error("این پزشک به رکورد پرسنل متصل نیست — ابتدا از «ویرایش»، پرسنل مرتبط را انتخاب کنید");
            return;
        }

        var picked = Services.PhotoTools.PickPhoto($"انتخاب عکس: {row.FullName}");
        if (picked is null) return;

        IsBusy = true;
        var ok = await Services.PhotoTools.UploadEmployeePhotoAsync(
            _api, row.EmployeeId.Value, picked.Value.Bytes, picked.Value.FileName);
        IsBusy = false;

        if (ok) { _snackbar.Success($"عکس «{row.FullName}» ذخیره شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ذخیرهٔ عکس ناموفق بود");
    }

    /// <summary>باز کردن فرم مودال و بارگذاری مجدد</summary>
    private void OpenForm(DoctorFormViewModel formVm)
    {
        var window = new DoctorFormWindow(formVm) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>پاسخ صفحه‌بندی پزشکان</summary>
    public sealed class DoctorsPage
    {
        public bool IsSuccess { get; set; }
        public List<DoctorDto>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>DTO پزشک منطبق بر سرور</summary>
    public sealed class DoctorDto
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public bool HasPhoto { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? MedicalLicenseNo { get; set; }
        public string? NezamPezeshkiCode { get; set; }
        public string? SpecialtyName { get; set; }
        public string? SubSpecialtyName { get; set; }
        public decimal ConsultationFee { get; set; }
        public int VisitDuration { get; set; }
        public decimal DoctorPercentage { get; set; }
        public bool IsActive { get; set; }
        public decimal Rating { get; set; }
    }
}

/// <summary>
/// ردیف برنامه هفتگی — قابل ویرایش در DataGrid تب ۴
/// </summary>
public partial class ScheduleRowVM : ObservableObject
{
    /// <summary>روز هفته — شنبه=۰ تا جمعه=۶</summary>
    public byte DayOfWeek { get; init; }

    /// <summary>نام فارسی روز</summary>
    public string DayName => DayOfWeek switch
    {
        0 => "شنبه", 1 => "یکشنبه", 2 => "دوشنبه", 3 => "سه‌شنبه",
        4 => "چهارشنبه", 5 => "پنجشنبه", 6 => "جمعه",
        _ => "؟"
    };

    /// <summary>روز فعال است</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>ساعت شروع — HH:mm</summary>
    [ObservableProperty]
    private string? _startTime;

    /// <summary>ساعت پایان</summary>
    [ObservableProperty]
    private string? _endTime;

    /// <summary>شروع استراحت</summary>
    [ObservableProperty]
    private string? _breakStart;

    /// <summary>پایان استراحت</summary>
    [ObservableProperty]
    private string? _breakEnd;

    /// <summary>شناسه اتاق</summary>
    [ObservableProperty]
    private long? _roomId;

    /// <summary>نام اتاق</summary>
    [ObservableProperty]
    private string? _roomName;

    /// <summary>ظرفیت حداکثر</summary>
    [ObservableProperty]
    private int? _maxPatients;
}

/// <summary>
/// ردیف غیبت پزشک — تب ۵ فرم پزشک
/// </summary>
public record AbsenceRow(
    long Id,
    string DateShamsi,
    string? FromTime,
    string? ToTime,
    string AbsenceTypeName,
    string? Reason,
    int BookedAppointments);

/// <summary>
/// گزینه اتاق معاینه برای Combobox
/// </summary>
public record RoomOption(long Id, string DisplayName);

/// <summary>
/// گزینه تخصص
/// </summary>
public record SpecialtyOption(long Id, string Name, long? ParentId);

/// <summary>
/// گزینه پرسنل
/// </summary>
public record EmployeeOption(long Id, string FullName, string? PersonnelCode);

/// <summary>
/// ViewModel فرم ثبت و ویرایش پزشک — گام U3 طبق فرم ۷:
/// ۵ تب: پایه / تنظیم ویزیت / امضا و مهر / برنامه کاری / غیبت‌ها
/// </summary>
public partial class DoctorFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly long? _doctorId;

    /* ── تب ۱: اطلاعات پایه ── */
    [ObservableProperty] private long? _employeeId;
    [ObservableProperty] private string _medicalLicenseNo = string.Empty;
    [ObservableProperty] private string _nezamPezeshkiCode = string.Empty;
    [ObservableProperty] private long? _specialtyId;
    [ObservableProperty] private long? _subSpecialtyId;
    [ObservableProperty] private string? _academicRank;
    [ObservableProperty] private string? _biography;

    /* ── تب ۲: تنظیمات ویزیت ── */
    [ObservableProperty] private string _consultationFeeText = "0";
    [ObservableProperty] private int _visitDuration = 15;
    [ObservableProperty] private int? _maxPatientsPerShift;
    [ObservableProperty] private decimal _doctorPercentage;
    [ObservableProperty] private bool _acceptsNewPatients = true;
    [ObservableProperty] private bool _onlineBookingEnabled = true;

    /* ── تب ۳: امضا و مهر ── */
    [ObservableProperty] private string? _signatureImageBase64;
    [ObservableProperty] private string? _stampImageBase64;

    /* ── تب ۴: برنامه هفتگی ── */
    public ObservableCollection<ScheduleRowVM> ScheduleRows { get; } = new();

    /* ── تب ۵: غیبت‌ها ── */
    public ObservableCollection<AbsenceRow> Absences { get; } = new();

    /* ── فرم غیبت جدید ── */
    [ObservableProperty] private string _newAbsenceDateShamsi = string.Empty;
    [ObservableProperty] private string? _newAbsenceFromTime;
    [ObservableProperty] private string? _newAbsenceToTime;
    [ObservableProperty] private int _newAbsenceTypeIndex;
    [ObservableProperty] private string? _newAbsenceReason;

    /* ── عکس پروفایل — چرخه هجدهم: عکس پرسنلِ مرتبط ── */
    [ObservableProperty] private System.Windows.Media.ImageSource? _photoPreview;
    [ObservableProperty] private bool _hasPhoto;

    /* ── عمومی ── */
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _formTitle = "ثبت پزشک جدید";
    [ObservableProperty] private bool _isBusy;

    /// <summary>گزینه‌های مرتبه علمی — طبق فرم ۷</summary>
    public List<string> AcademicRanks { get; } = new()
        { "عمومی", "متخصص", "فوق‌تخصص", "فلوشیپ", "استادیار", "دانشیار", "استاد" };

    /// <summary>گزینه‌های مدت ویزیت</summary>
    public List<int> VisitDurations { get; } = new() { 5, 10, 15, 20, 30, 45, 60 };

    /// <summary>نام فارسی انواع غیبت</summary>
    public List<string> AbsenceTypes { get; } = new() { "مرخصی", "استعلاجی", "کنگره", "سایر" };

    /// <summary>اتاق‌های معاینه</summary>
    public ObservableCollection<RoomOption> Rooms { get; } = new();

    /// <summary>تخصص‌های اصلی</summary>
    public ObservableCollection<SpecialtyOption> Specialties { get; } = new();

    /// <summary>
    /// همهٔ تخصص‌ها شامل فوق‌تخصص‌ها (فرزندان) — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «لیست فوق‌تخصص رو خالی میاره» — پیش‌تر فقط تخصص‌های والد بار می‌شدند و
    /// فیلتر فرزندان در همان فهرست والد می‌گشت که نتیجه‌ای نداشت؛ اکنون فهرست
    /// کامل جداگانه بار می‌شود و فیلتر از روی آن انجام می‌شود.
    /// </summary>
    public List<SpecialtyOption> AllSpecialties { get; } = new();

    /// <summary>فوق‌تخصص‌ها — فیلترشده بر اساس تخصص انتخابی</summary>
    public ObservableCollection<SpecialtyOption> SubSpecialties { get; } = new();

    /// <summary>پرسنل فعال</summary>
    public ObservableCollection<EmployeeOption> Employees { get; } = new();

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده</summary>
    public DoctorFormViewModel(IApiClient api, ISnackbarService snackbar, long? doctorId)
    {
        _api = api;
        _snackbar = snackbar;
        _doctorId = doctorId;
        if (doctorId.HasValue) FormTitle = "ویرایش اطلاعات پزشک";
        // هفت ردیف روزهای هفته
        for (byte day = 0; day <= 6; day++)
            ScheduleRows.Add(new ScheduleRowVM { DayOfWeek = day });
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری داده مرجع + ویرایش</summary>
    private async Task InitializeAsync()
    {
        await LoadLookupsAsync();
        if (_doctorId.HasValue)
        {
            var detail = await _api.GetAsync<DoctorDetailResponse>($"doctors/{_doctorId}");
            if (detail?.Data is null)
            {
                ErrorMessage = _api.LastError ?? "دریافت اطلاعات پزشک ناموفق بود";
                return;
            }
            var d = detail.Data;
            EmployeeId = d.EmployeeId;
            MedicalLicenseNo = d.MedicalLicenseNo ?? string.Empty;
            NezamPezeshkiCode = d.NezamPezeshkiCode ?? string.Empty;
            SpecialtyId = d.SpecialtyId;
            SubSpecialtyId = d.SubSpecialtyId;
            AcademicRank = d.AcademicRank;
            Biography = d.Biography;
            // ═══ چرخهٔ بیست‌وهشتم — ریشهٔ «حق ویزیت عددی نامعتبر است» ═══
            // پیش‌تر Money.Format (ارقام فارسی + جداکنندهٔ «٬» + تبدیل به واحد
            // نمایشی) در پراپرتی ریالیِ VM می‌نشست؛ ذخیره با decimal.TryParse
            // ساده شکست می‌خورد و در حالت تومان مقدار هم ۱۰ برابر کوچک می‌شد.
            // اکنون همان قرارداد MoneyRial: VM همیشه «ریال خالص با ارقام لاتین»
            // است؛ کادر مبلغ خودش قالب‌بندی نمایشی را انجام می‌دهد.
            ConsultationFeeText = d.ConsultationFee.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            VisitDuration = d.VisitDuration;
            MaxPatientsPerShift = d.MaxPatientsPerShift;
            DoctorPercentage = d.DoctorPercentage;
            AcceptsNewPatients = d.AcceptsNewPatients;
            OnlineBookingEnabled = d.OnlineBookingEnabled;
            SignatureImageBase64 = d.SignatureImageBase64;
            StampImageBase64 = d.StampImageBase64;
            FormTitle = $"ویرایش پزشک: {d.FullName}";
            FilterSubSpecialties();
            await LoadScheduleAsync();
            await LoadAbsencesAsync();
        }
    }

    /// <summary>بارگذاری داده مرجع</summary>
    private async Task LoadLookupsAsync()
    {
        await ReloadRoomsAsync();

        var specialties = await _api.GetAsync<SpecialtiesResponse>("lookups/specialties?onlyParents=true");
        if (specialties?.Data is not null)
            foreach (var s in specialties.Data)
                Specialties.Add(new SpecialtyOption(s.Id, s.Name, s.ParentId));

        // چرخه بیست‌ویکم: فهرست کامل (والد + فرزند) برای کشوی فوق‌تخصص —
        // بدون این درخواست دوم، فیلتر فرزندان همیشه خالی می‌ماند
        var allSpecs = await _api.GetAsync<SpecialtiesResponse>("lookups/specialties");
        if (allSpecs?.Data is not null)
            foreach (var s in allSpecs.Data.Where(s => s.ParentId is not null))
                AllSpecialties.Add(new SpecialtyOption(s.Id, s.Name, s.ParentId));

        var employees = await _api.GetAsync<EmployeesResponse>("lookups/employees");
        if (employees?.Data is not null)
            foreach (var e in employees.Data)
                Employees.Add(new EmployeeOption(e.Id, e.FullName, e.PersonnelCode));
    }

    /// <summary>فیلتر فوق‌تخصص‌ها بر اساس تخصص انتخابی</summary>
    partial void OnSpecialtyIdChanged(long? value) => FilterSubSpecialties();

    /// <summary>
    /// تغییر پرسنل مرتبط → پیش‌نمایش عکس همان پرسنل — چرخه هجدهم:
    /// عکس پزشک همان عکس رکورد پرسنل است (Doctor.EmployeeId → Employee.ProfilePicture).
    /// </summary>
    async partial void OnEmployeeIdChanged(long? value)
    {
        PhotoPreview = null;
        HasPhoto = false;
        if (value is null) return;
        var bytes = await Services.PhotoTools.DownloadEmployeePhotoAsync(_api, value.Value);
        if (bytes is { Length: > 0 })
        {
            PhotoPreview = Services.PhotoTools.ToImage(bytes);
            HasPhoto = true;
        }
    }

    /// <summary>تعویض عکس پزشک — انتخاب فایل و آپلود روی پرسنل مرتبط</summary>
    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        if (EmployeeId is null)
        {
            ErrorMessage = "برای عکس، ابتدا «پرسنل مرتبط» را انتخاب و فرم را ذخیره کنید";
            return;
        }

        var picked = Services.PhotoTools.PickPhoto("انتخاب عکس پزشک");
        if (picked is null) return;

        IsBusy = true;
        var ok = await Services.PhotoTools.UploadEmployeePhotoAsync(
            _api, EmployeeId.Value, picked.Value.Bytes, picked.Value.FileName);
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success("عکس ذخیره شد");
            PhotoPreview = Services.PhotoTools.ToImage(picked.Value.Bytes);
            HasPhoto = true;
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ذخیرهٔ عکس ناموفق بود";
        }
    }

    /// <summary>نمایش بزرگ عکس پزشک</summary>
    [RelayCommand]
    private async Task ViewPhotoAsync()
    {
        if (EmployeeId is null) { Services.PhotoTools.ShowLarge(null, "عکس پزشک"); return; }
        var bytes = await Services.PhotoTools.DownloadEmployeePhotoAsync(_api, EmployeeId.Value);
        Services.PhotoTools.ShowLarge(bytes, "عکس پزشک");
    }

    /// <summary>پر کردن فوق‌تخصص‌های مرتبط — از فهرست کامل فرزندان (چرخه بیست‌ویکم)</summary>
    private void FilterSubSpecialties()
    {
        SubSpecialties.Clear();
        if (SpecialtyId is null) return;
        foreach (var s in AllSpecialties.Where(s => s.ParentId == SpecialtyId))
            SubSpecialties.Add(s);
    }

    /// <summary>
    /// بازخوانی کشوی اتاق‌ها — پس از بازگشت از پنجرهٔ مدیریت اتاق‌ها نیز صدا زده می‌شود
    /// تا اتاق تازه‌ساخته بی‌درنگ در همین فرم قابل انتخاب باشد (چرخه بیست‌ویکم)
    /// </summary>
    private async Task ReloadRoomsAsync()
    {
        Rooms.Clear();
        var rooms = await _api.GetAsync<RoomsResponse>("lookups/rooms");
        if (rooms?.Data is not null)
            foreach (var r in rooms.Data)
                Rooms.Add(new RoomOption(r.Id, r.DisplayName));
    }

    /// <summary>
    /// باز کردن پنجرهٔ مدیریت اتاق‌ها — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «فیلد اتاق برای انتخاب هست ولی جایی برای تعریف اتاق و اسم اتاق نیست».
    /// پس از بستن پنجره، کشوی اتاق همین فرم تازه می‌شود.
    /// </summary>
    [RelayCommand]
    private void ManageRooms()
    {
        var vm = new RoomsManagerViewModel(_api, _snackbar);
        var window = new RoomsManagerWindow(vm)
        {
            Owner = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive)
        };
        window.ShowDialog();
        _ = ReloadRoomsAsync();
    }

    /// <summary>بارگذاری برنامه هفتگی</summary>
    private async Task LoadScheduleAsync()
    {
        var result = await _api.GetAsync<ScheduleResponse>($"doctors/{_doctorId}/schedule");
        if (result?.Data is null) return;
        ScheduleRows.Clear();
        foreach (var row in result.Data)
            ScheduleRows.Add(new ScheduleRowVM
            {
                DayOfWeek = row.DayOfWeek,
                IsEnabled = row.IsEnabled,
                StartTime = row.StartTime,
                EndTime = row.EndTime,
                BreakStart = row.BreakStart,
                BreakEnd = row.BreakEnd,
                RoomId = row.RoomId,
                RoomName = row.RoomName,
                MaxPatients = row.MaxPatients
            });
    }

    /// <summary>بارگذاری غیبت‌ها</summary>
    private async Task LoadAbsencesAsync()
    {
        var result = await _api.GetAsync<AbsencesResponse>($"doctors/{_doctorId}/absences");
        Absences.Clear();
        if (result?.Data is null) return;
        foreach (var a in result.Data)
            Absences.Add(new AbsenceRow(a.Id, a.DateShamsi, a.FromTime, a.ToTime,
                a.AbsenceTypeName, a.Reason, a.BookedAppointments));
    }

    /// <summary>ذخیره کل فرم — پایه + تنظیمات + امضا</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        // ═══ چرخهٔ بیست‌وهشتم — پارس امن ارقام فارسی/جداکننده ═══
        // فیلد ممکن است با ارقام فارسی پر یا پیش‌پر شده باشد؛ پارس مشترک
        // Money.TryParseRialDigits همهٔ شکل‌ها را می‌پذیرد (ریشۀ «حق ویزیت
        // عددی نامعتبر است» در ویرایش). مقدار VM بر حسب ریال است.
        if (!Helpers.Money.TryParseRialDigits(ConsultationFeeText, out var fee))
        { ErrorMessage = "حق ویزیت عددی نامعتبر است"; return; }
        if (DoctorPercentage is < 0 or > 100)
        { ErrorMessage = "درصد سهم پزشک باید بین ۰ تا ۱۰۰ باشد"; return; }
        if (_doctorId is null && SpecialtyId is null)
        { ErrorMessage = "انتخاب تخصص الزامی است"; return; }

        var body = new
        {
            employeeId = EmployeeId,
            medicalLicenseNo = MedicalLicenseNo,
            nezamPezeshkiCode = NezamPezeshkiCode,
            specialtyId = SpecialtyId,
            subSpecialtyId = SubSpecialtyId,
            academicRank = AcademicRank,
            biography = Biography,
            consultationFee = fee,
            visitDuration = VisitDuration,
            maxPatientsPerShift = MaxPatientsPerShift,
            doctorPercentage = DoctorPercentage,
            acceptsNewPatients = AcceptsNewPatients,
            onlineBookingEnabled = OnlineBookingEnabled,
            signatureImageBase64 = SignatureImageBase64,
            stampImageBase64 = StampImageBase64,
            isActive = true
        };

        IsBusy = true;
        var ok = _doctorId.HasValue
            ? await _api.PutAsync($"doctors/{_doctorId}", body)
            : await _api.PostAsync("doctors", body);
        IsBusy = false;

        if (!ok)
        {
            ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
            return;
        }
        await SaveScheduleAsync();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>ذخیره برنامه هفتگی — جدا از فرم اصلی</summary>
    [RelayCommand]
    private async Task SaveScheduleAsync()
    {
        if (_doctorId is null) return;
        var body = new
        {
            rows = ScheduleRows.Select(r => new
            {
                dayOfWeek = (int)r.DayOfWeek,
                isEnabled = r.IsEnabled,
                startTime = r.StartTime,
                endTime = r.EndTime,
                breakStart = r.BreakStart,
                breakEnd = r.BreakEnd,
                roomId = r.RoomId,
                roomName = r.RoomName,
                maxPatients = r.MaxPatients
            }).ToList()
        };
        var ok = await _api.PutAsync($"doctors/{_doctorId}/schedule", body);
        if (ok) _snackbar.Success("برنامه کاری هفتگی ذخیره شد");
        else _snackbar.Error(_api.LastError ?? "ذخیره برنامه کاری ناموفق بود");
    }

    /// <summary>انتخاب تصویر امضا — PNG شفاف ۳۰۰×۱۰۰</summary>
    [RelayCommand]
    private void PickSignature()
        => SignatureImageBase64 = PickImagePng("انتخاب تصویر امضا (PNG شفاف)");

    /// <summary>انتخاب تصویر مهر — ۱۵۰×۱۵۰</summary>
    [RelayCommand]
    private void PickStamp()
        => StampImageBase64 = PickImagePng("انتخاب تصویر مهر");

    /// <summary>دیالوگ انتخاب فایل PNG و تبدیل به Base64</summary>
    private static string? PickImagePng(string title)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "تصویر PNG|*.png"
        };
        if (dialog.ShowDialog() != true) return null;
        var bytes = System.IO.File.ReadAllBytes(dialog.FileName);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>ثبت غیبت جدید — با هشدار نوبت‌های رزروشده طبق فرم ۷</summary>
    [RelayCommand]
    private async Task AddAbsenceAsync()
    {
        if (_doctorId is null)
        { ErrorMessage = "ابتدا پزشک را ذخیره کنید"; return; }
        if (string.IsNullOrWhiteSpace(NewAbsenceDateShamsi))
        { ErrorMessage = "تاریخ غیبت الزامی است"; return; }

        var body = new
        {
            dateShamsi = NewAbsenceDateShamsi.Trim(),
            fromTime = string.IsNullOrWhiteSpace(NewAbsenceFromTime) ? null : NewAbsenceFromTime.Trim(),
            toTime = string.IsNullOrWhiteSpace(NewAbsenceToTime) ? null : NewAbsenceToTime.Trim(),
            absenceType = NewAbsenceTypeIndex + 1,
            reason = NewAbsenceReason
        };

        IsBusy = true;
        var response = await _api.PostAsync<AbsenceCreateResponse>($"doctors/{_doctorId}/absences", body);
        IsBusy = false;

        if (response?.Data is null)
        {
            ErrorMessage = _api.LastError ?? "ثبت غیبت ناموفق بود";
            return;
        }

        // هشدار نوبت‌های درگیر — «N نوبت رزرو شده وجود دارد. لغو شوند؟»
        if (response.Data.BookedAppointments > 0)
        {
            Views.AppDialog.Warn("نوبت‌های درگیر با غیبت", $"{ToPersian(response.Data.BookedAppointments)} نوبت رزرو شده در این بازه وجود دارد.\n" +
                "برای لغو یا جابه‌جایی نوبت‌ها با پذیرش هماهنگ کنید.");
        }

        NewAbsenceDateShamsi = string.Empty;
        NewAbsenceFromTime = null;
        NewAbsenceToTime = null;
        NewAbsenceReason = null;
        _snackbar.Success("غیبت ثبت شد");
        await LoadAbsencesAsync();
    }

    /// <summary>حذف غیبت</summary>
    [RelayCommand]
    private async Task DeleteAbsenceAsync(object? param)
    {
        if (param is not AbsenceRow row) return;
        var ok = await _api.DeleteAsync($"doctors/absences/{row.Id}");
        if (ok) { _snackbar.Success("غیبت حذف شد"); await LoadAbsencesAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>حذف جداکننده‌های هزارگان از متن عددی</summary>
    private static string DigitsOnly(string text) =>
        new(text.Where(char.IsDigit).ToArray());

    /// <summary>تبدیل عدد به رقم فارسی</summary>
    private static string ToPersian(int value) =>
        value.ToString().Replace('0', '۰').Replace('1', '۱').Replace('2', '۲')
            .Replace('3', '۳').Replace('4', '۴').Replace('5', '۵')
            .Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');

    /* ── پاسخ‌های API ── */

    public sealed class DoctorDetailResponse
    {
        public bool IsSuccess { get; set; }
        public DoctorDetail? Data { get; set; }
    }

    public sealed class DoctorDetail
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public string? MedicalLicenseNo { get; set; }
        public string? NezamPezeshkiCode { get; set; }
        public long? SpecialtyId { get; set; }
        public long? SubSpecialtyId { get; set; }
        public string? AcademicRank { get; set; }
        public string? Biography { get; set; }
        public decimal ConsultationFee { get; set; }
        public int VisitDuration { get; set; }
        public int? MaxPatientsPerShift { get; set; }
        public decimal DoctorPercentage { get; set; }
        public bool AcceptsNewPatients { get; set; }
        public bool OnlineBookingEnabled { get; set; }
        public string? SignatureImageBase64 { get; set; }
        public string? StampImageBase64 { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public sealed class RoomsResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoomItem>? Data { get; set; }
    }

    public sealed class RoomItem
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class SpecialtiesResponse
    {
        public bool IsSuccess { get; set; }
        public List<SpecialtyItem>? Data { get; set; }
    }

    public sealed class SpecialtyItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? ParentId { get; set; }
    }

    public sealed class EmployeesResponse
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeItem>? Data { get; set; }
    }

    public sealed class EmployeeItem
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PersonnelCode { get; set; }
    }

    public sealed class ScheduleResponse
    {
        public bool IsSuccess { get; set; }
        public List<ScheduleRow>? Data { get; set; }
    }

    public sealed class ScheduleRow
    {
        public byte DayOfWeek { get; set; }
        public bool IsEnabled { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? BreakStart { get; set; }
        public string? BreakEnd { get; set; }
        public long? RoomId { get; set; }
        public string? RoomName { get; set; }
        public int? MaxPatients { get; set; }
    }

    public sealed class AbsencesResponse
    {
        public bool IsSuccess { get; set; }
        public List<AbsenceItem>? Data { get; set; }
    }

    public sealed class AbsenceItem
    {
        public long Id { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public string? FromTime { get; set; }
        public string? ToTime { get; set; }
        public string AbsenceTypeName { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public int BookedAppointments { get; set; }
    }

    public sealed class AbsenceCreateResponse
    {
        public bool IsSuccess { get; set; }
        public AbsenceConflict? Data { get; set; }
    }

    public sealed class AbsenceConflict
    {
        public long AbsenceId { get; set; }
        public int BookedAppointments { get; set; }
    }
}
