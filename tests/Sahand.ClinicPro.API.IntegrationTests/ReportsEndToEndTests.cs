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

        // ═══ بازهٔ نمودار داشبورد باید تعداد روز انتخاب‌شده را برگرداند ═══
        foreach (var days in new[] { 14, 30 })
        {
            var dashboardChart = await client.GetAsync($"/api/v1/dashboard/revenue-chart?days={days}");
            dashboardChart.StatusCode.Should().Be(HttpStatusCode.OK);
            var chartPacket = await ReadAsync<Packet<JsonElement>>(dashboardChart);
            chartPacket.Data!.GetArrayLength().Should().Be(days,
                $"انتخاب {days} روز باید دقیقاً {days} نقطهٔ روزانه در نمودار بسازد");
            chartPacket.Data[days - 1].GetProperty("amount").GetDecimal().Should().Be(300000m,
                "درآمد روز باید بر اساس تاریخ پرداخت واقعی باشد، نه تاریخ صدور فاکتور");
        }

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
        AssertRowsMatchColumns(rev, "گزارش درآمد روزانه");

        // ═══ گزارش‌های مالی باید واقعاً خروجی متفاوت و هم‌اندازهٔ ستون‌های خود داشته باشند ═══
        foreach (var reportId in new[] { "monthly-revenue", "detailed-revenue", "revenue-trend" })
        {
            var response = await client.GetAsync($"/api/v1/reports/execute/{reportId}?fromDate=2000-01-01&toDate=2100-01-01");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var packet = await ReadAsync<Packet<JsonElement>>(response);
            AssertRowsMatchColumns(packet.Data!, $"گزارش {reportId}");
            packet.Data.GetProperty("rows").GetArrayLength().Should().BeGreaterThan(0,
                $"فاکتور بذری باید در گزارش «{reportId}» دیده شود");
        }

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

        // گزارش «۱۲ ماه اخیر» باید تمام ماه‌ها (حتی ماه‌های بدون درآمد) را
        // برگرداند، نه فقط یک ماه از بازهٔ پیش‌فرض ۳۰روزهٔ صفحه.
        var monthlyChartResponse = await client.GetAsync(
            "/api/v1/reports/execute/chart-revenue-monthly?fromDate=2000-01-01&toDate=2100-01-01");
        monthlyChartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var monthlyChart = await ReadAsync<Packet<JsonElement>>(monthlyChartResponse);
        monthlyChart.Data!.GetProperty("chart").GetProperty("points").GetArrayLength()
            .Should().Be(12, "گزارش ۱۲ ماهه باید برای هر ماه یک نقطه داشته باشد");
        AssertRowsMatchColumns(monthlyChart.Data, "گزارش نموداری درآمد ماهانه");
    }

    /// <summary>
    /// ═══ چرخهٔ ۷۲ — رگرسیون «گرهِ نیمه‌شبِ تا تاریخ» (ریشهٔ اصلی «همه‌چیز صفر») ═══
    /// گزارش مالک: «هیچکدام از گزارش‌ها کار نمیکنند و برای همه چیز صفر می‌نویسن».
    /// ریشه: کلاینت «تا تاریخ» را yyyy-MM-dd می‌فرستاد؛ سرور آن را ۰۰:۰۰ همان
    /// روز می‌پنداشت و کوئری‌های «CreatedAt کوچک‌تر-مساوی to» کل روزِ پایانی را حذف می‌کردند.
    /// انتخاب «از = تا = امروزِ دادهٔ تازه» یعنی بازهٔ عملاً تهی ⇒ همه‌چیز صفر؛
    /// حتی وقتی فاکتور همین امروز صادر شده بود. این تست دقیقاً همان سناریوی
    /// مالک را می‌سازد: فاکتور همین حالا صادر می‌شود و گزارش با بازهٔ
    /// «امروزِ ایران» (تک‌روزِ همین فاکتور) باید آن را ببیند.
    /// تست‌ چرخهٔ ۷۰ با بازهٔ عریض ۲۰۰۰..۲۱۰۰ این حالت را پوشش نمی‌داد.
    /// </summary>
    [Fact]
    public async Task Same_Day_Range_Should_Not_Drop_Todays_Data()
    {
        await _factory.SeedFlowDataAsync();
        var client = _factory.CreateClient();

        // ── ورود ادمین بذری ──
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await ReadAsync<Packet<LoginData>>(loginResponse);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Data!.AccessToken);

        // ── بیمار و فاکتورِ همین لحظه ──
        var createPatient = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "نیمه‌شب",
            lastName = "آزمایی",
            nationalCode = "0061457981", // چک‌سام معتبر — 0061457992 ردِ اعتبارسنجی کد ملی (400)
            mobile = "09121234597",
            gender = 2,
            birthDateShamsi = "1370/05/15"
        });
        createPatient.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await ReadAsync<Packet<JsonElement>>(createPatient);
        var patientId = patient.Data!.ValueKind == JsonValueKind.Number
            ? patient.Data.GetInt64()
            : patient.Data.GetProperty("id").GetInt64();

        var createInvoice = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            patientId,
            type = 1,
            taxPercent = 0m,
            items = new[]
            {
                new { description = "حق ویزیت نیمه‌شب", quantity = 1m, unitPrice = 250000m, discountPercent = 0m }
            }
        });
        createInvoice.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "فاکتور معتبر برای سناریوی بازهٔ تک‌روز صادر می‌شود");

        // ── «امروز» به وقت ایران (تقویم کاربر) — نه نیمه‌شب UTC ──
        var todayIran = (DateTime.UtcNow + TimeSpan.FromHours(3.5)).Date;
        var range = $"fromDate={todayIran:yyyy-MM-dd}&toDate={todayIran:yyyy-MM-dd}";

        // ── گزارش درآمد با بازهٔ تک‌روزِ همین فاکتور ──
        var revenue = await client.GetAsync($"/api/v1/reports/execute/daily-revenue?{range}");
        revenue.StatusCode.Should().Be(HttpStatusCode.OK);
        var revenueBody = await ReadAsync<Packet<JsonElement>>(revenue);
        var rev = revenueBody.Data!;
        rev.GetProperty("available").GetBoolean().Should().BeTrue();

        var total = rev.GetProperty("summary").GetProperty("total").GetDecimal();
        total.Should().BeGreaterThan(0m,
            because: "فاکتور همین امروز صادر شده است؛ «از = تا = امروز» نباید کل روز پایانی را " +
                     "با پنداشتن نیمه‌شبِ آن حذف کند و همه‌چیز صفر برگرداند — " +
                     "این دقیقاً ریشهٔ گزارش «همه گزارش‌ها صفر می‌نویسن» بود");

        // ردیف روزانهٔ امروزِ ایران هم باید موجود باشد
        rev.GetProperty("rows").GetArrayLength().Should().BeGreaterThan(0,
            because: "بازهٔ تک‌روزِ شامل فاکتور تازه باید دست‌کم یک ردیف روزانه بسازد");
    }

    /// <summary>هر ردیف JSON باید دقیقاً یک مقدار به‌ازای هر سرستون داشته باشد</summary>
    private static void AssertRowsMatchColumns(JsonElement data, string reportName)
    {
        var columnCount = data.GetProperty("columns").GetArrayLength();
        foreach (var row in data.GetProperty("rows").EnumerateArray())
            row.EnumerateObject().Count().Should().Be(columnCount,
                $"{reportName} نباید ستون‌های خالی یا مقادیر جابه‌جا داشته باشد");
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
