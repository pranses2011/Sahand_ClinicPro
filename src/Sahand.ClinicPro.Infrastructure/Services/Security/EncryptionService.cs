using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Security;

/// <summary>
/// سرویس رمزنگاری AES-256-GCM — برای فیلدهای حساس بیمار و اعتبارسنجی سرویس‌های خارجی
/// کلید از تنظیمات Security:EncryptionKey خوانده می‌شود
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int KeySize = 32;          // ۲۵۶ بیت
    private const int NonceSize = 12;        // استاندارد GCM
    private const int TagSize = 16;          // برچسب احراز
    private readonly byte[] _key;

    /// <summary>سازنده با خواندن کلید از پیکربندی</summary>
    public EncryptionService(IConfiguration configuration)
    {
        var keyText = configuration["Security:EncryptionKey"]
            ?? "SahandClinicPro-Default-32Byte-Encryption-Key!"; // ۴۸ کاراکتر
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyText));
    }

    /// <summary>رمزنگاری متن ساده — خروجی Base64 با نانس و برچسب</summary>
    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = EncryptBytes(plainBytes);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>رمزگشایی متن Base64</summary>
    public string Decrypt(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = DecryptBytes(cipherBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>رمزنگاری بایت‌های خام با AES-GCM</summary>
    public byte[] EncryptBytes(byte[] plainBytes)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        // ساختار خروجی: نانس (۱۲) + برچسب (۱۶) + متن رمز
        var output = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, output, NonceSize + TagSize, cipher.Length);
        return output;
    }

    /// <summary>رمزگشایی بایت‌های خام AES-GCM</summary>
    public byte[] DecryptBytes(byte[] cipherBytes)
    {
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipher = new byte[cipherBytes.Length - NonceSize - TagSize];

        Buffer.BlockCopy(cipherBytes, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(cipherBytes, NonceSize + TagSize, cipher, 0, cipher.Length);

        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }
}
