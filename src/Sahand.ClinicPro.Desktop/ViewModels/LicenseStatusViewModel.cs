using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف محدودیت لایسنس — نوار پیشرفت فرم ۳۱</summary>
public partial class LicenseLimitRow : ObservableObject
{
    /// <summary>عنوان</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>آیکن</summary>
    public string Icon { get; init; } = "";

    /// <summary>مصرف فعلی</summary>
    public int Current { get; init; }

    /// <summary>حداکثر</summary>
    public int Max { get; init; }

    /// <summary>درصد مصرف — ۰ تا ۱۰۰</summary>
    public double Percent => Max <= 0 ? 0 : Math.Min(100, Math.Round(Current * 100.0 / Max, 0));

    /// <summary>رنگ نوار — سبز زیر ۷۰ | نارنجی زیر ۹۰ | قرمز ۹۰ و بالاتر</summary>
    public string BarColor => Percent >= 90 ? "#C62828" : Percent >= 70 ? "#F9A825" : "#2E7D32";

    /// <summary>متن نمایش</summary>
    public string DisplayText => $"{Current:N0} / {Max:N0}";

    /// <summary>راهنمای ابزار</summary>
    public string ToolTipText => $"{Current:N0} از {Max:N0} مجاز استفاده شده";
}

/// <summary>ردیف ماژول فعال/غیرفعال لایسنس</summary>
public partial class LicenseModuleRow : ObservableObject
{
    /// <summary>نام ماژول</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>فعال است</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>متن وضعیت</summary>
    public string StatusText => IsEnabled ? "✅ فعال" : "❌ غیرفعال";
}

/// <summary>
/// ViewModel وضعیت لایسنس — گام U19 طبق فرم ۳۱:
/// اطلاعات لایسنس + محدودیت‌ها با ProgressBar + ماژول‌ها + فعال‌سازی برخط/برون‌خط
/// </summary>
public partial class LicenseStatusViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly IConfiguration _configuration;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "لایسنس";

    /// <summary>کلید لایسنس</summary>
    [ObservableProperty]
    private string _licenseKeyText = "—";

    /// <summary>نوع لایسنس</summary>
    [ObservableProperty]
    private string _typeText = "—";

    /// <summary>تاریخ صدور شمسی</summary>
    [ObservableProperty]
    private string _issuedText = "—";

    /// <summary>تاریخ انقضا شمسی</summary>
    [ObservableProperty]
    private string _expiryText = "—";

    /// <summary>روزهای باقیمانده</summary>
    [ObservableProperty]
    private string _remainingText = "—";

    /// <summary>وضعیت اعتبار</summary>
    [ObservableProperty]
    private string _statusText = "در حال بررسی...";

    /// <summary>رنگ وضعیت</summary>
    [ObservableProperty]
    private string _statusColor = "#F9A825";

    /// <summary>محدودیت‌ها</summary>
    public ObservableCollection<LicenseLimitRow> Limits { get; } = new();

    /// <summary>ماژول‌ها</summary>
    public ObservableCollection<LicenseModuleRow> Modules { get; } = new();

    /// <summary>کلید ورودی فعال‌سازی</summary>
    [ObservableProperty]
    private string? _activationKey;

    /// <summary>نام پشتیبان — دریافتی از سرور لایسنس (بدون هاردکد)</summary>
    [ObservableProperty]
    private string _supportName = "";

    /// <summary>نام شرکت پشتیبان</summary>
    [ObservableProperty]
    private string _supportCompany = "";

    /// <summary>تلفن ثابت پشتیبانی</summary>
    [ObservableProperty]
    private string _supportPhone = "";

    /// <summary>موبایل پشتیبانی</summary>
    [ObservableProperty]
    private string _supportMobile = "";

    /// <summary>ایمیل پشتیبانی</summary>
    [ObservableProperty]
    private string _supportEmail = "";

    /// <summary>وب‌سایت پشتیبانی</summary>
    [ObservableProperty]
    private string _supportWebsite = "";

    /// <summary>آیا اطلاعات پشتیبانی دریافت شده</summary>
    [ObservableProperty]
    private bool _hasSupport;

    /// <summary>سازنده</summary>
    public LicenseStatusViewModel(IApiClient api, ISnackbarService snackbar, IConfiguration configuration)
    {
        _api = api;
        _snackbar = snackbar;
        _configuration = configuration;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری وضعیت لایسنس از سرور</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<StatusResponse>("license/status");
            var s = response?.Data;
            if (s is null)
            {
                StatusText = "خطا در دریافت وضعیت";
                StatusColor = "#C62828";
                return;
            }

            LicenseKeyText = s.LicenseKey ?? "—";
            StatusText = s.IsValid ? "✅ فعال" : "❌ نامعتبر";
            StatusColor = s.IsValid ? "#2E7D32" : "#C62828";

            if (s.ExpiryDate is { } expiry)
            {
                ExpiryText = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(expiry);
                var days = (int)(expiry.Date - DateTime.Now.Date).TotalDays;
                RemainingText = days > 0 ? $"{days} روز باقیمانده" : "منقضی شده";
            }

            // ماژول‌ها — از فهرست مجاز سرور با مقایسه بدون حساسیت به حروف
            Modules.Clear();
            var enabledSet = new HashSet<string>(
                s.EnabledModules ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var m in ModuleCatalog)
                Modules.Add(new LicenseModuleRow
                {
                    Title = m.Value,
                    IsEnabled = enabledSet.Contains(m.Key) || enabledSet.Contains(m.Value)
                });

            // اطلاعات پشتیبانی — دریافتی از سرور لایسنس هنگام فعال‌سازی؛ سراسر برنامه از همین مقادیر استفاده می‌کند
            var sup = s.SupportInfo;
            HasSupport = sup is not null;
            if (sup is not null)
            {
                SupportName = sup.Name ?? "";
                SupportCompany = sup.Company ?? "";
                SupportPhone = sup.Phone ?? "";
                SupportMobile = sup.Mobile ?? "";
                SupportEmail = sup.Email ?? "";
                SupportWebsite = sup.Website ?? "";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>کاتالوگ نام‌های ماژول برای نمایش</summary>
    private static readonly Dictionary<string, string> ModuleCatalog = new()
    {
        ["Patients"] = "بیماران", ["Appointments"] = "نوبت‌دهی", ["Receptions"] = "پذیرش",
        ["Prescriptions"] = "نسخه‌نویسی", ["Invoices"] = "صندوق و فاکتور",
        ["Accounting"] = "حسابداری", ["Insurance"] = "بیمه", ["Lab"] = "آزمایشگاه",
        ["Pharmacy"] = "داروخانه", ["Sms"] = "پیامک", ["Imaging"] = "تصویربرداری",
        ["Dental"] = "دندانپزشکی", ["Eye"] = "چشم‌پزشکی", ["Physiotherapy"] = "فیزیوتراپی",
        ["Attendance"] = "حضور و غیاب",
        // ── ماژول‌های افزودنی نسخه ۱.۶.۰ — هماهنگ با کاتالوگ سرور لایسنس ──
        ["Survey"] = "نظرسنجی بیماران", ["Payroll"] = "فیش حقوقی",
        ["Paraclinical"] = "پاراکلینیک", ["EMR"] = "پرونده بالینی پیشرفته",
        ["Complementary"] = "طب مکمل و قالب چاپ", ["Branches"] = "مدیریت شعبه‌ها",
    };

    /// <summary>فعال‌سازی آنلاین با کلید ورودی</summary>
    [RelayCommand]
    private async Task ActivateOnlineAsync()
    {
        if (string.IsNullOrWhiteSpace(ActivationKey) || ActivationKey.Trim().Length < 10)
        { _snackbar.Error("کلید لایسنس معتبر وارد کنید"); return; }

        IsBusy = true;
        var ok = await _api.PostAsync("license/activate", new { licenseKey = ActivationKey.Trim() });
        IsBusy = false;

        if (ok) { _snackbar.Success("لایسنس فعال شد"); ActivationKey = null; await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "فعال‌سازی ناموفق بود");
    }

    /// <summary>بارگذاری فایل .lic برای فعال‌سازی آفلاین</summary>
    [RelayCommand]
    private async Task ActivateOfflineAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "فایل لایسنس|*.lic|همه فایل‌ها|*.*",
            Title = "انتخاب فایل لایسنس آفلاین"
        };
        if (dialog.ShowDialog() != true) return;

        var content = System.IO.File.ReadAllText(dialog.FileName);
        IsBusy = true;
        var ok = await _api.PostAsync("license/activate-offline", new { licenseFileContent = content });
        IsBusy = false;

        if (ok) { _snackbar.Success("لایسنس آفلاین فعال شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "فعال‌سازی آفلاین ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class StatusResponse
    {
        public bool IsSuccess { get; set; }
        public StatusData? Data { get; set; }
    }

    private sealed class StatusData
    {
        public bool IsValid { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDoctors { get; set; }
        public int MaxPatients { get; set; }
        public List<string>? EnabledModules { get; set; }
        public SupportInfoData? SupportInfo { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>اطلاعات پشتیبانی دریافتی از سرور — هیچ مقدار هاردکدی جایگزین آن نمی‌شود</summary>
    private sealed class SupportInfoData
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
    }
}
