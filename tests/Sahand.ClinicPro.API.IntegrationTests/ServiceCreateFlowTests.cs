using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// جریان ثبت خدمت تعرفه‌دار — گزارش مالک: «ثبت خدمت جدید خطای غیرمنتظره
/// در سرور رخ داد» حتی پس از اصلاح کد خودکار. این تست‌ها دقیقاً همان
/// بدنه‌ای را می‌فرستند که کلاینت دسکتاپ می‌فرستد تا خطای واقعی (وضعیت و
/// متن پاسخ سرور) در CI دیده و قفل شود.
/// </summary>
[Collection("Api")]
public class ServiceCreateFlowTests
{
    private readonly InMemoryApiFactory _factory;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ServiceCreateFlowTests(InMemoryApiFactory factory) => _factory = factory;

    private sealed class Packet<T>
    {
        public T? Data { get; set; }
    }

    private sealed class LoginData
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private async Task<HttpClient> LoginAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var packet = await login.Content.ReadFromJsonAsync<Packet<LoginData>>(_json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", packet!.Data!.AccessToken);
        return client;
    }

    /// <summary>همان بدنهٔ کلاینت دسکتاپ برای ساخت خدمت (کد خالی)</summary>
    private static object DesktopBody(string name, string? code) => new
    {
        serviceName = name,
        serviceCode = code,
        price = 150000m,
        defaultCost = 20000m,
        insuranceShare = 0m,
        patientShare = 100m,
        isActive = true,
        insuranceCompanyId = (long?)null
    };

    [Fact]
    public async Task CreateService_Twice_WithEmptyCode_Should_Succeed_WithAutoCodes()
    {
        var client = await LoginAsync();

        var first = await client.PostAsJsonAsync("/api/v1/lookups/services",
            DesktopBody("خدمت آزمایشی یک", null));
        var firstBody = await first.Content.ReadAsStringAsync();
        first.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"ساخت خدمت بدون کد باید موفق باشد — پاسخ سرور: {firstBody}");

        var second = await client.PostAsJsonAsync("/api/v1/lookups/services",
            DesktopBody("خدمت آزمایشی دو", null));
        var secondBody = await second.Content.ReadAsStringAsync();
        second.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"ساخت خدمت دوم بدون کد هم باید با کد خودکار موفق باشد — پاسخ سرور: {secondBody}");

        // هر دو خدمت در فهرست مدیریت با کدهای خودکار و متفاوت دیده شوند
        var list = await client.GetAsync("/api/v1/lookups/services/manage");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var packet = await list.Content.ReadFromJsonAsync<Packet<List<ManageRow>>>(_json);
        var created = packet!.Data!
            .Where(r => r.ServiceName.StartsWith("خدمت آزمایشی"))
            .ToList();
        created.Should().HaveCount(2);
        created.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.ServiceCode),
            because: "کد خالی باید خودکار ساخته شده باشد");
        created.Select(r => r.ServiceCode).Distinct().Should().HaveCount(2,
            because: "کدهای خودکار باید یکتا باشند");
    }

    [Fact]
    public async Task CreateService_DuplicateCode_And_LongName_Should_Be_400_Not_500()
    {
        var client = await LoginAsync();

        var ok = await client.PostAsJsonAsync("/api/v1/lookups/services",
            DesktopBody("خدمت کد دستی", "MANUAL-1"));
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        var dup = await client.PostAsJsonAsync("/api/v1/lookups/services",
            DesktopBody("خدمت کد تکراری", "MANUAL-1"));
        dup.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "کد تکراری باید خطای اعتبارسنجی فارسی بدهد، نه خطای ۵۰۰");

        var longName = await client.PostAsJsonAsync("/api/v1/lookups/services",
            DesktopBody(new string('خ', 60), null));
        longName.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "نام بلندتر از سقف دیتابیس باید ۴۰۰ فارسی بدهد، نه خطای ۵۰۰");
    }

    private sealed class ManageRow
    {
        public string? ServiceCode { get; set; }
        public string ServiceName { get; set; } = string.Empty;
    }
}
