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
