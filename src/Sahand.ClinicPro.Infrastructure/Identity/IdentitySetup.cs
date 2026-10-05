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

            // پاسخ فارسی برای توکن منقضی + پذیرش توکن Hub از query string (گام R1)
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
