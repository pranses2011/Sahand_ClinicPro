using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel حقوق و فیش حقوقی — گام R3 (چرخه چهارم)
/// انتخاب دوره شمسی، تولید خودکار، فهرست و پرداخت + پیش‌نمایش فیش چاپی
/// </summary>
public partial class PayrollViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "حقوق و فیش پرسنل";

    /// <summary>نام‌های ماه شمسی</summary>
    public static string[] MonthNames { get; } =
        { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
          "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

    /// <summary>سال‌های قابل انتخاب — از ۴ سال پیش تا سال جاری</summary>
    public static int[] Years { get; } = Enumerable.Range(
        PersianDateHelper.ToJalali(DateTime.Today).Year - 4, 5).Reverse().ToArray();

    /// <summary>سازنده</summary>
    public PayrollViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;

        // دوره جاری به‌صورت پیش‌فرض
        var today = PersianDateHelper.ToJalali(DateTime.Today);
        _selectedYear = today.Year;
        _selectedMonthIndex = today.Month - 1;
    }

    // ── انتخاب دوره ──

    /// <summary>سال دوره شمسی</summary>
    [ObservableProperty]
    private int _selectedYear;

    /// <summary>اندیس ماه — ۰ تا ۱۱</summary>
    [ObservableProperty]
    private int _selectedMonthIndex;

    /// <summary>متن دوره جاری — «۱۴۰۴ / ۰۷»</summary>
    public string PeriodText => $"{SelectedYear}/{SelectedMonthIndex + 1:00}";

    /// <summary>بازخورد تغییر دوره — بارگذاری خودکار فهرست</summary>
    partial void OnSelectedYearChanged(int value) => _ = LoadAsync();
    partial void OnSelectedMonthIndexChanged(int value) => _ = LoadAsync();

    // ── داده ──

    /// <summary>ردیف‌های حقوق دوره</summary>
    [ObservableProperty]
    private ObservableCollection<SalaryRow> _rows = new();

    /// <summary>ردیف انتخاب‌شده</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private SalaryRow? _selectedRow;

    /// <summary>آیا ردیفی انتخاب شده</summary>
    public bool HasSelection => SelectedRow is not null;

    /// <summary>جمع خالص دوره — کارت آماری</summary>
    [ObservableProperty]
    private decimal _totalNet;

    /// <summary>شمار کارکنان دوره</summary>
    [ObservableProperty]
    private int _employeeCount;

    /// <summary>بارگذاری فهرست حقوق دوره</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<SalariesResponse>(
                $"salaries?year={SelectedYear}&month={SelectedMonthIndex + 1}");
            var data = response?.Data ?? new List<SalaryDto>();
            Rows = new ObservableCollection<SalaryRow>(data.Select(ToRow));
            TotalNet = data.Sum(r => r.NetAmount);
            EmployeeCount = data.Count;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>تبدیل DTO به ردیف نمایشی</summary>
    private static SalaryRow ToRow(SalaryDto dto) => new(
        dto.Id, dto.EmployeeId, dto.EmployeeName, dto.PersonnelCode,
        FormatRial(dto.BaseSalary), FormatRial(dto.Overtime), FormatRial(dto.Bonus),
        FormatRial(dto.Insurance), FormatRial(dto.Tax), FormatRial(dto.Deductions),
        FormatRial(dto.NetAmount), dto.NetAmount,
        StatusText(dto.Status), dto.Status == 3,
        dto.Status == 1 ? "#1E88E5" : dto.Status == 3 ? "#43A047" : "#FB8C00");

    /// <summary>تولید حقوق دوره — همه کارکنان فعال</summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var response = await _api.PostAsync<GenerateResponse>("salaries/generate", new
            {
                year = SelectedYear,
                month = SelectedMonthIndex + 1
            });

            if (response?.IsSuccess == true)
            {
                _snackbar.Success(response.Message ??
                    $"حقوق دوره {PeriodText} تولید شد");
                await LoadAsync();
            }
            else
            {
                ErrorMessage = _api.LastError ?? "تولید حقوق ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>پرداخت ردیف انتخاب‌شده</summary>
    [RelayCommand]
    private async Task PayAsync()
    {
        if (SelectedRow is null || SelectedRow.IsPaid) return;
        IsBusy = true;
        try
        {
            if (await _api.PostAsync($"salaries/{SelectedRow.Id}/pay", null))
            {
                _snackbar.Success($"حقوق {SelectedRow.EmployeeName} پرداخت‌شده علامت خورد");
                await LoadAsync();
            }
            else
            {
                ErrorMessage = _api.LastError ?? "ثبت پرداخت ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>پیش‌نمایش فیش چاپی — باز کردن پنجره فیش</summary>
    [RelayCommand]
    private async Task ShowPayslipAsync()
    {
        if (SelectedRow is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<PayslipResponse>($"salaries/{SelectedRow.Id}/payslip");
            if (response?.Data is null)
            {
                ErrorMessage = _api.LastError ?? "دریافت فیش ناموفق بود";
                return;
            }

            var d = response.Data;
            var window = new Views.PayslipWindow(
                d.ClinicName, d.EmployeeName, d.PersonnelCode, d.NationalCode,
                d.PeriodText, MonthNames.ElementAtOrDefault(SelectedMonthIndex),
                d.BaseSalary, d.Overtime, d.Bonus,
                d.Insurance, d.Tax, d.Deductions, d.NetAmount);
            window.Owner = System.Windows.Application.Current?.MainWindow;
            window.ShowDialog();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>قالب‌بندی ریال با جداکننده هزارگان</summary>
    private static string FormatRial(decimal amount) => amount.ToString("#,0");

    /// <summary>متن وضعیت</summary>
    private static string StatusText(int status) => status switch
    {
        3 => "پرداخت‌شده", 2 => "تأییدشده", _ => "محاسبه‌شده"
    };

    // ── مدل‌های ردیف و پاسخ ──

    /// <summary>ردیف حقوق دوره</summary>
    public sealed record SalaryRow(
        long Id, long EmployeeId, string EmployeeName, string PersonnelCode,
        string BaseSalary, string Overtime, string Bonus,
        string Insurance, string Tax, string Deductions,
        string NetAmount, decimal NetAmountRaw,
        string StatusText, bool IsPaid, string StatusColor);

    private sealed class SalariesResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<SalaryDto>? Data { get; set; }
    }

    private sealed class SalaryDto
    {
        public long Id { get; set; }
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Overtime { get; set; }
        public decimal Bonus { get; set; }
        public decimal Insurance { get; set; }
        public decimal Tax { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetAmount { get; set; }
        public int Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    private sealed class GenerateResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int Data { get; set; }
    }

    private sealed class PayslipResponse
    {
        public PayslipDataDto? Data { get; set; }
    }

    private sealed class PayslipDataDto
    {
        public long SalaryId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = "—";
        public string NationalCode { get; set; } = "—";
        public string PeriodText { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Overtime { get; set; }
        public decimal Bonus { get; set; }
        public decimal Insurance { get; set; }
        public decimal Tax { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetAmount { get; set; }
    }
}
