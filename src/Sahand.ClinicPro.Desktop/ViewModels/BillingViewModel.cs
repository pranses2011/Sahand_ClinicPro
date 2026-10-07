using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

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
        : v >= 1_000 ? $"{v / 1_000:0.#}K" : v.ToString("N0");

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
    public string GrossTotalText => GrossTotal.ToString("N0");
    public string InsuranceShareText => InsuranceShare.ToString("N0");
    public string TaxText => TaxAmount.ToString("N0");
    public string PayableText => Payable.ToString("N0");
    public string RemainingText => Remaining.ToString("N0");

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
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName));

        var companies = await _api.GetAsync<PatientFormViewModel.LookupCompanies>("lookups/insurance-companies");
        if (companies?.Data is not null)
            foreach (var c in companies.Data)
                InsuranceCompanies.Add(new InsuranceOption(c.Id, c.Name));

        // پرسنل فعال برای انتخاب انجام‌دهنده هر قلم — چرخه نهم
        var employees = await _api.GetAsync<EmployeesPage>("hr/employees");
        if (employees?.Data is not null)
            foreach (var e in employees.Data.Where(e => e.IsActive))
                Performers.Add(new PerformerOption(e.Id, e.FullName));

        await LoadServicesAsync(string.Empty);
    }

    /// <summary>جستجوی خدمات با تعرفه</summary>
    partial void OnServiceSearchChanged(string value) => _ = LoadServicesAsync(value);

    /// <summary>بارگذاری خدمات</summary>
    private async Task LoadServicesAsync(string term)
    {
        var response = await _api.GetAsync<ServicesResponse>(
            $"lookups/services?search={Uri.EscapeDataString(term ?? string.Empty)}");
        Services.Clear();
        if (response?.Data is null) return;
        foreach (var s in response.Data)
            Services.Add(new ServiceOption(s.ServiceId, s.ServiceCode, s.ServiceName,
                s.Price, s.InsuranceShare, s.PatientShare));
    }

    /// <summary>جستجوی بیمار با Enter</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearch.Trim())}");
        var p = response?.Data?.FirstOrDefault();
        if (p is null) { _snackbar.Error("بیمار یافت نشد"); return; }
        Patient = new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode,
            p.Mobile, p.InsuranceName, p.Allergies, null);
        FormTitle = $"💰 صدور فاکتور — {Patient.FullName}";
    }

    /// <summary>افزودن خدمت به اقلام</summary>
    [RelayCommand]
    private void AddService()
    {
        if (SelectedService is null) { _snackbar.Error("ابتدا خدمت را انتخاب کنید"); return; }
        Items.Add(new InvoiceItemVM
        {
            ServiceId = SelectedService.ServiceId,
            Code = SelectedService.ServiceCode ?? "K-—",
            Name = SelectedService.ServiceName,
            UnitPrice = SelectedService.Price,
            InsuranceShare = InsuranceIndex > 0 ? SelectedService.InsuranceShare : 0,
            PatientShare = InsuranceIndex > 0 ? SelectedService.PatientShare : SelectedService.Price
        });
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

    /// <summary>ثبت و چاپ — زنجیره فاکتور + پرداخت</summary>
    [RelayCommand]
    private async Task SaveAndPrintAsync()
    {
        if (Patient is null) { ErrorMessage = "ابتدا بیمار را انتخاب کنید"; return; }
        if (Items.Count == 0) { ErrorMessage = "حداقل یک خدمت به فاکتور اضافه کنید"; return; }
        if (ReceivedAmount < Payable) { ErrorMessage = "مبلغ دریافتی کمتر از قابل پرداخت است"; return; }

        IsBusy = true;
        var company = InsuranceIndex > 0 ? InsuranceCompanies[InsuranceIndex - 1] : null;

        // پزشک انتخاب‌شده سطح فاکتور — به همه اقلام منتسب می‌شود (چرخه دهم) تا
        // انتساب خودکار «انجام‌دهنده از پرسنل مرتبط پزشک» در سرور فعال شود و
        // سود خدمات در فیش حقوقی پزشک (حقوق درصدی) لحاظ گردد
        var doctor = DoctorIndex >= 0 && DoctorIndex < Doctors.Count ? Doctors[DoctorIndex] : null;

        // ۱. ثبت فاکتور
        var invoice = await _api.PostAsync<InvoiceCreateResult>("invoices", new
        {
            patientId = Patient.Id,
            receptionId = (long?)null,
            visitId = (long?)null,
            type = 1,
            insuranceCompanyId = company?.Id,
            taxPercent = TaxPercent,
            items = Items.Select(i => new
            {
                serviceId = i.ServiceId,
                serviceName = i.Name,
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                discount = 0m,
                performedById = i.Performer?.Id,
                doctorId = doctor?.Id
            }),
            description = $"فاکتور دستی — {Patient.FullName}"
        });

        if (invoice?.Data is null or 0)
        {
            IsBusy = false;
            ErrorMessage = _api.LastError ?? "ثبت فاکتور ناموفق بود";
            return;
        }

        // ۲. ثبت پرداخت
        await _api.PostAsync("payments", new
        {
            invoiceId = invoice.Data,
            patientId = Patient.Id,
            amount = ReceivedAmount,
            method = PaymentMethodIndex + 1,
            referenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
            cardNumber = (string?)null
        });

        IsBusy = false;
        _snackbar.Success($"فاکتور #{invoice.Data} ثبت شد — چاپ در گام U19 متصل می‌شود");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>پیش‌نمایش چاپ</summary>
    [RelayCommand]
    private void Preview()
        => _snackbar.Info("پیش‌نمایش چاپ در گام U19 (قالب‌های چاپ) متصل می‌شود");

    /// <summary>بستن</summary>
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

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
