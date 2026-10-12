using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// کارخانهٔ تست مرز API — چرخهٔ ۵۲؛ دیتابیس درون‌حافظه‌ای اختصاصی.
/// چرخهٔ ۵۸ (بازخورد CI): سهمیه‌های نرخ، «هر مجموعهٔ xUnit» را یک میزبان واحد
/// می‌سنجند؛ پس تست‌های عمداً سهمیه‌سوز (انفجار لایسنس، قفل حساب) به
/// کارخانه‌های جدا رفتند تا شمارش‌ها قطعی شوند.
/// </summary>
public class ApiBoundaryApiFactory : InMemoryApiFactory
{
    /// <summary>نام دیتابیس اختصاصی مرز</summary>
    protected override string DatabaseName => "ApiBoundaryDb";
}

/// <summary>مجموعهٔ تست مرز API</summary>
[CollectionDefinition("ApiBoundary")]
public class ApiBoundaryCollectionFixture : ICollectionFixture<ApiBoundaryApiFactory>
{
}

/// <summary>کارخانهٔ اختصاصی تست‌های سقف نرخ لایسنس — بودجهٔ ۲۰/دقیقه جدا</summary>
public class ApiRateLimitApiFactory : InMemoryApiFactory
{
    /// <summary>نام دیتابیس اختصاصی نرخ</summary>
    protected override string DatabaseName => "ApiRateLimitDb";
}

/// <summary>مجموعهٔ تست سقف نرخ لایسنس</summary>
[CollectionDefinition("ApiRateLimit")]
public class ApiRateLimitCollectionFixture : ICollectionFixture<ApiRateLimitApiFactory>
{
}

/// <summary>کارخانهٔ اختصاصی تست قفل حساب — سهمیهٔ auth (۵/دقیقه) جدا</summary>
public class ApiAuthLockApiFactory : InMemoryApiFactory
{
    /// <summary>نام دیتابیس اختصاصی قفل حساب</summary>
    protected override string DatabaseName => "ApiAuthLockDb";
}

/// <summary>مجموعهٔ تست قفل حساب</summary>
[CollectionDefinition("ApiAuthLock")]
public class ApiAuthLockCollectionFixture : ICollectionFixture<ApiAuthLockApiFactory>
{
}

/// <summary>
/// تست‌های مرز API — چرخهٔ ۵۲ (BUG-06):
/// اندپوینت‌های عمومی مستندشده بدون توکن در دسترس؛ مسیرهای محافظت‌شده ۴۰۱ با
/// پاکت استاندارد؛ توکن تازه‌سازی نامعتبر ۴۰۰ فارسی؛ نسخه‌بندی مسیر؛ قرارداد سلامت.
/// بودجهٔ سهمیه‌ها در این کلاس عمداً سبک نگه داشته شده (۵ لایسنس + ۲ auth) تا
/// هیچ ادعایی به ترتیب اجرا وابسته نباشد — سهمیه‌سوزها در مجموعه‌های خودشان‌اند.
/// </summary>
[Collection("ApiBoundary")]
public class ApiBoundaryTests
{
    private readonly ApiBoundaryApiFactory _factory;

    /// <summary>سازنده تست — تزریق کارخانهٔ اختصاصی</summary>
    public ApiBoundaryTests(ApiBoundaryApiFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════ ۱) اندپوینت‌های عمومی مستندشده ═══════════════

    /// <summary>سه اندپوینت خواندنی لایسنس بدون توکن — وابستگی پنجرهٔ فعال‌سازی</summary>
    [Fact]
    public async Task Anonymous_License_Read_Endpoints_Should_Be_Reachable_Without_Token()
    {
        var client = _factory.CreateClient();

        (await client.GetAsync("/api/v1/license/status")).StatusCode
            .Should().Be(HttpStatusCode.OK, because: "وضعیت لایسنس پیش از ورود لازم است");
        (await client.GetAsync("/api/v1/license/support")).StatusCode
            .Should().Be(HttpStatusCode.OK, because: "اطلاعات پشتیبانی برای کاربر قفل‌شده لازم است");
        (await client.GetAsync("/api/v1/license/settings")).StatusCode
            .Should().Be(HttpStatusCode.OK, because: "پنجرهٔ فعال‌سازی نشانی سرور را می‌خواند");
    }

    /// <summary>ساخت درخواست فعال‌سازی آفلاین بدون احراز هویت</summary>
    [Fact]
    public async Task Activation_Request_Should_Build_Offline_Request_Without_Auth()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/license/activation-request?licenseKey=SCPRO-TEST");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"isSuccess\":true", because: "پاسخ استاندارد موفق است");
    }

    /// <summary>
    /// ذخیرهٔ نشانی سرور لایسنس با طرح غیرمجاز (ftp) باید ۴۰۰ فارسی بگیرد —
    /// رفع چرخهٔ ۵۸: پیش‌تر ftp:// با پیش‌افزودن ناخواستهٔ https «معتبر» جلوه
    /// می‌کرد و ذخیره می‌شد (هدایت بالقوه به سرور مهاجم).
    /// </summary>
    [Fact]
    public async Task License_Settings_Put_With_NonHttp_Scheme_Should_Return_400_Persian()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/license/settings",
            new { portalUrl = "ftp://evil.example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "طرح غیر http/https هرگز نباید پذیرفته شود");
        // بدنهٔ HTTP با کدگذاری پیش‌فرض JSON فارسی را \uXXXX می‌کند — با تجزیه می‌سنجم
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Contain("نشانی سرور لایسنس نامعتبر است");
    }

    // ═══════════════ ۲) مسیرهای محافظت‌شده ═══════════════

    /// <summary>مسیرهای اصلی ماژول‌ها بدون توکن فقط ۴۰۱ — نه ۵۰۰/۴۰۳/۴۰۵</summary>
    [Theory]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/doctors")]
    [InlineData("/api/v1/appointments")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/reports/daily-revenue?days=7")]
    [InlineData("/api/v1/backup/cloud/status")]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/roles")]
    [InlineData("/api/v1/branches")]
    [InlineData("/api/v1/notifications")]
    [InlineData("/api/v1/permissions")]
    public async Task Protected_Endpoints_Without_Token_Should_Return_401(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "مسیر «{0}» محافظت‌شده است و بدون توکن فقط چالش احراز هویت می‌گیرد", url);
    }

    /// <summary>بدنهٔ ۴۰۱ باید پاکت استاندارد فارسی باشد (پیام رمزگشایی‌شده از JSON)</summary>
    [Fact]
    public async Task Unauthorized_Body_Should_Be_Standard_Persian_ApiResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/patients");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        json.GetProperty("statusCode").GetInt32().Should().Be(401);
        json.GetProperty("message").GetString().Should().Contain("وارد برنامه شوید",
            because: "پیام ۴۰۱ فارسی و راهنمای اقدام دارد");
    }

    /// <summary>نسخهٔ ناشناخته (v99) باید ۴۰۴ بگیرد نه ۴۰۱ — نسخه‌بندی مسیرها زنده است</summary>
    [Fact]
    public async Task Unknown_Api_Version_Should_Return_404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v99/patients");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════ ۳) جریان احراز هویت (سبک — سهمیهٔ auth فقط ۲ درخواست) ═══════════════

    /// <summary>
    /// تمدید توکن با مقدار آشغال باید ۴۰۰ با پیام فارسی بگیرد نه ۵۰۰ —
    /// کلاینت دسکتاپ روی همین پیام «توکن منقضی» را نشان می‌دهد.
    /// </summary>
    [Fact]
    public async Task Refresh_With_Garbage_Token_Should_Return_400_With_Persian_Message()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = "garbage-token-value" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Contain("توکن تازه‌سازی نامعتبر است");
    }

    /// <summary>خروج بدون توکن قابل فراخوان است — خود refresh token در بدنه است</summary>
    [Fact]
    public async Task Logout_Is_Reachable_Without_Token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout",
            new { refreshToken = "any" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "خروج به توکنِ هدر وابسته نیست");
    }

    // ═══════════════ ۴) اسکلت عمومی ═══════════════

    /// <summary>Swagger در محیط توسعه فعال است — قرارداد OpenAPI زنده می‌ماند</summary>
    [Fact]
    public async Task Swagger_Should_Be_Available_In_Development()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "کارخانه در محیط Development اجرا می‌شود");
    }

    /// <summary>بدنهٔ سلامت باید وضعیت و زمان سرور را بدهد — قرارداد مانیتورینگ</summary>
    [Fact]
    public async Task Health_Body_Should_Report_Status_And_ServerTime()
    {
        var json = await _factory.CreateClient()
            .GetFromJsonAsync<JsonElement>("/health");

        json.GetProperty("status").GetString().Should().Be("Healthy");
        json.TryGetProperty("serverTime", out _).Should().BeTrue(because: "زمان سرور برای همگامی ساعت کلاینت لازم است");
    }
}

/// <summary>
/// سقف نرخ اندپوینت‌های لایسنس (چرخهٔ ۴۹) — چرخهٔ ۵۸:
/// مجموعهٔ اختصاصی تا ۲۱ درخواستِ انفجاری، سهمیهٔ ۲۰/دقیقهٔ بقیهٔ تست‌ها را نخورد.
/// </summary>
[Collection("ApiRateLimit")]
public class ApiRateLimitTests
{
    private readonly ApiRateLimitApiFactory _factory;

    /// <summary>سازنده تست — تزریق کارخانهٔ اختصاصی نرخ</summary>
    public ApiRateLimitTests(ApiRateLimitApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// انفجار ۲۱ درخواست به وضعیت لایسنس: دقیقاً پس از سقفِ ۲۰/دقیقه باید ۴۲۹ بیاید —
    /// نه زودتر (یعنی پیکربندی سخت‌تر از سند) نه هرگز (یعنی سقف خاموش است).
    /// </summary>
    [Fact]
    public async Task License_Endpoints_Burst_Over_Ceiling_Should_Get_429()
    {
        var client = _factory.CreateClient();
        var codes = new List<HttpStatusCode>();

        for (var i = 0; i < 21; i++)
            codes.Add((await client.GetAsync("/api/v1/license/status")).StatusCode);

        codes.Should().Contain(HttpStatusCode.TooManyRequests,
            because: "بیش از ۲۰ درخواست در دقیقه به اندپوینت‌های لایسنس باید سقف را فعال کند");
        codes.Count(c => c == HttpStatusCode.TooManyRequests).Should().BeLessThan(21,
            because: "سقف باید ۲۰ درخواست اول را رد کند — قفل کامل یعنی پیکربندی غلط");
        codes.Count(c => c == HttpStatusCode.OK).Should().BeGreaterThanOrEqualTo(20,
            because: "۲۰ درخواست اولِ سهمیهٔ این مجموعهٔ ایزوله باید موفق باشند");
    }
}

/// <summary>
/// قفل حساب پس از پنج شکست پیاپی — چرخهٔ ۵۸:
/// مجموعهٔ اختصاصی تا ۵ لاگین ناموفق، سهمیهٔ auth (۵/دقیقه) بقیهٔ تست‌ها را نخورد.
/// </summary>
[Collection("ApiAuthLock")]
public class ApiAuthLockTests
{
    private readonly ApiAuthLockApiFactory _factory;

    /// <summary>سازنده تست — تزریق کارخانهٔ اختصاصی قفل</summary>
    public ApiAuthLockTests(ApiAuthLockApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// پنج تلاش ناموفق پیاپی: هر تلاش ۴۰۰ با پیام فارسی «نادرست» می‌گیرد و در پایان،
    /// حساب ادمین در دیتابیس قفل (LockoutEnd فعال) است — گواهی از وضعیت دامنه.
    /// </summary>
    [Fact]
    public async Task Five_Failed_Logins_Should_Lock_Account_With_Persian_Errors()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { usernameOrEmail = "admin", password = "WrongPass#000" + i });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                because: "رمز نادرست خطای بدنه نیست — ۴۰۰ با پیام فارسی است (تلاش {0})", i + 1);
            (await response.Content.ReadAsStringAsync())
                .Should().Contain("نام کاربری یا رمز عبور نادرست است");
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var admin = await db.Users.FirstAsync(u => u.Username == "admin");
        admin.LockoutEnd.Should().NotBeNull(because: "پنج تلاش ناموفق پیاپی حساب را قفل می‌کند");
        admin.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow,
            because: "قفل هنوز فعال است (۳۰ دقیقه از لحظهٔ تلاش پنجم)");
    }
}
