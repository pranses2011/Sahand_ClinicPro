using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Controls.Charts;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف لیست نوبت‌های پیش‌رو</summary>
public record UpcomingAppointmentRow(
    long Id, int? QueueNumber, string Time, string PatientName,
    string DoctorName, string StatusText, string Status);

/// <summary>ردیف جدول فعالیت‌های اخیر</summary>
public record RecentActivityRow(string TimeShamsi, string User, string Action, string Entity);

/// <summary>
/// ویومدل داشبورد — گام U1 طبق پرامپت UI بخش ۱ (فرم ۲):
/// ۴ کارت آمار اصلی + ۳ کارت فرعی + نمودار درآمد + نوبت‌های پیش‌رو + فعالیت‌های اخیر
/// به‌روزرسانی خودکار هر ۶۰ ثانیه (Timer)
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly System.Windows.Threading.DispatcherTimer _refresh =
        new() { Interval = TimeSpan.FromSeconds(60) };

    /// <summary>عنوان صفحه در هدر</summary>
    public override string Title => "داشبورد";

    /* ── آمار اصلی (ردیف اول) ── */
    [ObservableProperty] private int _todayPatients;
    [ObservableProperty] private int _todayVisits;
    [ObservableProperty] private int _waitingCount;
    [ObservableProperty] private string _todayRevenueText = "0";

    /* ── آمار فرعی (ردیف دوم) ── */
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _todayPrescriptions;
    [ObservableProperty] private int _pendingLabOrders;

    /* ── عمومی ── */
    [ObservableProperty] private string _welcomeMessage = string.Empty;
    [ObservableProperty] private string _todayDateShamsi = string.Empty;
    [ObservableProperty] private string _connectionStatus = "در حال بارگذاری…";
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>داده نمودار درآمد ۷ روز — نمودار سطحی (نوع درست برای روند زمانی)</summary>
    public ObservableCollection<ChartPointData> RevenueChart { get; } = new();

    /// <summary>نوبت‌های پیش‌رو امروز</summary>
    public ObservableCollection<UpcomingAppointmentRow> UpcomingAppointments { get; } = new();

    /// <summary>آخرین فعالیت‌ها</summary>
    public ObservableCollection<RecentActivityRow> RecentActivities { get; } = new();

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public DashboardViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _refresh.Tick += async (_, _) => await LoadStatsAsync();
        _refresh.Start();
        _ = LoadStatsAsync();
    }

    /// <summary>بارگذاری کامل همه بخش‌ها — طبق LoadAsync پرامپت</summary>
    public async Task LoadStatsAsync()
    {
        WelcomeMessage = $"خوش آمدید! امروز {DateTime.Now:HH:mm} — وضعیت درمانگاه به‌روزرسانی شد.";
        TodayDateShamsi = Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.Now);
        ErrorMessage = string.Empty;

        // ۱) آمار خلاصه
        var stats = await _api.GetAsync<JsonElement>("dashboard/stats");
        if (stats.ValueKind == JsonValueKind.Object)
        {
            TodayPatients = stats.TryGetProperty("todayAppointments", out var appts) ? appts.GetInt32() : 0;
            TodayVisits = stats.TryGetProperty("todayVisits", out var visits) ? visits.GetInt32() : 0;
            WaitingCount = Math.Max(0, TodayPatients - TodayVisits);
            var revenue = stats.TryGetProperty("todayRevenue", out var rev) && rev.ValueKind != JsonValueKind.Null
                ? rev.GetDecimal() : 0;
            TodayRevenueText = revenue.ToString("N0");
            ConnectionStatus = "آمار به‌روز شد";
        }

        // ۲) هشدارها (موجودی کم و آزمایش در انتظار)
        var alerts = await _api.GetAsync<JsonElement>("dashboard/alerts");
        if (alerts.ValueKind == JsonValueKind.Object)
        {
            LowStockCount = alerts.TryGetProperty("lowStock", out var low) ? low.GetInt32() : 0;
            PendingLabOrders = alerts.TryGetProperty("pendingLabOrders", out var lab) ? lab.GetInt32() : 0;
        }

        // ۳) نمودار درآمد ۷ روز — چرخه یازدهم: میله‌ای سطح‌خورده بود؛
        // برای «روند در طول زمان» نوع درست نمودار، سطحی/خطی است
        var chart = await _api.GetAsync<JsonElement>("dashboard/revenue-chart?days=7");
        RevenueChart.Clear();
        if (chart.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in chart.EnumerateArray())
            {
                var label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "—" : "—";
                var amount = item.TryGetProperty("amount", out var a) && a.ValueKind != JsonValueKind.Null
                    ? (double)a.GetDecimal() : 0;
                RevenueChart.Add(new ChartPointData(label, amount, $"{label}: {amount:N0} ریال"));
            }
        }

        // ۴) نوبت‌های امروز
        var appointments = await _api.GetAsync<JsonElement>("dashboard/appointments-today?limit=8");
        UpcomingAppointments.Clear();
        if (appointments.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in appointments.EnumerateArray())
            {
                UpcomingAppointments.Add(new UpcomingAppointmentRow(
                    a.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                    a.TryGetProperty("queueNumber", out var q) && q.ValueKind != JsonValueKind.Null ? q.GetInt32() : null,
                    a.TryGetProperty("time", out var t) ? t.GetString() ?? "" : "",
                    a.TryGetProperty("patientName", out var p) ? p.GetString() ?? "" : "",
                    a.TryGetProperty("doctorName", out var d) ? d.GetString() ?? "" : "",
                    a.TryGetProperty("statusText", out var s) ? s.GetString() ?? "" : "",
                    a.TryGetProperty("status", out var st) ? st.GetString() ?? "" : ""));
            }
        }

        // ۵) فعالیت‌های اخیر
        var activities = await _api.GetAsync<JsonElement>("dashboard/recent-activities?limit=10");
        RecentActivities.Clear();
        if (activities.ValueKind == JsonValueKind.Array)
        {
            foreach (var act in activities.EnumerateArray())
            {
                RecentActivities.Add(new RecentActivityRow(
                    act.TryGetProperty("timeShamsi", out var ts) ? ts.GetString() ?? "" : "",
                    act.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "",
                    act.TryGetProperty("action", out var ac) ? ac.GetString() ?? "" : "",
                    act.TryGetProperty("entity", out var en) ? en.GetString() ?? "" : ""));
            }
        }
    }

    /// <summary>بارگذاری مجدد دستی (F5)</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadStatsAsync();
        _snackbar.Success("داشبورد به‌روزرسانی شد");
    }
}
