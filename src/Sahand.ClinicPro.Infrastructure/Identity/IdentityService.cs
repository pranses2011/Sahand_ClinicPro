using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.Common.Security;
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
    private readonly IPasswordPolicyProvider _passwordPolicyProvider;
    private readonly ILogger<IdentityService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public IdentityService(
        ClinicProDbContext context,
        JwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IPasswordPolicyProvider passwordPolicyProvider,
        ILogger<IdentityService> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _passwordPolicyProvider = passwordPolicyProvider;
        _logger = logger;
    }

    /// <summary>احراز هویت با نام کاربری یا ایمیل و رمز عبور</summary>
    public async Task<AuthResult> AuthenticateAsync(string usernameOrEmail, string password,
        string? ipAddress = null, string? deviceInfo = null, CancellationToken cancellationToken = default)
    {
        var normalized = usernameOrEmail.Trim().ToUpperInvariant();

        // ═══ چرخهٔ ۷۲ (گزارش مالک: «در لاگ عملیات به جای نام کامل، نام کاربری رو
        //      مینویسه») ═══ کاربر باید با «نام کامل پرسنل» شناسایی شود، نه کد
        //      ورود. پیش‌تر ادعای displayName توکن به‌سادگی Username بود و
        //      میدلور لاگ عملیات همان را در ستون کاربر می‌نوشت. اکنون پروفایل
        //      پرسنلِ متصل (Employee.FirstName + LastName) با Include بارگذاری
        //      و نام کامل در توکن نشسته می‌شود؛ بدون پرسنلِ متصل، نام کاربری
        //      پابرجا می‌ماند (کاربر سیستمی مثل admin بدون پرسنل).
        var user = await _context.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .Include(u => u.Employee)
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

        // نام کامل پرسنل متصل — چرخهٔ ۷۲: ریشهٔ «نام کاربری به‌جای نام کامل» در لاگ
        // عملیات همین‌جاست؛ ادعای displayName توکن از این پس نام واقعی پرسنل است
        // و میدلور لاگ عملیات آن را در ستون «کاربر» ثبت می‌کند.
        var displayName = ResolveFullName(user);

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
            .Include(u => u.Employee) // چرخهٔ ۷۲ — نام کامل برای ادعای displayName توکن تازه
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return RefreshResult.Failure("حساب کاربری در دسترس نیست");

        // چرخش توکن — ابطال قدیمی و صدور جفت جدید
        stored.IsRevoked = true;

        var roles = user.Roles.Select(r => r.Role!.Name).ToList();
        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        // چرخهٔ ۷۲ — توکن تازه‌شده هم نام کامل پرسنل را حمل می‌کند (نه فقط ورود اول)
        var (accessToken, _) = _tokenGenerator.GenerateAccessToken(user.Id, user.Username, ResolveFullName(user), roles, permissions);
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

        var policy = await _passwordPolicyProvider.GetCurrentAsync(cancellationToken);
        var policyError = policy.Validate(newPassword);
        if (policyError is not null)
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

    /// <summary>
    /// نام کامل نمایشی کاربر — چرخهٔ ۷۲: پرسنل متصل (نام + نام خانوادگی)؛
    /// کاربر بدون پرسنل (مثل admin اولیه) به نام کاربری‌اش برمی‌گردد.
    /// </summary>
    private static string ResolveFullName(Domain.Entities.Security.User user)
    {
        var employee = user.Employee;
        if (employee is null)
            return user.Username;

        var full = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrEmpty(full) ? user.Username : full;
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
