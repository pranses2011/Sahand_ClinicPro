﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف شرکت بیمه — چرخه بیست‌وسوم (گزارش شانزدهم مالک): فیلدهای Type/TypeName/IsActive
/// برای «تعریف بیمه جدید + ویرایش بیمه‌های موجود» افزوده شد
/// </summary>
public record CompanyRow(long Id, string Name, string? Code, decimal CoveragePercent,
    int Type = 4, string TypeName = "خصوصی", bool IsActive = true)
{
    /// <summary>متن فارسی وضعیت برای ستون گرید — بدون نیاز به مبدل (قابل‌حل بودن استاتیک XAML)</summary>
    public string StatusText => IsActive ? "فعال" : "غیرفعال";
}

/// <summary>
/// ردیف تعرفه — چرخه بیست‌وسوم: فیلد Id برای ویرایش و فیلد IsActive افزوده شد.
/// چرخه ۲۴: TargetBadge نشان می‌دهد این خدمت برای بیمهٔ منتخب «تعرفهٔ اختصاصی»
/// دارد یا قیمت نمایشی همان تعرفهٔ پایه/آزاد است.
/// </summary>
public record TariffRow(long Id, string Code, string ServiceName, decimal Price,
    decimal InsuranceShare, decimal PatientShare, bool IsActive = true,
    bool HasTargetTariff = false)
{
    /// <summary>متن فارسی وضعیت برای ستون گرید — بدون نیاز به مبدل</summary>
    public string StatusText => IsActive ? "فعال" : "غیرفعال";

    /// <summary>نشان تعرفهٔ اختصاصی — ✅ برای «تعرفهٔ مختص بیمهٔ منتخب دارد»</summary>
    public string TargetBadge => HasTargetTariff ? "اختصاصی" : "پایه";
}

/// <summary>ردیف مطالبه</summary>
public record ClaimRow(long Id, string? ClaimNumber, string InsuranceName,
    string? PeriodText, decimal TotalAmount, decimal ApprovedAmount, decimal ReceivedAmount, string StatusText, string StatusColor);

/// <summary>ردیف استعلام</summary>
public record InquiryRow(long Id, string Time, string PatientName, bool IsValid, string ResultText);

/// <summary>ردیف تسویه قلم فاکتور — چرخه ۱۳ (درخواست ۲-۱)</summary>
public record FractionLineRow(
    string Description,
    string Quantity,
    string ItemTotal,
    string Franchise,
    string InsuranceShare,
    string PatientShare,
    string RuleType,
    string RuleSource);

/// <summary>ردیف قاعده تسویه بیمه</summary>
public record FractionRuleRow(
    long Id,
    string ServiceName,
    string RuleType,
    string Detail,
    string Franchise);

/// <summary>
/// ViewModel بیمه — گام U11 طبق فرم ۱۹:
/// ۴ تب: شرکت‌ها / تعرفه‌ها / مطالبات / استعلامات
/// </summary>
public partial class InsuranceViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان</summary>
    public override string Title => "بیمه";

    /// <summary>شرکت‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<CompanyRow> _companies = new();

    /// <summary>شرکت بیمه انتخاب‌شده — برای ویرایش (چرخه بیست‌وسوم)</summary>
    [ObservableProperty]
    private CompanyRow? _selectedCompany;

    /// <summary>تعرفه‌ها</summary>
    public ObservableCollection<TariffRow> Tariffs { get; } = new();

    /// <summary>تعرفهٔ انتخاب‌شده — برای ویرایش (چرخه بیست‌وسوم)</summary>
    [ObservableProperty]
    private TariffRow? _selectedTariff;

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — تعرفهٔ به‌تفکیک بیمه ═══
    // «تعرفه‌ها برای بیمه‌ها متفاوت می‌تواند باشد و برای آزاد هم تعرفه تعریف بشود»
    // — انتخابگر هدف تعرفه: اندیس ۰ = «آزاد (بدون بیمه)»، اندیس‌های بعدی =
    // شرکت‌های بیمه. تعویض هدف، فهرست تعرفه‌ها را با پارامتر هدف از سرور
    // می‌خواند و ذخیره/ویرایش هم روی همان هدف اعمال می‌شود.

    /// <summary>اندیس هدف فعلی تعرفه‌ها — ۰ آزاد، ۱+ بیمه‌ها</summary>
    [ObservableProperty]
    private int _tariffTargetIndex;

    /// <summary>برچسب هدف فعلی — بالای جدول تعرفه‌ها</summary>
    [ObservableProperty]
    private string _tariffTargetLabel = "آزاد (بدون بیمه)";

    /// <summary>نام اهداف تعرفه برای انتخابگر — [۰] آزاد + [۱..] بیمه‌ها</summary>
    public ObservableCollection<string> TariffTargetNames { get; } = new() { "🆓 آزاد (بدون بیمه)" };

    /// <summary>تغییر هدف تعرفه — بارگذاری مجدد فهرست تعرفه‌ها برای هدف تازه</summary>
    partial void OnTariffTargetIndexChanged(int value)
    {
        var (label, _) = CurrentTariffTarget();
        TariffTargetLabel = label;
        _ = LoadTariffsForTargetAsync();
    }

    /// <summary>مطالبات</summary>
    [ObservableProperty]
    private ObservableCollection<ClaimRow> _claims = new();

    [ObservableProperty]
    private ClaimRow? _selectedClaim;

    /// <summary>استعلامات امروز</summary>
    public ObservableCollection<InquiryRow> Inquiries { get; } = new();

    /* ─── تب تسویه بیمه — چرخه ۱۳ ─── */

    /// <summary>شناسه فاکتور برای تسویه</summary>
    [ObservableProperty]
    private long? _fractionInvoiceId;

    /// <summary>اقلام تسویه محاسبه‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<FractionLineRow> _fractionLines = new();

    /// <summary>خلاصه صورتحساب بیمه‌ای</summary>
    [ObservableProperty]
    private string? _fractionSummaryText;

    /// <summary>قواعد تسویه بیمه انتخابی</summary>
    public ObservableCollection<FractionRuleRow> FractionRules { get; } = new();

    /// <summary>نمایه بیمه برای فرم قاعده</summary>
    [ObservableProperty]
    private int _ruleCompanyIndex;

    /// <summary>درصد بیمه در فرم قاعده</summary>
    [ObservableProperty]
    private decimal _rulePercent = 70m;

    /// <summary>مبلغ اسمی در فرم قاعده</summary>
    [ObservableProperty]
    private decimal _ruleNominal;

    /// <summary>فرانشیز در فرم قاعده</summary>
    [ObservableProperty]
    private decimal _ruleFranchise;

    /// <summary>نوع قاعده — false=درصدی true=اسمی</summary>
    [ObservableProperty]
    private bool _ruleIsNominal;

    /// <summary>شناسه فاکتور تسویه‌شده آخر (برای دکمه اعمال)</summary>
    [ObservableProperty]
    private long? _lastCalculatedInvoiceId;

    /// <summary>سازنده</summary>
    public InsuranceViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>
    /// بارگذاری فهرست تعرفه‌ها برای هدف فعلی — چرخه بیست‌وچهارم:
    /// اندپوینت مدیریت با پارامتر insuranceCompanyId فراخوانی می‌شود؛
    /// تهی = تعرفهٔ آزاد، مقدار = تعرفهٔ بیمهٔ منتخب (در نبود، مقدار پایه).
    /// </summary>
    private async Task LoadTariffsForTargetAsync()
    {
        var (_, targetId) = CurrentTariffTarget();
        var url = "lookups/services/manage" +
                   (targetId is null ? "" : $"?insuranceCompanyId={targetId}");
        var services = await _api.GetAsync<ServicesManageResponse>(url);
        Tariffs.Clear();
        foreach (var s in services?.Data ?? new List<ServiceTariffManageDto>())
            Tariffs.Add(new TariffRow(s.Id, s.ServiceCode ?? "—", s.ServiceName,
                s.Price, s.InsuranceShare, s.PatientShare, s.IsActive, s.HasTargetTariff));
    }

    /// <summary>بارگذاری هر چهار تب</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): بیمه‌ها از اندپوینت مدیریت خوانده
        // می‌شوند تا نوع/وضعیت هم برای ویرایش در دسترس باشد (شامل غیرفعال‌ها)
        var companies = await _api.GetAsync<CompaniesManageResponse>("insurance/companies/manage");
        Companies = new ObservableCollection<CompanyRow>(
            (companies?.Data ?? new List<CompanyManageDto>()).Select(c =>
                new CompanyRow(c.Id, c.Name, c.Code, c.CoveragePercent, c.Type, c.TypeName, c.IsActive)));

        // چرخه ۲۴: فهرست اهداف تعرفه — «آزاد» + بیمه‌ها؛ انتخاب فعلی معتبر نگه
        // می‌ماند اگر هنوز در بازه باشد (بعد از حذف بیمه ممکن است بیرون بیفتد)
        TariffTargetNames.Clear();
        TariffTargetNames.Add("🆓 آزاد (بدون بیمه)");
        foreach (var c in Companies)
            TariffTargetNames.Add(c.Name);
        if (TariffTargetIndex > Companies.Count)
            TariffTargetIndex = 0;

        // ═══ چرخه ۲۴: تعرفه‌ها با پارامتر هدف (بیمهٔ منتخب یا آزاد) خوانده می‌شوند ═══
        await LoadTariffsForTargetAsync();

        var claims = await _api.GetAsync<ClaimsResponse>("insurance/claims");
        Claims = new ObservableCollection<ClaimRow>(
            (claims?.Data ?? new List<ClaimDto>()).Select(c =>
            {
                var (text, color) = c.Status switch
                {
                    1 => ("پیش‌نویس", "#ECEFF1"),
                    2 => ("ارسال‌شده", "#BBDEFB"),
                    3 => ("تأییدشده", "#C8E6C9"),
                    4 => ("پرداخت‌شده", "#A5D6A7"),
                    5 => ("ردشده", "#FFCDD2"),
                    _ => ("—", "#ECEFF1")
                };
                return new ClaimRow(c.Id, c.ClaimNumber, c.InsuranceName,
                    c.PeriodEndShamsi is null ? c.PeriodStartShamsi : $"{c.PeriodStartShamsi} تا {c.PeriodEndShamsi}",
                    c.TotalAmount, c.ApprovedAmount, c.ReceivedAmount, text, color);
            }));

        IsBusy = false;
    }

    /// <summary>ایجاد مطالبه دوره‌ای جدید</summary>
    [RelayCommand]
    private async Task NewClaimAsync()
    {
        if (Companies.Count == 0) { _snackbar.Error("شرکت بیمه‌ای موجود نیست"); return; }
        var company = Companies[0];
        var ok = await _api.PostAsync("insurance/claims", new { insuranceCompanyId = company.Id });
        if (ok) { _snackbar.Success("مطالبه دوره‌ای ایجاد شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ایجاد مطالبه ناموفق بود");
    }

    /* ═══ مدیریت بیمه‌ها و تعرفه‌ها — چرخه بیست‌وسوم (گزارش شانزدهم مالک) ═══
       «در صفحه بیمه، بتوان بیمه جدید تعریف کرد و یا بیمه‌های موجود را ویرایش کرد»
       «در صفحه بیمه، بتوان تعرفه جدید تعریف کرد و یا تعرفه‌های موجود را ویرایش کرد»
       — فرم‌های مدیریتی مودال باز می‌شوند و پس از ذخیرهٔ موفق، فهرست تازه می‌شود */

    /// <summary>باز کردن فرم بیمهٔ جدید</summary>
    [RelayCommand]
    private void NewCompany()
    {
        var form = new CompanyFormViewModel(_api, null);
        var window = new Views.CompanyFormWindow(form)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync(); // در هر حالت (ذخیره یا انصراف) فهرست تازه می‌شود
    }

    /// <summary>باز کردن فرم ویرایش بیمهٔ انتخاب‌شده</summary>
    [RelayCommand]
    private void EditCompany()
    {
        if (SelectedCompany is null)
        {
            _snackbar.Error("ابتدا یک شرکت بیمه از جدول انتخاب کنید");
            return;
        }
        var form = new CompanyFormViewModel(_api, SelectedCompany);
        var window = new Views.CompanyFormWindow(form)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>باز کردن فرم تعرفهٔ جدید — با هدف منتخب (آزاد یا بیمه) چرخه ۲۴</summary>
    [RelayCommand]
    private void NewTariff()
    {
        var (targetLabel, targetId) = CurrentTariffTarget();
        var form = new TariffFormViewModel(_api, null, targetId, targetLabel);
        var window = new Views.TariffFormWindow(form)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>باز کردن فرم ویرایش تعرفهٔ انتخاب‌شده — با هدف منتخب چرخه ۲۴</summary>
    [RelayCommand]
    private void EditTariff()
    {
        if (SelectedTariff is null)
        {
            _snackbar.Error("ابتدا یک خدمت/تعرفه از جدول انتخاب کنید");
            return;
        }
        var (targetLabel, targetId) = CurrentTariffTarget();
        var form = new TariffFormViewModel(_api, SelectedTariff, targetId, targetLabel);
        var window = new Views.TariffFormWindow(form)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>
    /// هدف فعلی تعرفه‌ها — اندیس منتخب انتخابگر تب تعرفه: ۰ = آزاد،
    /// ۱+ = شرکت بیمهٔ i-اُم فهرست. برچسب فارسی و شناسهٔ سرور را برمی‌گرداند.
    /// </summary>
    private (string Label, long? Id) CurrentTariffTarget()
    {
        if (TariffTargetIndex <= 0 || TariffTargetIndex > Companies.Count)
            return ("آزاد (بدون بیمه)", null);
        var company = Companies[TariffTargetIndex - 1];
        return (company.Name, company.Id);
    }

    /* ─── تسویه حساب بیمه — چرخه ۱۳ (درخواست ۲-۱) ─── */

    /// <summary>محاسبه تسویه فاکتور بدون اعمال</summary>
    [RelayCommand]
    private async Task CalculateFractionmentAsync()
    {
        if (FractionInvoiceId is not { } invId || invId <= 0)
        { _snackbar.Error("شناسه فاکتور بیمه‌ای را وارد کنید"); return; }

        IsBusy = true;
        var response = await _api.GetAsync<FractionEnvelope>($"insurance/fractionment/{invId}");
        IsBusy = false;

        var d = response?.Data;
        if (d is null) { _snackbar.Error(_api.LastError ?? "محاسبه ناموفق بود — فاکتور بیمه‌ای معتبر وارد کنید"); return; }

        LastCalculatedInvoiceId = invId;
        // رفع CS8604: خطوط ممکن است تهی باشد — مجموعه خامل جایگزین می‌شود
        var lines = d.Lines ?? new List<FractionLineDto>();
        FractionLines = new ObservableCollection<FractionLineRow>(
            lines.Select(l => new FractionLineRow(
                l.Description, $"{l.Quantity:0.#}", Money.Format(l.ItemTotal),
                Money.Format(l.Franchise), Money.Format(l.InsuranceShare), Money.Format(l.PatientShare),
                l.RuleType, l.RuleSource)));
        FractionSummaryText = $"🧾 {d.InvoiceCode} — {d.PatientName} | بیمه: {d.InsuranceName} | " +
                              $"جمع: {Money.Format(d.Subtotal)} | سهم بیمه: {Money.Format(d.TotalInsuranceShare)} | سهم بیمار: {Money.Format(d.TotalPatientShare)} {Money.UnitLabel}";
        // چرخه بیست‌وسوم رفع CS8602: شمارش از متغیر محلیِ تهی‌شده (Lines خود DTO تهی‌پذیر است)
        _snackbar.Success($"محاسبه انجام شد — {lines.Count} قلم");
    }

    /// <summary>اعمال تسویه روی اقلام فاکتور</summary>
    [RelayCommand]
    private async Task ApplyFractionmentAsync()
    {
        if (LastCalculatedInvoiceId is not { } invId)
        { _snackbar.Info("ابتدا محاسبه را انجام دهید"); return; }
        IsBusy = true;
        var response = await _api.PostAsync<FractionEnvelope>($"insurance/fractionment/{invId}/apply", new { });
        IsBusy = false;
        if (response?.Data is not null)
        {
            _snackbar.Success($"تسویه اعمال شد — سهم بیمه {response.Data.TotalInsuranceShare:N0} ریال روی اقلام ثبت شد");
            await CalculateFractionmentAsync();
        }
        else _snackbar.Error(_api.LastError ?? "اعمال تسویه ناموفق بود");
    }

    /// <summary>بارگذاری قواعد تسویه</summary>
    [RelayCommand]
    private async Task LoadRulesAsync()
    {
        var response = await _api.GetAsync<RulesEnvelope>("insurance/fractionment-rules");
        FractionRules.Clear();
        foreach (var r in response?.Data ?? new List<RuleDto>())
        {
            FractionRules.Add(new FractionRuleRow(
                r.Id,
                r.ServiceName ?? "همه خدمات",
                r.RuleType == 2 ? "اسمی" : "درصدی",
                r.RuleType == 2
                    ? $"{r.NominalAmount:N0} ریال"
                    : $"{r.InsurancePercent:0.#}٪ بیمه",
                r.FranchiseAmount > 0 ? $"فرانشیز {r.FranchiseAmount:N0}" : "—"));
        }
    }

    /// <summary>ثبت قاعده جدید برای بیمه انتخابی</summary>
    [RelayCommand]
    private async Task AddRuleAsync()
    {
        if (Companies.Count == 0) { _snackbar.Error("شرکت بیمه‌ای موجود نیست"); return; }
        if (RuleCompanyIndex < 0 || RuleCompanyIndex >= Companies.Count)
        { _snackbar.Error("شرکت بیمه را انتخاب کنید"); return; }
        if (!RuleIsNominal && (RulePercent is < 0 or > 100))
        { _snackbar.Error("درصد بیمه بین ۰ تا ۱۰۰ باشد"); return; }

        IsBusy = true;
        var ok = await _api.PostAsync("insurance/fractionment-rules", new
        {
            id = 0L,
            insuranceCompanyId = Companies[RuleCompanyIndex].Id,
            serviceId = (long?)null,
            ruleType = RuleIsNominal ? 2 : 1,
            insurancePercent = RulePercent,
            nominalAmount = RuleNominal,
            franchiseAmount = RuleFranchise,
            perItemCeiling = 0m,
            notes = (string?)null
        });
        IsBusy = false;
        if (ok) { _snackbar.Success("قاعده تسویه ثبت شد"); await LoadRulesAsync(); }
        else _snackbar.Error(_api.LastError ?? "ثبت قاعده ناموفق بود");
    }

    /// <summary>حذف قاعده</summary>
    [RelayCommand]
    private async Task DeleteRuleAsync(FractionRuleRow? row)
    {
        if (row is null) return;
        var ok = await _api.DeleteAsync($"insurance/fractionment-rules/{row.Id}");
        if (ok) { _snackbar.Success("قاعده حذف شد"); await LoadRulesAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    public sealed class CompaniesResponse
    {
        public bool IsSuccess { get; set; }
        public List<CompanyDto>? Data { get; set; }
    }

    public sealed class CompanyDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    // ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک): پاسخ اندپوینت مدیریت بیمه‌ها ═══

    /// <summary>پاکت پاسخ فهرست بیمه‌ها برای مدیریت</summary>
    public sealed class CompaniesManageResponse
    {
        public bool IsSuccess { get; set; }
        public List<CompanyManageDto>? Data { get; set; }
    }

    /// <summary>ردیف بیمه در پنجرهٔ مدیریت — با نوع/درصد پوشش/وضعیت</summary>
    public sealed class CompanyManageDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal CoveragePercent { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>پاکت پاسخ فهرست تعرفه‌ها برای مدیریت</summary>
    public sealed class ServicesManageResponse
    {
        public bool IsSuccess { get; set; }
        public List<ServiceTariffManageDto>? Data { get; set; }
    }

    /// <summary>ردیف خدمت/تعرفه در پنجرهٔ مدیریت</summary>
    public sealed class ServiceTariffManageDto
    {
        public long Id { get; set; }
        public string? ServiceCode { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal InsuranceShare { get; set; }
        public decimal PatientShare { get; set; }
        public bool IsActive { get; set; }
        /// <summary>چرخه ۲۴: تعرفهٔ اختصاصی برای هدف منتخب (بیمه) دارد؟</summary>
        public bool HasTargetTariff { get; set; }
    }

    public sealed class ClaimsResponse
    {
        public bool IsSuccess { get; set; }
        public List<ClaimDto>? Data { get; set; }
    }

    public sealed class ClaimDto
    {
        public long Id { get; set; }
        public string? ClaimNumber { get; set; }
        public string InsuranceName { get; set; } = string.Empty;
        public string? PeriodStartShamsi { get; set; }
        public string? PeriodEndShamsi { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public int Status { get; set; }
    }

    /// <summary>پاسخ تسویه — چرخه ۱۳</summary>
    public sealed class FractionEnvelope
    {
        public bool IsSuccess { get; set; }
        public FractionData? Data { get; set; }
    }

    /// <summary>داده تسویه</summary>
    public sealed class FractionData
    {
        public long InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = "";
        public string InsuranceName { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string DateShamsi { get; set; } = "";
        public List<FractionLineDto>? Lines { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalFranchise { get; set; }
        public decimal TotalInsuranceShare { get; set; }
        public decimal TotalPatientShare { get; set; }
    }

    /// <summary>قلم تسویه</summary>
    public sealed class FractionLineDto
    {
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal Franchise { get; set; }
        public decimal InsuranceShare { get; set; }
        public decimal PatientShare { get; set; }
        public string RuleType { get; set; } = "";
        public string RuleSource { get; set; } = "";
    }

    /// <summary>پاسخ قواعد</summary>
    public sealed class RulesEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<RuleDto>? Data { get; set; }
    }

    /// <summary>ردیف قاعده</summary>
    public sealed class RuleDto
    {
        public long Id { get; set; }
        public string? ServiceName { get; set; }
        public int RuleType { get; set; }
        public decimal InsurancePercent { get; set; }
        public decimal NominalAmount { get; set; }
        public decimal FranchiseAmount { get; set; }
    }
}
