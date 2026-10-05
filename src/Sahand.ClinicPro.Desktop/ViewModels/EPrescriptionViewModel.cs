using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف نسخه الکترونیک</summary>
public record ErxRow(long Id, long PrescriptionId, string? TrackingCode,
    string StatusText, string StatusColor, bool IsSuccess, DateTime? SentAt);

/// <summary>
/// ViewModel نسخه الکترونیک — گام U18 طبق فرم بخش ۵:
/// آمار + پایش + بازارسال
/// </summary>
public partial class EPrescriptionViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نسخه الکترونیک";

    /// <summary>متن کارت کل</summary>
    [ObservableProperty]
    private string _totalText = "۰";

    /// <summary>موفق</summary>
    [ObservableProperty]
    private string _successText = "۰";

    /// <summary>ناموفق</summary>
    [ObservableProperty]
    private string _failedText = "۰";

    /// <summary>امروز</summary>
    [ObservableProperty]
    private string _todayText = "۰";

    /// <summary>نرخ موفقیت</summary>
    [ObservableProperty]
    private string _successRateText = "۰٪";

    /// <summary>فهرست نسخه‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<ErxRow> _prescriptions = new();

    /// <summary>انتخاب‌شده</summary>
    [ObservableProperty]
    private ErxRow? _selectedPrescription;

    /// <summary>سازنده</summary>
    public EPrescriptionViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری آمار و فهرست</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var stats = await _api.GetAsync<StatsResponse>("government/eprescription/stats");
            var s = stats?.Data;
            if (s is not null)
            {
                TotalText = $"{s.Total:N0}";
                SuccessText = $"{s.Success:N0}";
                FailedText = $"{s.Failed:N0}";
                TodayText = $"{s.TodayCount:N0}";
                SuccessRateText = $"{s.SuccessRate:0.#}٪";
            }

            var list = await _api.GetAsync<ListResponse>("government/eprescription/list?page=1&pageSize=100");
            Prescriptions = new ObservableCollection<ErxRow>(
                (list?.Data?.Items ?? new List<ErxDto>()).Select(MapRow));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>نگاشت ردیف</summary>
    private static ErxRow MapRow(ErxDto e)
    {
        var (statusText, color) = (e.IsSuccess, e.Status) switch
        {
            (true, _) => ("✅ ارسال موفق", "#2E7D32"),
            (false, "Rejected") => ("❌ رد شده", "#C62828"),
            (false, _) => ("⏳ در انتظار/خطا", "#F9A825")
        };
        return new ErxRow(e.Id, e.PrescriptionId, e.TrackingCode, statusText, color, e.IsSuccess, e.SentAt);
    }

    /// <summary>بازارسال نسخه ناموفق</summary>
    [RelayCommand]
    private async Task ResendAsync(ErxRow? row)
    {
        row ??= SelectedPrescription;
        if (row is null) { _snackbar.Info("ابتدا یک نسخه انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PostAsync($"government/eprescription/{row.Id}/resend", null);
        IsBusy = false;
        if (ok) { _snackbar.Success("نسخه بازارسال شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "بازارسال ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class StatsResponse
    {
        public bool IsSuccess { get; set; }
        public StatsData? Data { get; set; }
    }

    private sealed class StatsData
    {
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public int TodayCount { get; set; }
        public double SuccessRate { get; set; }
    }

    private sealed class ListResponse
    {
        public bool IsSuccess { get; set; }
        public ListPage? Data { get; set; }
    }

    private sealed class ListPage
    {
        public List<ErxDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private sealed class ErxDto
    {
        public long Id { get; set; }
        public long PrescriptionId { get; set; }
        public string? TrackingCode { get; set; }
        public string Status { get; set; } = "";
        public bool IsSuccess { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
