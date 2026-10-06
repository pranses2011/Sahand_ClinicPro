using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تنظیم قابل ویرایش</summary>
public partial class SettingRow : ObservableObject
{
    /// <summary>کلید</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>شرح</summary>
    public string? Description { get; init; }

    /// <summary>رمزنگاری‌شده</summary>
    public bool IsEncrypted { get; init; }

    /// <summary>مقدار</summary>
    [ObservableProperty]
    private string? _value;

    /// <summary>دسته — برای ذخیره</summary>
    public string Category { get; set; } = "General";
}

/// <summary>ردیف لاگ حسابرسی</summary>
public record AuditRow(
    long Id,
    string? Username,
    string Action,
    string? EntityName,
    long? EntityId,
    string? Module,
    string Time);

/// <summary>ردیف پرسنل</summary>
public record EmployeeRow(
    long Id,
    string? PersonnelCode,
    string FullName,
    string? PositionName,
    string? DepartmentName,
    string Mobile,
    string TypeText,
    bool IsActive,
    decimal BaseSalary = 0m,
    decimal CommissionPercent = 0m);

/// <summary>ردیف تاریخچه پشتیبان</summary>
public record BackupRow(string StatusText, string Date, string Mode);

/// <summary>ردیف مرورگر آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷)</summary>
public record ArchiveBrowserRow(
    string TableTitle,
    string? PatientName,
    string? SummaryLine,
    string DateText,
    int YearJalali);

/// <summary>
/// ViewModel تنظیمات و اداری — چهار زبانه مجزا
/// </summary>
public partial class AdminViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "تنظیمات";

    /// <summary>زبانه فعال — ۰ تنظیمات، ۱ لاگ، ۲ پرسنل، ۳ پشتیبان</summary>
    [ObservableProperty]
    private int _tabIndex;

    /// <summary>تنظیمات</summary>
    [ObservableProperty]
    private ObservableCollection<SettingRow> _settings = new();

    /// <summary>تنظیم انتخاب‌شده</summary>
    [ObservableProperty]
    private SettingRow? _selectedSetting;

    /// <summary>لاگ‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<AuditRow> _auditLogs = new();

    /// <summary>فیلتر نام موجودیت لاگ</summary>
    [ObservableProperty]
    private string? _auditEntityFilter;

    /// <summary>شماره صفحه لاگ</summary>
    [ObservableProperty]
    private int _auditPage = 1;

    /// <summary>پرسنل</summary>
    [ObservableProperty]
    private ObservableCollection<EmployeeRow> _employees = new();

    /// <summary>پرسنل انتخاب‌شده</summary>
    [ObservableProperty]
    private EmployeeRow? _selectedEmployee;

    /// <summary>تاریخچه پشتیبان</summary>
    [ObservableProperty]
    private ObservableCollection<BackupRow> _backups = new();

    /* ─── آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷) ─── */

    /// <summary>سال شمسی آرشیو</summary>
    [ObservableProperty]
    private string _archiveYear = "1403";

    /// <summary>حذف فیزیکی پس از آرشیو؟</summary>
    [ObservableProperty]
    private bool _archiveHardPurge;

    /// <summary>گزارش اجرا/پیش‌نمایش آرشیو</summary>
    [ObservableProperty]
    private string? _archiveReport;

    /// <summary>عبارت جستجوی مرورگر آرشیو</summary>
    [ObservableProperty]
    private string? _archiveSearchTerm;

    /// <summary>ردیف‌های مرورگر آرشیو</summary>
    [ObservableProperty]
    private ObservableCollection<ArchiveBrowserRow> _archiveRows = new();

    /// <summary>پیام وضعیت</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public AdminViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadSettingsAsync();
    }

    /// <summary>بارگذاری زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;
        if (value == 0) _ = LoadSettingsAsync();
        else if (value == 1) _ = LoadAuditAsync();
        else if (value == 2) _ = LoadEmployeesAsync();
        else if (value == 3) _ = LoadBackupsAsync();
        else if (value == 4) _ = SearchArchiveAsync();
    }

    /// <summary>بارگذاری تنظیمات</summary>
    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<SettingsResponse>("settings");
        if (response?.Data is not null)
            Settings = new ObservableCollection<SettingRow>(response.Data.Select(s => new SettingRow
            {
                Key = s.Key,
                Value = s.Value,
                Description = s.Description,
                IsEncrypted = s.IsEncrypted,
                Category = "General"
            }));
        else
            ErrorMessage = _api.LastError ?? "دریافت تنظیمات ناموفق بود";
        IsBusy = false;
    }

    /// <summary>ذخیره تنظیم انتخاب‌شده</summary>
    [RelayCommand]
    private async Task SaveSettingAsync()
    {
        if (SelectedSetting is null) return;
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;

        var ok = await _api.PutAsync("settings", new
        {
            key = SelectedSetting.Key,
            value = SelectedSetting.Value,
            category = SelectedSetting.Category
        });

        StatusMessage = ok
            ? $"تنظیم «{SelectedSetting.Key}» ذخیره شد"
            : (_api.LastError ?? "ذخیره ناموفق بود");
    }

    /// <summary>بارگذاری لاگ حسابرسی</summary>
    [RelayCommand]
    public async Task LoadAuditAsync()
    {
        IsBusy = true;
        var endpoint = $"auditlogs?page={AuditPage}&pageSize=50" +
                       (!string.IsNullOrWhiteSpace(AuditEntityFilter)
                           ? $"&entityName={Uri.EscapeDataString(AuditEntityFilter.Trim())}"
                           : string.Empty);
        var response = await _api.GetAsync<AuditResponse>(endpoint);
        if (response?.Data is not null)
            AuditLogs = new ObservableCollection<AuditRow>(response.Data.Select(a => new AuditRow(
                a.Id, a.Username, a.Action, a.EntityName, a.EntityId, a.Module, a.Time)));
        else
            ErrorMessage = _api.LastError ?? "دریافت لاگ ناموفق بود";
        IsBusy = false;
    }

    /// <summary>صفحه بعد لاگ</summary>
    [RelayCommand]
    private async Task AuditNextAsync()
    {
        AuditPage++;
        await LoadAuditAsync();
    }

    /// <summary>صفحه قبل لاگ</summary>
    [RelayCommand]
    private async Task AuditPrevAsync()
    {
        if (AuditPage > 1)
        {
            AuditPage--;
            await LoadAuditAsync();
        }
    }

    /// <summary>بارگذاری پرسنل</summary>
    [RelayCommand]
    public async Task LoadEmployeesAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<EmployeesResponse>("hr/employees");
        if (response?.Data is not null)
            Employees = new ObservableCollection<EmployeeRow>(response.Data.Select(e =>
                new EmployeeRow(e.Id, e.PersonnelCode, e.FullName, e.PositionName,
                    e.DepartmentName, e.Mobile, TypeTextOf(e.Type), e.IsActive,
                    e.BaseSalary, e.CommissionPercent)));
        else
            ErrorMessage = _api.LastError ?? "دریافت پرسنل ناموفق بود";
        IsBusy = false;
    }

    /// <summary>
    /// ویرایش اطلاعات حقوقی پرسنل — از دکمه کنار هر ردیف فهرست (چرخه دهم).
    /// حقوق پایه + درصد از سود خدمات؛ پس از ذخیره فهرست بازخوانی می‌شود.
    /// </summary>
    [RelayCommand]
    private void EditSalary(EmployeeRow? row)
    {
        if (row is null) return;

        var vm = new EmployeeSalaryEditViewModel(_api, row.Id, row.FullName,
            $"{(string.IsNullOrWhiteSpace(row.PositionName) ? "سمت: —" : $"سمت: {row.PositionName}")} • " +
            $"{(string.IsNullOrWhiteSpace(row.DepartmentName) ? "بخش: —" : $"بخش: {row.DepartmentName}")}",
            row.BaseSalary, row.CommissionPercent);
        var window = new EmployeeSalaryEditWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadEmployeesAsync();
    }

    /// <summary>متن فارسی نوع استخدام</summary>
    private static string TypeTextOf(int type) => type switch
    {
        1 => "تمام‌وقت",
        2 => "پاره‌وقت",
        3 => "قراردادی",
        4 => "کارآموز",
        _ => "سایر"
    };

    /// <summary>فرم ثبت پرسنل جدید</summary>
    [RelayCommand]
    private void NewEmployee()
    {
        var vm = new EmployeeFormViewModel(_api);
        var window = new EmployeeFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadEmployeesAsync();
    }

    /// <summary>بارگذاری تاریخچه پشتیبان</summary>
    [RelayCommand]
    public async Task LoadBackupsAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<BackupResponse>("backup/history");
        if (response?.Data is not null)
            Backups = new ObservableCollection<BackupRow>(response.Data.Select(b =>
                new BackupRow(
                    b.Status == "Completed" ? "موفق" : b.Status == "Failed" ? "ناموفق" : "در جریان",
                    b.StartedAt?.ToString("yyyy/MM/dd HH:mm") ?? "—",
                    $"{(b.IsAutomatic ? "خودکار" : "دستی")} — {b.Type ?? string.Empty}")));
        else
            ErrorMessage = _api.LastError ?? "دریافت تاریخچه ناموفق بود";
        IsBusy = false;
    }

    /// <summary>اجرای پشتیبان‌گیری دستی</summary>
    [RelayCommand]
    private async Task RunBackupAsync()
    {
        StatusMessage = string.Empty;
        ErrorMessage = string.Empty;
        IsBusy = true;
        var ok = await _api.PostAsync("backup/run", null);
        IsBusy = false;

        if (ok)
        {
            StatusMessage = "پشتیبان‌گیری آغاز شد";
            await LoadBackupsAsync();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "اجرای پشتیبان ناموفق بود";
        }
    }

    /// <summary>پاسخ تنظیمات</summary>
    public sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    /// <summary>DTO تنظیم</summary>
    public sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public bool IsEncrypted { get; set; }
    }

    /// <summary>پاسخ لاگ</summary>
    public sealed class AuditResponse
    {
        public bool IsSuccess { get; set; }
        public List<AuditDto>? Data { get; set; }
    }

    /// <summary>DTO لاگ</summary>
    public sealed class AuditDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public long? EntityId { get; set; }
        public string? Module { get; set; }
        public string Time { get; set; } = string.Empty;
    }

    /// <summary>پاسخ پرسنل</summary>
    public sealed class EmployeesResponse
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeDto>? Data { get; set; }
    }

    /// <summary>DTO پرسنل</summary>
    public sealed class EmployeeDto
    {
        public long Id { get; set; }
        public string? PersonnelCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PositionName { get; set; }
        public string? DepartmentName { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsActive { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal CommissionPercent { get; set; }
    }

    /// <summary>پاسخ پشتیبان</summary>
    public sealed class BackupResponse
    {
        public bool IsSuccess { get; set; }
        public List<BackupDto>? Data { get; set; }
    }

    /// <summary>پیش‌نمایش شمارشی آرشیو سال انتخابی — بدون تغییر داده</summary>
    [RelayCommand]
    private async Task ArchivePreviewAsync()
    {
        if (!int.TryParse(ArchiveYear?.Trim(), out var year))
        { ErrorMessage = "سال شمسی معتبر وارد کنید (مثل ۱۴۰۳)"; return; }

        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<ArchivePreviewResponse>($"admin/archive/preview?year={year}");
            var d = response?.Data;
            if (d is null) { ErrorMessage = _api.LastError ?? "پیش‌نمایش ناموفق بود"; return; }

            var lines = d.Items.Select(i =>
                $"• {i.Title}: {i.Candidates:N0} ردیف قابل انتقال" +
                (i.Blocked > 0 ? $" ({i.Blocked:N0} مسدود به‌دلیل ارجاع جدیدتر)" : "")).ToList();
            ArchiveReport = $"پیش‌نمایش آرشیو سال {d.Year} — برش از {d.Cutoff:yyyy-MM-dd}\n" +
                            string.Join("\n", lines) +
                            $"\nمجموع: {d.TotalCandidates:N0} قابل انتقال، {d.TotalBlocked:N0} مسدود";
        }
        finally { IsBusy = false; }
    }

    /// <summary>اجرای آرشیو سال انتخابی — با تأیید دو‌مرحله‌ای</summary>
    [RelayCommand]
    private async Task ArchiveRunAsync()
    {
        if (!int.TryParse(ArchiveYear?.Trim(), out var year))
        { ErrorMessage = "سال شمسی معتبر وارد کنید (مثل ۱۴۰۳)"; return; }

        var purgeNote = ArchiveHardPurge
            ? "\n⚠️ حذف فیزیکی فعال است — ردیف‌ها پس از ثبت نسخه آرشیو بلافاصله فیزیکی حذف می‌شوند."
            : "\nحذف نرم امن است — ردیف‌ها از دید برنامه حذف می‌شوند و پاک‌سازی فیزیکی با اسکریپت نگهداشت انجام می‌شود.";
        var answer = System.Windows.MessageBox.Show(
            $"آرشیو همه داده‌های قدیمی‌تر از آغاز سال {year} اجرا شود؟{purgeNote}\n\nادامه می‌دهید؟",
            "تأیید آرشیو سالانه", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<ArchiveRunResponse>("admin/archive/run",
                new { year, hardPurge = ArchiveHardPurge });
            var d = response?.Data;
            if (d is null) { ErrorMessage = _api.LastError ?? "اجرای آرشیو ناموفق بود"; return; }

            ArchiveReport = $"آرشیو سال {d.YearJalali} تکمیل شد — {d.TotalArchived:N0} ردیف منتقل شد (برش {d.Cutoff:yyyy-MM-dd})\n" +
                            string.Join("\n", d.Items.Select(i =>
                                $"• {i.Title}: {i.Archived:N0} منتقل" +
                                (i.Blocked > 0 ? $"، {i.Blocked:N0} مسدود" : "") +
                                (i.Archived < i.Candidates ? $" از {i.Candidates:N0} کاندید" : "")));
            await SearchArchiveAsync();
        }
        finally { IsBusy = false; }
    }

    /// <summary>جستجوی مرورگر آرشیو</summary>
    [RelayCommand]
    private async Task SearchArchiveAsync()
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(ArchiveSearchTerm))
            qs.Add($"term={Uri.EscapeDataString(ArchiveSearchTerm.Trim())}");
        if (int.TryParse(ArchiveYear?.Trim(), out _)) qs.Add($"take=200");

        var response = await _api.GetAsync<ArchiveSearchResponse>(
            "admin/archive/records" + (qs.Count > 0 ? "?" + string.Join("&", qs) : ""));
        var rows = response?.Data ?? new List<ArchiveRowDto>();
        ArchiveRows = new ObservableCollection<ArchiveBrowserRow>(rows.Select(r => new ArchiveBrowserRow(
            TableTitle: r.SourceTable switch
            {
                "Payments" => "پرداخت", "Invoices" => "فاکتور", "Prescriptions" => "نسخه",
                "LabOrders" => "آزمایش", "Visits" => "ویزیت", "Receptions" => "پذیرش",
                "Appointments" => "نوبت", _ => r.SourceTable,
            },
            PatientName: r.PatientName,
            SummaryLine: r.SummaryLine,
            DateText: Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(r.RecordDate),
            YearJalali: r.YearJalali)));
    }

    /// <summary>DTO پشتیبان — منطبق بر پاسخ ناشناس کنترلر</summary>
    public sealed class BackupDto
    {
        public long Id { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public long? FileSize { get; set; }
        public bool IsAutomatic { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /* ─── پاسخ‌های آرشیو — چرخه سیزدهم (درخواست ۷-۷) ─── */

    /// <summary>پاسخ پیش‌نمایش آرشیو</summary>
    public sealed class ArchivePreviewResponse
    {
        /// <summary>موفقیت</summary>
        public bool IsSuccess { get; set; }

        /// <summary>داده</summary>
        public ArchivePreviewData? Data { get; set; }
    }

    /// <summary>داده پیش‌نمایش آرشیو</summary>
    public sealed class ArchivePreviewData
    {
        /// <summary>سال شمسی</summary>
        public int Year { get; set; }

        /// <summary>تاریخ برش</summary>
        public DateTime Cutoff { get; set; }

        /// <summary>مجموع کاندیدها</summary>
        public int TotalCandidates { get; set; }

        /// <summary>مجموع مسدودها</summary>
        public int TotalBlocked { get; set; }

        /// <summary>ردیف‌های جدول‌به‌جدول</summary>
        public List<ArchivePreviewItemDto> Items { get; set; } = new();
    }

    /// <summary>ردیف پیش‌نمایش یک جدول</summary>
    public sealed class ArchivePreviewItemDto
    {
        /// <summary>نام جدول</summary>
        public string Table { get; set; } = "";

        /// <summary>عنوان فارسی</summary>
        public string Title { get; set; } = "";

        /// <summary>تعداد کاندید</summary>
        public int Candidates { get; set; }

        /// <summary>تعداد مسدود</summary>
        public int Blocked { get; set; }
    }

    /// <summary>پاسخ اجرای آرشیو</summary>
    public sealed class ArchiveRunResponse
    {
        /// <summary>موفقیت</summary>
        public bool IsSuccess { get; set; }

        /// <summary>داده</summary>
        public ArchiveRunData? Data { get; set; }
    }

    /// <summary>داده اجرای آرشیو</summary>
    public sealed class ArchiveRunData
    {
        /// <summary>سال شمسی</summary>
        public int YearJalali { get; set; }

        /// <summary>تاریخ برش</summary>
        public DateTime Cutoff { get; set; }

        /// <summary>مجموع منتقل‌شده</summary>
        public int TotalArchived { get; set; }

        /// <summary>ردیف‌های جدول‌به‌جدول</summary>
        public List<ArchiveRunItemDto> Items { get; set; } = new();
    }

    /// <summary>ردیف گزارش اجرای یک جدول</summary>
    public sealed class ArchiveRunItemDto
    {
        /// <summary>نام جدول</summary>
        public string Table { get; set; } = "";

        /// <summary>عنوان فارسی</summary>
        public string Title { get; set; } = "";

        /// <summary>تعداد کاندید</summary>
        public int Candidates { get; set; }

        /// <summary>تعداد منتقل‌شده</summary>
        public int Archived { get; set; }

        /// <summary>تعداد مسدود</summary>
        public int Blocked { get; set; }
    }

    /// <summary>پاسخ جستجوی آرشیو</summary>
    public sealed class ArchiveSearchResponse
    {
        /// <summary>موفقیت</summary>
        public bool IsSuccess { get; set; }

        /// <summary>ردیف‌ها</summary>
        public List<ArchiveRowDto>? Data { get; set; }
    }

    /// <summary>ردیف آرشیو</summary>
    public sealed class ArchiveRowDto
    {
        /// <summary>شناسه رکورد آرشیو</summary>
        public long Id { get; set; }

        /// <summary>جدول مبدأ</summary>
        public string SourceTable { get; set; } = "";

        /// <summary>شناسه مبدأ</summary>
        public long SourceId { get; set; }

        /// <summary>شناسه بیمار</summary>
        public long? PatientId { get; set; }

        /// <summary>نام بیمار</summary>
        public string? PatientName { get; set; }

        /// <summary>تاریخ رکورد</summary>
        public DateTime RecordDate { get; set; }

        /// <summary>سال شمسی</summary>
        public int YearJalali { get; set; }

        /// <summary>خلاصه‌سطر</summary>
        public string? SummaryLine { get; set; }
    }
}