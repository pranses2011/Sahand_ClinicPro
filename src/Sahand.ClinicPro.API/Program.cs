using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Sahand.ClinicPro.API.Filters;
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
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    // کنترلرها با نام‌گذاری camelCase فارسی‌پسند
    builder.Services.AddControllers();

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

    // لایه کاربرد: MediatR + رفتارها + AutoMapper + FluentValidation
    builder.Services.AddApplication();

    // لایه زیرساخت: DbContext + ریپازیتوری + سرویس‌ها + بذرها
    builder.Services.AddInfrastructure(builder.Configuration);

    // احراز هویت JWT Bearer و سرویس‌های امنیتی
    builder.Services.AddIdentityServices(builder.Configuration);

    // مجوزدهی مبتنی بر دسترسی پویا — سیاست‌های PERMISSION:x
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

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
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
