namespace Sahand.ClinicPro.Domain.Interfaces;

/// <summary>اطلاعات یک شیء ذخیره‌شده در ابر</summary>
public record CloudObjectInfo(
    string FileName,
    long Size,
    DateTime LastModified);

/// <summary>
/// رابط ذخیره‌سازی ابری فایل‌های پشتیبان — چرخه سیزدهم (درخواست ۷-۳).
/// پیاده‌سازی‌ها: S3-سازگار (AWS/آروان/ابستور) و پوشه محلی؛
/// رمزنگاری AES-256-GCM بر عهده سرویس ارکستراتور است تا پیاده‌سازی‌ها ساده بمانند.
/// </summary>
public interface ICloudBackupStorage
{
    /// <summary>آیا پیکربندی معتبر دارد؟ (بدون فراخوانی شبکه)</summary>
    bool IsConfigured { get; }

    /// <summary>نام ارائه‌دهنده برای ثبت در تاریخچه</summary>
    string ProviderName { get; }

    /// <summary>بارگذاری فایل پشتیبان به ابر</summary>
    /// <exception cref="InvalidOperationException">پیکربندی ناقص</exception>
    Task<CloudObjectInfo> UploadAsync(string fileName, Stream content, CancellationToken ct = default);

    /// <summary>بارگذاری مجدد فایل از ابر — برای تست صحت رمزگشایی هنگام بازیابی</summary>
    Task<Stream> DownloadAsync(string fileName, CancellationToken ct = default);

    /// <summary>فهرست فایل‌های پشتیبان ابر</summary>
    Task<IReadOnlyList<CloudObjectInfo>> ListAsync(CancellationToken ct = default);

    /// <summary>تست اتصال به مقصد ابری — پیام فارسی موفق/خطا برمی‌گرداند</summary>
    Task<(bool Ok, string Message)> TestConnectionAsync(CancellationToken ct = default);
}
