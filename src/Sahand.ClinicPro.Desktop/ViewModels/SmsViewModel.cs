using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تاریخچه پیامک — تب ۲ فرم ۲۶</summary>
public record SmsHistoryRow(long Id, string Recipient, string? PatientName, string? Message,
    string StatusText, string StatusColor, DateTime? SentAt, decimal Cost);

/// <summary>ردیف تمپلیت پیامک — تب ۳</summary>
public record SmsTemplateRow(long Id, string Name, string Code, string Content, bool IsActive);

/// <summary>ردیف پنل پیامک — تب ۴</summary>
public partial class SmsProviderRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>نام پنل</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>کد پنل</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>شماره ارسال</summary>
    public string? SenderNumber { get; init; }

    /// <summary>موجودی</summary>
    public decimal Balance { get; init; }

    /// <summary>اولویت — ۱ بالاترین برای زنجیره جایگزینی</summary>
    public int Priority { get; init; }

    /// <summary>پیش‌فرض</summary>
    public bool IsDefault { get; init; }

    /// <summary>فعال</summary>
    public bool IsActive { get; init; }

    /// <summary>متن وضعیت</summary>
    public string ActiveText => IsActive ? "🟢 فعال" : "🔴 غیرفعال";
}

/// <summary>نقطه نمودار روزانه — ارتفاع ستون از پیش مقیاس‌شده</summary>
public record DailySmsPoint(string Date, int Count, double BarHeight);

/// <summary>
/// ViewModel پیامک — گام U14 طبق فرم ۲۶:
/// ۵ تب: ارسال تکی/دسته‌ای + تاریخچه + تمپلیت‌ها + پنل‌ها + آمار
/// </summary>
public partial class SmsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "پیامک";

    /* ─── تب ۱: ارسال ─── */

    /// <summary>زبانه فعال — ایندکس</summary>
    [ObservableProperty]
    private int _tabIndex;

    /// <summary>نوع ارسال: تکی یا دسته‌ای</summary>
    [ObservableProperty]
    private bool _isBulk;

    /// <summary>شماره گیرنده — ارسال تکی</summary>
    [ObservableProperty]
    private string? _recipient;

    /// <summary>متن پیامک</summary>
    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>تعداد کاراکتر و پیامک — نمایش زنده</summary>
    public string CharCountText
    {
        get
        {
            var len = Message?.Length ?? 0;
            var pages = len == 0 ? 0 : (len <= 70 ? 1 : (int)Math.Ceiling(len / 67.0));
            return $"{len} / {(pages > 1 ? pages * 67 * 5 : 306)} ({pages} پیامک)";
        }
    }

    /// <summary>نوع مخاطب ارسال دسته‌ای</summary>
    [ObservableProperty]
    private string? _audienceType;

    /// <summary>گزینه‌های مخاطب — طبق فرم ۲۶</summary>
    public List<string> AudienceOptions { get; } = new()
    {
        "همه بیماران فعال", "بیماران با نوبت فردا", "بیماران بدهکار", "انتخاب دستی از لیست"
    };

    /// <summary>تمپلیت انتخابی</summary>
    [ObservableProperty]
    private SmsTemplateRow? _selectedTemplate;

    /// <summary>بازخورد تغییر متن — تازه‌سازی شمارنده</summary>
    partial void OnMessageChanged(string value) => OnPropertyChanged(nameof(CharCountText));

    /* ─── تب ۲: تاریخچه ─── */

    /// <summary>تاریخچه</summary>
    [ObservableProperty]
    private ObservableCollection<SmsHistoryRow> _history = new();

    /// <summary>فیلتر وضعیت تاریخچه</summary>
    [ObservableProperty]
    private string? _statusFilter;

    /// <summary>گزینه‌های فیلتر وضعیت</summary>
    public List<string> StatusOptions { get; } = new() { "همه", "Delivered", "Sent", "Failed", "Pending" };

    /* ─── تب ۳: تمپلیت‌ها ─── */

    /// <summary>تمپلیت‌ها</summary>
    public ObservableCollection<SmsTemplateRow> Templates { get; } = new();

    /* ─── تب ۴: پنل‌ها ─── */

    /// <summary>پنل‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<SmsProviderRow> _providers = new();

    /// <summary>پنل انتخاب‌شده</summary>
    [ObservableProperty]
    private SmsProviderRow? _selectedProvider;

    /* ─── تب ۵: آمار ─── */

    /// <summary>متن کارت‌های آماری</summary>
    [ObservableProperty]
    private string _sentText = "۰";

    /// <summary>تحویل‌شده</summary>
    [ObservableProperty]
    private string _deliveredText = "۰";

    /// <summary>خطا</summary>
    [ObservableProperty]
    private string _failedText = "۰";

    /// <summary>هزینه ماه</summary>
    [ObservableProperty]
    private string _monthCostText = "۰ ریال";

    /// <summary>درصد موفقیت</summary>
    [ObservableProperty]
    private string _successRateText = "۰٪";

    /// <summary>نقاط نمودار روزانه ۳۰ روز — ستون‌های ساده</summary>
    public ObservableCollection<DailySmsPoint> DailyPoints { get; } = new();

    /// <summary>حداکثر روزانه — برای مقیاس ستون‌ها</summary>
    [ObservableProperty]
    private double _maxDaily;

    /// <summary>سازنده</summary>
    public SmsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadTemplatesAsync();
    }

    /// <summary>بارگذاری داده زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        if (value == 1) _ = LoadHistoryAsync();
        else if (value == 3) _ = LoadProvidersAsync();
        else if (value == 4) _ = LoadStatsAsync();
    }

    /// <summary>ارسال پیامک تکی</summary>
    [RelayCommand]
    private async Task SendSingleAsync()
    {
        if (string.IsNullOrWhiteSpace(Recipient) || Recipient.Length != 11)
        { _snackbar.Error("شماره گیرنده باید ۱۱ رقم باشد"); return; }
        if (string.IsNullOrWhiteSpace(Message)) { _snackbar.Error("متن پیامک خالی است"); return; }

        IsBusy = true;
        var ok = await _api.PostAsync("sms/send", new { recipient = Recipient, message = Message });
        IsBusy = false;
        if (ok) { _snackbar.Success("پیامک ارسال شد"); Message = string.Empty; Recipient = null; }
        else _snackbar.Error(_api.LastError ?? "ارسال ناموفق بود");
    }

    /// <summary>ارسال دسته‌ای — با تأیید شمارش</summary>
    [RelayCommand]
    private async Task SendBulkAsync()
    {
        var audience = AudienceType switch
        {
            "همه بیماران فعال" => "active-patients",
            "بیماران با نوبت فردا" => "tomorrow-appointments",
            "بیماران بدهکار" => "debtors",
            _ => null
        };
        if (audience is null) { _snackbar.Info("ابتدا نوع مخاطبان را انتخاب کنید"); return; }
        if (string.IsNullOrWhiteSpace(Message)) { _snackbar.Error("متن پیامک خالی است"); return; }

        IsBusy = true;
        var response = await _api.PostAsync<BulkResponse>("sms/bulk-send",
            new { audienceType = audience, message = Message });
        IsBusy = false;
        if (response?.IsSuccess == true)
            _snackbar.Success($"ارسال دسته‌ای انجام شد ({response.Data?.Sent} از {response.Data?.Total})");
        else
            _snackbar.Error(_api.LastError ?? "ارسال دسته‌ای ناموفق بود");
    }

    /// <summary>بارگذاری تمپلیت‌ها</summary>
    private async Task LoadTemplatesAsync()
    {
        var response = await _api.GetAsync<TemplatesResponse>("sms/templates");
        Templates.Clear();
        foreach (var t in response?.Data ?? new List<TemplateDto>())
            Templates.Add(new SmsTemplateRow(t.Id, t.Name, t.Code, t.Content ?? "", true));
    }

    /// <summary>اعمال تمپلیت روی متن ارسال</summary>
    [RelayCommand]
    private void ApplyTemplate()
    {
        if (SelectedTemplate is null) return;
        Message = SelectedTemplate.Content;
        _snackbar.Info($"تمپلیت «{SelectedTemplate.Name}» اعمال شد — متغیرها را جایگزین کنید");
    }

    /// <summary>بارگذاری تاریخچه با فیلتر وضعیت</summary>
    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsBusy = true;
        try
        {
            var status = StatusFilter is null or "همه" ? "" : $"&status={StatusFilter}";
            var response = await _api.GetAsync<HistoryResponse>($"sms/history?page=1&pageSize=100{status}");
            History = new ObservableCollection<SmsHistoryRow>(
                (response?.Data?.Items ?? new List<HistoryDto>()).Select(m =>
                {
                    var (text, color) = m.Status switch
                    {
                        "Delivered" => ("✅ تحویل شد", "#2E7D32"),
                        "Sent" => ("⏳ ارسال شد", "#1565C0"),
                        "Failed" => ("❌ خطا", "#C62828"),
                        _ => ("در صف", "#F9A825")
                    };
                    return new SmsHistoryRow(m.Id, m.Recipient, m.PatientName, m.Message,
                        text, color, m.SentAt, m.Cost);
                }));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بازارسال پیامک خطادار</summary>
    [RelayCommand]
    private async Task ResendAsync(SmsHistoryRow row)
    {
        IsBusy = true;
        var ok = await _api.PostAsync("sms/send", new { recipient = row.Recipient, message = row.Message });
        IsBusy = false;
        if (ok) _snackbar.Success("بازارسال انجام شد");
        else _snackbar.Error(_api.LastError ?? "بازارسال ناموفق بود");
    }

    /// <summary>بارگذاری پنل‌ها</summary>
    [RelayCommand]
    public async Task LoadProvidersAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ProvidersResponse>("sms/providers");
            Providers = new ObservableCollection<SmsProviderRow>(
                (response?.Data ?? new List<ProviderDto>()).Select(p => new SmsProviderRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    SenderNumber = p.SenderNumber,
                    Balance = p.Balance,
                    Priority = p.Priority,
                    IsDefault = p.IsDefault,
                    IsActive = p.IsActive
                }));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>تست اتصال پنل انتخاب‌شده</summary>
    [RelayCommand]
    private async Task TestProviderAsync()
    {
        if (SelectedProvider is null) { _snackbar.Info("ابتدا یک پنل انتخاب کنید"); return; }
        IsBusy = true;
        var response = await _api.PostAsync<TestResponse>($"sms/providers/{SelectedProvider.Id}/test", null);
        IsBusy = false;
        if (response?.Data?.Connected == true)
            _snackbar.Success("اتصال پنل موفق بود");
        else
            _snackbar.Error($"اتصال ناموفق: {response?.Data?.Detail ?? "پاسخی دریافت نشد"}");
    }

    /// <summary>ذخیره پنل — ویرایش اولویت/وضعیت</summary>
    [RelayCommand]
    private async Task SaveProviderAsync()
    {
        if (SelectedProvider is null) { _snackbar.Info("ابتدا یک پنل انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PostAsync("sms/providers", new
        {
            id = SelectedProvider.Id,
            name = SelectedProvider.Name,
            code = SelectedProvider.Code,
            senderNumber = SelectedProvider.SenderNumber,
            apiKey = (string?)null,
            priority = SelectedProvider.Priority,
            isDefault = SelectedProvider.IsDefault,
            isActive = SelectedProvider.IsActive
        });
        IsBusy = false;
        if (ok) _snackbar.Success("پنل ذخیره شد");
        else _snackbar.Error(_api.LastError ?? "ذخیره پنل ناموفق بود");
    }

    /// <summary>بارگذاری آمار ۳۰ روزه</summary>
    [RelayCommand]
    public async Task LoadStatsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<StatsResponse>("sms/stats");
            var stats = response?.Data;
            if (stats is null) return;

            SentText = $"{stats.SentCount:N0}";
            DeliveredText = $"{stats.DeliveredCount:N0}";
            FailedText = $"{stats.FailedCount:N0}";
            MonthCostText = $"{stats.MonthCost:N0} ریال";
            SuccessRateText = $"{stats.SuccessRate:0.#}٪";

            DailyPoints.Clear();
            var raw = (stats.Daily ?? new List<DailyDto>())
                .Select(d => new DailySmsPoint(
                    Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(d.Date), d.Count, 2))
                .ToList();
            var max = raw.Count == 0 ? 1 : Math.Max(1, raw.Max(p => p.Count));
            MaxDaily = max;
            foreach (var p in raw)
                DailyPoints.Add(p with { BarHeight = Math.Max(2, Math.Round(p.Count * 60.0 / max)) });
        }
        finally
        {
            IsBusy = false;
        }
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class TemplatesResponse
    {
        public bool IsSuccess { get; set; }
        public List<TemplateDto>? Data { get; set; }
    }

    private sealed class TemplateDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Content { get; set; }
    }

    private sealed class HistoryResponse
    {
        public bool IsSuccess { get; set; }
        public HistoryPage? Data { get; set; }
    }

    private sealed class HistoryPage
    {
        public List<HistoryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private sealed class HistoryDto
    {
        public long Id { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string? PatientName { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? SentAt { get; set; }
        public decimal Cost { get; set; }
    }

    private sealed class ProvidersResponse
    {
        public bool IsSuccess { get; set; }
        public List<ProviderDto>? Data { get; set; }
    }

    private sealed class ProviderDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? SenderNumber { get; set; }
        public decimal Balance { get; set; }
        public int Priority { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class TestResponse
    {
        public bool IsSuccess { get; set; }
        public TestData? Data { get; set; }
    }

    private sealed class TestData
    {
        public bool Connected { get; set; }
        public string? Detail { get; set; }
    }

    private sealed class BulkResponse
    {
        public bool IsSuccess { get; set; }
        public BulkData? Data { get; set; }
    }

    private sealed class BulkData
    {
        public int Sent { get; set; }
        public int Total { get; set; }
    }

    private sealed class StatsResponse
    {
        public bool IsSuccess { get; set; }
        public StatsData? Data { get; set; }
    }

    private sealed class StatsData
    {
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }
        public decimal MonthCost { get; set; }
        public double SuccessRate { get; set; }
        public List<DailyDto>? Daily { get; set; }
    }

    private sealed class DailyDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
