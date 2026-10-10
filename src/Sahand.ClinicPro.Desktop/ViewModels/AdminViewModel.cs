using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
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

/// <summary>
/// ردیف پرسنل — چرخه هجدهم: HasPhoto برای ستون عکس.
/// ═══ چرخهٔ سی‌وهفتم (گزارش مالک) ═══ «در لیست پرسنل تصویرشان به‌صورت
/// بندانگشتیِ قابل‌کلیک برای بزرگ‌نمایی نمایش داده شود» — پراپرتی Photo پس
/// از بارگذاری فهرست غیرهمگام پر می‌شود؛ رکورد موقعیتی است و در برابری/
/// HashCode نقشی ندارد، فقط با PropertyChanged ستون تصویر گرید را به‌روز
/// می‌کند.
/// </summary>
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
    decimal CommissionPercent = 0m,
    bool HasPhoto = false) : INotifyPropertyChanged
{
    /// <summary>بندانگشتی عکس پرسنل — تهی یعنی جای‌نگهدار آدمک در ستون عکس</summary>
    private ImageSource? _photo;

    /// <summary>
    /// بندانگشتی دایره‌ای عکس پرسنل — پس از LoadEmployeesAsync برای ردیف‌های
    /// دارای عکس دانلود و از کش ایستا پر می‌شود؛ کلیک روی آن در گرید، عکس
    /// کامل را در نمایشگر تمام‌صفحه بزرگ می‌کند.
    /// </summary>
    public ImageSource? Photo
    {
        get => _photo;
        set
        {
            if (ReferenceEquals(_photo, value)) return;
            _photo = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photo)));
        }
    }

    /// <summary>اطلاع‌رسانی تغییر پراپرتی‌های غیرموقعیتی (Photo) به اتصال گرید</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}

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
        // چرخه بیست‌وسوم: زبانهٔ پرسنل حذف شد (صفحهٔ مستقل شد) — شماره‌ها یک واحد عقب رفتند
        if (value == 0) _ = LoadSettingsAsync();
        else if (value == 1) _ = LoadAuditAsync();
        else if (value == 2) _ = LoadBackupsAsync();
        else if (value == 3) _ = SearchArchiveAsync();
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

    /// <summary>
    /// ابطال‌کنندهٔ بارگذاری بندانگشتی‌های فهرست پرسنل — هر بازخوانی، دانلودهای
    /// نوبت قبلی را لغو می‌کند تا پاسخ‌های دیررس فهرست کهنه، تصاویر ردیف‌های
    /// تازه را جابه‌جا نکنند (چرخهٔ سی‌وهفتم).
    /// </summary>
    private System.Threading.CancellationTokenSource? _empThumbCts;

    /// <summary>بارگذاری پرسنل</summary>
    [RelayCommand]
    public async Task LoadEmployeesAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<EmployeesResponse>("hr/employees");
        if (response?.Data is not null)
        {
            Employees = new ObservableCollection<EmployeeRow>(response.Data.Select(e =>
                new EmployeeRow(e.Id, e.PersonnelCode, e.FullName, e.PositionName,
                    e.DepartmentName, e.Mobile, TypeTextOf(e.Type), e.IsActive,
                    e.BaseSalary, e.CommissionPercent, e.HasPhoto)));
            // چرخهٔ سی‌وهفتم: بندانگشتی‌ها پس از نمایش فهرست، غیرهمگام پر می‌شوند
            _ = LoadEmployeeThumbnailsAsync();
        }
        else
            ErrorMessage = _api.LastError ?? "دریافت پرسنل ناموفق بود";
        IsBusy = false;
    }

    /// <summary>
    /// بارگذاری بندانگشتی‌های عکس پرسنل — چرخهٔ سی‌وهفتم (گزارش مالک):
    /// «در لیست پزشکان و لیست پرسنل تصویرشان به‌صورت بندانگشتیِ قابل‌کلیک
    /// برای بزرگ‌نمایی نمایش داده شود». فقط ردیف‌های دارای عکس دانلود می‌شوند
    /// و بندانگشتی در کش ایستا می‌ماند تا بازخوانی‌ها دانلود تکراری نداشته
    /// باشند؛ دانلود پشت‌سرهم تا رودررویی با سرور در فهرست‌های بزرگ و ظاهر
    /// شدن به‌ترتیب ردیف‌ها.
    /// </summary>
    private async Task LoadEmployeeThumbnailsAsync()
    {
        // لغو نوبت قبلی — فهرست جای‌گذاری شده است. ⚠ بدون Dispose: نخ دانلود
        // قبلی هنوز ممکن است روی توکنِ کهنه ثبت‌نام کرده باشد (HttpClient داخل
        // SendAsync ثبت‌نام می‌کند) و Dispose هم‌زمان روی آن ObjectDisposedException
        // می‌سازد؛ CTS بدون تایمر منبع مدیریتی ندارد و با اتمام لغو به GC می‌رسد.
        _empThumbCts?.Cancel();
        _empThumbCts = new System.Threading.CancellationTokenSource();
        var ct = _empThumbCts.Token;

        foreach (var row in Employees.Where(r => r.HasPhoto).ToList())
        {
            if (ct.IsCancellationRequested) return;

            var key = Services.PhotoTools.EmployeeThumbnailKey(row.Id);
            var cached = Services.PhotoTools.GetCachedThumbnail(key);
            if (cached is not null) { row.Photo = cached; continue; }

            var bytes = await Services.PhotoTools.DownloadEmployeePhotoAsync(_api, row.Id, ct);
            if (ct.IsCancellationRequested) return;

            var thumb = Services.PhotoTools.ToThumbnail(bytes);
            if (thumb is null) continue; // عکسی نیست یا بایت خراب است — جای‌نگهدار می‌ماند
            Services.PhotoTools.CacheThumbnail(key, thumb);
            row.Photo = thumb;
        }
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

    /* ─── عکس پروفایل پرسنل — چرخه هجدهم (گزارش دوازدهم مالک) ─── */

    /// <summary>نمایش بزرگ عکس پرسنل — دانلود و گشودن در نمایشگر تمام‌صفحه</summary>
    [RelayCommand]
    private async Task ViewPhotoAsync(EmployeeRow? row)
    {
        if (row is null) return;
        var bytes = await Services.PhotoTools.DownloadEmployeePhotoAsync(_api, row.Id);
        Services.PhotoTools.ShowLarge(bytes, $"عکس: {row.FullName}");
    }

    /// <summary>انتخاب/تعویض عکس پرسنل — دیالوگ فایل + آپلود چندبخشی</summary>
    [RelayCommand]
    private async Task ChangePhotoAsync(EmployeeRow? row)
    {
        if (row is null) return;
        var picked = Services.PhotoTools.PickPhoto($"انتخاب عکس: {row.FullName}");
        if (picked is null) return;

        IsBusy = true;
        var ok = await Services.PhotoTools.UploadEmployeePhotoAsync(
            _api, row.Id, picked.Value.Bytes, picked.Value.FileName);
        IsBusy = false;

        if (ok)
        {
            StatusMessage = $"عکس «{row.FullName}» ذخیره شد";
            // چرخهٔ سی‌وهفتم: ابطال کش بندانگشتی همین پرسنل — بازخوانی فهرست نسخهٔ تازه را می‌آورد
            Services.PhotoTools.InvalidateThumbnail(Services.PhotoTools.EmployeeThumbnailKey(row.Id));
            await LoadEmployeesAsync();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ذخیرهٔ عکس ناموفق بود";
        }
    }

    /// <summary>حذف عکس پرسنل — با تأیید؛ فایل و مسیر دیتابیسی پاک می‌شوند</summary>
    [RelayCommand]
    private async Task DeletePhotoAsync(EmployeeRow? row)
    {
        if (row is null) return;
        var answer = Views.AppDialog.Confirm("حذف عکس", $"عکس «{row.FullName}» حذف شود؟", Views.AppDialogIcon.Question);
        if (answer != true) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"people/employees/{row.Id}/photo");
        IsBusy = false;

        if (ok)
        {
            StatusMessage = "عکس حذف شد";
            // چرخهٔ سی‌وهفتم: ابطال کش بندانگشتی — ردیف بدون عکس/با جای‌نگهدار تازه دیده می‌شود
            Services.PhotoTools.InvalidateThumbnail(Services.PhotoTools.EmployeeThumbnailKey(row.Id));
            await LoadEmployeesAsync();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "حذف عکس ناموفق بود";
        }
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

    /// <summary>
    /// ویرایش کامل اطلاعات پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «در لیست پرسنل امکان ویرایش پرسنل وجود ندارد». همان فرم ثبت، این بار
    /// در حالت ویرایش با شناسهٔ پرسنل باز می‌شود و پس از بستن فهرست بازخوانی
    /// می‌شود تا تغییرات بی‌درنگ دیده شوند.
    /// </summary>
    [RelayCommand]
    private void EditEmployee(EmployeeRow? row)
    {
        if (row is null) return;

        var vm = new EmployeeFormViewModel(_api, editEmployeeId: row.Id);
        var window = new EmployeeFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadEmployeesAsync();
    }

    /// <summary>
    /// حذف نرم پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «کلیدهای عملیاتی پرسنل ناقص‌اند». با تأیید دیالوگ فارسی خود برنامه،
    /// رکورد حذف نرم می‌شود (سابقهٔ مالی/حضور سالم می‌ماند) و فهرست بازخوانی
    /// می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task DeleteEmployeeAsync(EmployeeRow? row)
    {
        if (row is null) return;

        // دیالوگ فارسی خود برنامه — نه کادر پیام ویندوز (قانون مالک)
        var answer = Views.AppDialog.Confirm("حذف پرسنل",
            $"پرسنل «{row.FullName}» حذف شود؟\nحذف نرم است — سوابق مالی و حضور او سالم می‌مانند.",
            Views.AppDialogIcon.Question);
        if (answer != true) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"hr/employees/{row.Id}");
        IsBusy = false;

        if (ok)
        {
            StatusMessage = $"پرسنل «{row.FullName}» حذف شد";
            await LoadEmployeesAsync();
        }
        else
        {
            ErrorMessage = _api.LastError ?? "حذف پرسنل ناموفق بود";
        }
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
                    // چرخه هجدهم: تاریخ نمایشی پشتیبان با ارقام فارسی
                    b.StartedAt is { } sa
                        ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(sa.ToString("yyyy/MM/dd HH:mm"))
                        : "—",
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
        public bool HasPhoto { get; set; }
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
        var answer = Views.AppDialog.Confirm("تأیید آرشیو سالانه", $"آرشیو همه داده‌های قدیمی‌تر از آغاز سال {year} اجرا شود؟{purgeNote}\n\nادامه می‌دهید؟", Views.AppDialogIcon.Warning);
        if (answer != true) return;

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