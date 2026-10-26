using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گره درخت حساب</summary>
public partial class AccountNodeVM : ObservableObject
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public bool IsDetail { get; init; }
    public decimal Balance { get; init; }
    public string BalanceText => Money.Format(Balance);
    public string Icon => IsDetail ? "📄" : "📁";
    public ObservableCollection<AccountNodeVM> Children { get; } = new();
    public string Display => $"{Icon} {Code} — {Name} {(Balance != 0 ? $"(مانده: {BalanceText})" : "")}";
}

/// <summary>ردیف سند</summary>
public record JournalEntryRow(
    long Id, string? EntryNumber, string DateShamsi, string? Description,
    decimal TotalDebit, decimal TotalCredit, string StatusText, string StatusColor);

/// <summary>سطر سند قابل ویرایش</summary>
public partial class JournalLineVM : ObservableObject
{
    public long? AccountId { get; set; }
    public string AccountDisplay { get; set; } = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private decimal _debit;
    [ObservableProperty] private decimal _credit;
}

/// <summary>ردیف دفتر کل</summary>
public record LedgerRow(string DateShamsi, string? EntryNumber, string? Description,
    decimal Debit, decimal Credit, decimal Balance, string BalanceType);

/// <summary>ردیف تراز</summary>
public record TrialRow(string Code, string Name, decimal Opening, decimal Debit,
    decimal Credit, decimal Closing, string BalanceType);

/// <summary>ردیف سود و زیان</summary>
public record IncomeRow(string Title, decimal Amount, bool IsHeader, bool IsTotal);

/// <summary>
/// ViewModel حسابداری دوبل — گام U9 طبق فرم ۱۶:
/// ۵ تب: دفتر روزنامه + دفتر کل + سرفصل‌های چهارسطحی + تراز + سود و زیان
/// </summary>
public partial class AccountingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان</summary>
    public override string Title => "حسابداری";

    /* ── تب ۱: دفتر روزنامه ── */
    public ObservableCollection<JournalEntryRow> Entries { get; } = new();
    [ObservableProperty] private JournalEntryRow? _selectedEntry;
    [ObservableProperty] private bool _showEntryForm;

    /* ── فرم سند ── */
    [ObservableProperty] private string _entryDescription = string.Empty;
    public ObservableCollection<JournalLineVM> EntryLines { get; } = new();
    public decimal EntryDebitTotal => EntryLines.Sum(l => l.Debit);
    public decimal EntryCreditTotal => EntryLines.Sum(l => l.Credit);
    public bool IsBalanced => EntryDebitTotal == EntryCreditTotal && EntryLines.Count >= 2;
    public string BalanceStateText => IsBalanced ? "✅ متوازن" : "⚠️ جمع بدهکار و بستانکار برابر نیست!";
    public string BalanceStateColor => IsBalanced ? "#2E7D32" : "#C62828";

    /* ── تب ۲: دفتر کل ── */
    public ObservableCollection<AccountNodeVM> FlatAccounts { get; } = new();
    [ObservableProperty] private AccountNodeVM? _ledgerAccount;
    [ObservableProperty] private string? _ledgerFrom = PersianDateHelper.ToShamsi(new DateTime(IranClock.Now.Year, 1, 1));
    [ObservableProperty] private string? _ledgerTo = PersianDateHelper.ToShamsi(IranClock.Now);
    public ObservableCollection<LedgerRow> LedgerRows { get; } = new();

    /* ── تب ۳: سرفصل‌ها ── */
    public ObservableCollection<AccountNodeVM> AccountTree { get; } = new();
    [ObservableProperty] private AccountNodeVM? _selectedAccount;
    [ObservableProperty] private string _newAccountCode = string.Empty;
    [ObservableProperty] private string _newAccountName = string.Empty;
    [ObservableProperty] private int _newAccountTypeIndex;
    [ObservableProperty] private int _newLevelIndex = 3;
    [ObservableProperty] private bool _newNatureIsCredit;

    /* ── تب ۴: تراز ── */
    public ObservableCollection<TrialRow> TrialRows { get; } = new();
    public decimal TrialDebitTotal => TrialRows.Sum(r => r.Debit);
    public decimal TrialCreditTotal => TrialRows.Sum(r => r.Credit);
    public string TrialTotalText => TrialDebitTotal == TrialCreditTotal
        ? $"✅ متوازن — {TrialDebitTotal:N0}" : $"⚠️ نامتوازن — بده: {TrialDebitTotal:N0} / بستان: {TrialCreditTotal:N0}";

    /// <summary>
    /// پیام درون‌صفحه‌ای شکست گزارش‌های مالی — چرخهٔ سی‌وهشتم (گزارش مالک:
    /// «در گزارش‌های مالی خروجی نشان نمی‌دهد»): پیش‌تر شکست بارگذاری تراز/
    /// سود و زیان بی‌صدا می‌ماند و حتی توست «به‌روزرسانی شد» باجو می‌زد؛ اکنون
    /// علت داخل خود تب می‌نشیند و توست هم درست است.
    /// </summary>
    [ObservableProperty]
    private string? _reportsNotice;

    /* ── تب ۵: سود و زیان ── */
    public ObservableCollection<IncomeRow> IncomeRows { get; } = new();
    [ObservableProperty] private string? _incomeFrom = PersianDateHelper.ToShamsi(new DateTime(IranClock.Now.Year, 1, 1));
    [ObservableProperty] private string? _incomeTo = PersianDateHelper.ToShamsi(IranClock.Now);

    /// <summary>برچسب نوع حساب</summary>
    public string[] AccountTypeLabels { get; } = { "دارایی", "بدهی", "حقوق صاحبان سهام", "درآمد", "هزینه" };

    /// <summary>برچسب سطح</summary>
    public string[] LevelLabels { get; } = { "گروه", "کل", "معین", "تفصیلی" };

    /// <summary>سازنده</summary>
    public AccountingViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        EntryLines.Add(new JournalLineVM());
        EntryLines.Add(new JournalLineVM());

        // ═══ چرخهٔ چهل‌وهفتم — ریشه‌یابی «عملیات مالی ایراد داره» ═══
        // پیش‌تر جمع‌ها/توازن (EntryDebitTotal/IsBalanced) فقط با حذف سطر
        // بازمحاسبه می‌شدند؛ ویرایش مبلغ سطرها هیچ نوتیفیکیشنی روی والد نمی‌فرستاد
        // ⇒ دکمهٔ «ثبت سند» پس از تایپ مبالغ برای همیشه غیرفعال می‌ماند.
        // اکنون هر سطرِ افزوده‌شده مشترک تغییراتش می‌شود و جمع‌ها/توازن بی‌درنگ به‌روز می‌شوند.
        EntryLines.CollectionChanged += OnEntryLinesChanged;
        foreach (var line in EntryLines)
            line.PropertyChanged += OnLinePropertyChanged;

        _ = LoadAsync();
    }

    /// <summary>اشتراک/لغو اشتراک خودکار سطرهای مجموعه — با هر افزودن/حذف/بازنشانی</summary>
    private void OnEntryLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (JournalLineVM line in e.NewItems)
                line.PropertyChanged += OnLinePropertyChanged;
        if (e.OldItems is not null)
            foreach (JournalLineVM line in e.OldItems)
                line.PropertyChanged -= OnLinePropertyChanged;
    }

    /// <summary>تغییر مبلغ/شرح سطر → بازمحاسبهٔ جمع‌ها و توازن فرم سند</summary>
    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(JournalLineVM.Debit) or nameof(JournalLineVM.Credit))
            RaiseTotals();
    }

    /// <summary>بارگذاری همه داده‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        await LoadEntriesAsync();
        await LoadAccountsAsync();
        await LoadTrialAsync();
        await LoadIncomeAsync();
        IsBusy = false;
    }

    /// <summary>بارگذاری اسناد</summary>
    private async Task LoadEntriesAsync()
    {
        var response = await _api.GetAsync<EntriesResponse>("accounting/entries?page=1&pageSize=50");
        Entries.Clear();
        if (response?.Data is null) return;
        foreach (var e in response.Data)
            // چرخهٔ ۷۰ (گزارش مالک: «شماره به‌جای خط تیره») — ردیف‌های قدیمیِ
            // بدون EntryNumber پس از پرکردنِ راه‌اندازیِ سرور شماره دارند؛ اگر
            // باز هم تهی ماند (مثلاً سرور هنوز ری‌استارت نشده)، شناسهٔ سند
            // به‌عنوان جایگزین خوانا نمایش داده می‌شود — نه خط تیرهٔ خالی
            Entries.Add(new JournalEntryRow(e.Id,
                e.EntryNumber ?? $"سند شمارهٔ {Helpers.Money.ToPersianDigits(e.Id.ToString())}",
                e.DateShamsi, e.Description,
                e.TotalDebit, e.TotalCredit,
                e.Status == 2 ? "تأییدشده" : "پیش‌نویس",
                e.Status == 2 ? "#E8F5E9" : "#ECEFF1"));
    }

    /// <summary>بارگذاری درخت حساب‌ها — مسطح برای انتخاب</summary>
    private async Task LoadAccountsAsync()
    {
        var response = await _api.GetAsync<ChartResponse>("accounting/accounts");
        AccountTree.Clear();
        FlatAccounts.Clear();
        if (response?.Data is null) return;
        foreach (var node in response.Data)
        {
            var vm = MapNode(node);
            AccountTree.Add(vm);
            Flatten(vm);
        }
    }

    /// <summary>نگاشت گره بازگشتی</summary>
    private static AccountNodeVM MapNode(NodeDto node)
    {
        var vm = new AccountNodeVM
        {
            Id = node.Id, Code = node.AccountCode, Name = node.AccountName,
            Level = node.Level, IsDetail = node.IsDetail, Balance = node.CurrentBalance
        };
        foreach (var child in node.Children ?? new List<NodeDto>())
            vm.Children.Add(MapNode(child));
        return vm;
    }

    /// <summary>مسطح کردن درخت — برای Combobox دفتر کل</summary>
    private void Flatten(AccountNodeVM node)
    {
        FlatAccounts.Add(node);
        foreach (var child in node.Children)
            Flatten(child);
    }

    /* ── تب ۱: ثبت سند ── */

    /// <summary>باز کردن فرم سند</summary>
    [RelayCommand]
    private void NewEntry() => ShowEntryForm = true;

    // ═══ چرخهٔ چهل‌وهفتم — بستن فرم سند بدون ثبت ═══
    // پیش‌تر هیچ راهی برای بستن فرم سند (بدون ثبت) وجود نداشت و تنها راه،
    // ثبت موفق یا خروج از تب بود؛ دکمهٔ بستن صریح به نوار فرم اضافه شد.
    /// <summary>بستن فرم سند بدون ثبت</summary>
    [RelayCommand]
    private void HideEntryForm() => ShowEntryForm = false;

    // ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) ═══
    // «در صفحه حسابداری و تب دفتر روزنامه، با دابل کلیک بر روی یک سند،
    //  جزئیات کاملش رو بهمون نشون بده» — دیالوگ جزئیات با همهٔ سطرها و
    // عنوان حساب هر سطر؛ از سطرهای دفتر روزنامه با دابل‌کلیک باز می‌شود
    // (MouseBinding LeftDoubleClick روی گرید، هم‌الگوی نسخه‌ها).
    /// <summary>نمایش جزئیات کامل سند انتخاب‌شدهٔ دفتر روزنامه</summary>
    [RelayCommand]
    private void ShowEntryDetails()
    {
        if (SelectedEntry is null)
        {
            _snackbar.Info("ابتدا یک سند را از فهرست انتخاب کنید");
            return;
        }

        var owner = System.Windows.Application.Current?.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.IsActive);
        var dialog = new Views.JournalEntryDetailWindow(SelectedEntry.Id, _api)
        {
            Owner = owner
        };
        dialog.ShowDialog();
    }

    /// <summary>افزودن سطر خالی</summary>
    [RelayCommand]
    private void AddLine() => EntryLines.Add(new JournalLineVM());

    /// <summary>حذف سطر</summary>
    [RelayCommand]
    private void RemoveLine(object? param)
    {
        if (param is JournalLineVM line && EntryLines.Count > 2)
            EntryLines.Remove(line);
        RaiseTotals();
    }

    /// <summary>تغییر ارقام → بازمحاسبه توازن</summary>
    partial void OnEntryDescriptionChanged(string value) { }
    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(EntryDebitTotal));
        OnPropertyChanged(nameof(EntryCreditTotal));
        OnPropertyChanged(nameof(IsBalanced));
        OnPropertyChanged(nameof(BalanceStateText));
        OnPropertyChanged(nameof(BalanceStateColor));
    }

    /// <summary>ثبت سند — اعتبارسنجی قانون طلایی توازن</summary>
    [RelayCommand]
    private async Task SaveEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(EntryDescription))
        { _snackbar.Error("شرح سند الزامی است"); return; }
        if (EntryLines.Count < 2) { _snackbar.Error("سند حداقل ۲ سطر لازم دارد"); return; }
        if (!IsBalanced) { _snackbar.Error("جمع بدهکار و بستانکار برابر نیست"); return; }
        if (EntryLines.Any(l => l.AccountId is null)) { _snackbar.Error("حساب همه سطرها را انتخاب کنید"); return; }
        if (EntryLines.Any(l => l.Debit > 0 && l.Credit > 0))
        { _snackbar.Error("یک سطر نمی‌تواند همزمان بدهکار و بستانکار باشد"); return; }

        var body = new
        {
            description = EntryDescription.Trim(),
            lines = EntryLines.Select(l => new
            {
                accountId = l.AccountId!.Value,
                debit = l.Debit,
                credit = l.Credit,
                description = l.Description
            }).ToList()
        };
        var ok = await _api.PostAsync("accounting/entries", body);
        if (ok)
        {
            _snackbar.Success("سند حسابداری ثبت شد");
            ShowEntryForm = false;
            EntryDescription = string.Empty;
            EntryLines.Clear();
            EntryLines.Add(new JournalLineVM());
            EntryLines.Add(new JournalLineVM());
            RaiseTotals();
            await LoadEntriesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ثبت سند ناموفق بود");
    }

    /* ── تب ۲: دفتر کل ── */

    /// <summary>نمایش دفتر کل حساب انتخابی</summary>
    [RelayCommand]
    private async Task ShowLedgerAsync()
    {
        if (LedgerAccount is null) { _snackbar.Error("حساب را انتخاب کنید"); return; }
        var response = await _api.GetAsync<LedgerResponse>(
            $"accounting/ledger?accountId={LedgerAccount.Id}" +
            $"&from={Uri.EscapeDataString(LedgerFrom ?? string.Empty)}" +
            $"&to={Uri.EscapeDataString(LedgerTo ?? string.Empty)}");
        LedgerRows.Clear();
        if (response?.Data is null) { _snackbar.Error(_api.LastError ?? "دریافت دفتر کل ناموفق بود"); return; }
        foreach (var r in response.Data)
            LedgerRows.Add(new LedgerRow(r.DateShamsi, r.EntryNumber, r.Description,
                r.Debit, r.Credit, r.RunningBalance, r.BalanceType));
    }

    /* ── تب ۳: ثبت حساب ── */

    /// <summary>ثبت حساب جدید درخت چهارسطحی</summary>
    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountCode) || string.IsNullOrWhiteSpace(NewAccountName))
        { _snackbar.Error("کد و عنوان حساب الزامی است"); return; }

        var body = new
        {
            accountCode = NewAccountCode.Trim(),
            accountName = NewAccountName.Trim(),
            accountType = NewAccountTypeIndex + 1,
            level = NewLevelIndex + 1,
            nature = NewNatureIsCredit ? 2 : 1,
            parentAccountId = SelectedAccount?.Id,
            openingBalance = 0m
        };
        var ok = await _api.PostAsync("accounting/accounts", body);
        if (ok)
        {
            _snackbar.Success("حساب ثبت شد");
            NewAccountCode = string.Empty;
            NewAccountName = string.Empty;
            await LoadAccountsAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ثبت حساب ناموفق بود");
    }

    /* ── تب ۴: تراز ── */

    /// <summary>بارگذاری تراز — خروجی: موفقیت/شکست (چرخهٔ ۳۸)</summary>
    private async Task<bool> LoadTrialAsync()
    {
        var response = await _api.GetAsync<TrialResponse>(
            $"accounting/trial-balance?from={Uri.EscapeDataString(IncomeFrom ?? string.Empty)}" +
            $"&to={Uri.EscapeDataString(IncomeTo ?? string.Empty)}");
        TrialRows.Clear();
        if (response?.Data is null) return false; // شکست سرور/مجوز — علت با LastError
        foreach (var r in response.Data)
            TrialRows.Add(new TrialRow(r.AccountCode, r.AccountName, r.OpeningBalance,
                r.PeriodDebit, r.PeriodCredit, r.ClosingBalance, r.BalanceType));
        OnPropertyChanged(nameof(TrialTotalText));
        return true;
    }

    /* ── تب ۵: سود و زیان ── */

    /// <summary>بارگذاری سود و زیان — خروجی: موفقیت/شکست (چرخهٔ ۳۸)</summary>
    private async Task<bool> LoadIncomeAsync()
    {
        var response = await _api.GetAsync<IncomeResponse>(
            $"accounting/income-statement?from={Uri.EscapeDataString(IncomeFrom ?? string.Empty)}" +
            $"&to={Uri.EscapeDataString(IncomeTo ?? string.Empty)}");
        IncomeRows.Clear();
        if (response?.Data is null) return false;
        foreach (var r in response.Data)
            IncomeRows.Add(new IncomeRow(r.Title, r.Amount, r.IsHeader, r.IsTotal));
        return true;
    }

    /// <summary>
    /// به‌روزرسانی گزارش‌ها با بازه جدید — چرخهٔ سی‌وهشتم: نتیجهٔ واقعی هر دو
    /// گزارش اعلام می‌شود؛ شکست، پیامش درون صفحه می‌نشیند (پیش‌تر حتی روی
    /// شکست هم «به‌روزرسانی شد» می‌گفت — گمراه‌کننده بود).
    /// </summary>
    [RelayCommand]
    private async Task RefreshReportsAsync()
    {
        ReportsNotice = null;
        var trialOk = await LoadTrialAsync();
        var incomeOk = await LoadIncomeAsync();

        if (trialOk && incomeOk)
        {
            _snackbar.Success("گزارش‌های تراز و سود و زیان به‌روزرسانی شدند");
            return;
        }

        // دست‌کم یکی شکست خورده — نام بخش(ها) + علت سرور درون صفحه
        var failed = new List<string>();
        if (!trialOk) failed.Add("تراز آزمایشی");
        if (!incomeOk) failed.Add("سود و زیان");
        ReportsNotice = $"بارگذاری {string.Join(" و ", failed)} ناموفق بود — " +
            $"{_api.LastError ?? "خطای ناشناخته سرور"}";
        _snackbar.Error("بارگذاری گزارش‌های مالی ناموفق بود");
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class EntriesResponse
    {
        public bool IsSuccess { get; set; }
        public List<EntryDto>? Data { get; set; }
    }

    public sealed class EntryDto
    {
        public long Id { get; set; }
        public string? EntryNumber { get; set; }
        public string DateShamsi { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public int Status { get; set; }
    }

    public sealed class ChartResponse
    {
        public bool IsSuccess { get; set; }
        public List<NodeDto>? Data { get; set; }
    }

    public sealed class NodeDto
    {
        public long Id { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsDetail { get; set; }
        public decimal CurrentBalance { get; set; }
        public List<NodeDto>? Children { get; set; }
    }

    public sealed class LedgerResponse
    {
        public bool IsSuccess { get; set; }
        public List<LedgerDto>? Data { get; set; }
    }

    public sealed class LedgerDto
    {
        public string DateShamsi { get; set; } = string.Empty;
        public string? EntryNumber { get; set; }
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string BalanceType { get; set; } = string.Empty;
    }

    public sealed class TrialResponse
    {
        public bool IsSuccess { get; set; }
        public List<TrialDto>? Data { get; set; }
    }

    public sealed class TrialDto
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public string BalanceType { get; set; } = string.Empty;
    }

    public sealed class IncomeResponse
    {
        public bool IsSuccess { get; set; }
        public List<IncomeDto>? Data { get; set; }
    }

    public sealed class IncomeDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsHeader { get; set; }
        public bool IsTotal { get; set; }
    }
}
