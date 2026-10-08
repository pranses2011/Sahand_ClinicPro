using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
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

        /* ── چرخه دوازدهم (بند ۴-۲ سند تحلیل): نگهبان کلید امضا ──
           کلید پیش‌فرض/کوتاه در «تولید» یعنی هرکسی بتواند توکن جعلی بسازد؛
           در محیط تولید سرور با پیام روشن بالا نمی‌آید تا نصب‌کننده وادار به
           تنظیم کلید قوی شود (تولید: متغیر محیطی/Secrets؛ توسعه: user-secrets).
           حداقل: ۳۲ بایت (۲۵۶ بیت) و نامساوی مقدار پیش‌فرض. */
        const string DefaultKey = "SahandClinicPro-Development-Fallback-Secret-Key-Must-Change!2026";
        var isProduction = !string.Equals(
            configuration["Environment"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "",
            "Development", StringComparison.OrdinalIgnoreCase);
        var secret = string.IsNullOrEmpty(jwt.SecretKey) ? DefaultKey : jwt.SecretKey;
        if (isProduction)
        {
            if (string.IsNullOrEmpty(jwt.SecretKey) || jwt.SecretKey == DefaultKey)
                throw new InvalidOperationException(
                    "⛔ کلید امضای JWT تنظیم نشده است (Jwt:SecretKey). در تولید، یک کلید تصادفی حداقل ۶۴ کاراکتری " +
                    "به‌صورت متغیر محیطی یا Secret Manager تنظیم کنید — سرور برای ایمنی شما بالا نمی‌آید.");
            if (System.Text.Encoding.UTF8.GetBytes(secret).Length < 32)
                throw new InvalidOperationException(
                    "⛔ کلید امضای JWT کوتاه‌تر از ۳۲ بایت است (Jwt:SecretKey) — حداقل ۶۴ کاراکتر تصادفی لازم است. " +
                    "نمونه تولید: openssl rand -base64 48");
        }

        // احراز هویت JWT Bearer — با کلید از تنظیمات
        var signingKey = System.Text.Encoding.UTF8.GetBytes(secret);

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

            // پاسخ فارسی برای توکن منقضی + پذیرش توکن Hub از query string (گام R1)
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        // چرخه بیست‌وچهارم: اگر پاسخ قبلاً آغاز شده باشد نوشتن
                        // دوبارهٔ آن استثنای ثانویه می‌اندازد (گزارش هفدهم مالک)
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            context.Response.WriteAsync(JsonSerializer.Serialize(new
                            {
                                isSuccess = false,
                                message = "توکن دسترسی منقضی شده است — دوباره وارد شوید"
                            }));
                        }
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                },

                // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — گارد چالش ۴۰۱ ═══
                // لاگ سرور: «InvalidOperationException در JwtBearerHandler.
                // HandleChallengeAsync — StatusCode cannot be set because the
                // response has already started». وقتی درخواستِ بدون/با توکنِ
                // نامعتبر به اندپوینت محافظت‌شده می‌رسد و پاسخ به هر دلیلی
                // (میانهٔ خطای قبلی، فشرده‌سازی و…) آغاز شده باشد، چالشِ
                // پیش‌فرض ۴۰۱ کرش می‌کرد و «unhandled exception» می‌شد.
                // با شروعِ دستی رویداد، پاسخِ آغازشده را به چالش نمی‌فرستیم؛
                // رفتار عادی (هدر WWW-Authenticate + ۴۰۱ JSON فارسی) حفظ شد.
                OnChallenge = context =>
                {
                    if (context.Response.HasStarted)
                    {
                        // پاسخ آغاز شده — تعیین کد وضعیت ممکن نیست؛ فقط اعلان
                        // پیش‌فرض متوقف و درخواست خاتمه می‌یابد بدون استثنا
                        context.HandleResponse();
                        return System.Threading.Tasks.Task.CompletedTask;
                    }

                    // پاسخ فارسی استاندارد به‌جای Challenge خالی پیش‌فرض —
                    // کلاینت WPF پیام روشن می‌گیرد (توکن نیست/منقضی/نامعتبر)
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        isSuccess = false,
                        statusCode = 401,
                        message = string.IsNullOrEmpty(context.Error)
                            ? "برای این عملیات باید وارد برنامه شوید"
                            : "اعتبارنامه یا دسترسی شما معتبر نیست — دوباره وارد شوید",
                        errors = new List<string>(),
                        traceId = context.HttpContext.TraceIdentifier,
                        data = (object?)null,
                        elapsedMs = 0
                    }));
                    // جلوگیری از چالش پیش‌فرض (نوشتن سرآیند WWW-Authenticate
                    // و متن خالی) — بدنهٔ JSON خودمان پاسخ نهایی است
                    context.HandleResponse();
                    return System.Threading.Tasks.Task.CompletedTask;
                },

                // کلاینت WPF SignalR نمی‌تواند هدر Authorization بفرستد؛
                // طبق الگوی رسمی ASP.NET Core توکن از query string «access_token» خوانده می‌شود
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/queue") || path.StartsWithSegments("/hubs/notifications")))
                    {
                        context.Token = accessToken;
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
