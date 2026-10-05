using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
}
