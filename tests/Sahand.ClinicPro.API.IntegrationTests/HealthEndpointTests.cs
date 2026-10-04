using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی API — گام ۱.
/// میزبان آزمایشی درون‌حافظه‌ای (Test Server) را بالا می‌آورد و
/// صحت راه‌اندازی کامل خط لوله (Serilog، DI، Swagger، مسیریابی)
/// را از بیرون شبیه‌سازی‌شده بررسی می‌کند.
/// </summary>
[Collection("Api")]
public class HealthEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// سازنده تست — تزریق کارخانه مشترک بین تست‌های این کلاس.
    /// </summary>
    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// نقطه بررسی سلامت (/health) باید بدون احراز هویت پاسخ موفق بدهد؛
    /// کلاینت WPF از این نقطه برای سنجش دسترسی سرور استفاده می‌کند.
    /// </summary>
    [Fact]
    public async Task Health_Endpoint_Should_Return_Ok()
    {
        // آماده‌سازی: ساخت کلاینت HTTP روی میزبان آزمایشی
        var client = _factory.CreateClient();

        // عملیات: فراخوانی نقطه سلامت
        var response = await client.GetAsync("/health");

        // نتیجه مورد انتظار: کد ۲۰۰ که یعنی کل خط لوله راه‌اندازی سالم است
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "نقطه سلامت باید بدون احراز هویت در دسترس باشد");
    }
}
