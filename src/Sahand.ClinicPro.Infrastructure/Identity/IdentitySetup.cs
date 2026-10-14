using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Identity;

/// <summary>
/// راه‌اندازی احراز هویت JWT Bearer و سرویس‌های امنیتی
/// </summary>
public static class IdentitySetup
{
    /// <summary>
    /// ثبت JWT Bearer و سرویس‌های امنیتی — باید پس از AddInfrastructure فراخوانی شود
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // تنظیمات JWT از بخش Jwt
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        /* ── چرخهٔ ۴۹ (تکمیلی — گرفتِ CI، رفع IDX10703) ──
           جایگزینِ کلید توسعه باید به «خودِ» JwtOptions هم برسد: مولد توکن
           IOptions<JwtOptions> را می‌خواند نه متغیر محلی پایین را. بدون این،
           کلید خالیِ appsettings در توسعه ⟵ SymmetricSecurityKey طول صفر ⟵
           هر لاگین موفق با ۵۰۰ «IDX10703: key length is zero» می‌افتاد.
           PostConfigure فقط در تهی بودن مقدار واقعی پر می‌کند (تولید سرور
           پیش از این گارد سه‌لایه کلید الزامی را رد می‌کند). */
        services.PostConfigure<JwtOptions>(o =>
        {
            if (string.IsNullOrEmpty(o.SecretKey))
                o.SecretKey = "SahandClinicPro-Development-Fallback-Secret-Key-Must-Change!2026";
        });

        /* ── چرخه دوازدهم (بند ۴-۲ سند تحلیل) + چرخهٔ ۴۹ (BUG-01): نگهبان کلید امضا ──
           کلید پیش‌فرض/کوتاه/الگوی ناامن در «تولید» یعنی هرکسی بتواند توکن جعلی بسازد؛
           در محیط تولید سرور با پیام روشن بالا نمی‌آید تا نصب‌کننده وادار به
           تنظیم کلید قوی شود (تولید: متغیر محیطی Jwt__SecretKey یا Secret Manager؛
           توسعه: user-secrets یا خالی = کلید جایگزین توسعه).
           حداقل: ۳۲ بایت (۲۵۶ بیت) و نامساوی مقدار پیش‌فرض.
           چرخهٔ ۴۹: کلید هاردکد از appsettings.json حذف شد؛ علاوه بر ثابتِ
           پیش‌فرض، هر کلیدی که «الگوی کلید توسعه» داشته باشد (مثل کلید پیشین
           مخزن که از گارد قدیمی عبور می‌کرد) هم در تولید رد می‌شود. */
        const string DefaultKey = "SahandClinicPro-Development-Fallback-Secret-Key-Must-Change!2026";
        var isProduction = !string.Equals(
            configuration["Environment"]
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? "",
            "Development", StringComparison.OrdinalIgnoreCase);
        var secret = string.IsNullOrEmpty(jwt.SecretKey) ? DefaultKey : jwt.SecretKey;
        if (isProduction)
        {
            if (string.IsNullOrEmpty(jwt.SecretKey) || jwt.SecretKey == DefaultKey)
                throw new InvalidOperationException(
                    "⛔ کلید امضای JWT تنظیم نشده است (Jwt:SecretKey). در تولید، یک کلید تصادفی حداقل ۶۴ کاراکتری " +
                    "به‌صورت متغیر محیطی (Jwt__SecretKey) یا Secret Manager تنظیم کنید — سرور برای ایمنی شما بالا نمی‌آید. " +
                    "راهنمای کامل: docs/guides/deploy-jwt.md");
            if (System.Text.Encoding.UTF8.GetBytes(secret).Length < 32)
                throw new InvalidOperationException(
                    "⛔ کلید امضای JWT کوتاه‌تر از ۳۲ بایت است (Jwt:SecretKey) — حداقل ۶۴ کاراکتر تصادفی لازم است. " +
                    "نمونه تولید: openssl rand -base64 48");
            // چرخهٔ ۴۹ (BUG-01): تشخیص الگو — کلید پیشین مخزن
            // (SahandClinicPro-Development-Secret-Key-Change-In-Production-…) بلندتر از
            // ۳۲ بایت بود و از دو گارد قبلی عبور می‌کرد؛ این‌جا با تطبیق الگو رد می‌شود.
            // الگوها کوچک‌سازی شده‌اند و در کلید تصادفیِ خالص (base64) هرگز ظاهر نمی‌شوند.
            var lowered = secret.ToLowerInvariant();
            string[] insecurePatterns =
            {
                "development-secret", "change-in-production", "must-change",
                "fallback-secret", "sahandclinicpro-development",
                "replaceme", "changeme"
            };
            if (Array.Exists(insecurePatterns, p => lowered.Contains(p, StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "⛔ کلید امضای JWT دارای الگوی ناامن تشخیص داده شد (Jwt:SecretKey) — کلیدهای حاوی عبارت‌هایی مثل " +
                    "«Development-Secret / Change-In-Production / Must-Change» در تولید پذیرفته نمی‌شوند. " +
                    "یک کلید تصادفی خالص بسازید (openssl rand -base64 48) — راهنما: docs/guides/deploy-jwt.md");
        }

        // احراز هویت JWT Bearer — با کلید از تنظیمات
        var signingKey = System.Text.Encoding.UTF8.GetBytes(secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ClockSkew = System.TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };

            // پاسخ فارسی برای توکن منقضی + پذیرش توکن Hub از query string (گام R1)
            options.Events = new JwtBearerEvents
            {
                // ═══ چرخهٔ ۷۳ (گزارش مالک: «InvalidOperationException: StatusCode
                //      cannot be set because the response has already started») ═══
                // ریشهٔ یقینی خطا همین‌جا بود: WriteAsyncِ «بدون await»! جریان خراب‌کننده:
                //   ۱) توکنِ منقضی در هدر می‌آید (مثلاً بعد از ۸ ساعت کار پیوسته،
                //      عصرها!) ← SecurityTokenExpiredException
                //   ۲) این رویداد اجرا می‌شد، StatusCode=401 می‌گذاشت و WriteAsync
                //      را «شروع می‌کرد و منتظر نمی‌ماند» (fire-and-forget)
                //   ۳) هندلر بلافاصله بازمی‌گشت و خط لوله ادامه می‌یافت؛ نوشتنِ
                //      هم‌زمانِ بدنه، پاسخ را «پشتِ سرِ» ادامهٔ خط لوله آغاز می‌کرد
                //      (سرآیندها flush می‌شد) — حتی برای اندپوینت‌های بی‌نام که
                //      MVC به‌هرحال اجرا می‌شود
                //   ۴) ObjectResult اکشن می‌خواست StatusCode بگذارد ← پاسخ از قبل
                //      آغاز شده ← InvalidOperationException در ObjectResult.OnFormatting
                //      — دقیقاً رد پشتهٔ گزارش مالک
                // قانون اصلاح: «در رویدادهای احراز هویت هرگز پاسخ نوشته نمی‌شود» —
                // این رویداد حتی برای اندپوینت‌های [AllowAnonymous] هم اجرا می‌شود
                // و نوشتن پاسخ یعنی رقابت با خودِ اکشن. نقش صحیح این رویداد فقط
                // «نشانه‌گذاری نتیجه» است: Fail استثنا را بدون بازپرتاب ثبت می‌کند:
                //   • اندپوینت محافظت‌شده ← OnChallenge با پیام فارسی ۴۰۱ می‌دهد
                //     (تشخیص «منقضی» از AuthenticateFailure در همان‌جا انجام می‌شود)
                //   • اندپوینت بی‌نام ← اکشن عادی اجرا می‌شود، هیچ رقابتی نیست
                //   • استثنا هرگز بازپرتاب نمی‌شود ← دیگر ۵۰۰ِ «خطای غیرمنتظره»
                //     برای توکن منقضی/جعلی ثبت نمی‌شود
                OnAuthenticationFailed = context =>
                {
                    // همهٔ خطاهای اعتبارسنجی توکن (منقضی/امضای نامعتبر/قالب خراب)
                    // یکسان رفتار می‌کنند: نتیجه = Fail؛ بدون نوشتن پاسخ و بدون بازپرتاب.
                    // (Fail همان AuthenticateResult.Fail(exception) را می‌نشاند — پراپرتی
                    // Result خودش فقط‌خواندنی است و ست کردن مستقیم کامپایل نمی‌شود؛
                    // شکستِ ثبت‌شده بعداً از طریق AuthenticateFailure به OnChallenge
                    // می‌رسد تا پیام «توکن منقضی» در جای درست ساخته شود)
                    context.Fail(context.Exception);
                    return System.Threading.Tasks.Task.CompletedTask;
                },

                // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — گارد چالش ۴۰۱ ═══
                // لاگ سرور: «InvalidOperationException در JwtBearerHandler.
                // HandleChallengeAsync — StatusCode cannot be set because the
                // response has already started». وقتی درخواستِ بدون/با توکنِ
                // نامعتبر به اندپوینت محافظت‌شده می‌رسد و پاسخ به هر دلیلی
                // (میانهٔ خطای قبلی، فشرده‌سازی و…) آغاز شده باشد، چالشِ
                // پیش‌فرض ۴۰۱ کرش می‌کرد و «unhandled exception» می‌شد.
                // با شروعِ دستی رویداد، پاسخِ آغازشده را به چالش نمی‌فرستیم؛
                // رفتار عادی (هدر WWW-Authenticate + ۴۰۱ JSON فارسی) حفظ شد.
                //
                // ═══ چرخهٔ ۷۳ — دو اصلاح در همین رویداد ═══
                //   ۱) WriteAsync پیش‌تر «بدون await» بود (همان رقابت مرگبارِ
                //      OnAuthenticationFailed) — حالا لامبدا async است و نوشتنِ
                //      کامل پیش از HandleResponse انجام می‌شود
                //   ۲) پیام «توکن منقضی» که قبلاً در OnAuthenticationFailed نوشته
                //      می‌شد، این‌جا از AuthenticateFailure تشخیص داده می‌شود —
                //      جای درستش همین‌جاست چون فقط اندپوینت‌های محافظت‌شده به
                //      چالش می‌رسند و پاسخ هنوز آغاز نشده است
                OnChallenge = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        // پاسخ آغاز شده — تعیین کد وضعیت ممکن نیست؛ فقط اعلان
                        // پیش‌فرض متوقف و درخواست خاتمه می‌یابد بدون استثنا
                        context.HandleResponse();
                        return;
                    }

                    // پیام فارسی استاندارد به‌جای Challenge خالی پیش‌فرض —
                    // کلاینت WPF پیام روشن می‌گیرد (توکن نیست/منقضی/نامعتبر)؛
                    // تشخیص «منقضی» از شکست اعتبارسنجی (نه متن خطای انگلیسی)
                    var expired = context.AuthenticateFailure is SecurityTokenExpiredException;
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        isSuccess = false,
                        statusCode = 401,
                        message = expired
                            ? "توکن دسترسی منقضی شده است — دوباره وارد شوید"
                            : string.IsNullOrEmpty(context.Error)
                                ? "برای این عملیات باید وارد برنامه شوید"
                                : "اعتبارنامه یا دسترسی شما معتبر نیست — دوباره وارد شوید",
                        errors = new List<string>(),
                        traceId = context.HttpContext.TraceIdentifier,
                        data = (object?)null,
                        elapsedMs = 0
                    }));
                    // جلوگیری از چالش پیش‌فرض (نوشتن سرآیند WWW-Authenticate
                    // و متن خالی) — بدنهٔ JSON خودمان پاسخ نهایی است
                    context.HandleResponse();
                },

                // کلاینت WPF SignalR نمی‌تواند هدر Authorization بفرستد؛
                // طبق الگوی رسمی ASP.NET Core توکن از query string «access_token» خوانده می‌شود
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/queue") || path.StartsWithSegments("/hubs/notifications")))
                    {
                        context.Token = accessToken;
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
