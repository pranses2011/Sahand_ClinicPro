# -*- coding: utf-8 -*-
"""گام ۵ — بخش ۲: پیاده‌سازی اینترفیس‌های Application در Infrastructure"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))


def write(rel, content):
    full = os.path.join(INFRA, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


write("Persistence/TransactionManager.cs", """
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مدیر تراکنش — پیچیدن DbContext.Database در تراکنش برای TransactionBehavior
/// </summary>
public class TransactionManager : ITransactionManager
{
    private readonly ClinicProDbContext _context;
    private readonly ILogger<TransactionManager> _logger;
    private IDbContextTransaction? _transaction;

    /// <summary>سازنده با تزریق کانتکست و لاگر</summary>
    public TransactionManager(ClinicProDbContext context, ILogger<TransactionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>آغاز تراکنش جدید اگر قبلاً باز نشده باشد</summary>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            _logger.LogDebug("تراکنش قبلاً باز است — از بازکردن مجدد خودداری شد");
            return;
        }
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>تثبیت تراکنش باز</summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>به عقب برگرداندن تراکنش باز</summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
""")

write("Services/DateTimeService.cs", """
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services;

/// <summary>
/// سرویس زمان — زمان فعلی جهانی و به‌وقت ایران
/// </summary>
public class DateTimeService : IDateTimeService
{
    /// <summary>منطقه زمانی رسمی ایران</summary>
    private static readonly TimeZoneInfo IranZone =
        TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

    /// <summary>زمان فعلی به وقت جهانی</summary>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>زمان فعلی به وقت ایران</summary>
    public DateTime NowIran => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranZone);

    /// <summary>امروز به وقت ایران</summary>
    public DateOnly TodayIran => DateOnly.FromDateTime(NowIran);
}
""")

write("Services/Security/EncryptionService.cs", """
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
""")

# ═══ ثبت در DI ═══
p = os.path.join(INFRA, "DependencyInjection.cs")
s = open(p, encoding="utf-8-sig").read()
s = s.replace("""        // بذرهای داده اولیه""",
"""        // سرویس‌های انتزاعی Application پیاده‌سازی‌شده در زیرساخت
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddSingleton<IDateTimeService, Services.DateTimeService>();
        services.AddSingleton<IEncryptionService, Services.Security.EncryptionService>();

        // بذرهای داده اولیه""")
if "using Sahand.ClinicPro.Infrastructure.Services;" not in s:
    s = s.replace("using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;",
                  "using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;\nusing Sahand.ClinicPro.Infrastructure.Services;")
open(p, "w", encoding="utf-8-sig").write(s)
print("  + DependencyInjection.cs (services registered)")

print("✅ گام ۵ — بخش ۲ کامل شد")
