using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف تاریخچه بکاپ — فرم ۲۹</summary>
public record BackupHistoryRow(long Id, string TypeText, string StatusText, string StatusColor,
    string DateText, string SizeText, string? FilePath, bool IsAutomatic);

/// <summary>
/// ViewModel بکاپ و بازیابی — گام U15 طبق فرم ۲۹:
/// وضعیت + تاریخچه + بکاپ فوری + تنظیمات خودکار سه‌سطحی + بازیابی
/// </summary>
public partial class BackupViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "بکاپ و بازیابی";

    /// <summary>تاریخچه بکاپ‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<BackupHistoryRow> _history = new();

    /// <summary>متن وضعیت آخرین بکاپ کامل</summary>
    [ObservableProperty]
    private string _lastFullText = "—";

    /// <summary>متن وضعیت آخرین بکاپ لگ</summary>
    [ObservableProperty]
    private string _lastLogText = "—";

    /// <summary>بکاپ انتخاب‌شده</summary>
    [ObservableProperty]
    private BackupHistoryRow? _selectedBackup;

    /* ─── تنظیمات بکاپ خودکار ─── */

    /// <summary>بکاپ کامل روزانه فعال</summary>
    [ObservableProperty]
    private bool _dailyFullEnabled = true;

    /// <summary>ساعت بکاپ کامل</summary>
    [ObservableProperty]
    private string _dailyFullTime = "03:00";

    /// <summary>بکاپ تفاضلی فعال — هر چند ساعت</summary>
    [ObservableProperty]
    private bool _diffEnabled = true;

    /// <summary>فاصله بکاپ تفاضلی (ساعت)</summary>
    [ObservableProperty]
    private int _diffHours = 6;

    /// <summary>بکاپ لگ تراکنش فعال</summary>
    [ObservableProperty]
    private bool _logEnabled = true;

    /// <summary>فاصله بکاپ لگ (ساعت)</summary>
    [ObservableProperty]
    private int _logHours = 1;

    /// <summary>مسیر ذخیره محلی</summary>
    [ObservableProperty]
    private string _backupPath = @"D:\\ClinicPro\\Backups";

    /// <summary>نگهداری بکاپ کامل (روز)</summary>
    [ObservableProperty]
    private int _keepFullDays = 30;

    /// <summary>نگهداری تفاضلی (روز)</summary>
    [ObservableProperty]
    private int _keepDiffDays = 7;

    /// <summary>نگهداری لگ (ساعت)</summary>
    [ObservableProperty]
    private int _keepLogHours = 48;

    /// <summary>رمزنگاری فایل بکاپ</summary>
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

    /// <summary>آیا بکاپ انتخاب‌شده نسخه ابری دارد؟</summary>
    [ObservableProperty]
    private bool _canVerifyCloud;

    /// <summary>شناسه آخرین رکورد ابری برای تست رمزگشایی</summary>
    private long? _lastCloudRecordId;

    /// <summary>سازنده</summary>
    public BackupViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
        _ = LoadCloudStatusAsync();
    }

    /// <summary>بارگذاری تاریخچه بکاپ‌ها</summary>
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
                    FormatDate(b.StartedAt), FormatSize(b.FileSize), b.FilePath, b.IsAutomatic);
            }).ToList();

            History = new ObservableCollection<BackupHistoryRow>(rows);

            var lastFull = rows.FirstOrDefault(r => r.TypeText == "کامل" && r.StatusText == "✅ موفق");
            LastFullText = lastFull is null ? "بکاپ کاملی ثبت نشده" : $"✅ {lastFull.DateText} ({lastFull.SizeText})";
            var lastLog = rows.FirstOrDefault(r => r.TypeText == "لگ");
            LastLogText = lastLog is null ? "—" : $"{lastLog.DateText} ({lastLog.SizeText})";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بکاپ فوری کامل</summary>
    [RelayCommand]
    private async Task RunBackupAsync()
    {
        IsBusy = true;
        var ok = await _api.PostAsync("backup/run", null);
        IsBusy = false;
        if (ok) { _snackbar.Success("بکاپ فوری ثبت شد — اجرای فایل توسط سرویس پشتیبان انجام می‌شود"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ثبت بکاپ ناموفق بود");
    }

    /// <summary>ذخیره تنظیمات بکاپ خودکار</summary>
    [RelayCommand]
    private async Task SaveAutoSettingsAsync()
    {
        IsBusy = true;
        var allOk = true;
        var entries = new Dictionary<string, string?>
        {
            ["DailyFullEnabled"] = DailyFullEnabled ? "true" : "false",
            ["DailyFullTime"] = DailyFullTime,
            ["DiffEnabled"] = DiffEnabled ? "true" : "false",
            ["DiffHours"] = DiffHours.ToString(),
            ["LogEnabled"] = LogEnabled ? "true" : "false",
            ["LogHours"] = LogHours.ToString(),
            ["BackupPath"] = BackupPath,
            ["KeepFullDays"] = KeepFullDays.ToString(),
            ["KeepDiffDays"] = KeepDiffDays.ToString(),
            ["KeepLogHours"] = KeepLogHours.ToString(),
            ["EncryptBackup"] = EncryptBackup ? "true" : "false",
            ["NotifyOnError"] = NotifyOnError ? "true" : "false"
        };
        foreach (var (key, value) in entries)
        {
            if (!await _api.PutAsync("settings", new { key, value, category = "Backup" }))
                allOk = false;
        }
        IsBusy = false;
        if (allOk) _snackbar.Success("تنظیمات بکاپ خودکار ذخیره شد");
        else _snackbar.Error("ذخیره برخی تنظیمات ناموفق بود");
    }

    /// <summary>بازیابی از بکاپ انتخاب‌شده — با هشدار جایگزینی داده</summary>
    [RelayCommand]
    private void Restore()
    {
        if (SelectedBackup is null) { _snackbar.Info("ابتدا یک بکاپ از فهرست انتخاب کنید"); return; }

        // تأیید دو‌مرحله‌ای — پیام هشدار طبق فرم ۲۹
        var answer = System.Windows.MessageBox.Show(
            $"⚠️ هشدار: بازیابی از بکاپ {SelectedBackup.DateText} تمام داده‌های فعلی را جایگزین می‌کند!\n\nادامه می‌دهید؟",
            "تأیید بازیابی", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

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
