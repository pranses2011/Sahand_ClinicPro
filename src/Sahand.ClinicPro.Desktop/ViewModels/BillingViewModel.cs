using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف فاکتور — منطبق با InvoiceListDto سرور</summary>
public record InvoiceRow(
    long Id,
    string? Number,
    string PatientName,
    string Date,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string PaymentText,
    string StatusText);

/// <summary>
/// ViewModel مالی و صندوق — فهرست فاکتورها با فیلتر وضعیت پرداخت
/// </summary>
public partial class BillingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private const int PageSize = 20;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مالی و صندوق";

    /// <summary>فاکتورهای صفحه فعلی</summary>
    [ObservableProperty]
    private ObservableCollection<InvoiceRow> _invoices = new();

    /// <summary>فاکتور انتخاب‌شده</summary>
    [ObservableProperty]
    private InvoiceRow? _selectedInvoice;

    /// <summary>جستجو</summary>
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    /// <summary>شماره صفحه</summary>
    [ObservableProperty]
    private int _pageNumber = 1;

    /// <summary>تعداد صفحات</summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>تعداد کل</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>نمایه وضعیت پرداخت فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _paymentFilterIndex;

    /// <summary>برچسب وضعیت پرداخت</summary>
    public string[] PaymentLabels { get; } =
        { "پرداخت‌نشده", "پرداخت جزیی", "پرداخت‌شده", "پرداخت اضافه" };

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public BillingViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری صفحه فعلی</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = $"invoices?page={PageNumber}&pageSize={PageSize}" +
                       (PaymentFilterIndex > 0 ? $"&status={PaymentFilterIndex}" : string.Empty) +
                       (!string.IsNullOrWhiteSpace(SearchTerm)
                           ? $"&search={Uri.EscapeDataString(SearchTerm.Trim())}"
                           : string.Empty);

        var response = await _api.GetAsync<InvoicesPage>(endpoint);

        if (response?.Data is not null)
        {
            Invoices = new ObservableCollection<InvoiceRow>(response.Data.Select(MapRow));
            PageNumber = response.PageNumber;
            TotalPages = response.TotalPages;
            TotalCount = response.TotalCount;
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت فاکتورها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف</summary>
    private static InvoiceRow MapRow(InvoiceDto i) => new(
        i.Id, i.InvoiceNumber, i.PatientName, i.DateShamsi,
        i.TotalAmount, i.PaidAmount, i.RemainingAmount,
        PaymentTextOf(i.PaymentStatus), StatusTextOf(i.Status));

    /// <summary>متن فارسی وضعیت پرداخت</summary>
    private static string PaymentTextOf(int status) => status is >= 1 and <= 4
        ? new[] { "پرداخت‌نشده", "پرداخت جزیی", "پرداخت‌شده", "پرداخت اضافه" }[status - 1]
        : "نامشخص";

    /// <summary>متن فارسی وضعیت فاکتور</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "پیش‌نویس",
        2 => "صادرشده",
        3 => "پرداخت جزیی",
        4 => "پرداخت‌شده",
        5 => "ابطال‌شده",
        _ => "نامشخص"
    };

    /// <summary>جستجو — صفحه اول</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>صفحه بعد</summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber < TotalPages)
        {
            PageNumber++;
            await LoadAsync();
        }
    }

    /// <summary>صفحه قبل</summary>
    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
            await LoadAsync();
        }
    }

    /// <summary>صدور فاکتور جدید</summary>
    [RelayCommand]
    private void NewInvoice()
    {
        var vm = new InvoiceFormViewModel(_api);
        var window = new InvoiceFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ثبت پرداخت برای فاکتور انتخاب‌شده</summary>
    [RelayCommand]
    private void RecordPayment()
    {
        if (SelectedInvoice is null) return;

        var vm = new PaymentWindowViewModel(_api, SelectedInvoice.Id,
            SelectedInvoice.RemainingAmount);
        var window = new PaymentWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>پاسخ صفحه فاکتورها</summary>
    public sealed class InvoicesPage
    {
        public bool IsSuccess { get; set; }
        public List<InvoiceDto>? Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>DTO فاکتور</summary>
    public sealed class InvoiceDto
    {
        public long Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public int PaymentStatus { get; set; }
        public int Status { get; set; }
    }
}