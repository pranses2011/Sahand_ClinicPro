using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Enums;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// نتیجه استعلام بیمه در فرم پذیرش
/// </summary>
public record InquiryState(bool IsDone, bool IsEligible, string Message, string? ApprovalCode, decimal CoveragePercent);

/// <summary>
/// ViewModel پذیرش — گام U5 طبق پرامپت UI بخش ۲ (فرم ۱۰):
/// کارت‌های آماری + جستجوی هوشمند + استعلام بیمه با Timeout + محاسبه خودکار هزینه + صف
/// </summary>
public partial class ReceptionsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "پذیرش بیمار";

    /* ── کارت‌های آماری ── */
    [ObservableProperty] private int _waitingCount;
    [ObservableProperty] private int _receivedCount;
    [ObservableProperty] private int _inQueue;
    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private string _todayRevenueDisplay = "۰";

    /// <summary>ردیف انتخاب‌شدهٔ صف — برای عملیات ردیفی مثل چاپ مجدد قبض</summary>
    [ObservableProperty] private ReceptionRow? _selectedReception;

    /// <summary>تاریخ تاریخچهٔ پذیرش (شمسی) — برای چاپ مجدد قبض روزهای گذشته</summary>
    [ObservableProperty] private string _historyDateShamsi =
        Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(IranClock.Now);

    /// <summary>عنوان فهرست صف — «صف پذیرش امروز» یا تاریخچهٔ تاریخ انتخابی</summary>
    [ObservableProperty] private string _queueTitle = "صف پذیرش امروز";

    /* ── جستجوی هوشمند ── */
    /// <summary>عبارت جستجو — نام/نام‌خانوادگی/کد ملی/کد پیگیری APT/موبایل</summary>
    [ObservableProperty]
    private string _smartSearch = string.Empty;
    private int _patientSearchVersion;
    private long? _selectedAppointmentId;
    private long? _selectedAppointmentPatientId;
    private long? _selectedAppointmentDoctorId;
    private void ClearSelectedAppointment()
    {
        _selectedAppointmentId = null;
        _selectedAppointmentPatientId = null;
        _selectedAppointmentDoctorId = null;
        FoundAppointmentInfo = null;
    }
    partial void OnSmartSearchChanged(string value)
    {
        _patientSearchVersion++;
        ClearSelectedAppointment();
        FoundPatient = null;
        FoundAppointmentInfo = null;
        SelectedSearchResult = null;
        PatientResults = new ObservableCollection<QuickPatient>();
    }

    [ObservableProperty]
    private bool _isSearchingPatient;

    /// <summary>بیمار یافته‌شده</summary>
    [ObservableProperty]
    private QuickPatient? _foundPatient;

    /// <summary>
    /// نتایج جستجوی بیمار — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «جستجو بر اساس نام/نام‌خانوادگی و تطبیق جزئی کد ملی/نام — الان باید کد ملی
    /// را کامل بزنیم» — ریشه: کلاینت نتایج جزئی سرور را با شرط «کد ملی == دقیق»
    /// دوربردارد می‌کرد. اکنون همهٔ نتایج برای «انتخاب» فهرست می‌شوند.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearchResults))]
    private ObservableCollection<QuickPatient> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده از فهرست نتایج جستجو — چرخه بیست‌وسوم</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearchResults))]
    private QuickPatient? _selectedSearchResult;

    /// <summary>
    /// فهرست نتایج فقط تا پیش از انتخاب بیمار دیده شود — چرخه بیست‌وسوم:
    /// منبع ListBox با انتخاب پاک نمی‌شود (انتخاب را باطل می‌کرد) بلکه مخفی می‌شود
    /// </summary>
    public bool ShowSearchResults => FoundPatient is null && PatientResults.Count > 0;

    /// <summary>بازخورد انتخاب از فهرست نتایج — بیمار همان لحظه تنظیم می‌شود</summary>
    partial void OnSelectedSearchResultChanged(QuickPatient? value)
    {
        if (value is null) return;
        FoundPatient = value;
        _snackbar.Success($"بیمار انتخاب شد: {value.FullName}");
    }

    /// <summary>
    /// تغییر بیمار یافته‌شده — چرخهٔ ۴۴ (گزارش مالک): «در فرم پذیرش وقتی بیمار
    /// رو انتخاب می‌کنیم، نوع (بیمه) بر اساس بیمهٔ بیمار انتخاب بشه».
    /// بیمهٔ اصلی ثبت‌شده در پروندهٔ بیمار، بی‌درنگ روی کشوی «نوع بیمه» نشست
    /// می‌گیرد؛ بیمارِ بدون بیمه به «آزاد (بدون بیمه)» برمی‌گردد. کاربر
    /// همچنان می‌تواند بعداً مقدار را عمداً عوض کند (مثلاً بیمهٔ تبعی).
    /// </summary>
    partial void OnFoundPatientChanged(QuickPatient? value)
    {
        InvalidateInquiry();
        if (_selectedAppointmentId is not null && value?.Id != _selectedAppointmentPatientId) ClearSelectedAppointment();
        ApplyPatientInsurance(value);
        if (value is null || IsReceptionOutcomeUnknown || _receptionRecoveryStore is null) return;
        try
        {
            var pending = _receptionRecoveryStore.Load(_invoiceRecoveryServer, value.Id);
            if (pending is not null) RestorePendingReception(pending);
        }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error("بازیابی محلی پذیرش قابل خواندن نیست؛ پیش از ثبت جدید سوابق را بررسی و با پشتیبانی تماس بگیرید.");
        }
    }

    /// <summary>
    /// نشاندن بیمهٔ پروندهٔ بیمار روی کشوی بیمه — چرخهٔ ۴۴.
    /// اگر کشو هنوز از سرور پر نشده باشد (جستجوی خیلی زودهنگام)، این متد
    /// در پایان LoadInsuranceAsync هم دوباره صدا زده می‌شود تا بیمهٔ بیمار
    /// پس از آماده‌شدن فهرست نیز اعمال شود (گارد مسابقهٔ بارگذاری).
    /// </summary>
    private void ApplyPatientInsurance(QuickPatient? patient)
    {
        if (patient is null) return;

        // بیمهٔ پرونده → جایگاهش در کشو؛ نبود بیمه/نبود شرکت در فهرست → آزاد
        var company = patient.InsuranceCompanyId is { } companyId
            ? InsuranceCompanies.FirstOrDefault(c => c.Id == companyId)
            : null;
        var index = company is null ? 0 : InsuranceCompanies.IndexOf(company);
        if (index < 0) index = 0; // شرکت در فهرست نیست (مثلاً غیرفعال شده) → آزاد
        InsuranceIndex = index;

        // ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) ═══
        // «در فرم پذیرش وقتی بیمار رو انتخاب میکنیم، فیلد نوع بیمه بر اساس
        // بیمه بیمار انتخاب بشه» — زنجیرهٔ انتخاب خودکار از چرخهٔ ۴۴ کامل
        // بود ولی بی‌صدا؛ توست تأیید، اعمال آن را دیدنی و قابل راستی‌آزمایی
        // می‌کند (و در نبود بیمه هم کاربر می‌داند چرا «آزاد» نشسته).
        if (company is not null)
            _snackbar.Info($"بیمهٔ پروندهٔ بیمار روی «{company.Name}» تنظیم شد");
        else if (patient.InsuranceCompanyId is not null)
            _snackbar.Warning($"بیمهٔ پرونده («{patient.InsuranceName ?? "نامشخص"}») در فهرست فعال نیست — «آزاد» انتخاب شد");
        else
            _snackbar.Info("بیمار بیمه‌ای ندارد — «آزاد (بدون بیمه)» تنظیم شد");
    }

    /// <summary>نوبت فعال یافته‌شده — اگر بیمار نوبت داشته باشد</summary>
    [ObservableProperty]
    private string? _foundAppointmentInfo;

    /* ── فرم پذیرش ── */

    // ═══ چرخهٔ ۷۶ (گزارش مالک چرخهٔ ۷۴) — فرم مرحله‌ای (ویزارد) پذیرش ═══
    // «ورود اطلاعات و ثبت به‌صورت مرحله‌ای (Wizard) + چیدمان خیلی زیباتر و
    // شکیل‌تر» — فرم بلندِ تک‌صفحه‌ای به سه گامِ گویا شکسته شد:
    //   گام ۰ — بیمار: جستجوی هوشمند + انتخاب + کارت پرونده
    //   گام ۱ — خدمت: نوع پذیرش + پزشک/خدمت + انجام‌دهنده/اتاق + لوازم مصرفی
    //   گام ۲ — ثبت: بیمه/استعلام/ارجاع + یادداشت + محاسبهٔ هزینه + دکمه‌ها
    // پیمایش با دکمه‌های «مرحلهٔ بعد/قبل» و اعتبارسنجیِ همان گام (همان پیام‌های
    // راهنمای اسنک‌بار که پیش‌تر فقط هنگام ثبت دیده می‌شدند).

    /// <summary>
    /// گام جاری ویزارد پذیرش — بازطراحی مالک: ۰ نوع پذیرش (ویزیت پزشک یا
    /// خدمات درمانی با دکمه‌های بزرگ)، ۱ بیمار، ۲ پزشک/خدمت، ۳ ثبت
    /// </summary>
    [ObservableProperty]
    private int _wizardStep;

    /// <summary>حالت ویزیت پزشک فعال است؟ (برای برجسته‌سازی کارت نوع پذیرش)</summary>
    public bool IsDoctorMode => ReceptionModeIndex == 0;

    /// <summary>حالت خدمات درمانی فعال است؟</summary>
    public bool IsServiceMode => ReceptionModeIndex == 1;

    /// <summary>انتخاب «ویزیت پزشک» در گام نوع پذیرش — رفتن خودکار به گام بیمار</summary>
    [RelayCommand]
    private void SelectDoctorMode()
    {
        ReceptionModeIndex = 0;
        WizardStep = 1;
    }

    /// <summary>انتخاب «خدمات درمانی» در گام نوع پذیرش — رفتن خودکار به گام بیمار</summary>
    [RelayCommand]
    private void SelectServiceMode()
    {
        ReceptionModeIndex = 1;
        WizardStep = 1;
    }

    /// <summary>
    /// رفتن مستقیم به گام با کلیک روی عنوان گام — هوشمند: اگر پیش‌نیاز گام
    /// کامل نباشد، کاربر به اولین گام ناتمام برده می‌شود و پیام راهنما می‌بیند
    /// </summary>
    [RelayCommand]
    private void GoToStep(object? parameter)
    {
        if (!int.TryParse(parameter?.ToString(), out var step)) return;
        step = Math.Clamp(step, 0, 3);
        if (step >= 2 && FoundPatient is null)
        {
            _snackbar.Error("ابتدا در گام بیمار، بیمار را جستجو و انتخاب کنید");
            WizardStep = 1;
            return;
        }
        if (step >= 3 && ReceptionModeIndex == 0 && SelectedDoctor is null)
        {
            _snackbar.Error("ابتدا پزشک را انتخاب کنید");
            WizardStep = 2;
            return;
        }
        if (step >= 3 && ReceptionModeIndex == 1 && SelectedServices.Count == 0)
        {
            _snackbar.Error("ابتدا خدمت درمانی را انتخاب کنید");
            WizardStep = 2;
            return;
        }
        WizardStep = step;
    }

    /* ── بخش‌های اختیاری صفحه (پنل مدیریت بالای فرم؛ انتخاب کاربر در
       تنظیمات محلی ذخیره می‌شود تا دفعهٔ بعد همان‌طور بماند) ── */

    /// <summary>نمایش آمار امروز بالای صفحه</summary>
    [ObservableProperty]
    private bool _showStatsPanel = true;

    /// <summary>نمایش صف پذیرش امروز</summary>
    [ObservableProperty]
    private bool _showQueuePanel = true;

    /// <summary>نمایش مراجعه‌کننده‌های اخیر</summary>
    [ObservableProperty]
    private bool _showRecentPanel;

    /// <summary>نمایش نوبت‌های امروز</summary>
    [ObservableProperty]
    private bool _showAppointmentsPanel;

    /// <summary>نمایش خلاصهٔ مالی امروز</summary>
    [ObservableProperty]
    private bool _showFinancePanel;

    partial void OnShowStatsPanelChanged(bool value) => SavePanelPrefs();
    partial void OnShowQueuePanelChanged(bool value) => SavePanelPrefs();
    partial void OnShowRecentPanelChanged(bool value) => SavePanelPrefs();
    partial void OnShowAppointmentsPanelChanged(bool value) => SavePanelPrefs();
    partial void OnShowFinancePanelChanged(bool value) => SavePanelPrefs();

    private void SavePanelPrefs()
    {
        LocalSettingsStore.Set("Reception.Panel.Stats", ShowStatsPanel.ToString());
        LocalSettingsStore.Set("Reception.Panel.Queue", ShowQueuePanel.ToString());
        LocalSettingsStore.Set("Reception.Panel.Recent", ShowRecentPanel.ToString());
        LocalSettingsStore.Set("Reception.Panel.Appointments", ShowAppointmentsPanel.ToString());
        LocalSettingsStore.Set("Reception.Panel.Finance", ShowFinancePanel.ToString());
    }

    private void LoadPanelPrefs()
    {
        if (bool.TryParse(LocalSettingsStore.Get("Reception.Panel.Stats"), out var s)) ShowStatsPanel = s;
        if (bool.TryParse(LocalSettingsStore.Get("Reception.Panel.Queue"), out var q)) ShowQueuePanel = q;
        if (bool.TryParse(LocalSettingsStore.Get("Reception.Panel.Recent"), out var r)) ShowRecentPanel = r;
        if (bool.TryParse(LocalSettingsStore.Get("Reception.Panel.Appointments"), out var a)) ShowAppointmentsPanel = a;
        if (bool.TryParse(LocalSettingsStore.Get("Reception.Panel.Finance"), out var f)) ShowFinancePanel = f;
    }

    /// <summary>مراجعه‌کننده‌های اخیر (از پذیرش‌های امروز) برای انتخاب یک‌کلیکه</summary>
    public ObservableCollection<string> RecentPatients { get; } = new();

    /// <summary>انتخاب مراجعه‌کنندهٔ اخیر — نامش جستجو و انتخاب می‌شود</summary>
    [RelayCommand]
    private async Task SelectRecentPatient(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        SmartSearch = name;
        WizardStep = 1;
        await SmartSearchAsync();
    }

    /// <summary>نوبت‌های امروز (برای پنل اختیاری بالای صفحه)</summary>
    public ObservableCollection<MiniAppointmentRow> TodayAppointmentsMini { get; } = new();

    /// <summary>جمع فاکتورهای امروز (متن نمایشی پنل مالی)</summary>
    [ObservableProperty]
    private string _todayInvoicedText = "—";

    /// <summary>جمع پرداخت‌شدهٔ امروز (متن نمایشی پنل مالی)</summary>
    [ObservableProperty]
    private string _todayPaidText = "—";

    /// <summary>
    /// رفتن به گام بعد — اعتبارسنجی گام جاری با پیام راهنمای فارسی؛
    /// در گام آخر (ثبت) مخفی است و کار با دکمه‌های زنجیرهٔ پذیرش ادامه می‌یابد
    /// </summary>
    [RelayCommand]
    private void NextStep()
    {
        // گام ۰ (نوع پذیرش) پیش‌نیاز ندارد — حالت پیش‌فرض «ویزیت پزشک» است
        if (WizardStep == 1 && FoundPatient is null)
        {
            _snackbar.Error("ابتدا بیمار را جستجو و انتخاب کنید");
            return;
        }
        if (WizardStep == 2)
        {
            if (ReceptionModeIndex == 1 && SelectedServices.Count == 0)
            {
                _snackbar.Error("ابتدا دست‌کم یک خدمت درمانی انتخاب کنید");
                return;
            }
            if (ReceptionModeIndex == 0 && SelectedDoctor is null)
            {
                _snackbar.Error("ابتدا پزشک را انتخاب کنید");
                return;
            }
        }
        if (WizardStep < 3)
            WizardStep++;
    }

    /// <summary>بازگشت به گام قبل — در گام نخست مخفی است</summary>
    [RelayCommand]
    private void BackStep()
    {
        if (WizardStep > 0)
            WizardStep--;
    }

    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — «پذیرش خدمت درمانی» ═══
    // «برای خدمات تزریقات و پانسمان هیچ قابلیتی نیست؛ بیمارِ آمپول/سرم/
    // پانسمان یا فقط آزمایش/عکس چه شود؟ قسمت پذیرش فقط پزشک دارد» — دو حالت:
    //   ۰ ویزیت پزشک (رفتار پیشین) — پزشک الزامی
    //   ۱ خدمت درمانی — خدمت از تعرفه‌ها (تزریقات/پانسمان/آزمایش/تصویربرداری…)
    //     انتخاب می‌شود؛ پزشک نیست و انجام‌دهنده از پرسنل (اختیاری) است.

    /// <summary>ایندکس حالت پذیرش — ۰ ویزیت پزشک، ۱ خدمت درمانی</summary>
    [ObservableProperty]
    private int _receptionModeIndex;

    /// <summary>
    /// تغییر حالت پذیرش → بازمحاسبهٔ هزینه — چرخهٔ بیست‌وششم: جابه‌جایی میان
    /// ویزیت پزشک و خدمت درمانی، بخش هزینه را از حق ویزیت پزشک به تعرفهٔ
    /// خدمت منتخب (و برعکس) سوییچ می‌کند
    /// </summary>
    partial void OnReceptionModeIndexChanged(int value)
    {
        InvalidateInquiry();
        OnPropertyChanged(nameof(IsDoctorMode));
        OnPropertyChanged(nameof(IsServiceMode));
        RecalculateCost();
    }

    /// <summary>گزینه‌های حالت پذیرش</summary>
    public string[] ReceptionModes { get; } = { "🩺 ویزیت پزشک", "💉 خدمت درمانی (تزریقات/پانسمان/آزمایش/…)" };

    /// <summary>خدمات تعرفه‌دار فعال — برای حالت خدمت درمانی</summary>
    public ObservableCollection<ServiceOption> Services { get; } = new();

    /// <summary>خدمت انتخاب‌شدهٔ پذیرش خدمتی</summary>
    [ObservableProperty]
    private ServiceOption? _selectedService;

    /// <summary>
    /// تغییر خدمت منتخب → بازمحاسبهٔ هزینه با تعرفهٔ همان خدمت —
    /// چرخهٔ بیست‌وششم (گزارش هجدهم مالک): «در پذیرش خدمات درمانی، تعرفه
    /// خدمات رو نمیاره»؛ اکنون قیمت و سهم‌های بیمه/بیمار خدمتِ منتخب
    /// بی‌درنگ در بخش هزینه اعمال می‌شود.
    /// </summary>
    partial void OnSelectedServiceChanged(ServiceOption? value) => RecalculateCost();

    // ═══ پذیرش چندخدمتی (تأیید مالک) — بیماری که هم‌زمان چند خدمت درمانی
    // دارد (مثلاً تزریق عضلانی + تزریق وریدی + سرم‌تراپی) دیگر به چند پذیرش
    // و چند قبض جدا نیاز ندارد: از کاتالوگ با جست‌وجو اضافه می‌کند، برای هر
    // خدمت تعداد می‌گذارد و در پایان «یک» پذیرش، «یک» فاکتور و «یک» قبض دارد.

    /// <summary>کاتالوگ خدماتِ پالایش‌شده با متن جست‌وجو</summary>
    public ObservableCollection<ServiceOption> FilteredServices { get; } = new();

    /// <summary>خدمات انتخاب‌شدهٔ این مراجعه (با تعداد هر خدمت)</summary>
    public ObservableCollection<SelectedServiceLine> SelectedServices { get; } = new();

    /// <summary>متن جست‌وجو در کاتالوگ خدمات</summary>
    [ObservableProperty]
    private string _serviceSearchText = string.Empty;

    partial void OnServiceSearchTextChanged(string value) => RefreshFilteredServices();

    /// <summary>خلاصهٔ خدمات انتخاب‌شده — تعداد کل و جمع تعرفه</summary>
    public string SelectedServicesSummaryText => SelectedServices.Count == 0
        ? "هنوز خدمتی انتخاب نشده است"
        : $"{FaNum(SelectedServices.Sum(l => l.Quantity))} خدمت انتخاب شده — جمع تعرفه {Money.Format(SelectedServices.Sum(l => l.Service.Price * l.Quantity))}";

    /// <summary>بازسازی فهرست کاتالوگ بر اساس متن جست‌وجو</summary>
    private void RefreshFilteredServices()
    {
        FilteredServices.Clear();
        var q = ServiceSearchText?.Trim();
        foreach (var svc in Services)
            if (string.IsNullOrEmpty(q) || svc.Name.Contains(q))
                FilteredServices.Add(svc);
    }

    /// <summary>افزودن خدمت از کاتالوگ — افزودن دوبارهٔ همان خدمت، تعدادش را زیاد می‌کند</summary>
    [RelayCommand]
    private void AddService(ServiceOption? service)
    {
        if (service is null) return;
        if (!_serviceTariffsLoaded || !Services.Contains(service))
        { _snackbar.Error("تعرفه‌های بیمهٔ انتخابی هنوز آماده نیستند؛ فهرست تعرفه را تازه‌سازی کنید."); return; }
        var line = SelectedServices.FirstOrDefault(l => l.Service.Id == service.Id);
        if (line is not null) line.Quantity++;
        else
        {
            line = new SelectedServiceLine(service);
            line.Changed += OnSelectedServiceLineChanged;
            SelectedServices.Add(line);
        }
        SyncPrimaryService();
        RecalculateCost();
    }

    /// <summary>حذف یک ردیف خدمت انتخاب‌شده</summary>
    [RelayCommand]
    private void RemoveServiceLine(SelectedServiceLine? line)
    {
        if (line is null) return;
        line.Changed -= OnSelectedServiceLineChanged;
        SelectedServices.Remove(line);
        SyncPrimaryService();
        RecalculateCost();
    }

    /// <summary>زیادکردن تعداد یک خدمت</summary>
    [RelayCommand]
    private void IncreaseLineQty(SelectedServiceLine? line) { if (line is not null) line.Quantity++; }

    /// <summary>کم‌کردن تعداد یک خدمت (کمینه ۱)</summary>
    [RelayCommand]
    private void DecreaseLineQty(SelectedServiceLine? line) { if (line is not null && line.Quantity > 1) line.Quantity--; }

    private void OnSelectedServiceLineChanged() => RecalculateCost();

    /// <summary>خدمت اصلی پذیرش = نخستین خدمت انتخاب‌شده (برای شناسهٔ خدمت پذیرش و صف)</summary>
    private void SyncPrimaryService() => SelectedService = SelectedServices.FirstOrDefault()?.Service;

    /// <summary>نام خلاصهٔ خدمات برای پذیرش/فاکتور — مثل «تزریق عضلانی + تزریق وریدی ×۲»</summary>
    private string BuildServiceSummary() => string.Join(" + ",
        SelectedServices.Select(l => l.Quantity > 1 ? $"{l.Service.Name} ×{FaNum(l.Quantity)}" : l.Service.Name));

    /// <summary>ارقام لاتین ← فارسی برای نمایش</summary>
    private static string FaNum(int n) => n.ToString()
        .Replace("0", "۰").Replace("1", "۱").Replace("2", "۲").Replace("3", "۳").Replace("4", "۴")
        .Replace("5", "۵").Replace("6", "۶").Replace("7", "۷").Replace("8", "۸").Replace("9", "۹");

    /// <summary>یک ردیف خدمت انتخاب‌شده با تعداد</summary>
    public partial class SelectedServiceLine : ObservableObject
    {
        /// <summary>رویداد تغییر تعداد/خدمت برای بازمحاسبهٔ هزینه</summary>
        public event Action? Changed;

        /// <summary>خدمت این ردیف</summary>
        [ObservableProperty]
        private ServiceOption _service;

        /// <summary>تعداد این خدمت (مثلاً ۲ تزریق وریدی)</summary>
        [ObservableProperty]
        private int _quantity = 1;

        public SelectedServiceLine(ServiceOption service) => _service = service;

        partial void OnServiceChanged(ServiceOption value)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(UnitPriceText));
            OnPropertyChanged(nameof(LineTotalText));
            Changed?.Invoke();
        }

        partial void OnQuantityChanged(int value)
        {
            if (value < 1) { Quantity = 1; return; }
            OnPropertyChanged(nameof(LineTotalText));
            Changed?.Invoke();
        }

        /// <summary>نام خدمت</summary>
        public string Name => Service.Name;

        /// <summary>قیمت واحد نمایشی</summary>
        public string UnitPriceText => Money.Format(Service.Price);

        /// <summary>جمع ردیف نمایشی</summary>
        public string LineTotalText => Money.Format(Service.Price * Quantity);
    }

    /// <summary>پرسنل فعال برای انتخاب انجام‌دهندهٔ خدمت</summary>
    public ObservableCollection<PerformerOption> Performers { get; } = new();

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — اتاق انجام خدمت پذیرش خدمتی ═══
    // «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای خدمات
    // درمانی نمی‌شود معین کرد» — فهرست اتاق‌های فعال از lookups/rooms؛ برای
    // ویزیت پزشک نمایش داده نمی‌شود چون اتاق از برنامهٔ کاری خود پزشک می‌آید

    /// <summary>اتاق‌های فعال درمانگاه — برای پذیرش خدمت درمانی</summary>
    public ObservableCollection<RoomOption> Rooms { get; } = new();

    /// <summary>اتاق انتخاب‌شدهٔ انجام خدمت (اختیاری)</summary>
    [ObservableProperty]
    private RoomOption? _selectedRoom;

    /// <summary>گزینهٔ اتاق معاینه — نام نمایشی از نام کامل یا شماره اتاق</summary>
    public record RoomOption(long Id, string DisplayName);

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — لوازم مصرفی درمانگاه در پذیرش خدمتی ═══
    // «لوازمی که استفاده می‌شود مثل سرنگ و سرم و گاز استریل و مواردی از این
    // قبیل رو وقتی بیمار با خودش نمیاره، باید از لوازم خود درمانگاه استفاده
    // بکنیم و در پذیرش برای اونها هم هزینه به هزینه‌های بیمار اضافه بشه» —
    // اقلام مصرفیِ دارای موجودی از GET pharmacy/consumables می‌آیند؛ کاربر
    // قلم و تعداد برمی‌گزیند، جمع هزینه بی‌درنگ به «قابل پرداخت بیمار»
    // می‌چسبد و هنگام ثبت، موجودی FEFO کم و تراکنش انبار با منشأ همین
    // پذیرش ثبت می‌شود؛ فاکتور اقلام مصرفی را با «معافیت بیمه‌ای» (۱۰۰٪
    // بیمار) صادر می‌کند.

    /// <summary>گزینهٔ قلم مصرفی از انبار — با موجودی کل و قیمت فروش</summary>
    public record ConsumableOption(long Id, string Name, string? Form, decimal Available, decimal SellPrice)
    {
        /// <summary>متن کمبوباکس — نام + موجودی + قیمت</summary>
        public string Display =>
            $"{Name}{(string.IsNullOrWhiteSpace(Form) ? "" : $" ({Form})")} — موجودی {Available:0.#} · {SellPrice:N0}";
    }

    /// <summary>ردیف مصرف انتخاب‌شده برای این پذیرش — تعداد قابل ویرایش است</summary>
    public partial class ConsumableLine : ObservableObject
    {
        /// <summary>قلم مصرفی</summary>
        public ConsumableOption Option { get; }

        /// <summary>تعداد مصرف‌شده</summary>
        [ObservableProperty]
        private decimal _quantity;

        /// <summary>بازخورد تغییر تعداد → نوسازی جمع کل در ویومدل پدر</summary>
        public event EventHandler? LineChanged;

        partial void OnQuantityChanged(decimal value) => LineChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>جمع این ردیف = قیمت فروش × تعداد</summary>
        public decimal LineTotal => Option.SellPrice * Quantity;

        /// <summary>سازنده</summary>
        public ConsumableLine(ConsumableOption option, decimal quantity)
        {
            Option = option;
            _quantity = quantity;
        }
    }

    /// <summary>اقلام مصرفیِ در دسترس (دارای موجودی سالم)</summary>
    public ObservableCollection<ConsumableOption> ConsumableOptions { get; } = new();

    /// <summary>گارد هم‌زمانی نوسازی اقلام مصرفی — چرخهٔ ۴۵</summary>
    private bool _refreshingConsumables;

    /// <summary>آیا خطای بارگذاری اقلام در این نشست یک‌بار نشان داده شده؟ — چرخهٔ ۴۵</summary>
    private bool _consumablesErrorShown;

    /// <summary>ردیف‌های مصرف انتخاب‌شدهٔ این پذیرش</summary>
    public ObservableCollection<ConsumableLine> Consumables { get; } = new();

    /// <summary>قلم مصرفی منتخب برای افزودن</summary>
    [ObservableProperty]
    private ConsumableOption? _selectedConsumable;

    /// <summary>تعداد برای قلم در حال افزودن</summary>
    [ObservableProperty]
    private decimal _consumableQty = 1;

    /// <summary>جمع هزینهٔ لوازم مصرفی انتخاب‌شده</summary>
    public decimal ConsumablesTotal => Consumables.Sum(c => c.LineTotal);

    /// <summary>قابل پرداخت نهایی بیمار = سهم بیمار خدمت + لوازم مصرفی</summary>
    public decimal PayableByPatient => PatientShare + ConsumablesTotal;

    /// <summary>نمایش جمع لوازم مصرفی</summary>
    public string ConsumablesTotalDisplay => Money.Format(ConsumablesTotal);

    /// <summary>نمایش قابل پرداخت نهایی (خدمت + مصرفی)</summary>
    public string PayableByPatientDisplay => Money.Format(PayableByPatient);

    /// <summary>آیا ردیف مصرفی انتخاب شده؟ — برای نمایش بخش جمع مصرفی</summary>
    public bool HasConsumables => Consumables.Count > 0;

    /// <summary>انجام‌دهندهٔ انتخاب‌شده (اختیاری)</summary>
    [ObservableProperty]
    private PerformerOption? _selectedPerformer;

    /// <summary>گزینهٔ خدمت تعرفه‌دار — چرخهٔ بیست‌وششم: سهم‌های تعرفه هم همراهش می‌آید</summary>
    public record ServiceOption(long Id, string Name, decimal Price,
        decimal TariffInsuranceShare = 0m, decimal TariffPatientShare = 100m)
    {
        /// <summary>قیمت نمایشی برای ردیف کاتالوگ</summary>
        public string PriceText => Money.Format(Price);
    }

    /// <summary>گزینهٔ پرسنل انجام‌دهنده</summary>
    public record PerformerOption(long Id, string FullName);
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>پزشک انتخاب‌شده</summary>
    [ObservableProperty]
    private DoctorOption? _selectedDoctor;

    /// <summary>نمایه بیمه — صفر یعنی آزاد</summary>
    [ObservableProperty]
    private int _insuranceIndex;

    /// <summary>تنظیم مستقل اعمال بیمه برای ویزیت</summary>
    private bool _insurancePolicyLoaded;
    private bool _insuranceListLoaded;
    private bool _isInitializing = true;
    private bool ApplyInsuranceToDoctorVisits { get; set; } = true;

    /// <summary>تنظیم مستقل اعمال بیمه برای خدمات درمانی</summary>
    private bool ApplyInsuranceToMedicalServices { get; set; } = true;

    /// <summary>شماره بیمه</summary>
    [ObservableProperty]
    private string _insuranceNumber = string.Empty;
    partial void OnInsuranceNumberChanged(string value) => InvalidateInquiry();

    /// <summary>کد ارجاع</summary>
    [ObservableProperty]
    private string? _referralCode;

    /// <summary>یادداشت پذیرش</summary>
    [ObservableProperty]
    private string? _receptionNotes;

    /// <summary>نمایه نوع ویزیت</summary>
    [ObservableProperty]
    private int _visitTypeIndex;

    /// <summary>وضعیت استعلام</summary>
    [ObservableProperty]
    private InquiryState? _inquiry;

    /// <summary>در حال استعلام بیمه</summary>
    [ObservableProperty]
    private bool _isInquiring;

    /* ── محاسبه هزینه ── */
    /// <summary>حق ویزیت</summary>
    [ObservableProperty]
    private decimal _visitFee;

    /// <summary>سهم بیمه</summary>
    [ObservableProperty]
    private decimal _insuranceShare;

    /// <summary>سهم بیمار</summary>
    [ObservableProperty]
    private decimal _patientShare;

    /// <summary>درصد پوشش</summary>
    [ObservableProperty]
    private decimal _coveragePercent;

    /// <summary>
    /// آیا بیمهٔ واقعی انتخاب شده است؟ — چرخهٔ سی‌ودوم (گزارش مالک):
    /// در حالت «آزاد (بدون بیمه)» ستون «سهم بیمه» بخش هزینه پنهان و کل مبلغ
    /// به‌صورت درشت به‌عنوان سهم بیمار نمایش داده می‌شود؛ نمایش دو سهم برای
    /// بیمار بدون بیمه گمراه‌کننده و اشتباه است.
    /// </summary>
    [ObservableProperty]
    private bool _hasInsurance = true;

    /// <summary>نمایش حق ویزیت</summary>
    public string FeeDisplay => Money.Format(VisitFee);

    /// <summary>نمایش سهم بیمه</summary>
    public string InsuranceShareDisplay => Money.Format(InsuranceShare);

    /// <summary>نمایش سهم بیمار — درشت و قرمز</summary>
    public string PatientShareDisplay => Money.Format(PatientShare);

    /// <summary>پس‌زمینه ردیف صف</summary>
    public string[] QueueColors { get; } = { "#FFFFFF", "#FFF8E1", "#E3F2FD", "#E8F5E9", "#ECEFF1", "#FFEBEE" };

    /// <summary>پذیرش‌های امروز</summary>
    [ObservableProperty]
    private ObservableCollection<ReceptionRow> _receptions = new();

    /// <summary>سازنده</summary>
    private readonly IInvoiceRecoveryStore? _invoiceRecoveryStore;
    private readonly IReceptionRecoveryStore? _receptionRecoveryStore;
    private readonly string _invoiceRecoveryServer;
    private PendingReception? _pendingReception;

    public ReceptionsViewModel(IApiClient api, ISnackbarService snackbar, IInvoiceRecoveryStore? invoiceRecoveryStore = null,
        IReceptionRecoveryStore? receptionRecoveryStore = null)
    {
        _api = api;
        _snackbar = snackbar;
        _invoiceRecoveryStore = invoiceRecoveryStore ?? (api is ApiClient ? new InvoiceRecoveryStore() : null);
        _receptionRecoveryStore = receptionRecoveryStore ?? (api is ApiClient ? new ReceptionRecoveryStore() : null);
        _invoiceRecoveryServer = api is ApiClient client ? client.PaymentRecoveryServer : "test-api";
        LoadPanelPrefs();
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری اولیه</summary>
    private async Task InitializeAsync()
    {
        try
        {
        await LoadInsuranceBillingPolicyAsync();
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
        _insuranceListLoaded = companies?.Data is not null;
        if (!_insuranceListLoaded) _snackbar.Error("دریافت فهرست بیمه ناموفق بود؛ پیش از ثبت آن را تازه‌سازی کنید.");
        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): «نوع بیمه گزینهٔ آزاد وجود ندارد و
        // هنوز بیمهٔ آسیا به‌عنوان آزاد ثبت می‌شود» — ریشهٔ باگ: کد، نمایهٔ صفرِ
        // کشو را «آزاد/بدون بیمه» تفسیر می‌کرد (InsuranceIndex <= 0) ولی کشو هیچ
        // آیتم آزادی نداشت؛ یعنی نخستین شرکت سرور (مثلاً آسیا) انتخاب می‌شد و
        // هم‌زمان چون نمایه‌اش صفر بود به‌عنوان «بدون بیمه» ثبت می‌شد — هم گزینهٔ
        // آزاد وجود نداشت هم بیمهٔ انتخابی قعر می‌رفت. رفع ساختاری: آیتم صریح
        // «آزاد (بدون بیمه)» با شناسهٔ صفر در ابتدای کشو درج می‌شود تا نمایهٔ صفر
        // واقعاً آزاد باشد و شرکت‌ها از نمایهٔ ۱ به بعد دقیق ثبت شوند (هم‌الگوی
        // پنجرهٔ فرم پذیرش — ReceptionFormViewModel).
        InsuranceCompanies.Add(new InsuranceOption(0, "آزاد (بدون بیمه)", false));
        if (companies?.Data is not null)
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name, c.IsContracted, c.CoveragePercent));

        // چرخهٔ ۴۴ — بیمارِ هم‌اکنون انتخاب‌شده (جستجوی زودهنگام پیش از آماده‌شدن
        // کشو) اکنون که فهرست بیمه‌ها کامل است، بیمهٔ پرونده‌اش روی کشو می‌نشیند
        ApplyPatientInsurance(FoundPatient);
        await LoadServicesAndPerformersAsync(GetEffectiveServiceInsuranceCompanyId(InsuranceIndex));

        await LoadAsync();
        }
        catch (Exception) { _snackbar.Error("بارگذاری اولیه پذیرش ناموفق بود؛ اطلاعات بیمه را تازه‌سازی کنید."); }
        finally { _isInitializing = false; }
    }

    /// <summary>بارگذاری آمار و صف امروز</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var stats = await _api.GetAsync<QueueStatsResponse>("receptions/queue-stats");
        if (stats?.Data is not null)
        {
            WaitingCount = stats.Data.WaitingCount;
            ReceivedCount = stats.Data.ReceivedCount;
            InQueue = stats.Data.InQueue;
            TodayRevenue = stats.Data.TodayRevenue;
            TodayRevenueDisplay = TodayRevenue >= 1_000_000
                ? $"{TodayRevenue / 1_000_000:0.#}M"
                : Money.Format(TodayRevenue);
        }

        var response = await _api.GetAsync<ReceptionsResponse>("receptions/today");
        if (response?.Data is not null)
        {
            // بارگذاری صف امروز نباید عنوان تاریخچهٔ قبلی را روی دادهٔ امروز نگه دارد.
            QueueTitle = "صف پذیرش امروز";
            Receptions = new ObservableCollection<ReceptionRow>(response.Data.Select(MapRow));
            // مراجعه‌کننده‌های اخیر + جمع‌های مالی پنل‌های اختیاری
            RecentPatients.Clear();
            foreach (var name in response.Data.Select(r => r.PatientName)
                         .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(8))
                RecentPatients.Add(name);
            TodayInvoicedText = Money.Format(response.Data.Sum(r => r.InvoiceTotal));
            TodayPaidText = Money.Format(response.Data.Sum(r => r.PaidAmount));
        }
        else
            ErrorMessage = _api.LastError ?? string.Empty;

        // نوبت‌های امروز برای پنل اختیاری (بهترین تلاش؛ خطا بی‌صدا رد می‌شود)
        try
        {
            var appts = await _api.GetAsync<AppointmentsViewModel.TodayResponse>("appointments");
            TodayAppointmentsMini.Clear();
            if (appts?.Data is not null)
                foreach (var a in appts.Data.Take(10))
                    TodayAppointmentsMini.Add(new MiniAppointmentRow(
                        a.Time, a.PatientName, a.DoctorName, MiniApptStatusText(a.Status)));
        }
        catch { /* پنل نوبت‌ها نباید جریان پذیرش را متوقف کند */ }

        // ═══ چرخهٔ ۴۵ (گزارش مالک: «در پذیرش، لیست اقلام مصرفی رو نمیاره و
        //    نمیتونم انتخاب بکنم») ═══ ریشه: ReceptionsViewModel تک‌نمونه است و
        // اقلام مصرفی فقط «یک‌بار در سازنده» (زمان ورود به برنامه) بار می‌شدند
        // — کاربر قلم و ورود کالای تازه ثبت می‌کرد ولی فهرست پذیرش تا ری‌استارت
        // هیچ‌وقت تازه نمی‌شد! اکنون با هر بارگذاری صفحهٔ پذیرش نوسازی می‌شود
        // (همراه با گارد هم‌زمانی و آشکارسازی خطای سرور).
        _ = RefreshConsumablesAsync();

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف صف</summary>
    private static ReceptionRow MapRow(ReceptionDto r) => new(
        r.Id, r.ReceptionNumber, r.PatientName, r.DoctorName, r.TimeShamsi,
        StatusTextOf(r.Status), r.Status,
        Math.Clamp(r.Status - 1, 0, 5),
        r.InsuranceName,
        r.WaitingTime.HasValue ? $"{r.WaitingTime} دقیقه" : "—",
        new[] { "#FFFFFF", "#FFF8E1", "#E3F2FD", "#E8F5E9", "#ECEFF1", "#FFEBEE" }[Math.Clamp(r.Status - 1, 0, 5)],
        // چرخهٔ ۳۹ — اتاق انجام خدمت
        r.RoomName,
        // ═══ چرخه سی‌ویکم — مبالغ مالی هر پذیرش در فهرست روز ═══ جمع فاکتور +
        // پرداخت‌شده + متن وضعیت مالی (پرداخت‌شده/پرداخت نشده/بدون فاکتور)
        r.InvoiceTotal,
        r.PaidAmount,
        r.InvoiceTotal > 0
            ? (r.PaidAmount >= r.InvoiceTotal ? "تسویه" : "پرداخت‌نشده")
            : "بدون فاکتور");

    /// <summary>متن وضعیت نوبت (پنل نوبت‌های امروز)</summary>
    private static string MiniApptStatusText(int status) => status switch
    {
        1 => "رزروشده", 2 => "تأییدشده", 3 => "حاضر", 4 => "در ویزیت",
        5 => "تکمیل‌شده", 6 => "لغوشده", 7 => "عدم حضور", _ => "نامشخص"
    };

    /// <summary>متن وضعیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "پذیرش شده", 2 => "در صف", 3 => "در ویزیت", 4 => "ویزیت‌شده", 5 => "ترخیص‌شده", 6 => "لغوشده",
        _ => "نامشخص"
    };

    /// <summary>
    /// چاپ مجدد قبض یک پذیرش انجام‌شده (امروز یا گذشته) — قبض از دادهٔ
    /// واقعی ذخیره‌شدهٔ همان پذیرش (فاکتور و اقلام و پرداخت) بازسازی و با
    /// همان چاپگر قبض پذیرش چاپ می‌شود؛ پذیرش بدون فاکتور پیام صادقانه می‌گیرد
    /// </summary>

    /// <summary>
    /// چاپ قبض — قرارداد html_first (استودیو قالب): اول قالب پیش‌فرضِ فعالِ
    /// «قبض پذیرش» سرور با دادهٔ همین پذیرش رندر و چاپ می‌شود؛ اگر قالبی
    /// نبود یا سرور در دسترس نبود، قبض داخلیِ حرارتی دقیقاً مثل قبل چاپ
    /// می‌شود — هیچ چاپی از دست نمی‌رود
    /// </summary>
    private async Task PrintReceiptAsync(ReceptionReceipt receipt)
    {
        var vars = new Dictionary<string, object?>
        {
            ["clinic_name"] = receipt.ClinicName,
            ["patient_name"] = receipt.PatientName,
            ["patient_national_code"] = receipt.NationalCode ?? string.Empty,
            ["patient_mobile"] = receipt.Mobile ?? string.Empty,
            ["queue_number"] = receipt.DisplayReceptionNumber,
            ["reception_code"] = receipt.DisplayReceptionNumber,
            ["reception_date"] = receipt.DateText,
            ["reception_time"] = receipt.TimeText,
            ["service_name"] = receipt.ServiceName ?? receipt.DoctorName,
            ["performer_name"] = receipt.DoctorName,
            ["room_name"] = receipt.RoomName ?? string.Empty,
            ["insurance_name"] = receipt.InsuranceName,
            ["items_html"] = TemplatedPrintService.ItemsTableHtml(receipt.Items.Select(i =>
                (i.Name,
                 Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(i.Quantity == Math.Truncate(i.Quantity) ? i.Quantity.ToString("0") : i.Quantity.ToString("0.##")),
                 Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(i.Total)))),
            ["items_total"] = Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(receipt.ItemsTotal),
            ["insurance_share"] = Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(receipt.InsuranceShare),
            ["patient_share"] = Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(receipt.PatientShare),
            ["payable"] = Money.FormatWithUnit(receipt.Payable),
            ["amount"] = Money.FormatWithUnit(receipt.Payable),
            ["discount"] = Money.FormatWithUnit(receipt.Discount),
            ["tax"] = Money.FormatWithUnit(receipt.Tax),
            ["paid_amount"] = Sahand.ClinicPro.Desktop.Helpers.Money.FormatWithUnit(receipt.Paid),
            ["payment_method"] = receipt.PaymentMethodText,
            ["invoice_number"] = receipt.DisplayInvoiceNumbers,
            ["print_date"] = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(IranClock.Now)
        };
        var result = await TemplatedPrintService.TryPrintAsync(_api, "ReceptionReceipt", vars,
            $"قبض پذیرش — {receipt.PatientName}", () => ReceptionReceiptPrinter.Print(receipt));
        if (result == TemplatePrintOutcome.Fallback)
        {
            _snackbar.Warning("قالب انتخابی دریافت نشد؛ قبض با طرح داخلی چاپ می‌شود.");
            result = ReceptionReceiptPrinter.Print(receipt) ? TemplatePrintOutcome.Printed : TemplatePrintOutcome.NotPrinted;
        }
        if (result == TemplatePrintOutcome.Printed)
            _snackbar.Success("قبض به چاپگر ارسال شد");
        else if (result == TemplatePrintOutcome.NotPrinted)
            _snackbar.Info("قبض به چاپگر ارسال نشد");
        else
            _snackbar.Info("پنجره چاپ قالب بسته شد؛ نتیجه دیالوگ چاپ مرورگر قابل تأیید نیست");
    }

    private async Task<ReceptionReceipt?> LoadReceiptAsync(long receptionId)
    {
        var response = await _api.GetAsync<ReceptionReceiptResponse>($"receptions/{receptionId}/receipt");
        var d = response?.Data;
        if (d is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت اطلاعات ذخیره‌شده قبض ناموفق بود");
            return null;
        }
        return new ReceptionReceipt(
            QueueNumber: d.ReceptionId, InvoiceNumber: 0,
            PatientName: d.PatientName, NationalCode: d.NationalCode, Mobile: d.Mobile,
            DoctorName: d.DoctorName, ServiceName: d.ServiceName, RoomName: d.RoomName,
            InsuranceName: d.InsuranceName, DateText: d.DateText, TimeText: d.TimeText,
            Items: d.Items.Select(i => new ReceptionReceiptItem(i.Name, i.Quantity, i.UnitPrice)).ToList(),
            ItemsTotal: d.ItemsTotal, InsuranceShare: d.InsuranceShare, PatientShare: d.PatientShare,
            Payable: d.Payable, Paid: d.Paid, PaymentMethod: d.PaymentMethod,
            ReferenceNumber: d.ReferenceNumber, CardNumber: d.CardNumber,
            ClinicName: await GetReceiptClinicNameAsync(),
            ReceptionNumberText: string.IsNullOrWhiteSpace(d.ReceptionNumber) ? "—" : d.ReceptionNumber,
            InvoiceNumbersText: string.IsNullOrWhiteSpace(d.InvoiceNumbers) ? "—" : d.InvoiceNumbers,
            Discount: d.Discount, Tax: d.Tax);
    }

    [RelayCommand]
    private async Task ReprintReceiptAsync(ReceptionRow? row)
    {
        row ??= SelectedReception;
        if (row is null) { _snackbar.Error("ابتدا یک پذیرش را از فهرست انتخاب کنید"); return; }
        try
        {
            var receipt = await LoadReceiptAsync(row.Id);
            if (receipt is not null) await PrintReceiptAsync(receipt);
        }
        catch (Exception ex)
        {
            _snackbar.Error($"چاپ قبض انجام نشد: {ex.Message}");
        }
    }

    /// <summary>نمایش پذیرش‌های تاریخ شمسی انتخابی — تاریخچه برای چاپ مجدد قبض</summary>
    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        var gregorian = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
            .ToGregorian(HistoryDateShamsi?.Trim());
        if (gregorian is null)
        {
            _snackbar.Error("تاریخ شمسی معتبر وارد کنید — مثال: ۱۴۰۵/۰۵/۱۸");
            return;
        }
        IsBusy = true;
        var dateText = gregorian.Value.ToString("yyyy-MM-dd");
        var response = await _api.GetAsync<ReceptionsResponse>($"receptions/by-date?date={dateText}");
        IsBusy = false;
        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت پذیرش‌های آن تاریخ ناموفق بود");
            return;
        }
        Receptions = new ObservableCollection<ReceptionRow>(response.Data.Select(MapRow));
        QueueTitle = $"پذیرش‌های {HistoryDateShamsi} (تاریخچه)";
        _snackbar.Info($"{response.Data.Count} پذیرش برای {HistoryDateShamsi} یافت شد");
    }

    /// <summary>بازگشت فهرست صف به پذیرش‌های امروز</summary>
    [RelayCommand]
    private async Task BackToTodayAsync()
    {
        QueueTitle = "صف پذیرش امروز";
        HistoryDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(IranClock.Now);
        await LoadAsync();
    }

    /* ═══════════ جستجوی هوشمند ═══════════ */

    /// <summary>
    /// جستجو — تشخیص خودکار نوع: کد پیگیری APT / نام / کد ملی / موبایل.
    /// چرخه بیست‌وسوم (گزارش شانزدهم مالک): پیش‌تر حداقل ۴ نویسه لازم بود و کلاینت
    /// نتایج جزئی سرور را با شرط تطابق «دقیق» کد ملی/موبایل دور می‌ریخت — یعنی فقط
    /// کد ملی کامل جواب می‌داد. اکنون: حداقل ۲ نویسه؛ همهٔ نتایج جزئی (نام، نام
    /// خانوادگی، بخشی از کد ملی/موبایل/پرونده) از سرور می‌آید؛ یک نتیجه = انتخاب
    /// خودکار؛ چند نتیجه = فهرست برای انتخاب کاربر؛ صفر = پیام راهنما.
    /// </summary>
    [RelayCommand]
    private async Task SmartSearchAsync()
    {
        if (IsSubmitting || IsCreatingUnidentifiedPatient || IsSearchingPatient) return;
        var term = SmartSearch.Trim();
        if (term.Length < 2) { _snackbar.Error("بخشی از نام، کد ملی، موبایل یا کد پیگیری نوبت را وارد کنید"); return; }
        var version = ++_patientSearchVersion;
        IsSearchingPatient = true;
        ClearSelectedAppointment();
        FoundPatient = null;
        FoundAppointmentInfo = null;
        SelectedSearchResult = null;
        PatientResults = new ObservableCollection<QuickPatient>();
        try
        {
            if (term.StartsWith("APT", StringComparison.OrdinalIgnoreCase))
            {
                var appts = await _api.PostAsync<AppointmentsViewModel.SearchResponse>("appointments/search", new
                { search = term, pageNumber = 1, pageSize = 5 });
                if (version != _patientSearchVersion) return;
                var matches = appts?.Data?.Where(a => string.Equals(a.AppointmentCode, term, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
                if (matches is null || matches.Count != 1)
                { _snackbar.Error("نوبت یکتا با این کد یافت نشد؛ اطلاعات را بررسی کنید."); return; }
                var appt = matches[0];
                if (appt.PatientId is null || appt.DoctorId is null || appt.Status is not (1 or 2))
                { _snackbar.Error("شناسه بیمار یا پزشک نوبت موجود نیست یا نوبت در وضعیت قابل پذیرش نیست؛ سرور و برنامه را به‌روز کنید."); return; }
                if (string.IsNullOrWhiteSpace(appt.PatientMobile))
                { _snackbar.Error("شماره بیمار در نوبت موجود نیست؛ بیمار را جداگانه جستجو کنید."); return; }
                var patients = await FindPatientsAsync(appt.PatientMobile);
                if (version != _patientSearchVersion) return;
                var patientMatches = patients?.Where(x => x.Id == appt.PatientId && x.Mobile == appt.PatientMobile).Take(2).ToList();
                if (patientMatches is null || patientMatches.Count != 1)
                { _snackbar.Error("بیمار یکتا برای این نوبت یافت نشد؛ بیمار را جداگانه جستجو کنید."); return; }
                var doctor = Doctors.FirstOrDefault(d => d.Id == appt.DoctorId);
                if (doctor is null) { _snackbar.Error("پزشک نوبت در فهرست فعال یافت نشد؛ نوبت را بررسی کنید."); return; }
                FoundPatient = patientMatches[0];
                _selectedAppointmentId = appt.Id;
                _selectedAppointmentPatientId = appt.PatientId;
                _selectedAppointmentDoctorId = appt.DoctorId;
                FoundAppointmentInfo = $"نوبت {appt.DateShamsi} ساعت {appt.Time} — {appt.DoctorName}";
                SelectedDoctor = doctor;
            }
            else
            {
                var results = await FindPatientsAsync(term);
                if (version != _patientSearchVersion) return;
                if (results is null) { _snackbar.Error(_api.LastError ?? "دریافت نتایج جستجو ناموفق بود"); return; }
                if (results.Count == 0) _snackbar.Error("بیماری یافت نشد — بخشی از نام یا کد ملی را امتحان کنید یا ابتدا بیمار را ثبت کنید");
                else if (results.Count == 1)
                { FoundPatient = results[0]; _snackbar.Success($"بیمار یافت شد: {FoundPatient.FullName}"); }
                else
                { PatientResults = new ObservableCollection<QuickPatient>(results); _snackbar.Info($"{results.Count} بیمار یافت شد — از فهرست انتخاب کنید"); }
            }
        }
        catch (Exception)
        { if (version == _patientSearchVersion) _snackbar.Error("دریافت نتایج جستجو ناموفق بود؛ دوباره تلاش کنید."); }
        finally { IsSearchingPatient = false; }
    }

    /// <summary>
    /// یافتن بیماران با تطبیق جزئی — چرخه بیست‌وسوم: دیگر با شرط «کد ملی == دقیق»
    /// دوربرداشته نمی‌شود؛ سرور خودش Contains روی نام/کدملی/موبایل/پرونده و
    /// مقایسهٔ فشردهٔ نیم‌فاصله انجام می‌دهد
    /// </summary>
    /// <summary>جنسیت بیمار ناشناس — ۰=مرد، ۱=زن</summary>
    [ObservableProperty] private int _unidentifiedGenderIndex;

    /// <summary>سن تقریبی بیمار ناشناس (اختیاری)</summary>
    [ObservableProperty] private string _unidentifiedAgeText = string.Empty;

    /// <summary>
    /// پذیرش فوری بیمار ناشناس (اورژانسی) — ساخت پروندهٔ موقت با کد یکتای
    /// سیستمی بدون کد ملی؛ بیمار بلافاصله انتخاب می‌شود و تکمیل هویت
    /// بعداً از پروندهٔ بیمار انجام می‌گردد
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditReception))]
    private bool _isCreatingUnidentifiedPatient;

    [RelayCommand]
    private async Task CreateUnidentifiedPatientAsync()
    {
        if (IsSubmitting || IsSearchingPatient || IsCreatingUnidentifiedPatient) return;
        IsCreatingUnidentifiedPatient = true;
        try
        {
        int? age = int.TryParse(UnidentifiedAgeText?.Trim(), out var a) ? a : null;
        var response = await _api.PostAsync<UnidentifiedPatientEnvelope>("patients/unidentified",
            new { gender = UnidentifiedGenderIndex + 1, approximateAge = age });
        var dto = response?.Data;
        if (dto is null)
        {
            _snackbar.Error((_api.LastError ?? "پاسخ ساخت پرونده دریافت نشد") + "؛ پیش از تکرار وجود پرونده را بررسی کنید.");
            return;
        }
        FoundPatient = new QuickPatient(dto.Id, "بیمار ناشناس", dto.TemporaryCode ?? string.Empty,
            dto.Mobile ?? string.Empty, null, null, null, null, UnidentifiedGenderIndex + 1);
        PatientResults = new ObservableCollection<QuickPatient>();
        _snackbar.Success($"پروندهٔ موقت ساخته شد — کد موقت: {dto.TemporaryCode} (تکمیل هویت بعداً از پروندهٔ بیمار)");
        }
        catch (Exception) { _snackbar.Error("پاسخ ساخت پرونده دریافت نشد؛ پیش از تکرار وجود پرونده را بررسی کنید."); }
        finally { IsCreatingUnidentifiedPatient = false; }
    }

    /// <summary>پاکت پاسخ ساخت بیمار ناشناس</summary>
    public sealed class UnidentifiedPatientEnvelope
    {
        public bool IsSuccess { get; set; }
        public UnidentifiedPatientDto? Data { get; set; }
    }

    /// <summary>DTO بیمار ناشناس — منطبق بر PatientListDto سرور</summary>
    public sealed class UnidentifiedPatientDto
    {
        public long Id { get; set; }
        public string? TemporaryCode { get; set; }
        public string? Mobile { get; set; }
    }

    private async Task<List<QuickPatient>?> FindPatientsAsync(string term)
    {
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term)}");
        if (response?.Data is null) return null;

        return response.Data
            .Select(p => new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}",
                p.IsUnidentified && !string.IsNullOrWhiteSpace(p.TemporaryCode) ? p.TemporaryCode! : p.NationalCode,
                p.Mobile, p.InsuranceName, p.Allergies, null, p.InsuranceCompanyId))
            .ToList();
    }

    /* ═══════════ استعلام بیمه ═══════════ */

    /// <summary>تغییر پزشک → محاسبه هزینه</summary>
    partial void OnSelectedDoctorChanged(DoctorOption? value) => RecalculateCost();
    partial void OnCoveragePercentChanged(decimal value) => RecalculateCost();

    /// <summary>شناسهٔ مؤثر بیمه برای تعرفهٔ خدمت؛ فقط شرکت طرف‌قرارداد و سیاست فعال پذیرفته می‌شود.</summary>
    /// <param name="index">نمایهٔ شرکت در فهرست بیمه‌ها — نمایهٔ صفر یا نامعتبر به معنی تعرفهٔ آزاد است.</param>
    private long? GetEffectiveServiceInsuranceCompanyId(int index)
    {
        if (!ApplyInsuranceToMedicalServices || index < 0 || index >= InsuranceCompanies.Count) return null;
        var company = InsuranceCompanies[index];
        return company is { Id: > 0, IsContracted: true } ? company.Id : null;
    }

    /// <summary>نام درمانگاه برای عنوان قبض — کش‌شده از تنظیم ClinicInfo لایسنس</summary>
    private string? _receiptClinicName;

    /// <summary>
    /// نام درمانگاهِ عنوان قبض پذیرش — از تنظیمات ClinicInfo (همگام‌شده از
    /// سرور لایسنس) خوانده و کش می‌شود؛ در قطعی شبکه نام چاپی محلی و در
    /// نبود هر دو «درمانگاه» برمی‌گردد (هرگز نام نرم‌افزار روی قبض نمی‌آید).
    /// </summary>
    private async Task<string> GetReceiptClinicNameAsync()
    {
        if (!string.IsNullOrWhiteSpace(_receiptClinicName))
            return _receiptClinicName;
        try
        {
            var response = await _api.GetAsync<InsurancePolicySettingsResponse>(
                "settings?category=ClinicInfo");
            var name = response?.Data?.FirstOrDefault(x => x.Key == "ClinicName")?.Value;
            if (!string.IsNullOrWhiteSpace(name))
                return _receiptClinicName = name.Trim();
        }
        catch
        {
            // بهترین تلاش — قبض به نام چاپی محلی یا عنوان خنثی چاپ می‌شود
        }
        var local = LocalSettingsStore.Get("Printer.ClinicName");
        return string.IsNullOrWhiteSpace(local) ? "درمانگاه" : local.Trim();
    }

    private async Task LoadInsuranceBillingPolicyAsync()
    {
        _insurancePolicyLoaded = false;
        var response = await _api.GetAsync<InsurancePolicySettingsResponse>("settings/insurance-policy");
        if (response?.Data is null)
        { _snackbar.Error("دریافت تنظیمات بیمه ناموفق بود؛ پیش از ثبت آن را تازه‌سازی کنید."); return; }
        _insurancePolicyLoaded = true;
        var settings = response.Data;
        var visits = settings.FirstOrDefault(x => x.Key == "ApplyToDoctorVisits")?.Value;
        var services = settings.FirstOrDefault(x => x.Key == "ApplyToMedicalServices")?.Value;
        ApplyInsuranceToDoctorVisits = !bool.TryParse(visits, out var applyVisits) || applyVisits;
        ApplyInsuranceToMedicalServices = !bool.TryParse(services, out var applyServices) || applyServices;
        RecalculateCost();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditReception))]
    private bool _isRefreshingInsurance;

    /// <summary>دریافت دوبارهٔ تنظیمات و شرکت‌های بیمه پس از خطا.</summary>
    [RelayCommand]
    private async Task RefreshInsuranceAsync()
    {
        if (IsSubmitting || IsCreatingUnidentifiedPatient || IsRefreshingInsurance) return;
        if (_isInitializing) { _snackbar.Info("بارگذاری اولیه هنوز تمام نشده است."); return; }
        IsRefreshingInsurance = true;
        var selectedId = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count ? InsuranceCompanies[InsuranceIndex].Id : 0;
        try
        {
            await LoadInsuranceBillingPolicyAsync();
            _insuranceListLoaded = false;
            var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
            if (companies?.Data is null)
            { _snackbar.Error("دریافت فهرست بیمه ناموفق بود؛ دوباره تلاش کنید."); return; }
            InsuranceCompanies.Clear();
            InsuranceCompanies.Add(new InsuranceOption(0, "آزاد (بدون بیمه)", false));
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name, c.IsContracted, c.CoveragePercent));
            _insuranceListLoaded = true;
            var index = InsuranceCompanies.ToList().FindIndex(c => c.Id == selectedId);
            if (index < 0)
            { InsuranceIndex = -1; _snackbar.Warning("شرکت بیمه قبلی در فهرست جدید نیست؛ بیمه را دوباره انتخاب کنید."); }
            else { InsuranceIndex = index; InvalidateInquiry(); }
            await LoadServicesAndPerformersAsync(GetEffectiveServiceInsuranceCompanyId(InsuranceIndex));
            RecalculateCost();
        }
        catch (Exception) { _snackbar.Error("دریافت اطلاعات بیمه ناموفق بود؛ دوباره تلاش کنید."); }
        finally { IsRefreshingInsurance = false; }
    }

    private sealed class InsurancePolicySettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<InsurancePolicySetting>? Data { get; set; }
    }

    private sealed class InsurancePolicySetting
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private int _serviceLoadVersion;
    private bool _serviceTariffsLoaded;

    /// <summary>تلاش مجدد برای دریافت تعرفهٔ بیمهٔ فعلی.</summary>
    [RelayCommand]
    private async Task RefreshServiceTariffsAsync() =>
        await LoadServicesAndPerformersAsync(GetEffectiveServiceInsuranceCompanyId(InsuranceIndex));

    /// <summary>بارگذاری خدمات، تعرفهٔ مؤثر و پرسنل پذیرش خدمت درمانی.</summary>
    /// <param name="insuranceCompanyId">شناسهٔ شرکت طرف‌قرارداد؛ تهی یعنی تعرفهٔ آزاد.</param>
    private async Task LoadServicesAndPerformersAsync(long? insuranceCompanyId = null)
    {
        // خدمات فعال با تعرفهٔ هدفِ منتخب — سهم‌های بیمه/بیمار همراه قیمت می‌آید
        var version = ++_serviceLoadVersion;
        _serviceTariffsLoaded = false;
        var url = $"lookups/services?search=&insuranceCompanyId={insuranceCompanyId?.ToString() ?? ""}";
        ServicesLookupResponse? services;
        try { services = await _api.GetAsync<ServicesLookupResponse>(url); }
        catch (Exception)
        {
            if (version == _serviceLoadVersion) _snackbar.Error("دریافت تعرفه‌ها ناموفق بود؛ دوباره تلاش کنید.");
            return;
        }
        if (version != _serviceLoadVersion) return;
        if (services?.Data is null)
        { _snackbar.Error("دریافت تعرفه‌ها ناموفق بود؛ دوباره تلاش کنید. ثبت خدمت تا دریافت تعرفه معتبر ممکن نیست."); return; }
        Services.Clear();
        foreach (var s2 in services.Data)
            Services.Add(new ServiceOption(s2.ServiceId, s2.ServiceName, s2.Price,
                s2.InsuranceShare, s2.PatientShare));
        RefreshFilteredServices();
        foreach (var line in SelectedServices)
        {
            var same = Services.FirstOrDefault(s => s.Id == line.Service.Id);
            if (same is not null) line.Service = same;
        }
        SyncPrimaryService();
        RecalculateCost();
        _serviceTariffsLoaded = true;
        if (SelectedServices.Any(line => !Services.Any(s => s.Id == line.Service.Id)))
            _snackbar.Warning("برخی خدمات انتخابی در تعرفهٔ جدید نیستند؛ آن‌ها را حذف یا اصلاح کنید.");

        // پرسنل فعال برای انتخاب انجام‌دهنده — فقط یک بار لازم است
        if (Performers.Count == 0)
        {
            var employees = await _api.GetAsync<EmployeesLookupResponse>("lookups/employees");
            foreach (var e in employees?.Data ?? new List<EmployeeLookupDto>())
                Performers.Add(new PerformerOption(e.Id, e.FullName));
        }

        // ═══ چرخهٔ ۳۹ — اتاق‌های فعال برای پذیرش خدمتی؛ فقط یک بار لازم است ═══
        if (Rooms.Count == 0)
        {
            var rooms = await _api.GetAsync<RoomsLookupResponse>("lookups/rooms");
            foreach (var room in rooms?.Data ?? new List<RoomLookupDto>())
                Rooms.Add(new RoomOption(room.Id,
                    !string.IsNullOrWhiteSpace(room.DisplayName) ? room.DisplayName : "اتاق"));
        }

        // ═══ چرخهٔ ۴۵ — اقلام مصرفی: بارگذاری مستقل و همیشه تازه ═══
        // پیش‌تر همین‌جا با گارد «Count == 0» فقط یک‌بار در عمر برنامه بار می‌شد
        // (ریشهٔ «لیست اقلام مصرفی نمیاره»)؛ اکنون نوسازیِ هر صفحه به متد
        // اختصاصی RefreshConsumablesAsync منتقل شده (گارد خطا/هم‌زمانی هم دارد).
        await RefreshConsumablesAsync();
    }

    /// <summary>
    /// نوسازی اقلام مصرفیِ دارای موجودی — چرخهٔ ۴۵.
    /// هر بارگذاری صفحهٔ پذیرش صدا زده می‌شود تا قلم/ورود کالای تازه بی‌درنگ
    /// در کمبو دیده شود (پیش‌تر فقط یک‌بار در عمر برنامه بار می‌شد).
    /// • گارد هم‌زمانی: دو فراخوانی روی هم نمی‌افتند (بارگذاری اولیه + نوسازی
    ///   صفحه می‌توانند پشت‌سرهم باشند)
    /// • گارد خطا: شکست سرور با پیام فارسی دیده می‌شود (فقط بار اول هر نشست —
    ///   جلوگیری از باران توست در هر تغییر بیمه)
    /// • انتخاب فعلی حفظ می‌شود اگر قلم هنوز موجودی دارد
    /// </summary>
    private async Task RefreshConsumablesAsync()
    {
        // نوسازیِ در جریان است — این فراخوانی همان است؛ چیزی گم نمی‌شود
        if (_refreshingConsumables) return;
        _refreshingConsumables = true;
        try
        {
            var consumables = await _api.GetAsync<ConsumablesResponse>("consumables");
            var rows = consumables?.Data;
            if (rows is null)
            {
                // پاسخ ناموفق — پیش از این بی‌صدا می‌ماند و کاربر فقط «لیست خالی»
                // می‌دید؛ اکنون علت دیده می‌شود (یک‌بار در هر نشست)
                if (!_consumablesErrorShown)
                {
                    _consumablesErrorShown = true;
                    _snackbar.Error($"بارگذاری اقلام مصرفی ناموفق: {_api.LastError ?? "خطای ناشناخته"}");
                }
                return;
            }

            // جایگزینی درون‌جای فهرست — کمبو و پالایش زندهٔ آن به همین نمونه وصل‌اند
            ConsumableOptions.Clear();
            foreach (var c in rows)
                ConsumableOptions.Add(new ConsumableOption(c.MedicationId, c.Name, c.Form, c.TotalQuantity, c.SellPrice));

            // انتخاب فعلی اگر هنوز موجودی دارد حفظ شود؛ وگرنه پاک شود (موجودی تمام شده)
            if (SelectedConsumable is { } current
                && ConsumableOptions.FirstOrDefault(x => x.Id == current.Id) is { } fresh)
                SelectedConsumable = fresh;
            else
                SelectedConsumable = null;
        }
        catch (Exception ex)
        {
            if (!_consumablesErrorShown)
            {
                _consumablesErrorShown = true;
                _snackbar.Error($"بارگذاری اقلام مصرفی ناموفق: {ex.Message}");
            }
        }
        finally
        {
            _refreshingConsumables = false;
        }
    }

    // ═════════════════ چرخهٔ ۳۹ — دستورات بخش لوازم مصرفی ═════════════════

    /// <summary>
    /// افزودن قلم مصرفی به فهرست مصرف این پذیرش — اگر همان قلم قبلاً باشد
    /// تعدادش افزایش می‌یابد (ادغام) تا فهرست تمیز بماند؛ سقف موجودی انبار
    /// سمت کلاینت هم کنترل می‌شود (سرور هم جداگانه رد می‌کند).
    /// </summary>
    [RelayCommand]
    private void AddConsumable()
    {
        if (SelectedConsumable is null)
        {
            _snackbar.Error("ابتدا قلم مصرفی را انتخاب کنید");
            return;
        }
        if (ConsumableQty <= 0)
        {
            _snackbar.Error("تعداد باید بیشتر از صفر باشد");
            return;
        }

        // ادغام با ردیف موجود همان قلم
        var existing = Consumables.FirstOrDefault(c => c.Option.Id == SelectedConsumable.Id);
        var newQuantity = (existing?.Quantity ?? 0) + ConsumableQty;
        if (newQuantity > SelectedConsumable.Available)
        {
            _snackbar.Error($"موجودی انبار «{SelectedConsumable.Name}» فقط {SelectedConsumable.Available:0.#} است");
            return;
        }

        if (existing is not null)
            existing.Quantity = newQuantity;
        else
        {
            var line = new ConsumableLine(SelectedConsumable, ConsumableQty);
            line.LineChanged += (_, _) => RefreshConsumableTotals();
            Consumables.Add(line);
        }

        ConsumableQty = 1;
        SelectedConsumable = null;
        RefreshConsumableTotals();
    }

    /// <summary>حذف ردیف مصرف از فهرست</summary>
    /// <param name="line">ردیف قابل حذف — از CommandParameter دکمه می‌آید</param>
    [RelayCommand]
    private void RemoveConsumable(ConsumableLine? line)
    {
        if (line is null) return;
        Consumables.Remove(line);
        RefreshConsumableTotals();
    }

    /// <summary>
    /// نوسازی جمع‌های مصرفی — پس از هر افزودن/حذف/تغییر تعداد فراخوانده می‌شود؛
    /// جمع مصرفی + قابل پرداخت نهایی + نمایش بخش خالی/پر را با هم تازه می‌کند.
    /// </summary>
    private void RefreshConsumableTotals()
    {
        OnPropertyChanged(nameof(ConsumablesTotal));
        OnPropertyChanged(nameof(ConsumablesTotalDisplay));
        OnPropertyChanged(nameof(PayableByPatient));
        OnPropertyChanged(nameof(PayableByPatientDisplay));
        OnPropertyChanged(nameof(HasConsumables));
    }

    /// <summary>پاسخ فهرست خدمات — lookups/services</summary>
    private sealed class ServicesLookupResponse
    {
        public bool IsSuccess { get; set; }
        public List<ServiceLookupDto>? Data { get; set; }
    }

    /// <summary>ردیف خدمت تعرفه‌دار</summary>
    private sealed class ServiceLookupDto
    {
        public long ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        /// <summary>درصد سهم بیمهٔ تعرفهٔ منتخب — چرخهٔ ۲۶</summary>
        public decimal InsuranceShare { get; set; }

        /// <summary>درصد سهم بیمار تعرفهٔ منتخب — چرخهٔ ۲۶</summary>
        public decimal PatientShare { get; set; }
    }

    /// <summary>پاسخ فهرست پرسنل</summary>
    private sealed class EmployeesLookupResponse
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeLookupDto>? Data { get; set; }
    }

    /// <summary>ردیف پرسنل</summary>
    private sealed class EmployeeLookupDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>پاسخ فهرست اتاق‌های فعال — چرخهٔ ۳۹</summary>
    private sealed class RoomsLookupResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoomLookupDto>? Data { get; set; }
    }

    /// <summary>
    /// ردیف اتاق فعال — چرخهٔ ۴۳ (گزارش مالک: «در پذیرش اسم اتاق‌ها رو درست
    /// نمیاره و همشون رو اتاق مینویسه»): سرور فیلد «displayName» می‌فرستد (نام
    /// اتاق یا «اتاق + شماره»)؛ پیش‌تر DTO کلاینت RoomName/RoomNumber می‌خواند که
    /// در پاسخ سرور وجود ندارد → هر دو تهی می‌ماند و متن پس‌قرینهٔ «اتاق » بدون
    /// شماره نمایش داده می‌شد. DoctorsViewModel از همان ابتدا درست بود و
    /// DisplayName را می‌خواند (قانون هم‌کلاسی‌ها: اینجا هم همان قرارداد).
    /// </summary>
    private sealed class RoomLookupDto
    {
        public long Id { get; set; }
        public string? DisplayName { get; set; }
    }

    /// <summary>پاسخ فهرست اقلام مصرفی — چرخهٔ ۳۹</summary>
    private sealed class ConsumablesResponse
    {
        public bool IsSuccess { get; set; }
        public List<ConsumableDto>? Data { get; set; }
    }

    /// <summary>ردیف قلم مصرفی از GET pharmacy/consumables</summary>
    private sealed class ConsumableDto
    {
        public long MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Form { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal SellPrice { get; set; }
    }

    /// <summary>پاسخ ثبت مصرف لوازم — POST pharmacy/consume</summary>
    private sealed class ConsumeResponse
    {
        public bool IsSuccess { get; set; }
        public ConsumeResultDto? Data { get; set; }
    }

    /// <summary>رسید مصرف با اقلام و قیمت واقعی بچ‌ها</summary>
    private sealed class ConsumeResultDto
    {
        public long ReceptionId { get; set; }
        public string ReceptionNumber { get; set; } = string.Empty;
        public List<ConsumeItemResultDto>? Items { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>قلم مصرف‌شدهٔ واقعی با قیمت محاسبه‌شده از بچ‌ها</summary>
    private sealed class ConsumeItemResultDto
    {
        public long MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    private int _inquiryVersion;
    private void InvalidateInquiry()
    {
        _inquiryVersion++;
        Inquiry = null;
        CoveragePercent = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count
            ? InsuranceCompanies[InsuranceIndex].CoveragePercent : 0m;
    }

    /// <summary>تغییر بیمه → محاسبه هزینه + بارگذاری مجدد تعرفه‌ها با بیمهٔ جدید</summary>
    partial void OnInsuranceIndexChanged(int value)
    {
        InvalidateInquiry();
        var selectedCompany = value >= 0 && value < InsuranceCompanies.Count
            ? InsuranceCompanies[value]
            : null;
        CoveragePercent = selectedCompany?.CoveragePercent ?? 0m;
        RecalculateCost();

        // پاسخ‌های دیررس نباید تعرفهٔ بیمهٔ فعلی را بازنویسی کنند.
        _ = LoadServicesAndPerformersAsync(GetEffectiveServiceInsuranceCompanyId(value));
    }

    /// <summary>استعلام بیمه — با Timeout ۳۰ ثانیه و پیام مناسب</summary>
    [RelayCommand]
    private async Task VerifyInsuranceAsync()
    {
        if (IsInquiring || IsSubmitting) return;
        if (FoundPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        if (InsuranceIndex <= 0 || InsuranceIndex >= InsuranceCompanies.Count) { _snackbar.Error("ابتدا نوع بیمه را انتخاب کنید"); return; }

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع آف‌بای‌وان ═══
        // آیتم «آزاد» با شناسهٔ صفر در ابتدای کشوست و شرکت‌های واقعی از
        // نمایهٔ ۱؛ خواندن با «نمایهٔ منهای یک» شرکت بعدی را می‌داد و بیمهٔ
        // انتخابی بیمار در پذیرش قعر می‌رفت (ریشهٔ «تامین اجتماعی زدم ولی
        // آزاد ثبت می‌شود» در صف پذیرش)؛ اکنون خودِ نمایه خوانده می‌شود
        var company = InsuranceCompanies[InsuranceIndex];
        if (!company.IsContracted)
        {
            _snackbar.Error("این شرکت طرف قرارداد نیست؛ محاسبه به‌صورت آزاد انجام می‌شود");
            return;
        }
        if (ReceptionModeIndex == 0 && !ApplyInsuranceToDoctorVisits ||
            ReceptionModeIndex == 1 && !ApplyInsuranceToMedicalServices)
        {
            _snackbar.Info("اعمال بیمه برای این دسته در تنظیمات درمانگاه غیرفعال است");
            return;
        }
        var inquiryVersion = ++_inquiryVersion;
        IsInquiring = true;
        try
        {
            var response = await _api.PostAsync<InquiryResponse>("insurance/inquiries", new
            {
                patientId = FoundPatient.Id,
                insuranceCompanyId = company.Id,
                insuranceNumber = string.IsNullOrWhiteSpace(InsuranceNumber) ? null : InsuranceNumber.Trim()
            });

            if (inquiryVersion != _inquiryVersion) return;
            if (response?.Data is null)
            {
                CoveragePercent = 0;
                Inquiry = new InquiryState(true, false, _api.LastError ?? "استعلام بیمه ناموفق بود", null, 0);
                _snackbar.Error("استعلام بیمه ناموفق بود: " + Inquiry.Message);
            }
            else
            {
                Inquiry = new InquiryState(true, response.Data.IsEligible, response.Data.Message,
                    response.Data.ApprovalCode, response.Data.CoveragePercent);
                if (response.Data.IsEligible)
                {
                    CoveragePercent = response.Data.CoveragePercent;
                    _snackbar.Success($"✅ بیمه تأیید شد — کد: {response.Data.ApprovalCode ?? "—"}");
                }
                else
                {
                    CoveragePercent = 0;
                    _snackbar.Info($"بیمه معتبر نیست: {response.Data.Message}");
                }
            }
        }
        catch (TaskCanceledException)
        {
            if (inquiryVersion != _inquiryVersion) return;
            // Timeout — «سامانه بیمه پاسخ نداد - ادامه بصورت آزاد؟»
            var proceed = Views.AppDialog.Confirm("عدم پاسخ سامانه بیمه", "سامانه بیمه پاسخ نداد. پذیرش به‌صورت آزاد ادامه یابد؟", Views.AppDialogIcon.Warning) == true;
            if (proceed)
            {
                InsuranceIndex = 0;
                Inquiry = null; // انتخاب صریح آزاد؛ استعلام شکست‌خورده مانع پذیرش آزاد نیست
                _snackbar.Info("پذیرش آزاد انتخاب شد؛ تعرفهٔ آزاد را بررسی کنید.");
            }
        }
        finally
        {
            IsInquiring = false;
            RecalculateCost();
        }
    }

    /// <summary>
    /// محاسبه خودکار هزینه — حق ویزیت × درصد بیمه.
    /// چرخه بیست‌ویکم (گزارش پانزدهم مالک): «برای پزشک ۴۵۰ حق ویزیت زدم ولی در
    /// پذیرش ۷۰ می‌آورد» — پیش‌تر مقدار ثابت هاردکد ۷۰۰٬۰۰۰ ریال بود؛ اکنون
    /// حق ویزیت واقعی از پروندهٔ همان پزشک (DoctorOption.ConsultationFee) خوانده می‌شود.
    /// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ «در پذیرش خدمات درمانی،
    /// تعرفه خدمات رو نمیاره» — در حالت خدمت درمانی، قیمت و سهم‌های بیمه/
    /// بیمار از «تعرفهٔ همان خدمت برای بیمهٔ منتخب» اعمال می‌شود (تعرفهٔ آزاد:
    /// ۱۰۰٪ بیمار) و بخش هزینه بی‌درنگ به‌روز می‌شود.
    /// ═══ چرخهٔ سی‌ودوم (گزارش مالک) ═══ «در پذیرش هم درست اعمال نمی‌شود و
    /// برای بیمهٔ آزاد هم سهم بیمه و سهم بیمار می‌اندازد که اشتباه است — در
    /// بدون بیمه باید تمام تعرفه را بیمار پرداخت کند»: اگر بیمهٔ منتخب
    /// «آزاد» است، هر درصد بیمه‌ای (حتی مقدار ذخیره‌شدهٔ قدیمی روی تعرفهٔ آزاد)
    /// نادیده گرفته می‌شود؛ سهم بیمه صفر و کل مبلغ عهدهٔ بیمار است.
    /// </summary>
    private void RecalculateCost()
    {
        // بیمهٔ منتخب فعلی — نمایهٔ صفر «آزاد (بدون بیمه)» با شناسهٔ ۰ است
        var selectedCompany = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count
            ? InsuranceCompanies[InsuranceIndex]
            : null;
        var hasContractedInsurance = selectedCompany is { Id: > 0, IsContracted: true };
        var categoryEnabled = ReceptionModeIndex == 1
            ? ApplyInsuranceToMedicalServices
            : ApplyInsuranceToDoctorVisits;
        var hasApplicableInsurance = hasContractedInsurance && categoryEnabled && Inquiry is not { IsDone: true, IsEligible: false };
        // ستون سهم بیمه فقط وقتی دیده می‌شود که شرکت طرف قرارداد باشد و
        // تنظیم مستقل همان دستهٔ ویزیت/خدمت اجازهٔ اعمال بیمه بدهد.
        HasInsurance = hasApplicableInsurance;

        if (ReceptionModeIndex == 1)
        {
            // ═══ حالت خدمت درمانی — تعرفهٔ خدمت منتخب ═══
            // قیمت = تعرفهٔ خدمت برای بیمهٔ منتخب؛ سهم‌ها از درصدهای تعرفهٔ همان
            // خدمت — مگر در حالت آزاد که کل مبلغ سهم بیمار است (۰٪ بیمه)
            // پذیرش چندخدمتی: جمع تعرفهٔ همهٔ ردیف‌ها (قیمت × تعداد) و سهم
            // بیمهٔ هر ردیف با درصد تعرفهٔ خودش محاسبه می‌شود
            VisitFee = SelectedServices.Sum(l => l.Service.Price * l.Quantity);
            InsuranceShare = SelectedServices.Sum(l => Math.Round(
                l.Service.Price * l.Quantity *
                (hasApplicableInsurance ? l.Service.TariffInsuranceShare : 0m) / 100m));
            PatientShare = VisitFee - InsuranceShare;
        }
        else
        {
            // ═══ حالت ویزیت پزشک — رفتار پیشین ═══
            // حق ویزیت واقعی پزشک انتخاب‌شده — صفر یعنی پزشک هنوز انتخاب نشده
            VisitFee = SelectedDoctor?.ConsultationFee ?? 0;

            // درصد پوشش: از استعلام یا صفر برای آزاد
            var percent = hasApplicableInsurance ? CoveragePercent : 0m;
            InsuranceShare = Math.Round(VisitFee * percent / 100m);
            PatientShare = VisitFee - InsuranceShare;
        }

        OnPropertyChanged(nameof(FeeDisplay));
        OnPropertyChanged(nameof(InsuranceShareDisplay));
        OnPropertyChanged(nameof(PatientShareDisplay));
        // چرخهٔ ۳۹ — سهم بیمار تغییر کرد ⇒ قابل پرداخت نهایی (با مصرفی‌ها) هم باید تازه شود
        OnPropertyChanged(nameof(SelectedServicesSummaryText));
        OnPropertyChanged(nameof(PayableByPatient));
        OnPropertyChanged(nameof(PayableByPatientDisplay));
    }

    /// <summary>حذف ارقام غیررقمی</summary>
    private static string CleanDigits(string text) => new(text.Where(char.IsDigit).ToArray());

    /* ═══════════ ثبت پذیرش ═══════════ */

    /// <summary>اعتبارسنجی مشترک — چرخه ۲۴: در حالت خدمت درمانی به‌جای پزشک، خدمت الزامی است</summary>
    private bool ValidateReception()
    {
        if (FoundPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return false; }
        if (_isInitializing || !_insuranceListLoaded || !_insurancePolicyLoaded || IsRefreshingInsurance || InsuranceIndex < 0 || InsuranceIndex >= InsuranceCompanies.Count)
        { _snackbar.Error("اطلاعات بیمه آماده نیست؛ تنظیمات و فهرست بیمه را تازه‌سازی و انتخاب کنید."); return false; }
        if (IsInquiring) { _snackbar.Error("استعلام بیمه هنوز در حال انجام است؛ پیش از ثبت منتظر پاسخ بمانید."); return false; }
        if (_selectedAppointmentId is not null && (ReceptionModeIndex != 0 || FoundPatient.Id != _selectedAppointmentPatientId || SelectedDoctor?.Id != _selectedAppointmentDoctorId))
        { _snackbar.Error("بیمار یا پزشک پذیرش با نوبت انتخاب‌شده مطابقت ندارد؛ نوبت را بررسی یا جستجو را پاک کنید."); return false; }
        if (Inquiry is { IsDone: true, IsEligible: false })
        { _snackbar.Error("استعلام بیمه تأیید نشده است؛ پیش از ثبت مجدداً استعلام کنید یا بیمهٔ آزاد را انتخاب کنید."); return false; }
        if (ReceptionModeIndex == 0 && ApplyInsuranceToDoctorVisits && Inquiry is { IsDone: true, IsEligible: true } &&
            InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count &&
            InsuranceCompanies[InsuranceIndex] is { Id: > 0, IsContracted: true } company && CoveragePercent != company.CoveragePercent)
        { _snackbar.Error("درصد استعلام با درصد قابل محاسبه در فاکتور یکسان نیست؛ پیش از ثبت با مسئول بیمه یا صندوق بررسی کنید."); return false; }
        if (ReceptionModeIndex == 1)
        {
            if (SelectedServices.Count == 0) { _snackbar.Error("ابتدا دست‌کم یک خدمت درمانی انتخاب کنید"); return false; }
            if (!_serviceTariffsLoaded || SelectedServices.Any(line => !Services.Any(s => s.Id == line.Service.Id)))
            { _snackbar.Error("تعرفهٔ معتبر بیمهٔ فعلی آماده نیست یا خدمتی در فهرست جدید وجود ندارد؛ تعرفه را تازه‌سازی و خدمات را بررسی کنید."); return false; }
        }
        else
        {
            if (SelectedDoctor is null) { _snackbar.Error("ابتدا پزشک را انتخاب کنید"); return false; }
        }
        return true;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditReception))]
    [NotifyPropertyChangedFor(nameof(CanRetryReception))]
    private bool _isSubmitting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditReception))]
    [NotifyPropertyChangedFor(nameof(CanRetryReception))]
    private bool _isReceptionOutcomeUnknown;

    /// <summary>فرم در طول ثبت و تا بررسی نتیجه نامعلوم قابل تغییر نیست.</summary>
    public bool CanEditReception => !IsSubmitting && !IsReceptionOutcomeUnknown && !IsCreatingUnidentifiedPatient && !IsRefreshingInsurance;

    public bool CanRetryReception => !IsSubmitting && IsReceptionOutcomeUnknown && _pendingReception is not null &&
        (_api as ApiClient)?.BranchId == _pendingReception.BranchId &&
        (_api is not ApiClient client || client.PaymentRecoveryServer == _invoiceRecoveryServer);
    public string ReceptionRecoveryInfo => _pendingReception is null ? string.Empty :
        $"درخواست ذخیره‌شده: {_pendingReception.PatientName} (شناسه {_pendingReception.Request.PatientId}) — {_pendingReception.TargetName}؛ نوبت: {_pendingReception.Request.AppointmentId?.ToString() ?? "بدون نوبت"}؛ بیمه: {_pendingReception.InsuranceName}؛ کد ارجاع: {_pendingReception.Request.ReferralCode ?? "ندارد"}. ارسال دوباره ممکن است پذیرش را ثبت کند؛ ابتدا صف، انبار، فاکتور و پرداخت را بررسی کنید. ادامه زنجیره خودکار نیست.";

    private void RestorePendingReception(PendingReception pending)
    {
        _pendingReception = pending;
        IsReceptionOutcomeUnknown = true;
        OnPropertyChanged(nameof(ReceptionRecoveryInfo));
        OnPropertyChanged(nameof(CanRetryReception));
    }

    /// <summary>فقط همان پذیرش بازپخش می‌شود؛ مراحل بعدی به‌صورت خودکار تکرار نمی‌شوند.</summary>
    [RelayCommand]
    private async Task RetryReceptionAsync()
    {
        if (!CanRetryReception || _pendingReception is null) return;
        var pending = _pendingReception;
        IDisposable? lease = null;
        try
        {
            lease = _receptionRecoveryStore?.AcquireWorkflow(_invoiceRecoveryServer, pending.Request.PatientId);
            IsSubmitting = true;
            if (!CanRetryContext(pending))
            { _snackbar.Error("سرور یا شعبه تغییر کرده است؛ درخواست را در محل اصلی بررسی کنید."); return; }
            var result = await _api.PostAsync<ReceptionCreateResponse>("receptions/idempotent", pending.Request);
            if (result?.Data is null or <= 0)
            { _snackbar.Error("نتیجه پذیرش هنوز نامعلوم است؛ صف و سایر مراحل را بررسی کنید. " + _api.LastError); return; }
            try { _receptionRecoveryStore?.Clear(pending); }
            catch (Exception)
            { _snackbar.Error($"پذیرش {result.Data} تأیید شد اما فایل بازیابی پاک نشد؛ با پشتیبانی بررسی کنید."); return; }
            _pendingReception = null;
            OnPropertyChanged(nameof(ReceptionRecoveryInfo));
            OnPropertyChanged(nameof(CanRetryReception));
            _snackbar.Info($"پذیرش {result.Data} تأیید شد؛ مصرف، فاکتور و پرداخت خودکار انجام نشدند. سوابق را بررسی کنید.");
        }
        catch (Exception) { _snackbar.Error("بازیابی پذیرش انجام نشد یا زنجیره هنوز در حال اجراست؛ سوابق را بررسی کنید."); }
        finally { IsSubmitting = false; lease?.Dispose(); }
    }

    private bool CanRetryContext(PendingReception pending) => (_api as ApiClient)?.BranchId == pending.BranchId &&
        (_api is not ApiClient client || client.PaymentRecoveryServer == _invoiceRecoveryServer);

    /// <summary>«پذیرش و فاکتور» — ثبت پذیرش + صدور فاکتور بدون پرداخت</summary>
    [RelayCommand]
    private async Task ReceptionAndInvoiceAsync() => await SubmitReceptionAsync(null);

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — کارت‌خوان در پذیرش ═══
    // «در پذیرش فقط پرداخت نقدی وجود داره و کارت خوان پیاده سازی نشده» —
    // دکمهٔ «پذیرش و پرداخت» اکنون پنجرهٔ انتخاب روش (نقدی/کارتخوان + مرجع و
    // چهار رقم کارت برای کارتی — همان الگوی ماژول مالی) را می‌گشاید و سپس
    // کل زنجیره (پذیرش → مصرف لوازم → فاکتور → پرداخت با روش منتخب) اجرا می‌شود.

    /// <summary>
    /// «پذیرش و پرداخت» — گشودن پنجرهٔ پرداخت (نقدی/کارتخوان) و سپس ثبت زنجیره
    /// </summary>
    [RelayCommand]
    private async Task ReceptionAndPayAsync()
    {
        if (IsSubmitting || IsReceptionOutcomeUnknown || !ValidateReception()) return;

        // مبلغ پیش‌بینی‌شده = سهم بیمار خدمت + جمع لوازم مصرفی (قیمت پیشنهادیِ
        // بچ پرموجودی؛ مبلغ نهایی پس از مصرف واقعی از بچ‌ها در زنجیره محاسبه می‌شود)
        var confirmedPayable = PayableByPatient;
        if (confirmedPayable <= 0m)
        { _snackbar.Info("مبلغ قابل پرداخت مثبت نیست؛ برای صدور بدون پرداخت از «پذیرش و فاکتور» استفاده کنید."); return; }
        var payVm = new ReceptionPayViewModel(PayableByPatientDisplay);
        var payWindow = new Views.ReceptionPayWindow(payVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        payWindow.ShowDialog();

        // انصراف — هیچ چیزی ثبت نمی‌شود
        if (payVm.Result is null) return;

        await SubmitReceptionAsync(payVm.Result, confirmedPayable);
    }

    /// <summary>
    /// اجرای زنجیره پذیرش طبق فرم ۱۰ — پذیرش + مصرف لوازم + فاکتور + پرداخت اختیاری
    /// </summary>
    /// <param name="payment">اطلاعات پرداخت منتخب از پنجرهٔ پرداخت — تهی یعنی بدون پرداخت (فقط فاکتور)</param>
    private async Task SubmitReceptionAsync(ReceptionPaymentInfo? payment, decimal? confirmedPayable = null)
    {
        if (IsSubmitting || IsReceptionOutcomeUnknown || !ValidateReception()) return;
        try
        {
            if (_invoiceRecoveryStore?.Load(_invoiceRecoveryServer, FoundPatient!.Id) is not null)
            {
                _snackbar.Error("فاکتور حل‌نشده‌ای برای این بیمار روی این دستگاه وجود دارد؛ پیش از پذیرش جدید، آن را در صندوق بررسی کنید.");
                return;
            }
        }
        catch (Exception)
        {
            _snackbar.Error("بازیابی فاکتور بیمار قابل بررسی نیست؛ پذیرش ارسال نشد. با پشتیبانی تماس بگیرید.");
            return;
        }
        IDisposable? workflowLease = null;
        try
        {
            workflowLease = _receptionRecoveryStore?.AcquireWorkflow(_invoiceRecoveryServer, FoundPatient!.Id);
            var existing = _receptionRecoveryStore?.Load(_invoiceRecoveryServer, FoundPatient!.Id);
            if (existing is not null)
            {
                RestorePendingReception(existing);
                workflowLease?.Dispose();
                _snackbar.Error("درخواست پذیرش حل‌نشده وجود دارد؛ ابتدا سوابق را بررسی کنید.");
                return;
            }
        }
        catch (Exception)
        {
            workflowLease?.Dispose();
            _snackbar.Error("قفل یا بازیابی پذیرش در دسترس نیست؛ درخواست جدید ارسال نشد.");
            return;
        }
        IsSubmitting = true;
        var receptionBranch = (_api as ApiClient)?.BranchId;
        try
        {

        // ═══ چرخهٔ بیست‌وششم — رفع آف‌بای‌وان بیمه (همان ریشهٔ بالا) ═══
        // نمایهٔ صفر = آزاد (شناسهٔ ۰) → تهی ارسال می‌شود؛ نمایه‌های بعدی
        // خودِ شرکت منتخب — نه یک شیفت عقب
        var selectedCompany = InsuranceIndex >= 0 && InsuranceIndex < InsuranceCompanies.Count
            ? InsuranceCompanies[InsuranceIndex]
            : null;
        var company = selectedCompany is { Id: > 0 } ? selectedCompany : null;

        IsBusy = true;

        // ═══ چرخه ۲۴ — پذیرش خدمت درمانی یا ویزیت پزشک ═══
        var isServiceMode = ReceptionModeIndex == 1;

        // ۱. رزرو پایدار پیش از ثبت پذیرش؛ کلید تا پایان زنجیره حفظ می‌شود.
        var receptionRequest = new CreateReceptionDto(FoundPatient!.Id,
            isServiceMode ? null : SelectedDoctor!.Id, _selectedAppointmentId, company?.Id ?? 0L,
            string.IsNullOrWhiteSpace(ReferralCode) ? null : ReferralCode.Trim(),
            string.IsNullOrWhiteSpace(ReceptionNotes) ? null : ReceptionNotes.Trim(),
            isServiceMode ? SelectedService!.Id : null, isServiceMode ? BuildServiceSummary() : null,
            isServiceMode ? SelectedPerformer?.Id : null, isServiceMode ? SelectedRoom?.Id : null, Guid.NewGuid());
        var pendingReception = new PendingReception(receptionRequest, FoundPatient.FullName,
            isServiceMode ? BuildServiceSummary() : SelectedDoctor!.Name, _invoiceRecoveryServer, receptionBranch, company?.Name ?? "آزاد");
        try { _receptionRecoveryStore?.Reserve(pendingReception); }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            try
            {
                var existing = _receptionRecoveryStore?.Load(_invoiceRecoveryServer, FoundPatient.Id);
                if (existing is not null) RestorePendingReception(existing);
            }
            catch (Exception) { }
            _snackbar.Error("رزرو ایمن پذیرش ناموفق بود و درخواست جدید ارسال نشد؛ سوابق را بررسی کنید.");
            return;
        }
        _pendingReception = pendingReception;
        OnPropertyChanged(nameof(ReceptionRecoveryInfo));
        if (!CanRetryContext(pendingReception))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error("سرور یا شعبه تغییر کرد؛ پذیرش ارسال نشد و درخواست برای بررسی حفظ شد.");
            return;
        }
        var reception = await _api.PostAsync<ReceptionCreateResponse>("receptions/idempotent", receptionRequest);
        if (reception?.Data is null or <= 0)
        {
            IsReceptionOutcomeUnknown = true;
            IsBusy = false;
            _snackbar.Error((_api.LastError ?? "پاسخ ثبت پذیرش دریافت نشد") + "؛ پیش از تکرار، صف پذیرش را بررسی کنید.");
            return;
        }

        if (!CanRetryContext(pendingReception))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ سرور یا شعبه تغییر کرد و مراحل بعدی ارسال نشدند. ابتدا صف و صندوق را بررسی کنید.");
            return;
        }

        // ═══ ۱/۵. ثبت مصرف لوازم درمانگاه — چرخهٔ ۳۹ (فقط پذیرش خدمتی با اقلام مصرفی) ═══
        // «وقتی بیمار با خودش نمیاره باید از لوازم خود درمانگاه استفاده بکنیم و
        // در پذیرش برای اونها هم هزینه به هزینه‌های بیمار اضافه بشه» — پیش از صدور
        // فاکتور، موجودی FEFO کم می‌شود و قیمت واقعی بچ‌ها برای اقلام فاکتور
        // برمی‌گردد؛ شکست مصرف (مثلاً کمبود هم‌زمان موجودی) با دیالوگ فارسی و
        // با نتیجهٔ نامعلوم ادامه نمی‌دهیم؛ پذیرش ثبت‌شده باقی می‌ماند تا
        // مصرف و فاکتور به‌صورت دستی بررسی و تکمیل شوند.
        ConsumeResultDto? consumeResult = null;
        if (isServiceMode && Consumables.Count > 0)
        {
            var consume = await _api.PostAsync<ConsumeResponse>("consumables/consume", new
            {
                receptionId = reception.Data,
                items = Consumables.Select(c => new { medicationId = c.Option.Id, quantity = c.Quantity }).ToList()
            });
            if (consume?.Data is null)
            {
                IsReceptionOutcomeUnknown = true;
                IsBusy = false;
                _snackbar.Error($"پذیرش با شناسه {reception.Data} ثبت شد؛ پاسخ برداشت لوازم دریافت نشد یا ناموفق بود. پیش از تکرار یا صدور فاکتور، مصرف و موجودی را بررسی کنید.");
                await LoadAsync();
                return;
            }
            consumeResult = consume.Data;
        }

        if (!CanRetryContext(pendingReception))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ سرور یا شعبه تغییر کرد و فاکتور ارسال نشد. مصرف و صف را بررسی کنید.");
            return;
        }

        // ۲. صدور فاکتور خودکار — قلم ویزیت با کد خدمت K
        // ═══ چرخهٔ سی‌ویکم (گزارش مالک) — ریشهٔ «هیچ مبلغی در هیچ‌کجای برنامه
        // ذخیره نشد» ═══ دو ایراد ساختاری همین‌جا بود:
        //   (الف) ناهمخوانی نام فیلدها: دسکتاپ «serviceName» و «discount» می‌فرستاد
        //        ولی قرارداد سرور «description» و «discountPercent» است → نام قلم
        //        فاکتور برای همیشه تهی می‌ماند (ServiceName نادیده گرفته می‌شد)؛
        //        اکنون نام‌های صحیح قرارداد ارسال می‌شود.
        //   (ب) بلع خاموش خطا: اگر صدور فاکتور یا ثبت پرداخت شکست بخورد، پیام
        //        موفقیت بی‌قید و شرط چاپ می‌شد و کاربر گمان می‌کرد همه‌چیز ثبت
        //        شده؛ اکنون شکست هر حلقه با دیالوگ فارسی خودِ برنامه (قانون ششم
        //        مالک — بدون MessageBox ویندوز) اعلام و کل زنجیره صادقانه گزارش می‌شود.
        var invoiceDto = new CreateInvoiceDto(FoundPatient!.Id, reception.Data, null, InvoiceType.Visit,
            company?.Id, 0m, BuildInvoiceItems(isServiceMode, consumeResult),
            isServiceMode ? $"پذیرش خدمت {BuildServiceSummary()} — {FoundPatient.FullName}" : $"پذیرش {FoundPatient.FullName}");
        var preview = await _api.PostAsync<InvoicePreviewResponse>("invoices/preview", invoiceDto);
        if (preview?.Data is null || string.IsNullOrWhiteSpace(preview.Data.QuoteVersion))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ محاسبه فاکتور دریافت نشد و فاکتور ارسال نشد. مصرف و صندوق را بررسی کنید. " + _api.LastError);
            return;
        }
        if (payment is not null && preview.Data.PatientPayable != confirmedPayable)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ مبلغ سرور با مبلغ تأییدشده متفاوت است و فاکتور یا پرداخت ارسال نشد. در صندوق بررسی کنید.");
            return;
        }
        if (!CanRetryContext(pendingReception))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ سرور یا سرور یا شعبه تغییر کرد و فاکتور ارسال نشد.");
            return;
        }
        var request = invoiceDto with { ExpectedQuote = preview.Data.QuoteVersion, RequestId = Guid.NewGuid() };
        var pending = new PendingInvoice(request, preview.Data, _invoiceRecoveryServer, receptionBranch);
        try { _invoiceRecoveryStore?.Reserve(pending); }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ رزرو ایمن فاکتور ناموفق بود و درخواست ارسال نشد. بازیابی و صندوق را بررسی کنید.");
            return;
        }
        if (!CanRetryContext(pendingReception))
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ فاکتور ارسال نشد و درخواست برای بررسی در صندوق ذخیره شد.");
            return;
        }
        var invoice = await _api.PostAsync<InvoiceCreateResponse>("invoices/idempotent", request);
        if (invoice?.Data is null or <= 0)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} ثبت شد؛ نتیجه صدور فاکتور نامعلوم است. پس از بررسی سوابق، بیمار را در فرم فاکتور صندوق برای بازیابی انتخاب کنید. " + _api.LastError);
            return;
        }
        try { _invoiceRecoveryStore?.Clear(pending); }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"فاکتور {invoice.Data} صادر شد اما فایل بازیابی پاک نشد؛ پرداخت ارسال نشد. در صندوق بررسی کنید.");
            return;
        }

        // ۳. پرداخت (اختیاری) — روش و مرجع از پنجرهٔ پرداخت پذیرش (چرخهٔ ۳۹)
        var paymentFailed = false;
        var paymentConfirmedButRecoveryRetained = false;
        if (payment is not null && invoice?.Data is not null && invoice.Data > 0)
        {
            if (!CanRetryContext(pendingReception))
            {
                IsReceptionOutcomeUnknown = true;
                _snackbar.Error($"فاکتور {invoice.Data} صادر شد؛ سرور یا شعبه تغییر کرد و پرداخت ارسال نشد. در صندوق بررسی کنید.");
                return;
            }
            // چرخهٔ ۳۹ — مبلغ قابل پرداخت بیمار = سهم بیمار خدمت + جمع لوازم مصرفی
            // (قیمت واقعی بچ‌ها از مصرف؛ اگر مصرف رد شده و کاربر ادامه داد، فقط خدمت)
            var detail = await _api.GetAsync<ReceptionInvoiceEnvelope>($"invoices/{invoice.Data}");
            var payable = detail?.Data?.RemainingAmount;
            if (!CanAutomaticallyPay(detail?.Data, invoice.Data.Value, FoundPatient.Id, confirmedPayable))
            {
                paymentFailed = true;
                _snackbar.Warning($"فاکتور صادر شد، اما مبلغ نهایی تأیید نشد یا با مبلغ تأییدشده تفاوت دارد؛ پرداخت ثبت نشد. شناسه فاکتور {invoice.Data} را در صندوق بررسی کنید.");
            }
            else if (!CanRetryContext(pendingReception))
            {
                paymentFailed = true;
                _snackbar.Warning($"فاکتور {invoice.Data} صادر شد؛ سرور یا شعبه تغییر کرد و پرداخت ارسال نشد. در صندوق بررسی کنید.");
            }
            else
            {
                var sentPayment = false;
                try
                {
                    var attempt = PaymentAttempt.Reserve(_api, invoice.Data.Value, FoundPatient.Id, payable!.Value,
                        payment.Method, payment.ReferenceNumber, payment.CardNumber);
                    if (!attempt.ContextMatches)
                    {
                        paymentFailed = true;
                        _snackbar.Warning($"فاکتور {invoice.Data} صادر شد؛ سرور یا شعبه تغییر کرد و پرداخت ارسال نشد. بازیابی را در صندوق بررسی کنید.");
                    }
                    else
                    {
                        sentPayment = true;
                        var paymentResult = await _api.PostAsync<PaymentCreateResponse>("payments/idempotent", attempt.Request.ToRequest());
                        if (paymentResult?.Data is null || paymentResult.Data <= 0)
                        {
                            paymentFailed = true;
                            Views.AppDialog.Error("پاسخ پرداخت دریافت نشد",
                                $"فاکتور {invoice.Data} صادر شد ولی نتیجه پرداخت مشخص نیست. پس از بررسی دریافت وجه و سوابق، همان فاکتور را در صندوق باز کنید.{Environment.NewLine}{_api.LastError}");
                        }
                        else
                        {
                            try { attempt.Clear(); }
                            catch (Exception)
                            {
                                paymentFailed = true;
                                paymentConfirmedButRecoveryRetained = true;
                                _snackbar.Warning($"پرداخت فاکتور {invoice.Data} ثبت شد ولی فایل بازیابی پاک نشد؛ در صندوق بررسی کنید.");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    paymentFailed = true;
                    _snackbar.Warning(sentPayment
                        ? $"فاکتور {invoice.Data} صادر شد؛ نتیجه پرداخت نامعلوم است و باید در صندوق بررسی شود."
                        : $"فاکتور {invoice.Data} صادر شد؛ رزرو ایمن پرداخت ناموفق بود و پرداخت ارسال نشد.");
                }
            }
        }

        IsBusy = false;

        var queueNo = reception.Data;
        // پیام پایانی صادقانه — وضعیت واقعی هر سه حلقه (پذیرش/فاکتور/پرداخت)
        // چرخه سی‌ویکم: دیگر پس از شکست فاکتور/پرداخت، پیام «ثبت شد» چاپ نمی‌شود
        if (paymentConfirmedButRecoveryRetained)
            _snackbar.Info($"پذیرش و پرداخت ثبت شد — شناسه پذیرش {queueNo}؛ فایل بازیابی پرداخت در صندوق بررسی شود");
        else if (paymentFailed)
            _snackbar.Info($"پذیرش موفق — شناسه پذیرش {queueNo}؛ فاکتور صادر شد، وضعیت پرداخت را در صندوق بررسی کنید");
        else if (consumeResult is not null)
            _snackbar.Success($"پذیرش موفق — شناسه پذیرش {queueNo} (فاکتور شامل {consumeResult.Items?.Count ?? 0} قلم لوازم مصرفی ثبت شد)");
        else if (payment is not null)
            _snackbar.Success($"پذیرش موفق — شناسه پذیرش: {queueNo} (فاکتور و پرداخت {(payment.Method == 2 ? "کارتخوان" : "نقدی")} ثبت شد)");
        else
            _snackbar.Success($"پذیرش موفق — شناسه پذیرش: {queueNo} (فاکتور صادر شد)");

        if (paymentFailed)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Warning("زنجیره کامل تأیید نشد؛ درخواست پذیرش برای بررسی باقی ماند. ابتدا صف، فاکتور و پرداخت را بررسی کنید.");
            return;
        }
        try { _receptionRecoveryStore?.Clear(pendingReception); }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error($"پذیرش {reception.Data} و فاکتور ثبت شدند اما فایل بازیابی پاک نشد؛ در صندوق بررسی کنید.");
            return;
        }
        _pendingReception = null;
        OnPropertyChanged(nameof(ReceptionRecoveryInfo));
        OnPropertyChanged(nameof(CanRetryReception));

        // ═══ چرخهٔ ۸۲ (گزارش مالک) — چاپ قبض پذیرش پس از پرداخت ═══
        // «بعد از پذیرش و پرداخت و انتخاب روش پرداخت و تأیید، قبضی چاپ نمیشه؛
        // یک قبض کامل و شیک برای پرینتر حرارتی ۸ سانتی می‌خوام». قبض فقط وقتی
        // معنا دارد که هم فاکتور صادر شده و هم پرداخت واقعاً ثبت شده باشد؛ در
        // شکست فاکتور/پرداخت، چاپ قبضِ جعلی ممنوع است (همان قانون صداقت چرخهٔ ۳۱).
        // چاپ از دروازهٔ PrintFlow می‌گذرد، پس تنظیم «پیش‌نمایش قبل از چاپ» هم
        // رعایت می‌شود. خطای چاپ نباید زنجیرهٔ ثبت‌شده را زیر سؤال ببرد، لذا در
        // try/catch جدا و با پیام ملایم اعلام می‌شود.
        if (!paymentFailed && payment is not null && invoice?.Data is > 0)
        {
            try
            {
                // چاپ اولیه و چاپ مجدد از یک منبع مالی ذخیره‌شده استفاده می‌کنند؛
                // در صورت شکست دریافت، قبض حدسی یا دارای شناسه به جای شماره صادر نمی‌شود.
                var receipt = await LoadReceiptAsync(reception.Data.Value);
                if (receipt is not null) await PrintReceiptAsync(receipt);
                else _snackbar.Warning("پذیرش و پرداخت ثبت شد؛ داده قبض دریافت نشد، از چاپ مجدد استفاده کنید.");
            }
            catch (Exception ex)
            {
                // چاپ یک کار جانبی است — ثبت مالی انجام شده و نباید پیام خطا بدهد
                _snackbar.Info($"پذیرش ثبت شد؛ چاپ قبض انجام نشد: {ex.Message}");
            }
        }

        // ═══ پاک‌سازی فرم — چرخهٔ ۷۴: پاک‌سازی کامل پس از ثبت موفق ═══
        // گزارش مالک: «در پذیرش، بعد از پذیرش و پرداخت، هنوز اطلاعات بیمار در
        // فیلدهای پذیرش باقی میمونه». سه نشتی قبلی:
        //   ۱) PatientResults/SelectedSearchResult پاک نمی‌شد → با پاک‌شدن
        //      FoundPatient، «نتایج جستجوی بیمار قبلی» دوباره نمایان می‌شد؛
        //   ۲) SelectedDoctor پاک نمی‌شد → پزشکِ پذیرش قبل روی کشو می‌ماند؛
        //   ۳) بخش هزینه (VisitFee/سهم بیمه/بیمار) اعداد پذیرش قبل را نگه می‌داشت.
        SmartSearch = string.Empty;
        FoundPatient = null;
        FoundAppointmentInfo = null;
        PatientResults = new ObservableCollection<QuickPatient>();
        SelectedSearchResult = null;
        InsuranceNumber = string.Empty;
        ReferralCode = null;
        ReceptionNotes = null;
        Inquiry = null;
        InsuranceIndex = 0;
        SelectedDoctor = null;
        // چرخه ۲۴ — پاک‌سازی حالت خدمت (و خدمات انتخاب‌شدهٔ چندخدمتی)
        SelectedServices.Clear();
        ServiceSearchText = string.Empty;
        SelectedService = null;
        SelectedPerformer = null;
        // چرخهٔ ۳۹ — پاک‌سازی اتاق و اقلام مصرفی
        SelectedRoom = null;
        SelectedConsumable = null;
        ConsumableQty = 1;
        Consumables.Clear();
        RefreshConsumableTotals();
        // صفرکردن صریح بخش هزینه — بدون پزشک/خدمت، RecalculateCost مقادیر را
        // روی صفر می‌نشاند تا هیچ عددی از پذیرش قبلی روی صفحه نماند
        RecalculateCost();
        // چرخهٔ ۷۶ — ویزارد برای پذیرش بعدی به گام نخست بازمی‌گردد
        WizardStep = 0;

        await LoadAsync();
        }
        catch (Exception)
        {
            IsReceptionOutcomeUnknown = true;
            _snackbar.Error("پاسخ زنجیرهٔ پذیرش کامل دریافت نشد؛ پیش از تکرار، پذیرش، فاکتور و پرداخت را در صف و صندوق بررسی کنید.");
        }
        finally { IsBusy = false; IsSubmitting = false; workflowLease?.Dispose(); }
    }

    /// <summary>
    /// ساخت اقلام فاکتور پذیرش — چرخهٔ ۳۹: قلم خدمت/ویزیت + اقلام لوازم مصرفی
    /// با قیمت واقعی بچ‌ها (از نتیجهٔ مصرف) و پرچم «معاف از بیمه» (۱۰۰٪ بیمار).
    /// نام فیلدها مطابق قرارداد سرور: description + discountPercent (درس چرخهٔ ۳۱).
    /// </summary>
    /// <param name="isServiceMode">حالت پذیرش خدمتی؟</param>
    /// <param name="consumeResult">نتیجهٔ مصرف لوازم — تهی یعنی بدون مصرفی</param>
    private List<InvoiceItemDto> BuildInvoiceItems(bool isServiceMode, ConsumeResultDto? consumeResult)
    {
        var items = new List<InvoiceItemDto>();
        if (isServiceMode)
        {
            foreach (var line in SelectedServices)
                items.Add(new InvoiceItemDto(line.Service.Id, line.Service.Name, line.Quantity, line.Service.Price,
                    0, null, 0, SelectedPerformer?.Id));
        }
        else
        {
            items.Add(new InvoiceItemDto(null, $"ویزیت {SelectedDoctor!.Name}", 1m, VisitFee,
                0, SelectedDoctor.Id, 0));
        }
        foreach (var consumed in consumeResult?.Items ?? new List<ConsumeItemResultDto>())
            items.Add(new InvoiceItemDto(null, $"لوازم مصرفی: {consumed.Name}", consumed.Quantity, consumed.UnitPrice,
                0, null, 0, isServiceMode ? SelectedPerformer?.Id : null, true, consumed.MedicationId));
        return items;
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class QueueStatsResponse
    {
        public bool IsSuccess { get; set; }
        public QueueStatsData? Data { get; set; }
    }

    public sealed class QueueStatsData
    {
        public int WaitingCount { get; set; }
        public int ReceivedCount { get; set; }
        public int InQueue { get; set; }
        public decimal TodayRevenue { get; set; }
    }

    public sealed class ReceptionsResponse
    {
        public bool IsSuccess { get; set; }
        public List<ReceptionDto>? Data { get; set; }
    }

    public sealed class ReceptionDto
    {
        public long Id { get; set; }
        public string? ReceptionNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string TimeShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? InsuranceName { get; set; }
        public int? WaitingTime { get; set; }

        // چرخهٔ ۳۹ — اتاق محل انجام خدمت (پذیرش خدمتی؛ ویزیت پزشک تهی)
        public string? RoomName { get; set; }

        // چرخه سی‌ویکم — مبالغ مالی پذیرش (جمع فاکتورهای متصل + پرداخت‌شده)
        public decimal InvoiceTotal { get; set; }
        public decimal PaidAmount { get; set; }
    }

    /// <summary>پاکت پاسخ دادهٔ قبض پذیرش — چاپ مجدد</summary>
    public sealed class ReceptionReceiptResponse
    {
        public bool IsSuccess { get; set; }
        public ReceptionReceiptData? Data { get; set; }
    }

    /// <summary>دادهٔ قبض پذیرش از سرور — منطبق بر ReceptionReceiptDto</summary>
    public sealed class ReceptionReceiptData
    {
        public long ReceptionId { get; set; }
        public string? ReceptionNumber { get; set; }
        public string? InvoiceNumbers { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? NationalCode { get; set; }
        public string? Mobile { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? ServiceName { get; set; }
        public string? RoomName { get; set; }
        public string InsuranceName { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string TimeText { get; set; } = string.Empty;
        public List<ReceptionReceiptItemData> Items { get; set; } = new();
        public decimal ItemsTotal { get; set; }
        public decimal InsuranceShare { get; set; }
        public decimal PatientShare { get; set; }
        public decimal Payable { get; set; }
        public decimal Paid { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public int PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? CardNumber { get; set; }
    }

    /// <summary>قلم قبض — چاپ مجدد</summary>
    public sealed class ReceptionReceiptItemData
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class ReceptionCreateResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }

    public sealed class InquiryResponse
    {
        public bool IsSuccess { get; set; }
        public InquiryData? Data { get; set; }
    }

    public sealed class InquiryData
    {
        public long InquiryId { get; set; }
        public bool IsEligible { get; set; }
        public bool WasOnline { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ApprovalCode { get; set; }
        public decimal CoveragePercent { get; set; }
    }

    /// <summary>پرداخت خودکار تنها با مبلغ و هویت تأییدشدهٔ فاکتور مجاز است.</summary>
    public static bool CanAutomaticallyPay(ReceptionInvoiceDetail? detail, long invoiceId, long patientId, decimal? confirmedAmount) =>
        detail is not null && detail.Id == invoiceId && detail.PatientId == patientId &&
        detail.RemainingAmount is > 0m && detail.RemainingAmount == confirmedAmount;

    public sealed class ReceptionInvoiceEnvelope
    {
        public ReceptionInvoiceDetail? Data { get; set; }
    }

    public sealed class ReceptionInvoiceDetail
    {
        public long? Id { get; set; }
        public long? PatientId { get; set; }
        public decimal? RemainingAmount { get; set; }
    }

    public sealed class InvoicePreviewResponse
    {
        public InvoicePreviewDto? Data { get; set; }
    }

    public sealed class InvoiceCreateResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }

    public sealed class PaymentCreateResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }
}

/// <summary>
/// ردیف صف پذیرش — با رنگ‌بندی وضعیت
/// </summary>
/// <summary>ردیف خلاصهٔ نوبت امروز — پنل اختیاری «نوبت‌های امروز» در صفحهٔ پذیرش</summary>
public record MiniAppointmentRow(string Time, string PatientName, string DoctorName, string StatusText);

public record ReceptionRow(
    long Id,
    string? Number,
    string PatientName,
    string DoctorName,
    string Time,
    string StatusText,
    int StatusId,
    int ColorIndex,
    string? InsuranceName,
    string WaitingText,
    string StatusColor,
    // چرخهٔ ۳۹ — اتاق محل انجام خدمت (پذیرش خدمتی)
    string? RoomName,
    // چرخه سی‌ویکم — مبالغ مالی پذیرش برای نمایش در فهرست روز
    decimal InvoiceTotal,
    decimal PaidAmount,
    string FinancialText);
