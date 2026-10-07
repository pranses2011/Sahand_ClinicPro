using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel تنظیمات سیستم — گام U15 طبق فرم ۲۸:
/// سایدبار منو + بخش‌ها: اطلاعات درمانگاه / ماژول‌ها / سامانه‌های دولتی / چاپگر / تم / امنیت
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly IThemeService _theme;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "تنظیمات";

    /// <summary>بخش فعال سایدبار — ۰ اطلاعات، ۱ ماژول‌ها، ۲ دولتی، ۳ چاپگر، ۴ تم، ۵ امنیت</summary>
    [ObservableProperty]
    private int _sectionIndex;

    /* ─── بخش اطلاعات درمانگاه ─── */

    /// <summary>نام درمانگاه</summary>
    [ObservableProperty]
    private string? _clinicName;

    /// <summary>شماره مجوز</summary>
    [ObservableProperty]
    private string? _licenseNumber;

    /// <summary>کد اقتصادی</summary>
    [ObservableProperty]
    private string? _economicCode;

    /// <summary>شناسه ملی</summary>
    [ObservableProperty]
    private string? _nationalId;

    /// <summary>نام مدیر</summary>
    [ObservableProperty]
    private string? _managerName;

    /// <summary>تلفن</summary>
    [ObservableProperty]
    private string? _phone;

    /// <summary>ایمیل</summary>
    [ObservableProperty]
    private string? _email;

    /// <summary>آدرس</summary>
    [ObservableProperty]
    private string? _address;

    /* ─── بخش ماژول‌ها ─── */

    /// <summary>ردیف ماژول با وضعیت لایسنس و فعال بودن</summary>
    public ObservableCollection<ModuleStatusRow> Modules { get; } = new();

    /// <summary>ماژول انتخاب‌شده</summary>
    [ObservableProperty]
    private ModuleStatusRow? _selectedModule;

    /* ─── بخش سامانه‌های دولتی ─── */

    /// <summary>ردیف سامانه دولتی</summary>
    public ObservableCollection<GovSystemCard> GovSystems { get; } = new();

    /// <summary>سامانه انتخاب‌شده</summary>
    [ObservableProperty]
    private GovSystemCard? _selectedGovSystem;

    /* ─── بخش چاپگر ─── */

    /// <summary>چاپگر پیش‌فرض</summary>
    [ObservableProperty]
    private string? _defaultPrinter;

    /// <summary>چاپگر فاکتور</summary>
    [ObservableProperty]
    private string? _invoicePrinter;

    /// <summary>چاپگر نسخه</summary>
    [ObservableProperty]
    private string? _prescriptionPrinter;

    /// <summary>سایز کاغذ فاکتور</summary>
    [ObservableProperty]
    private string? _invoicePaperSize = "A5";

    /// <summary>گزینه‌های سایز کاغذ</summary>
    public List<string> PaperSizes { get; } = new() { "A4", "A5", "حرارنی ۸۰mm", "حرارنی ۵۸mm" };

    /// <summary>نمایش پیش‌نمایش قبل چاپ</summary>
    [ObservableProperty]
    private bool _showPreview = true;

    /// <summary>چاپ خودکار بعد ثبت فاکتور</summary>
    [ObservableProperty]
    private bool _autoPrintInvoice;

    /// <summary>تعداد نسخه چاپ</summary>
    [ObservableProperty]
    private int _printCopies = 1;

    /* ─── بخش تم ─── */

    /// <summary>تم تاریک فعال</summary>
    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>فهرست تم‌های رنگی — چرخه ۱۹ (گزارش سیزدهم مالک)</summary>
    public ObservableCollection<ThemeOptionRow> ThemeOptions { get; } = new();

    /// <summary>کلید تم رنگی انتخاب‌شده — با تغییر، فوری اعمال و ذخیره می‌شود</summary>
    [ObservableProperty]
    private string _selectedThemeKey = "Blue";

    /// <summary>فونت انتخابی</summary>
    [ObservableProperty]
    private string? _selectedFont = "Vazirmatn";

    /// <summary>گزینه‌های فونت</summary>
    public List<string> FontOptions { get; } = new() { "Vazirmatn", "IRANSans", "Yekan" };

    /// <summary>سایز فونت پایه</summary>
    [ObservableProperty]
    private double _baseFontSize = 13;

    /// <summary>انیمیشن‌ها فعال</summary>
    [ObservableProperty]
    private bool _animationsEnabled = true;

    /* ─── بخش امنیت ─── */

    /// <summary>حداقل طول رمز</summary>
    [ObservableProperty]
    private int _minPasswordLength = 8;

    /// <summary>الزام حروف بزرگ و کوچک</summary>
    [ObservableProperty]
    private bool _requireMixedCase = true;

    /// <summary>الزام عدد</summary>
    [ObservableProperty]
    private bool _requireDigit = true;

    /// <summary>الزام کاراکتر خاص</summary>
    [ObservableProperty]
    private bool _requireSpecialChar = true;

    /// <summary>تعداد تلاش ناموفق تا قفل</summary>
    [ObservableProperty]
    private int _maxFailedAttempts = 5;

    /// <summary>مدت قفل (دقیقه)</summary>
    [ObservableProperty]
    private int _lockoutMinutes = 30;

    /// <summary>مدت انقضای رمز (روز) — صفر یعنی غیرفعال</summary>
    [ObservableProperty]
    private int _passwordExpiryDays = 90;

    /// <summary>مدت نشست (دقیقه)</summary>
    [ObservableProperty]
    private int _sessionTimeoutMinutes = 20;

    /// <summary>ثبت Audit Log</summary>
    [ObservableProperty]
    private bool _auditLogEnabled = true;

    /// <summary>سازنده</summary>
    public SettingsViewModel(IApiClient api, ISnackbarService snackbar, IThemeService theme)
    {
        _api = api;
        _snackbar = snackbar;
        _theme = theme;
        foreach (var th in _theme.Themes)
            ThemeOptions.Add(new ThemeOptionRow { Theme = th });
        SelectedThemeKey = _theme.CurrentThemeKey;
        RefreshThemeSelection();

        // مقداردهی اولیه بخش‌ها — داده ماژول/دولتی از سرور در انتخاب بخش
        InitializeModules();
        InitializeGovSystems();
        _ = LoadClinicInfoAsync();
    }

    /// <summary>بارگذاری بخش فعال</summary>
    partial void OnSectionIndexChanged(int value)
    {
        if (value == 1) _ = LoadModulesStatusAsync();
        else if (value == 2) _ = LoadGovSystemsAsync();
    }

    /* ─── اطلاعات درمانگاه ─── */

    /// <summary>بارگذاری اطلاعات درمانگاه از تنظیمات</summary>
    private async Task LoadClinicInfoAsync()
    {
        var response = await _api.GetAsync<SettingsResponse>("settings?category=ClinicInfo");
        var map = response?.Data ?? new List<SettingDto>();
        ClinicName = Value(map, "ClinicName");
        LicenseNumber = Value(map, "LicenseNumber");
        EconomicCode = Value(map, "EconomicCode");
        NationalId = Value(map, "NationalId");
        ManagerName = Value(map, "ManagerName");
        Phone = Value(map, "Phone");
        Email = Value(map, "Email");
        Address = Value(map, "Address");
    }

    /// <summary>ذخیره اطلاعات درمانگاه</summary>
    [RelayCommand]
    private async Task SaveClinicInfoAsync()
    {
        var entries = new Dictionary<string, string?>
        {
            ["ClinicName"] = ClinicName, ["LicenseNumber"] = LicenseNumber,
            ["EconomicCode"] = EconomicCode, ["NationalId"] = NationalId,
            ["ManagerName"] = ManagerName, ["Phone"] = Phone,
            ["Email"] = Email, ["Address"] = Address
        };
        await SaveSettingsBatchAsync("ClinicInfo", entries);
    }

    /* ─── ماژول‌ها ─── */

    /// <summary>مقداردهی اولیه فهرست ماژول‌ها — وضعیت لایسنس از سرور تازه می‌شود</summary>
    private void InitializeModules()
    {
        var catalog = new (string Title, string Icon)[]
        {
            ("بیماران", "👤"), ("نوبت‌دهی", "📅"), ("پذیرش", "🩺"), ("نسخه‌نویسی", "💊"),
            ("صندوق و فاکتور", "💰"), ("حسابداری", "📊"), ("بیمه", "🏦"), ("آزمایشگاه", "🧪"),
            ("داروخانه", "🏥"), ("پیامک", "📱"), ("تصویربرداری", "📷"), ("دندانپزشکی", "🦷"),
            ("چشم‌پزشکی", "👁"), ("فیزیوتراپی", "🏃"), ("سپاس", "🏛"), ("نسخه الکترونیک", "📤")
        };
        foreach (var (title, icon) in catalog)
            Modules.Add(new ModuleStatusRow { Title = title, Icon = icon, IsCore = title is "بیماران" or "نوبت‌دهی" or "پذیرش" });
    }

    /// <summary>بارگذاری وضعیت لایسنس ماژول‌ها از سرور</summary>
    private async Task LoadModulesStatusAsync()
    {
        var response = await _api.GetAsync<LicenseStatusResponse>("license/status");
        var enabled = response?.Data?.EnabledModules ?? new List<string>();
        foreach (var m in Modules)
        {
            var isLicensed = enabled.Contains(m.Title, StringComparer.OrdinalIgnoreCase) ||
                             enabled.Contains(ToEnglishKey(m.Title), StringComparer.OrdinalIgnoreCase);
            m.LicenseText = isLicensed ? "✅ مجاز" : "❌ ندارد";
            m.IsToggleEnabled = isLicensed && !m.IsCore;
            m.IsEnabled = isLicensed; // مجاز و پیش‌فرض فعال — Core همیشه فعال
            m.IsLocked = !isLicensed;
        }
    }

    /// <summary>تبدیل عنوان فارسی به کلید انگلیسی ماژول</summary>
    private static string ToEnglishKey(string title) => title switch
    {
        "بیماران" => "Patients", "نوبت‌دهی" => "Appointments", "پذیرش" => "Receptions",
        "نسخه‌نویسی" => "Prescriptions", "صندوق و فاکتور" => "Billing", "حسابداری" => "Accounting",
        "بیمه" => "Insurance", "آزمایشگاه" => "Lab", "داروخانه" => "Pharmacy", "پیامک" => "Sms",
        "تصویربرداری" => "Imaging", "دندانپزشکی" => "Dental", "چشم‌پزشکی" => "Eye",
        "فیزیوتراپی" => "Physiotherapy", "سپاس" => "Government", "نسخه الکترونیک" => "Government",
        _ => title
    };

    /* ─── سامانه‌های دولتی ─── */

    /// <summary>مقداردهی کارت‌های سامانه دولتی — طبق فرم ۲۸</summary>
    private void InitializeGovSystems()
    {
        GovSystems.Add(new GovSystemCard { Name = "سامانه سپاس", Code = "sepas", ApiUrl = "https://api.sepas.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه سیب", Code = "sib", ApiUrl = "https://sib.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه ناب", Code = "nab", ApiUrl = "https://nab.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "سامانه سینا", Code = "sina", ApiUrl = "https://sina.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "نسخه الکترونیک", Code = "erx", ApiUrl = "https://erx.behdasht.gov.ir", IsConnected = false });
        GovSystems.Add(new GovSystemCard { Name = "بیمه سلامت", Code = "health-insurance", ApiUrl = "", IsConnected = false });
    }

    /// <summary>بارگذاری وضعیت اتصال سامانه‌های دولتی از تنظیمات</summary>
    private async Task LoadGovSystemsAsync()
    {
        var response = await _api.GetAsync<SettingsResponse>("settings?category=Government");
        var map = response?.Data ?? new List<SettingDto>();
        foreach (var g in GovSystems)
        {
            g.Username = Value(map, $"{g.Code}.username");
            g.IsEnabled = Value(map, $"{g.Code}.enabled") == "true";
        }
    }

    /// <summary>ذخیره سامانه دولتی — از دکمه کارت</summary>
    [RelayCommand]
    private async Task SaveGovSystemAsync(GovSystemCard? g)
    {
        if (g is null) return;
        await SaveSettingsBatchAsync("Government", new Dictionary<string, string?>
        {
            [$"{g.Code}.apiUrl"] = g.ApiUrl,
            [$"{g.Code}.username"] = g.Username,
            [$"{g.Code}.enabled"] = g.IsEnabled ? "true" : "false"
        });
    }

    /* ─── چاپگر ─── */

    /// <summary>ذخیره تنظیمات چاپگر</summary>
    [RelayCommand]
    private async Task SavePrinterSettingsAsync()
    {
        await SaveSettingsBatchAsync("Printer", new Dictionary<string, string?>
        {
            ["DefaultPrinter"] = DefaultPrinter,
            ["InvoicePrinter"] = InvoicePrinter,
            ["PrescriptionPrinter"] = PrescriptionPrinter,
            ["InvoicePaperSize"] = InvoicePaperSize,
            ["ShowPreview"] = ShowPreview ? "true" : "false",
            ["AutoPrintInvoice"] = AutoPrintInvoice ? "true" : "false",
            ["PrintCopies"] = PrintCopies.ToString()
        });
    }

    /// <summary>تست چاپگر — ارسال صفحه آزمایشی به چاپگر پیش‌فرض ویندوز</summary>
    [RelayCommand]
    private void TestPrint()
    {
        try
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
                _snackbar.Success($"تست چاپ به «{printDialog.PrintQueue.FullName}\" ارسال شد");
        }
        catch (Exception)
        {
            _snackbar.Error("دسترسی به چاپگر ممکن نشد");
        }
    }

    /* ─── تم ─── */

    /// <summary>تغییر تم روشن/تاریک — اعمال فوری روی پوسته</summary>
    [RelayCommand]
    private void ApplyTheme() => _theme.SetTheme(IsDarkTheme);

    /// <summary>انتخاب تم رنگی از کاتالوگ — اعمال فوری + ذخیره (چرخه ۱۹)</summary>
    [RelayCommand]
    private void SelectTheme(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        SelectedThemeKey = key;
    }

    /// <summary>با تغییر کلید تم: اعمال روی پوسته + تازه‌سازی نشانگر انتخاب + ذخیره</summary>
    partial void OnSelectedThemeKeyChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        _theme.ApplyTheme(value, IsDarkTheme);
        RefreshThemeSelection();
        _ = SaveThemeSettingsAsync();
    }

    /// <summary>هماهنگی نشانگر انتخاب‌شده با کلید فعلی</summary>
    private void RefreshThemeSelection()
    {
        foreach (var row in ThemeOptions)
            row.IsSelected = string.Equals(row.Theme.Key, SelectedThemeKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ذخیره تنظیمات تم</summary>
    [RelayCommand]
    private async Task SaveThemeSettingsAsync()
    {
        await SaveSettingsBatchAsync("Theme", new Dictionary<string, string?>
        {
            ["IsDark"] = IsDarkTheme ? "true" : "false",
            ["Name"] = SelectedThemeKey,
            ["Font"] = SelectedFont,
            ["BaseFontSize"] = BaseFontSize.ToString("0"),
            ["Animations"] = AnimationsEnabled ? "true" : "false"
        });
    }

    /* ─── امنیت ─── */

    /// <summary>ذخیره تنظیمات امنیت</summary>
    [RelayCommand]
    private async Task SaveSecuritySettingsAsync()
    {
        await SaveSettingsBatchAsync("Security", new Dictionary<string, string?>
        {
            ["MinPasswordLength"] = MinPasswordLength.ToString(),
            ["RequireMixedCase"] = RequireMixedCase ? "true" : "false",
            ["RequireDigit"] = RequireDigit ? "true" : "false",
            ["RequireSpecialChar"] = RequireSpecialChar ? "true" : "false",
            ["MaxFailedAttempts"] = MaxFailedAttempts.ToString(),
            ["LockoutMinutes"] = LockoutMinutes.ToString(),
            ["PasswordExpiryDays"] = PasswordExpiryDays.ToString(),
            ["SessionTimeoutMinutes"] = SessionTimeoutMinutes.ToString(),
            ["AuditLogEnabled"] = AuditLogEnabled ? "true" : "false"
        });
    }

    /* ─── کمکی ─── */

    /// <summary>خواندن مقدار کلید از فهرست تنظیمات</summary>
    private static string? Value(List<SettingDto> settings, string key)
        => settings.FirstOrDefault(s => s.Key == key)?.Value;

    /// <summary>ذخیره دسته تنظیمات با یک کلید دسته</summary>
    private async Task SaveSettingsBatchAsync(string category, Dictionary<string, string?> entries)
    {
        IsBusy = true;
        var allOk = true;
        foreach (var (key, value) in entries)
        {
            var ok = await _api.PutAsync("settings", new { key, value, category });
            if (!ok) allOk = false;
        }
        IsBusy = false;
        if (allOk) _snackbar.Success("تنظیمات ذخیره شد");
        else _snackbar.Error(_api.LastError ?? "ذخیره برخی تنظیمات ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    private sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Category { get; set; }
    }

    private sealed class LicenseStatusResponse
    {
        public bool IsSuccess { get; set; }
        public LicenseStatusData? Data { get; set; }
    }

    private sealed class LicenseStatusData
    {
        public bool IsValid { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxDoctors { get; set; }
        public int MaxPatients { get; set; }
        public List<string>? EnabledModules { get; set; }
        public string? Message { get; set; }
    }
}

/// <summary>ردیف ماژول با وضعیت لایسنس — بخش ماژول‌ها</summary>
public partial class ModuleStatusRow : ObservableObject
{
    /// <summary>عنوان فارسی</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>آیکن</summary>
    public string Icon { get; init; } = "";

    /// <summary>ماژول هسته — غیرقابل غیرفعال‌سازی</summary>
    public bool IsCore { get; init; }

    /// <summary>متن وضعیت لایسنس</summary>
    [ObservableProperty]
    private string _licenseText = "—";

    /// <summary>فعال در سیستم</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>کلید لایسنس ندارد</summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary>سوییچ فعال/غیرفعال قابل تغییر است</summary>
    [ObservableProperty]
    private bool _isToggleEnabled;

    /// <summary>متن دکمه عملیات</summary>
    public string ActionText => IsCore ? "Core" : (IsLocked ? "خرید ماژول" : "⚙️");
}

/// <summary>کارت سامانه دولتی — بخش سامانه‌های دولتی</summary>
public partial class GovSystemCard : ObservableObject
{
    /// <summary>نام نمایشی</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>کد سرویس</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>آدرس API</summary>
    [ObservableProperty]
    private string? _apiUrl;

    /// <summary>نام کاربری</summary>
    [ObservableProperty]
    private string? _username;

    /// <summary>رمز عبور — فقط ورود جدید</summary>
    [ObservableProperty]
    private string? _password;

    /// <summary>فعال</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>وضعیت اتصال آخرین بررسی</summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>متن وضعیت اتصال</summary>
    public string ConnectedText => IsConnected ? "🟢 متصل" : "⚪ بررسی نشده";
}

/// <summary>
/// ردیف تم رنگی در فهرست تنظیمات — چرخه ۱۹:
/// نشانگر انتخاب + رنگ پیش‌نمایش به‌صورت برش‌پذیر برای XAML
/// </summary>
public partial class ThemeOptionRow : ObservableObject
{
    /// <summary>تعریف تم از کاتالوگ</summary>
    public required AppTheme Theme { get; init; }

    /// <summary>کلید</summary>
    public string Key => Theme.Key;

    /// <summary>نام فارسی</summary>
    public string Title => Theme.Title;

    /// <summary>آیکون تم</summary>
    public string Icon => Theme.Icon;

    /// <summary>رنگ پیش‌نمایش</summary>
    public System.Windows.Media.Color PrimaryColor =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Theme.PrimaryHex);

    /// <summary>انتخاب‌شده است؟</summary>
    [ObservableProperty]
    private bool _isSelected;
}
