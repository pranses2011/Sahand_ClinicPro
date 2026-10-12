using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly InMemoryApiFactory _factory;

    /// <summary>
    /// سازنده تست — تزریق کارخانه مشترک بین تست‌های این کلاس.
    /// </summary>
    public HealthEndpointTests(InMemoryApiFactory factory)
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

    // ═══ چرخهٔ ۵۵ (BUG-20): نقاط سلامت استاندارد مانیتورینگ ═══

    /// <summary>
    /// Liveness فقط «فرآیند بالا است» را می‌گوید — هیچ وابستگی‌ای نمی‌سنجد؛
    /// پرابِ restart مانیتورینگ روی همین قرارداد تکیه می‌کند.
    /// </summary>
    [Fact]
    public async Task Health_Live_Should_Return_Ok_Without_DependencyChecks()
    {
        var json = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/live");

        json.GetProperty("status").GetString().Should().Be("Healthy");
        json.TryGetProperty("serverTime", out _).Should().BeTrue();
    }

    /// <summary>
    /// Readiness باید سه سنجش (دیتابیس/دیسک/کارها) را با وضعیت بدهد — در میزبان
    /// آزمایشی InMemory دیتابیس وصل است و سنجش‌های غیرقابل‌اعمال «سالم» گزارش می‌شوند.
    /// </summary>
    [Fact]
    public async Task Health_Ready_Should_Report_All_Checks_And_Stay_Serveable()
    {
        var response = await _factory.CreateClient().GetAsync("/health/ready");

        // تا وقتی همهٔ سنجش‌ها سالم‌اند ۲۰۰ — وگرنه ۵۰۳ (قرارداد readiness)
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "در میزبان آزمایشی همهٔ سنجش‌های ready باید سالم باشند");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("Healthy");
        json.GetProperty("checks").EnumerateObject().Select(c => c.Name)
            .Should().Contain(new[] { "database", "disk", "jobs" });
        json.GetProperty("checks").GetProperty("database").GetProperty("healthy").GetBoolean()
            .Should().BeTrue(because: "دیتابیس InMemory وصل است");
    }
}
