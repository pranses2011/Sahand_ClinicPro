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
    /// <summary>عبارت جستجو — کد ملی یا کد پیگیری APT یا موبایل</summary>
    [ObservableProperty]
    private string _smartSearch = string.Empty;

    /// <summary>بیمار یافته‌شده</summary>
    [ObservableProperty]
    private QuickPatient? _foundPatient;

    /// <summary>نوبت فعال یافته‌شده — اگر بیمار نوبت داشته باشد</summary>
    [ObservableProperty]
    private string? _foundAppointmentInfo;

    /* ── فرم پذیرش ── */
    public ObservableCollection<DoctorOption> Doctors { get; } = new();
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
    }

    /// <summary>بارگذاری اولیه</summary>
    private async Task InitializeAsync()
    {
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
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
        new[] { "#FFFFFF", "#FFF8E1", "#E3F2FD", "#E8F5E9", "#ECEFF1", "#FFEBEE" }[Math.Clamp(r.Status - 1, 0, 5)]);

    /// <summary>متن وضعیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "پذیرش شده", 2 => "در صف", 3 => "در ویزیت", 4 => "ویزیت‌شده", 5 => "ترخیص‌شده", 6 => "لغوشده",
        _ => "نامشخص"
    };

    /* ═══════════ جستجوی هوشمند ═══════════ */

    /// <summary>جستجو — تشخیص خودکار نوع: کد ملی ۱۰رقمی / کد APT / موبایل</summary>
    [RelayCommand]
    private async Task SmartSearchAsync()
    {
        var term = SmartSearch.Trim();
        if (term.Length < 4) { _snackbar.Error("کد ملی، موبایل یا کد پیگیری نوبت را وارد کنید"); return; }

        IsBusy = true;
        FoundPatient = null;
        FoundAppointmentInfo = null;
        Inquiry = null;

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
            var patient = await FindPatientByNational(appt.PatientMobile ?? "");
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
            // جستجوی بیمار با کد ملی یا موبایل
            var patient = await FindPatientByNational(term);
            if (patient is null)
            {
                IsBusy = false;
                _snackbar.Error("بیماری با این مشخصه یافت نشد — ابتدا بیمار را ثبت کنید");
                return;
            }
            FoundPatient = patient;
        }

        IsBusy = false;
        _snackbar.Success($"بیمار یافت شد: {FoundPatient.FullName}");
    }

    /// <summary>یافتن بیمار با کد ملی یا موبایل</summary>
    private async Task<QuickPatient?> FindPatientByNational(string term)
    {
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term)}");
        var p = response?.Data?.FirstOrDefault(x =>
            x.NationalCode == term || x.Mobile == term);
        if (p is null) return null;
        return new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile,
            p.InsuranceName, p.Allergies, null);
    }

    /* ═══════════ استعلام بیمه ═══════════ */

    /// <summary>تغییر پزشک → محاسبه هزینه</summary>
    partial void OnSelectedDoctorChanged(DoctorOption? value) => RecalculateCost();

    /// <summary>تغییر بیمه → محاسبه هزینه</summary>
    partial void OnInsuranceIndexChanged(int value)
    {
        Inquiry = null;
        RecalculateCost();
    }

    /// <summary>استعلام بیمه — با Timeout ۳۰ ثانیه و پیام مناسب</summary>
    [RelayCommand]
    private async Task VerifyInsuranceAsync()
    {
        if (FoundPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        if (InsuranceIndex <= 0) { _snackbar.Error("ابتدا نوع بیمه را انتخاب کنید"); return; }

        var company = InsuranceCompanies[InsuranceIndex - 1];
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
    /// </summary>
    private void RecalculateCost()
    {
        // حق ویزیت واقعی پزشک انتخاب‌شده — صفر یعنی پزشک هنوز انتخاب نشده
        VisitFee = SelectedDoctor?.ConsultationFee ?? 0;

        // درصد پوشش: از استعلام یا صفر برای آزاد
        var percent = InsuranceIndex <= 0 ? 0 : CoveragePercent;
        InsuranceShare = Math.Round(VisitFee * percent / 100m);
        PatientShare = VisitFee - InsuranceShare;
        OnPropertyChanged(nameof(FeeDisplay));
        OnPropertyChanged(nameof(InsuranceShareDisplay));
        OnPropertyChanged(nameof(PatientShareDisplay));
    }

    /// <summary>حذف ارقام غیررقمی</summary>
    private static string CleanDigits(string text) => new(text.Where(char.IsDigit).ToArray());

    /* ═══════════ ثبت پذیرش ═══════════ */

    /// <summary>اعتبارسنجی مشترک</summary>
    private bool ValidateReception()
    {
        if (FoundPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return false; }
        if (SelectedDoctor is null) { _snackbar.Error("ابتدا پزشک را انتخاب کنید"); return false; }
        return true;
    }

    /// <summary>«پذیرش و فاکتور» — ثبت پذیرش + صدور فاکتور بدون پرداخت</summary>
    [RelayCommand]
    private async Task ReceptionAndInvoiceAsync() => await SubmitReceptionAsync(withPayment: false);

    /// <summary>«پذیرش و پرداخت نقدی» — ثبت پذیرش + فاکتور + پرداخت نقدی</summary>
    [RelayCommand]
    private async Task ReceptionAndPayAsync() => await SubmitReceptionAsync(withPayment: true);

    /// <summary>اجرای زنجیره پذیرش طبق فرم ۱۰ — پذیرش + فاکتور + پرداخت اختیاری</summary>
    private async Task SubmitReceptionAsync(bool withPayment)
    {
        if (!ValidateReception()) return;

        var company = InsuranceIndex > 0 ? InsuranceCompanies[InsuranceIndex - 1] : null;

        IsBusy = true;

        // ۱. ثبت پذیرش
        var reception = await _api.PostAsync<ReceptionCreateResponse>("receptions", new
        {
            patientId = FoundPatient!.Id,
            doctorId = SelectedDoctor!.Id,
            appointmentId = (long?)null,
            insuranceCompanyId = company?.Id,
            referralCode = string.IsNullOrWhiteSpace(ReferralCode) ? null : ReferralCode.Trim(),
            notes = string.IsNullOrWhiteSpace(ReceptionNotes) ? null : ReceptionNotes.Trim()
        });
        if (reception?.Data is null)
        {
            IsBusy = false;
            _snackbar.Error(_api.LastError ?? "ثبت پذیرش ناموفق بود");
            return;
        }

        // ۲. صدور فاکتور خودکار — قلم ویزیت با کد خدمت K
        var invoice = await _api.PostAsync<InvoiceCreateResponse>("invoices", new
        {
            patientId = FoundPatient.Id,
            receptionId = reception.Data,
            visitId = (long?)null,
            type = 1,
            insuranceCompanyId = company?.Id,
            taxPercent = 0m,
            items = new List<object>
            {
                new { serviceId = (long?)null, serviceName = $"ویزیت {SelectedDoctor.Name}", quantity = 1, unitPrice = VisitFee, discount = 0m }
            },
            description = $"پذیرش {FoundPatient.FullName}"
        });

        // ۳. پرداخت نقدی (اختیاری)
        if (withPayment && invoice?.Data is not null && invoice.Data > 0)
        {
            var payment = await _api.PostAsync<PaymentCreateResponse>("payments", new
            {
                invoiceId = invoice.Data,
                patientId = FoundPatient.Id,
                amount = PatientShare,
                method = 1,
                referenceNumber = (string?)null,
                cardNumber = (string?)null
            });
            if (payment?.Data is null)
                _snackbar.Info("پرداخت ثبت نشد — از ماژول مالی تکمیل کنید");
        }

        IsBusy = false;

        var queueNo = reception.Data;
        _snackbar.Success(withPayment
            ? $"پذیرش موفق — شماره صف: {queueNo} (فاکتور و پرداخت ثبت شد)"
            : $"پذیرش موفق — شماره صف: {queueNo} (فاکتور صادر شد)");

        // پاک‌سازی فرم
        SmartSearch = string.Empty;
        FoundPatient = null;
        FoundAppointmentInfo = null;
        InsuranceNumber = string.Empty;
        ReferralCode = null;
        ReceptionNotes = null;
        Inquiry = null;
        InsuranceIndex = 0;

        await LoadAsync();
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
    string StatusColor);
