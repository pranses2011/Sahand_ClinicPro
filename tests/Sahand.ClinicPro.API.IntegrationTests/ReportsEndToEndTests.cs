using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// ═══ تست سرتاسری گزارش‌ها — چرخهٔ ۷۰ (گزارش مالک) ═══
/// «هیچ‌کدام از گزارش‌ها کار نمی‌کنند و برای همه چیز صفر (۰) می‌نویسن و
/// کارت نمودار را خالی نشان می‌دهند، چه جدولی و چه نموداری».
/// این تست جریان واقعی را می‌سازد (بیمار → فاکتور → پرداخت) و سپس
/// گزارش‌های «جدولی» و «نموداری» را از درون HTTP اجرا می‌کند و
/// تأیید می‌کند که: ۱) خلاصه‌ها غیرصفرند ۲) بلاک نمودار نقاط دارد
/// ۳) ردیف‌های جدول خالی نیستند. اگر مالک باز هم صفر ببیند، علت
/// بیرون از سرور است (کلاینت/دیتای ایستگاه) و در همین متن مستند می‌شود.
/// </summary>
public class ReportsEndToEndTests : IClassFixture<FlowApiFactory>
{
    private readonly FlowApiFactory _factory;

    /// <summary>سازنده با کارخانهٔ جریان</summary>
    public ReportsEndToEndTests(FlowApiFactory factory) => _factory = factory;

    /// <summary>پاکت استاندارد پاسخ — خواندن data</summary>
    private sealed class Packet<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
    }

    private sealed class LoginData
    {
        public string? AccessToken { get; set; }
    }

    /// <summary>
    /// جریان کامل: ورود ادمین → بیمار → فاکتور ۵۰۰٬۰۰۰ ریالی → پرداخت ۳۰۰٬۰۰۰
    /// سپس اجرای سه گزارش (جدولی: درآمد روزانه — جدولی: روش‌های پرداخت —
    /// نموداری: روند درآمد) و پالایش خروجی.
    /// </summary>
    [Fact]
    public async Task Seeded_Invoice_And_Payment_Should_Produce_NonZero_Reports()
    {
        var doctorId = await _factory.SeedFlowDataAsync();
        var client = _factory.CreateClient();

        // ── ورود ادمین بذری ──
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await ReadAsync<Packet<LoginData>>(loginResponse);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Data!.AccessToken);

        // ── بیمار ──
        var createPatient = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "گزارش",
            lastName = "آزمایی",
            nationalCode = "0061457991",
            mobile = "09121234599",
            gender = 2,
            birthDateShamsi = "1370/05/15"
        });
        createPatient.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await ReadAsync<Packet<JsonElement>>(createPatient);
        // پاسخ ایجاد بیمار «خود شناسه» است (عدد) یا شیء با id — هر دو پذیرفته می‌شود
        var patientId = patient.Data!.ValueKind == JsonValueKind.Number
            ? patient.Data.GetInt64()
            : patient.Data.GetProperty("id").GetInt64();

        // ── فاکتور ۵۰۰٬۰۰۰ ریالی ──
        var createInvoice = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            patientId,
            type = 1,
            taxPercent = 0m,
            items = new[]
            {
                new { description = "حق ویزیت", quantity = 1m, unitPrice = 500000m, discountPercent = 0m }
            }
        });
        createInvoice.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "فاکتور با قلم معتبر صادر می‌شود");
        var invoicePacket = await ReadAsync<Packet<JsonElement>>(createInvoice);
        var invoiceId = invoicePacket.Data!.ValueKind == JsonValueKind.Number
            ? invoicePacket.Data.GetInt64()
            : invoicePacket.Data.GetProperty("id").GetInt64();

        // ── پرداخت ۳۰۰٬۰۰۰ ریالی نقدی ──
        var pay = await client.PostAsJsonAsync("/api/v1/payments", new
        {
            invoiceId,
            patientId,
            amount = 300000m,
            method = 1,
            referenceNumber = "TRX-100"
        });
        pay.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "پرداخت معتبر باید ثبت شود");

        // ═══ گزارش ۱ (جدولی): درآمد روزانه — خلاصه باید غیرصفر باشد ═══
        var revenue = await client.GetAsync("/api/v1/reports/execute/daily-revenue?fromDate=2000-01-01&toDate=2100-01-01");
        revenue.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "با ماژول Reports فعال، اجرای گزارش نباید خطا دهد");
        var revenueBody = await ReadAsync<Packet<JsonElement>>(revenue);
        var rev = revenueBody.Data!;
        rev.GetProperty("available").GetBoolean().Should().BeTrue("گزارش درآمد باید در دسترس باشد");

        var revSummary = rev.GetProperty("summary");
        var total = revSummary.GetProperty("total").GetDecimal();
        total.Should().Be(500000m, because: "فاکتور ۵۰۰٬۰۰۰ ریالی در بازه است و جمع درآمد همان می‌شود");
        revSummary.GetProperty("paid").GetDecimal().Should().Be(300000m, "پرداخت ۳۰۰٬۰۰۰ ثبت شد");
        rev.GetProperty("rows").GetArrayLength().Should().BeGreaterThan(0, "ردیف روزانه باید موجود باشد");

        // ═══ گزارش ۲ (جدولی): روش‌های پرداخت — نقدی باید ۳۰۰٬۰۰۰ بدهد ═══
        var methods = await client.GetAsync("/api/v1/reports/execute/payment-methods?fromDate=2000-01-01&toDate=2100-01-01");
        methods.StatusCode.Should().Be(HttpStatusCode.OK);
        var methodsBody = await ReadAsync<Packet<JsonElement>>(methods);
        var mth = methodsBody.Data!;
        // تفکیک روش‌ها در «ردیف‌ها»ست (روش/تعداد/مبلغ/سهم) — نه در خلاصه؛
        // ردیفِ «نقدی» باید مبلغ ۳۰۰٬۰۰۰ را نشان دهد
        var cashRows = mth.GetProperty("rows").EnumerateArray()
            .Where(r => r.TryGetProperty("method", out var mName) && mName.GetString()!.Contains("نقدی"))
            .ToList();
        cashRows.Should().HaveCount(1, because: "یک پرداخت نقدی در تفکیک روش‌ها باید دقیقاً یک ردیف بسازد");
        cashRows[0].GetProperty("amount").GetDecimal().Should().Be(300000m,
            because: "پرداخت نقدی ۳۰۰٬۰۰۰ در تفکیک روش‌ها باید خودش را نشان دهد");

        // ═══ گزارش ۳ (نموداری): روند درآمد — نقاط نمودار باید غیرخالی باشد ═══
        var chart = await client.GetAsync("/api/v1/reports/execute/chart-revenue-trend?fromDate=2000-01-01&toDate=2100-01-01");
        chart.StatusCode.Should().Be(HttpStatusCode.OK);
        var chartBody = await ReadAsync<Packet<JsonElement>>(chart);
        var ch = chartBody.Data!;
        ch.GetProperty("available").GetBoolean().Should().BeTrue("گزارش نموداری باید در دسترس باشد");
        ch.TryGetProperty("chart", out var chartBlock).Should().BeTrue("بلاک نمودار باید در پاسخ باشد");
        chartBlock.GetProperty("points").GetArrayLength().Should().BeGreaterThan(0,
            because: "نقاط نمودار باید پر باشند — «کارت نمودار خالی» یعنی این آرایه تهی است");
        var firstPoint = chartBlock.GetProperty("points")[0];
        firstPoint.GetProperty("value").GetDouble().Should().Be(500000.0,
            because: "ارزش نقطهٔ درآمد همان جمع فاکتور است");
    }

    /// <summary>خواندن پاسخ و نگاشت به پاکت</summary>
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("پاسخ قابل نگاشت نبود");
    }
}
