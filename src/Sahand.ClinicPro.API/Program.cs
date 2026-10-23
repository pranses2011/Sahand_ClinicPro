using System.Threading.RateLimiting;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.API.Health;
using Hangfire;
using Sahand.ClinicPro.API.Middleware;
using Sahand.ClinicPro.Application;
using Sahand.ClinicPro.Infrastructure;
using Sahand.ClinicPro.Infrastructure.Identity;
using Serilog;

// ═══ چرخهٔ سی‌امین — نمایش فارسی صحیح در کنسول ویندوز (conhost) ═══
// سابقه: چرخهٔ ۲۶ کدپیج UTF-8 را ثابت کرد (نویسه‌ها سالم چاپ می‌شوند) و
// چرخهٔ ۲۸ فونت TrueType «Courier New» را روی کنسول گذاشت تا گلیف‌های
// عربی موجود باشند؛ اما conhost هرگز متن را «شکل‌دهی» نمی‌کند (نه اتصال
// حروف، نه چینش دوجهته) — پس کلمهٔ «راه‌اندازی» همچنان با حروف جدا از
// هم و وارونه دیده می‌شد. راه‌حل قطعی همین چرخه: قالب‌بند
// PersianConsoleTextFormatter (متصب روی سینک کنسول در appsettings.json)
// که فقط برای نمایشِ کنسول، متن را به «اشکال نمایشی متصل + چینش بصری
// راست‌به‌چپ» پیش‌پردازش می‌کند (موتور RtlConsoleShaper)؛ فونت Courier
// New به‌عنوان تنها فونت کنسول ویندوز با پوشش اشکال نمایشی عربی/فارسی
// حفظ می‌شود. در ترمینال‌های مدرن (Windows Terminal/VS Code) و خروجی
// ریدایرکت‌شده متن خام منطقی چاپ می‌شود و متن دوبار معکوس نمی‌شود؛
// راه مطالعهٔ مطمئن لاگ: فایل‌های UTF-8 روزانه در پوشهٔ Logs کنار API.
// پیاده‌سازی P/Invoke تنظیم فونت در «partial class Program» انتهای همین فایل است.

// نقطه ورود API سامانه جامع مدیریت درمانگاه سهند (Sahand ClinicPro)
// گام‌های ۱ تا ۹: Serilog، Swagger، DI کامل، Identity، میدلورها، Rate Limit
// ═══════════════════════════════════════════════════════════════════

// لاگر بوت‌استرپ — قبل از ساخت Host فعال می‌شود تا خطاهای
// مرحله راه‌اندازی نیز ثبت شوند (الگوی two-stage logging سریلوگ)
// چرخهٔ سی‌امین: همین قالب‌بند فارسی تا پیام‌های آغازین هم در conhost
// خوانا باشند؛ تشخیص محیط داخل خود قالب‌بند است
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Sahand.ClinicPro.API.Logging.PersianConsoleTextFormatter())
    .CreateBootstrapLogger();

// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — نمایش فارسی در لاگ کنسول API ═══
// پیام‌های فارسی سریلوگ روی کنسول ویندوز به‌صورت «???? ????» دیده می‌شدند؛
// ریشه: کدپیج پیش‌فرض کنسول (مثلاً CP437/CP850) نویسه‌های فارسی را به «?»
// ترجمه می‌کند. با UTF-8 قرار دادن کدپیجِ خروجی، متن فارسی سالم چاپ می‌شود.
// در حالت‌های بدون کنسول متصل (اجرا به‌عنوان سرویس یا ریدایرکت خروجی)
// تنظیم کدپیج IOException می‌دهد — بی‌خطر در try/catch بلعیده می‌شود.
try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    // چرخهٔ ۲۸ — فونت TrueType شکل‌دهنده (حروف متصل) پس از کدپیج UTF-8
    TrySetPersianCapableConsoleFont();
}
catch
{
    // بدون کنسول متصل — مسیر عادی سرویس/CI؛ خروجی سریلوگ ریدایرکت می‌شود
    // و کدپیج موضوعیت ندارد
}

try
{
    Log.Information("🚀 در حال راه‌اندازی API سامانه درمانگاه سهند");

    var builder = WebApplication.CreateBuilder(args);

    // پشتیبانی رسمی از Service Control Manager ویندوز؛ بدون این میزبان،
    // نصب با sc.exe بعد از مهلت کوتاه با خطای 1053 متوقف می‌شد.
    builder.Host.UseWindowsService(options =>
        options.ServiceName = "SahandClinicPro.API");

    // ── سرویس‌ها ──

    // Serilog جایگزین لاگر پیش‌فرض؛ تنظیمات از appsettings.json خوانده می‌شود
    // چرخه دوازدهم (B1 — بند ۴-۱۳ سند تحلیل): سینک Seq فقط وقتی آدرس سرور
    // تنظیم شده باشد (متغیر محیطی SEQ_SERVER_URL یا همان appsettings) اضافه
    // می‌شود؛ استقرار بدون Seq کاملاً بی‌خطر است و فقط کنسول لاگ می‌گیرد
    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(context.Configuration);

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — سینک فایل با کدگذاری UTF-8 ═══
        // حتی با اصلاح کدپیج کنسول، فونت‌های کنسول ویندوز ممکن است نویسه‌های
        // فارسی را کامل رندر نکنند؛ برای مطالعهٔ مطمئنِ لاگ‌ها (به‌ویژه برای
        // پشتیبانی) سینک فایل روزانهٔ UTF-8 افزوده شد: Logs/api-YYYYMMDD.log
        // با نگهداشت ۶۱ روز. shared اجازهٔ هم‌زمان‌نویسی چند پردازه را می‌دهد
        // و long-running پردازهٔ API هر ۲ ثانیه خروجی را روی دیسک فلاش می‌کند.
        loggerConfiguration.WriteTo.File(
            path: Path.Combine(AppContext.BaseDirectory, "Logs", "api-.log"),
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: Serilog.RollingInterval.Day,
            retainedFileCountLimit: 61,
            encoding: System.Text.Encoding.UTF8,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(2));

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
    // ═══ چرخهٔ ۶۷ (گزارش مالک: خطای «اطلاعات ارسالی نامعتبر است» ثبت شیفت) ═══
    // مبدل‌های سهل‌گیر TimeOnly/DateOnly: پارس بومی TimeOnly در net8.0 فقط
    // «HH:mm:ss» را می‌پذیرد و فرمت کوتاه «HH:mm» با JsonException رد می‌شد؛
    // با این مبدل‌ها هر دو قالب (و کسر ثانیهٔ کامل) پذیرفته می‌شوند تا
    // کلاینت‌های قدیمی هم بدون به‌روزرسانی اجباری کار کنند.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new Sahand.ClinicPro.API.Common.Json.TimeOnlyLenientJsonConverter());
            options.JsonSerializerOptions.Converters.Add(
                new Sahand.ClinicPro.API.Common.Json.DateOnlyLenientJsonConverter());
        });

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

    // ═══ چرخهٔ ۶۱ (P-05) — کش دومرحله‌ای اختیاری دادهٔ مرجع ═══
    // پیش‌فرض خاموش است (استقرار تک‌نمونه‌ای درمانگاه — L1 کفایت می‌کند).
    // با «Cache:L2:Enabled=true» برای استقرار چندنمونه‌ای، IDistributedCache
    // ثبت و IReferenceCache با نسخهٔ دومرحله‌ای جایگزین می‌شود؛ Provider=Redis
    // در آینده فقط با ثبت AddStackExchangeRedisCache (قبل از این نقطه) فعال
    // می‌شود — این بلوک هیچ تغییری نمی‌خواهد.
    if (builder.Configuration.GetValue<bool>("Cache:L2:Enabled"))
    {
        // اگر میزبان IDistributedCache ثبت نکرده باشد، پشتیبان حافظه‌ای درون‌برنامه‌ای
        // ثبت می‌شود (برای چند میزبان .NET جدا روی یک سرور مشترک هم مفید است)
        builder.Services.AddDistributedMemoryCache();
        var l1Ttl = TimeSpan.FromSeconds(builder.Configuration.GetValue("Cache:L2:L1TtlSeconds", 90));
        var l2Ttl = TimeSpan.FromSeconds(builder.Configuration.GetValue("Cache:L2:L2TtlSeconds", 600));
        builder.Services.AddSingleton<Sahand.ClinicPro.Application.Common.Caching.IReferenceCache>(sp =>
            new Sahand.ClinicPro.Application.Common.Caching.TwoLevelReferenceCache(
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
                l1Ttl, l2Ttl));
    }

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
        // چرخه بیست‌وسوم (گزارش شانزدهم مالک): «بعضاً خطای 429 می‌دهد» — سقف
        // پیشین ۱۰۰ درخواست در دقیقه برای کلاینت دسکتاپِ فعال (نظرسنجی داشبورد،
        // بروزرسانی زندهٔ صف، ذخیرهٔ تنظیمات با Debounce، بارگذاری فرم‌ها) به‌سادگی
        // پُر می‌شد و خطای 429 به کاربر می‌رسید؛ این کلاینتِ اختصاصیِ درمانگاه است
        // نه API عمومی — سقف به ۶۰۰ در دقیقه (۱۰ در ثانیه) افزایش یافت. سیاست
        // سخت‌گیرانهٔ ورود (۵ تلاش در دقیقه) دست‌نخورده ماند.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 600,
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

        // ═══ چرخهٔ ۴۹ (BUG-01 / مرور AllowAnonymous): سیاست لایسنس ═══
        // اندپوینت‌های فعال‌سازی/تنظیم لایسنس به‌ناچار پیش از ورود عمومی‌اند
        // (پنجرهٔ فعال‌سازی دسکتاپ، پیش از وجود توکن)؛ برای مقاوم‌سازی در برابر
        // سوءاستفاده — به‌ویژه PUT settings که نشانی سرور لایسنس را تغییر می‌دهد —
        // سقف ۲۰ درخواست در دقیقه از هر IP اعمال می‌شود (کلاینت واقعی در دقایق
        // فعال‌سازی حداکثر ۲-۳ فراخوان دارد؛ فراتر از آن نشانهٔ سوءاستفاده است).
        options.AddPolicy("license", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    var app = builder.Build();

    // ── خط لوله درخواست ──

    // ═══ چرخهٔ سی‌وهشتم (گزارش مالک: «خروجی لاگ سرور API رنگ‌بندی و قالب‌بندی
    // شده باشد») ═══ لاگ ساخت‌یافتهٔ پایان هر درخواست: رویداد تک‌خطی با
    // مشخصه‌های RequestMethod/Path/StatusCode/Elapsed — قالب‌بند فارسی کنسول
    // همین رویدادها را به شکل خوانای «← GET /api/... → ۲۰۰ · ۴۵ms» با رنگ
    // کد وضعیت رندر می‌کند؛ پیش از این خطایی از این دسته در لاگ نبود.
    app.UseSerilogRequestLogging();

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

        // یادآوری خودکار نوبت — هر ساعت، دو موج پیامک ۲۴ و ۲ ساعت قبل از نوبت
        // با رعایت ساعات سکوت (پیش‌فرض ۲۲:۰۰-۰۷:۰۰) — چرخهٔ ۶۹ (فاز ۳ / F-03)؛
        // فعال/غیرفعال و ساعات سکوت از SystemSetting بدون ری‌استارت قابل تغییر است
        Hangfire.RecurringJob.AddOrUpdate<Sahand.ClinicPro.Infrastructure.Services.Scheduling.AppointmentReminderJob>(
            "appointment-reminders", job => job.RunAsync(System.Threading.CancellationToken.None),
            Hangfire.Cron.Hourly());
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

    // ═══ چرخهٔ سی‌امین — هشدار «Failed to determine the https port for
    // redirect» ═══
    // استقرار درمانگاه روی HTTP شبکهٔ محلی اجرا می‌شود (کلاینت WPF مستقیم
    // به http وصل است) و هیچ نشانی HTTPS‌ای به برنامه داده نشده است؛ در
    // این حالت میدلور ریدایرکت در هر درخواست فقط همین هشدار را می‌نوشت و
    // هیچ کاری انجام نمی‌داد. اکنون ریدایرکت فقط وقتی فعال می‌شود که
    // نشانی https واقعی در پیکربندی موجود باشد (نشانی‌های برنامه یا
    // متغیر محیطی استاندارد ASPNETCORE_HTTPS_PORT) — در استقرار‌های HTTPS
    // دار هم رفتار دست‌نخورده باقی می‌ماند.
    // ═══ چرخهٔ ۸۱ — ریشهٔ شکست تولید سند OpenAPI ═══
    // app.Urls پیش از اجرای میزبان به IServerAddressesFeature نیاز دارد و اگر
    // سرور ساخته نشده باشد استثنا می‌دهد:
    //   InvalidOperationException: Feature 'IServerAddressesFeature' is not present
    // ابزار GetDocument (هدف GenerateOpenApiDocuments) برنامه را فقط تا لحظهٔ
    // ساخت Host اجرا می‌کند و Run را انجام نمی‌دهد؛ بنابراین خواندن app.Urls
    // این‌جا کل تولید سند را می‌شکست. نشانی‌ها از همان منابع پیکربندی خوانده
    // می‌شوند که WebApplication.Urls از آن‌ها پر می‌شود (بدون وابستگی به سرور).
    var configuredUrls = builder.Configuration["urls"]
                         ?? builder.Configuration["ASPNETCORE_URLS"]
                         ?? string.Empty;
    var hasHttpsEndpoint = configuredUrls
                               .Split(';', StringSplitOptions.RemoveEmptyEntries)
                               .Any(u => u.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                           || !string.IsNullOrWhiteSpace(
                               Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT"));
    if (hasHttpsEndpoint)
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

    // نصب‌کننده با --migrate-database ساخت/ارتقای دیتابیس را صریحاً و خودکار
    // پیش از ثبت سرویس انجام می‌دهد. این جداسازی مانع مهلت‌تمام‌شدن SCM ویندوز
    // هنگام ساخت اولیهٔ schema می‌شود. در اجرای عادی، خودکارسازی روشن است مگر
    // نصب‌کنندهٔ سرویس آن را پس از اجرای موفق همین گام خاموش کرده باشد.
    var migrateDatabaseOnly = args.Any(arg =>
        string.Equals(arg, "--migrate-database", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "--migrate-database=true", StringComparison.OrdinalIgnoreCase));
    var autoMigrateOnStartup = builder.Configuration.GetValue("Database:AutoMigrateOnStartup", true);
    if (migrateDatabaseOnly || autoMigrateOnStartup)
        await app.Services.InitializeDatabaseAsync();
    else
        Log.Information("اعمال دیتابیس هنگام نصب/ارتقا مدیریت می‌شود؛ اجرای خودکار startup خاموش است");

    if (migrateDatabaseOnly)
    {
        Log.Information("✅ ساخت/به‌روزرسانی خودکار دیتابیس کامل شد؛ خروج موفق");
        return;
    }

    // نقطه بررسی سلامت — کلاینت WPF و مانیتورینگ از این مسیر استفاده می‌کنند
    // چرخهٔ ۵۵ (BUG-20): نسخه از فرادادهٔ اسمبلی (Directory.Build.props) — نه هاردکد 1.0.0
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        message = "سامانه درمانگاه سهند فعال است",
        serverTime = DateTime.UtcNow,
        version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "unknown"
    }))
    .AllowAnonymous()
    .ExcludeFromDescription();

    // ═══ چرخهٔ ۵۵ (BUG-20): نقاط سلامت استاندارد مانیتورینگ ═══
    // /health/live فقط زنده‌بودن فرآیند — برای پراب restart؛
    // /health/ready آمادگی سرویس‌دهی — دیتابیس + فضای دیسک پشتیبان + Hangfire
    app.MapClinicProHealthChecks();

    Log.Information("✅ API درمانگاه سهند راه‌اندازی شد و در حال سرویس‌دهی است");

    // ═══ چرخه سی‌ویکم (گزارش مالک) — تپ قلب یکبارهٔ استارتاپ ═══
    // «در تنظیمات و اطلاعات درمانگاه، اطلاعات رو از لایسنس نمیاره و نشون نمیده» —
    // ریشهٔ دوم (در کنار فعال‌سازی): تپ قلبِ همگام‌سازِ اطلاعات درمانگاه فقط کارِ
    // زمان‌بندی‌شدهٔ Hangfire ساعت ۳ بامداد بود که (الف) در استقرارهایی که Jobs:Enabled
    // خاموش است هرگز اجرا نمی‌شود و (ب) در بقیه تا اولین ۳ صبح طول می‌کشد. اکنون یک
    // تپ قلب یکبارهٔ تأخیری (۴۵ ثانیه پس از بالا آمدن، خارج از مسیر راه‌اندازی) اجرا
    // می‌شود: اگر لایسنسی ثبت شده باشد، حضور به سرور لایسنس اعلام و اطلاعات درمانگاه
    // (نام/مدیر/تلفن/نشانی/کدها) روی دستهٔ ClinicInfo تنظیمات همگام می‌شود — همان
    // چیزی که صفحهٔ تنظیمات نمایش می‌دهد. خطای شبکه کاملاً بی‌صدا بلعیده می‌شود
    // (مهلت آفلاین ۳۰ روزهٔ خودش در تپ قلب شبانه جبران می‌کند).
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(45));
            using var scope = app.Services.CreateScope();
            var licenseSvc = scope.ServiceProvider
                .GetRequiredService<Sahand.ClinicPro.Application.Interfaces.ILicenseService>();
            var report = await licenseSvc.HeartbeatAsync();
            Log.Information(report.IsValid
                ? "🫀 تپ قلب استارتاپ موفق — اطلاعات درمانگاه از سرور لایسنس همگام شد"
                : "🫀 تپ قلب استارتاپ ناموفق: {Message}", report.Message);
        }
        catch (Exception ex)
        {
            // بی‌صدا — شبکه/سرور لایسنس در دسترس نبود؛ راه‌اندازی کامل شده است
            Log.Debug(ex, "تپ قلب یکبارهٔ استارتاپ اجرا نشد (بی‌اهمیت)");
        }
    });

    await app.RunAsync();
}
catch (HostAbortedException)
{
    // چرخه چهاردهم (پیوست P3 — گزارش مالک): ابزارهای طراحی EF Core مانند
    // «dotnet ef database update» برای برداشتن تنظیمات، هاست را می‌سازند و
    // سپس با پرتاب همین خطا عمداً متوقفش می‌کنند — توقف «عمدی و طبیعی» است.
    // پیش‌تر این خطا به چنگال کلیِ پایین می‌افتاد و با پیام «💥 خطای مهلک» و
    // stack trace چاپ می‌شد و کاربرِ راهنمای SQL را می‌ترساند. پرتاب دوباره
    // الزامی است: خودِ ابزار EF منتظر همین خطاست (OperationExecutor آن را
    // موفقیت می‌شمارد) و بلعیدنش رفتار طراحی‌زمان را تغییر می‌دهد.
    Log.Information("🛠 توقف عمدی هاست توسط ابزار طراحی EF Core — طبیعی است");
    throw;
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
public partial class Program
{
    // ═══ چرخهٔ بیست‌وهشتم — فونت TrueType شکل‌دهندهٔ فارسی برای کنسول ویندوز ═══
    // توضیح کامل ریشه/راه‌حل در سربرگ ابتدای همین فایل.

    /// <summary>ساختار انتخاب فونت کنسول (CONSOLE_FONT_INFO_EX)</summary>
    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct ConsoleFontInfoEx
    {
        /// <summary>اندازهٔ ساختار — باید پیش از فراخوانی ست شود</summary>
        public uint cbSize;
        /// <summary>نمایهٔ فونت در جدول فونت‌های کنسول — صفر هنگام تعیین با نام</summary>
        public uint nFont;
        /// <summary>عرض نویسه — صفر یعنی پیش‌فرض</summary>
        public short dwFontSizeX;
        /// <summary>ارتفاع نویسه (پیکسل)</summary>
        public short dwFontSizeY;
        /// <summary>خانوادهٔ فونت — باید پرچم TrueType داشته باشد</summary>
        public int FontFamily;
        /// <summary>وزن فونت — ۴۰۰ معمولی</summary>
        public int FontWeight;
        /// <summary>نام فونت — حداکثر ۳۲ نویسه</summary>
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FaceName;
    }

    /// <summary>دریافت دستگیرهٔ جریان خروجی استاندارد کنسول</summary>
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>تنظیم فونت جاری کنسول — فقط conhost ویندوز</summary>
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true,
        CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow,
        ref ConsoleFontInfoEx lpConsoleCurrentFontEx);

    /// <summary>
    /// تنظیم فونت TrueType شکل‌دهندهٔ فارسی روی کنسول ویندوز — پس از
    /// UTF-8 کردن کدپیج صدا زده می‌شود؛ هر خطایی بی‌صدا بلعیده می‌شود.
    /// </summary>
    private static void TrySetPersianCapableConsoleFont()
    {
        try
        {
            if (!System.OperatingSystem.IsWindows())
                return;

            const int stdOutputHandle = -11; // STD_OUTPUT_HANDLE
            var handle = GetStdHandle(stdOutputHandle);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                return;

            var font = new ConsoleFontInfoEx
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<ConsoleFontInfoEx>(),
                nFont = 0,
                dwFontSizeX = 0,   // صفر = اندازهٔ پیش‌فرض خود conhost
                dwFontSizeY = 18,  // ارتفاع خوانا برای متن فارسی
                FontFamily = 54,   // TMPF_TRUETYPE (0x04) | FF_MODERN (0x30)
                FontWeight = 400,  // وزن معمولی
                FaceName = "Courier New" // TrueType دارای گلیف عربی در همهٔ ویندوزها
            };
            SetCurrentConsoleFontEx(handle, bMaximumWindow: false, ref font);
        }
        catch
        {
            // محیط بدون کنسول متصل یا دسترسی ناکافی — بی‌خطر؛ خروجی همیشه
            // در فایل‌های UTF-8 روزانه هم ثبت می‌شود
        }
    }
}
