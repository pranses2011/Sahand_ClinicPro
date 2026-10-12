using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف درآمد روزانه — با ارتفاع نرمال‌شده نمودار</summary>
public record RevenuePoint(
    string Date,
    decimal Total,
    decimal Paid,
    decimal Remaining,
    double BarHeight);

/// <summary>ردیف عملکرد پزشک</summary>
public record DoctorPerformanceRow(
    string DoctorName,
    int VisitsCount,
    decimal TotalRevenue,
    decimal DoctorShare,
    double BarHeight);

/// <summary>
/// ViewModel گزارش‌ها — درآمد روزانه و عملکرد پزشکان با بازه قابل انتخاب
/// </summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private const double MaxBarHeight = 140;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "گزارش‌ها";

    /// <summary>نمایه بازه — ۰: هفته، ۱: ماه، ۲: فصل</summary>
    [ObservableProperty]
    private int _rangeIndex = 1;

    /// <summary>نقاط درآمد روزانه</summary>
    [ObservableProperty]
    private ObservableCollection<RevenuePoint> _revenue = new();

    /// <summary>عملکرد پزشکان</summary>
    [ObservableProperty]
    private ObservableCollection<DoctorPerformanceRow> _performance = new();

    /// <summary>جمع درآمد بازه</summary>
    [ObservableProperty]
    private decimal _totalRevenue;

    /// <summary>جمع وصولی</summary>
    [ObservableProperty]
    private decimal _totalPaid;

    /// <summary>جمع مانده</summary>
    [ObservableProperty]
    private decimal _totalRemaining;

    /// <summary>جمع ویزیت‌ها</summary>
    [ObservableProperty]
    private int _totalVisits;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public ReportsViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    /// <summary>بازه انتخاب‌شده به روز</summary>
    private int Days => RangeIndex switch { 0 => 7, 1 => 30, 2 => 90, _ => 30 };

    /// <summary>بارگذاری هر دو گزارش</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var revenueTask = _api.GetAsync<RevenueResponse>($"reports/daily-revenue?days={Days}");
        var perfTask = _api.GetAsync<PerformanceResponse>($"reports/doctor-performance?days={Days}");
        await Task.WhenAll(revenueTask, perfTask);

        // چرخهٔ ۵۰ (BUG-03): خواندن نتیجهٔ هر دو Task با await — پیش‌تر نتیجه‌ها
        // به‌صورت هم‌گام و مسدودکننده از Taskها بیرون کشیده می‌شد که در بدترین
        // حالت (خطای شبکه) استثنای درون‌پیچیده به UI می‌رسید و رابط کاربری را
        // هنگ می‌داد؛ await هم استثنا را عادی می‌کند و هم رشتهٔ UI را قفل نمی‌کند.
        var revenue = await revenueTask;
        var perf = await perfTask;

        if (revenue?.Data is not null)
        {
            var max = Math.Max(1m, revenue.Data.Count == 0 ? 1m : revenue.Data.Max(r => r.Total));
            Revenue = new ObservableCollection<RevenuePoint>(
                revenue.Data.Select(r => new RevenuePoint(
                    r.DateShamsi, r.Total, r.Paid, r.Remaining,
                    (double)(r.Total / max) * MaxBarHeight)));
            TotalRevenue = revenue.Data.Sum(r => r.Total);
            TotalPaid = revenue.Data.Sum(r => r.Paid);
            TotalRemaining = revenue.Data.Sum(r => r.Remaining);
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت گزارش درآمد ناموفق بود";
        }

        if (perf?.Data is not null)
        {
            var max = Math.Max(1m, perf.Data.Count == 0 ? 1m : perf.Data.Max(p => p.TotalRevenue));
            Performance = new ObservableCollection<DoctorPerformanceRow>(
                perf.Data.Select(p => new DoctorPerformanceRow(
                    p.DoctorName, p.VisitsCount, p.TotalRevenue, p.DoctorShare,
                    (double)(p.TotalRevenue / max) * MaxBarHeight)));
            TotalVisits = perf.Data.Sum(p => p.VisitsCount);
        }

        IsBusy = false;
    }

    /// <summary>تغییر بازه — بارگذاری مجدد</summary>
    partial void OnRangeIndexChanged(int value) => _ = LoadAsync();

    /// <summary>پاسخ درآمد</summary>
    public sealed class RevenueResponse
    {
        public bool IsSuccess { get; set; }
        public List<RevenueDto>? Data { get; set; }
    }

    /// <summary>DTO درآمد روزانه</summary>
    public sealed class RevenueDto
    {
        public string DateShamsi { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }

    /// <summary>پاسخ عملکرد</summary>
    public sealed class PerformanceResponse
    {
        public bool IsSuccess { get; set; }
        public List<PerformanceDto>? Data { get; set; }
    }

    /// <summary>DTO عملکرد پزشک</summary>
    public sealed class PerformanceDto
    {
        public long DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int VisitsCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal DoctorShare { get; set; }
    }
}