using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Sahand.ClinicPro.API.Filters;
using Hangfire;
using Sahand.ClinicPro.API.Middleware;
using Sahand.ClinicPro.Application;
using Sahand.ClinicPro.Infrastructure;
using Sahand.ClinicPro.Infrastructure.Identity;
using Serilog;

// ═══════════════════════════════════════════════════════════════════
// نقطه ورود API سامانه جامع مدیریت درمانگاه سهند (Sahand ClinicPro)
// گام‌های ۱ تا ۹: Serilog، Swagger، DI کامل، Identity، میدلورها، Rate Limit
// ═══════════════════════════════════════════════════════════════════

// لاگر بوت‌استرپ — قبل از ساخت Host فعال می‌شود تا خطاهای
// مرحله راه‌اندازی نیز ثبت شوند (الگوی two-stage logging سریلوگ)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("🚀 در حال راه‌اندازی API سامانه درمانگاه سهند");

    var builder = WebApplication.CreateBuilder(args);

    // ── سرویس‌ها ──

    // Serilog جایگزین لاگر پیش‌فرض؛ تنظیمات از appsettings.json خوانده می‌شود
    // چرخه دوازدهم (B1 — بند ۴-۱۳ سند تحلیل): سینک Seq فقط وقتی آدرس سرور
    // تنظیم شده باشد (متغیر محیطی SEQ_SERVER_URL یا همان appsettings) اضافه
    // می‌شود؛ استقرار بدون Seq کاملاً بی‌خطر است و فقط کنسول لاگ می‌گیرد
    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(context.Configuration);

        var seqUrl = context.Configuration["SEQ_SERVER_URL"];
        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            loggerConfiguration.WriteTo.Seq(seqUrl);
        }
    });

    // فشرده‌سازی پاسخ JSON
    builder.Services.AddResponseCompression(options =>
        options.MimeTypes = new[] { "application/json", "text/plain" });

    // کنترلرها با نام‌گذاری camelCase فارسی‌پسند
    builder.Services.AddControllers();

    // کش حافظه‌ای — کش یک‌ساعته کاتالوگ گزارش‌ها (چرخه دوازدهم B5)
    builder.Services.AddMemoryCache();

    // پاسخ استاندارد فارسی برای خطاهای ModelState — به‌جای ProblemDetails پیش‌فرض
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(ms => ms.Value is { Errors.Count: > 0 })
                .SelectMany(ms => ms.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                Sahand.ClinicPro.Application.Common.Models.ApiResponse<object>.Failure(
                    "اطلاعات ارسالی نامعتبر است", 400, errors));
        };
    });

    // کشف endpointها برای Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    // مستندسازی Swagger با عنوان فارسی و پشتیبانی JWT
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1.0.0",
            Title = "سامانه جامع مدیریت درمانگاه سهند — API",
            Description = "رابط برنامه‌نویسی درمانگاه سهند (Sahand ClinicPro) — " +
                          "نوبت‌دهی، ویزیت، نسخه الکترونیک، مالی و حسابداری دوبل، " +
                          "پاراکلینیک، داروخانه، پیامک و یکپارچه‌سازی با سامانه‌های دولتی",
            Contact = new OpenApiContact { Name = "تیم توسعه سهند کلینیک‌پرو" }
        });

        // تعریف امنیت Bearer برای آزمودن endpointهای محافظت‌شده
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "توکن دسترسی را اینجا وارد کنید (بدون کلمه Bearer)"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // لایه کاربرد: MediatR + رفتارها + نگاشتگر Mapperly + FluentValidation
    builder.Services.AddApplication();

    // لایه زیرساخت: DbContext + ریپازیتوری + سرویس‌ها + بذرها
    builder.Services.AddInfrastructure(builder.Configuration);

    // احراز هویت JWT Bearer و سرویس‌های امنیتی
    builder.Services.AddIdentityServices(builder.Configuration);

    // اتصال بلادرنگ SignalR — گام R1: صف شعبه‌ای + اعلان کاربر
    builder.Services.AddSignalR(options =>
    {
        // اندازه پیام امن — اعلان‌ها و رویدادهای صف سبک هستند
        options.MaximumReceiveMessageSize = 256 * 1024;
        // پینگ سریع‌تر برای تشخیص قطع کلاینت‌های WPF
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    });
    // پیاده‌سازی واقعی جایگزین پیش‌فرض بی‌اثر لایه کاربرد می‌شود
    builder.Services.AddScoped<Sahand.ClinicPro.Application.Interfaces.IRealtimeNotifier,
        Sahand.ClinicPro.API.Realtime.SignalRRealtimeNotifier>();

    // مجوزدهی مبتنی بر دسترسی پویا — سیاست‌های PERMISSION:x
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // ماژول حضور و غیاب — درایورهای دستگاه و همگام‌ساز
    builder.Services.AddSingleton<Sahand.ClinicPro.Infrastructure.Services.Attendance.ZktecoPullDriver>();
    builder.Services.AddSingleton<Sahand.ClinicPro.Infrastructure.Services.Attendance.HikvisionIsapiDriver>();
    builder.Services.AddSingleton<Sahand.ClinicPro.Infrastructure.Services.Attendance.SimulatedDeviceDriver>();
    builder.Services.AddSingleton<Sahand.ClinicPro.Domain.Interfaces.IAttendanceDriverFactory,
        Sahand.ClinicPro.Infrastructure.Services.Attendance.AttendanceDriverFactory>();
    builder.Services.AddScoped<Sahand.ClinicPro.Domain.Interfaces.IAttendanceSynchronizer,
        Sahand.ClinicPro.Application.Features.Attendance.AttendanceSynchronizer>();

    // محدودیت نرخ درخواست — ۱۰۰ در دقیقه عادی و ۵ در دقیقه برای ورود
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // سیاست عمومی همه endpointها
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // سیاست سخت‌گیرانه ورود — ۵ تلاش در دقیقه
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    var app = builder.Build();

    // ── خط لوله درخواست ──

    // مدیریت سراسری خطاها به پاسخ استاندارد فارسی
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // سرآیندهای امنیتی استاندارد وب
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // داشبورد Hangfire — فقط با فعال‌بودن کارهای پس‌زمینه
    if (app.Configuration.GetValue("Jobs:Enabled", false))
    {
        app.UseHangfireDashboard("/jobs", new Hangfire.DashboardOptions
        {
            Authorization = new[] { new Sahand.ClinicPro.API.HangfireDashboardAuthFilter() }
        });
    }

    // کارهای زمان‌بندی‌شده — فقط وقتی Hangfire فعال است
    if (app.Configuration.GetValue("Jobs:Enabled", false))
    {
        Hangfire.RecurringJob.AddOrUpdate<Sahand.ClinicPro.Infrastructure.Services.SMS.SmsQueueProcessor>(
            "sms-queue", processor => processor.ProcessQueueAsync(System.Threading.CancellationToken.None),
            Hangfire.Cron.Minutely());

        // تپ قلب روزانه لایسنس — ساعت ۳ بامداد (اعلان حضور به سرور لایسنس + مهلت آفلاین)
        Hangfire.RecurringJob.AddOrUpdate<Sahand.ClinicPro.Infrastructure.Services.License.LicenseHeartbeatJob>(
            "license-heartbeat", job => job.RunAsync(System.Threading.CancellationToken.None),
            Hangfire.Cron.Daily(3));

        // همگام‌سازی دوره‌ای دستگاه‌های حضور و غیاب — هر دقیقه سنجش موعد هر دستگاه
        Hangfire.RecurringJob.AddOrUpdate<Sahand.ClinicPro.Infrastructure.Services.Attendance.AttendanceSyncJob>(
            "attendance-sync", job => job.RunAsync(System.Threading.CancellationToken.None),
            Hangfire.Cron.Minutely());

        // پشتیبان‌گیری خودکار شبانه — ساعت ۲ بامداد + پاک‌سازی نسخه‌های فراتر از
        // سهمیه نگهداشت (Backup:RetentionCount) — چرخه دوازدهم (بند ۴-۳ سند تحلیل)
        Hangfire.RecurringJob.AddOrUpdate<Sahand.ClinicPro.Infrastructure.Services.Backup.BackupJob>(
            "backup-nightly", job => job.RunAsync(System.Threading.CancellationToken.None),
            Hangfire.Cron.Daily(2));
    }

    // Swagger فقط در محیط توسعه فعال است
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "درمانگاه سهند v1");
            options.DocumentTitle = "API درمانگاه سهند";
        });
    }

    app.UseHttpsRedirection();

    // فشرده‌سازی پاسخ — کاهش پهنای‌باند برای کلاینت WPF
    app.UseResponseCompression();

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // اعتبارسنجی لایسنس — پس از احراز هویت روی مسیرهای ماژول‌محور
    app.UseMiddleware<Sahand.ClinicPro.API.Middleware.BranchContextMiddleware>();
    app.UseMiddleware<LicenseValidationMiddleware>();

    // لاگ عملیات — همه نوشتن‌های API ثبت ردگیری می‌شوند
    app.UseMiddleware<AuditLogMiddleware>();

    app.MapControllers();

    // نقشه‌یابی Hubهای بلادرنگ — احراز هویت JWT از query string (گام R1)
    app.MapHub<Sahand.ClinicPro.API.Hubs.QueueHub>("/hubs/queue");
    app.MapHub<Sahand.ClinicPro.API.Hubs.NotificationHub>("/hubs/notifications");

    // بذرافشان داده‌های اولیه — نقش‌ها، دسترسی‌ها، ادمین، داده‌های پایه
    // در نبود دیتابیس (محیط تست) خطا ثبت و عبور می‌شود
    await app.Services.InitializeDatabaseAsync();

    // نقطه بررسی سلامت — کلاینت WPF و مانیتورینگ از این مسیر استفاده می‌کنند
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        message = "سامانه درمانگاه سهند فعال است",
        serverTime = DateTime.UtcNow,
        version = "1.0.0"
    }))
    .AllowAnonymous()
    .ExcludeFromDescription();

    Log.Information("✅ API درمانگاه سهند راه‌اندازی شد و در حال سرویس‌دهی است");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 خطای مهلک در راه‌اندازی API درمانگاه سهند");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// نشانگر کلاس Program برای تست‌های یکپارچگی WebApplicationFactory
/// </summary>
public partial class Program { }
