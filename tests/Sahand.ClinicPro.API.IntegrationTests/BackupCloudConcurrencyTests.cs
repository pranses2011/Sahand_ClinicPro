using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// تست رگرسیون چرخهٔ ۵۰ (BUG-02) — حذف انسداد رشته‌ای ساخت سرویس پشتیبان ابری.
/// پیش‌تر نخِ پردازش درخواست، هنگام ساختِ نخستین سرویس ابری، به‌صورت هم‌گام
/// منتظرِ خواندن پیکربندی از دیتابیس می‌ماند (تماس مسدودکننده روی نتیجهٔ
/// عملیات ناهمگام) — زیر بار همزمان این الگو می‌توانست رشته‌ها را گره بزند و
/// اندپوینت‌های پشتیبان ابری را تا ابد معلق کند. الگوی async lazy با قفل
/// SemaphoreSlim (چرخهٔ ۵۰) این ریسک را صفر کرده و این تست تا ابد مانع بازگشت آن است.
/// </summary>
[Collection("Api")]
public class BackupCloudConcurrencyTests
{
    private readonly InMemoryApiFactory _factory;

    /// <summary>سازنده تست — تزریق کارخانهٔ مشترک میزبان آزمایشی</summary>
    public BackupCloudConcurrencyTests(InMemoryApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// ۵۰ درخواست همزمان به وضعیت پشتیبان ابری — همه باید «پاسخ» بگیرند؛
    /// هیچ‌کدام نباید به بن‌بست/تایم‌اوت منجر شود (رفع چرخهٔ ۵۰ باید سبز بماند).
    /// </summary>
    [Fact]
    public async Task Cloud_Endpoints_Under_50_Concurrent_Requests_Should_Not_Deadlock()
    {
        // آماده‌سازی: ورود با بذر ادمین پیش‌فرض (admin / Admin@123)
        var loginClient = _factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "بذر ادمین پیش‌فرض در محیط توسعه فعال است");
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("data").GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrEmpty(because: "ورود موفق باید توکن بدهد");

        // عملیات: شلیک هم‌زمان ۵۰ درخواست وضعیت ابری — پیش از چرخهٔ ۵۰ همین
        // بار، نخ‌ها را روی تماس مسدودکنندهٔ نخستین ساخت سرویس گره می‌زد.
        // تایم‌اوت تختِ ۲۰ ثانیه: بن‌بست واقعی هرگز پاسخ نمی‌داد و تست شکست می‌خورد.
        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(async _ =>
        {
            using var client = _factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/v1/backup/cloud/status");
            return response.StatusCode;
        }));

        // نتیجهٔ مورد انتظار: هر ۵۰ درخواست پاسخ کامل گرفته‌اند — نه ۴۰۱
        // (احراز هویت ادمین برقرار است) و نه تایم‌اوت/قطع (کلاس عملیات کامل شد).
        results.Should().HaveCount(50);
        results.Should().NotContain(HttpStatusCode.Unauthorized,
            because: "نقش Admin مجوز backup.view را دارد");
        results.Count(c => c == HttpStatusCode.OK).Should().Be(50,
            because: "اندپوینت وضعیت ابری با پیکربندی خالی هم پاسخ موفق می‌دهد");
    }
}
