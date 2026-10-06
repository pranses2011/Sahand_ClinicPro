using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست‌های کمکی ApiHttp — چرخه چهاردهم (گزارش پنجم مالک):
/// دو باگ پنهانِ ارتباط دسکتاپ↔API که تا اولین اجرای واقعی پنهان مانده بودند:
/// (۱) افتادن قطعه «v1» در ترکیب مسیر نسبی با BaseAddress بدون اسلش پایانی →
///     همه درخواست‌ها به api/… بدون v1 می‌رفتند و 404 با بدنه خالی می‌گرفتند؛
/// (۲) پاکت استاندارد isSuccess/message/data که لاگین و تمدید آن را باز نمی‌کردند.
/// </summary>
public class ApiHttpTests
{
    // ── نرمال‌سازی آدرس پایه ──

    [Fact]
    public void NormalizeBaseUrl_AppendsTrailingSlash_WhenMissing()
    {
        Assert.Equal("http://localhost:5000/api/v1/",
            ApiHttp.NormalizeBaseUrl("http://localhost:5000/api/v1"));
    }

    [Fact]
    public void NormalizeBaseUrl_KeepsSingleTrailingSlash_WhenAlreadyPresent()
    {
        var normalized = ApiHttp.NormalizeBaseUrl("http://localhost:5000/api/v1/");
        Assert.Equal("http://localhost:5000/api/v1/", normalized);
        // بدون ایجاد اسلش دوتایی در انتهای مسیر
        Assert.False(normalized.EndsWith("//"), "نباید به اسلش دوتایی ختم شود");
    }

    [Fact]
    public void NormalizeBaseUrl_FallsBackToDefault_WhenBlank()
    {
        Assert.Equal("http://localhost:5000/api/v1/", ApiHttp.NormalizeBaseUrl(null));
        Assert.Equal("http://localhost:5000/api/v1/", ApiHttp.NormalizeBaseUrl(string.Empty));
        Assert.Equal("http://localhost:5000/api/v1/", ApiHttp.NormalizeBaseUrl("   "));
    }

    // ── رگرسیون 404: رفتار ترکیب مسیر طبق RFC 3986 ──

    [Fact]
    public void UriCombination_WithNormalizedBase_KeepsVersionSegment()
    {
        // بعد از نرمال‌سازی، مسیر نسبی بدون افتادن «v1» ترکیب می‌شود
        var baseUri = new Uri(ApiHttp.NormalizeBaseUrl("http://localhost:5000/api/v1"));
        var full = new Uri(baseUri, "auth/login");
        Assert.Equal("http://localhost:5000/api/v1/auth/login", full.AbsoluteUri);
    }

    [Fact]
    public void UriCombination_WithoutTrailingSlash_DropsVersionSegment()
    {
        // مستندسازی باگ اصلی: بدون اسلش پایانی، قطعه آخر مسیر پایه می‌افتد و
        // درخواست به api/auth/login (بدون v1) می‌رسد → 404 با بدنه خالی
        var baseUri = new Uri("http://localhost:5000/api/v1");
        var full = new Uri(baseUri, "auth/login");
        Assert.Equal("http://localhost:5000/api/auth/login", full.AbsoluteUri);
    }

    // ── باز کردن پاکت استاندارد پاسخ ──

    [Fact]
    public void ParseEnvelope_LoginSuccess_ExtractsTokensFromDataField()
    {
        // هم‌شکل پاسخ واقعی POST api/v1/auth/login (camelCase از سرور)
        var json = """
            {
              "isSuccess": true,
              "message": "ورود با موفقیت انجام شد",
              "data": {
                "accessToken": "token-abc",
                "refreshToken": "refresh-xyz",
                "expiresIn": 1800,
                "username": "admin",
                "displayName": "مدیر سیستم",
                "roles": ["Admin"],
                "permissions": [],
                "mustChangePassword": true
              }
            }
            """;

        var envelope = ApiHttp.ParseEnvelope<LoginPayload>(json);

        Assert.NotNull(envelope);
        Assert.True(envelope.IsSuccess);
        Assert.Equal("ورود با موفقیت انجام شد", envelope.Message);
        Assert.NotNull(envelope.Data);
        Assert.Equal("token-abc", envelope.Data.AccessToken);
        Assert.Equal("refresh-xyz", envelope.Data.RefreshToken);
        Assert.Equal(1800, envelope.Data.ExpiresIn);
        Assert.Equal("admin", envelope.Data.Username);
        Assert.True(envelope.Data.MustChangePassword);
    }

    [Fact]
    public void ParseEnvelope_ErrorPacket_CarriesPersianMessage()
    {
        var json = """
            {
              "isSuccess": false,
              "message": "نام کاربری یا رمز عبور نادرست است",
              "statusCode": 400,
              "errors": ["AUTH_INVALID_CREDENTIALS"]
            }
            """;

        var envelope = ApiHttp.ParseEnvelope<LoginPayload>(json);

        Assert.NotNull(envelope);
        Assert.False(envelope.IsSuccess);
        Assert.Equal("نام کاربری یا رمز عبور نادرست است", envelope.Message);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public void ParseEnvelope_CorruptBody_ReturnsNull()
    {
        // پاسخ 404 خام (بدنه خالی/غیر JSON) — فراخواننده باید پیام جایگزین بسازد
        Assert.Null(ApiHttp.ParseEnvelope<LoginPayload>(""));
        Assert.Null(ApiHttp.ParseEnvelope<LoginPayload>("<html>404</html>"));
    }

    /// <summary>ساختار داده ورود — هم‌شکل LoginResult درون ApiClient</summary>
    private sealed record LoginPayload(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string Username,
        bool MustChangePassword);
}
