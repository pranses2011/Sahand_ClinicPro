using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Infrastructure.Identity;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// چرخه دوازدهم (A1 — بند ۴-۲ سند تحلیل):
/// نگهبان کلید امضای JWT — در تولید، کلید پیش‌فرض یا کوتاه نباید پذیرفته شود.
/// کلید پیش‌فرض فقط برای توسعه مجاز است (user-secrets/متغیر محیطی برای تولید).
/// </summary>
public class JwtKeyGuardTests
{
    /// <summary>ساخت پیکربندی درون‌حافظه با کلید دلخواه</summary>
    private static IConfiguration MakeConfig(string? secretKey, string environment)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Environment"] = environment,
            ["Jwt:Issuer"] = "Sahand.ClinicPro",
            ["Jwt:Audience"] = "ClinicProClients",
            ["Jwt:ExpiryMinutes"] = "60",
        };
        if (secretKey is not null)
            dict["Jwt:SecretKey"] = secretKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void Production_WithDefaultKey_MustThrowWithClearPersianMessage()
    {
        // کلید پیش‌فرضِ سورس — در تولید باید رد شود
        var act = () =>
        {
            using var _ = new ServiceCollection()
                .AddIdentityServices(MakeConfig(
                    "SahandClinicPro-Development-Fallback-Secret-Key-Must-Change!2026",
                    "Production"))
                .BuildServiceProvider();
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:SecretKey*");
    }

    [Fact]
    public void Production_WithMissingKey_MustThrow()
    {
        var act = () =>
        {
            using var _ = new ServiceCollection()
                .AddIdentityServices(MakeConfig(null, "Production"))
                .BuildServiceProvider();
        };

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Production_WithShortKey_MustThrow()
    {
        // کمتر از ۳۲ بایت — حتی اگر تصادفی باشد
        var act = () =>
        {
            using var _ = new ServiceCollection()
                .AddIdentityServices(MakeConfig("short-key-1234", "Production"))
                .BuildServiceProvider();
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*۳۲ بایت*");
    }

    [Fact]
    public void Production_WithStrongRandomKey_MustSucceed()
    {
        // کلید قوی تصادفی (۶۴ بایت) — باید بدون استثنا ساخته شود
        var strong = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

        var act = () =>
        {
            using var provider = new ServiceCollection()
                .AddIdentityServices(MakeConfig(strong, "Production"))
                .BuildServiceProvider();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Development_WithDefaultKey_MustSucceedForLocalWork()
    {
        // در توسعه کلید پیش‌فرض مجاز است تا راه‌اندازی محلی ساده بماند
        var act = () =>
        {
            using var provider = new ServiceCollection()
                .AddIdentityServices(MakeConfig(null, "Development"))
                .BuildServiceProvider();
        };

        act.Should().NotThrow();
    }
}
