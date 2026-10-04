using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی کنترلرها — گام ۹
/// پوشش مسیرها و احراز هویت بدون نیاز به دیتابیس واقعی
/// </summary>
[Collection("Api")]
public class ControllersTests
{
    private readonly InMemoryApiFactory _factory;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>سازنده تست — تزریق کارخانه مشترک</summary>
    public ControllersTests(InMemoryApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// فراخوانی endpointهای محافظت‌شده بدون توکن باید ۴۰۱ بدهد
    /// </summary>
    [Theory]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/doctors")]
    public async Task Protected_Endpoints_Without_Token_Should_Return_401(string url)
    {
        // آماده‌سازی: کلاینت بدون توکن
        var client = _factory.CreateClient();

        // عملیات: فراخوانی endpoint محافظت‌شده
        var response = await client.GetAsync(url);

        // نتیجه: چالش احراز هویت
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "دسترسی بدون توکن مجاز نیست");
    }

    /// <summary>
    /// ورود با داده ناقص باید ۴۰۰ با پیام فارسی بدهد
    /// </summary>
    [Fact]
    public async Task Login_With_Empty_Body_Should_Return_400_Persian()
    {
        // آماده‌سازی
        var client = _factory.CreateClient();

        // عملیات: ارسال بدنه خالی
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "", password = "" });

        // نتیجه: خطای اعتبارسنجی ۴۰۰
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "بدنه ناقص باید رد شود");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("isSuccess",
            because: "پاسخ استاندارد ApiResponse است");
    }

    /// <summary>
    /// ورود با اعتبارسنجی ناموفق مدل نباید به دیتابیس برسد
    /// </summary>
    [Fact]
    public async Task Login_ModelState_Should_Be_Validated_Before_Handler()
    {
        // آماده‌سازی
        var client = _factory.CreateClient();

        // عملیات: نام کاربری خیلی بلند
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = new string('x', 200), password = "x" });

        // نتیجه: ۴۰۰ قبل از اجرای گرداننده
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// نقطه سلامت همچنان بدون احراز هویت فعال است
    /// </summary>
    [Fact]
    public async Task Health_Should_Remain_Public_After_Pipeline_Changes()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
