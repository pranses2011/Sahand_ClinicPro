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
/// کارخانهٔ تست مرز API — دیتابیس درون‌حافظه‌ای اختصاصی
/// چرخهٔ ۵۲: ایزوله از مجموعهٔ «Api» مشترک؛ چون تست‌های این کلاس عمداً
/// سقف نرخ (429) و قفل حساب را تحریک می‌کنند و نباید به کارخانهٔ مشترک
/// (که سایر کلاس‌ها با لاگینِ ادمینِ سالم کار می‌کنند) سرایت کنند.
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

/// <summary>
/// تست‌های مرز API — چرخهٔ ۵۲ (BUG-06):
/// اندپوینت‌های عمومیِ مستندشده (۱۲ مورد چرخهٔ ۴۹) باید بدون توکن در دسترس
/// باشند؛ بقیهٔ مسیرها ۴۰۱ با پاسخ استاندارد فارسی؛ ورود ناموفق ۴۰۰ با
/// پیام فارسی؛ پنج شکست پیاپی = قفل حساب؛ توکن تازه‌سازی نامعتبر = ۴۰۰ فارسی؛
/// سقف نرخ اندپوینت‌های لایسنس (چرخهٔ ۴۹) زنده است؛ و نسخه‌بندی مسیرها.
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

    /// <summary>
    /// سه اندپوینت خواندنی لایسنس باید بدون توکن ۲۰۰ بدهند — پنجرهٔ فعال‌سازی
    /// دسکتاپ پیش از ورود به آن‌ها وابسته است (مرور چرخهٔ ۴۹).
    /// </summary>
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

    /// <summary>
    /// ساخت درخواست فعال‌سازی آفلاین بدون احراز هویت — گردش‌کار فعال‌سازی برون‌خط.
    /// </summary>
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
    /// ذخیرهٔ نشانی سرور لایسنس با طرح غیرمجاز (ftp) باید هرگز پذیرفته نشود —
    /// پذیرش کورکورانه هر نشانه‌ای خطر هدایت کلاینت به سرور مهاجم دارد.
    /// ادعا مقاوم به ترتیب اجراست: سقف نرخ لایسنس (چرخهٔ ۴۹) ممکن است این
    /// درخواست را ۴۲۹ داده باشد؛ آنچه امنیتی است «هرگز ۲۰۰» است و بدنهٔ فارسی در ۴۰۰.
    /// </summary>
    [Fact]
    public async Task License_Settings_Put_With_NonHttp_Scheme_Should_Never_Be_Accepted()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/license/settings",
            new { portalUrl = "ftp://evil.example.com" });

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            because: "نشانی با طرح غیر http/https هرگز نباید ذخیره شود");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            (await response.Content.ReadAsStringAsync())
                .Should().Contain("نشانی سرور لایسنس نامعتبر است");
        }
    }

    // ═══════════════ ۲) مسیرهای محافظت‌شده ═══════════════

    /// <summary>
    /// همهٔ مسیرهای اصلی ماژول‌ها بدون توکن باید ۴۰۱ بدهند — نه ۵۰۰ نه ۴۰۳.
    /// </summary>
    [Theory]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/doctors")]
    [InlineData("/api/v1/appointments")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/payments")]
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

    /// <summary>
    /// بدنهٔ ۴۰۱ باید پاسخ استاندارد فارسی ApiResponse باشد — کلاینت WPF
    /// روی همین قرارداد پیام‌های «دوباره وارد شوید» را نشان می‌دهد.
    /// </summary>
    [Fact]
    public async Task Unauthorized_Body_Should_Be_Standard_Persian_ApiResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/patients");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("\"isSuccess\":false");
        body.Should().Contain("وارد برنامه شوید",
            because: "پیام ۴۰۱ فارسی و راهنمای اقدام دارد");
    }

    /// <summary>
    /// نسخهٔ ناشناختهٔ مسیر (v99) باید ۴۰۴ بگیرد نه ۴۰۱ — نسخه‌بندی مسیرها زنده است.
    /// </summary>
    [Fact]
    public async Task Unknown_Api_Version_Should_Return_404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v99/patients");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════ ۳) جریان احراز هویت ═══════════════

    /// <summary>
    /// پنج تلاش ناموفق پیاپی: هر تلاش ۴۰۰ با پیام فارسی «نادرست» می‌گیرد و در
    /// پایان، حساب ادمین در دیتابیس قفل (LockoutEnd فعال) است — گواهی از خودِ
    /// وضعیت دامنه، بی‌نیاز به ششمین درخواست که سقفِ نرخِ ورود (۵/دقیقه) آن را می‌گرفت.
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

        // گواهی قفل از وضعیت دامنه — پنج شکست پیاپی ⟵ LockoutEnd سه‌دقیقه‌ای فعال (۳۰ دقیقه)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var admin = await db.Users.FirstAsync(u => u.Username == "admin");
        admin.LockoutEnd.Should().NotBeNull(because: "پنج تلاش ناموفق پیاپی حساب را قفل می‌کند");
        admin.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow,
            because: "قفل هنوز فعال است (۳۰ دقیقه از لحظهٔ تلاش پنجم)");
    }

    /// <summary>
    /// تمدید توکن با مقدار آشغال باید ۴۰۰ با پیام فارسی بگیرد نه ۵۰۰ —
    /// کلاینت روی همین پیام «توکن منقضی» را به کاربر نشان می‌دهد.
    /// </summary>
    [Fact]
    public async Task Refresh_With_Garbage_Token_Should_Return_400_With_Persian_Message()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = "garbage-token-value" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain("توکن تازه‌سازی نامعتبر است");
    }

    /// <summary>
    /// خروج بدون توکن قابل فراخوان است (ابطالِ خودِ refresh token در بدنه است) —
    /// فقط «قابل دسترس» را می‌سنجم، نه نتیجهٔ منطقی‌اش را.
    /// </summary>
    [Fact]
    public async Task Logout_Is_Reachable_Without_Token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout",
            new { refreshToken = "any" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "خروج به توکنِ هدر وابسته نیست — خود refresh token در بدنه است");
    }

    // ═══════════════ ۴) سقف نرخ اندپوینت‌های لایسنس (چرخهٔ ۴۹) ═══════════════

    /// <summary>
    /// انفجار ۲۱ درخواست به وضعیت لایسنس باید حداقل یک ۴۲۹ بگیرد —
    /// سقف ۲۰/دقیقهٔ چرخهٔ ۴۹ روی تنها مسیر نوشتاریِ عمومیِ ریسک‌دار زنده است.
    /// (درخواست‌های مصرفی این کلاس سقف را فقط زودتر فعال می‌کنند — ادعای «حداقل یک ۴۲۹» پایدار است)
    /// </summary>
    [Fact]
    public async Task License_Endpoints_Burst_Over_Ceiling_Should_Get_429()
    {
        var client = _factory.CreateClient();
        var codes = new List<HttpStatusCode>();

        for (var i = 0; i < 21; i++)
            codes.Add((await client.GetAsync("/api/v1/license/status")).StatusCode);

        codes.Should().Contain(HttpStatusCode.TooManyRequests,
            because: "بیش از ۲۰ درخواست در دقیقه به اندپوینت‌های لایسنس نباید بی‌پاسخ بماند");
        codes.Count(c => c == HttpStatusCode.TooManyRequests).Should().BeLessThan(21,
            because: "سقف نرخ باید اجازهٔ ۲۰ درخواست اول را بدهد — قفل کامل یعنی تنظیم غلط");
    }

    // ═══════════════ ۵) اسکلت عمومی ═══════════════

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
