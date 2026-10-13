using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف درخواست آزمایش</summary>
/// <remarks>
/// چرخهٔ ۶۷: پراپرتی <see cref="CanPrint"/> تعیین می‌کند دکمهٔ «چاپ نتیجه»
/// برای این ردیف نمایش داده شود یا نه (وضعیت ۳=در جریان / ۴=نتیجه آماده).
/// </remarks>
public record LabOrderRow(
    long Id,
    string? Number,
    string PatientName,
    string DoctorName,
    string Date,
    string StatusText,
    int ItemsCount,
    int Status = 0)
{
    /// <summary>آیا دکمهٔ «چاپ نتیجه» برای این ردیف نمایش داده شود؟
    /// چرخهٔ ۶۷ (گزارش مالک: «امکان چاپ هم داشته باشه») — وضعیت ۳ (در جریان)
    /// و ۴ (نتیجه آماده) یعنی دست‌کم نتیجه‌ای ثبت شده که چاپش معنا دارد.</summary>
    public bool CanPrint => Status is 3 or 4;
}

/// <summary>قلم آزمایش قابل ویرایش نتیجه</summary>
public partial class LabItemRow : ObservableObject
{
    /// <summary>شناسه قلم</summary>
    public long Id { get; init; }

    /// <summary>نام آزمایش</summary>
    public string TestName { get; init; } = string.Empty;

    /// <summary>واحد</summary>
    public string? Unit { get; init; }

    /// <summary>بازه طبیعی</summary>
    public string? NormalRange { get; init; }

    /// <summary>وضعیت فعلی قلم</summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>نتیجه متنی</summary>
    [ObservableProperty]
    private string? _result;

    /// <summary>نتیجه عددی</summary>
    [ObservableProperty]
    private string? _resultNumeric;

    /// <summary>توضیح</summary>
    [ObservableProperty]
    private string? _comment;
}

/// <summary>
/// ViewModel آزمایشگاه — درخواست‌ها با فیلتر وضعیت
/// </summary>
public partial class LabViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "آزمایشگاه";

    /// <summary>درخواست‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<LabOrderRow> _orders = new();

    /// <summary>درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    private LabOrderRow? _selectedOrder;

    /// <summary>نمایه وضعیت فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    /// <summary>برچسب وضعیت‌ها</summary>
    public string[] StatusLabels { get; } =
        { "سفارش‌شده", "نمونه‌گیری‌شده", "در جریان", "نتیجه آماده" };

    /// <summary>پزشکان — برای فرم درخواست</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>سازنده با تزریق کلاینت API و سرویس توست (چرخهٔ ۶۷ — چاپ نتیجه)</summary>
    public LabViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری پزشکان و درخواست‌ها</summary>
    private async Task InitializeAsync()
    {
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        await LoadAsync();
    }

    /// <summary>بارگذاری درخواست‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = "lab/orders" +
                       (StatusFilterIndex > 0 ? $"?status={StatusFilterIndex}" : string.Empty);

        var response = await _api.GetAsync<LabOrdersResponse>(endpoint);

        if (response?.Data is not null)
        {
            Orders = new ObservableCollection<LabOrderRow>(response.Data.Select(o => new LabOrderRow(
                o.Id, o.OrderNumber, o.PatientName, o.DoctorName, o.DateShamsi,
                StatusTextOf(o.Status), o.ItemsCount, o.Status)));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت درخواست‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>متن فارسی وضعیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "سفارش‌شده",
        2 => "نمونه‌گیری‌شده",
        3 => "در جریان",
        4 => "نتیجه آماده",
        _ => "نامشخص"
    };

    /// <summary>ثبت درخواست آزمایش جدید</summary>
    [RelayCommand]
    private void NewOrder()
    {
        var vm = new LabOrderFormViewModel(_api, Doctors);
        var window = new LabOrderFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ثبت نتیجه درخواست انتخاب‌شده</summary>
    [RelayCommand]
    private void RecordResult()
    {
        if (SelectedOrder is null) return;

        var vm = new LabResultViewModel(_api, SelectedOrder.Id);
        var window = new LabResultWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>
    /// چاپ برگهٔ نتیجهٔ یک درخواست — چرخهٔ ۶۷ (گزارش مالک: «لیست آزمایشگاه
    /// ... امکان چاپ هم داشته باشه»). جزئیات درخواست (بیمار/پزشک/اقلام با
    /// نتایج ثبت‌شده) از سرور خوانده می‌شود، سند فارسی راست‌چین ساخته می‌شود
    /// و از دروازهٔ PrintFlow می‌گذرد (پیش‌نمایش اختیاری طبق تنظیمات چاپ).
    /// </summary>
    [RelayCommand]
    private async Task PrintResultAsync(LabOrderRow? row)
    {
        if (row is null) return;

        var response = await _api.GetAsync<LabResultViewModel.LabOrderDetailEnvelope>($"lab/orders/{row.Id}");
        var detail = response?.Data;
        if (detail is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت جزئیات درخواست برای چاپ ناموفق بود");
            return;
        }

        var printed = Services.LabResultPrinter.Print(detail, row.Id);
        if (printed)
            _snackbar.Success($"برگهٔ نتیجهٔ «{row.Number ?? row.Id.ToString()}» به چاپگر ارسال شد");
    }

    /// <summary>پاسخ درخواست‌ها</summary>
    public sealed class LabOrdersResponse
    {
        public bool IsSuccess { get; set; }
        public List<LabOrderDto>? Data { get; set; }
    }

    /// <summary>DTO درخواست</summary>
    public sealed class LabOrderDto
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public int ItemsCount { get; set; }
    }

    /* ── کاتالوگ تست‌ها — تب ۵ گام U10 ── */

    /// <summary>آیتم کاتالوگ تست</summary>
    public record TestCatalogRow(string Name, string? Category, string? Unit, decimal Price);

    /// <summary>کاتالوگ تست‌های فعال</summary>
    [ObservableProperty]
    private ObservableCollection<TestCatalogRow> _testCatalog = new();

    /// <summary>بارگذاری کاتالوگ تست‌ها از داده مرجع</summary>
    public async Task LoadCatalogAsync()
    {
        var response = await _api.GetAsync<CatalogResponse>("lookups/lab-tests");
        if (response?.Data is null) return;
        TestCatalog = new ObservableCollection<TestCatalogRow>(
            response.Data.Select(t => new TestCatalogRow(t.Name, t.Category, t.Unit, t.Price)));
    }

    /// <summary>پاسخ کاتالوگ آزمایش‌ها</summary>
    public sealed class CatalogResponse
    {
        public bool IsSuccess { get; set; }
        public List<CatalogItem>? Data { get; set; }
    }

    /// <summary>آیتم آزمایش مرجع</summary>
    public sealed class CatalogItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
    }
}
