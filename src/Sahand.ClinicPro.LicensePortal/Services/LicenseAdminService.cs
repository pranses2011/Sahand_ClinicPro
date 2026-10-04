using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace Sahand.ClinicPro.LicensePortal.Services;

/// <summary>ردیف لایسنس در فهرست پنل</summary>
public record LicenseRowDto(
    long Id,
    string LicenseKey,
    string TypeText,
    int MaxUsers,
    int MaxDoctors,
    int MaxPatients,
    string ExpiryText,
    string StatusText,
    string StatusColor,
    bool IsActivated,
    bool IsRevoked);

/// <summary>آمار داشبورد پنل</summary>
public record PortalStatsDto(
    int Total,
    int Active,
    int Expired,
    int PendingActivation,
    int Revoked,
    int Subscription,
    int Perpetual);

/// <summary>درخواست صدور لایسنس — قابل تغییر برای اتصال دوطرفه فرم</summary>
public record IssueLicenseRequest
{
    /// <summary>نوع — ۱ اشتراکی، ۲ دائمی</summary>
    public int LicenseType { get; set; } = 1;

    /// <summary>مدت اشتراک به ماه</summary>
    public int DurationMonths { get; set; } = 12;

    /// <summary>حداکثر کاربر</summary>
    public int MaxUsers { get; set; } = 5;

    /// <summary>حداکثر پزشک</summary>
    public int MaxDoctors { get; set; } = 10;

    /// <summary>حداکثر بیمار</summary>
    public int MaxPatients { get; set; } = 5000;

    /// <summary>ماژول‌های فعال</summary>
    public List<string> EnabledModules { get; set; } = new();
}

/// <summary>لاگ اعتبارسنجی برای نمایش</summary>
public record CheckLogRow(DateTime? CheckDate, string ResultText, bool IsOnline, string? Message);

/// <summary>
/// سرویس مدیریت لایسنس سمت پنل — صدور و تمدید و ابطال
/// تصمیم معماری: پنل به‌عنوان سامانه صادرکننده مستقیماً از DbContext استفاده می‌کند؛
/// در استقرار عملیاتی رشته اتصال به دیتابیس مجزای لایسنس اشاره می‌کند.
/// </summary>
public class LicenseAdminService
{
    private readonly ClinicProDbContext _db;
    private readonly IConfiguration _configuration;

    /// <summary>سازنده با تزریق زمینه داده و پیکربندی</summary>
    public LicenseAdminService(ClinicProDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    /// <summary>کاتالوگ ماژول‌های قابل انتخاب هنگام صدور</summary>
    public static readonly string[] ModuleCatalog =
    {
        "Dashboard", "Patients", "Appointments", "Receptions", "Visits", "Prescriptions",
        "Billing", "Lab", "Pharmacy", "Imaging", "Dental", "Eye", "Physiotherapy",
        "Accounting", "HR", "Reports", "Sms", "Insurance", "Government", "Settings",
        "Backup", "Audit", "Users"
    };

    /// <summary>فهرست لایسنس‌ها با فیلتر وضعیت</summary>
    public async Task<List<LicenseRowDto>> ListAsync(string? filter = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.ClinicLicense.AsNoTracking().Where(l => !l.IsDeleted);

        if (filter == "active")
            query = query.Where(l => (l.ExpiryDate == null || l.ExpiryDate >= today)
                && l.HardwareFingerprint != null);
        else if (filter == "expired")
            query = query.Where(l => l.ExpiryDate != null && l.ExpiryDate < today);
        else if (filter == "pending")
            query = query.Where(l => l.HardwareFingerprint == null);

        var list = await query.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();
        return list.Select(ToRow).ToList();
    }

    /// <summary>نگاشت موجودیت به ردیف نمایش</summary>
    private static LicenseRowDto ToRow(ClinicLicense l)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = l.ExpiryDate.HasValue && l.ExpiryDate < today;
        var activated = !string.IsNullOrEmpty(l.HardwareFingerprint);

        var (status, color) = expired
            ? ("منقضی‌شده", "#e53935")
            : !activated
                ? ("در انتظار فعال‌سازی", "#fb8c00")
                : ("فعال", "#43a047");

        return new LicenseRowDto(
            l.Id, l.LicenseKey,
            l.LicenseType == LicenseType.Subscription ? "اشتراکی" : "دائمی",
            l.MaxUsers, l.MaxDoctors, l.MaxPatients,
            l.ExpiryDate?.ToString("yyyy/MM/dd") ?? "بدون انقضا",
            status, color, activated, false);
    }

    /// <summary>آمار داشبورد</summary>
    public async Task<PortalStatsDto> GetStatsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _db.ClinicLicense.AsNoTracking().Where(l => !l.IsDeleted).ToListAsync();

        return new PortalStatsDto(
            all.Count,
            all.Count(l => (l.ExpiryDate == null || l.ExpiryDate >= today) && l.HardwareFingerprint != null),
            all.Count(l => l.ExpiryDate != null && l.ExpiryDate < today),
            all.Count(l => l.HardwareFingerprint == null),
            await _db.ClinicLicense.AsNoTracking().CountAsync(l => l.IsDeleted),
            all.Count(l => l.LicenseType == LicenseType.Subscription),
            all.Count(l => l.LicenseType == LicenseType.Perpetual));
    }

    /// <summary>صدور لایسنس جدید — تولید کلید + امضای اختیاری RSA</summary>
    public async Task<LicenseRowDto> IssueAsync(IssueLicenseRequest request)
    {
        var license = new ClinicLicense
        {
            LicenseKey = GenerateKey(),
            LicenseType = request.LicenseType == 2 ? LicenseType.Perpetual : LicenseType.Subscription,
            MaxUsers = Math.Max(1, request.MaxUsers),
            MaxDoctors = Math.Max(1, request.MaxDoctors),
            MaxPatients = Math.Max(1, request.MaxPatients),
            MaxBranches = 1,
            EnabledModules = System.Text.Json.JsonSerializer.Serialize(request.EnabledModules),
            IssuedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate = request.LicenseType == 2
                ? null
                : DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(Math.Max(1, request.DurationMonths)))
        };

        SignLicense(license);

        _db.ClinicLicense.Add(license);
        await _db.SaveChangesAsync();
        return ToRow(license);
    }

    /// <summary>تمدید لایسنس اشتراکی</summary>
    public async Task<bool> RenewAsync(long id, int months)
    {
        var license = await _db.ClinicLicense.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (license is null) return false;

        var baseDate = license.ExpiryDate is { } expiry && expiry > DateOnly.FromDateTime(DateTime.UtcNow)
            ? expiry
            : DateOnly.FromDateTime(DateTime.UtcNow);
        license.ExpiryDate = baseDate.AddMonths(Math.Max(1, months));
        license.LicenseType = LicenseType.Subscription;

        SignLicense(license);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>ابطال لایسنس — حذف نرم</summary>
    public async Task<bool> RevokeAsync(long id)
    {
        var license = await _db.ClinicLicense.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (license is null) return false;

        license.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>آخرین لاگ‌های اعتبارسنجی</summary>
    public async Task<List<CheckLogRow>> RecentChecksAsync(int take = 30)
    {
        var logs = await _db.LicenseCheckLog.AsNoTracking()
            .Where(l => !l.IsDeleted)
            .OrderByDescending(l => l.CheckDate)
            .Take(take)
            .ToListAsync();

        return logs.Select(l => new CheckLogRow(
            l.CheckDate,
            l.Result switch
            {
                LicenseValidationResult.Valid => "معتبر",
                LicenseValidationResult.Expired => "منقضی",
                LicenseValidationResult.InvalidSignature => "امضای نامعتبر",
                _ => l.Result.ToString()
            },
            l.IsOnlineCheck, l.Message)).ToList();
    }

    /// <summary>تولید کلید لایسنس با قالب SCPRO-XXXXX-XXXXX-XXXXX</summary>
    private static string GenerateKey()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(15);
        var chars = bytes.Select(b => alphabet[b % alphabet.Length]).ToArray();
        return $"SCPRO-{new string(chars, 0, 5)}-{new string(chars, 5, 5)}-{new string(chars, 10, 5)}";
    }

    /// <summary>امضای RSA-2048 — فقط در صورت پیکربندی کلید خصوصی پنل</summary>
    /// <remarks>payload یکسان با سمت اعتبارسنجی: Key|Expiry|MaxUsers</remarks>
    private void SignLicense(ClinicLicense license)
    {
        var privateKeyXml = _configuration["License:RsaPrivateKey"];
        if (string.IsNullOrEmpty(privateKeyXml)) return;

        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            var payload = System.Text.Encoding.UTF8.GetBytes(
                $"{license.LicenseKey}|{license.ExpiryDate:yyyy-MM-dd}|{license.MaxUsers}");
            license.SignatureData = rsa.SignData(payload,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            // کلید نامعتبر — بدون امضا صادر می‌شود و در پنل دیده می‌شود
        }
    }
}