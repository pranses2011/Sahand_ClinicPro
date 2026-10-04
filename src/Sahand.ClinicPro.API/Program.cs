using Microsoft.OpenApi.Models;
using Sahand.ClinicPro.Application;
using Sahand.ClinicPro.Infrastructure;
using Serilog;

// ═══════════════════════════════════════════════════════════════════
// نقطه ورود API سامانه جامع مدیریت درمانگاه سهند (Sahand ClinicPro)
// گام ۱: راه‌اندازی اولیه با Serilog، Swagger و اسکلت DI
// کنترلرها و میدلورهای کامل از گام ۹ به بعد اضافه می‌شوند.
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

    // کنترلرها — ۲۴ کنترلر از گام ۹ به بعد اضافه می‌شوند
    builder.Services.AddControllers();

    // کشف endpoint ها برای Swagger
    builder.Services.AddEndpointsApiExplorer();

    // مستندسازی Swagger با عنوان فارسی (نسخه مسیرها: api/v1)
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
    });

    // لایه کاربرد: MediatR + AutoMapper + FluentValidation
    builder.Services.AddApplication();

    // لایه زیرساخت: DbContext + ریپازیتوری + سرویس‌ها (گام ۳ و ۴)
    builder.Services.AddInfrastructure(builder.Configuration);

    // 📌 گام ۴: احراز هویت JWT + Refresh Token
    // 📌 گام ۹: میدلورها (خطا، لایسنس، لاگ عملیات، محدودیت درخواست) + SignalR

    var app = builder.Build();

    // ── خط لوله درخواست ──

    // Swagger فقط در محیط توسعه فعال است (تا گام ۳۱ که بسته‌بندی نهایی می‌شود)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "درمانگاه سهند v1");
            options.DocumentTitle = "API درمانگاه سهند";
        });
    }

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
    }));

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // خطاهای مهلک مرحله راه‌اندازی
    Log.Fatal(ex, "❌ خاتمه ناگهانی API درمانگاه سهند");
}
finally
{
    // تخلیه بافرهای لاگ قبل از خروج
    Log.CloseAndFlush();
}

/// <summary>
/// اعلام عمومی کلاس Program برای دسترسی تست‌های یکپارچگی
/// از طریق WebApplicationFactory (الگوی استاندارد Minimal Hosting).
/// </summary>
public partial class Program { }
