using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// تست‌های endpoint پیشرفت مرکز آموزش — چرخه سیزدهم (درخواست ۸-۶):
/// ثبت تکمیل درس ایدمپوتنت، خواندن فهرست و بازنشانی
/// </summary>
[Collection("Flow")]
public class TrainingProgressEndpointTests
{
    private readonly FlowApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>سازنده تست — تزریق کارخانه</summary>
    public TrainingProgressEndpointTests(FlowApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>کلاینت احراز هویت‌شده ادمین</summary>
    private async Task<HttpClient> MakeClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK, "بذر ادمین باید ورود را ممکن کند");

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("data").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Progress_Lifecycle_Works()
    {
        var client = await MakeClientAsync();

        // ۱) خواندن اولیه — موفق
        var get1 = await client.GetAsync("/api/v1/training/progress");
        get1.StatusCode.Should().Be(HttpStatusCode.OK);

        // ۲) ثبت تکمیل دو درس
        (await client.PutAsync("/api/v1/training/progress/reception", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsync("/api/v1/training/progress/appointment", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // ۳) ثبت مجدد همان درس — ایدمپوتنت
        (await client.PutAsync("/api/v1/training/progress/reception", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // ۴) خواندن — هر دو درس بدون تکرار
        var get2 = await client.GetAsync("/api/v1/training/progress");
        var body = await get2.Content.ReadFromJsonAsync<JsonElement>(Json);
        var data = body.GetProperty("data");
        var lessons = data.GetProperty("completedLessons").Deserialize<List<string>>(Json)!;
        lessons.Should().Contain(new[] { "reception", "appointment" }, "هر دو درس ثبت شده‌اند");
        lessons.Count.Should().Be(2, "ثبت مجدد درس نباید تکرار بسازد");

        // ۵) بازنشانی
        (await client.DeleteAsync("/api/v1/training/progress"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var get3 = await client.GetAsync("/api/v1/training/progress");
        var body3 = await get3.Content.ReadFromJsonAsync<JsonElement>(Json);
        body3.GetProperty("data").GetProperty("completedLessons").GetArrayLength()
            .Should().Be(0, "پس از بازنشانی فهرست خالی است");
    }

    [Fact]
    public async Task CompleteLesson_InvalidId_Rejected()
    {
        var client = await MakeClientAsync();
        var bad = await client.PutAsync("/api/v1/training/progress/" + new string('x', 120), null);
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest, "شناسه بلندتر از سقف رد می‌شود");
    }

    [Fact]
    public async Task Progress_RequiresAuthentication()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/v1/training/progress");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "پیشرفت آموزش فقط برای کاربران واردشده است");
    }
}
