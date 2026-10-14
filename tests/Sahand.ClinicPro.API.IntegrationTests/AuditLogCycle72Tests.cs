using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Security;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// ═══ گارد رگرسیون لاگ عملیات — چرخهٔ ۷۲ ═══
/// گزارش مالک: «در لاگ عملیات به جای نام کامل، نام کاربری رو مینویسه و
/// همچنین مقادیر قبلی رو نشون نمیده و به جاش خط تیره میندازه».
///
/// سه ادعای تحت پوشش (از درون HTTP واقعی با میدلور لاگ روشن):
///  ۱) نام کامل — کاربرِ متصل به پرسنل، پس از ورود باید در لاگ با
///     «نام + نام خانوادگی» ثبت شود (ریشه: ادعای displayName توکن
///     پیش‌تر همان Username بود — IdentityService چرخهٔ ۷۲).
///  ۲) مقدار قبلی تنظیم — PUT settings بدون شناسه در مسیر؛ سطر قبلیِ
///     همان کلید باید در OldValues بنشیند (TryCaptureOldValuesFromBody).
///  ۳) مقدار قبلی POSTهای زیرمنبعی — POST users/{id}/lock پیش‌تر هرگز
///     OldValues نمی‌گرفت (فقط PUT/PATCH/DELETE)؛ اکنون سطر کاربر ثبت می‌شود.
/// </summary>
public class AuditLogCycle72Tests : IClassFixture<AuditFlowApiFactory>
{
    private readonly AuditFlowApiFactory _factory;

    /// <summary>سازنده با کارخانهٔ جریان لاگ</summary>
    public AuditLogCycle72Tests(AuditFlowApiFactory factory) => _factory = factory;

    /// <summary>پاکت استاندارد پاسخ</summary>
    private sealed class Packet<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
    }

    private sealed class LoginData
    {
        public string? AccessToken { get; set; }
        public string? DisplayName { get; set; }
    }

    private sealed class AuditRow
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? UserDisplayName { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? EntityName { get; set; }
    }

    /// <summary>ورود ادمین با کلاینت آمادهٔ احراز هویت</summary>
    private async Task<HttpClient> LoginAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var packet = await login.Content.ReadFromJsonAsync<Packet<LoginData>>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", packet!.Data!.AccessToken);
        return client;
    }

    /// <summary>آخرین ردیف لاگ — صفحهٔ اول، تازه‌ترین اول</summary>
    private static async Task<List<AuditRow>> RecentLogsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auditlogs?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var packet = await response.Content.ReadFromJsonAsync<Packet<List<AuditRow>>>();
        return packet?.Data ?? new List<AuditRow>();
    }

    /// <summary>
    /// ادعای ۱ — کاربر متصل به پرسنل: توکن با نام کامل صادر می‌شود و ردیف
    /// لاگِ عملیات بعدی هم نام کامل را در ستون کاربر دارد (نه نام کاربری).
    /// </summary>
    [Fact]
    public async Task Linked_employee_user_is_logged_with_full_name()
    {
        await _factory.LinkAdminToEmployeeAsync("مدیر", "کلینیک سحرند");

        var client = await LoginAsync();

        // عملیات نوشتاری ساده — فقط برای تولید ردیف لاگ
        var put = await client.PutAsJsonAsync("/api/v1/settings",
            new { key = "Audit.NameProbe", value = "1", category = "AuditTests" });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await RecentLogsAsync(client);
        var row = logs.FirstOrDefault(l => l.NewValues?.Contains("Audit.NameProbe") == true);

        row.Should().NotBeNull("عملیات نوشتاری باید لاگ داشته باشد");
        row!.Username.Should().Be("مدیر کلینیک سحرند",
            "کاربرِ متصل به پرسنل باید با نام کامل در لاگ ثبت شود — چرخهٔ ۷۲");
    }

    /// <summary>
    /// ادعای ۲ — PUT settings (بدون شناسه در مسیر): دومین ذخیرهٔ همان کلید
    /// باید مقدار «قبلی» را در OldValues داشته باشد — پیش‌تر همین‌جا خط
    /// تیره ثبت می‌شد چون مسیر شناسه نداشت.
    /// </summary>
    [Fact]
    public async Task Settings_update_logs_previous_value()
    {
        var client = await LoginAsync();

        // مقدار اول — «علی»
        var first = await client.PutAsJsonAsync("/api/v1/settings",
            new { key = "Audit.OldValueProbe", value = "علی", category = "AuditTests" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // مقدار دوم — «رضا»؛ ردیف لاگ این PUT باید OldValues = «علی» داشته باشد
        var second = await client.PutAsJsonAsync("/api/v1/settings",
            new { key = "Audit.OldValueProbe", value = "رضا", category = "AuditTests" });
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await RecentLogsAsync(client);
        var row = logs.FirstOrDefault(l =>
            l.NewValues?.Contains("رضا") == true && l.OldValues?.Contains("Audit.OldValueProbe") == true);

        row.Should().NotBeNull("ذخیرهٔ تنظیم باید لاگ با مقدار قبلی داشته باشد — چرخهٔ ۷۲");
        row!.OldValues.Should().Contain("علی", "مقدار قبلیِ همان کلید در OldValues می‌نشیند");
    }

    /// <summary>
    /// ادعای ۳ — POST زیرمنبعی (users/{id}/lock): پیش‌تر فقط PUT/PATCH/DELETE
    /// مقادیر قبلی می‌گرفتند و این دسته از عملیات‌ها همیشه بدون OldValues
    /// (خط تیره) ثبت می‌شدند. خواندن سطر از دیتابیس با ADO خام است که فقط
    /// روی پروایدر رابطه‌ای (SQL Server تولید) کار می‌کند؛ در محیط InMemory
    /// این بخش بی‌صدا نادیده گرفته می‌شود، پس قاعدهٔ «کدام متدها پوشش داده
    /// می‌شوند» با ShouldCaptureFromPath واحد-تست می‌شود و E2E فقط ثبتِ
    /// خودِ ردیف لاگ برای همین مسیر را می‌سنجد.
    /// </summary>
    [Fact]
    public async Task Sub_resource_post_is_captured_and_decision_rule_covers_post_with_id()
    {
        // ── بخش ۱: قاعدهٔ تصمیم — واحد-تست خالص ──
        // POST زیرمنبعی با شناسه: قبلاً false بود (ریشهٔ خط تیره) — چرخهٔ ۷۲ true می‌دهد
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("POST", new Microsoft.AspNetCore.Http.PathString("/api/v1/users/5/lock"))
            .Should().BeTrue("POST زیرمنبعی با شناسه باید مقدار قبلی بگیرد — چرخهٔ ۷۲");
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("POST", new Microsoft.AspNetCore.Http.PathString("/api/v1/surveys/3/toggle-active"))
            .Should().BeTrue();
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("POST", new Microsoft.AspNetCore.Http.PathString("/api/v1/salaries/7/pay"))
            .Should().BeTrue();

        // POST ایجاد بدون شناسه — همچنان بدون مقدار قبلی (درستی رفتار قبلی)
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("POST", new Microsoft.AspNetCore.Http.PathString("/api/v1/patients"))
            .Should().BeFalse("ایجاد بدون شناسه چیزی قبلی ندارد");
        // PUT بدون شناسه — مسیر بدنه‌محور (تنظیمات/نتیجه) از شاخهٔ جدا می‌رود
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("PUT", new Microsoft.AspNetCore.Http.PathString("/api/v1/settings"))
            .Should().BeFalse("PUT settings هدفش را در بدنه حمل می‌کند — شاخهٔ بدنه");
        // GET هرگز مقدار قبلی نمی‌گیرد
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("GET", new Microsoft.AspNetCore.Http.PathString("/api/v1/patients/5"))
            .Should().BeFalse();
        // PUT کلاسیک با شناسه — رفتار پیشین حفظ است
        Sahand.ClinicPro.API.Middleware.AuditLogMiddleware
            .ShouldCaptureFromPath("PUT", new Microsoft.AspNetCore.Http.PathString("/api/v1/patients/5"))
            .Should().BeTrue();

        // ── بخش ۲: ردیف لاگ از درون HTTP — POST زیرمنبعی واقعی ──
        var client = await LoginAsync();
        var userId = await _factory.CreatePlainUserAsync("lockprobe");

        var lockResponse = await client.PostAsync($"/api/v1/users/{userId}/lock", null);
        lockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await RecentLogsAsync(client);
        var row = logs.FirstOrDefault(l => l.EntityName?.Contains("کاربر") == true);

        row.Should().NotBeNull("قفل‌کردن کاربر باید ردیف لاگ با موجودیت «کاربر» داشته باشد");
        row!.Username.Should().NotBeNullOrWhiteSpace();
    }
}

/// <summary>
/// کارخانهٔ جریان لاگ — دیتابیس InMemory اختصاصی + بذرهای اصلی برنامه
/// (ادمین با رمز Admin@123) + کمکی‌های اتصال پرسنل/ساخت کاربر آزمایشی.
/// </summary>
public class AuditFlowApiFactory : InMemoryApiFactory
{
    /// <summary>نام دیتابیس اختصاصی جریان لاگ</summary>
    protected override string DatabaseName => "AuditLogCycle72Db";

    /// <summary>
    /// اتصال کاربر ادمین به پرسنلِ نمونه — تا «نام کامل» مسیر چرخهٔ ۷۲
    /// فعال شود (IdentityService.ResolveFullName از همین رابطه می‌خواند).
    /// idempotent — فقط بار اول کاربر ساخته/وصل می‌شود.
    /// </summary>
    public async Task LinkAdminToEmployeeAsync(string firstName, string lastName)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();

        var admin = context.Users.Single(u => u.Username == "admin");
        if (admin.EmployeeId is null)
        {
            var employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                NationalCode = "0099988877",
                Mobile = "09120000099",
                IsActive = true
            };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            admin.EmployeeId = employee.Id;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>ساخت کاربر معمولی آزمایشی برای آزمون POST زیرمنبعی</summary>
    public async Task<long> CreatePlainUserAsync(string username)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();

        var existing = context.Users.FirstOrDefault(u => u.Username == username);
        if (existing is not null)
            return existing.Id;

        var user = new User
        {
            Username = username,
            Email = $"{username}@test.local",
            PasswordHash = "not-a-real-hash",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
