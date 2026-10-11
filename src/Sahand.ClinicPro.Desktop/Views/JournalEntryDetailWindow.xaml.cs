using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ جزئیات سند حسابداری — چرخهٔ چهل‌وهفتم (گزارش مالک):
/// «در صفحه حسابداری و تب دفتر روزنامه، با دابل کلیک بر روی یک سند،
/// جزئیات کاملش رو بهمون نشون بده».
/// پنجره شناسهٔ سند را می‌گیرد، در Loaded آن را از
/// GET accounting/entries/{id} می‌خواند و سربرگ/سطرها/جمع‌ها را پر می‌کند؛
/// همهٔ سطرها با «کد و عنوان حساب» (نه شناسهٔ خام) نمایش داده می‌شوند.
/// هم‌الگوی گفت‌وگوهای بی‌واسطهٔ برنامه (PrescriptionDetailWindow).
/// </summary>
public partial class JournalEntryDetailWindow : Window, INotifyPropertyChanged
{
    private readonly long _entryId;
    private readonly IApiClient _api;

    /// <summary>عنوان سربرگ — شمارهٔ سند با ارقام فارسی</summary>
    public string EntryNumberText
    {
        get => _entryNumberText;
        private set => SetField(ref _entryNumberText, value);
    }
    private string _entryNumberText = "—";

    /// <summary>تاریخ شمسی سند</summary>
    public string DateText
    {
        get => _dateText;
        private set => SetField(ref _dateText, value);
    }
    private string _dateText = string.Empty;

    /// <summary>شرح سند</summary>
    public string Description
    {
        get => _description;
        private set => SetField(ref _description, value);
    }
    private string _description = "در حال بارگذاری…";

    /// <summary>برچسب وضعیت سند</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }
    private string _statusText = "—";

    /// <summary>رنگ پس‌زمینهٔ نشان وضعیت</summary>
    public string StatusColor
    {
        get => _statusColor;
        private set => SetField(ref _statusColor, value);
    }
    private string _statusColor = "#ECEFF1";

    /// <summary>جمع بدهکار سند</summary>
    public decimal TotalDebit
    {
        get => _totalDebit;
        private set => SetField(ref _totalDebit, value);
    }
    private decimal _totalDebit;

    /// <summary>جمع بستانکار سند</summary>
    public decimal TotalCredit
    {
        get => _totalCredit;
        private set => SetField(ref _totalCredit, value);
    }
    private decimal _totalCredit;

    /// <summary>سطرهای سند — رکورد نمایش با کد/عنوان حساب</summary>
    public ObservableCollection<LineRow> Lines { get; } = new();

    /// <summary>
    /// سازنده — شناسهٔ سند و کلاینت API؛ بارگذاری در Loaded انجام می‌شود تا
    /// پنجره ابتدا نمایان شود و کاربر منتظرِ صفحهٔ سفید نماند.
    /// </summary>
    public JournalEntryDetailWindow(long entryId, IApiClient api)
    {
        _entryId = entryId;
        _api = api;
        DataContext = this;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    /// <summary>بارگذاری جزئیات سند از اندپوینت چرخهٔ ۴۷</summary>
    private async Task LoadAsync()
    {
        var response = await _api.GetAsync<EntryDetailResponse>($"accounting/entries/{_entryId}");
        var dto = response?.Data;
        if (dto is null)
        {
            // دیالوگ فارسی خود برنامه (قانون مالک) — نه MessageBox ویندوز
            AppDialog.Error("جزئیات سند",
                $"بارگذاری سند ناموفق بود — {_api.LastError ?? "خطای ناشناخته سرور"}");
            return;
        }

        EntryNumberText = Helpers.Money.ToPersianDigits(dto.EntryNumber ?? "—");
        DateText = Helpers.Money.ToPersianDigits(dto.DateShamsi ?? string.Empty);
        Description = dto.Description ?? "بدون شرح";
        TotalDebit = dto.TotalDebit;
        TotalCredit = dto.TotalCredit;
        // وضعیت: ۲ تأییدشده (سرور از چرخهٔ ۴۷ اسناد جدید را Approved ثبت می‌کند)
        StatusText = dto.Status == 2 ? "تأییدشده" : dto.Status == 3 ? "ابطال‌شده" : "پیش‌نویس";
        StatusColor = dto.Status == 2 ? "#E8F5E9" : dto.Status == 3 ? "#FFEBEE" : "#ECEFF1";

        Lines.Clear();
        foreach (var line in dto.Lines ?? new List<LineDto>())
            Lines.Add(new LineRow(line.LineNumber, line.AccountCode, line.AccountName,
                line.Description, line.Debit, line.Credit));
    }

    /// <summary>بستن پنجره</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>رویداد تغییر پراپرتی — برای اتصال‌های یک‌طرفهٔ پنجره</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>ست‌کنندهٔ اطلاع‌رسان پراپرتی</summary>
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>ردیف نمایش سطر سند</summary>
    public sealed record LineRow(
        int LineNo, string AccountCode, string AccountName,
        string? Description, decimal Debit, decimal Credit);

    /* ─── DTO پاسخ سرور ─── */

    private sealed class EntryDetailResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public EntryDetailDto? Data { get; set; }
    }

    private sealed class EntryDetailDto
    {
        public long Id { get; set; }
        public string? EntryNumber { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public int Status { get; set; }
        public List<LineDto>? Lines { get; set; }
    }

    private sealed class LineDto
    {
        public int LineNumber { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
