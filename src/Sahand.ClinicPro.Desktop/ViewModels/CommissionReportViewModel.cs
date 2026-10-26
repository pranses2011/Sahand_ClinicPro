using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel گزارش تفصیلی «سود و حقوق درصدی هر پرسنل» — چرخه دهم
/// فیلتر دوره/پرسنل + جدول خلاصه + جزئیات اقلام پرسنل انتخاب‌شده +
/// خروجی اکسل دو برگ (خلاصه/جزئیات) از endpoint گزارش
/// </summary>
public partial class CommissionReportViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان پنجره</summary>
    public override string Title => "گزارش تفصیلی سود و حقوق درصدی پرسنل";

    /// <summary>نام ماه‌های شمسی</summary>
    public static string[] MonthNames => PersianDateHelper.MonthNames;

    /// <summary>سال‌های قابل انتخاب — از ۴ سال پیش تا سال جاری</summary>
    public static int[] Years { get; } = Enumerable.Range(
        PersianDateHelper.ToJalali(IranClock.Today).Year - 4, 5).Reverse().ToArray();

    /// <summary>سازنده</summary>
    public CommissionReportViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;

        // دوره جاری به‌صورت پیش‌فرض
        var today = PersianDateHelper.ToJalali(IranClock.Today);
        _selectedYear = today.Year;
        _selectedMonthIndex = today.Month - 1;
    }

    // ── فیلترها ──

    /// <summary>سال دوره شمسی</summary>
    [ObservableProperty]
    private int _selectedYear;

    /// <summary>اندیس ماه — ۰ تا ۱۱</summary>
    [ObservableProperty]
    private int _selectedMonthIndex;

    /// <summary>اندیس پرسنل — صفر یعنی «همه پرسنل»</summary>
    [ObservableProperty]
    private int _selectedEmployeeIndex;

    /// <summary>گزینه‌های فیلتر پرسنل — «همه» + فهرست پرسنل فعال</summary>
    [ObservableProperty]
    private ObservableCollection<EmployeeOption> _employees = new();

    // ── داده گزارش ──

    /// <summary>خلاصه هر پرسنل</summary>
    [ObservableProperty]
    private ObservableCollection<SummaryRow> _summaryRows = new();

    /// <summary>ردیف خلاصه انتخاب‌شده — جزئیات همان پرسنل نمایش داده می‌شود</summary>
    [ObservableProperty]
    private SummaryRow? _selectedSummaryRow;

    /// <summary>جزئیات اقلام — همه یا پرسنل انتخاب‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<DetailRow> _detailRows = new();

    /// <summary>نام درمانگاه از سربرگ گزارش</summary>
    [ObservableProperty]
    private string _clinicName = string.Empty;

    /// <summary>متن دوره — «مهر ۱۴۰۴»</summary>
    [ObservableProperty]
    private string _periodText = string.Empty;

    /// <summary>جمع سود خدمات دوره — کارت آماری</summary>
    [ObservableProperty]
    private decimal _totalProfit;

    /// <summary>جمع حقوق درصدی دوره — کارت آماری</summary>
    [ObservableProperty]
    private decimal _totalCommission;

    /// <summary>نقاط نمودار دونات سهم پرسنل از حقوق درصدی — چرخه یازدهم</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPointData>? _commissionChartPoints;

    /// <summary>متن مرکز دونات — جمع حقوق درصدی دوره</summary>
    [ObservableProperty]
    private string _commissionChartCenter = "";

    /// <summary>تعداد اقلام منتسب‌شده دوره</summary>
    [ObservableProperty]
    private int _serviceCount;

    /// <summary>تعداد پرسنل گزارش</summary>
    [ObservableProperty]
    private int _employeeCount;

    /// <summary>پیام وضعیت پس از ذخیره اکسل</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>همه جزئیات دوره — منبع فیلتر جزئیات</summary>
    private List<DetailRow> _allDetails = new();

    /// <summary>بارگذاری اولیه — فهرست پرسنل + گزارش دوره جاری</summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadEmployeesAsync();
        await LoadAsync();
    }

    /// <summary>بارگذاری گزینه‌های فیلتر پرسنل</summary>
    private async Task LoadEmployeesAsync()
    {
        var response = await _api.GetAsync<EmployeesResponse>("hr/employees");
        var options = new ObservableCollection<EmployeeOption> { new(0, "همه پرسنل") };
        if (response?.Data is not null)
            foreach (var e in response.Data)
                options.Add(new EmployeeOption(e.Id, e.FullName));
        Employees = options;
    }

    /// <summary>بارگذاری گزارش دوره با فیلترهای انتخابی</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var employeeId = SelectedEmployeeIndex > 0 && SelectedEmployeeIndex < Employees.Count
                ? Employees[SelectedEmployeeIndex].Id
                : (long?)null;

            var endpoint = $"reports/commission-detail?year={SelectedYear}" +
                           $"&month={SelectedMonthIndex + 1}" +
                           (employeeId.HasValue ? $"&employeeId={employeeId}" : string.Empty);

            var response = await _api.GetAsync<ReportResponse>(endpoint);
            var data = response?.Data;
            if (data is null)
            {
                ErrorMessage = _api.LastError ?? "دریافت گزارش ناموفق بود";
                return;
            }

            ClinicName = data.ClinicName;
            PeriodText = data.PeriodText;
            TotalProfit = data.TotalProfit;
            TotalCommission = data.TotalCommission;
            ServiceCount = data.ServiceCountTotal;
            EmployeeCount = data.EmployeeCount;

            SummaryRows = new ObservableCollection<SummaryRow>(
                (data.Summary ?? new List<SummaryDto>()).Select(s => new SummaryRow(
                    s.EmployeeId, s.PersonnelCode, s.FullName, s.DepartmentName, s.PositionName,
                    s.IsDoctorLinked, s.DoctorNames,
                    s.ServiceCount, s.TotalRevenue, s.TotalCost, s.TotalProfit,
                    s.CommissionPercent, s.CommissionBase, s.ServiceCommission,
                    s.SalaryStatusText)));

            // نمودار دونات سهم پرسنل از حقوق درصدی — چرخه یازدهم (ماهیت سهم از کل)
            CommissionChartPoints = SummaryRows
                .Where(s => s.ServiceCommission > 0)
                .OrderByDescending(s => s.ServiceCommission)
                .Take(10)
                .Select(s => new ChartPointData(
                    s.FullName,
                    (double)s.ServiceCommission,
                    // چرخهٔ بیست‌وهفتم: واحد پول پویا از تنظیمات (ریال/تومان)
                    $"{s.FullName}: {Money.FormatWithUnit(s.ServiceCommission)} (درصد {s.CommissionPercentText})"))
                .ToList();
            CommissionChartCenter = Money.Format(TotalCommission);

            _allDetails = (data.Details ?? new List<DetailDto>()).Select(d => new DetailRow(
                d.EmployeeId, d.EmployeeName, d.InvoiceNumber, d.InvoiceDateShamsi,
                d.PatientName, d.ServiceName, d.Quantity, d.UnitPrice, d.UnitCost,
                d.TotalPrice, d.Profit, d.Attribution)).ToList();

            ApplyDetailFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بازخورد تغییر ردیف خلاصه — فیلتر جزئیات همان پرسنل</summary>
    partial void OnSelectedSummaryRowChanged(SummaryRow? value) => ApplyDetailFilter();

    /// <summary>نمایش همه جزئیات — برداشتن انتخاب خلاصه</summary>
    [RelayCommand]
    private void ShowAllDetails()
    {
        SelectedSummaryRow = null;
        ApplyDetailFilter();
    }

    /// <summary>اعمال فیلتر جزئیات بر اساس ردیف خلاصه انتخاب‌شده</summary>
    private void ApplyDetailFilter()
    {
        DetailRows = SelectedSummaryRow is null
            ? new ObservableCollection<DetailRow>(_allDetails)
            : new ObservableCollection<DetailRow>(
                _allDetails.Where(d => d.EmployeeId == SelectedSummaryRow.EmployeeId));
    }

    /// <summary>خروجی اکسل — دانلود xlsx دو برگ و ذخیره در محل انتخابی</summary>
    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;
        try
        {
            var employeeId = SelectedEmployeeIndex > 0 && SelectedEmployeeIndex < Employees.Count
                ? Employees[SelectedEmployeeIndex].Id
                : (long?)null;

            var endpoint = $"reports/commission-detail/excel?year={SelectedYear}" +
                           $"&month={SelectedMonthIndex + 1}" +
                           (employeeId.HasValue ? $"&employeeId={employeeId}" : string.Empty);

            var bytes = await _api.DownloadFileAsync(endpoint);
            if (bytes is null || bytes.Length == 0)
            {
                ErrorMessage = _api.LastError ?? "دریافت فایل اکسل ناموفق بود";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "ذخیره گزارش سود و حقوق درصدی",
                Filter = "فایل اکسل|*.xlsx",
                FileName = $"گزارش-سود-و-حقوق-درصدی-{SelectedYear}-{SelectedMonthIndex + 1:00}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            StatusMessage = $"فایل اکسل ذخیره شد: {dialog.FileName}";
            _snackbar.Success("خروجی اکسل ذخیره شد");
        }
        catch (IOException ex)
        {
            ErrorMessage = $"ذخیره فایل ناموفق بود — {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── مدل‌های نمایشی ──

    /// <summary>گزینه فیلتر پرسنل</summary>
    public sealed record EmployeeOption(long Id, string FullName);

    /// <summary>ردیف خلاصه پرسنل</summary>
    public sealed record SummaryRow(
        long EmployeeId, string PersonnelCode, string FullName,
        string DepartmentName, string PositionName,
        bool IsDoctorLinked, string DoctorNames,
        int ServiceCount, decimal TotalRevenue, decimal TotalCost, decimal TotalProfit,
        decimal CommissionPercent, decimal CommissionBase, decimal ServiceCommission,
        string SalaryStatusText)
    {
        /// <summary>جمع فروش — نمایش هزارگان</summary>
        public string TotalRevenueText => Money.Format(TotalRevenue);

        /// <summary>جمع هزینه — نمایش هزارگان</summary>
        public string TotalCostText => Money.Format(TotalCost);

        /// <summary>سود خدمات — نمایش هزارگان</summary>
        public string TotalProfitText => Money.Format(TotalProfit);

        /// <summary>مبنای حقوق درصدی — نمایش هزارگان</summary>
        public string CommissionBaseText => Money.Format(CommissionBase);

        /// <summary>حقوق درصدی — نمایش هزارگان</summary>
        public string ServiceCommissionText => Money.Format(ServiceCommission);

        /// <summary>درصد — نمایش بدون اعشار زائد</summary>
        public string CommissionPercentText => CommissionPercent.ToString("0.##");

        /// <summary>نشان پزشک متصل — «پزشک ✓» یا «—»</summary>
        public string DoctorBadge => IsDoctorLinked ? "پزشک ✓" : "—";
    }

    /// <summary>ردیف جزئیات قلم</summary>
    public sealed record DetailRow(
        long EmployeeId, string EmployeeName, string InvoiceNumber, string InvoiceDateShamsi,
        string PatientName, string ServiceName, decimal Quantity,
        decimal UnitPrice, decimal UnitCost, decimal TotalPrice, decimal Profit,
        string Attribution)
    {
        /// <summary>مبلغ قلم — نمایش هزارگان</summary>
        public string TotalPriceText => Money.Format(TotalPrice);

        /// <summary>سود قلم — نمایش هزارگان</summary>
        public string ProfitText => Money.Format(Profit);

        /// <summary>قیمت واحد — نمایش هزارگان</summary>
        public string UnitPriceText => Money.Format(UnitPrice);

        /// <summary>هزینه واحد — نمایش هزارگان</summary>
        public string UnitCostText => Money.Format(UnitCost);
    }

    // ── مدل‌های پاسخ API ──

    private sealed class EmployeesResponse
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeDto>? Data { get; set; }
    }

    private sealed class EmployeeDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    private sealed class ReportResponse
    {
        public ReportDataDto? Data { get; set; }
    }

    private sealed class ReportDataDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string PeriodText { get; set; } = string.Empty;
        public string GeneratedAtShamsi { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public List<SummaryDto>? Summary { get; set; }
        public List<DetailDto>? Details { get; set; }
        public int EmployeeCount { get; set; }
        public int ServiceCountTotal { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalCommission { get; set; }
    }

    private sealed class SummaryDto
    {
        public long EmployeeId { get; set; }
        public string PersonnelCode { get; set; } = "—";
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = "—";
        public string PositionName { get; set; } = "—";
        public bool IsDoctorLinked { get; set; }
        public string DoctorNames { get; set; } = "—";
        public int ServiceCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal CommissionBase { get; set; }
        public decimal ServiceCommission { get; set; }
        public string SalaryStatusText { get; set; } = string.Empty;
    }

    private sealed class DetailDto
    {
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = "—";
        public string InvoiceDateShamsi { get; set; } = "—";
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Profit { get; set; }
        public string Attribution { get; set; } = string.Empty;
    }
}
