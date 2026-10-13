using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

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

    /// <summary>
    /// ═══ چرخهٔ ۷۰ (گزارش مالک) — «همه‌چیز صفر، کارت نمودار خالی» ═══
    /// ریشه: شکست بارگذاری (شایع‌ترین علت: ماژول Reports در لایسنس فعال
    /// نیست → ۴۰۳ از میدلور لایسنس) پیش‌تر «بی‌صدا» بود — کارت‌ها ۰ نشان
    /// می‌دادند و نمودارها خالی می‌ماندند. اکنون: پرچم LoadFailed کارت‌ها
    /// را به «—» می‌شاند، بنر خطا با علت فارسی (از LastError — همان پیام
    /// میدلور: «ماژول Reports در لایسنس شما فعال نیست») بالا می‌آید و
    /// دکمهٔ «تلاش دوباره» دارد.
    /// </summary>
    [ObservableProperty]
    private bool _loadFailed;

    /// <summary>نمایش جمع درآمد — «—» در شکست به‌جای ۰ گمراه‌کننده</summary>
    public string TotalRevenueDisplay => LoadFailed ? "—" : Money.Format(TotalRevenue);

    /// <summary>نمایش جمع وصولی</summary>
    public string TotalPaidDisplay => LoadFailed ? "—" : Money.Format(TotalPaid);

    /// <summary>نمایش جمع مانده</summary>
    public string TotalRemainingDisplay => LoadFailed ? "—" : Money.Format(TotalRemaining);

    /// <summary>نمایش جمع ویزیت‌ها</summary>
    public string TotalVisitsDisplay => LoadFailed
        ? "—"
        : Money.ToPersianDigits(TotalVisits.ToString());

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

        // چرخهٔ ۷۰ — هر دو گزارش باید بیایند؛ شکست هرکدام = بنر خطا + «—»
        var revenueOk = revenue?.Data is not null;
        var perfOk = perf?.Data is not null;
        LoadFailed = !revenueOk || !perfOk;
        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalPaidDisplay));
        OnPropertyChanged(nameof(TotalRemainingDisplay));
        OnPropertyChanged(nameof(TotalVisitsDisplay));

        if (revenueOk && revenue!.Data is not null)
        {
            var rows = revenue.Data;
            var max = Math.Max(1m, rows.Count == 0 ? 1m : rows.Max(r => r.Total));
            Revenue = new ObservableCollection<RevenuePoint>(
                rows.Select(r => new RevenuePoint(
                    r.DateShamsi, r.Total, r.Paid, r.Remaining,
                    (double)(r.Total / max) * MaxBarHeight)));
            TotalRevenue = rows.Sum(r => r.Total);
            TotalPaid = rows.Sum(r => r.Paid);
            TotalRemaining = rows.Sum(r => r.Remaining);
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت گزارش درآمد ناموفق بود";
        }

        if (perfOk && perf!.Data is not null)
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

    /* ─── چیپ‌های بازهٔ چرخهٔ ۷۰ — سه سوییچ خوانا به‌جای کمبوی عددی ─── */

    /// <summary>بازهٔ هفت‌روزه</summary>
    public bool IsWeekRange
    {
        get => RangeIndex == 0;
        set { if (value && RangeIndex != 0) RangeIndex = 0; OnPropertyChanged(nameof(IsWeekRange)); }
    }

    /// <summary>بازهٔ سی‌روزه</summary>
    public bool IsMonthRange
    {
        get => RangeIndex == 1;
        set { if (value && RangeIndex != 1) RangeIndex = 1; OnPropertyChanged(nameof(IsMonthRange)); }
    }

    /// <summary>بازهٔ نودروزه</summary>
    public bool IsQuarterRange
    {
        get => RangeIndex == 2;
        set { if (value && RangeIndex != 2) RangeIndex = 2; OnPropertyChanged(nameof(IsQuarterRange)); }
    }

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