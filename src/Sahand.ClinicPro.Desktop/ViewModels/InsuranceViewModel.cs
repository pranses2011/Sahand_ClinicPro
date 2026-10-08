﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف شرکت بیمه</summary>
public record CompanyRow(long Id, string Name, string? Code, decimal CoveragePercent);

/// <summary>ردیف تعرفه</summary>
public record TariffRow(string Code, string ServiceName, decimal Price, decimal InsuranceShare, decimal PatientShare);

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

    /// <summary>تعرفه‌ها</summary>
    public ObservableCollection<TariffRow> Tariffs { get; } = new();

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

    /// <summary>بارگذاری هر چهار تب</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        var companies = await _api.GetAsync<CompaniesResponse>("lookups/insurance-companies");
        Companies = new ObservableCollection<CompanyRow>(
            (companies?.Data ?? new List<CompanyDto>()).Select(c =>
                new CompanyRow(c.Id, c.Name, c.Code, 70m)));

        var services = await _api.GetAsync<InvoiceFormViewModel.ServicesResponse>("lookups/services");
        Tariffs.Clear();
        foreach (var s in services?.Data ?? new List<InvoiceFormViewModel.ServiceDto>())
            Tariffs.Add(new TariffRow(s.ServiceCode ?? "—", s.ServiceName,
                s.Price, s.InsuranceShare, s.PatientShare));

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
