using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Services.Backup.Cloud;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های پشتیبان ابری — چرخه سیزدهم (درخواست ۷-۳):
/// رمزنگاری AES-256-GCM رفت‌وبرگشت + کشف دستکاری، ذخیره‌ساز پوشه‌ای،
/// ارکستراتور بارگذاری/تست بازیابی و امضاساز AWS V4
/// </summary>
public class CloudBackupTests
{
    /// <summary>ساخت پیکربندی درون‌حافظه‌ای</summary>
    private static IConfiguration MakeConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Aes_RoundTrip_PreservesContent()
    {
        var key = Convert.FromBase64String(AesBackupEncryptor.GenerateKey());
        var plain = Encoding.UTF8.GetBytes("MITSL backup content — سهند کلینیک‌پرو ۱۲۳");

        var cipher = AesBackupEncryptor.Encrypt(plain, key);

        AesBackupEncryptor.IsEncrypted(cipher).Should().BeTrue("قالب سرآیند SCPENC1 دارد");
        cipher.Should().NotBeEquivalentTo(plain, "خروجی رمزشده است");

        var decrypted = AesBackupEncryptor.Decrypt(cipher, key);
        decrypted.Should().BeEquivalentTo(plain, "رمزگشایی محتوا را عیناً برمی‌گرداند");
    }

    [Fact]
    public void Aes_WrongKey_Throws()
    {
        var key1 = Convert.FromBase64String(AesBackupEncryptor.GenerateKey());
        var key2 = Convert.FromBase64String(AesBackupEncryptor.GenerateKey());
        var cipher = AesBackupEncryptor.Encrypt("data"u8.ToArray(), key1);

        var act = () => AesBackupEncryptor.Decrypt(cipher, key2);
        act.Should().Throw<CryptographicException>("برچسب GCM با کلید نادرست احراز نمی‌شود");
    }

    [Fact]
    public void Aes_TamperedBlob_Throws()
    {
        var key = Convert.FromBase64String(AesBackupEncryptor.GenerateKey());
        var cipher = AesBackupEncryptor.Encrypt(new byte[] { 1, 2, 3, 4, 5 }, key);
        // دستکاری یک بیت از رمزنوشته
        cipher[^5] ^= 0xFF;

        var act = () => AesBackupEncryptor.Decrypt(cipher, key);
        act.Should().Throw<CryptographicException>("اصالت داده با برچسب GCM تأیید می‌شود");
    }

    [Fact]
    public void Aes_InvalidKeyLength_Throws()
    {
        var act = () => AesBackupEncryptor.Encrypt("data"u8.ToArray(), new byte[16]);
        act.Should().Throw<ArgumentException>("کلید باید ۳۲ بایت باشد");
    }

    [Fact]
    public async Task LocalFolder_UploadListDownload_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"cloudtest-{Guid.NewGuid():N}");
        var storage = new LocalFolderCloudStorage(dir);

        var content = Encoding.UTF8.GetBytes("backup-bytes-پشتیبان");
        using (var ms = new MemoryStream(content))
        {
            var uploaded = await storage.UploadAsync("test_20260101.bak", ms);
            uploaded.Size.Should().Be(content.Length, "حجم فایل بارگذاری‌شده درست است");
        }

        var list = await storage.ListAsync();
        list.Should().ContainSingle().Which.FileName.Should().Be("test_20260101.bak");

        await using var download = await storage.DownloadAsync("test_20260101.bak");
        using var reader = new MemoryStream();
        await download.CopyToAsync(reader);
        reader.ToArray().Should().BeEquivalentTo(content, "محتوا بدون تغییر برمی‌گردد");

        var (ok, message) = await storage.TestConnectionAsync();
        ok.Should().BeTrue(message);
    }

    [Fact]
    public async Task CloudService_UploadWithEncryption_VerifyRestore()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"cloudsvc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var key = AesBackupEncryptor.GenerateKey();
        var config = MakeConfig(new Dictionary<string, string?>
        {
            ["Backup:Cloud:Provider"] = "local",
            ["Backup:Cloud:LocalFolder"] = dir,
            ["Backup:Cloud:EncryptionKey"] = key,
        });
        var service = new CloudBackupService(config, NullLogger<CloudBackupService>.Instance);

        service.IsEnabled.Should().BeTrue("ارائه‌دهنده محلی تنظیم شده است");
        service.IsEncryptionEnabled.Should().BeTrue("کلید رمزنگاری تنظیم شده است");

        // فایل پشتیبان ساختگی روی دیسک
        var backupPath = Path.Combine(dir, "SahandClinicPro_20260101_000000.bak");
        var backupBytes = Encoding.UTF8.GetBytes("FAKE-BACKUP-DATA-سهند");
        await File.WriteAllBytesAsync(backupPath, backupBytes);

        var record = new BackupHistory
        {
            BackupType = BackupType.Full,
            FilePath = backupPath,
            Status = BackupStatus.Success,
            StorageType = StorageType.Local,
        };

        var uploaded = await service.UploadBackupAsync(record);

        record.StorageType.Should().Be(StorageType.Cloud, "رکورد ابری علامت می‌خورد");
        record.IsCloudEncrypted.Should().BeTrue("کلید تنظیم شده است");
        record.CloudFileName.Should().NotBeNull().And.EndWith(".enc");
        uploaded.FileName.Should().EndWith(".enc", "پسوند رمزشده");

        // فایل روی «ابر» واقعاً رمزشده است
        var stored = await File.ReadAllBytesAsync(Path.Combine(dir, Path.GetFileName(record.CloudFileName!)));
        AesBackupEncryptor.IsEncrypted(stored).Should().BeTrue("فایل ذخیره‌شده رمزشده است");

        // تست بازیابی — دانلود + رمزگشایی موفق
        var (ok, message, plainSize) = await service.VerifyRestoreAsync(record.CloudFileName!);
        ok.Should().BeTrue(message);
        plainSize.Should().Be(backupBytes.Length, "حجم رمزگشایی‌شده با نسخه اصلی یکی است");
    }

    [Fact]
    public async Task CloudService_DisabledProvider_Throws()
    {
        var config = MakeConfig(new Dictionary<string, string?>());
        var service = new CloudBackupService(config, NullLogger<CloudBackupService>.Instance);

        service.IsEnabled.Should().BeFalse("هیچ ارائه‌دهنده‌ای تنظیم نشده است");
        var act = () => service.UploadBackupAsync(new BackupHistory { FilePath = "x" });
        await act.Should().ThrowAsync<InvalidOperationException>("سرویس غیرفعال است");
    }

    [Fact]
    public void AwsV4Signer_ProducesStableSignature()
    {
        var uri = new Uri("https://s3.example.com/my-bucket/backups/file.bak");
        var auth1 = AwsV4Signer.Sign(HttpMethod.Put, uri, null,
            AwsV4Signer.Sha256Hex("payload"u8.ToArray()),
            "AKIDEXAMPLE", "secret", "us-east-1",
            new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc));
        var auth2 = AwsV4Signer.Sign(HttpMethod.Put, uri, null,
            AwsV4Signer.Sha256Hex("payload"u8.ToArray()),
            "AKIDEXAMPLE", "secret", "us-east-1",
            new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc));

        auth1.Should().StartWith("AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/");
        auth1.Should().Contain("SignedHeaders=host;x-amz-content-sha256;x-amz-date");
        auth1.Should().Be(auth2, "ورودی یکسان امضای پایدار می‌دهد");
        // امضا هگز ۶۴ نویسه است
        var signature = auth1.Split("Signature=")[1];
        signature.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");
    }
}
