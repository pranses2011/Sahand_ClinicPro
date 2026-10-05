using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تراکنش سپاس</summary>
public record SepasTransactionRow(long Id, string Patient, string TypeText, string? ReferenceId,
    string StatusText, string StatusColor, int RetryCount, DateTime? SentAt, string? Error);

/// <summary>ردیف لاگ API دولتی</summary>
public record ApiCallLogRow(long Id, string? Service, string? Endpoint, string? Method,
    int? StatusCode, int? DurationMs, DateTime? CalledAt, bool IsSuccess, string? Request, string? Response);

/// <summary>
/// ViewModel سامانه سپاس — گام U18 طبق فرم بخش ۵ (۵ تب):
/// وضعیت + تراکنش‌ها + دریافتی‌ها + تنظیمات + لاگ API
/// </summary>
public partial class SepasViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "سامانه سپاس";

    /// <summary>زبانه فعال</summary>
    [ObservableProperty]
    private int _tabIndex;

    /* ─── تب ۱: وضعیت ─── */

    /// <summary>کل تراکنش‌ها</summary>
    [ObservableProperty]
    private string _totalText = "۰";

    /// <summary>همگام‌شده</summary>
    [ObservableProperty]
    private string _syncedText = "۰";

    /// <summary>در انتظار</summary>
    [ObservableProperty]
    private string _pendingText = "۰";

    /// <summary>ناموفق</summary>
    [ObservableProperty]
    private string _failedText = "۰";

    /// <summary>امروز</summary>
    [ObservableProperty]
    private string _todayText = "۰";

    /// <summary>آخرین همگام‌سازی</summary>
    [ObservableProperty]
    private string _lastSyncText = "—";

    /* ─── تب ۲: تراکنش‌ها ─── */

    /// <summary>تراکنش‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<SepasTransactionRow> _transactions = new();

    /// <summary>تراکنش انتخاب‌شده</summary>
    [ObservableProperty]
    private SepasTransactionRow? _selectedTransaction;

    /* ─── تب ۵: لاگ API ─── */

    /// <summary>لاگ‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<ApiCallLogRow> _apiLogs = new();

    /// <summary>لاگ انتخاب‌شده</summary>
    [ObservableProperty]
    private ApiCallLogRow? _selectedApiLog;

    /// <summary>سازنده</summary>
    public SepasViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadStatsAsync();
    }

    /// <summary>بارگذاری داده زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        if (value == 0) _ = LoadStatsAsync();
        else if (value == 1) _ = LoadTransactionsAsync();
        else if (value == 4) _ = LoadApiLogsAsync();
    }

    /// <summary>بارگذاری آمار وضعیت</summary>
    [RelayCommand]
    public async Task LoadStatsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<StatsResponse>("government/sepas/stats");
            var s = response?.Data;
            if (s is null) return;
            TotalText = $"{s.Total:N0}";
            SyncedText = $"{s.Synced:N0}";
            PendingText = $"{s.Pending:N0}";
            FailedText = $"{s.Failed:N0}";
            TodayText = $"{s.TodayCount:N0}";
            LastSyncText = s.LastSyncAt is null ? "—"
                : Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(s.LastSyncAt.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بارگذاری تراکنش‌ها</summary>
    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<TransactionsResponse>("government/sepas/transactions?page=1&pageSize=100");
            Transactions = new ObservableCollection<SepasTransactionRow>(
                (response?.Data?.Items ?? new List<TransactionDto>()).Select(MapTransaction));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>نگاشت ردیف تراکنش</summary>
    private static SepasTransactionRow MapTransaction(TransactionDto t)
    {
        var (statusText, color) = t.Status switch
        {
            "Synced" => ("✅ همگام", "#2E7D32"),
            "Pending" => ("⏳ در انتظار", "#F9A825"),
            "Failed" => ("❌ ناموفق", "#C62828"),
            _ => (t.Status, "#90A4AE")
        };
        return new SepasTransactionRow(t.Id, t.Patient, TranslateType(t.Type), t.ReferenceId,
            statusText, color, t.RetryCount, t.SentAt, t.Error);
    }

    /// <summary>ترجمه نوع تراکنش</summary>
    private static string TranslateType(string type) => type switch
    {
        "Encounter" => "ثبت ویزیت", "Payment" => "پرداخت",
        "Referral" => "ارجاع", _ => type
    };

    /// <summary>بازارسال تراکنش ناموفق</summary>
    [RelayCommand]
    private async Task RetryAsync(SepasTransactionRow? row)
    {
        row ??= SelectedTransaction;
        if (row is null) { _snackbar.Info("ابتدا یک تراکنش انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PostAsync($"government/sepas/transactions/{row.Id}/retry", null);
        IsBusy = false;
        if (ok) { _snackbar.Success("تراکنش بازارسال شد"); await LoadTransactionsAsync(); }
        else _snackbar.Error(_api.LastError ?? "بازارسال ناموفق بود");
    }

    /// <summary>بارگذاری لاگ API</summary>
    [RelayCommand]
    public async Task LoadApiLogsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ApiLogsResponse>("government/api-call-logs?page=1&pageSize=100");
            ApiLogs = new ObservableCollection<ApiCallLogRow>(
                (response?.Data?.Items ?? new List<ApiLogDto>()).Select(l => new ApiCallLogRow(
                    l.Id, l.ServiceName, l.Endpoint, l.Method, l.StatusCode, l.Duration,
                    l.CalledAt, l.IsSuccess, l.RequestBody, l.ResponseBody)));
        }
        finally
        {
            IsBusy = false;
        }
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
        public int Synced { get; set; }
        public int Pending { get; set; }
        public int Failed { get; set; }
        public int TodayCount { get; set; }
        public DateTime? LastSyncAt { get; set; }
    }

    private sealed class TransactionsResponse
    {
        public bool IsSuccess { get; set; }
        public TransactionsPage? Data { get; set; }
    }

    private sealed class TransactionsPage
    {
        public List<TransactionDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private sealed class TransactionDto
    {
        public long Id { get; set; }
        public string Patient { get; set; } = "";
        public string Type { get; set; } = "";
        public string? ReferenceId { get; set; }
        public string Status { get; set; } = "";
        public int RetryCount { get; set; }
        public DateTime? SentAt { get; set; }
        public string? Error { get; set; }
    }

    private sealed class ApiLogsResponse
    {
        public bool IsSuccess { get; set; }
        public ApiLogsPage? Data { get; set; }
    }

    private sealed class ApiLogsPage
    {
        public List<ApiLogDto> Items { get; set; } = new();
    }

    private sealed class ApiLogDto
    {
        public long Id { get; set; }
        public string? ServiceName { get; set; }
        public string? Endpoint { get; set; }
        public string? Method { get; set; }
        public int? StatusCode { get; set; }
        public int? Duration { get; set; }
        public DateTime? CalledAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
    }
}
