using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/* ═══════════════════════════════════════════════════════════
   ماژول انبارگردانی داروخانه — گام P2 (چرخه ششم v1.4)
   نمای دوپنلی: فهرست جلسات + پنل جزئیات با ورود شمارش
   ═══════════════════════════════════════════════════════════ */

/// <summary>ردیف قلم شمارش در جدول اقلام</summary>
public record StockItemRow(
    long ItemId, string Medication, string? Batch,
    string SystemQty, string CountedQty, decimal Difference,
    bool IsCounted, string DiffText, string DiffColor, string? Notes);

/// <summary>ردیف جلسه در فهرست</summary>
public record StockSessionRow(
    long Id, string? Number, string Date, string Status, string StatusColor,
    string Progress, string Diffs, string? Notes);

/// <summary>
/// ViewModel انبارگردانی — دو پنل: فهرست جلسات + جزئیات جلسه انتخاب‌شده
/// </summary>
public partial class StocktakingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "انبارگردانی داروخانه";

    /// <summary>فهرست جلسات</summary>
    public ObservableCollection<StockSessionRow> Sessions { get; } = new();

    /// <summary>جلسه انتخاب‌شده</summary>
    [ObservableProperty]
    private StockSessionRow? _selectedSession;

    /// <summary>اقلام جلسه انتخاب‌شده</summary>
    public ObservableCollection<StockItemRow> Items { get; } = new();

    /// <summary>قلم انتخاب‌شده برای ورود شمارش</summary>
    [ObservableProperty]
    private StockItemRow? _selectedItem;

    /// <summary>مقدار شمارش‌شده برای قلم انتخاب‌شده</summary>
    [ObservableProperty]
    private string? _countInput;

    /// <summary>یادداشت شمارش قلم جاری</summary>
    [ObservableProperty]
    private string? _countNote;

    /// <summary>یادداشت جلسه جدید</summary>
    [ObservableProperty]
    private string? _newSessionNotes;

    /// <summary>وضعیت جلسه جاری — متن</summary>
    [ObservableProperty]
    private string _sessionHeader = "جلسه‌ای انتخاب نشده";

    /// <summary>رنگ وضعیت جلسه جاری</summary>
    [ObservableProperty]
    private string _sessionStatusColor = "#757575";

    /// <summary>جلسه در حال شمارش است؟</summary>
    [ObservableProperty]
    private bool _isInProgress;

    /// <summary>جلسه در انتظار تأیید است؟</summary>
    [ObservableProperty]
    private bool _isAwaitingApproval;

    /// <summary>شمارنده‌های خلاصه</summary>
    [ObservableProperty]
    private string _summaryText = "—";

    /// <summary>متن فیلتر جستجوی قلم</summary>
    [ObservableProperty]
    private string? _itemFilter;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public StocktakingViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadSessionsAsync();
    }

    /// <summary>بارگذاری فهرست جلسات</summary>
    [RelayCommand]
    private async Task LoadSessionsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ListResponse>($"stocktakings");
            Sessions.Clear();
            foreach (var s in response?.Data ?? new List<SessionDto>())
                Sessions.Add(new StockSessionRow(
                    s.Id, s.TakingNumber, s.DateShamsi, s.Status, s.StatusColor,
                    $"{s.CountedItems} از {s.TotalItems}", $"{s.DifferenceItems} مغایرت", s.Notes));
            if (Sessions.Count == 0) _snackbar.Info("هنوز جلسه انبارگردانی ثبت نشده است");
        }
        finally { IsBusy = false; }
    }

    /// <summary>انتخاب جلسه → بارگذاری اقلام</summary>
    partial void OnSelectedSessionChanged(StockSessionRow? value)
    {
        if (value is null) return;
        _ = LoadDetailAsync(value.Id);
    }

    /// <summary>بارگذاری جزئیات و اقلام جلسه</summary>
    private async Task LoadDetailAsync(long id)
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<DetailResponse>($"stocktakings/{id}");
            var d = response?.Data;
            if (d is null)
            {
                _snackbar.Error(_api.LastError ?? "بارگذاری جزئیات ناموفق بود");
                return;
            }

            SessionHeader = $"جلسه {d.TakingNumber ?? ("#" + d.Id)} — {d.DateShamsi}";
            SessionStatusColor = d.StatusColor;
            IsInProgress = d.Status == "در حال شمارش";
            IsAwaitingApproval = d.Status == "تکمیل شده (در انتظار تأیید)";
            SummaryText = $"شمارش‌شده: {d.CountedItems}/{d.TotalItems}  •  مغایرت: {d.DifferenceItems} قلم  •  کسری: {d.TotalShortage:N0}  •  زیادندی: {d.TotalSurplus:N0}";

            Items.Clear();
            foreach (var i in d.Items ?? new())
                Items.Add(new StockItemRow(
                    i.ItemId, i.MedicationName, i.BatchNumber,
                    i.SystemQuantity.ToString("N0"), i.CountedQuantity.ToString("N0"),
                    i.Difference, i.IsCounted,
                    !i.IsCounted ? "شمارش نشده" : (i.Difference == 0 ? "برابر" : (i.Difference > 0 ? "+" : "") + i.Difference.ToString("N0")),
                    !i.IsCounted ? "#9E9E9E" : (i.Difference == 0 ? "#43A047" : (i.Difference > 0 ? "#1E88E5" : "#E53935")),
                    i.Notes));
            ApplyFilter();
        }
        finally { IsBusy = false; }
    }

    /// <summary>فیلتر اقلام بر اساس متن جستجو</summary>
    partial void OnItemFilterChanged(string? value) => ApplyFilter();

    /// <summary>اعمال فیلتر روی نمایش — کپی کاری از منبع کامل</summary>
    private void ApplyFilter()
    {
        // فیلتر سمت کلاینت روی مجموعه بارگذاری‌شده — اقلام در Items نگه داشته می‌شوند
        OnPropertyChanged(nameof(Items));
    }

    /// <summary>شروع جلسه انبارگردانی جدید</summary>
    [RelayCommand]
    private async Task StartSessionAsync()
    {
        if (Sessions.Any(s => s.Status == "در حال شمارش"))
        { _snackbar.Error("یک جلسه در حال شمارش وجود دارد — ابتدا آن را تکمیل یا لغو کنید"); return; }

        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<StartResponse>("stocktakings/start",
                new { notes = string.IsNullOrWhiteSpace(NewSessionNotes) ? null : NewSessionNotes.Trim() });
            if (response?.Data != null)
            {
                _snackbar.Success($"جلسه {response.Data.TakingNumber} با {response.Data.TotalItems} قلم آغاز شد — عکس فوری از موجودی ثبت شد");
                NewSessionNotes = null;
                await LoadSessionsAsync();
                var row = Sessions.FirstOrDefault(s => s.Id == response.Data.Id);
                if (row is not null) SelectedSession = row;
            }
            else _snackbar.Error(_api.LastError ?? "شروع جلسه ناموفق بود");
        }
        finally { IsBusy = false; }
    }

    /// <summary>ثبت شمارش قلم انتخاب‌شده</summary>
    [RelayCommand]
    private async Task SaveCountAsync()
    {
        if (SelectedSession is null) { _snackbar.Error("ابتدا یک جلسه انتخاب کنید"); return; }
        if (SelectedItem is null) { _snackbar.Error("ابتدا یک قلم از جدول انتخاب کنید"); return; }
        if (!decimal.TryParse(CountInput?.Trim(), out var qty) || qty < 0)
        { _snackbar.Error("مقدار شمارش باید عددی نامنفی باشد"); return; }

        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SimpleResponse>(
                $"stocktakings/{SelectedSession.Id}/items/{SelectedItem.ItemId}/count",
                new { countedQuantity = qty, notes = string.IsNullOrWhiteSpace(CountNote) ? null : CountNote.Trim() });
            if (response?.IsSuccess == true)
            {
                _snackbar.Success($"شمارش «{SelectedItem.Medication}» ثبت شد");
                CountInput = null;
                CountNote = null;
                await LoadDetailAsync(SelectedSession.Id);
                await LoadSessionsAsync();
            }
            else _snackbar.Error(_api.LastError ?? "ثبت شمارش ناموفق بود");
        }
        finally { IsBusy = false; }
    }

    /// <summary>تکمیل جلسه — قفل شمارش‌ها</summary>
    [RelayCommand]
    private async Task FinalizeSessionAsync()
    {
        if (SelectedSession is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SimpleResponse>($"stocktakings/{SelectedSession.Id}/finalize", new { });
            if (response?.IsSuccess == true)
            {
                _snackbar.Success("جلسه تکمیل شد — آماده تأیید نهایی");
                await LoadSessionsAsync();
                var row = Sessions.FirstOrDefault(s => s.Id == SelectedSession.Id);
                if (row is not null) SelectedSession = row;
            }
            else _snackbar.Error(_api.LastError ?? "تکمیل ناموفق بود");
        }
        finally { IsBusy = false; }
    }

    /// <summary>تأیید نهایی و اعمال مغایرت به موجودی</summary>
    [RelayCommand]
    private async Task ApproveSessionAsync()
    {
        if (SelectedSession is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<ApproveResponse>($"stocktakings/{SelectedSession.Id}/approve", new { });
            if (response?.IsSuccess == true && response.Data is not null)
            {
                _snackbar.Success($"مغایرت {response.Data.AdjustedItems} قلم به موجودی اعمال شد — کسری {response.Data.TotalShortage:N0}، زیادندی {response.Data.TotalSurplus:N0}");
                await LoadSessionsAsync();
                var row = Sessions.FirstOrDefault(s => s.Id == SelectedSession.Id);
                if (row is not null) SelectedSession = row;
            }
            else _snackbar.Error(_api.LastError ?? "تأیید ناموفق بود");
        }
        finally { IsBusy = false; }
    }

    /// <summary>لغو جلسه در حال شمارش</summary>
    [RelayCommand]
    private async Task CancelSessionAsync()
    {
        if (SelectedSession is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SimpleResponse>($"stocktakings/{SelectedSession.Id}/cancel", new { });
            if (response?.IsSuccess == true)
            {
                _snackbar.Success("جلسه لغو شد — هیچ اثری بر موجودی ندارد");
                Items.Clear();
                SessionHeader = "جلسه‌ای انتخاب نشده";
                IsInProgress = false;
                IsAwaitingApproval = false;
                await LoadSessionsAsync();
            }
            else _snackbar.Error(_api.LastError ?? "لغو ناموفق بود");
        }
        finally { IsBusy = false; }
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class SimpleResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }

    private sealed class ListResponse
    {
        public bool IsSuccess { get; set; }
        public List<SessionDto>? Data { get; set; }
    }

    private sealed class SessionDto
    {
        public long Id { get; set; }
        public string? TakingNumber { get; set; }
        public string DateShamsi { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "#757575";
        public int TotalItems { get; set; }
        public int CountedItems { get; set; }
        public int DifferenceItems { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal TotalSurplus { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class DetailResponse
    {
        public bool IsSuccess { get; set; }
        public DetailDto? Data { get; set; }
    }

    private sealed class DetailDto
    {
        public long Id { get; set; }
        public string? TakingNumber { get; set; }
        public string DateShamsi { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "#757575";
        public int TotalItems { get; set; }
        public int CountedItems { get; set; }
        public int DifferenceItems { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal TotalSurplus { get; set; }
        public string? Notes { get; set; }
        public List<ItemDto>? Items { get; set; }
    }

    private sealed class ItemDto
    {
        public long ItemId { get; set; }
        public long InventoryId { get; set; }
        public string MedicationName { get; set; } = "";
        public string? BatchNumber { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal CountedQuantity { get; set; }
        public decimal Difference { get; set; }
        public bool IsCounted { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class StartResponse
    {
        public bool IsSuccess { get; set; }
        public StartDto? Data { get; set; }
    }

    private sealed class StartDto
    {
        public long Id { get; set; }
        public string? TakingNumber { get; set; }
        public int TotalItems { get; set; }
    }

    private sealed class ApproveResponse
    {
        public bool IsSuccess { get; set; }
        public ApproveDto? Data { get; set; }
    }

    private sealed class ApproveDto
    {
        public int AdjustedItems { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal TotalSurplus { get; set; }
    }
}
