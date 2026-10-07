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

    /// <summary>پشتیبان‌گیری فوری کامل</summary>
    [RelayCommand]
    private async Task RunBackupAsync()
    {
        IsBusy = true;
        var ok = await _api.PostAsync("backup/run", null);
        IsBusy = false;
        if (ok) { _snackbar.Success("پشتیبان‌گیری فوری ثبت شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ثبت پشتیبان‌گیری ناموفق بود");
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
        var ok = await _api.PutAsync("backup/auto-config", new
        {
            enabled = DailyFullEnabled,
            hour,
            directory = BackupPath
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
    }

    /// <summary>بازیابی از پشتیبان انتخاب‌شده — با هشدار جایگزینی داده</summary>
    [RelayCommand]
    private void Restore()
    {
        if (SelectedBackup is null) { _snackbar.Info("ابتدا یک پشتیبان از فهرست انتخاب کنید"); return; }

        // تأیید دو‌مرحله‌ای — پیام هشدار طبق فرم ۲۹
        var answer = Views.AppDialog.Confirm("تأیید بازیابی", $"⚠️ هشدار: بازیابی از پشتیبان {SelectedBackup.DateText} تمام داده‌های فعلی را جایگزین می‌کند!\n\nادامه می‌دهید؟", Views.AppDialogIcon.Warning);
        if (answer != true) return;

        RestoreStatus = "بازیابی در نسخه استقرار سرور توسط ادمین سامانه و سرویس پشتیبان انجام می‌شود";
        _snackbar.Success("راهنمای بازیابی: فایل .bak انتخاب‌شده را به SQL Server Restore کنید");
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
}
