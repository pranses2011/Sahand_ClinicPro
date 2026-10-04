using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Licensing;

/// <summary>
/// لایسنس محلی درمانگاه - رکورد رمزنگاری‌شده با امضای RSA-2048
/// </summary>
/// <remarks>
/// اعتبارسنجی سه‌مرحله‌ای: اثر انگشت سخت‌افزار و انقضا و امضای RSA
/// heartbeat هر ۲۴ ساعت با عطف‌بندی ۳۰ روز آفلاین
/// </remarks>
public class ClinicLicense : AuditableEntity, ISoftDeletable
{
    /// <summary>کلید لایسنس</summary>
    public string LicenseKey { get; set; } = null!;

    /// <summary>کد فعال‌سازی</summary>
    public string? ActivationCode { get; set; }

    /// <summary>نوع لایسنس - اشتراکی یا دائمی</summary>
    public LicenseType LicenseType { get; set; }

    /// <summary>حداکثر کاربر همزمان</summary>
    public int MaxUsers { get; set; } = 1;

    /// <summary>حداکثر پزشک</summary>
    public int MaxDoctors { get; set; } = 1;

    /// <summary>حداکثر بیمار</summary>
    public int MaxPatients { get; set; } = 1000;

    /// <summary>حداکثر شعبه</summary>
    public int MaxBranches { get; set; } = 1;

    /// <summary>ماژول‌های فعال - JSON رمزنگاری‌شده</summary>
    public string? EnabledModules { get; set; }

    /// <summary>تاریخ صدور</summary>
    public DateOnly? IssuedDate { get; set; }

    /// <summary>تاریخ انقضا - تهی یعنی دائمی</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>اثر انگشت سخت‌افزاری - SHA-256 از شش مؤلفه</summary>
    public string? HardwareFingerprint { get; set; }

    /// <summary>امضای دیجیتال RSA-2048 با SHA256</summary>
    public byte[]? SignatureData { get; set; }

    /// <summary>آخرین اعتبارسنجی موفق</summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>فعال‌سازی آفلاین</summary>
    public bool IsOfflineActivation { get; set; } = false;
}
