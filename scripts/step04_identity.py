# -*- coding: utf-8 -*-
"""گام ۴ — Infrastructure.Identity: JWT + BCrypt + TokenService + AuthService + RBAC"""
import os, json

ROOT = os.path.join(os.path.dirname(__file__), "..")
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ Application: مدل‌ها و اینترفیس‌ها ═══════════
write("Common/Models/AuthResult.cs", """
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// نتیجه احراز هویت — توکن‌های دسترسی و تازه‌سازی همراه اطلاعات کاربر
/// </summary>
public sealed class AuthResult
{
    /// <summary>آیا احراز هویت موفق بود</summary>
    public bool Succeeded { get; init; }

    /// <summary>پیام خطا در صورت شکست</summary>
    public string? Error { get; init; }

    /// <summary>توکن دسترسی JWT</summary>
    public string? AccessToken { get; init; }

    /// <summary>توکن تازه‌سازی</summary>
    public string? RefreshToken { get; init; }

    /// <summary>زمان انقضای توکن دسترسی به ثانیه</summary>
    public int ExpiresIn { get; init; }

    /// <summary>نام کاربری</summary>
    public string? Username { get; init; }

    /// <summary>نام نمایشی کاربر</summary>
    public string? DisplayName { get; init; }

    /// <summary>نقش‌های کاربر</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>دسترسی‌های کاربر</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    /// <summary>آیا تغییر رمز عبور الزامی است</summary>
    public bool MustChangePassword { get; init; }

    /// <summary>ساخت نتیجه موفق</summary>
    public static AuthResult Success(string accessToken, string refreshToken, int expiresIn,
        string username, string displayName, IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions, bool mustChangePassword) => new()
    {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = expiresIn,
        Username = username,
        DisplayName = displayName,
        Roles = roles,
        Permissions = permissions,
        MustChangePassword = mustChangePassword
    };

    /// <summary>ساخت نتیجه ناموفق</summary>
    public static AuthResult Failure(string error) => new() { Succeeded = false, Error = error };
}
""", APP)

write("Common/Models/RefreshResult.cs", """
namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// نتیجه تمدید توکن با توکن تازه‌سازی
/// </summary>
public sealed record RefreshResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? Error)
{
    /// <summary>ساخت نتیجه موفق</summary>
    public static RefreshResult Success(string accessToken, string refreshToken, int expiresIn) =>
        new(true, accessToken, refreshToken, expiresIn, null);

    /// <summary>ساخت نتیجه ناموفق</summary>
    public static RefreshResult Failure(string error) =>
        new(false, null, null, 0, error);
}
""", APP)

write("Interfaces/IPasswordHasher.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس هش رمز عبور با BCrypt — ضریب سختی ۱۲
/// </summary>
public interface IPasswordHasher
{
    /// <summary>هش کردن رمز عبور متن ساده</summary>
    string Hash(string password);

    /// <summary>بررسی تطابق رمز ورودی با هش ذخیره‌شده</summary>
    bool Verify(string password, string hash);

    /// <summary>آیا رمز عبور سیاست امنیتی را برآورده می‌کند — حداقل ۸ کاراکتر با حرف و رقم</summary>
    bool MeetsPolicy(string password, out string errorMessage);
}
""", APP)

write("Interfaces/IIdentityService.cs", """
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس احراز هویت — ورود، تمدید توکن، تغییر رمز و کنترل دسترسی
/// </summary>
public interface IIdentityService
{
    /// <summary>احراز هویت با نام کاربری و رمز عبور — قفل ۳۰ دقیقه‌ای بعد از ۵ تلاش ناموفق</summary>
    Task<AuthResult> AuthenticateAsync(string usernameOrEmail, string password,
        string? ipAddress = null, string? deviceInfo = null, CancellationToken cancellationToken = default);

    /// <summary>تمدید توکن دسترسی با توکن تامه‌سازی معتبر — چرخش توکن قدیمی</summary>
    Task<RefreshResult> RefreshTokenAsync(string refreshToken,
        string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>خروج و ابطال توکن تازه‌سازی</summary>
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>تغییر رمز عبور با بررسی رمز فعلی و تاریخچه پنج رمز اخیر</summary>
    Task<Result> ChangePasswordAsync(long userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>گرفتن دسترسی‌های کاربر — از نقش‌های او</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>گرفتن نقش‌های کاربر</summary>
    Task<IReadOnlyList<string>> GetRolesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>آیا کاربر دسترسی مشخصی دارد</summary>
    Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken = default);
}
""", APP)

# ═══════════ Infrastructure: پیاده‌سازی‌ها ═══════════
write("Identity/PasswordHasher.cs", """
using System.Text.RegularExpressions;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// هش رمز عبور با BCrypt-Net-Next — ضریب سختی ۱۲
/// </summary>
public partial class PasswordHasher : IPasswordHasher
{
    /// <summary>ضریب سختی BCrypt — تعادل امنیت و سرعت</summary>
    public const int WorkFactor = 12;

    [GeneratedRegex(@"^(?=.*[a-zA-Z])(?=.*\\d).{8,}$")]
    private static partial Regex PolicyRegex();

    /// <summary>هش کردن رمز عبور متن ساده</summary>
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    /// <summary>بررسی تطابق رمز ورودی با هش ذخیره‌شده</summary>
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    /// <summary>سیاست رمز: حداقل ۸ کاراکتر شامل حرف و رقم</summary>
    public bool MeetsPolicy(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد";
            return false;
        }
        if (!PolicyRegex().IsMatch(password))
        {
            errorMessage = "رمز عبور باید شامل حرف و رقم باشد";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }
}
""", INFRA)

write("Identity/JwtTokenGenerator.cs", """
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// تنظیمات JWT — از بخش Jwt در appsettings خوانده می‌شود
/// </summary>
public class JwtOptions
{
    /// <summary>نام بخش پیکربندی در appsettings</summary>
    public const string SectionName = "Jwt";

    /// <summary>کلید امضا — در تولید باید از محیط یا KeyVault تزریق شود</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>صادرکننده توکن</summary>
    public string Issuer { get; set; } = "Sahand.ClinicPro";

    /// <summary>مخاطب توکن</summary>
    public string Audience { get; set; } = "Sahand.ClinicPro.Clients";

    /// <summary>عمر توکن دسترسی به دقیقه — پیش‌فرض ۳۰</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 30;

    /// <summary>عمر توکن تازه‌سازی به روز — پیش‌فرض ۷</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

/// <summary>
/// مولد توکن JWT با امضای HMAC-SHA256 و توکن تازه‌سازی امن تصادفی
/// </summary>
public class JwtTokenGenerator
{
    private readonly JwtOptions _options;

    /// <summary>سازنده با تنظیمات JWT</summary>
    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>عمر توکن دسترسی به ثانیه</summary>
    public int ExpirySeconds => _options.AccessTokenExpiryMinutes * 60;

    /// <summary>تولید توکن دسترسی با ادعاهای کاربر و نقش‌ها و دسترسی‌ها</summary>
    public (string Token, DateTime ExpiresAt) GenerateAccessToken(
        long userId, string username, string displayName,
        IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("displayName", displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>تولید توکن تازه‌سازی امن با ۲۵۶ بیت تصادفی</summary>
    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return (Convert.ToBase64String(randomBytes),
            DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays));
    }

    /// <summary>پارامترهای اعتبارسنجی برای میانجیر JWT</summary>
    public TokenValidationParameters GetValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = _options.Issuer,
        ValidAudience = _options.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
}
""", INFRA)

write("Identity/IdentityService.cs", """
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// سرویس احراز هویت — ورود با قفل حساب، چرخش توکن تازه‌سازی و کنترل دسترسی RBAC
/// </summary>
public class IdentityService : IIdentityService
{
    /// <summary>حداکثر تلاش ناموفق قبل از قفل</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>مدت قفل حساب به دقیقه</summary>
    public const int LockoutMinutes = 30;

    /// <summary>تعداد رمزهای اخیر که نباید تکرار شوند</summary>
    public const int PasswordHistoryDepth = 5;

    private readonly ClinicProDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<IdentityService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public IdentityService(
        ClinicProDbContext context,
        JwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        ILogger<IdentityService> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>احراز هویت با نام کاربری یا ایمیل و رمز عبور</summary>
    public async Task<AuthResult> AuthenticateAsync(string usernameOrEmail, string password,
        string? ipAddress = null, string? deviceInfo = null, CancellationToken cancellationToken = default)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        var user = await _context.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized,
                cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("تلاش ورود با نام کاربری ناشناخته {Username} از {Ip}",
                usernameOrEmail, ipAddress ?? "نامشخص");
            return AuthResult.Failure("نام کاربری یا رمز عبور نادرست است");
        }

        if (!user.IsActive)
            return AuthResult.Failure("حساب کاربری غیرفعال است — با مدیر سامانه تماس بگیرید");

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remaining = (user.LockoutEnd.Value - DateTime.UtcNow).Minutes;
            return AuthResult.Failure($"حساب به دلیل تلاش‌های ناموفق قفل شده است — {remaining + 1} دقیقه دیگر تلاش کنید");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;
                _logger.LogWarning("حساب کاربر {Username} به دلیل پنج تلاش ناموفق قفل شد", user.Username);
            }
            await _context.SaveChangesAsync(cancellationToken);
            return AuthResult.Failure("نام کاربری یا رمز عبور نادرست است");
        }

        // ورود موفق — بازنشانی شمارنده‌ها
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;

        var roles = user.Roles.Select(r => r.Role!.Name).ToList();
        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        var displayName = user.Username; // در گام‌های بعد از پروفایل پرسنل تکمیل می‌شود

        var (accessToken, _) = _tokenGenerator.GenerateAccessToken(user.Id, user.Username, displayName, roles, permissions);
        var (refreshToken, refreshExpiry) = _tokenGenerator.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiry,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IsRevoked = false
        });

        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            SessionToken = accessToken[..64],
            LoginAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = deviceInfo,
            IsActive = true
        });

        await _context.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(accessToken, refreshToken,
            _tokenGenerator.ExpirySeconds, user.Username, displayName, roles, permissions, user.MustChangePassword);
    }

    /// <summary>تمدید توکن دسترسی با چرخش توکن تازه‌سازی</summary>
    public async Task<RefreshResult> RefreshTokenAsync(string refreshToken,
        string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var stored = await _context.Users
            .SelectMany(u => u.RefreshTokens)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (stored is null || stored.IsRevoked)
            return RefreshResult.Failure("توکن تازه‌سازی نامعتبر است");

        if (stored.ExpiresAt <= DateTime.UtcNow)
            return RefreshResult.Failure("توکن تازه‌سازی منقضی شده است — دوباره وارد شوید");

        var user = await _context.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return RefreshResult.Failure("حساب کاربری در دسترس نیست");

        // چرخش توکن — ابطال قدیمی و صدور جفت جدید
        stored.IsRevoked = true;

        var roles = user.Roles.Select(r => r.Role!.Name).ToList();
        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        var (accessToken, _) = _tokenGenerator.GenerateAccessToken(user.Id, user.Username, user.Username, roles, permissions);
        var (newRefresh, expiry) = _tokenGenerator.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefresh,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiry,
            IpAddress = ipAddress
        });

        await _context.SaveChangesAsync(cancellationToken);
        return RefreshResult.Success(accessToken, newRefresh, _tokenGenerator.ExpirySeconds);
    }

    /// <summary>ابطال توکن تامه‌سازی هنگام خروج</summary>
    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await _context.Users
            .SelectMany(u => u.RefreshTokens)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>تغییر رمز عبور با سیاست امنیتی و تاریخچه</summary>
    public async Task<Result> ChangePasswordAsync(long userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure(new Error("USER_NOT_FOUND", "کاربر یافت نشد"));

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
            return Result.Failure(new Error("WRONG_PASSWORD", "رمز عبور فعلی نادرست است"));

        if (!_passwordHasher.MeetsPolicy(newPassword, out var policyError))
            return Result.Failure(new Error("WEAK_PASSWORD", policyError));

        // جلوگیری از تکرار پنج رمز اخیر
        var history = await _context.Set<PasswordHistory>()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.ChangedAt)
            .Take(PasswordHistoryDepth)
            .ToListAsync(cancellationToken);

        if (history.Any(h => _passwordHasher.Verify(newPassword, h.PasswordHash)))
            return Result.Failure(new Error("REPEATED_PASSWORD", "استفاده از رمزهای اخیر مجاز نیست"));

        // ثبت رمز قبلی در تاریخچه و اعمال رمز جدید
        _context.Set<PasswordHistory>().Add(new PasswordHistory
        {
            UserId = userId,
            PasswordHash = user.PasswordHash,
            ChangedAt = DateTime.UtcNow
        });

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
        user.SecurityStamp = Guid.NewGuid().ToString("N");

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>دسترسی‌های کاربر از نقش‌ها</summary>
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .SelectMany(ur => ur.Role!.Permissions)
            .Select(rp => rp.Permission!.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>نقش‌های کاربر</summary>
    public async Task<IReadOnlyList<string>> GetRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>بررسی دسترسی اتمی</summary>
    public async Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .SelectMany(ur => ur.Role!.Permissions)
            .AnyAsync(rp => rp.Permission!.Name == permission, cancellationToken);
    }
}
""", INFRA)

# ═══════════ DI ═══════════
write("Identity/IdentitySetup.cs", """
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// راه‌اندازی احراز هویت JWT Bearer و سرویس‌های امنیتی
/// </summary>
public static class IdentitySetup
{
    /// <summary>
    /// ثبت JWT Bearer و سرویس‌های امنیتی — باید پس از AddInfrastructure فراخوانی شود
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // تنظیمات JWT از بخش Jwt
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // احراز هویت JWT Bearer — با کلید از تنظیمات
        var signingKey = System.Text.Encoding.UTF8.GetBytes(
            string.IsNullOrEmpty(jwt.SecretKey)
                ? "SahandClinicPro-Development-Fallback-Secret-Key-Must-Change!2026"
                : jwt.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ClockSkew = System.TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };

            // پاسخ فارسی برای توکن منقضی
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            isSuccess = false,
                            message = "توکن دسترسی منقضی شده است — دوباره وارد شوید"
                        }));
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
""", INFRA)

print("✅ گام ۴ — Identity کامل شد")

# ═══════════ appsettings ═══════════
settings_path = os.path.join(API, "appsettings.json")
settings = json.loads(open(settings_path, encoding="utf-8-sig").read())
settings["Jwt"] = {
    "SecretKey": "SahandClinicPro-Development-Secret-Key-Change-In-Production-2026!@#x9KmQ",
    "Issuer": "Sahand.ClinicPro",
    "Audience": "Sahand.ClinicPro.Clients",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 7
}
settings["Security"] = {
    "MaxFailedLoginAttempts": 5,
    "LockoutMinutes": 30,
    "PasswordHistoryDepth": 5
}
with open(settings_path, "w", encoding="utf-8") as f:
    json.dump(settings, f, ensure_ascii=False, indent=2)
print("  + appsettings.json Jwt section")

print("✅ گام ۴ — Identity کامل شد")
