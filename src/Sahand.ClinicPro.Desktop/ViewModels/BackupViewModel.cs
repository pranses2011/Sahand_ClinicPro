using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تاریخچه پشتیبان — فرم ۲۹</summary>
public record BackupHistoryRow(long Id, string TypeText, string StatusText, string StatusColor,
    string DateText, string SizeText, string? FilePath, bool IsAutomatic,
    string CompletedText, string StorageText, string? CloudFileName, string? ErrorMessage);

/// <summary>
/// ردیف فایل پشتیبان قابل انتخاب برای بازیابی — چرخه بیست‌وسوم (گزارش شانزدهم
/// مالک): «امکان انتخاب دستی فایل پشتیبان برای بازیابی» — فایل‌های ZIP/BAK
/// پوشهٔ پشتیبان از سرور فهرست می‌شوند.
/// </summary>
public record BackupFileRow(
    string FileName,
    string FullPath,
    string SizeText,
    string DateShamsi,
    string Kind);

/// <summary>
/// ViewModel پشتیبان‌گیری و بازیابی — گام U15 طبق فرم ۲۹:
/// وضعیت + تاریخچه + پشتیبان‌گیری فوری + تنظیمات خودکار + بازیابی
/// </summary>
public partial class BackupViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "پشتیبان‌گیری و بازیابی";

    /// <summary>تاریخچه پشتیبان‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<BackupHistoryRow> _history = new();

    /// <summary>متن وضعیت آخرین پشتیبان کامل</summary>
    [ObservableProperty]
    private string _lastFullText = "—";

    /// <summary>متن وضعیت آخرین پشتیبان لگ</summary>
    [ObservableProperty]
    private string _lastLogText = "—";

    /// <summary>پشتیبان انتخاب‌شده</summary>
    [ObservableProperty]
    private BackupHistoryRow? _selectedBackup;

    /// <summary>
    /// فایل‌های پشتیبان پوشهٔ ذخیره — چرخه بیست‌وسوم: برای انتخاب دستی بازیابی.
    /// نام فایل‌ها از این چرخه شمسی و پسوندشان ZIP است (فشرده).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BackupFileRow> _backupFiles = new();

    /// <summary>فایل پشتیبان انتخاب‌شده برای بازیابی دستی — چرخه بیست‌وسوم</summary>
    [ObservableProperty]
    private BackupFileRow? _selectedBackupFile;

    /* ─── تنظیمات پشتیبان‌گیری خودکار ─── */

    /// <summary>پشتیبان کامل روزانه فعال</summary>
    [ObservableProperty]
    private bool _dailyFullEnabled = true;

    /// <summary>ساعت پشتیبان کامل</summary>
    [ObservableProperty]
    private string _dailyFullTime = "03:00";

    /// <summary>پشتیبان تفاضلی فعال — هر چند ساعت</summary>
    [ObservableProperty]
    private bool _diffEnabled = true;

    /// <summary>فاصلهٔ پشتیبان تفاضلی (ساعت)</summary>
    [ObservableProperty]
    private int _diffHours = 6;

    /// <summary>پشتیبان لگ تراکنش فعال</summary>
    [ObservableProperty]
    private bool _logEnabled = true;

    /// <summary>فاصلهٔ پشتیبان لگ (ساعت)</summary>
    [ObservableProperty]
    private int _logHours = 1;

    /// <summary>مسیر ذخیرهٔ محلی پشتیبان‌ها — چرخه بیستم: رشتهٔ verbatim تک‌بک‌اسلش؛ قبلاً دوبل بود</summary>
    [ObservableProperty]
    private string _backupPath = @"D:\ClinicPro\Backups";

    /// <summary>نگهداری پشتیبان کامل (روز)</summary>
    [ObservableProperty]
    private int _keepFullDays = 30;

    /// <summary>نگهداری تفاضلی (روز)</summary>
    [ObservableProperty]
    private int _keepDiffDays = 7;

    /// <summary>نگهداری لگ (ساعت)</summary>
    [ObservableProperty]
    private int _keepLogHours = 48;

    /// <summary>رمزنگاری فایل پشتیبان</summary>
    [ObservableProperty]
    private bool _encryptBackup = true;

    /// <summary>اعلان در صورت خطا</summary>
    [ObservableProperty]
    private bool _notifyOnError = true;

    // ═══ چرخهٔ ۸۲ (گزارش مالک) — «ارسال بکاپ به ایمیل» ═══
    // «در تنظیمات بکاپ‌گیری خودکار، گزینهٔ ارسال به ایمیل و یک فیلد برای ایمیل
    // مقصد بگذار» — کلیدها در SystemSettings دستهٔ Backup ذخیره می‌شوند و کارگر
    // شبانهٔ سرور پس از هر پشتیبان موفق، فایل را (تا سقف ۱۵ مگابایت) پیوست
    // می‌کند یا نامهٔ اطلاع‌رسانی با مسیر فایل می‌فرستد.

    /// <summary>ارسال نسخهٔ پشتیبان خودکار به ایمیل</summary>
    [ObservableProperty]
    private bool _emailBackupEnabled;

    /// <summary>نشانی ایمیل مقصد پشتیبان</summary>
    [ObservableProperty]
    private string? _emailBackupTo;

    /// <summary>متن پیشرفت بازیابی</summary>
    [ObservableProperty]
    private string _restoreStatus = string.Empty;

    /* ─── پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳) ─── */

    /// <summary>رنگ بج وضعیت ابری</summary>
    [ObservableProperty]
    private string _cloudStatusColor = "#9E9E9E";

    /// <summary>متن بج وضعیت ابری</summary>
    [ObservableProperty]
    private string _cloudStatusBadge = "غیرفعال";

    /// <summary>متن آخرین بارگذاری ابری</summary>
    [ObservableProperty]
    private string _cloudLastUploadText = "—";

    /// <summary>متن تعداد نسخه‌های ابری</summary>
    [ObservableProperty]
    private string _cloudVersionsText = "—";

    /// <summary>متن حجم کل ابری</summary>
    [ObservableProperty]
    private string _cloudTotalSizeText = "—";

    /// <summary>پیام ابری — نتیجه تست اتصال/بارگذاری</summary>
    [ObservableProperty]
    private string? _cloudMessage;

    /// <summary>آیا پشتیبان انتخاب‌شده نسخه ابری دارد؟</summary>
    [ObservableProperty]
    private bool _canVerifyCloud;

    /* ─── تنظیم پشتیبان ابری — چرخه بیستم (گزارش چهاردهم مالک) ─── */

    /// <summary>نمایهٔ ارائه‌دهندهٔ ابری — ۰ غیرفعال، ۱ سازگار S3، ۲ پوشهٔ محلی</summary>
    [ObservableProperty]
    private int _cloudProviderIndex;

    /// <summary>گزینه‌های ارائه‌دهندهٔ ابری</summary>
    public List<string> CloudProviderOptions { get; } =
        new() { "غیرفعال", "سازگار S3 (آروان/آبرار/مین‌آیو)", "پوشهٔ محلی/شبکه" };

    /// <summary>نشانی نقطهٔ اتصال S3 — مثل s3.ir-thr-at1.arvanstorage.ir</summary>
    [ObservableProperty]
    private string? _cloudEndpoint;

    /// <summary>نام سطل</summary>
    [ObservableProperty]
    private string? _cloudBucket;

    /// <summary>ناحیه — پیش‌فرض us-east-1</summary>
    [ObservableProperty]
    private string? _cloudRegion = "us-east-1";

    /// <summary>پیشوند کلیدها در سطل — پیش‌فرض backups/</summary>
    [ObservableProperty]
    private string? _cloudPrefix = "backups/";

    /// <summary>پوشهٔ محلی برای ارائه‌دهندهٔ local</summary>
    [ObservableProperty]
    private string? _cloudLocalFolder;

    /// <summary>کلید دسترسی — خالی یعنی بدون تغییر از قبلی</summary>
    [ObservableProperty]
    private string? _cloudAccessKey;

    /// <summary>کلید رمزی — خالی یعنی بدون تغییر</summary>
    [ObservableProperty]
    private string? _cloudSecretKey;

    /// <summary>کلید رمزنگاری AES-256 — خالی یعنی بدون تغییر</summary>
    [ObservableProperty]
    private string? _cloudEncryptionKey;

    /// <summary>بارگذاری خودکار پس از هر پشتیبان موفق</summary>
    [ObservableProperty]
    private bool _cloudAutoUpload;

    /// <summary>کلید دسترسی قبلاً ذخیره شده؟ — برای راهنمای «خالی = بدون تغییر»</summary>
    [ObservableProperty]
    private bool _cloudHasStoredKeys;

    /// <summary>سازنده</summary>
    public BackupViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
        _ = LoadCloudStatusAsync();
        _ = LoadCloudConfigAsync(); // تنظیمات ابری برای فرم فعال‌سازی (چرخه بیستم)
        _ = LoadAutoSettingsAsync(); // تنظیمات خودکار — چرخه بیست‌ویکم
        _ = LoadBackupFilesAsync(); // فهرست فایل‌ها برای بازیابی دستی — چرخه بیست‌وسوم
    }

    /// <summary>بارگذاری تاریخچه پشتیبان‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<BackupHistoryResponse>("backup/history");
            var rows = (response?.Data ?? new List<BackupDto>()).Select(b =>
            {
                var (statusText, color) = b.Status switch
                {
                    "Success" => ("✅ موفق", "#2E7D32"),
                    "Failed" => ("❌ خطا", "#C62828"),
                    _ => ("⏳ در حال اجرا", "#F9A825")
                };
                var typeText = b.Type switch
                {
                    "Full" => "کامل", "Differential" => "تفاضلی", _ => "لگ"
                };
                return new BackupHistoryRow(b.Id, typeText, statusText, color,
                    FormatDate(b.StartedAt), FormatSize(b.FileSize), b.FilePath, b.IsAutomatic,
                    FormatDate(b.CompletedAt),
                    b.Storage == "Cloud" ? "ابر" : "محلی",
                    b.CloudFileName, string.IsNullOrWhiteSpace(b.ErrorMessage) ? null : b.ErrorMessage);
            }).ToList();

            History = new ObservableCollection<BackupHistoryRow>(rows);

            var lastFull = rows.FirstOrDefault(r => r.TypeText == "کامل" && r.StatusText == "✅ موفق");
            LastFullText = lastFull is null ? "پشتیبان کاملی ثبت نشده" : $"✅ {lastFull.DateText} ({lastFull.SizeText})";
            var lastLog = rows.FirstOrDefault(r => r.TypeText == "لگ");
            LastLogText = lastLog is null ? "—" : $"{lastLog.DateText} ({lastLog.SizeText})";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══ چرخه ۲۴ — انتخاب فرمت پشتیبان (گزارش هفدهم مالک) ═══
    // «برای فایل پشتیبان فقط زیپ نباشه و فرمت رو خودمون انتخاب بکنیم» —
    // دو گزینه: ZIP فشرده (پیش‌فرض، حجم کم) و BAK خام (سازگاری با ابزارهای
    // SQL). بازیابی هر دو فرمت را خودکار تشخیص می‌دهد.

    /// <summary>ایندکس فرمت پشتیبان — ۰ ZIP فشرده، ۱ BAK خام</summary>
    [ObservableProperty]
    private int _backupFormatIndex;

    /// <summary>گزینه‌های فرمت پشتیبان</summary>
    public string[] BackupFormats { get; } = { "ZIP فشرده (حجم کمتر)", "BAK خام SQL Server" };

    /// <summary>پشتیبان‌گیری فوری کامل — چرخه ۲۳: پاسخ واقعی است؛ نام فایل شمسی
    /// برگردانده می‌شود؛ چرخه ۲۴: فرمت از انتخابگر (ZIP/BAK) ارسال می‌شود</summary>
    [RelayCommand]
    private async Task RunBackupAsync()
    {
        IsBusy = true;
        var format = BackupFormatIndex == 1 ? "bak" : "zip";
        var response = await _api.PostAsync<RunBackupResponse>($"backup/run?format={format}", null);
        IsBusy = false;
        if (response is { IsSuccess: true })
        {
            _snackbar.Success((BackupFormatIndex == 1
                ? "پشتیبان خام BAK با نام شمسی ذخیره شد: "
                : "پشتیبان فشرده با نام شمسی ذخیره شد: ") + response.Data?.FileName);
            await LoadAsync();
            await LoadBackupFilesAsync();
        }
        else
            _snackbar.Error(_api.LastError ?? "پشتیبان‌گیری ناموفق بود");
    }

    /// <summary>
    /// حذف دستی فایل پشتیبان انتخاب‌شده — چرخه ۲۴ (گزارش هفدهم مالک):
    /// «امکان حذف فایل‌های پشتیبان قدیمی به‌صورت دستی و اتوماتیک هم باشد» —
    /// حذف خودکار از طریق سهمیهٔ نگهداشت شبانه انجام می‌شود؛ این فرمان حذف
    /// دستیِ فایل منتخب از دیسک + تاریخچه است (با دیالوگ فارسی تأیید).
    /// </summary>
    [RelayCommand]
    private async Task DeleteBackupFileAsync()
    {
        if (SelectedBackupFile is null)
        {
            _snackbar.Error("ابتدا یک فایل پشتیبان از فهرست انتخاب کنید");
            return;
        }

        var answer = Views.AppDialog.Confirm("تأیید حذف فایل پشتیبان",
            $"فایل «{SelectedBackupFile.FileName}» ({SelectedBackupFile.SizeText}) برای همیشه از دیسک حذف می‌شود.\n\nادامه می‌دهید؟");
        if (answer != true) return;

        IsBusy = true;
        var ok = await _api.PostAsync("backup/delete-file",
            new { fullPath = SelectedBackupFile.FullPath });
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success("فایل پشتیبان حذف شد");
            await LoadBackupFilesAsync();
        }
        else
            _snackbar.Error(_api.LastError ?? "حذف فایل ناموفق بود");
    }

    /// <summary>پاسخ پشتیبان‌گیری فوری — نام و مسیر ZIP — چرخه بیست‌وسوم</summary>
    public sealed class RunBackupResponse
    {
        public bool IsSuccess { get; set; }
        public RunBackupData? Data { get; set; }
    }

    /// <summary>دادهٔ پاسخ پشتیبان‌گیری</summary>
    public sealed class RunBackupData
    {
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// ذخیرهٔ تنظیمات پشتیبان‌گیری خودکار — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «بکاپ خودکار انجام نمی‌شود» — اکنون یک درخواست یکجا به backup/auto-config
    /// می‌رود (پیش‌تر ۱۲ درخواست جداگانه بود و «ذخیره برخی تنظیمات ناموفق بود» می‌داد)؛
    /// کارگر پس‌زمینهٔ سرور (AutomaticBackupWorker) این تنظیمات را هر ۵ دقیقه
    /// می‌خواند و پشتیبان روزانهٔ واقعی می‌گیرد.
    /// </summary>
    [RelayCommand]
    private async Task SaveAutoSettingsAsync()
    {
        // ساعت از متن قالب HH:mm جدا می‌شود (کارگر سرور فقط ساعت می‌خواهد)
        var hour = 2;
        var parts = (DailyFullTime ?? "02:00").Split(':', '،');
        if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out var h))
            hour = Math.Clamp(h, 0, 23);

        IsBusy = true;
        // اعتبارسنجی سبک مقصد ایمیل — از ارسال به نشانی نامعتبر جلوگیری می‌کند
        var emailTo = EmailBackupTo?.Trim();
        if (EmailBackupEnabled && (string.IsNullOrWhiteSpace(emailTo) || !emailTo.Contains('@')))
        {
            _snackbar.Error("برای «ارسال به ایمیل»، نشانی ایمیل مقصد را کامل وارد کنید");
            return;
        }

        var ok = await _api.PutAsync("backup/auto-config", new
        {
            enabled = DailyFullEnabled,
            hour,
            directory = BackupPath,
            emailEnabled = EmailBackupEnabled,
            emailTo = emailTo ?? string.Empty
        });
        IsBusy = false;
        if (ok) _snackbar.Success("تنظیمات پشتیبان‌گیری خودکار ذخیره شد");
        else _snackbar.Error(_api.LastError ?? "ذخیرهٔ تنظیمات ناموفق بود");
    }

    /// <summary>
    /// بارگذاری تنظیمات پشتیبان‌گیری خودکار از سرور — چرخه بیست‌ویکم:
    /// وضعیت فعال/ساعت/مسیر در فرم نمایش داده می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task LoadAutoSettingsAsync()
    {
        var response = await _api.GetAsync<AutoConfigResponse>("backup/auto-config");
        var d = response?.Data;
        if (d is null) return;
        DailyFullEnabled = d.Enabled;
        DailyFullTime = $"{d.Hour:00}:00";
        if (!string.IsNullOrWhiteSpace(d.Directory))
            BackupPath = d.Directory;
        EmailBackupEnabled = d.EmailEnabled;
        EmailBackupTo = d.EmailTo;
    }

    /// <summary>
    /// بازیابی از پشتیبان انتخاب‌شدهٔ تاریخچه — چرخه بیست‌وسوم (گزارش شانزدهم
    /// مالک): بازیابی اکنون واقعی است — سرور ZIP را استخراج و RESTORE DATABASE
    /// با REPLACE اجرا می‌کند. پیش‌تر فقط پیام راهنما نشان داده می‌شد.
    /// </summary>
    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedBackup is null) { _snackbar.Info("ابتدا یک پشتیبان از فهرست انتخاب کنید"); return; }
        if (string.IsNullOrWhiteSpace(SelectedBackup.FilePath))
        { _snackbar.Error("این رکورد مسیر فایل ندارد — از فهرست فایل‌ها استفاده کنید"); return; }

        // تأیید دو‌مرحله‌ای — پیام هشدار طبق فرم ۲۹
        var answer = Views.AppDialog.Confirm("تأیید بازیابی",
            $"⚠️ هشدار: بازیابی از پشتیبان {SelectedBackup.DateText} تمام داده‌های فعلی را جایگزین می‌کند و همهٔ کاربران موقتاً قطع می‌شوند!\n\nادامه می‌دهید؟",
            Views.AppDialogIcon.Warning);
        if (answer != true) return;

        await ExecuteRestoreAsync(SelectedBackup.FilePath);
    }

    /// <summary>
    /// فهرست فایل‌های پشتیبان پوشه — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «امکان انتخاب دستی فایل پشتیبان برای بازیابی» — از endpoint تازهٔ
    /// backup/files (تازه‌ترین اول، ZIP/BAK).
    /// </summary>
    [RelayCommand]
    private async Task LoadBackupFilesAsync()
    {
        var response = await _api.GetAsync<BackupFilesResponse>("backup/files");
        if (response?.Data is null) return;

        BackupFiles = new ObservableCollection<BackupFileRow>(response.Data.Select(f => new BackupFileRow(
            f.FileName ?? "—",
            f.FullPath ?? "",
            f.SizeText ?? "—",
            f.LastWrite is { } t
                ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsiFa(t)
                : "—",
            f.Kind == "zip" ? "ZIP فشرده" : "BAK خام")));
    }

    /// <summary>بازیابی از فایل دستی انتخاب‌شده — چرخه بیست‌وسوم</summary>
    [RelayCommand]
    private async Task RestoreSelectedFileAsync()
    {
        if (SelectedBackupFile is null)
        { _snackbar.Info("ابتدا یک فایل پشتیبان از فهرست انتخاب کنید"); return; }

        var answer = Views.AppDialog.Confirm("تأیید بازیابی",
            $"⚠️ هشدار: بازیابی از فایل «{SelectedBackupFile.FileName}» تمام داده‌های فعلی را جایگزین می‌کند و همهٔ کاربران موقتاً قطع می‌شوند!\n\nادامه می‌دهید؟",
            Views.AppDialogIcon.Warning);
        if (answer != true) return;

        await ExecuteRestoreAsync(SelectedBackupFile.FullPath);
    }

    /// <summary>
    /// انتخاب پوشهٔ ذخیرهٔ محلی بکاپ خودکار با دیالوگ پوشهٔ ویندوز —
    /// گزارش مالک: «برای محل ذخیره محلی دکمه انتخاب پوشه هم باشه».
    /// </summary>
    [RelayCommand]
    private void BrowseBackupPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "انتخاب پوشهٔ ذخیرهٔ نسخه‌های پشتیبان",
        };
        if (!string.IsNullOrWhiteSpace(BackupPath) && System.IO.Directory.Exists(BackupPath))
            dialog.InitialDirectory = BackupPath;
        if (dialog.ShowDialog() == true)
            BackupPath = dialog.FolderName;
    }

    /// <summary>
    /// انتخاب فایل پشتیبان از دیسک/فلش و بازیابی — چرخهٔ بیست‌وششم (گزارش
    /// هجدهم مالک): «امکان انتخاب فایل پشتیبانی که در لیست نیست وجود ندارد
    /// و می‌خوام از هارد یا فلش انتخاب کنم». با دیالوگ انتخاب فایل ویندوز
    /// هر ZIP/BAK از هر درایوی انتخاب می‌شود، سپس با فرم چندبخشی به سرور
    /// بارگذاری و همان‌جا بازیابی می‌شود — مستقل از اینکه سرور روی همین
    /// ماشین باشد یا ماشین دیگری.
    /// </summary>
    [RelayCommand]
    private async Task RestoreFromExternalFileAsync()
    {
        // دیالوگ انتخاب فایل — عنوان فارسی و فقط پسوندهای پشتیبان
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "انتخاب فایل پشتیبان (ZIP/BAK) از هارد یا فلش",
            Filter = "فایل پشتیبان (*.zip;*.bak)|*.zip;*.bak|همهٔ فایل‌ها (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var fileName = System.IO.Path.GetFileName(dialog.FileName);
        long sizeBytes;
        try
        {
            sizeBytes = new System.IO.FileInfo(dialog.FileName).Length;
        }
        catch
        {
            _snackbar.Error("خواندن فایل ممکن نشد — مسیر را بررسی کنید");
            return;
        }
        var sizeText = sizeBytes > 1024 * 1024 * 1024
            ? $"{sizeBytes / 1024.0 / 1024 / 1024:0.#} گیگابایت"
            : sizeBytes > 1024 * 1024 ? $"{sizeBytes / 1024.0 / 1024:0.#} مگابایت" : $"{sizeBytes / 1024.0:0.#} کیلوبایت";

        // تأیید دو‌مرحله‌ای — مانند بازیابی از فهرست
        var answer = Views.AppDialog.Confirm("تأیید بازیابی از فایل خارجی",
            $"⚠️ هشدار: بازیابی از فایل «{fileName}» ({sizeText}) تمام داده‌های فعلی را جایگزین می‌کند و همهٔ کاربران موقتاً قطع می‌شوند!\n\nادامه می‌دهید؟",
            Views.AppDialogIcon.Warning);
        if (answer != true) return;

        // بارگذاری چندبخشی به سرور — بدنهٔ بزرگ؛ وضعیت به‌روز می‌شود
        IsBusy = true;
        RestoreStatus = "در حال بارگذاری فایل به سرور…";
        try
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
            var response = await _api.PostFileAsync<RestoreUploadResponse>("backup/upload-restore",
                fileName, bytes);
            IsBusy = false;

            if (response?.IsSuccess == true)
            {
                RestoreStatus = "بازیابی با موفقیت انجام شد — برنامه را ببندید و دوباره باز کنید";
                _snackbar.Success("بازیابی انجام شد — برنامه را ری‌استارت کنید");
            }
            else
            {
                RestoreStatus = $"بازیابی ناموفق: {response?.Message ?? _api.LastError}";
                _snackbar.Error(response?.Message ?? _api.LastError ?? "بازیابی ناموفق بود");
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            RestoreStatus = $"بازیابی ناموفق: {ex.Message}";
            _snackbar.Error("خواندن/بارگذاری فایل ناموفق بود — فایل سالم و در دسترس است؟");
        }
    }

    /// <summary>پاسخ بارگذاری-و-بازیابی — پاکت استاندارد با پیام</summary>
    private sealed class RestoreUploadResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>اجرای بازیابی واقعی از طریق سرور — پیام نتیجه به کاربر</summary>
    private async Task ExecuteRestoreAsync(string fullPath)
    {
        IsBusy = true;
        RestoreStatus = "در حال بازیابی… سرور فایل را استخراج و RESTORE اجرا می‌کند";
        var ok = await _api.PostAsync("backup/restore", new { fullPath });
        IsBusy = false;

        if (ok)
        {
            RestoreStatus = "بازیابی با موفقیت انجام شد — برنامه را ببندید و دوباره باز کنید";
            _snackbar.Success("بازیابی انجام شد — برنامه را ری‌استارت کنید");
        }
        else
        {
            RestoreStatus = $"بازیابی ناموفق: {_api.LastError}";
            _snackbar.Error(_api.LastError ?? "بازیابی ناموفق بود");
        }
    }

    /// <summary>قالب‌بندی تاریخ شمسی</summary>
    private static string FormatDate(DateTime? utc)
        => utc is null ? "—" : Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(utc.Value);

    /// <summary>قالب‌بندی حجم</summary>
    private static string FormatSize(long? bytes)
        => bytes is null or 0 ? "—" : bytes > 1024 * 1024 * 1024
            ? $"{bytes / 1024.0 / 1024 / 1024:0.#} GB"
            : $"{bytes / 1024.0 / 1024:0.#} MB";

    /* ─── پشتیبان ابری — دستورات چرخه سیزدهم (درخواست ۷-۳) ─── */

    /// <summary>
    /// بارگذاری پیکربندی ابری برای فرم تنظیم — چرخه بیستم (گزارش چهاردهم مالک):
    /// «جایی برای فعال و تنظیم کردن پشتیبان ابری نیست»
    /// </summary>
    [RelayCommand]
    private async Task LoadCloudConfigAsync()
    {
        var response = await _api.GetAsync<CloudConfigResponse>("backup/cloud/config");
        var d = response?.Data;
        if (d is null) return;

        CloudProviderIndex = d.Provider switch
        {
            "s3" or "s3compatible" or "arvan" or "abrar" => 1,
            "local" or "folder" => 2,
            _ => 0,
        };
        CloudEndpoint = d.Endpoint;
        CloudBucket = d.Bucket;
        CloudRegion = string.IsNullOrWhiteSpace(d.Region) ? "us-east-1" : d.Region;
        CloudPrefix = string.IsNullOrWhiteSpace(d.Prefix) ? "backups/" : d.Prefix;
        CloudLocalFolder = d.LocalFolder;
        CloudHasStoredKeys = d.HasAccessKey;
        CloudAutoUpload = d.AutoUpload;
        // کلیدهای رمزی هرگز به کلاینت برنمی‌گردند — کادرها خالی می‌مانند
        CloudAccessKey = null;
        CloudSecretKey = null;
        CloudEncryptionKey = null;
    }

    /// <summary>
    /// ذخیرهٔ پیکربندی ابری — چرخه بیستم: کلیدهای رمزیِ خالی ارسال نمی‌شوند
    /// تا مقدار ذخیره‌شدهٔ قبلی حفظ شود
    /// </summary>
    [RelayCommand]
    private async Task SaveCloudConfigAsync()
    {
        IsBusy = true;
        var ok = await _api.PutAsync("backup/cloud/config", new
        {
            provider = CloudProviderIndex switch { 1 => "s3", 2 => "local", _ => "" },
            endpoint = CloudEndpoint?.Trim(),
            bucket = CloudBucket?.Trim(),
            region = CloudRegion?.Trim(),
            prefix = CloudPrefix?.Trim(),
            localFolder = CloudLocalFolder?.Trim(),
            // null = بدون تغییر برای کلیدهای رمزی
            accessKey = string.IsNullOrWhiteSpace(CloudAccessKey) ? null : CloudAccessKey.Trim(),
            secretKey = string.IsNullOrWhiteSpace(CloudSecretKey) ? null : CloudSecretKey.Trim(),
            encryptionKey = string.IsNullOrWhiteSpace(CloudEncryptionKey) ? null : CloudEncryptionKey.Trim(),
            autoUpload = CloudAutoUpload,
        });
        IsBusy = false;
        if (!ok)
        {
            _snackbar.Error(_api.LastError ?? "ذخیرهٔ تنظیمات ابری ناموفق بود");
            return;
        }
        _snackbar.Success(CloudProviderIndex == 0
            ? "پشتیبان ابری غیرفعال شد"
            : "تنظیمات پشتیبان ابری ذخیره شد — با «تست اتصال» صحت آن را بررسی کنید");
        await LoadCloudConfigAsync();
        await LoadCloudStatusAsync();
    }

    /// <summary>بارگذاری وضعیت پشتیبان ابری</summary>
    [RelayCommand]
    private async Task LoadCloudStatusAsync()
    {
        var response = await _api.GetAsync<CloudStatusResponse>("backup/cloud/status");
        var d = response?.Data;
        if (d is null) return;

        CloudStatusBadge = d.Enabled ? (d.Encrypted ? "فعال • رمزنگاری‌شده" : "فعال") : "غیرفعال";
        CloudStatusColor = d.Enabled ? "#2E7D32" : "#9E9E9E";
        CloudLastUploadText = d.LastUpload is null
            ? "هنوز نسخه‌ای بارگذاری نشده"
            : $"{FormatDate(d.LastUpload)} ({d.LastFile ?? "—"})";
        CloudVersionsText = $"{d.Versions} نسخه";
        CloudTotalSizeText = FormatSize(d.TotalSize);
        CanVerifyCloud = d.Versions > 0;
    }

    /// <summary>تست اتصال به مقصد ابری</summary>
    [RelayCommand]
    private async Task CloudTestAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<CloudTestResponse>("backup/cloud/test", null);
            var d = response?.Data;
            if (d is null) { _snackbar.Error(_api.LastError ?? "تست اتصال ناموفق بود"); return; }
            CloudMessage = d.Message;
            if (d.Ok) _snackbar.Success(d.Message);
            else _snackbar.Error(d.Message);
        }
        finally { IsBusy = false; }
    }

    /// <summary>بارگذاری دستی آخرین پشتیبان به ابر</summary>
    [RelayCommand]
    private async Task CloudUploadAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<CloudUploadResponse>("backup/cloud/upload-latest", null);
            var d = response?.Data;
            if (d is null) { _snackbar.Error(_api.LastError ?? "بارگذاری ابری ناموفق بود"); return; }
            CloudMessage = $"{d.Message} — {d.FileName} ({FormatSize(d.Size)}{(d.Encrypted ? "، رمزنگاری‌شده" : "")})";
            _snackbar.Success("بارگذاری ابری انجام شد");
            await LoadCloudStatusAsync();
        }
        finally { IsBusy = false; }
    }

    /// <summary>تست رمزگشایی آخرین نسخه ابری — بدون دست‌زدن به بانک اصلی</summary>
    [RelayCommand]
    private async Task CloudVerifyAsync()
    {
        // آخرین رکورد ابری از وضعیت گرفته می‌شود؛ endpoint رکورد محور است
        var response = await _api.GetAsync<CloudStatusResponse>("backup/cloud/status");
        var s = response?.Data;
        if (s is null || s.Versions == 0) { _snackbar.Info("نسخه ابری برای تست وجود ندارد"); return; }

        // شناسه آخرین رکورد ابری از تاریخچه — جدیدترین ردیف با سطر ابری
        var history = await _api.GetAsync<BackupHistoryResponse>("backup/history");
        var cloudRow = (history?.Data ?? new List<BackupDto>())
            .FirstOrDefault(b => b.Storage == "Cloud" || b.CloudFileName is not null);
        if (cloudRow is null) { _snackbar.Info("رکورد ابری در تاریخچه پیدا نشد"); return; }

        IsBusy = true;
        try
        {
            var verify = await _api.PostAsync<CloudVerifyResponse>($"backup/cloud/verify-restore/{cloudRow.Id}", null);
            var v = verify?.Data;
            if (v is null) { _snackbar.Error(_api.LastError ?? "تست بازیابی ناموفق بود"); return; }
            CloudMessage = v.Message;
            if (v.Ok) _snackbar.Success(v.Message);
            else _snackbar.Error(v.Message);
        }
        finally { IsBusy = false; }
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class BackupHistoryResponse
    {
        public bool IsSuccess { get; set; }
        public List<BackupDto>? Data { get; set; }
    }

    private sealed class BackupDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = "Full";
        public string Status { get; set; } = "Success";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? FileSize { get; set; }
        public bool IsAutomatic { get; set; }
        public string? FilePath { get; set; }
        public string? Storage { get; set; }
        public string? CloudFileName { get; set; }

        /// <summary>پیام خطا در صورت ناموفقی — چرخه بیست‌ویکم برای پنل جزئیات</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>پاسخ تنظیمات پشتیبان خودکار — چرخه بیست‌ویکم</summary>
    private sealed class AutoConfigResponse
    {
        public bool IsSuccess { get; set; }
        public AutoConfigData? Data { get; set; }
    }

    private sealed class AutoConfigData
    {
        public bool Enabled { get; set; }
        public int Hour { get; set; }
        public string? Directory { get; set; }

        /// <summary>ارسال بکاپ خودکار به ایمیل — چرخهٔ ۸۲</summary>
        public bool EmailEnabled { get; set; }

        /// <summary>ایمیل مقصد بکاپ — چرخهٔ ۸۲</summary>
        public string? EmailTo { get; set; }
    }

    private sealed class CloudStatusResponse
    {
        public bool IsSuccess { get; set; }
        public CloudStatusData? Data { get; set; }
    }

    private sealed class CloudStatusData
    {
        public bool Enabled { get; set; }
        public string? Provider { get; set; }
        public bool Encrypted { get; set; }
        public DateTime? LastUpload { get; set; }
        public string? LastFile { get; set; }
        public int Versions { get; set; }
        public long TotalSize { get; set; }
    }

    /// <summary>پاسخ پیکربندی ابری — چرخه بیستم</summary>
    private sealed class CloudConfigResponse
    {
        public bool IsSuccess { get; set; }
        public CloudConfigData? Data { get; set; }
    }

    /// <summary>دادهٔ پیکربندی ابری — کلیدها فقط به‌صورت پرچم برمی‌گردند</summary>
    private sealed class CloudConfigData
    {
        public string? Provider { get; set; }
        public string? Endpoint { get; set; }
        public string? Bucket { get; set; }
        public string? Region { get; set; }
        public string? Prefix { get; set; }
        public string? LocalFolder { get; set; }
        public bool HasAccessKey { get; set; }
        public bool HasSecretKey { get; set; }
        public bool HasEncryptionKey { get; set; }
        public bool AutoUpload { get; set; }
    }

    private sealed class CloudTestResponse
    {
        public bool IsSuccess { get; set; }
        public CloudTestData? Data { get; set; }
    }

    private sealed class CloudTestData
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
    }

    private sealed class CloudUploadResponse
    {
        public bool IsSuccess { get; set; }
        public CloudUploadData? Data { get; set; }
    }

    private sealed class CloudUploadData
    {
        public string FileName { get; set; } = "";
        public long Size { get; set; }
        public bool Encrypted { get; set; }
        public string Message { get; set; } = "";
    }

    private sealed class CloudVerifyResponse
    {
        public bool IsSuccess { get; set; }
        public CloudVerifyData? Data { get; set; }
    }

    private sealed class CloudVerifyData
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public long PlainSize { get; set; }
    }

    /// <summary>پاسخ فهرست فایل‌های پشتیبان — چرخه بیست‌وسوم (backup/files)</summary>
    private sealed class BackupFilesResponse
    {
        public bool IsSuccess { get; set; }
        public List<BackupFileDto>? Data { get; set; }
    }

    /// <summary>ردیف فایل پشتیبان از سرور — منطبق بر خروجی BackupController.ListFiles</summary>
    private sealed class BackupFileDto
    {
        public string? FileName { get; set; }
        public string? FullPath { get; set; }
        public long SizeBytes { get; set; }
        public string? SizeText { get; set; }
        public DateTime? LastWrite { get; set; }
        public string? Kind { get; set; }
    }
}
