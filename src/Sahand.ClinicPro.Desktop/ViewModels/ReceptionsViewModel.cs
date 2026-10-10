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

    /* ── جستجوی هوشمند ── */
    /// <summary>عبارت جستجو — نام/نام‌خانوادگی/کد ملی/کد پیگیری APT/موبایل</summary>
    [ObservableProperty]
    private string _smartSearch = string.Empty;

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

    /// <summary>نوبت فعال یافته‌شده — اگر بیمار نوبت داشته باشد</summary>
    [ObservableProperty]
    private string? _foundAppointmentInfo;

    /* ── فرم پذیرش ── */
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
    partial void OnReceptionModeIndexChanged(int value) => RecalculateCost();

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
        decimal TariffInsuranceShare = 0m, decimal TariffPatientShare = 100m);

    /// <summary>گزینهٔ پرسنل انجام‌دهنده</summary>
    public record PerformerOption(long Id, string FullName);
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>پزشک انتخاب‌شده</summary>
    [ObservableProperty]
    private DoctorOption? _selectedDoctor;

    /// <summary>نمایه بیمه — صفر یعنی آزاد</summary>
    [ObservableProperty]
    private int _insuranceIndex;

    /// <summary>شماره بیمه</summary>
    [ObservableProperty]
    private string _insuranceNumber = string.Empty;

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

    /// <summary>پذیرش انتخاب‌شده در صف</summary>
    [ObservableProperty]
    private ReceptionRow? _selectedReception;

    /// <summary>سازنده</summary>
    public ReceptionsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
        // چرخه ۲۴: خدمات تعرفه‌دار + پرسنل برای «پذیرش خدمت درمانی»
        _ = LoadServicesAndPerformersAsync();
    }

    /// <summary>بارگذاری اولیه</summary>
    private async Task InitializeAsync()
    {
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): «نوع بیمه گزینهٔ آزاد وجود ندارد و
        // هنوز بیمهٔ آسیا به‌عنوان آزاد ثبت می‌شود» — ریشهٔ باگ: کد، نمایهٔ صفرِ
        // کشو را «آزاد/بدون بیمه» تفسیر می‌کرد (InsuranceIndex <= 0) ولی کشو هیچ
        // آیتم آزادی نداشت؛ یعنی نخستین شرکت سرور (مثلاً آسیا) انتخاب می‌شد و
        // هم‌زمان چون نمایه‌اش صفر بود به‌عنوان «بدون بیمه» ثبت می‌شد — هم گزینهٔ
        // آزاد وجود نداشت هم بیمهٔ انتخابی قعر می‌رفت. رفع ساختاری: آیتم صریح
        // «آزاد (بدون بیمه)» با شناسهٔ صفر در ابتدای کشو درج می‌شود تا نمایهٔ صفر
        // واقعاً آزاد باشد و شرکت‌ها از نمایهٔ ۱ به بعد دقیق ثبت شوند (هم‌الگوی
        // پنجرهٔ فرم پذیرش — ReceptionFormViewModel).
        InsuranceCompanies.Add(new InsuranceOption(0, "آزاد (بدون بیمه)"));
        if (companies?.Data is not null)
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));

        await LoadAsync();
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
            Receptions = new ObservableCollection<ReceptionRow>(response.Data.Select(MapRow));
        else
            ErrorMessage = _api.LastError ?? string.Empty;

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

    /// <summary>متن وضعیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "پذیرش شده", 2 => "در صف", 3 => "در ویزیت", 4 => "ویزیت‌شده", 5 => "ترخیص‌شده", 6 => "لغوشده",
        _ => "نامشخص"
    };

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
        var term = SmartSearch.Trim();
        if (term.Length < 2) { _snackbar.Error("بخشی از نام، کد ملی، موبایل یا کد پیگیری نوبت را وارد کنید"); return; }

        IsBusy = true;
        FoundPatient = null;
        FoundAppointmentInfo = null;
        Inquiry = null;
        SelectedSearchResult = null;

        if (term.StartsWith("APT", StringComparison.OrdinalIgnoreCase))
        {
            // جستجو با کد پیگیری نوبت
            var appts = await _api.PostAsync<AppointmentsViewModel.SearchResponse>("appointments/search", new
            {
                search = term, pageNumber = 1, pageSize = 5
            });
            var appt = appts?.Data?.FirstOrDefault();
            if (appt is null)
            {
                IsBusy = false;
                _snackbar.Error("نوبتی با این کد پیگیری یافت نشد");
                return;
            }
            // بیمارِ نوبت با موبایل (تطابق دقیق — موبایل نوبت داریم)
            var patient = (await FindPatientsAsync(appt.PatientMobile ?? ""))
                .FirstOrDefault(x => x.Mobile == appt.PatientMobile);
            if (patient is null)
            {
                IsBusy = false;
                _snackbar.Error("بیمار نوبت یافت نشد");
                return;
            }
            FoundPatient = patient;
            FoundAppointmentInfo = $"نوبت {appt.DateShamsi} ساعت {appt.Time} — {appt.DoctorName}";
            var doctor = Doctors.FirstOrDefault(d => d.Name == appt.DoctorName);
            if (doctor is not null) SelectedDoctor = doctor;
        }
        else
        {
            // جستجوی بیمار — تطبیق جزئی نام/کد ملی/موبایل/پرونده از سمت سرور
            var results = await FindPatientsAsync(term);

            switch (results.Count)
            {
                case 0:
                    _snackbar.Error("بیماری یافت نشد — بخشی از نام یا کد ملی را امتحان کنید یا ابتدا بیمار را ثبت کنید");
                    break;

                case 1:
                    // تک‌نتیجه — انتخاب خودکار بدون نیاز به کلیک اضافه
                    FoundPatient = results[0];
                    _snackbar.Success($"بیمار یافت شد: {FoundPatient.FullName}");
                    break;

                default:
                    // چندنتیجه — فهرست برای انتخاب کاربر (گزارش شانزدهم مالک:
                    // «جستجو موارد رو بیاره تا انتخاب بکنیم»)
                    PatientResults = new ObservableCollection<QuickPatient>(results);
                    _snackbar.Info($"{results.Count} بیمار یافت شد — از فهرست انتخاب کنید");
                    break;
            }
        }

        IsBusy = false;
    }

    /// <summary>
    /// یافتن بیماران با تطبیق جزئی — چرخه بیست‌وسوم: دیگر با شرط «کد ملی == دقیق»
    /// دوربرداشته نمی‌شود؛ سرور خودش Contains روی نام/کدملی/موبایل/پرونده و
    /// مقایسهٔ فشردهٔ نیم‌فاصله انجام می‌دهد
    /// </summary>
    private async Task<List<QuickPatient>> FindPatientsAsync(string term)
    {
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term)}");
        if (response?.Data is null) return new List<QuickPatient>();

        return response.Data
            .Select(p => new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile,
                p.InsuranceName, p.Allergies, null))
            .ToList();
    }

    /* ═══════════ استعلام بیمه ═══════════ */

    /// <summary>تغییر پزشک → محاسبه هزینه</summary>
    partial void OnSelectedDoctorChanged(DoctorOption? value) => RecalculateCost();

    /// <summary>
    /// بارگذاری خدمات تعرفه‌دار و پرسنل — چرخه ۲۴: برای پذیرش خدمت درمانی.
    /// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ «تعرفه خدمات رو نمیاره؛
    /// باید با فیلتر بیمه و خدمات، تعرفهٔ اون خدمات رو بیاره» — تعرفه‌ها
    /// اکنون به‌ازای بیمهٔ منتخب از سرور خوانده می‌شود (تعرفهٔ بیمه → در
    /// نبود، تعرفهٔ آزاد → در نبود، قیمت پایه) و با هر تغییر بیمه/حالت دوباره
    /// بارگذاری می‌شود؛ در حالت آزاد تعرفهٔ آزاد (سهم بیمار ۱۰۰٪) می‌آید.
    /// </summary>
    /// <param name="insuranceCompanyId">شناسهٔ بیمهٔ منتخب — تهی = آزاد</param>
    private async Task LoadServicesAndPerformersAsync(long? insuranceCompanyId = null)
    {
        // خدمات فعال با تعرفهٔ هدفِ منتخب — سهم‌های بیمه/بیمار همراه قیمت می‌آید
        var url = $"lookups/services?search=&insuranceCompanyId={insuranceCompanyId?.ToString() ?? ""}";
        var services = await _api.GetAsync<ServicesLookupResponse>(url);
        Services.Clear();
        foreach (var s2 in services?.Data ?? new List<ServiceLookupDto>())
            Services.Add(new ServiceOption(s2.ServiceId, s2.ServiceName, s2.Price,
                s2.InsuranceShare, s2.PatientShare));

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
                    !string.IsNullOrWhiteSpace(room.RoomName) ? room.RoomName : $"اتاق {room.RoomNumber}"));
        }

        // ═══ چرخهٔ ۳۹/۴۱ — اقلام مصرفیِ دارای موجودی؛ فقط یک بار لازم است ═══
        // فهرست خالی به معنی «هیچ قلم مصرفی‌ای هنوز ورود کالا نشده» است؛ بخش
        // مصرفی همان‌جا راهنمای «از صفحهٔ لوازم مصرفی ورود کالا» نشان می‌دهد.
        // چرخهٔ ۴۱: مسیر از pharmacy/consumables به consumables (مستقل از لایسنس
        // داروخانه) منتقل شد تا درمانگاهِ بدون آن ماژول هم مصرفی‌ها را ببیند.
        if (ConsumableOptions.Count == 0)
        {
            var consumables = await _api.GetAsync<ConsumablesResponse>("consumables");
            foreach (var c in consumables?.Data ?? new List<ConsumableDto>())
                ConsumableOptions.Add(new ConsumableOption(c.MedicationId, c.Name, c.Form, c.TotalQuantity, c.SellPrice));
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

    /// <summary>ردیف اتاق فعال</summary>
    private sealed class RoomLookupDto
    {
        public long Id { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomName { get; set; }
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

    /// <summary>تغییر بیمه → محاسبه هزینه + بارگذاری مجدد تعرفه‌ها با بیمهٔ جدید</summary>
    partial void OnInsuranceIndexChanged(int value)
    {
        Inquiry = null;
        RecalculateCost();

        // ═══ چرخهٔ بیست‌وششم ═══ تعرفه‌ها بیمه‌محورند — با تغییر بیمه، فهرست
        // خدمات با تعرفهٔ همان بیمه بازخوانی می‌شود و خدمتِ قبلی هم اگر هنوز
        // موجود باشد دوباره انتخاب می‌شود تا قیمت‌ها بی‌درنگ درست بمانند.
        // (نمایهٔ صفر = آزاد → بیمهٔ تهی به سرور می‌رود)
        var selectedCompany = value >= 0 && value < InsuranceCompanies.Count
            ? InsuranceCompanies[value]
            : null;
        var companyId = selectedCompany is { Id: > 0 } ? selectedCompany.Id : (long?)null;
        var previousServiceName = SelectedService?.Name;
        _ = LoadServicesAndPerformersAsync(companyId).ContinueWith(_ =>
        {
            if (previousServiceName is null) return;
            var same = Services.FirstOrDefault(s => s.Name == previousServiceName);
            if (same is not null)
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => SelectedService = same);
        });
    }

    /// <summary>استعلام بیمه — با Timeout ۳۰ ثانیه و پیام مناسب</summary>
    [RelayCommand]
    private async Task VerifyInsuranceAsync()
    {
        if (FoundPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        if (InsuranceIndex <= 0) { _snackbar.Error("ابتدا نوع بیمه را انتخاب کنید"); return; }

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع آف‌بای‌وان ═══
        // آیتم «آزاد» با شناسهٔ صفر در ابتدای کشوست و شرکت‌های واقعی از
        // نمایهٔ ۱؛ خواندن با «نمایهٔ منهای یک» شرکت بعدی را می‌داد و بیمهٔ
        // انتخابی بیمار در پذیرش قعر می‌رفت (ریشهٔ «تامین اجتماعی زدم ولی
        // آزاد ثبت می‌شود» در صف پذیرش)؛ اکنون خودِ نمایه خوانده می‌شود
        var company = InsuranceCompanies[InsuranceIndex];
        IsInquiring = true;
        try
        {
            var response = await _api.PostAsync<InquiryResponse>("insurance/inquiries", new
            {
                patientId = FoundPatient.Id,
                insuranceCompanyId = company.Id,
                insuranceNumber = string.IsNullOrWhiteSpace(InsuranceNumber) ? null : InsuranceNumber.Trim()
            });

            if (response?.Data is null)
            {
                Inquiry = new InquiryState(true, false, _api.LastError ?? "بیمه نامعتبر است", null, 0);
                _snackbar.Error("بیمه نامعتبر: " + Inquiry.Message);
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
            // Timeout — «سامانه بیمه پاسخ نداد - ادامه بصورت آزاد؟»
            var proceed = Views.AppDialog.Confirm("عدم پاسخ سامانه بیمه", "سامانه بیمه پاسخ نداد. پذیرش به‌صورت آزاد ادامه یابد؟", Views.AppDialogIcon.Warning) == true;
            if (proceed)
            {
                InsuranceIndex = 0;
                Inquiry = new InquiryState(true, false, "استعلام ناموفق — پذیرش آزاد", null, 0);
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
        var isFree = selectedCompany is not { Id: > 0 };
        // حالت نمایشی بخش هزینه — برای آزاد، ستون سهم بیمه پنهان می‌شود
        HasInsurance = !isFree;

        if (ReceptionModeIndex == 1)
        {
            // ═══ حالت خدمت درمانی — تعرفهٔ خدمت منتخب ═══
            // قیمت = تعرفهٔ خدمت برای بیمهٔ منتخب؛ سهم‌ها از درصدهای تعرفهٔ همان
            // خدمت — مگر در حالت آزاد که کل مبلغ سهم بیمار است (۰٪ بیمه)
            VisitFee = SelectedService?.Price ?? 0;
            var insurancePercent = !isFree ? SelectedService?.TariffInsuranceShare ?? 0m : 0m;
            InsuranceShare = Math.Round(VisitFee * insurancePercent / 100m);
            PatientShare = VisitFee - InsuranceShare;
        }
        else
        {
            // ═══ حالت ویزیت پزشک — رفتار پیشین ═══
            // حق ویزیت واقعی پزشک انتخاب‌شده — صفر یعنی پزشک هنوز انتخاب نشده
            VisitFee = SelectedDoctor?.ConsultationFee ?? 0;

            // درصد پوشش: از استعلام یا صفر برای آزاد
            var percent = isFree ? 0 : CoveragePercent;
            InsuranceShare = Math.Round(VisitFee * percent / 100m);
            PatientShare = VisitFee - InsuranceShare;
        }

        OnPropertyChanged(nameof(FeeDisplay));
        OnPropertyChanged(nameof(InsuranceShareDisplay));
        OnPropertyChanged(nameof(PatientShareDisplay));
        // چرخهٔ ۳۹ — سهم بیمار تغییر کرد ⇒ قابل پرداخت نهایی (با مصرفی‌ها) هم باید تازه شود
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
        if (ReceptionModeIndex == 1)
        {
            if (SelectedService is null) { _snackbar.Error("ابتدا خدمت درمانی را انتخاب کنید"); return false; }
        }
        else
        {
            if (SelectedDoctor is null) { _snackbar.Error("ابتدا پزشک را انتخاب کنید"); return false; }
        }
        return true;
    }

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
        if (!ValidateReception()) return;

        // مبلغ پیش‌بینی‌شده = سهم بیمار خدمت + جمع لوازم مصرفی (قیمت پیشنهادیِ
        // بچ پرموجودی؛ مبلغ نهایی پس از مصرف واقعی از بچ‌ها در زنجیره محاسبه می‌شود)
        var payVm = new ReceptionPayViewModel(PayableByPatientDisplay);
        var payWindow = new Views.ReceptionPayWindow(payVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        payWindow.ShowDialog();

        // انصراف — هیچ چیزی ثبت نمی‌شود
        if (payVm.Result is null) return;

        await SubmitReceptionAsync(payVm.Result);
    }

    /// <summary>
    /// اجرای زنجیره پذیرش طبق فرم ۱۰ — پذیرش + مصرف لوازم + فاکتور + پرداخت اختیاری
    /// </summary>
    /// <param name="payment">اطلاعات پرداخت منتخب از پنجرهٔ پرداخت — تهی یعنی بدون پرداخت (فقط فاکتور)</param>
    private async Task SubmitReceptionAsync(ReceptionPaymentInfo? payment)
    {
        if (!ValidateReception()) return;

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

        // ۱. ثبت پذیرش
        var reception = await _api.PostAsync<ReceptionCreateResponse>("receptions", new
        {
            patientId = FoundPatient!.Id,
            doctorId = isServiceMode ? (long?)null : SelectedDoctor!.Id,
            appointmentId = (long?)null,
            insuranceCompanyId = company?.Id,
            referralCode = string.IsNullOrWhiteSpace(ReferralCode) ? null : ReferralCode.Trim(),
            notes = string.IsNullOrWhiteSpace(ReceptionNotes) ? null : ReceptionNotes.Trim(),
            // حالت خدمت درمانی — خدمت + انجام‌دهندهٔ اختیاری از پرسنل
            serviceId = isServiceMode ? SelectedService!.Id : (long?)null,
            serviceName = isServiceMode ? SelectedService!.Name : null,
            performedByEmployeeId = isServiceMode ? SelectedPerformer?.Id : (long?)null,
            // چرخهٔ ۳۹ — اتاق محل انجام خدمت؛ فقط پذیرش خدمتی (ویزیت پزشک: اتاق از
            // برنامهٔ کاری خود پزشک می‌آید و ارسال تهی معنای درست دارد)
            roomId = isServiceMode ? SelectedRoom?.Id : (long?)null
        });
        if (reception?.Data is null)
        {
            IsBusy = false;
            _snackbar.Error(_api.LastError ?? "ثبت پذیرش ناموفق بود");
            return;
        }

        // ═══ ۱/۵. ثبت مصرف لوازم درمانگاه — چرخهٔ ۳۹ (فقط پذیرش خدمتی با اقلام مصرفی) ═══
        // «وقتی بیمار با خودش نمیاره باید از لوازم خود درمانگاه استفاده بکنیم و
        // در پذیرش برای اونها هم هزینه به هزینه‌های بیمار اضافه بشه» — پیش از صدور
        // فاکتور، موجودی FEFO کم می‌شود و قیمت واقعی بچ‌ها برای اقلام فاکتور
        // برمی‌گردد؛ شکست مصرف (مثلاً کمبود هم‌زمان موجودی) با دیالوگ فارسی و
        // حق انتخاب «ادامه بدون مصرفی» یا توقف صادقانه اعلام می‌شود — پذیرش
        // ثبت‌شده باقی می‌ماند تا زنجیره دستی تکمیل شود.
        ConsumeResultDto? consumeResult = null;
        var consumablesSkipped = false;
        if (isServiceMode && Consumables.Count > 0)
        {
            var consume = await _api.PostAsync<ConsumeResponse>("consumables/consume", new
            {
                receptionId = reception.Data,
                items = Consumables.Select(c => new { medicationId = c.Option.Id, quantity = c.Quantity }).ToList()
            });
            if (consume?.Data is null)
            {
                IsBusy = false;
                var proceed = Views.AppDialog.Confirm("ثبت مصرف لوازم ناموفق",
                    $"پذیرش با شماره صف {reception.Data} ثبت شد اما برداشت لوازم مصرفی از انبار شکست خورد:{Environment.NewLine}{_api.LastError ?? "خطای ناشناخته سرور"}{Environment.NewLine}{Environment.NewLine}ادامه می‌دهید؟ (فاکتور فقط با هزینهٔ خدمت صادر می‌شود و اقلام مصرفی از انبار کم نشدند)", Views.AppDialogIcon.Warning) == true;
                if (!proceed)
                {
                    _snackbar.Info($"پذیرش ثبت شد — شماره صف {reception.Data}؛ فاکتور/پرداخت متوقف شد تا مصرف لوازم اصلاح شود (اقلام را کم کنید یا از صفحهٔ «لوازم مصرفی» ورود کالا ثبت کنید)");
                    await LoadAsync();
                    return;
                }
                consumablesSkipped = true;
            }
            else
            {
                consumeResult = consume.Data;
            }
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
        var invoiceFailed = false; // وضعیت صدور فاکتور برای پیام پایانی صادقانه
        var invoice = await _api.PostAsync<InvoiceCreateResponse>("invoices", new
        {
            patientId = FoundPatient.Id,
            receptionId = reception.Data,
            visitId = (long?)null,
            type = 1,
            insuranceCompanyId = company?.Id,
            taxPercent = 0m,
            items = BuildInvoiceItems(isServiceMode, consumeResult),
            description = isServiceMode
                ? $"پذیرش خدمت {SelectedService!.Name} — {FoundPatient.FullName}"
                : $"پذیرش {FoundPatient.FullName}"
        });
        if (invoice?.Data is null || invoice.Data <= 0)
        {
            // پذیرش ثبت شده ولی فاکتور نه — از ماژول «صندوق و فاکتور» قابل تکمیل است؛
            // خطای واقعی سرور (LastError) نمایش داده می‌شود تا ریشه مشخص باشد
            invoiceFailed = true;
            IsBusy = false;
            Views.AppDialog.Error("صدور فاکتور ناموفق",
                $"پذیرش با شماره صف {reception.Data} ثبت شد اما صدور فاکتور شکست خورد:{Environment.NewLine}{_api.LastError ?? "خطای ناشناخته سرور"}{Environment.NewLine}{Environment.NewLine}فاکتور را بعداً از ماژول «صندوق و فاکتور» صادر کنید.");
        }

        // ۳. پرداخت (اختیاری) — روش و مرجع از پنجرهٔ پرداخت پذیرش (چرخهٔ ۳۹)
        var paymentFailed = false;
        if (payment is not null && invoice?.Data is not null && invoice.Data > 0)
        {
            // چرخهٔ ۳۹ — مبلغ قابل پرداخت بیمار = سهم بیمار خدمت + جمع لوازم مصرفی
            // (قیمت واقعی بچ‌ها از مصرف؛ اگر مصرف رد شده و کاربر ادامه داد، فقط خدمت)
            var payable = PatientShare + (consumeResult?.TotalCost ?? 0m);
            var paymentResult = await _api.PostAsync<PaymentCreateResponse>("payments", new
            {
                invoiceId = invoice.Data,
                patientId = FoundPatient.Id,
                amount = payable,
                // روش و مرجع از پنجرهٔ پرداخت — ۱ نقدی، ۲ کارتخوان (چرخهٔ ۳۹)
                method = payment.Method,
                referenceNumber = payment.ReferenceNumber,
                cardNumber = payment.CardNumber
            });
            if (paymentResult?.Data is null || paymentResult.Data <= 0)
            {
                // پرداخت ثبت نشد — کاربر باید بداند؛ فاکتور بدهکار مانده است
                paymentFailed = true;
                IsBusy = false;
                Views.AppDialog.Error("ثبت پرداخت ناموفق",
                    $"فاکتور صادر شد اما ثبت پرداخت {payable:N0} ریال شکست خورد:{Environment.NewLine}{_api.LastError ?? "خطای ناشناخته سرور"}{Environment.NewLine}{Environment.NewLine}پرداخت را بعداً از ماژول «صندوق و فاکتور» تکمیل کنید.");
            }
        }

        IsBusy = false;

        var queueNo = reception.Data;
        // پیام پایانی صادقانه — وضعیت واقعی هر سه حلقه (پذیرش/فاکتور/پرداخت)
        // چرخه سی‌ویکم: دیگر پس از شکست فاکتور/پرداخت، پیام «ثبت شد» چاپ نمی‌شود
        if (invoiceFailed)
            _snackbar.Info($"پذیرش ثبت شد — شماره صف {queueNo}؛ صدور فاکتور ناموفق بود (از ماژول مالی تکمیل کنید)");
        else if (paymentFailed)
            _snackbar.Info($"پذیرش موفق — شماره صف {queueNo}؛ فاکتور صادر شد اما پرداخت ثبت نشد");
        else if (consumablesSkipped)
            _snackbar.Info($"پذیرش موفق — شماره صف {queueNo}؛ فاکتور فقط با هزینهٔ خدمت صادر شد (مصرف لوازم ثبت نشد)");
        else if (consumeResult is not null)
            _snackbar.Success($"پذیرش موفق — شماره صف {queueNo} (فاکتور شامل {consumeResult.Items?.Count ?? 0} قلم لوازم مصرفی ثبت شد)");
        else if (payment is not null)
            _snackbar.Success($"پذیرش موفق — شماره صف: {queueNo} (فاکتور و پرداخت {(payment.Method == 2 ? "کارتخوان" : "نقدی")} ثبت شد)");
        else
            _snackbar.Success($"پذیرش موفق — شماره صف: {queueNo} (فاکتور صادر شد)");

        // پاک‌سازی فرم
        SmartSearch = string.Empty;
        FoundPatient = null;
        FoundAppointmentInfo = null;
        InsuranceNumber = string.Empty;
        ReferralCode = null;
        ReceptionNotes = null;
        Inquiry = null;
        InsuranceIndex = 0;
        // چرخه ۲۴ — پاک‌سازی حالت خدمت
        SelectedService = null;
        SelectedPerformer = null;
        // چرخهٔ ۳۹ — پاک‌سازی اتاق و اقلام مصرفی
        SelectedRoom = null;
        SelectedConsumable = null;
        ConsumableQty = 1;
        Consumables.Clear();
        RefreshConsumableTotals();

        await LoadAsync();
    }

    /// <summary>
    /// ساخت اقلام فاکتور پذیرش — چرخهٔ ۳۹: قلم خدمت/ویزیت + اقلام لوازم مصرفی
    /// با قیمت واقعی بچ‌ها (از نتیجهٔ مصرف) و پرچم «معاف از بیمه» (۱۰۰٪ بیمار).
    /// نام فیلدها مطابق قرارداد سرور: description + discountPercent (درس چرخهٔ ۳۱).
    /// </summary>
    /// <param name="isServiceMode">حالت پذیرش خدمتی؟</param>
    /// <param name="consumeResult">نتیجهٔ مصرف لوازم — تهی یعنی بدون مصرفی</param>
    private List<object> BuildInvoiceItems(bool isServiceMode, ConsumeResultDto? consumeResult)
    {
        var items = new List<object>
        {
            // چرخه ۲۴ — قلم فاکتور: در حالت خدمتی، خودِ خدمت با قیمت تعرفه؛
            // در حالت ویزیت، ویزیت پزشک (رفتار پیشین)
            isServiceMode
                ? new { serviceId = (long?)SelectedService!.Id, description = SelectedService.Name, quantity = 1, unitPrice = SelectedService.Price, discountPercent = 0m }
                : new { serviceId = (long?)null, description = $"ویزیت {SelectedDoctor!.Name}", quantity = 1, unitPrice = VisitFee, discountPercent = 0m }
        };

        // چرخهٔ ۳۹ — اقلام مصرفی با قیمت واقعی برداشت از بچ‌ها؛ معاف از بیمه
        foreach (var consumed in consumeResult?.Items ?? new List<ConsumeItemResultDto>())
        {
            items.Add(new
            {
                serviceId = (long?)null,
                description = $"لوازم مصرفی: {consumed.Name}",
                quantity = consumed.Quantity,
                unitPrice = consumed.UnitPrice,
                discountPercent = 0m,
                isInsuranceExempt = true
            });
        }

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
