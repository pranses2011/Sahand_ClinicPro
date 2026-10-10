using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

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

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — مبالغ سیستم و مغایرت زنده ═══
    // «فقط کادرهایی هستن که خودمون بنویسیم در حالی که باید مبالغ رو بیاره و
    // ما هم شمارش رو بنویسیم تا مغایرت رو نشون بده و بعد ثبت بکنه» — مبالغ
    // ثبت‌شدهٔ سیستم (فروش نقدی/کارتخوان/آنلاین امروز) از payments/today-summary
    // خوانده و کنار ورودی شمارش نشان داده می‌شود؛ مغایرت همان لحظه که شمارش
    // تایپ می‌شود محاسبه و رنگ می‌شود؛ رقم نهایی معتبر همچنان از رسید سرور
    // پس از بستن می‌آید (زیرا ممکن است هم‌زمان تراکنش جدیدی ثبت شود).

    /// <summary>فروش نقدی امروز بر اساس سیستم</summary>
    [ObservableProperty]
    private decimal _systemCash;

    /// <summary>فروش کارتخوان امروز بر اساس سیستم</summary>
    [ObservableProperty]
    private decimal _systemCard;

    /// <summary>پرداخت آنلاین امروز بر اساس سیستم</summary>
    [ObservableProperty]
    private decimal _systemOnline;

    /// <summary>خالص امروز بر اساس سیستم (نقد + کارت + آنلاین)</summary>
    [ObservableProperty]
    private decimal _systemNet;

    /// <summary>تعداد فاکتورهای امروز بر اساس سیستم</summary>
    [ObservableProperty]
    private int _systemInvoiceCount;

    // ═══ چرخهٔ ۴۳ (گزارش مالک: «در بستن شیفت و تحویل صندوق، مبالغ رو درست
    // نشون نمیده») — همهٔ نمایش‌های مبلغ از Money.Format می‌گذرند: ارقام فارسی،
    // جداکنندهٔ هزارگان و مهم‌تر از همه «تبدیل واحد نمایشی» (در حالت تومان،
    // مبالغ ریالیِ سرور بر ۱۰ تقسیم و با برچسب تومان نمایش داده می‌شوند؛
    // پیش‌تر ارقام خام ریالی با ارقام لاتین نمایش داده می‌شدند و مقایسه با
    // شمارشِ تایپی کاربر ناممکن می‌شد). ورودی‌های شمارش هم با MoneyInput +
    // مبدل MoneyRial در XAML وصل شدند تا ورودی و نمایش هم‌واحد باشند. ═══

    /// <summary>نمایش فروش نقدی سیستم — با واحد پول نمایشی</summary>
    public string SystemCashDisplay => Money.Format(SystemCash);

    /// <summary>نمایش فروش کارتی سیستم — با واحد پول نمایشی</summary>
    public string SystemCardDisplay => Money.Format(SystemCard);

    /// <summary>نمایش آنلاین سیستم — با واحد پول نمایشی</summary>
    public string SystemOnlineDisplay => Money.Format(SystemOnline);

    /// <summary>نمایش خالص سیستم — با واحد پول نمایشی</summary>
    public string SystemNetDisplay => Money.Format(SystemNet);

    /// <summary>برچسب واحد پول نمایشی — هم‌گام با تنظیمات ظاهر</summary>
    public string CurrencyLabel => Money.UnitLabel;

    /// <summary>یادداشت تحویل</summary>
    [ObservableProperty]
    private string? _handoverNote;

    /// <summary>نمایه تحویل‌گیرنده از فهرست کاربران</summary>
    [ObservableProperty]
    private int _handoverToIndex = -1;

    /// <summary>گزینه‌های تحویل‌گیرنده (کاربران صندوقدار)</summary>
    public ObservableCollection<UserOption> Users { get; } = new();

    /* ─── چرخهٔ ۳۹ — مغایرت زندهٔ پیش از ثبت ─── */

    /// <summary>مغایرت نقدی زنده = شمارش − سیستم</summary>
    public decimal LiveCashDiff => CountedCash - SystemCash;

    /// <summary>مغایرت کارتی زنده = شمارش − سیستم</summary>
    public decimal LiveCardDiff => CountedCard - SystemCard;

    /// <summary>مغایرت خالص زنده = (نقد + کارت شمارش‌شده) − (نقد + کارت سیستم)</summary>
    public decimal LiveNetDiff => (CountedCash + CountedCard) - (SystemCash + SystemCard);

    /// <summary>متن وضعیت مغایرت زنده — مطابقت/کم/اضافه</summary>
    public string LiveDiffState => Math.Abs(LiveNetDiff) < 0.5m
        ? "مغایرتی نیست — شمارش با سیستم مطابقت دارد"
        : LiveNetDiff < 0 ? $"کسری {Math.Abs(LiveNetDiff):N0} — شمارش کمتر از سیستم است" : $"اضافه {LiveNetDiff:N0} — شمارش بیشتر از سیستم است";

    /// <summary>رنگ وضعیت مغایرت زنده</summary>
    public string LiveDiffColor => Math.Abs(LiveNetDiff) < 0.5m ? "#2E7D32" : LiveNetDiff < 0 ? "#C62828" : "#E65100";

    /// <summary>نمایش مغایرت نقدی زنده — با واحد پول نمایشی و ارقام فارسی</summary>
    public string LiveCashDiffDisplay => $"{(LiveCashDiff >= 0 ? "+" : "−")}{Money.Format(Math.Abs(LiveCashDiff))}";

    /// <summary>نمایش مغایرت کارتی زنده — با واحد پول نمایشی</summary>
    public string LiveCardDiffDisplay => $"{(LiveCardDiff >= 0 ? "+" : "−")}{Money.Format(Math.Abs(LiveCardDiff))}";

    /// <summary>نمایش مغایرت خالص زنده — با واحد پول نمایشی</summary>
    public string LiveNetDiffDisplay => $"{(LiveNetDiff >= 0 ? "+" : "−")}{Money.Format(Math.Abs(LiveNetDiff))}";

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

    /// <summary>بارگذاری کاربران، مبالغ سیستم و تاریخچه</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        var users = await _api.GetAsync<UsersEnvelope>("users?pageSize=50");
        Users.Clear();
        foreach (var u in users?.Data ?? new List<UserDto>())
            Users.Add(new UserOption(u.Id, $"{u.FullName} ({u.Username})"));

        // ═══ چرخهٔ ۳۹ — مبالغ امروز سیستم برای مغایرت‌گیری زنده ═══
        // شکست خواندن، بخش شمارش را بی‌اثر نمی‌کند — فقط ستون سیستم خالی می‌ماند
        var summary = await _api.GetAsync<TodaySummaryEnvelope>("payments/today-summary");
        if (summary?.Data is not null)
        {
            SystemCash = summary.Data.CashTotal;
            SystemCard = summary.Data.CardTotal;
            SystemOnline = summary.Data.OnlineTotal;
            SystemNet = summary.Data.NetTotal;
            SystemInvoiceCount = summary.Data.InvoiceCount;
        }
        RefreshLiveDiffs();

        var shifts = await _api.GetAsync<ShiftsEnvelope>("payments/shifts");
        History.Clear();
        foreach (var s in shifts?.Data ?? new List<ShiftDto>())
            History.Add(new ShiftHistoryRow(
                s.ReceiptNumber,
                s.RegisterName,
                s.ClosedText,
                s.ClosedByName,
                Money.Format(s.TotalCash),
                Money.Format(s.TotalCard),
                Money.Format(s.TotalOnline),
                Money.Format(s.Difference),
                s.DifferenceState,
                s.DifferenceColor));

        IsBusy = false;
    }

    // ─── نوسازی مغایرت‌های زنده با هر تغییر شمارش/سیستم (چرخهٔ ۳۹) ───

    /// <summary>بازخورد تغییر شمارش نقد → مغایرت‌ها</summary>
    partial void OnCountedCashChanged(decimal value) => RefreshLiveDiffs();

    /// <summary>بازخورد تغییر شمارش کارت → مغایرت‌ها</summary>
    partial void OnCountedCardChanged(decimal value) => RefreshLiveDiffs();

    /// <summary>بازخورد تغییر فروش نقدی سیستم → مغایرت‌ها</summary>
    partial void OnSystemCashChanged(decimal value) => RefreshLiveDiffs();

    /// <summary>بازخورد تغییر فروش کارتی سیستم → مغایرت‌ها</summary>
    partial void OnSystemCardChanged(decimal value) => RefreshLiveDiffs();

    /// <summary>اعلان همهٔ پراپرتی‌های محاسباتی مغایرت زنده</summary>
    private void RefreshLiveDiffs()
    {
        OnPropertyChanged(nameof(LiveCashDiff));
        OnPropertyChanged(nameof(LiveCardDiff));
        OnPropertyChanged(nameof(LiveNetDiff));
        OnPropertyChanged(nameof(LiveDiffState));
        OnPropertyChanged(nameof(LiveDiffColor));
        OnPropertyChanged(nameof(LiveCashDiffDisplay));
        OnPropertyChanged(nameof(LiveCardDiffDisplay));
        OnPropertyChanged(nameof(LiveNetDiffDisplay));
        OnPropertyChanged(nameof(SystemCashDisplay));
        OnPropertyChanged(nameof(SystemCardDisplay));
        OnPropertyChanged(nameof(SystemOnlineDisplay));
        OnPropertyChanged(nameof(SystemNetDisplay));
        OnPropertyChanged(nameof(CurrencyLabel));
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
            $"🧾 رسید {Money.ToPersianDigits(r.ReceiptNumber)} — {r.RegisterName}\n" +
            $"بازه: {r.OpenedText} تا {r.ClosedText} | بستن‌دهنده: {r.ClosedByName}\n" +
            $"سیستم — نقد {Money.Format(r.TotalCash)} | کارت {Money.Format(r.TotalCard)} | آنلاین {Money.Format(r.TotalOnline)} | جمع {Money.Format(r.SystemTotal)}\n" +
            $"شمارش — نقد {Money.Format(r.CountedCash)} | کارت {Money.Format(r.CountedCard)} | جمع {Money.Format(r.CountedTotal)}\n" +
            $"اختلاف نقد: {Money.Format(r.CashDifference)} | کارت: {Money.Format(r.CardDifference)} | خالص: {Money.FormatWithUnit(r.NetDifference)} ({r.DifferenceState})\n" +
            (string.IsNullOrWhiteSpace(r.HandoverNote) ? "" : $"یادداشت: {r.HandoverNote}\n") +
            $"تعداد تراکنش‌های شیفت: {Money.ToPersianDigits(r.PaymentCount.ToString())}";

        _snackbar.Success($"شیفت بسته شد — رسید {Money.ToPersianDigits(r.ReceiptNumber)} صادر شد");

        // ═══ چرخهٔ ۴۳ (گزارش مالک: «با بستن شیفت میگه رسید ثبت شد ولی پیشنمایش
        // رسید رو نشون نمیده تا چاپ بکنیم») ═══ پیش‌نمایش رسید همان لحظه باز
        // می‌شود — چیدمان «رسید کاغذی» با دکمهٔ چاپ (سند ۸۰ میلی‌متری حرارتی)
        var receipt = new Services.ShiftReceipt(
            r.ReceiptNumber, r.RegisterName, r.OpenedText, r.ClosedText, r.ClosedByName,
            r.TotalCash, r.TotalCard, r.TotalOnline, r.SystemTotal,
            r.CountedCash, r.CountedCard, r.CountedTotal,
            r.CashDifference, r.CardDifference, r.NetDifference,
            r.DifferenceState, r.DifferenceColor, r.PaymentCount, r.HandoverNote);
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var preview = new Views.ShiftReceiptPreviewWindow(receipt)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            preview.Show();
        });

        await LoadAsync();
    }

    /* ─── DTOهای سبک ─── */

    /// <summary>پاسخ فهرست کاربران</summary>
    public sealed class UsersEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<UserDto>? Data { get; set; }
    }

    /// <summary>پاسخ خلاصهٔ امروز صندوق — چرخهٔ ۳۹ (مبالغ سیستم بستن شیفت)</summary>
    public sealed class TodaySummaryEnvelope
    {
        public bool IsSuccess { get; set; }
        public TodaySummaryDto? Data { get; set; }
    }

    /// <summary>ردیف خلاصهٔ امروز — همان CashRegisterSummaryDto سرور</summary>
    public sealed class TodaySummaryDto
    {
        public int InvoiceCount { get; set; }
        public decimal CashTotal { get; set; }
        public decimal CardTotal { get; set; }
        public decimal OnlineTotal { get; set; }
        public decimal RefundTotal { get; set; }
        public decimal NetTotal { get; set; }
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
