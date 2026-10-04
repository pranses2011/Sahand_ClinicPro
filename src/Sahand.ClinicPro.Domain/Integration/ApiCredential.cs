namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// اعتبارسنجی سرویس خارجی - رمزنگاری‌شده با AES-256
/// </summary>
public class ApiCredential : AuditableEntity, ISoftDeletable
{
    /// <summary>نام سرویس - یکتا</summary>
    public string ServiceName { get; set; } = null!;

    /// <summary>نام کاربری</summary>
    public string? Username { get; set; }

    /// <summary>رمز عبور رمزنگاری‌شده</summary>
    public byte[]? PasswordEncrypted { get; set; }

    /// <summary>کلید API رمزنگاری‌شده</summary>
    public byte[]? ApiKeyEncrypted { get; set; }

    /// <summary>توکن دسترسی</summary>
    public string? Token { get; set; }

    /// <summary>انقضای توکن</summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>نشانی پایه سرویس</summary>
    public string? Endpoint { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;
}
