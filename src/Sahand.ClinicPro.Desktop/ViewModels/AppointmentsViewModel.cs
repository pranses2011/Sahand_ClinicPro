using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Controls;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// گزینهٔ پزشک در کشوها — چرخه بیست‌ویکم: حق ویزیت واقعی پزشک هم همراه است
/// تا در پذیرش به‌جای مقدار هاردکد، همان عدد ثبت‌شده در پروندهٔ پزشک استفاده شود
/// (گزارش پانزدهم مالک: «برای پزشک ۴۵۰ حق ویزیت زدم ولی در پذیرش ۷۰ می‌آورد»)
/// </summary>
public record DoctorOption(long Id, string Name, string? SpecialtyName, decimal ConsultationFee = 0);

/// <summary>ردیف لیست انتظار — چرخه ۱۳ (درخواست ۳-۴)</summary>
public record WaitlistRow(
    long Id,
    string PatientName,
    string PatientMobile,
    string DoctorName,
    string PreferredText,
    string PriorityText,
    string StatusText,
    string StatusColor,
    string NotifiedText,
    string? Notes);

/// <summary>
/// اسلات زمانی نمایشی — گام U4 طبق فرم ۸ (سبز/خط‌خورده/انتخاب)
/// </summary>
public partial class SlotVM : ObservableObject
{
    /// <summary>ساعت — HH:mm</summary>
    public string Time { get; init; } = string.Empty;

    /// <summary>آزاد است</summary>
    public bool IsAvailable { get; init; }

    /// <summary>ساعت استراحت است</summary>
    public bool IsBreak { get; init; }

    /// <summary>نام بیمار رزروکننده — برای ToolTip</summary>
    public string? PatientName { get; init; }

    /// <summary>جنسیت بیمار (Male/Female) — آیکن نوار جزئیات</summary>
    public string? PatientGender { get; init; }

    /// <summary>سن بیمار</summary>
    public int? PatientAge { get; init; }

    /// <summary>نوع/شرکت بیمهٔ بیمار</summary>
    public string? PatientInsurance { get; init; }

    /// <summary>کد ملی بیمار</summary>
    public string? PatientNationalCode { get; init; }

    /// <summary>تلفن بیمار</summary>
    public string? PatientPhone { get; init; }

    /// <summary>انتخاب فعلی کاربر</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>سنجاق‌شده در نوار جزئیات (کلیک روی اسلات رزروشده)</summary>
    [ObservableProperty]
    private bool _isPinned;

    /// <summary>اتاق ویزیت</summary>
    public string? RoomName { get; init; }

    /// <summary>هزینه تخمینی</summary>
    public decimal Fee { get; init; }

    /// <summary>رزروشده است (بیمار دارد و استراحت نیست)</summary>
    public bool IsBooked => !IsAvailable && !IsBreak && !string.IsNullOrWhiteSpace(PatientName);

    /// <summary>ساعت این اسلات گذشته است (امروز و قبل از اکنون) — هنگام بارگذاری ست می‌شود</summary>
    public bool IsPast { get; set; }

    /// <summary>آیکن جنسیت برای نوار جزئیات</summary>
    public string GenderIcon => PatientGender == "Female" ? "👩" : "👨";

    /// <summary>متن روبان مورب گوشهٔ کاشی</summary>
    public string RibbonText => IsSelected ? "✓ انتخاب" : IsPast ? "گذشته" : IsBreak ? "استراحت" : IsBooked ? "رزرو" : "آزاد";

    /// <summary>زیرعنوان کاشی</summary>
    public string SubText => IsPast ? "سپری شده" : IsBreak ? "🌙 استراحت" : IsBooked ? PatientName ?? "" : (string.IsNullOrWhiteSpace(RoomName) ? "آزاد" : "🩺 " + RoomName);

    /// <summary>ساعت عددی</summary>
    public int Hour => Time.Length >= 2 && int.TryParse(Time[..2], out var h) ? h : 0;

    /// <summary>گروه صبح/ظهر/عصر</summary>
    public string Group => Hour < 12 ? "صبح" : Hour < 16 ? "ظهر" : "عصر";

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(RibbonText));
    partial void OnIsPinnedChanged(bool value) => OnPropertyChanged(nameof(RibbonText));
}

/// <summary>گروه اسلات‌ها (صبح/ظهر/عصر) برای نمای گروه‌بندی‌شده</summary>
public sealed class SlotGroup
{
    public string Title { get; init; } = "";
    public string Icon { get; init; } = "";
    public System.Collections.ObjectModel.ObservableCollection<SlotVM> Items { get; } = new();
    public int Count => Items.Count;
}

/// <summary>
/// ردیف نوبت فهرست — ۱۳ ستون طبق فرم ۹ + اولویت رنگی (چرخهٔ ۷۵)
/// </summary>
public record AppointmentListRow(
    long Id,
    bool IsSelected,
    string? Code,
    string PatientName,
    string? PatientMobile,
    string DoctorName,
    string? SpecialtyName,
    string DateShamsi,
    string Time,
    int Status,
    string StatusText,
    string StatusColor,
    int Type,
    string TypeText,
    int Source,
    string SourceText,
    decimal EstimatedFee,
    // متن فارسی اولویت — «🚨 اورژانس / ⬆ بالا / عادی» (چرخهٔ ۷۵؛ واژگان یکدست چرخهٔ ۷۹)
    string PriorityText = "عادی",
    // رنگ اختصاصی اولویت — قرمز اورژانس / نارنجی بالا / خاکستری عادی
    string PriorityColor = "#78909C");

/// <summary>
/// بیمار انتخاب‌شده در رزرو سریع — چرخهٔ ۴۴: شناسهٔ بیمهٔ اصلی پرونده
/// (InsuranceCompanyId) با پارامتر اختیاری به رکورد اضافا شد تا هیچ نقطهٔ
/// ساختِ موجود نشکند؛ فرم پذیرش با انتخاب بیمار، نوع بیمه را از همین
/// شناسه به‌صورت خودکار انتخاب می‌کند (گزارش مالک).
/// </summary>
public record QuickPatient(
    long Id,
    string FullName,
    string NationalCode,
    string Mobile,
    string? InsuranceName,
    string? Allergies,
    string? LastVisitDateShamsi,
    long? InsuranceCompanyId = null);

/// <summary>
/// ViewModel نوبت‌دهی — دو تب رزرو و فهرست — گام U4 طبق پرامپت UI بخش ۲ (فرم ۸ و ۹)
/// تب ۱: جستجوی بیمار + تقویم شمسی + اسلات‌ها + خلاصه و ثبت
/// تب ۲: فیلترهای کامل + Badge وضعیت + عملیات دسته‌ای
/// </summary>
public partial class AppointmentsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly INavigationService _navigation;
    private System.Windows.Threading.DispatcherTimer? _debounce;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نوبت‌دهی";

    /* ═══════════ تب ۳: لیست انتظار — چرخه ۱۳ (درخواست ۳-۴) ═══════════ */

    /// <summary>ردیف‌های لیست انتظار</summary>
    [ObservableProperty]
    private ObservableCollection<WaitlistRow> _waitlist = new();

    /// <summary>بیمار انتخاب‌شده برای افزودن به لیست انتظار</summary>
    [ObservableProperty]
    private QuickPatient? _waitlistPatient;

    /// <summary>متن جستجوی بیمار لیست انتظار</summary>
    [ObservableProperty]
    private string _waitlistPatientSearch = string.Empty;

    /// <summary>نتایج جستجوی بیمار لیست انتظار</summary>
    [ObservableProperty]
    private ObservableCollection<QuickPatient> _waitlistPatientResults = new();

    /// <summary>نمایه پزشک دلخواه — -1 یعنی هر پزشک</summary>
    [ObservableProperty]
    private int _waitlistDoctorIndex = -1;

    /// <summary>تاریخ شروع بازه دلخواه</summary>
    [ObservableProperty]
    private DateTime? _waitlistFromDate = DateTime.Today;

    /// <summary>تاریخ پایان بازه دلخواه</summary>
    [ObservableProperty]
    private DateTime? _waitlistToDate = DateTime.Today.AddDays(14);

    /// <summary>نمایه بازه روز — 0=هر 1=صبح 2=عصر 3=شب</summary>
    [ObservableProperty]
    private int _waitlistTimeIndex;

    /// <summary>نمایه اولویت — 0=معمولی 1=فوری 2=اورژانس</summary>
    [ObservableProperty]
    private int _waitlistPriorityIndex;

    /// <summary>یادداشت لیست انتظار</summary>
    [ObservableProperty]
    private string? _waitlistNotes;

    /* ═══ چرخهٔ ۷۴ — مرتب‌سازی چندستونهٔ فهرست نوبت‌ها (گزارش مالک) ═══
       وضعیت مرتب‌سازی به‌صورت فهرست اولویت‌دار (مسیر، جهت) نگهداری می‌شود؛
       کلاینت View (AppointmentsView.OnGridSorting) آن را می‌سازد و اینجا روی
       نمای مجموعهٔ Rows اعمال می‌شود؛ پس از هر جستجو/صفحه‌بندی جدید هم
       بازاعمال می‌شود تا «مرتب‌سازی با هر جستجو ریست نشود». */

    /// <summary>وضعیت مرتب‌سازی فعلی — فهرست اولویت‌دار (اول = اولویت اصلی)</summary>
    private List<(string Path, System.ComponentModel.ListSortDirection Direction)> _sortState = new();

    /// <summary>خواندن وضعیت مرتب‌سازی (کپی — برای منطق کلیک در View)</summary>
    public List<(string Path, System.ComponentModel.ListSortDirection Direction)> GetSortState() => new(_sortState);

    /// <summary>
    /// نصب وضعیت مرتب‌سازی جدید + اعمال بی‌درنگ روی نمای مجموعهٔ ردیف‌ها —
    /// پیکان‌های هدر ستون‌ها از روی SortDescriptions نما به‌روز می‌شوند
    /// </summary>
    public void SetSortState(List<(string Path, System.ComponentModel.ListSortDirection Direction)> state)
    {
        _sortState = state ?? new List<(string, System.ComponentModel.ListSortDirection)>();
        ApplySortToView();
    }

    /// <summary>اعمال وضعیت مرتب‌سازی روی نمای مجموعهٔ Rows (در صورت آماده‌بودن)</summary>
    private void ApplySortToView()
    {
        if (Rows.Count == 0 && _sortState.Count == 0) return;
        try
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Rows);
            if (view is null) return;
            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                foreach (var (path, dir) in _sortState)
                    view.SortDescriptions.Add(new System.ComponentModel.SortDescription(path, dir));
            }
        }
        catch
        {
            // مسیر نامعتبر (مثلاً ستون حذف‌شده) — مرتب‌سازی نادیده؛ فهرست سالم می‌ماند
        }
    }

    /* ═══════════ تب ۱: رزرو نوبت ═══════════ */

    /// <summary>متن جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearchText = string.Empty;

    /// <summary>نتایج جستجوی بیمار</summary>
    [ObservableProperty]
    private ObservableCollection<QuickPatient> _patientSearchResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private QuickPatient? _selectedPatient;

    /// <summary>پزشکان فعال</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>پزشک انتخاب‌شده</summary>
    [ObservableProperty]
    private DoctorOption? _selectedDoctor;

    /// <summary>تاریخ انتخابی تقویم — میلادی</summary>
    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    /// <summary>تاریخ انتخابی شمسی</summary>
    [ObservableProperty]
    private string _selectedDateShamsi = PersianDateHelper.ToShamsi(DateTime.Today);

    /// <summary>ماه نمایشی تقویم</summary>
    [ObservableProperty]
    private int _displayMonth;

    /// <summary>سال نمایشی تقویم</summary>
    [ObservableProperty]
    private int _displayYear;

    /// <summary>نشانگرهای تقویم — تاریخ → تعداد نوبت آزاد</summary>
    [ObservableProperty]
    private IDictionary<DateTime, int>? _markedDates;

    /// <summary>روزهای غیرفعال تقویم</summary>
    [ObservableProperty]
    private IEnumerable<DateTime>? _disabledDates;

    /// <summary>آمار ظرفیت روزانه برای تقویم تازه — تاریخ ← (رزروشده، کل ظرفیت)</summary>
    [ObservableProperty]
    private IDictionary<DateTime, CalendarDayStat>? _dayStats;

    /// <summary>آمار ماه‌های سال نمایشی برای نمای انتخاب ماه تقویم</summary>
    [ObservableProperty]
    private IDictionary<int, CalendarMonthStat>? _monthStats;

    /// <summary>کلید کش آمار سالانه (پزشک|سال) — بارگذاری تکراری نشود</summary>
    private string _monthStatsKey = string.Empty;

    /// <summary>اسلات‌های روز انتخابی</summary>
    public ObservableCollection<SlotVM> Slots { get; } = new();

    /// <summary>اسلات انتخاب‌شده</summary>
    [ObservableProperty]
    private SlotVM? _selectedSlot;

    /// <summary>نمایه نوع ویزیت — اولیه/پیگیری/اورژانس/مشاوره</summary>
    [ObservableProperty]
    private int _selectedTypeIndex;

    /// <summary>نمایه اولویت — عادی/بالا/اورژانس</summary>
    [ObservableProperty]
    private int _selectedPriorityIndex;

    /// <summary>یادداشت نوبت</summary>
    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>اتاق ویزیت — از اسلات</summary>
    [ObservableProperty]
    private string? _selectedRoom;

    /// <summary>هزینه تخمینی</summary>
    [ObservableProperty]
    private decimal _estimatedFee;

    // ═══ چرخهٔ ۷۶ (گزارش مالک چرخهٔ ۷۴) — فرم مرحله‌ای (ویزارد) نوبت‌دهی ═══
    // «ورود اطلاعات و ثبت به‌صورت مرحله‌ای (Wizard) + چیدمان خیلی زیباتر و
    // شکیل‌تر» — تب رزرو نوبت به سه گامِ گویا شکسته شد:
    //   گام ۰ — بیمار و پزشک: جستجو + انتخاب + کارت پرونده + پزشک
    //   گام ۱ — زمان: تقویم شمسی + اسلات‌های آزاد روز
    //   گام ۲ — تأیید و ثبت: خلاصهٔ انتخاب + نوع/اولویت/یادداشت + دکمهٔ ثبت
    // پیمایش با دکمه‌های «مرحلهٔ بعد/قبل» و اعتبارسنجیِ همان گام.

    /// <summary>گام جاری ویزارد نوبت‌دهی — ۰ بیمار و پزشک، ۱ زمان، ۲ تأیید و ثبت</summary>
    [ObservableProperty]
    private int _wizardStep;

    /// <summary>
    /// رفتن به گام بعد — اعتبارسنجی گام جاری با پیام راهنمای فارسی؛
    /// در گام آخر مخفی است و کار با دکمهٔ «تایید و ثبت نوبت» ادامه می‌یابد
    /// </summary>
    [RelayCommand]
    private void NextStep()
    {
        if (WizardStep == 0)
        {
            if (SelectedPatient is null)
            {
                _snackbar.Error("ابتدا بیمار را انتخاب کنید");
                return;
            }
            if (SelectedDoctor is null)
            {
                _snackbar.Error("ابتدا پزشک را انتخاب کنید");
                return;
            }
        }
        if (WizardStep == 1 && SelectedSlot is null)
        {
            _snackbar.Error("ابتدا ساعت نوبت را انتخاب کنید");
            return;
        }
        if (WizardStep < 2)
            WizardStep++;
    }

    /// <summary>بازگشت به گام قبل — در گام نخست مخفی است</summary>
    [RelayCommand]
    private void BackStep()
    {
        if (WizardStep > 0)
            WizardStep--;
    }

    /// <summary>نوبت‌های امروز</summary>
    public ObservableCollection<TodayRow> TodayAppointments { get; } = new();

    /* ═══════════ تب ۲: فهرست نوبت‌ها ═══════════ */

    /// <summary>
    /// از تاریخ — فیلتر. چرخه بیست‌ویکم (گزارش پانزدهم مالک): فیلتر از/تا تاریخ
    /// با انتخابگر تاریخ شمسی انتخاب می‌شود؛ پیش‌فرض = امروز.
    /// </summary>
    [ObservableProperty]
    private string? _fromDateShamsi = PersianDateHelper.ToShamsi(DateTime.Today);

    /// <summary>
    /// تا تاریخ — فیلتر. پیش‌فرض یک هفته جلوتر تا فهرست هفتهٔ جاری خالی نماند
    /// (چرخه بیست‌ویکم: «فهرست نوبت‌ها نمایش داده نمی‌شود» — بازهٔ امروز-فقطر-امروز
    /// اغلب خالی بود و کاربر فکر می‌کرد لیست کار نمی‌کند).
    /// </summary>
    [ObservableProperty]
    private string? _toDateShamsi = PersianDateHelper.ToShamsi(DateTime.Today.AddDays(7));

    /// <summary>نمایه فیلتر پزشک — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _doctorFilterIndex;

    /// <summary>نمایه فیلتر وضعیت — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    /// <summary>نمایه فیلتر نوع — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _typeFilterIndex;

    /// <summary>نمایه فیلتر منبع — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _sourceFilterIndex;

    /// <summary>جستجوی متنی فهرست</summary>
    [ObservableProperty]
    private string _listSearch = string.Empty;

    /// <summary>ردیف‌های فهرست</summary>
    [ObservableProperty]
    private ObservableCollection<AppointmentListRow> _rows = new();

    /// <summary>ردیف انتخاب‌شده</summary>
    [ObservableProperty]
    private AppointmentListRow? _selectedRow;

    /// <summary>شماره صفحه فهرست</summary>
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

    /// <summary>برچسب وضعیت‌ها — ایندکس +۱ = مقدار enum</summary>
    public string[] StatusLabels { get; } =
        { "رزرو شده", "تأیید شده", "حاضر", "در حال ویزیت", "تکمیل شده", "لغو شده", "عدم مراجعه" };

    /// <summary>رنگ Badge وضعیت‌ها</summary>
    public string[] StatusColors { get; } =
        { "#ECEFF1", "#BBDEFB", "#C8E6C9", "#90CAF9", "#A5D6A7", "#FFCDD2", "#FFE0B2" };

    /// <summary>برچسب انواع نوبت</summary>
    public string[] TypeLabels { get; } = { "اولیه", "پیگیری", "اورژانس", "مشاوره" };

    /// <summary>برچسب منبع نوبت</summary>
    public string[] SourceLabels { get; } = { "حضوری", "تلفنی", "آنلاین" };

    /// <summary>انواع ویزیت — تب رزرو</summary>
    public string[] VisitTypes { get; } = { "اولیه", "پیگیری", "اورژانس", "مشاوره" };

    /// <summary>اولویت‌ها</summary>
    public string[] Priorities { get; } = { "عادی", "بالا", "اورژانس" };

    /// <summary>سازنده</summary>
    public AppointmentsViewModel(IApiClient api, ISnackbarService snackbar, INavigationService navigation)
    {
        _api = api;
        _snackbar = snackbar;
        _navigation = navigation;
        var today = PersianDateHelper.ToJalali(DateTime.Today);
        DisplayYear = today.Year;
        DisplayMonth = today.Month;
        _ = InitializeAsync();
    }

    /// <summary>
    /// تازه‌سازی هنگام هر بار دیده‌شدن صفحهٔ نوبت‌دهی — ویومدل صفحه یک بار
    /// هنگام ساخت پنجرهٔ اصلی ساخته می‌شود و بارگذاری اولیه‌اش ممکن است با
    /// آماده‌نبودن سرور/توکن هم‌زمان شده باشد؛ پیش‌تر در آن حالت فهرست
    /// پزشکان و تقویم برای همیشه خالی می‌ماندند (گزارش مکرر مالک: «تقویم
    /// روزها و برنامهٔ پزشک را نمی‌آورد»). اکنون هر بار ورود به صفحه:
    /// پزشکان (اگر خالی‌اند) + تقویم ماه + اسلات‌های روز از نو خوانده می‌شوند.
    /// </summary>
    public async Task RefreshOnActivatedAsync()
    {
        try
        {
            if (Doctors.Count == 0)
            {
                var doctors = await _api.GetAsync<DoctorsPage>("doctors?pageSize=200");
                if (doctors?.Data is not null)
                    foreach (var d in doctors.Data)
                        Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));
                if (SelectedDoctor is null && Doctors.Count > 0)
                    SelectedDoctor = Doctors[0];
            }
            if (SelectedDate is null)
            {
                SelectedDate = DateTime.Today;
                SelectedDateShamsi = PersianDateHelper.ToShamsi(DateTime.Today);
            }
            if (SelectedDoctor is not null)
            {
                await LoadCalendarAsync();
                await LoadSlotsAsync();
            }
            else
            {
                CalendarStatus = "فهرست پزشکان از سرور دریافت نشد — اتصال سرور را بررسی کنید؛ صفحه را دوباره باز کنید.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshOnActivated failed: {ex.Message}");
            CalendarStatus = "خطا در برقراری ارتباط با سرور هنگام بارگذاری تقویم.";
        }
    }

    /// <summary>بارگذاری اولیه — پزشکان + امروز + لیست انتظار (چرخهٔ ۷۴)</summary>
    private async Task InitializeAsync()
    {
        var doctors = await _api.GetAsync<DoctorsPage>("doctors?pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));
        // روز پیش‌فرض «امروز» و انتخاب خودکار نخستین پزشک تا تقویم و برنامهٔ
        // پزشک همان لحظهٔ بازشدن صفحه پر شوند (گزارش مالک: «روزها و برنامهٔ
        // پزشک انتخابی نمی‌آید» — پیش‌تر تا کلیک کاربر روی یک روز/پزشک، هیچ
        // بارگذاری‌ای رخ نمی‌داد).
        if (SelectedDate is null)
        {
            SelectedDate = DateTime.Today;
            SelectedDateShamsi = PersianDateHelper.ToShamsi(DateTime.Today);
        }
        if (SelectedDoctor is null && Doctors.Count > 0)
            SelectedDoctor = Doctors[0];
        await LoadTodayAppointmentsAsync();
        await SearchListAsync();

        // ═══ چرخهٔ ۷۴ (گزارش مالک) ═══ «با انتخاب بیمار و زدن دکمه ثبت در لیست،
        // میگه ثبت شد ولی در لیست نشون نمیده» — ریشهٔ اصلی: تب «لیست انتظار»
        // هرگز در شروع بارگذاری نمی‌شد (فقط با دکمهٔ نوسازی) و کاربر فهرستِ خالی
        // می‌دید؛ اکنون هم‌زمان با باز شدن صفحه بارگذاری می‌شود.
        await LoadWaitlistAsync();
    }

    /* ─── جستجوی بیمار با Debounce ۳۰۰ms ─── */

    /// <summary>تغییر متن جستجو → راه‌اندازی تایمر Debounce</summary>
    partial void OnPatientSearchTextChanged(string value)
    {
        _debounce?.Stop();
        if (value.Length < 2)
        {
            PatientSearchResults.Clear();
            return;
        }
        _debounce = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await SearchPatientAsync();
        };
        _debounce.Start();
    }

    /// <summary>جستجوی بیمار از سرور</summary>
    private async Task SearchPatientAsync()
    {
        var response = await _api.GetAsync<PatientSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearchText.Trim())}");
        PatientSearchResults.Clear();
        if (response?.Data is null) return;
        foreach (var p in response.Data.Take(10))
            PatientSearchResults.Add(new QuickPatient(p.Id,
                $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile,
                p.InsuranceName, p.Allergies, p.LastVisitDateShamsi));
    }

    /// <summary>انتخاب بیمار از نتایج</summary>
    [RelayCommand]
    private void SelectPatient(object? param)
    {
        if (param is QuickPatient patient)
        {
            SelectedPatient = patient;
            PatientSearchText = patient.FullName;
            PatientSearchResults.Clear();
        }
    }

    /// <summary>پیام وضعیت تقویم/برنامهٔ پزشک — زیر تقویم دیده می‌شود تا اگر
    /// برنامه‌ای بارگذاری نشد، علت (قطع سرور/نبود برنامه) بی‌صدا نماند.</summary>
    [ObservableProperty]
    private string _calendarStatus = string.Empty;

    /// <summary>خط اطلاعات تقویم هنگام بارگذاری موفق برنامه (روزهای کاری/نوبت‌ها)</summary>
    [ObservableProperty]
    private string _calendarInfo = string.Empty;

    /// <summary>تغییر پزشک → بارگذاری تقویم ماه + اسلات‌های روز انتخابی</summary>
    partial void OnSelectedDoctorChanged(DoctorOption? value)
    {
        if (value is not null)
        {
            _ = LoadCalendarAsync();
            _ = LoadSlotsAsync();
        }
    }

    // ═══ چرخهٔ ۷۴ (گزارش مالک): «با انتخاب پزشک، در تقویم فقط برنامه ماه جاری ═══
    // علامت گذاری میکنه و با عوض کردن ماه دیگه برنامه مشخص نیست» — ریشه:
    // فلش‌های داخلی خودِ کنترل تقویم فقط DisplayMonth/Year را جابه‌جا می‌کردند
    // و هیچ‌کس رویدادشان را به بارگذاری نشانگرها وصل نمی‌کرد؛ فقط دکمه‌های
    // بیرونی (که حذف شدند) LoadCalendarAsync را صدا می‌زدند. اکنون تغییرِ ماه/
    // سال از «هر مسیری» (فلش داخلی، ناوبری ماه/سال کنترل، مقداردهی فرم) با
    // گارد ضدتلاطم (debounce ۲۵۰ms — جابه‌جایی دی→بهمن هم سال را عوض می‌کند)
    // به بارگذاری نشانگرهای همان ماه می‌انجامد.
    partial void OnDisplayMonthChanged(int value) => ScheduleCalendarReload();

    /// <summary>تغییر سال نمایشی تقویم → بارگذاری نشانگرهای همان ماه</summary>
    partial void OnDisplayYearChanged(int value) => ScheduleCalendarReload();

    /// <summary>تایمر ضدتلاطم بارگذاری تقویم — هر تغییر پیاپیِ ماه/سال فقط یک GET</summary>
    private System.Windows.Threading.DispatcherTimer? _calendarReloadDebounce;

    /// <summary>زمان‌بندی بارگذاری نشانگرهای ماه نمایشی پس از توقف چرخش ماه/سال</summary>
    private void ScheduleCalendarReload()
    {
        _calendarReloadDebounce?.Stop();
        _calendarReloadDebounce = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(250) };
        _calendarReloadDebounce.Tick += (_, _) =>
        {
            _calendarReloadDebounce!.Stop();
            _ = LoadCalendarAsync();
        };
        _calendarReloadDebounce.Start();
    }

    /// <summary>
    /// بارگذاری نشانگرهای تقویم ماه جاری — خطای سرور/شبکه نباید برنامه را
    /// ببندد یا تقویم را خالی از روز کند: روزها با دادهٔ خالی هم رندر می‌شوند.
    /// </summary>
    private async Task LoadCalendarAsync()
    {
        if (SelectedDoctor is null) return;
        try
        {
        var response = await _api.GetAsync<CalendarSummaryResponse>(
            $"doctors/{SelectedDoctor.Id}/calendar-summary?year={DisplayYear}&month={DisplayMonth}");
        if (response?.Data?.Days is null)
        {
            CalendarStatus = "برنامهٔ پزشک از سرور دریافت نشد — اتصال سرور را بررسی کنید و دوباره تلاش کنید.";
            return;
        }
        var scheduledDays = response.Data.Days.Count(d => d.HasSchedule && !d.IsOff);
        var totalAppointments = response.Data.Days.Sum(d => d.BookedSlots);
        CalendarStatus = scheduledDays == 0
            ? "برای این پزشک در این ماه برنامهٔ کاری ثبت نشده است (از صفحهٔ پزشکان، برنامهٔ هفتگی را ثبت کنید)."
            : string.Empty;
        CalendarInfo = scheduledDays > 0
            ? $"برنامهٔ این ماه بارگذاری شد: {scheduledDays} روز کاری، {totalAppointments} نوبت ثبت‌شده."
            : string.Empty;
        if (scheduledDays == 0) CalendarInfo = string.Empty;

        var marked = new Dictionary<DateTime, int>();
        var disabled = new List<DateTime>();
        var stats = new Dictionary<DateTime, CalendarDayStat>();
        foreach (var day in response.Data.Days)
        {
            var g = PersianDateHelper.ToGregorian(day.DateShamsi);
            if (g is null) continue;
            var date = g.Value.Date;
            // آمار هر روزِ دارای برنامه نگه داشته می‌شود تا جزئیات (نوار
            // ظرفیت و شمار نوبت) حتی برای روزهای پُر هم دیده شود.
            if (day.HasSchedule && day.TotalSlots > 0)
                stats[date] = new CalendarDayStat(day.BookedSlots, day.TotalSlots);
            // غیرقابل‌کلیک فقط تعطیل/غیبت/بدون‌برنامه است؛ «پُر بودن» را خود
            // کنترل با برچسب تکمیل نشان می‌دهد، نه با قفل‌کردن همهٔ روزها.
            if (day.IsOff)
                disabled.Add(date);
            else if (day.FreeSlots > 0)
                marked[date] = day.FreeSlots;
        }
        MarkedDates = marked;
        DisabledDates = disabled;
        DayStats = stats;
        _ = LoadMonthStatsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCalendar failed: {ex.Message}");
            MarkedDates ??= new Dictionary<DateTime, int>();
            DayStats ??= new Dictionary<DateTime, CalendarDayStat>();
            CalendarStatus = "خطا در دریافت برنامهٔ پزشک — اتصال به سرور برقرار نیست یا سرور پاسخ نمی‌دهد.";
            CalendarInfo = string.Empty;
        }
    }

    /// <summary>
    /// آمار ۱۲ ماه سال نمایشی برای نمای انتخاب ماه تقویم — یک بار برای هر
    /// (پزشک، سال)؛ جمع نوبت‌ها، ظرفیت کل و روزهای کاری هر ماه.
    /// </summary>
    private async Task LoadMonthStatsAsync()
    {
        if (SelectedDoctor is null) return;
        var key = $"{SelectedDoctor.Id}|{DisplayYear}";
        if (_monthStatsKey == key) return;
        _monthStatsKey = key;
        var doctorId = SelectedDoctor.Id;
        var year = DisplayYear;
        try
        {
        var result = new Dictionary<int, CalendarMonthStat>();
        for (var month = 1; month <= 12; month++)
        {
            var response = await _api.GetAsync<CalendarSummaryResponse>(
                $"doctors/{doctorId}/calendar-summary?year={year}&month={month}");
            if (response?.Data?.Days is null) continue;
            var booked = 0; var total = 0; var workDays = 0;
            foreach (var day in response.Data.Days)
            {
                if (!day.HasSchedule || day.IsOff) continue;
                workDays++;
                booked += day.BookedSlots;
                total += day.TotalSlots;
            }
            result[month] = new CalendarMonthStat(booked, total, workDays);
        }
        // پاسخ دیررسِ پزشک/سال قبلی روی وضعیت فعلی ننشیند
        if (SelectedDoctor?.Id == doctorId && DisplayYear == year)
            MonthStats = result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMonthStats failed: {ex.Message}");
            _monthStatsKey = string.Empty; // دفعهٔ بعد دوباره تلاش شود
        }
    }

    // چرخهٔ ۷۴: فرمان‌های بیرونی «ماه بعد/قبل» حذف شدند — ناوبری ماه از فلش‌های
    // داخلی خودِ کنترل تقویم انجام می‌شود (دو ردیف دکمهٔ تکراری حذف شد) و
    // بارگذاری نشانگرها با OnDisplayMonthChanged/OnDisplayYearChanged خودکار است.

    /// <summary>انتخاب تاریخ از تقویم → بارگذاری اسلات‌ها</summary>
    [RelayCommand]
    private void SelectDate(DateTime? date)
    {
        if (date is null) return;
        SelectedDate = date;
        SelectedDateShamsi = PersianDateHelper.ToShamsi(date.Value);
        _ = LoadSlotsAsync();
    }

    /// <summary>بارگذاری اسلات‌های روز انتخابی</summary>
    private async Task LoadSlotsAsync()
    {
        Slots.Clear();
        SlotGroups.Clear();
        _pinnedSlot = null;
        DetailSlot = null;
        SelectedSlot = null;
        if (SelectedDoctor is null || SelectedDate is null) return;
        try
        {
        var response = await _api.GetAsync<SlotsResponse>(
            $"appointments/doctor/{SelectedDoctor.Id}/available-slots?date={Uri.EscapeDataString(SelectedDateShamsi)}");
        if (response?.Data is null) return;
        var isToday = SelectedDate?.Date == DateTime.Today;
        var nowTime = TimeOnly.FromDateTime(DateTime.Now);
        foreach (var s in response.Data)
        {
            var past = isToday && TimeOnly.TryParse(s.Time, out var st) && st < nowTime;
            Slots.Add(new SlotVM
            {
                Time = s.Time,
                IsAvailable = s.IsAvailable && !past,
                IsBreak = s.IsBreak,
                PatientName = s.PatientName,
                RoomName = s.RoomName,
                Fee = s.Fee,
                PatientGender = s.PatientGender,
                PatientAge = s.PatientAge,
                PatientInsurance = s.PatientInsurance,
                PatientNationalCode = s.PatientNationalCode,
                PatientPhone = s.PatientPhone,
                IsPast = past
            });
        }
        RefreshSlotStats();
        BuildSlotGroups();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadSlots failed: {ex.Message}");
        }
    }

    /// <summary>انتخاب اسلات</summary>
    [RelayCommand]
    private void SelectSlot(SlotVM slot)
    {
        if (!slot.IsAvailable || slot.IsPast) return;
        foreach (var s in Slots) s.IsSelected = false;
        slot.IsSelected = true;
        SelectedSlot = slot;
        SelectedRoom = slot.RoomName;
        EstimatedFee = slot.Fee;
    }

    /// <summary>گروه‌های ناحیه‌ای اسلات (صبح/ظهر/عصر)</summary>
    public System.Collections.ObjectModel.ObservableCollection<SlotGroup> SlotGroups { get; } = new();

    /// <summary>فیلتر فعلی اسلات‌ها: all/free/booked</summary>
    [ObservableProperty]
    private string _slotFilter = "all";

    /// <summary>اسلاتی که نوار جزئیات اکنون نشان می‌دهد (هاور موقت یا سنجاق‌شده)</summary>
    [ObservableProperty]
    private SlotVM? _detailSlot;

    /// <summary>اسلات سنجاق‌شده با کلیک</summary>
    private SlotVM? _pinnedSlot;

    /// <summary>آمار روز اسلات‌ها</summary>
    [ObservableProperty] private int _freeCount;
    [ObservableProperty] private int _bookedCount;
    [ObservableProperty] private int _breakCount;
    [ObservableProperty] private int _occupancyPercent;
    [ObservableProperty] private int _filteredCount;

    /// <summary>بازسازی آمار سربرگ اسلات‌ها (آزاد/رزرو/استراحت/درصد پرشدن)</summary>
    private void RefreshSlotStats()
    {
        FreeCount = Slots.Count(s => s.IsAvailable && !s.IsBreak && !s.IsPast);
        BookedCount = Slots.Count(s => s.IsBooked);
        BreakCount = Slots.Count(s => s.IsBreak);
        var denom = Slots.Count(s => !s.IsBreak);
        OccupancyPercent = denom == 0 ? 0 : (int)Math.Round(100.0 * Slots.Count(s => s.IsBooked || s.IsPast) / denom);
    }

    /// <summary>بازسازی گروه‌های ناحیه‌ای بر پایهٔ فیلتر فعلی (بدون دست‌کاری انتخاب)</summary>
    public void BuildSlotGroups()
    {
        SlotGroups.Clear();
        IEnumerable<SlotVM> q = Slots;
        q = SlotFilter switch
        {
            "free" => q.Where(s => s.IsAvailable && !s.IsBreak && !s.IsPast),
            "booked" => q.Where(s => s.IsBooked),
            _ => q
        };
        var list = q.ToList();
        FilteredCount = list.Count;
        foreach (var (title, icon) in new[] { ("صبح", "🌅"), ("ظهر", "☀️"), ("عصر", "🌇") })
        {
            var g = new SlotGroup { Title = title, Icon = icon };
            foreach (var s in list.Where(s => s.Group == title)) g.Items.Add(s);
            if (g.Items.Count > 0) SlotGroups.Add(g);
        }
    }

    /// <summary>تغییر فیلتر اسلات‌ها از چیپ‌های فیلتر</summary>
    [RelayCommand]
    private void SetSlotFilter(string? filter)
    {
        SlotFilter = string.IsNullOrWhiteSpace(filter) ? "all" : filter;
        BuildSlotGroups();
    }

    /// <summary>هاور روی اسلات رزروشده: نمایش موقت در نوار جزئیات</summary>
    public void HoverSlot(SlotVM slot)
    {
        if (slot.IsBooked) DetailSlot = slot;
    }

    /// <summary>خروج موس: بازگشت به اسلات سنجاق‌شده (یا خالی)</summary>
    public void UnhoverSlot(SlotVM slot)
    {
        if (ReferenceEquals(DetailSlot, slot)) DetailSlot = _pinnedSlot;
    }

    /// <summary>کلیک روی اسلات رزروشده: سنجاق/برداشتن سنجاق نوار جزئیات</summary>
    public void TogglePinSlot(SlotVM slot)
    {
        if (!slot.IsBooked) return;
        if (ReferenceEquals(_pinnedSlot, slot))
        {
            slot.IsPinned = false;
            _pinnedSlot = null;
            DetailSlot = null;
        }
        else
        {
            if (_pinnedSlot is not null) _pinnedSlot.IsPinned = false;
            _pinnedSlot = slot;
            slot.IsPinned = true;
            DetailSlot = slot;
        }
    }

    /// <summary>ثبت نهایی نوبت — اعتبارسنجی + POST</summary>
    [RelayCommand]
    private async Task BookAsync()
    {
        if (SelectedPatient is null) { _snackbar.Error("ابتدا بیمار را انتخاب کنید"); return; }
        if (SelectedDoctor is null) { _snackbar.Error("ابتدا پزشک را انتخاب کنید"); return; }
        if (SelectedSlot is null) { _snackbar.Error("ابتدا ساعت نوبت را انتخاب کنید"); return; }

        var body = new
        {
            patientId = SelectedPatient.Id,
            doctorId = SelectedDoctor.Id,
            dateShamsi = SelectedDateShamsi,
            time = SelectedSlot.Time,
            type = SelectedTypeIndex + 1,
            source = 1,
            // ═══ چرخهٔ ۷۸ — اولویت انتخابی کاربر ارسال می‌شود ═══
            // پیش‌تر ارسال نمی‌شد و سرور هم «عادی» ذخیره می‌کرد؛ در نتیجه تب
            // «فهرست نوبت‌ها» همه را «معمولی» نشان می‌داد (گزارش مالک).
            // نمایهٔ صفرم = عادی(۳) / نخست = بالا(۲) / دوم = اورژانس(۱).
            priority = SelectedPriorityIndex switch { 2 => 1, 1 => 2, _ => 3 },
            notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        IsBusy = true;
        var response = await _api.PostAsync<BookResponse>("appointments", body);
        IsBusy = false;

        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? (response is null ? "ثبت نوبت ناموفق بود" : "این ساعت رزرو شده است"));
            return;
        }

        var code = response.Data.AppointmentCode ?? "—";
        _snackbar.Success($"نوبت ثبت شد — کد پیگیری: {code}");

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — قبض نوبت ═══
        // «یک چاپگر پیش‌فرض برای نوبت‌دهی باشد تا قبض‌های نوبت با آن چاپ شود»:
        // اگر «چاپ خودکار قبض» در تنظیمات فعال باشد، قبض فیشی همان لحظه با
        // چاپگر نوبت‌دهی انتخاب‌شده چاپ می‌شود (بدون هیچ دیالوگی). نام چاپگر و
        // پرچم خودکار در ذخیره‌گاه محلی هم آینه می‌شوند تا بدون درخواست اضافهٔ
        // شبکه در دسترس باشند.
        var ticketPatient = SelectedPatient.FullName;
        var ticketDoctor = SelectedDoctor.Name;
        var ticketDate = SelectedDateShamsi;
        var ticketTime = SelectedSlot?.Time ?? string.Empty;
        var ticketType = VisitTypes.ElementAtOrDefault(Math.Clamp(SelectedTypeIndex, 0, VisitTypes.Length - 1));
        if (LocalSettingsStore.Get("Printer.AutoPrintTicket") == "true")
        {
            var ticketPrinter = LocalSettingsStore.Get("Printer.TicketPrinter");
            var clinicTitle = LocalSettingsStore.Get("Printer.ClinicName") is { Length: > 0 } clinic
                ? clinic : "درمانگاه سهند";
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                Services.AppointmentTicketPrinter.Print(
                    ticketPrinter, clinicTitle, code, ticketPatient, ticketDoctor,
                    ticketDate, ticketTime, $"نوبت {ticketType}"));
        }

        SelectedPatient = null;
        PatientSearchText = string.Empty;
        Notes = string.Empty;
        SelectedSlot = null;
        foreach (var s in Slots) s.IsSelected = false;
        // چرخهٔ ۷۶ — ویزارد برای نوبت بعدی به گام نخست بازمی‌گردد
        WizardStep = 0;
        await LoadSlotsAsync();
        await LoadTodayAppointmentsAsync();
    }

    /// <summary>بارگذاری نوبت‌های امروز — جدول پایین تب رزرو</summary>
    private async Task LoadTodayAppointmentsAsync()
    {
        var response = await _api.GetAsync<TodayResponse>("appointments");
        TodayAppointments.Clear();
        if (response?.Data is null) return;
        foreach (var a in response.Data)
            TodayAppointments.Add(new TodayRow(a.Id, a.Time, a.PatientName, a.DoctorName,
                a.Status, StatusText(a.Status)));
    }

    /* ═══════════ تب ۲: فهرست ═══════════ */

    /// <summary>جستجوی فهرست با فیلترها</summary>
    [RelayCommand]
    private async Task SearchListAsync()
    {
        IsBusy = true;
        var body = new
        {
            fromDateShamsi = string.IsNullOrWhiteSpace(FromDateShamsi) ? null : FromDateShamsi.Trim(),
            toDateShamsi = string.IsNullOrWhiteSpace(ToDateShamsi) ? null : ToDateShamsi.Trim(),
            doctorId = DoctorFilterIndex <= 0 ? null : (long?)Doctors[DoctorFilterIndex - 1].Id,
            status = StatusFilterIndex <= 0 ? null : (int?)StatusFilterIndex,
            type = TypeFilterIndex <= 0 ? null : (int?)(TypeFilterIndex + 1),
            source = SourceFilterIndex <= 0 ? null : (int?)(SourceFilterIndex + 1),
            search = string.IsNullOrWhiteSpace(ListSearch) ? null : ListSearch.Trim(),
            pageNumber = PageNumber,
            pageSize = PageSize
        };
        var response = await _api.PostAsync<SearchResponse>("appointments/search", body);
        if (response?.Data is not null)
        {
            Rows = new ObservableCollection<AppointmentListRow>(
                response.Data.Select(MapRow));
            PageNumber = response.PageNumber;
            TotalPages = response.TotalPages;
            TotalCount = response.TotalCount;

            // ═══ چرخهٔ ۷۴ — مرتب‌سازی چندستونه پس از هر جستجو حفظ می‌شود ═══
            // کلکشن Rows تازه‌سازی می‌شود و نمای پیشین از بین می‌رود؛ وضعیت
            // ذخیره‌شده دوباره روی نمای مجموعهٔ جدید اعمال می‌شود
            ApplySortToView();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت فهرست ناموفق بود";
        }
        IsBusy = false;
    }

    /// <summary>نگاشت ردیف با Badge وضعیت + بج رنگی اولویت (چرخهٔ ۷۵)</summary>
    private AppointmentListRow MapRow(AppointmentSearchItem a) => new(
        a.Id, false, a.AppointmentCode, a.PatientName, a.PatientMobile,
        a.DoctorName, a.SpecialtyName, a.DateShamsi, a.Time,
        a.Status, StatusText(a.Status), StatusColors[Math.Clamp(a.Status - 1, 0, 6)],
        a.Type, TypeLabels[Math.Clamp(a.Type - 1, 0, 3)],
        a.Source, SourceLabels[Math.Clamp(a.Source - 1, 0, 2)],
        a.EstimatedFee,
        // ═══ چرخهٔ ۷۵ — اولویت با رنگ خودش (گزارش مالک: «اولویت رو هم با رنگ
        // خودش در لیست نشون بده») — ۱ اورژانس/۲ بالا/۳ یا هر چیز دیگر عادی.
        // چرخهٔ ۷۹: واژگان دقیقاً هم‌سانِ فرم ثبت («عادی/بالا/اورژانس») ═══
        a.Priority switch { 1 => "🚨 اورژانس", 2 => "⬆ بالا", _ => "عادی" },
        a.Priority switch { 1 => "#C62828", 2 => "#E65100", _ => "#78909C" });

    /// <summary>متن فارسی وضعیت</summary>
    private string StatusText(int status) =>
        status >= 1 && status <= 7 ? StatusLabels[status - 1] : "؟";

    /// <summary>تغییر صفحه فهرست</summary>
    [RelayCommand]
    private async Task PageChangedAsync(object? page)
    {
        if (page is int p && p >= 1) PageNumber = p;
        await SearchListAsync();
    }

    /// <summary>تأیید حضور نوبت</summary>
    [RelayCommand]
    private async Task ConfirmArrivalAsync()
    {
        if (SelectedRow is null) { _snackbar.Error("ابتدا یک نوبت انتخاب کنید"); return; }
        var ok = await _api.PutAsync($"appointments/{SelectedRow.Id}/status",
            new { status = 3, reason = (string?)null });
        if (ok) { _snackbar.Success("حضور بیمار تأیید شد"); await SearchListAsync(); }
        else _snackbar.Error(_api.LastError ?? "تغییر وضعیت ناموفق بود");
    }

    /// <summary>لغو تک‌نوبت</summary>
    [RelayCommand]
    private async Task CancelOneAsync()
    {
        if (SelectedRow is null) { _snackbar.Error("ابتدا یک نوبت انتخاب کنید"); return; }
        var confirmed = Views.AppDialog.Confirm("تأیید لغو نوبت", $"لغو نوبت «{SelectedRow.PatientName}» در {SelectedRow.DateShamsi} ساعت {SelectedRow.Time}؟", Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;
        var ok = await _api.PutAsync($"appointments/{SelectedRow.Id}/status",
            new { status = 6, reason = "لغو از فهرست نوبت‌ها" });
        if (ok) { _snackbar.Success("نوبت لغو شد"); await SearchListAsync(); }
        else _snackbar.Error(_api.LastError ?? "لغو ناموفق بود");
    }

    /// <summary>لغو دسته‌ای نوبت‌های انتخاب‌شده</summary>
    [RelayCommand]
    private async Task BatchCancelAsync()
    {
        var selected = Rows.Where(r => r.IsSelected).Select(r => r.Id).ToList();
        if (selected.Count == 0) { _snackbar.Error("ابتدا نوبت‌ها را با ستون انتخاب علامت بزنید"); return; }
        var confirmed = Views.AppDialog.Confirm("لغو دسته‌ای", $"{selected.Count} نوبت انتخاب شده است. همه لغو شوند؟", Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        var response = await _api.PostAsync<BatchResponse>("appointments/batch-cancel",
            new { ids = selected, reason = "لغو دسته‌ای" });
        if (response?.Data is not null)
        {
            _snackbar.Success($"{response.Data.Succeeded} نوبت لغو شد");
            await SearchListAsync();
        }
        else _snackbar.Error(_api.LastError ?? "لغو دسته‌ای ناموفق بود");
    }

    /// <summary>ارسال پیامک یادآوری نوبت‌های فردا</summary>
    [RelayCommand]
    private async Task SendRemindersAsync()
    {
        var response = await _api.PostAsync<BatchResponse>("appointments/send-reminders", new { });
        if (response?.Data is not null)
            _snackbar.Success($"{response.Data.Succeeded} پیامک یادآوری در صف ارسال قرار گرفت");
        else
            _snackbar.Error(_api.LastError ?? "ارسال یادآوری ناموفق بود");
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class DoctorsPage
    {
        public bool IsSuccess { get; set; }
        public List<DoctorDto>? Data { get; set; }
    }

    public sealed class DoctorDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? SpecialtyName { get; set; }

        /// <summary>حق ویزیت به ریال — چرخه بیست‌ویکم: برای پذیرش</summary>
        public decimal ConsultationFee { get; set; }
    }

    public sealed class PatientSearchResponse
    {
        public bool IsSuccess { get; set; }
        public List<PatientQuick>? Data { get; set; }
    }

    public sealed class PatientQuick
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? InsuranceName { get; set; }
        public string? Allergies { get; set; }
        public string? LastVisitDateShamsi { get; set; }
    }

    public sealed class CalendarSummaryResponse
    {
        public bool IsSuccess { get; set; }
        public CalendarData? Data { get; set; }
    }

    public sealed class CalendarData
    {
        public List<CalendarDay>? Days { get; set; }
    }

    public sealed class CalendarDay
    {
        public string DateShamsi { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public int FreeSlots { get; set; }
        public bool HasSchedule { get; set; }
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public bool IsOff { get; set; }
    }

    public sealed class SlotsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SlotItem>? Data { get; set; }
    }

    public sealed class SlotItem
    {
        public string Time { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? PatientName { get; set; }
        public bool IsBreak { get; set; }
        public string? RoomName { get; set; }
        public decimal Fee { get; set; }
        public string? PatientGender { get; set; }
        public int? PatientAge { get; set; }
        public string? PatientInsurance { get; set; }
        public string? PatientNationalCode { get; set; }
        public string? PatientPhone { get; set; }
    }

    public sealed class BookResponse
    {
        public bool IsSuccess { get; set; }
        public BookData? Data { get; set; }
    }

    public sealed class BookData
    {
        public long Id { get; set; }
        public string? AppointmentCode { get; set; }
    }

    public sealed class TodayResponse
    {
        public bool IsSuccess { get; set; }
        public List<TodayItem>? Data { get; set; }
    }

    public sealed class TodayItem
    {
        public long Id { get; set; }
        public string Time { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int Status { get; set; }
    }

    /// <summary>پاسخ جستجوی نوبت‌ها — تک‌لایه منطبق بر ToPagedResponse سرور (چرخه ۱۹)</summary>
    public sealed class SearchResponse
    {
        public bool IsSuccess { get; set; }
        public List<AppointmentSearchItem>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public sealed class AppointmentSearchItem
    {
        public long Id { get; set; }
        public string? AppointmentCode { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientMobile { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? SpecialtyName { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int Status { get; set; }
        public int Type { get; set; }
        public int Source { get; set; }
        public decimal EstimatedFee { get; set; }
        // اولویت نوبت — ۱ اورژانس/۲ فوری/۳ معمولی (چرخهٔ ۷۵)
        public int Priority { get; set; } = 3;
    }

    public sealed class BatchResponse
    {
        public bool IsSuccess { get; set; }
        public BatchData? Data { get; set; }
    }

    public sealed class BatchData
    {
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
    }

    /* ═══════════ لیست انتظار — چرخه ۱۳ (درخواست ۳-۴) ═══════════ */

    /// <summary>بارگذاری لیست انتظار</summary>
    [RelayCommand]
    public async Task LoadWaitlistAsync()
    {
        var response = await _api.GetAsync<WaitlistEnvelope>("appointments/waitlist");
        // ═══ چرخهٔ ۷۴ (گزارش مالک) ═══ «میگه ثبت شد ولی در لیست نشون نمیده» —
        // شکستِ بی‌صدای GET پیش‌تر فهرست قبلی/خالی را نگه می‌داشت و کاربر هیچ
        // بازخوردی نمی‌گرفت؛ اکنون خطای شبکه/مجوز با توست فارسی اعلام می‌شود
        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت لیست انتظار ناموفق بود");
            return;
        }
        Waitlist = new ObservableCollection<WaitlistRow>(
            response.Data.Select(w => new WaitlistRow(
                w.Id, w.PatientName, w.PatientMobile, w.DoctorName,
                w.PreferredText, w.PriorityText, w.StatusText, w.StatusColor,
                w.NotifiedText, w.Notes)));
    }

    /// <summary>جستجوی بیمار برای لیست انتظار — با debounce</summary>
    [RelayCommand]
    private async Task WaitlistPatientSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(WaitlistPatientSearch) || WaitlistPatientSearch.Trim().Length < 3)
        {
            WaitlistPatientResults.Clear();
            return;
        }
        var response = await _api.GetAsync<PatientSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(WaitlistPatientSearch.Trim())}");
        WaitlistPatientResults.Clear();
        if (response?.Data is null) return;
        foreach (var p in response.Data.Take(10))
            WaitlistPatientResults.Add(new QuickPatient(p.Id,
                $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile,
                p.InsuranceName, p.Allergies, p.LastVisitDateShamsi));
    }

    /// <summary>ثبت بیمار در لیست انتظار</summary>
    [RelayCommand]
    private async Task AddToWaitlistAsync()
    {
        if (WaitlistPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو و انتخاب کنید"); return; }
        if (WaitlistFromDate is not { } from || WaitlistToDate is not { } to)
        { _snackbar.Error("بازه تاریخ دلخواه را کامل کنید"); return; }
        if (to < from) { _snackbar.Error("تاریخ پایان قبل از شروع است"); return; }

        var ok = await _api.PostAsync("appointments/waitlist", new
        {
            patientId = WaitlistPatient.Id,
            doctorId = WaitlistDoctorIndex >= 0 && WaitlistDoctorIndex < Doctors.Count
                ? Doctors[WaitlistDoctorIndex].Id
                : (long?)null,
            specialtyId = (long?)null,
            // چرخهٔ ۷۹ (ریشه‌کنی «سال ۰۷۸۴ ستون بازهٔ دلخواه»): تاریخ‌های ارسالی
            // به سرور «صریحاً» با فرهنگ خنثی قالب‌بندی می‌شوند — پیش‌تر تقویمِ
            // فرهنگ fa-IR اجزای جلالی می‌فرستاد (1405-07-22) و سرور آن را سال
            // ۱۴۰۵ میلادی می‌پنداشت؛ نمایش مجدد شمسی سال ۰۷۸۴ می‌ساخت.
            preferredFrom = from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            preferredTo = to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            preferredTimeOfDay = WaitlistTimeIndex switch { 1 => "صبح", 2 => "عصر", 3 => "شب", _ => (string?)null },
            priority = WaitlistPriorityIndex switch { 2 => 1, 1 => 2, _ => 3 },
            notes = WaitlistNotes
        });

        if (ok)
        {
            _snackbar.Success($"«{WaitlistPatient.FullName}» در لیست انتظار ثبت شد");
            WaitlistPatient = null;
            WaitlistPatientSearch = string.Empty;
            WaitlistPatientResults.Clear();
            WaitlistNotes = null;
            await LoadWaitlistAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ثبت در لیست انتظار ناموفق بود");
    }

    /// <summary>لغو ردیف لیست انتظار</summary>
    [RelayCommand]
    private async Task CancelWaitlistAsync(WaitlistRow? row)
    {
        if (row is null) return;
        var ok = await _api.PostAsync($"appointments/waitlist/{row.Id}/cancel", new { });
        if (ok) { _snackbar.Success("ردیف لیست انتظار لغو شد"); await LoadWaitlistAsync(); }
        else _snackbar.Error(_api.LastError ?? "لغو ناموفق بود");
    }

    /// <summary>انتخاب بیمار از نتایج جستجوی لیست انتظار</summary>
    [RelayCommand]
    private void SelectWaitlistPatient(object? param)
    {
        if (param is QuickPatient patient)
        {
            WaitlistPatient = patient;
            WaitlistPatientResults.Clear();
            WaitlistPatientSearch = patient.FullName;
        }
    }

    /// <summary>پاسخ فهرست لیست انتظار</summary>
    public sealed class WaitlistEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<WaitlistDto>? Data { get; set; }
    }

    /// <summary>ردیف API لیست انتظار</summary>
    public sealed class WaitlistDto
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = "";
        public string PatientMobile { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string PreferredText { get; set; } = "";
        public string PriorityText { get; set; } = "";
        public string StatusText { get; set; } = "";
        public string StatusColor { get; set; } = "#78909C";
        public string NotifiedText { get; set; } = "—";
        public string? Notes { get; set; }
    }
}

/// <summary>
/// ردیف نوبت امروز — جدول پایین تب رزرو
/// </summary>
public record TodayRow(long Id, string Time, string PatientName, string DoctorName, int Status, string StatusText);
