using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel داشبورد — بارگذاری آمار امروز از API
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "داشبورد";

    [ObservableProperty] private int _newPatients;
    [ObservableProperty] private int _todayAppointments;
    [ObservableProperty] private int _todayVisits;
    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private string _todayDateShamsi = string.Empty;
    [ObservableProperty] private string _connectionStatus = "در حال بررسی اتصال...";

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public DashboardViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadStatsAsync();
    }

    /// <summary>درآمد به‌صورت متنی جدا شده با کاما</summary>
    public string TodayRevenueText => TodayRevenue.ToString("N0");

    /// <summary>بارگذاری آمار امروز</summary>
    public async Task LoadStatsAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var stats = await _api.GetAsync<DashboardStats>("dashboard/stats");

        if (stats is not null)
        {
            NewPatients = stats.NewPatients;
            TodayAppointments = stats.TodayAppointments;
            TodayVisits = stats.TodayVisits;
            TodayRevenue = stats.TodayRevenue;
            TodayDateShamsi = stats.TodayDateShamsi;
            ConnectionStatus = "اتصال به سرور برقرار است";
        }
        else
        {
            ConnectionStatus = "اتصال به سرور برقرار نیست";
            ErrorMessage = _api.LastError ?? "دریافت آمار ناموفق بود";
        }

        IsBusy = false;
        OnPropertyChanged(nameof(TodayRevenueText));
    }

    /// <summary>ساختار پاسخ آمار داشبورد — منطبق با API</summary>
    public sealed record DashboardStats(
        int NewPatients,
        int TodayAppointments,
        int ActiveAppointments,
        int TodayVisits,
        decimal TodayRevenue,
        string TodayDateShamsi);
}
