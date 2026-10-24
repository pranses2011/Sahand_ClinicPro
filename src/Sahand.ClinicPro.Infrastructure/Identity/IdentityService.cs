using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
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
    private readonly IEmailSender _emailSender;
    private readonly ILogger<IdentityService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public IdentityService(
        ClinicProDbContext context,
        JwtTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IPasswordPolicyProvider passwordPolicyProvider,
        IEmailSender emailSender,
        ILogger<IdentityService> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _passwordPolicyProvider = passwordPolicyProvider;
        _emailSender = emailSender;
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

        // ═══ چرخهٔ ۸۴ — رمز موقت بازیابی «در انتظار» ═══
        // رمز اصلی همیشه اولویت دارد. رمز موقتِ ایمیل‌شده فقط وقتی پذیرفته
        // می‌شود که کاربر واقعاً با خودش وارد شود؛ در همان لحظه جایگزین رمز
        // اصلی و تغییرش اجباری می‌شود. اگر نامه نادیده گرفته شود، رمز اصلی
        // سر جایش است و ورود با آن، رمز موقتِ در انتظار را هم باطل می‌کند.
        var mainPasswordOk = _passwordHasher.Verify(password, user.PasswordHash);
        var recoveryPending = !string.IsNullOrEmpty(user.PendingRecoveryPasswordHash)
            && user.PendingRecoveryExpiresAt.HasValue
            && user.PendingRecoveryExpiresAt.Value > DateTime.UtcNow;
        var recoveryPasswordOk = !mainPasswordOk && recoveryPending
            && _passwordHasher.Verify(password, user.PendingRecoveryPasswordHash!);

        if (!mainPasswordOk && !recoveryPasswordOk)
        {
            // رمز موقت منقضی پاک می‌شود تا برای همیشه معتبر نماند
            if (!string.IsNullOrEmpty(user.PendingRecoveryPasswordHash) && !recoveryPending)
            {
                user.PendingRecoveryPasswordHash = null;
                user.PendingRecoveryExpiresAt = null;
            }
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

        if (recoveryPasswordOk)
        {
            // همین لحظه (و فقط همین لحظه) رمز عوض می‌شود — با تغییر اجباری
            user.PasswordHash = user.PendingRecoveryPasswordHash!;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = true;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            _logger.LogInformation("ورود با رمز موقت بازیابی برای کاربر {User} — تغییر رمز اجباری شد", user.Username);
        }
        // چه با رمز اصلی وارد شده باشد چه با موقت، «در انتظار» دیگر معنا ندارد
        user.PendingRecoveryPasswordHash = null;
        user.PendingRecoveryExpiresAt = null;

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
        // رمز تازه، هر رمز موقتِ بازیابیِ در انتظار را هم باطل می‌کند
        user.PendingRecoveryPasswordHash = null;
        user.PendingRecoveryExpiresAt = null;

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

    /// <summary>
    /// بازیابی رمز با ایمیل — چرخهٔ ۸۲ (گزارش مالک): «فراموشی رمز» کار نمی‌کرد.
    /// رمز عبور فقط هش‌شده ذخیره می‌شود و بازیابی متن اصلی رمز ممکن نیست؛ این
    /// متد «رمز موقت» تازه می‌سازد، آن را به ایمیل ثبت‌شدهٔ همان کاربر می‌فرستد
    /// و پرچم تغییر اجباری رمز را روشن می‌کند.
    /// </summary>
    /// <remarks>
    /// ═══ ترتیب کارها عمدی است ═══
    /// اول ایمیل ارسال می‌شود و «فقط پس از موفقیت ارسال» هش رمز عوض می‌شود؛
    /// اگر SMTP تنظیم نباشد یا ارسال شکست بخورد، رمز قبلی کاربر دست‌نخورده
    /// می‌ماند و او با همان رمز همیشگی وارد می‌شود — هیچ کاربری قفل نمی‌شود.
    /// برای جلوگیری از افشای فهرست کاربران، نبودِ حساب هم «موفق» برگردانده
    /// می‌شود و پیام یکسان است.
    /// </remarks>
    public async Task<Result> SendPasswordRecoveryAsync(string usernameOrEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return Result.Failure(new Error("RECOVERY_INVALID",
                "نام کاربری یا ایمیل را وارد کنید"));

        var normalized = usernameOrEmail.Trim().ToUpperInvariant();
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => !u.IsDeleted &&
                (u.Username.ToUpper() == normalized || u.Email.ToUpper() == normalized),
                cancellationToken);

        // حساب ناشناخته/غیرفعال — پاسخ یکسان با حساب معتبر (ضد افشای فهرست کاربران)
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("درخواست بازیابی رمز برای حساب ناشناخته یا بی‌ایمیل: {Input}",
                usernameOrEmail.Trim());
            return Result.Success();
        }

        if (!_emailSender.IsConfigured)
            return Result.Failure(new Error("RECOVERY_EMAIL_DISABLED",
                "سرویس ایمیل سرور تنظیم نشده است — بخش Email در appsettings را پر کنید یا با مدیر سامانه تماس بگیرید"));

        var temporaryPassword = GenerateTemporaryPassword();
        var displayName = ResolveFullName(user);

        var email = await _emailSender.SendAsync(
            user.Email,
            "بازیابی رمز عبور — سامانه جامع مدیریت درمانگاه سهند",
            BuildRecoveryEmailBody(displayName, user.Username, temporaryPassword),
            null,
            cancellationToken);

        if (!email.Success)
        {
            _logger.LogWarning("ارسال ایمیل بازیابی رمز برای {User} ناموفق بود: {Error}",
                user.Username, email.Error);
            return Result.Failure(new Error("RECOVERY_EMAIL_FAILED",
                email.Error ?? "ارسال ایمیل ناموفق بود"));
        }

        // ═══ چرخهٔ ۸۴ — رمز «همین لحظه» عوض نمی‌شود ═══
        // گزارش مالک: «احتمال دارد شخصی اشتباهاً نام کاربری دیگری را بزند و
        // رمز او عوض شود؛ اگر نامه نادیده گرفته شد، کاربر با رمز قبلی خودش
        // وارد شود.» پس رمز موقت فقط در ستونِ «در انتظار» می‌نشیند (۲ ساعت
        // اعتبار)؛ رمز اصلی، پرچم تغییر اجباری، قفل حساب و نشست‌های باز همگی
        // دست‌نخورده می‌مانند. جایگزینی فقط هنگام ورودِ واقعی با رمز موقت
        // در AuthenticateAsync اتفاق می‌افتد.
        user.PendingRecoveryPasswordHash = _passwordHasher.Hash(temporaryPassword);
        user.PendingRecoveryExpiresAt = DateTime.UtcNow.AddHours(2);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("رمز موقت بازیابی (در انتظار، ۲ ساعت) برای کاربر {User} به ایمیل ثبت‌شده ارسال شد",
            user.Username);
        return Result.Success();
    }

    /// <summary>
    /// ساخت رمز موقت خوانا — بدون کاراکترهای اشتباه‌شدنی (O/0 و l/1) و با ترکیب
    /// حرف بزرگ، حرف کوچک، رقم و نماد تا سیاست رمز عبور را هم برآورده کند.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "@#$%&*!";
        const string all = upper + lower + digits;

        var chars = new char[12];
        chars[0] = upper[System.Security.Cryptography.RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[System.Security.Cryptography.RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[System.Security.Cryptography.RandomNumberGenerator.GetInt32(all.Length)];

        // جابه‌جایی تصادفی — نماد و رقم همیشه در جای ثابت نباشند
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>بدنهٔ فارسی و راست‌چین ایمیل بازیابی — بدون وابستگی به قالب بیرونی</summary>
    private static string BuildRecoveryEmailBody(string displayName, string username,
        string temporaryPassword)
    {
        var now = PersianDateHelper.ToShamsiWithTime(DateTime.Now);
        return $"""
        <div dir="rtl" style="font-family:Tahoma,Segoe UI,sans-serif;text-align:right;line-height:1.9;
                    color:#263238;background:#F7FAFD;padding:18px;border-radius:12px">
          <h2 style="margin:0 0 10px;color:#0078D7">بازیابی رمز عبور</h2>
          <p>کاربر گرامی <b>{displayName}</b>،</p>
          <p>درخواست بازیابی رمز برای حساب «<b>{username}</b>» در سامانهٔ درمانگاه سهند ثبت شد.</p>
          <p style="margin:14px 0">رمز موقت شما:
            <b style="font-family:Consolas,monospace;font-size:18px;letter-spacing:1px;
                      background:#EAF3FC;border:1px solid #0078D7;border-radius:6px;padding:3px 8px;
                      color:#0D47A1">{temporaryPassword}</b>
          </p>
          <p>با این رمز وارد شوید؛ سامانه در نخستین ورود، شما را به انتخاب رمز تازه راهنمایی می‌کند.</p>
          <p>این رمز موقت فقط <b>۲ ساعت</b> اعتبار دارد و تا وقتی با آن وارد نشوید، رمز قبلی شما همچنان کار می‌کند.</p>
          <p style="color:#B71C1C">اگر این درخواست از سوی شما نبوده است، کافی است این نامه را
             نادیده بگیرید — رمز شما تغییر نکرده است.</p>
          <hr style="border:none;border-top:1px dashed #B0BEC5" />
          <p style="font-size:12px;color:#78909C">زمان درخواست: {now} — این نامه به‌صورت خودکار ارسال شده است.</p>
        </div>
        """;
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
