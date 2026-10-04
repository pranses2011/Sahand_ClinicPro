namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// پنل پیامک - تنظیمات ده پنل ایرانی با احراز رمزنگاری‌شده
/// </summary>
public class SmsProvider : AuditableEntity, ISoftDeletable
{
    /// <summary>نام پنل مثل کاوه‌نگار</summary>
    public string ProviderName { get; set; } = null!;

    /// <summary>کد یکتای پنل</summary>
    public string? ProviderCode { get; set; }

    /// <summary>نشانی API</summary>
    public string? ApiUrl { get; set; }

    /// <summary>کلید API رمزنگاری‌شده با AES-256</summary>
    public byte[]? ApiKeyEncrypted { get; set; }

    /// <summary>نام کاربری پنل</summary>
    public string? Username { get; set; }

    /// <summary>رمز عبور رمزنگاری‌شده</summary>
    public byte[]? PasswordEncrypted { get; set; }

    /// <summary>شماره فرستنده</summary>
    public string? SenderNumber { get; set; }

    /// <summary>اولویت در زنجیره جایگزینی</summary>
    public int Priority { get; set; } = 1;

    /// <summary>موجودی اعتبار</summary>
    public decimal Balance { get; set; } = 0m;

    /// <summary>پنل پیش‌فرض</summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;
}
