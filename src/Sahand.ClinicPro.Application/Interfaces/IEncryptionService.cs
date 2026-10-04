namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس رمزنگاری AES-256 — برای فیلدهای حساس بیمار و اعتبارسنجی‌های سرویس‌های خارجی
/// </summary>
public interface IEncryptionService
{
    /// <summary>رمزنگاری متن ساده با AES-256-GCM و بازگشت Base64</summary>
    string Encrypt(string plainText);

    /// <summary>رمزگشایی متن Base64 رمزنگاری‌شده</summary>
    string Decrypt(string cipherText);

    /// <summary>رمزنگاری بایت‌های خام</summary>
    byte[] EncryptBytes(byte[] plainBytes);

    /// <summary>رمزگشایی بایت‌های خام</summary>
    byte[] DecryptBytes(byte[] cipherBytes);
}
