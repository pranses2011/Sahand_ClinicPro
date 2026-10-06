using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تاریخچه شیفت — چرخه ۱۳ (درخواست ۲-۲)</summary>
public record ShiftHistoryRow(
    string ReceiptNumber,
    string RegisterName,
    string ClosedText,
    string ClosedByName,
    string TotalCash,
    string TotalCard,
    string TotalOnline,
    string Difference,
    string DifferenceState,
    string DifferenceColor);

/// <summary>
/// ViewModel بستن شیفت صندوق و تحویل — چرخه ۱۳ (درخواست ۲-۲)
/// شمارش فیزیکی نقد/کارت ↔ سیستم + اختلاف‌سنجی + رسید + تاریخچه
/// </summary>
public partial class ShiftCloseViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>شمارش فیزیکی نقد (ریال)</summary>
    [ObservableProperty]
    private decimal _countedCash;

    /// <summary>شمارش فیزیکی کارت (ریال)</summary>
    [ObservableProperty]
    private decimal _countedCard;

    /// <summary>یادداشت تحویل</summary>
    [ObservableProperty]
    private string? _handoverNote;

    /// <summary>نمایه تحویل‌گیرنده از فهرست کاربران</summary>
    [ObservableProperty]
    private int _handoverToIndex = -1;

    /// <summary>گزینه‌های تحویل‌گیرنده (کاربران صندوقدار)</summary>
    public ObservableCollection<UserOption> Users { get; } = new();

    /// <summary>تاریخچه شیفت‌ها</summary>
    public ObservableCollection<ShiftHistoryRow> History { get; } = new();

    /// <summary>رسید آخرین بستن — خلاصه متنی</summary>
    [ObservableProperty]
    private string? _receiptText;

    /// <summary>رنگ وضعیت اختلاف</summary>
    [ObservableProperty]
    private string _differenceState = "—";

    /// <summary>رنگ وضعیت</summary>
    [ObservableProperty]
    private string _differenceColor = "#78909C";

    /// <summary>مشغول</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>گزینه کاربر برای تحویل</summary>
    public record UserOption(long Id, string DisplayName);

    /// <summary>سازنده</summary>
    public ShiftCloseViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری کاربران و تاریخچه</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        var users = await _api.GetAsync<UsersEnvelope>("users?pageSize=50");
        Users.Clear();
        foreach (var u in users?.Data ?? new List<UserDto>())
            Users.Add(new UserOption(u.Id, $"{u.FullName} ({u.Username})"));

        var shifts = await _api.GetAsync<ShiftsEnvelope>("payments/shifts");
        History.Clear();
        foreach (var s in shifts?.Data ?? new List<ShiftDto>())
            History.Add(new ShiftHistoryRow(
                s.ReceiptNumber,
                s.RegisterName,
                s.ClosedText,
                s.ClosedByName,
                $"{s.TotalCash:N0}",
                $"{s.TotalCard:N0}",
                $"{s.TotalOnline:N0}",
                $"{s.Difference:N0}",
                s.DifferenceState,
                s.DifferenceColor));

        IsBusy = false;
    }

    /// <summary>بستن شیفت و صدور رسید</summary>
    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        if (CountedCash < 0 || CountedCard < 0)
        {
            _snackbar.Error("شمارش فیزیکی نمی‌تواند منفی باشد");
            return;
        }

        IsBusy = true;
        var response = await _api.PostAsync<ReceiptEnvelope>("payments/shifts/close", new
        {
            countedCash = CountedCash,
            countedCard = CountedCard,
            handoverToUserId = HandoverToIndex >= 0 && HandoverToIndex < Users.Count
                ? Users[HandoverToIndex].Id
                : (long?)null,
            handoverNote = HandoverNote
        });
        IsBusy = false;

        var r = response?.Data;
        if (r is null)
        {
            _snackbar.Error(_api.LastError ?? "بستن شیفت ناموفق بود");
            return;
        }

        DifferenceState = r.DifferenceState;
        DifferenceColor = r.DifferenceColor;
        ReceiptText =
            $"🧾 رسید {r.ReceiptNumber} — {r.RegisterName}\n" +
            $"بازه: {r.OpenedText} تا {r.ClosedText} | بستن‌دهنده: {r.ClosedByName}\n" +
            $"سیستم — نقد {r.TotalCash:N0} | کارت {r.TotalCard:N0} | آنلاین {r.TotalOnline:N0} | جمع {r.SystemTotal:N0}\n" +
            $"شمارش — نقد {r.CountedCash:N0} | کارت {r.CountedCard:N0} | جمع {r.CountedTotal:N0}\n" +
            $"اختلاف نقد: {r.CashDifference:N0} | کارت: {r.CardDifference:N0} | خالص: {r.NetDifference:N0} ریال ({r.DifferenceState})\n" +
            (string.IsNullOrWhiteSpace(r.HandoverNote) ? "" : $"یادداشت: {r.HandoverNote}\n") +
            $"تعداد تراکنش‌های شیفت: {r.PaymentCount}";

        _snackbar.Success($"شیفت بسته شد — رسید {r.ReceiptNumber} صادر شد");
        await LoadAsync();
    }

    /* ─── DTOهای سبک ─── */

    /// <summary>پاسخ فهرست کاربران</summary>
    public sealed class UsersEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<UserDto>? Data { get; set; }
    }

    /// <summary>ردیف کاربر</summary>
    public sealed class UserDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string? FullName { get; set; }
    }

    /// <summary>پاسخ تاریخچه شیفت‌ها</summary>
    public sealed class ShiftsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<ShiftDto>? Data { get; set; }
    }

    /// <summary>ردیف شیفت</summary>
    public sealed class ShiftDto
    {
        public string ReceiptNumber { get; set; } = "";
        public string RegisterName { get; set; } = "";
        public string ClosedText { get; set; } = "";
        public string ClosedByName { get; set; } = "";
        public decimal TotalCash { get; set; }
        public decimal TotalCard { get; set; }
        public decimal TotalOnline { get; set; }
        public decimal Difference { get; set; }
        public string DifferenceState { get; set; } = "";
        public string DifferenceColor { get; set; } = "#78909C";
    }

    /// <summary>پاسخ رسید</summary>
    public sealed class ReceiptEnvelope
    {
        public bool IsSuccess { get; set; }
        public ReceiptDto? Data { get; set; }
    }

    /// <summary>رسید بستن شیفت</summary>
    public sealed class ReceiptDto
    {
        public long ShiftId { get; set; }
        public string ReceiptNumber { get; set; } = "";
        public string RegisterName { get; set; } = "";
        public string OpenedText { get; set; } = "";
        public string ClosedText { get; set; } = "";
        public string ClosedByName { get; set; } = "";
        public decimal TotalCash { get; set; }
        public decimal TotalCard { get; set; }
        public decimal TotalOnline { get; set; }
        public decimal SystemTotal { get; set; }
        public decimal CountedCash { get; set; }
        public decimal CountedCard { get; set; }
        public decimal CountedTotal { get; set; }
        public decimal CashDifference { get; set; }
        public decimal CardDifference { get; set; }
        public decimal NetDifference { get; set; }
        public string DifferenceState { get; set; } = "";
        public string DifferenceColor { get; set; } = "#78909C";
        public string? HandoverNote { get; set; }
        public int PaymentCount { get; set; }
    }
}
