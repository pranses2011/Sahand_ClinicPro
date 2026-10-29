using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف فاکتور اخیر</summary>
public record InvoiceRow(
    long Id,
    string InvoiceNumber,
    string DateShamsi,
    string PatientName,
    decimal Total,
    decimal Paid,
    string PaymentStatusText,
    string PaymentStatusColor);

/// <summary>تراکنش صندوق امروز</summary>
public record CashRow(
    long Id, string? Number, string Time, long? InvoiceId,
    string PatientName, decimal Amount, string MethodText);

/// <summary>خدمت قابل افزودن به فاکتور</summary>
public record ServiceOption(
    long ServiceId, string? ServiceCode, string ServiceName,
    decimal Price, decimal InsuranceShare, decimal PatientShare);

/// <summary>قلم فاکتور قابل ویرایش</summary>
public partial class InvoiceItemVM : ObservableObject
{
    public long? ServiceId { get; init; }
    public string Code { get; init; } = "—";
    public string Name { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }

    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _insuranceShare;
    [ObservableProperty] private decimal _patientShare;

    /// <summary>پرسنل انجام‌دهنده این خدمت — مبنای حقوق درصدی از سود (چرخه نهم)</summary>
    [ObservableProperty]
    private PerformerOption? _performer;

    public decimal Total => Quantity * UnitPrice;
    public decimal TotalInsurance => Quantity * InsuranceShare;
    public decimal TotalPatient => Quantity * PatientShare;
    partial void OnQuantityChanged(int value) => RaiseTotals();
    partial void OnUnitPriceChanged(decimal value) => RaiseTotals();
    partial void OnInsuranceShareChanged(decimal value) => RaiseTotals();
    partial void OnPatientShareChanged(decimal value) => RaiseTotals();
    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalInsurance));
        OnPropertyChanged(nameof(TotalPatient));
    }
}

/// <summary>گزینه پرسنل انجام‌دهنده خدمت</summary>
public record PerformerOption(long Id, string FullName);

/// <summary>
/// ViewModel مالی و صندوق — گام U8 طبق پرامپت UI بخش ۳ (فرم ۱۴ و ۱۵):
/// کارت‌های آماری صندوق + فرم صدور فاکتور با اقلام K و پنل خلاصه +
/// روش‌های پرداخت + فاکتورهای اخیر + تراکنش‌های امروز
/// </summary>
public partial class BillingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان</summary>
    public override string Title => "مالی و صندوق";

    /* ── کارت‌های صندوق ── */
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private string _cashTotalDisplay = "۰";
    [ObservableProperty] private string _cardTotalDisplay = "۰";
    [ObservableProperty] private string _netTotalDisplay = "۰";
    [ObservableProperty] private decimal _cashTotal;
    [ObservableProperty] private decimal _cardTotal;
    [ObservableProperty] private decimal _netTotal;

    /* ── فاکتورهای اخیر ── */
    [ObservableProperty]
    private ObservableCollection<InvoiceRow> _invoices = new();

    [ObservableProperty]
    private InvoiceRow? _selectedInvoice;

    /* ── تراکنش‌های امروز ── */
    public ObservableCollection<CashRow> Transactions { get; } = new();

    /// <summary>سازنده</summary>
    public BillingViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری کارت‌ها + فاکتورها + تراکنش‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var summary = await _api.GetAsync<SummaryResponse>("payments/today-summary");
        if (summary?.Data is not null)
        {
            InvoiceCount = summary.Data.InvoiceCount;
            CashTotal = summary.Data.CashTotal;
            CardTotal = summary.Data.CardTotal;
            NetTotal = summary.Data.NetTotal;
            CashTotalDisplay = Format(CashTotal);
            CardTotalDisplay = Format(CardTotal);
            NetTotalDisplay = Format(NetTotal);
        }

        var invoices = await _api.GetAsync<InvoicesResponse>("invoices?page=1&pageSize=15");
        if (invoices?.Data is not null)
            Invoices = new ObservableCollection<InvoiceRow>(invoices.Data.Select(MapInvoice));

        var tx = await _api.GetAsync<TransactionsResponse>("payments/today");
        Transactions.Clear();
        if (tx?.Data is not null)
            foreach (var t in tx.Data)
                Transactions.Add(new CashRow(t.PaymentId, t.PaymentNumber, t.Time,
                    t.InvoiceId, t.PatientName, t.Amount, t.MethodText));

        IsBusy = false;
    }

    /// <summary>قالب‌بندی فشرده مبالغ — ۳.۸M</summary>
    private static string Format(decimal v) => v >= 1_000_000
        ? $"{v / 1_000_000:0.##}M"
        : v >= 1_000 ? $"{v / 1_000:0.#}K" : Money.Format(v);

    /// <summary>نگاشت ردیف فاکتور</summary>
    private static InvoiceRow MapInvoice(InvoiceDto i)
    {
        var status = i.PaymentStatus switch
        {
            1 => ("پرداخت‌نشده", "#FFEBEE"),
            2 => ("پرداخت جزئی", "#FFF3E0"),
            3 => ("پرداخت‌شده", "#E8F5E9"),
            _ => ("—", "#ECEFF1")
        };
        return new InvoiceRow(i.Id, i.InvoiceNumber ?? i.Id.ToString(),
            i.DateShamsi ?? "—", i.PatientName, i.TotalAmount,
            i.PaidAmount, status.Item1, status.Item2);
    }

    /// <summary>صدور فاکتور جدید — فرم کامل</summary>
    [RelayCommand]
    private void NewInvoice()
    {
        var vm = new InvoiceFormViewModel(_api, _snackbar);
        var window = new InvoiceFormWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>بستن شیفت صندوق و تحویل — چرخه ۱۳ (درخواست ۲-۲)</summary>
    [RelayCommand]
    private void CloseShift()
    {
        var vm = new ShiftCloseViewModel(_api, _snackbar);
        var window = new ShiftCloseWindow { Owner = System.Windows.Application.Current.MainWindow };
        window.DataContext = vm;
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ثبت پرداخت برای فاکتور انتخابی</summary>
    [RelayCommand]
    private void RecordPayment()
    {
        if (SelectedInvoice is null) { _snackbar.Error("ابتدا یک فاکتور انتخاب کنید"); return; }
        var vm = new PaymentWindowViewModel(_api, SelectedInvoice.Id,
            SelectedInvoice.Total - SelectedInvoice.Paid);
        var window = new PaymentWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class SummaryResponse
    {
        public bool IsSuccess { get; set; }
        public SummaryData? Data { get; set; }
    }

    public sealed class SummaryData
    {
        public int InvoiceCount { get; set; }
        public decimal CashTotal { get; set; }
        public decimal CardTotal { get; set; }
        public decimal OnlineTotal { get; set; }
        public decimal RefundTotal { get; set; }
        public decimal NetTotal { get; set; }
    }

    public sealed class TransactionsResponse
    {
        public bool IsSuccess { get; set; }
        public List<TransactionDto>? Data { get; set; }
    }

    public sealed class TransactionDto
    {
        public long PaymentId { get; set; }
        public string? PaymentNumber { get; set; }
        public string Time { get; set; } = string.Empty;
        public long? InvoiceId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public string MethodText { get; set; } = string.Empty;
    }

    /// <summary>پاسخ فاکتورها — تک‌لایه منطبق بر ToPagedResponse سرور (چرخه ۱۹)</summary>
    public sealed class InvoicesResponse
    {
        public bool IsSuccess { get; set; }
        public List<InvoiceDto>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public sealed class InvoiceDto
    {
        public long Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? DateShamsi { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int PaymentStatus { get; set; }
    }
}

/// <summary>
/// ViewModel فرم صدور فاکتور — گام U8 طبق فرم ۱۴:
/// اقلام خدمت با کد K + خلاصه (تخفیف مبلغ↔درصد + بیمه + مالیات) + ۵ روش پرداخت
/// </summary>
public partial class InvoiceFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private int _serviceSearchVersion;
    private int _patientSearchVersion;
    private bool _invoiceSubmissionLocked;
    [ObservableProperty] private bool _hasInvalidInvoiceInput;
    public bool CanEditInvoice => !IsBusy && !_invoiceSubmissionLocked;
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanEditInvoice));

    /// <summary>عنوان</summary>
    [ObservableProperty]
    private string _formTitle = "💰 صدور فاکتور";

    /// <summary>جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearch = string.Empty;

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private QuickPatient? _patient;

    /// <summary>نمایه پزشک — صفر یعنی بدون پزشک</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>نمایه بیمه — صفر یعنی آزاد</summary>
    [ObservableProperty]
    private int _insuranceIndex;

    /// <summary>تغییر بیمهٔ فاکتور — چرخه ۲۴: فهرست خدمات با تعرفهٔ بیمهٔ منتخب
    /// دوباره خوانده می‌شود (در نبود تعرفهٔ بیمه، همان تعرفهٔ آزاد نمایش می‌یابد)
    /// تا قیمت اقلام تازه دقیق باشد</summary>
    partial void OnInsuranceIndexChanged(int value)
        => _ = LoadServicesAsync(ServiceSearch);

    /// <summary>بیمه‌ها</summary>
    public ObservableCollection<InsuranceOption> InsuranceCompanies { get; } = new();

    /// <summary>خدمات قابل انتخاب</summary>
    public ObservableCollection<ServiceOption> Services { get; } = new();

    /// <summary>پرسنل انجام‌دهنده — برای انتساب هر قلم و محاسبه حقوق درصدی از سود</summary>
    public ObservableCollection<PerformerOption> Performers { get; } = new();

    /// <summary>جستجوی خدمت</summary>
    [ObservableProperty]
    private string _serviceSearch = string.Empty;

    /// <summary>خدمت انتخاب‌شده برای افزودن</summary>
    [ObservableProperty]
    private ServiceOption? _selectedService;

    /// <summary>تعداد خدمت</summary>
    [ObservableProperty]
    private int _serviceQuantity = 1;

    /// <summary>اقلام فاکتور</summary>
    public ObservableCollection<InvoiceItemVM> Items { get; } = new();

    /* ── پنل خلاصه ── */
    /// <summary>تخفیف ریالی</summary>
    [ObservableProperty]
    private decimal _discountAmount;

    /// <summary>درصد تخفیف</summary>
    [ObservableProperty]
    private decimal _discountPercent;

    /// <summary>نرخ مالیات</summary>
    [ObservableProperty]
    private decimal _taxPercent;

    /// <summary>نمایه روش پرداخت — نقد/کارت/بانکی/اقساطی</summary>
    [ObservableProperty]
    private int _paymentMethodIndex;

    /// <summary>مبلغ دریافتی</summary>
    [ObservableProperty]
    private decimal _receivedAmount;

    /// <summary>شماره ارجاع — برای کارت</summary>
    [ObservableProperty]
    private string? _referenceNumber;

    /// <summary>جمع کل</summary>
    public decimal GrossTotal => Items.Sum(i => i.Total);

    /// <summary>سهم بیمه</summary>
    public decimal InsuranceShare => Items.Sum(i => i.TotalInsurance);

    /// <summary>پس از تخفیف</summary>
    public decimal AfterDiscount => Math.Max(0, GrossTotal - DiscountAmount);

    /// <summary>مالیات</summary>
    public decimal TaxAmount => Math.Round(AfterDiscount * TaxPercent / 100m);

    /// <summary>قابل پرداخت</summary>
    public decimal Payable => AfterDiscount + TaxAmount;

    /// <summary>باقیمانده</summary>
    public decimal Remaining => Math.Max(0, Payable - ReceivedAmount);

    /// <summary>متن‌های نمایشی</summary>
    public string GrossTotalText => Money.Format(GrossTotal);
    public string InsuranceShareText => Money.Format(InsuranceShare);
    public string TaxText => Money.Format(TaxAmount);
    public string PayableText => Money.Format(Payable);
    public string RemainingText => Money.Format(Remaining);

    /// <summary>در حال ثبت</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده</summary>
    public InvoiceFormViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری داده مرجع</summary>
    private async Task InitializeAsync()
    {
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): همان رفعِ صفحهٔ پذیرش — قانون ۵
        // (رفع خطای مشابه در کل برنامه): کشوی بیمه صورتحساب هم پیش‌تر فاقد گزینهٔ
        // «آزاد» بود و نمایهٔ صفر (نخستین شرکت سرور) به‌عنوان بدون‌بیمه تفسیر می‌شد؛
        // آیتم صریح آزاد با شناسهٔ صفر در ابتدا درج شد تا انتخاب آزاد واقعی و
        // ثبت دقیق شرکت انتخابی (نمایهٔ ۱ به بعد) هر دو ممکن شود.
        InsuranceCompanies.Add(new InsuranceOption(0, "آزاد (بدون بیمه)", false));
        if (companies?.Data is not null)
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name, c.IsContracted, c.CoveragePercent));

        // پرسنل فعال برای انتخاب انجام‌دهنده هر قلم — چرخه نهم
        var employees = await _api.GetAsync<EmployeesPage>("hr/employees");
        if (employees?.Data is not null)
            foreach (var e in employees.Data.Where(e => e.IsActive))
                Performers.Add(new PerformerOption(e.Id, e.FullName));

        await LoadServicesAsync(string.Empty);
    }

    /// <summary>جستجوی خدمات با تعرفه</summary>
    partial void OnServiceSearchChanged(string value) => _ = LoadServicesAsync(value);

    /// <summary>بارگذاری خدمات — چرخه ۲۴: با بیمهٔ منتخب فاکتور (تعرفهٔ بیمه‌ای
    /// در نبود، تعرفهٔ آزاد) تا قیمت‌ها همگام با بیمهٔ انتخابی باشد</summary>
    private async Task LoadServicesAsync(string term)
    {
        // شناسهٔ بیمهٔ منتخب — صفر = آزاد → پارامتری نمی‌فرستیم
        var insuranceId = InsuranceIndex > 0 && InsuranceIndex < InsuranceCompanies.Count
            ? InsuranceCompanies[InsuranceIndex].Id
            : (long?)null;
        var url = "lookups/services?search=" + Uri.EscapeDataString(term ?? string.Empty) +
                  (insuranceId is null ? "" : $"&insuranceCompanyId={insuranceId}");
        var version = ++_serviceSearchVersion;
        SelectedService = null;
        Services.Clear();
        try
        {
            var response = await _api.GetAsync<ServicesResponse>(url);
            if (version != _serviceSearchVersion) return;
            if (response?.Data is null) { _snackbar.Error("دریافت تعرفه خدمات ناموفق بود."); return; }
            foreach (var s in response.Data)
                Services.Add(new ServiceOption(s.ServiceId, s.ServiceCode, s.ServiceName,
                    s.Price, s.InsuranceShare, s.PatientShare));
        }
        catch (Exception) { if (version == _serviceSearchVersion) _snackbar.Error("دریافت تعرفه خدمات ناموفق بود."); }
    }

    /// <summary>جستجوی بیمار با Enter</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (!CanEditInvoice) return;
        if (Patient is not null && Items.Count > 0) { _snackbar.Error("برای تغییر بیمار ابتدا اقلام فاکتور را حذف کنید."); return; }
        Patient = null;
        var version = ++_patientSearchVersion;
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, PatientSearch);
        if (version != _patientSearchVersion || !CanEditInvoice || patient is null) return;
        Patient = patient;
        FormTitle = $"💰 صدور فاکتور — {Patient.FullName}";
    }

    /// <summary>افزودن خدمت به اقلام</summary>
    [RelayCommand]
    private void AddService()
    {
        if (!CanEditInvoice) return;
        if (HasInvalidInvoiceInput) { _snackbar.Error("ورودی عددی نامعتبر است؛ ابتدا آن را اصلاح کنید."); return; }
        if (SelectedService is null || !Services.Contains(SelectedService)) { _snackbar.Error("ابتدا خدمت را از فهرست جاری انتخاب کنید"); return; }
        if (ServiceQuantity <= 0 || ServiceQuantity > 99999999) { _snackbar.Error("تعداد خدمت نامعتبر است."); return; }
        var item = new InvoiceItemVM
        {
            ServiceId = SelectedService.ServiceId,
            Code = SelectedService.ServiceCode ?? "K-—",
            Name = SelectedService.ServiceName,
            UnitPrice = SelectedService.Price,
            Quantity = ServiceQuantity,
            InsuranceShare = InsuranceIndex > 0 ? SelectedService.InsuranceShare : 0,
            PatientShare = InsuranceIndex > 0 ? SelectedService.PatientShare : SelectedService.Price
        };
        item.PropertyChanged += (_, _) => RaiseTotals();
        Items.Add(item);
        SelectedService = null;
        ServiceSearch = string.Empty;
        RaiseTotals();
    }

    /// <summary>حذف قلم</summary>
    [RelayCommand]
    private void RemoveItem(object? param)
    {
        if (param is not InvoiceItemVM item) return;
        Items.Remove(item);
        RaiseTotals();
    }

    /// <summary>تغییر تخفیف مبلغی → درصد خودکار</summary>
    partial void OnDiscountAmountChanged(decimal value)
    {
        if (GrossTotal > 0 && value >= 0)
        {
            var percent = Math.Round(value * 100m / GrossTotal, 1);
            if (Math.Abs(percent - DiscountPercent) > 0.05m)
                DiscountPercent = percent;
        }
        RaiseTotals();
    }

    /// <summary>تغییر درصد تخفیف → مبلغ خودکار</summary>
    partial void OnDiscountPercentChanged(decimal value)
    {
        if (value >= 0 && value <= 100)
        {
            var amount = Math.Round(GrossTotal * value / 100m);
            if (Math.Abs(amount - DiscountAmount) > 0.5m)
                DiscountAmount = amount;
        }
        RaiseTotals();
    }

    partial void OnTaxPercentChanged(decimal value) => RaiseTotals();
    partial void OnReceivedAmountChanged(decimal value) => RaiseTotals();

    /// <summary>بازمحاسبه نمایش‌ها</summary>
    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(GrossTotal));
        OnPropertyChanged(nameof(InsuranceShare));
        OnPropertyChanged(nameof(AfterDiscount));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(Payable));
        OnPropertyChanged(nameof(Remaining));
        OnPropertyChanged(nameof(GrossTotalText));
        OnPropertyChanged(nameof(InsuranceShareText));
        OnPropertyChanged(nameof(TaxText));
        OnPropertyChanged(nameof(PayableText));
        OnPropertyChanged(nameof(RemainingText));
    }

    /// <summary>صدور و پرداخت فقط پس از تطبیق مبلغ قطعی سرور.</summary>
    [RelayCommand]
    private async Task SaveAndPrintAsync()
    {
        if (!CanEditInvoice) return;
        if (HasInvalidInvoiceInput) { ErrorMessage = "ورودی عددی نامعتبر است؛ پیش از صدور آن را اصلاح کنید."; return; }
        if (Patient is null || Patient.Id <= 0) { ErrorMessage = "ابتدا بیمار را به‌طور یکتا انتخاب کنید"; return; }
        if (Items.Count == 0 || Items.Any(i => i.Quantity <= 0 || i.ServiceId is not > 0)) { ErrorMessage = "خدمت و تعداد معتبر انتخاب کنید"; return; }
        if (InsuranceCompanies.Count == 0 || InsuranceIndex != 0 || InsuranceCompanies[0].Id != 0 || DiscountAmount != 0 || DiscountPercent != 0 || TaxPercent != 0)
        { ErrorMessage = "در این فرم فعلاً فقط فاکتور آزاد بدون تخفیف و مالیات مجاز است؛ محاسبه بیمه و تخفیف نیازمند پیش‌نمایش معتبر سرور است."; return; }
        if (PaymentMethodIndex is < 0 or > 3 || ReceivedAmount < 0 || ReceivedAmount > Payable || decimal.Truncate(ReceivedAmount) != ReceivedAmount)
        { ErrorMessage = "مبلغ دریافتی یا روش پرداخت نامعتبر است."; return; }
        var patient = Patient;
        var expectedAmount = Payable;
        var received = ReceivedAmount;
        var method = PaymentMethodIndex switch { 0 => 1, 1 => 2, 2 => 5, 3 => 4, _ => 0 };
        var doctor = DoctorIndex >= 0 && DoctorIndex < Doctors.Count ? Doctors[DoctorIndex] : null;
        IsBusy = true;
        var sentInvoice = false;
        long? invoiceId = null;
        try
        {
            sentInvoice = true;
            var invoice = await _api.PostAsync<InvoiceCreateResult>("invoices", new
            {
                patientId = patient.Id, receptionId = (long?)null, visitId = (long?)null, type = 1,
                insuranceCompanyId = (long?)null, taxPercent = 0m,
                items = Items.Select(i => new { serviceId = i.ServiceId, description = i.Name, quantity = i.Quantity,
                    unitPrice = i.UnitPrice, discountPercent = 0m, performedById = i.Performer?.Id, doctorId = doctor?.Id }).ToList(),
                description = $"فاکتور دستی — {patient.FullName}"
            });
            _invoiceSubmissionLocked = true;
            OnPropertyChanged(nameof(CanEditInvoice));
            if (invoice?.Data is null or <= 0)
            { ErrorMessage = "نتیجه صدور فاکتور نامعلوم است؛ پیش از تکرار، فهرست فاکتورها را بررسی کنید. " + _api.LastError; return; }
            invoiceId = invoice.Data;
            var detail = await _api.GetAsync<PaymentWindowViewModel.InvoiceDetailEnvelope>($"invoices/{invoiceId}");
            if (detail?.Data is null || detail.Data.Id != invoiceId || detail.Data.PatientId != patient.Id || detail.Data.RemainingAmount != expectedAmount)
            { ErrorMessage = $"فاکتور #{invoiceId} صادر شد، اما مبلغ یا هویت با پیش‌نمایش منطبق نیست؛ پرداخت ارسال نشد. در صندوق بررسی کنید."; return; }
            if (received == 0)
            { _snackbar.Success($"فاکتور #{invoiceId} صادر شد؛ پرداختی ثبت نشد."); CloseRequested?.Invoke(this, EventArgs.Empty); return; }
            var paymentOk = await _api.PostAsync("payments", new
            {
                invoiceId, patientId = patient.Id, amount = received, method,
                referenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(), cardNumber = (string?)null
            });
            if (!paymentOk)
            { ErrorMessage = $"فاکتور #{invoiceId} صادر شد؛ نتیجه پرداخت نامعلوم است. پیش از تلاش مجدد، سوابق پرداخت را بررسی کنید. " + _api.LastError; return; }
            _snackbar.Success($"فاکتور #{invoiceId} و پرداخت ثبت شدند؛ چاپ خودکار انجام نشده است.");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            if (sentInvoice) { _invoiceSubmissionLocked = true; OnPropertyChanged(nameof(CanEditInvoice)); }
            ErrorMessage = invoiceId.HasValue
                ? $"فاکتور #{invoiceId} صادر شده؛ وضعیت پرداخت را پیش از تلاش مجدد در صندوق بررسی کنید."
                : "نتیجه صدور فاکتور نامعلوم است؛ پیش از تلاش مجدد فهرست فاکتورها را بررسی کنید.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>پیش‌نمایش چاپ</summary>
    [RelayCommand]
    private void Preview()
        => _snackbar.Info("پیش‌نمایش چاپ در گام U19 (قالب‌های چاپ) متصل می‌شود");

    /// <summary>بستن</summary>
    [RelayCommand]
    private void Close() { if (!IsBusy) CloseRequested?.Invoke(this, EventArgs.Empty); }

    public sealed class ServicesResponse
    {
        public bool IsSuccess { get; set; }
        public List<ServiceDto>? Data { get; set; }
    }

    public sealed class ServiceDto
    {
        public long ServiceId { get; set; }
        public string? ServiceCode { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal InsuranceShare { get; set; }
        public decimal PatientShare { get; set; }
    }

    public sealed class InvoiceCreateResult
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }

    /// <summary>پاسخ فهرست پرسنل (hr/employees)</summary>
    public sealed class EmployeesPage
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeRow>? Data { get; set; }
    }

    /// <summary>ردیف پرسنل</summary>
    public sealed class EmployeeRow
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
