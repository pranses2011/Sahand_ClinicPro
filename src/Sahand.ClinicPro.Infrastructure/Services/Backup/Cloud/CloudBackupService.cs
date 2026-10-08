using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;

/// <summary>
/// ارکستراتور پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳).
/// انتخاب ارائه‌دهنده از پیکربندی (Backup:Cloud:Provider = s3 | local | خالی=غیرفعال)،
/// رمزنگاری AES-256-GCM با کلید Backup:Cloud:EncryptionKey،
/// بارگذاری فایل پشتیبان و به‌روزرسانی رکورد تاریخچه.
/// </summary>
public class CloudBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudBackupService> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    /// <summary>سازنده با پیکربندی و لاگر — بدون کارخانه HTTP برای استفاده سبک</summary>
    public CloudBackupService(IConfiguration configuration, ILogger<CloudBackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>سازنده کامل با کارخانه HTTP برای S3</summary>
    public CloudBackupService(IConfiguration configuration, ILogger<CloudBackupService> logger,
        IHttpClientFactory httpClientFactory) : this(configuration, logger)
        => _httpClientFactory = httpClientFactory;

    /// <summary>آیا پشتیبان ابری فعال است؟</summary>
    public bool IsEnabled
        => !string.IsNullOrWhiteSpace(_configuration["Backup:Cloud:Provider"]);

    /// <summary>آیا رمزنگاری فایل‌های ابری پیکربندی شده است؟</summary>
    public bool IsEncryptionEnabled
        => !string.IsNullOrWhiteSpace(_configuration["Backup:Cloud:EncryptionKey"]);

    /// <summary>ساخت ذخیره‌ساز ابری مطابق پیکربندی — تهی وقتی فعال نیست</summary>
    public ICloudBackupStorage? CreateStorage()
    {
        var provider = _configuration["Backup:Cloud:Provider"]?.Trim().ToLowerInvariant();
        return provider switch
        {
            "s3" or "s3compatible" or "arvan" or "abrar" => CreateS3(),
            "local" or "folder" => new LocalFolderCloudStorage(
                _configuration["Backup:Cloud:LocalFolder"] ?? ""),
            _ => null,
        };
    }

    /// <summary>ساخت ذخیره‌ساز S3 از پیکربندی</summary>
    private ICloudBackupStorage? CreateS3()
    {
        var storage = new S3CompatibleCloudStorage(
            _httpClientFactory?.CreateClient("CloudBackup") ?? new HttpClient(),
            _configuration["Backup:Cloud:Endpoint"] ?? "",
            _configuration["Backup:Cloud:Bucket"] ?? "",
            _configuration["Backup:Cloud:AccessKey"] ?? "",
            _configuration["Backup:Cloud:SecretKey"] ?? "",
            _configuration["Backup:Cloud:Region"] ?? "us-east-1",
            _configuration["Backup:Cloud:Prefix"] ?? "backups/");
        return storage.IsConfigured ? storage : null;
    }

    /// <summary>بارگذاری فایل یک رکورد پشتیبان به ابر — با رمزنگاری اختیاری</summary>
    /// <remarks>خطا به فراخواننده می‌رسد؛ BackupJob آن را best-effort می‌گیرد</remarks>
    public async Task<CloudObjectInfo> UploadBackupAsync(BackupHistory record, CancellationToken ct = default)
    {
        var storage = CreateStorage()
            ?? throw new InvalidOperationException("پشتیبان ابری فعال نیست");

        if (string.IsNullOrWhiteSpace(record.FilePath) || !File.Exists(record.FilePath))
            throw new InvalidOperationException(
                $"فایل پشتیبان «{record.FilePath}» روی دیسک موجود نیست — ابتدا پشتیبان محلی بسازید");

        var bytes = await File.ReadAllBytesAsync(record.FilePath, ct);
        var fileName = Path.GetFileName(record.FilePath);

        // رمزنگاری اختیاری AES-256-GCM — چرخه بیست‌وسوم رفع CS8600: کلید تهی‌پذیر
        byte[]? key = LoadEncryptionKey();
        if (key is not null)
        {
            bytes = AesBackupEncryptor.Encrypt(bytes, key);
            fileName += ".enc";
        }

        using var stream = new MemoryStream(bytes);
        var uploaded = await storage.UploadAsync(fileName, stream, ct);

        // به‌روزرسانی رکورد — ابری و رمزشده
        record.StorageType = StorageType.Cloud;
        record.CloudFileName = uploaded.FileName;
        record.CloudUploadedAt = DateTime.UtcNow;
        record.CloudProvider = storage.ProviderName;
        record.IsCloudEncrypted = key is not null;
        record.FileSize = bytes.Length;
        _logger.LogInformation(
            "پشتیبان به ابر بارگذاری شد — {Provider}://{Key} ({Size:N0} بایت{Enc})",
            storage.ProviderName, uploaded.FileName, uploaded.Size, key is not null ? "، رمزشده" : "");
        return uploaded;
    }

    /// <summary>تست صحت بازیابی — دانلود از ابر + رمزگشایی در حافظه (بدون بازنویسی)</summary>
    /// <remarks>ماهیت «آتش‌نشان»: صحت کلید و سالم‌بودن فایل بدون دست‌زدن به بانک اصلی</remarks>
    public async Task<(bool Ok, string Message, long PlainSize)> VerifyRestoreAsync(
        string cloudFileName, CancellationToken ct = default)
    {
        var storage = CreateStorage()
            ?? throw new InvalidOperationException("پشتیبان ابری فعال نیست");

        try
        {
            await using var download = await storage.DownloadAsync(cloudFileName, ct);
            using var ms = new MemoryStream();
            await download.CopyToAsync(ms, ct);
            var blob = ms.ToArray();

            if (cloudFileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) ||
                AesBackupEncryptor.IsEncrypted(blob))
            {
                var key = LoadEncryptionKey()
                    ?? throw new InvalidOperationException(
                        "فایل رمزشده است اما کلید Backup:Cloud:EncryptionKey تنظیم نشده است");
                var plain = AesBackupEncryptor.Decrypt(blob, key);
                return (true, $"رمزگشایی موفق — {plain.Length:N0} بایت سالم", plain.Length);
            }

            return (true, $"فایل سالم دانلود شد — {blob.Length:N0} بایت", blob.Length);
        }
        catch (Exception ex)
        {
            return (false, $"تست بازیابی ناموفق — {ex.Message}", 0);
        }
    }

    /// <summary>خواندن کلید رمزنگاری از پیکربندی — تهی اگر تنظیم نشده</summary>
    /// <exception cref="InvalidOperationException">کلید تنظیم‌شده اما نامعتبر</exception>
    public byte[]? LoadEncryptionKey()
    {
        var base64 = _configuration["Backup:Cloud:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            var key = Convert.FromBase64String(base64);
            if (key.Length != 32)
                throw new InvalidOperationException("کلید رمزنگاری باید ۳۲ بایت باشد");
            return key;
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "کلید رمزنگاری ابری Base64 نامعتبر است — با ابزار تولید کلید بسازید");
        }
    }
}
