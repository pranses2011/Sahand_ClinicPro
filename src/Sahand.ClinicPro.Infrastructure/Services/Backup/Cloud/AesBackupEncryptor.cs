using System.Security.Cryptography;

namespace Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;

/// <summary>
/// رمزنگاری AES-256-GCM فایل‌های پشتیبان — چرخه سیزدهم (درخواست ۷-۳).
/// قالب خروجی: سرآیند «SCPENC1» (۷ بایت) + نانس ۱۲ بایتی + رمزنوشته + برچسب ۱۶ بایتی.
/// کلید ۳۲ بایتی از پیکربندی (Base64) خوانده می‌شود و هرگز در بانک ذخیره نمی‌شود.
/// </summary>
public static class AesBackupEncryptor
{
    /// <summary>شناسه قالب — کنترل نسخه آینده</summary>
    public const string Header = "SCPENC1";

    /// <summary>طول نانس استاندارد GCM</summary>
    public const int NonceLength = 12;

    /// <summary>طول برچسب احراز اصالت GCM</summary>
    public const int TagLength = 16;

    /// <summary>تولید کلید جدید ۳۲ بایتی — برای قرار دادن در پیکربندی</summary>
    /// <returns>کلید به‌صورت Base64</returns>
    public static string GenerateKey()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(key);
    }

    /// <summary>رمزنگاری بایت‌های پشتیبان با کلید ۳۲ بایتی</summary>
    /// <exception cref="ArgumentException">کلیر نامعتبر</exception>
    public static byte[] Encrypt(byte[] plain, byte[] key)
    {
        ValidateKey(key);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagLength];

        using var gcm = new AesGcm(key, TagLength);
        gcm.Encrypt(nonce, plain, cipher, tag);

        // سرآیند + نانس + رمزنوشته + برچسب
        var result = new byte[Header.Length + NonceLength + cipher.Length + TagLength];
        System.Text.Encoding.ASCII.GetBytes(Header).CopyTo(result, 0);
        nonce.CopyTo(result, Header.Length);
        cipher.CopyTo(result, Header.Length + NonceLength);
        tag.CopyTo(result, Header.Length + NonceLength + cipher.Length);
        return result;
    }

    /// <summary>رمزگشایی — هنگام بازیابی؛ اصالت داده با برچسب GCM احراز می‌شود</summary>
    /// <exception cref="CryptographicException">داده دست‌کاری‌شده یا کلید نادرست</exception>
    /// <exception cref="FormatException">قالب ناشناخته</exception>
    public static byte[] Decrypt(byte[] blob, byte[] key)
    {
        ValidateKey(key);
        if (blob.Length < Header.Length + NonceLength + TagLength)
            throw new FormatException("فایل رمزشده ناقص است");

        var header = System.Text.Encoding.ASCII.GetString(blob, 0, Header.Length);
        if (header != Header)
            throw new FormatException("قالب فایل رمزشده شناسایی نشد");

        var nonce = new ReadOnlySpan<byte>(blob, Header.Length, NonceLength);
        var cipherLength = blob.Length - Header.Length - NonceLength - TagLength;
        var cipher = new ReadOnlySpan<byte>(blob, Header.Length + NonceLength, cipherLength);
        var tag = new ReadOnlySpan<byte>(blob, Header.Length + NonceLength + cipherLength, TagLength);

        var plain = new byte[cipherLength];
        using var gcm = new AesGcm(key, TagLength);
        gcm.Decrypt(nonce, cipher, tag, plain, null);
        return plain;
    }

    /// <summary>آیا بایت‌ها قالب رمزشده این سرویس را دارند؟</summary>
    public static bool IsEncrypted(byte[] blob)
        => blob.Length > Header.Length &&
           System.Text.Encoding.ASCII.GetString(blob, 0, Header.Length) == Header;

    /// <summary>اعتبارسنجی طول کلید</summary>
    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != 32)
            throw new ArgumentException("کلید رمزنگاری باید دقیقاً ۳۲ بایت (۲۵۶ بیت) باشد — با GenerateKey بسازید");
    }
}
