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

/// <summary>
/// ردیف ماژول فعال/غیرفعال لایسنس — چرخه هجدهم (گزارش دوازدهم مالک):
/// چیپس‌ها پیش‌تر فقط «غیرفعال» نشان می‌دادند؛ اکنون «نام ماژول + وضعیت»
/// نمایش داده می‌شود و پس‌زمینهٔ چیپس با فعال شدن سبز می‌شود.
/// </summary>
public partial class LicenseModuleRow : ObservableObject
{
    /// <summary>نام ماژول</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>فعال است</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>متن وضعیت</summary>
    public string StatusText => IsEnabled ? "✅ فعال" : "غیرفعال";

    /// <summary>متن کامل چیپس — «بیماران • فعال»</summary>
    public string DisplayText => $"{Title} • {StatusText}";

    /// <summary>رنگ پس‌زمینهٔ چیپس — سبز فعال / خاکستری غیرفعال</summary>
    public string ChipBackground => IsEnabled ? "#E8F5E9" : "#F5F5F5";

    /// <summary>رنگ قاب چیپس</summary>
    public string ChipBorder => IsEnabled ? "#A5D6A7" : "#E0E0E0";

    /// <summary>رنگ متن چیپس</summary>
    public string ChipForeground => IsEnabled ? "#1B5E20" : "#9E9E9E";

    /// <summary>بازخورد تغییر فعال بودن — تازه‌سازی رنگ‌ها و متن</summary>
    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(ChipBackground));
        OnPropertyChanged(nameof(ChipBorder));
        OnPropertyChanged(nameof(ChipForeground));
    }
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

    /// <summary>
    /// پیام سرور لایسنس — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
    /// «پیام‌های سرور لایسنس در برنامه نمایش داده شود» — پیام متنی پنل
    /// (هشدار انقضای نزدیک، اطلاعیهٔ تمدید و…) که در پاسخ license/status
    /// می‌آید و پیش‌تر نادیده گرفته می‌شد؛ در بنر هشدار بالای صفحه نمایش می‌یابد.
    /// </summary>
    [ObservableProperty]
    private string? _serverMessage;

    /// <summary>آیا پیام سرور برای نمایش هست؟ — برای پنهان‌سازی بنر وقتی پیامی نیست</summary>
    [ObservableProperty]
    private bool _hasServerMessage;

    /// <summary>محدودیت‌ها</summary>
    public ObservableCollection<LicenseLimitRow> Limits { get; } = new();

    /// <summary>ماژول‌ها</summary>
    public ObservableCollection<LicenseModuleRow> Modules { get; } = new();

    /// <summary>کلید ورودی فعال‌سازی</summary>
    [ObservableProperty]
    private string? _activationKey;

    /// <summary>
    /// رویداد فعال‌سازی موفق لایسنس — چرخه هفدهم (گزارش یازدهم):
    /// پنجرهٔ فعال‌سازی مستقل (LicenseActivationWindow) این رویداد را می‌شنود تا
    /// بنر موفقیت را نشان دهد و پس از آن خودش را با نتیجهٔ موفق ببندد؛ پیش‌تر
    /// هیچ راهی نبود فراخواننده بفهمد فعال‌سازی واقعاً انجام شده است.
    /// </summary>
    public event EventHandler? LicenseActivated;

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

    /// <summary>منبع اطلاعات پشتیبانی — ذخیره‌شده | پنل | پیش‌فرض</summary>
    [ObservableProperty]
    private string _supportSource = "default";

    /* ─── چرخه هجدهم (گزارش دوازدهم مالک) ─── */

    /// <summary>
    /// نشانی سرور لایسنس — پیش‌فرض رسمی محصول؛ با دکمهٔ ذخیره قابل تغییر
    /// و در تنظیمات سرور (SystemSettings) ماندگار است.
    /// </summary>
    [ObservableProperty]
    private string _portalUrl = "https://clinic-license.ea-fixer.ir";

    /// <summary>پیام وضعیت ذخیرهٔ نشانی سرور</summary>
    [ObservableProperty]
    private string? _portalSaveMessage;

    /// <summary>سازنده</summary>
    public LicenseStatusViewModel(IApiClient api, ISnackbarService snackbar, IConfiguration configuration)
    {
        _api = api;
        _snackbar = snackbar;
        _configuration = configuration;
        _ = LoadAsync();
        _ = LoadPortalUrlAsync();
        _ = LoadSupportInfoAsync();
    }

    /// <summary>
    /// بارگذاری نشانی سرور لایسنس از تنظیمات API — چرخه هجدهم:
    /// فیلد «سرور لایسنس» پیش‌فرض رسمی را نشان می‌دهد و قابل ویرایش است.
    /// </summary>
    public async Task LoadPortalUrlAsync()
    {
        var response = await _api.GetAnonymousAsync<PortalSettingsResponse>("license/settings");
        if (response?.Data is { } d && !string.IsNullOrWhiteSpace(d.PortalUrl))
            PortalUrl = d.PortalUrl;
    }

    /// <summary>ذخیرهٔ نشانی سرور لایسنس واردشدهٔ کاربر — PUT license/settings</summary>
    [RelayCommand]
    private async Task SavePortalUrlAsync()
    {
        PortalSaveMessage = null;
        var ok = await _api.PutAnonymousAsync("license/settings", new { portalUrl = PortalUrl?.Trim() });
        if (ok)
        {
            PortalSaveMessage = "✅ نشانی سرور لایسنس ذخیره شد";
            _snackbar.Success("نشانی سرور لایسنس ذخیره شد");
            await LoadPortalUrlAsync(); // مقدار نرمال‌شده (api.php) بازخوانی می‌شود
        }
        else
        {
            PortalSaveMessage = _api.LastError ?? "❌ ذخیرهٔ نشانی ناموفق بود";
        }
    }

    /// <summary>
    /// بارگذاری اطلاعات پشتیبانی — چرخه هجدهم: «بصورت پیش‌فرض اطلاعات پشتیبانی
    /// موجود باشه و از دفعات بعد بروزرسانی از سرور لایسنس رو انجام بده»؛
    /// GET license/support با refresh — اول مقادیر ذخیره‌شده، سپس پنل، سپس پیش‌فرض.
    /// </summary>
    public async Task LoadSupportInfoAsync(bool refresh = false)
    {
        var endpoint = $"license/support" + (refresh ? "?refresh=true" : string.Empty);
        var response = await _api.GetAnonymousAsync<SupportResponse>(endpoint);
        var sup = response?.Data;
        if (sup is null) return; // best-effort — کارت با آخرین مقادیر می‌ماند

        SupportName = sup.Name ?? "";
        SupportCompany = sup.Company ?? "";
        SupportPhone = sup.Phone ?? "";
        SupportMobile = sup.Mobile ?? "";
        SupportEmail = sup.Email ?? "";
        SupportWebsite = sup.Website ?? "";
        SupportSource = sup.Source ?? "default";
        HasSupport = !string.IsNullOrWhiteSpace(SupportCompany)
                     || !string.IsNullOrWhiteSpace(SupportMobile)
                     || !string.IsNullOrWhiteSpace(SupportPhone)
                     || !string.IsNullOrWhiteSpace(SupportEmail)
                     || !string.IsNullOrWhiteSpace(SupportWebsite)
                     || !string.IsNullOrWhiteSpace(SupportName);
    }

    /// <summary>
    /// دریافت فایل درخواست فعال‌سازی آفلاین (.req) — چرخه هجدهم (گزارش دوازدهم):
    /// «نمیشه برای فعالسازی آفلاین فایل .req رو گرفت»؛ اکنون پاسخ endpoint
    /// activation-request با پسوند .req ذخیره می‌شود و برای فروشنده ارسال می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task DownloadRequestFileAsync()
    {
        // کلید واردشده (در صورت وجود) داخل فایل درج می‌شود تا پنل تطابق کلید را بررسی کند
        var key = string.IsNullOrWhiteSpace(ActivationKey) ? null : ActivationKey.Trim();
        var endpoint = "license/activation-request" +
                       (key is null ? string.Empty : $"?licenseKey={Uri.EscapeDataString(key)}");
        var response = await _api.GetAnonymousAsync<ActivationRequestResponse>(endpoint);
        var req = response?.Data;
        if (req is null)
        {
            _snackbar.Error(_api.LastError ?? "ساخت فایل درخواست ناموفق بود");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "فایل درخواست فعال‌سازی|*.req|همه فایل‌ها|*.*",
            Title = "ذخیرهٔ فایل درخواست فعال‌سازی آفلاین",
            FileName = $"activation-request-{IranClock.Now:yyyyMMdd-HHmm}.req"
        };
        if (dialog.ShowDialog() != true) return;

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = System.Text.Json.JsonSerializer.Serialize(req, options);
        await System.IO.File.WriteAllTextAsync(dialog.FileName, json);
        _snackbar.Success($"فایل درخواست ذخیره شد — «{dialog.FileName}» را برای فروشنده بفرستید تا فایل .lic بگیرید");
    }

    /// <summary>
    /// بارگذاری وضعیت لایسنس — چرخهٔ سی‌ودوم (گزارش مالک): دکمهٔ «بررسی آنلاین»
    /// اکنون واقعاً برخط است؛ پیش‌تر فقط وضعیت «محلی» ذخیره‌شده را می‌خواند و
    /// ماژول تازه‌فعال‌شده در پنل لایسنس هرگز دیده نمی‌شد. ابتدا تپ قلب لحظه‌ای
    /// از طریق اندپوینت جدید license/refresh زده می‌شود (حضور اعلام، محدودیت‌ها/
    /// ماژول‌ها/اطلاعات درمانگاه تازه و پیام‌های پشتیبانی تحویل می‌شوند) و سپس
    /// وضعیت تازه‌شده خوانده می‌شود؛ در قطعی شبکه هم بی‌خطا به مقادیر محلی
    /// برمی‌گردد (مهلت آفلاین خودش در سرور اعمال می‌شود).
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // ۱) تازه‌سازی لحظه‌ای از سرور لایسنس — best-effort و بی‌صدا
            try { await _api.PostAnonymousAsync("license/refresh", new { }); }
            catch { /* قطعی شبکه — وضعیت محلی خوانده می‌شود */ }

            // ۲) خواندن وضعیت (اکنون تازه‌شده)
            var response = await _api.GetAnonymousAsync<StatusResponse>("license/status");
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

            // چرخهٔ بیست‌وهفتم: پیام سرور لایسنس در بنر بالای صفحه —
            // پیام هر بار جای‌گذاری می‌شود؛ نبود پیام بنر را پنهان می‌کند
            ServerMessage = string.IsNullOrWhiteSpace(s.Message) ? null : s.Message.Trim();
            HasServerMessage = ServerMessage is not null;

            if (s.ExpiryDate is { } expiry)
            {
                // چرخه هجدهم: تاریخ انقضا با ارقام فارسی
                ExpiryText = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsiFa(expiry);
                var days = (int)(expiry.Date - IranClock.Now.Date).TotalDays;
                RemainingText = days > 0
                    ? $"{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(days.ToString())} روز باقیمانده"
                    : "منقضی شده";
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

    /// <summary>
    /// کاتالوگ نام‌های ماژول برای نمایش — چرخه نوزدهم (گزارش سیزدهم مالک):
    /// کلیدها دقیقاً همان کاتالوگ سرور لایسنس PHP (ls_module_catalog) هستند.
    /// ماژول «صندوق و فاکتور» قبلاً با کلید Invoices جستجو می‌شد در حالی که
    /// سرور آن را Billing می‌فرستد و چیپس همیشه «غیرفعال» دیده می‌شد.
    /// </summary>
    private static readonly Dictionary<string, string> ModuleCatalog = new()
    {
        ["Dashboard"] = "داشبورد", ["Patients"] = "بیماران", ["Appointments"] = "نوبت‌دهی",
        ["Receptions"] = "پذیرش", ["Visits"] = "ویزیت", ["Prescriptions"] = "نسخه‌نویسی",
        ["Billing"] = "صندوق و فاکتور",
        ["Accounting"] = "حسابداری", ["Insurance"] = "بیمه", ["Lab"] = "آزمایشگاه",
        ["Pharmacy"] = "داروخانه", ["Sms"] = "پیامک", ["Imaging"] = "تصویربرداری",
        ["Dental"] = "دندانپزشکی", ["Eye"] = "چشم‌پزشکی", ["Physiotherapy"] = "فیزیوتراپی",
        ["Attendance"] = "حضور و غیاب", ["HR"] = "پرسنل", ["Reports"] = "گزارش‌ها",
        ["Government"] = "سامانه‌های دولتی", ["Settings"] = "تنظیمات", ["Backup"] = "پشتیبان‌گیری",
        ["Audit"] = "لاگ عملیات", ["Users"] = "کاربران",
        // ── ماژول‌های افزودنی نسخه ۱.۶.۰ — هماهنگ با کاتالوگ سرور لایسنس ──
        ["Survey"] = "نظرسنجی بیماران", ["Payroll"] = "فیش حقوقی",
        ["Paraclinical"] = "پاراکلینیک", ["EMR"] = "پرونده بالینی پیشرفته",
        ["Complementary"] = "طب مکمل و قالب چاپ", ["Branches"] = "مدیریت شعبه‌ها",
        // ── ماژول‌های نسخه ۲.۰.۰ (چرخه سیزدهم) — هماهنگ با کاتالوگ سرور لایسنس ──
        ["ClinicalSafety"] = "ایمنی دارویی و تشخیصی", ["PrintTemplates"] = "طراح قالب چاپ سفارشی",
        ["InsuranceSettlement"] = "تسویه حساب بیمه", ["ShiftCash"] = "صندوق و تحویل شیفت",
        ["Waitlist"] = "لیست انتظار نوبت", ["AdhocReports"] = "گزارش‌ساز رایگانه",
        ["CloudBackup"] = "پشتیبان ابری", ["Archive"] = "آرشیو سالانه",
        ["Training"] = "مرکز آموزش تعاملی",
    };

    /// <summary>فعال‌سازی آنلاین با کلید ورودی</summary>
    [RelayCommand]
    private async Task ActivateOnlineAsync()
    {
        if (string.IsNullOrWhiteSpace(ActivationKey) || ActivationKey.Trim().Length < 10)
        { _snackbar.Error("کلید لایسنس معتبر وارد کنید"); return; }

        IsBusy = true;
        var ok = await _api.PostAnonymousAsync("license/activate", new { licenseKey = ActivationKey.Trim() });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success("لایسنس فعال شد");
            ActivationKey = null;
            LicenseActivated?.Invoke(this, EventArgs.Empty);
            await LoadAsync();
        }
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
        var ok = await _api.PostAnonymousAsync("license/activate-offline", new { licenseFileContent = content });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success("لایسنس آفلاین فعال شد");
            LicenseActivated?.Invoke(this, EventArgs.Empty);
            await LoadAsync();
        }
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

    /* ─── پاسخ‌های چرخه هجدهم ─── */

    /// <summary>پاسخ تنظیمات سرور لایسنس</summary>
    private sealed class PortalSettingsResponse
    {
        public bool IsSuccess { get; set; }
        public PortalSettingsData? Data { get; set; }
    }

    /// <summary>دادهٔ تنظیمات سرور لایسنس</summary>
    private sealed class PortalSettingsData
    {
        public string? PortalUrl { get; set; }
    }

    /// <summary>پاسخ اطلاعات پشتیبانی</summary>
    private sealed class SupportResponse
    {
        public bool IsSuccess { get; set; }
        public SupportData? Data { get; set; }
    }

    /// <summary>دادهٔ اطلاعات پشتیبانی</summary>
    private sealed class SupportData
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Source { get; set; }
    }

    /// <summary>پاسخ درخواست فعال‌سازی آفلاین</summary>
    private sealed class ActivationRequestResponse
    {
        public bool IsSuccess { get; set; }
        public ActivationRequestData? Data { get; set; }
    }

    /// <summary>دادهٔ فایل .req — همان قالبی که پنل لایسنس می‌پذیرد</summary>
    private sealed class ActivationRequestData
    {
        public int FormatVersion { get; set; }
        public string? RequestType { get; set; }
        public string? Fingerprint { get; set; }
        public string? LicenseKey { get; set; }
        public string? ClinicName { get; set; }
        public string? AppVersion { get; set; }
        public string? RequestedAt { get; set; }
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
