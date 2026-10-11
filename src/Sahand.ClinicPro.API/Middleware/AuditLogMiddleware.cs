using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور لاگ عملیات — همه عملیات نوشتن (POST/PUT/DELETE) ثبت ردگیری می‌شوند
/// مقادیر حساس (رمز) هرگز ثبت نمی‌شوند
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>متدهای ثبت‌شونده</summary>
    private static readonly string[] WriteMethods = { "POST", "PUT", "PATCH", "DELETE" };

    /// <summary>فیلدهای حساس که هرگز لاگ نمی‌شوند</summary>
    private static readonly string[] SensitiveFields = { "password", "token", "secret", "apiKey" };

    /// <summary>
    /// ستون‌های حساس سطح دیتابیس — برای «مقادیر قبلی» که مستقیم از سطر
    /// دیتابیس خوانده می‌شوند؛ هش رمز و نشانگرهای امنیتی هرگز نباید در
    /// لاگ عملیات قابل مشاهده باشند (چرخه ۲۶ — گزارش هجدهم مالک)
    /// </summary>
    private static readonly string[] SensitiveColumns =
        { "password", "token", "secret", "apikey", "hash", "stamp" };

    /// <summary>سقف طول متن مقادیر قبلی/جدید — جلوگیری از باد کردن جدول لاگ</summary>
    private const int MaxValuesLength = 8000;

    /// <summary>سازنده با نقطه بعدی</summary>
    public AuditLogMiddleware(RequestDelegate next) => _next = next;

    /// <summary>فراخوانی میدلور</summary>
    public async Task InvokeAsync(HttpContext context, ClinicProDbContext dbContext)
    {
        if (!WriteMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase)
            || !context.Request.Path.StartsWithsWith("/api"))
        {
            await _next(context);
            return;
        }

        // خواندن بدنه برای ثبت — سپس بازنشانی برای خواندن بعدی
        string? requestBody = null;
        context.Request.EnableBuffering();
        if (context.Request.ContentLength is > 0 and < 100_000)
        {
            // ═══ چرخهٔ سی‌امین — قطع اتصال کلاینت هنگام ارسال بدنه ═══
            // «Unexpected end of request content» یعنی کلاینت وسط ارسال
            // بدنه اتصال را بریده است (تایم‌اوت کلاینت، بستن برنامهٔ دسکتاپ،
            // لغو درخواست در Swagger و…). این رده خطا «خطای برنامه» نیست؛
            // پیش‌تر همین استثنا تا میدلور مدیریت خطا بالا می‌آمد و به‌عنوان
            // «خطای غیرمنتظره» (ERR با TraceId) ثبت می‌شد. اکنون خواندنِ
            // بدنهٔ ناقص برای لاگ بی‌ارزش است: بی‌صدا رها می‌شود و جریان
            // به مرحلهٔ بعد می‌رود تا مدل‌بایندینگ خودش خطای زیرساختی را به
            // Kestrel برگرداند (میدلور مدیریت خطا هم این استثنا را عمداً
            // بازمی‌پرتابد تا Kestrel اتصال را بی‌سروصدا ببندد).
            try
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
            }
            catch (Exception ex) when (ex is BadHttpRequestException or IOException
                                       or OperationCanceledException)
            {
                // بدنه ناقص — چیزی برای ثبت نیست
                requestBody = null;
            }
            finally
            {
                // بازنشانی محل جریان حتی در شکست خواندن — خواننده‌های بعدی
                try { context.Request.Body.Position = 0; }
                catch { /* جریان در حالت خطا — نادیده گرفته می‌شود */ }
            }
        }

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — «مقادیر قبلی چیزی نشان نمی‌دهد» ═══
        // برای ویرایش/حذف، وضعیت سطر «پیش از» اجرای هندلر از دیتابیس خوانده
        // می‌شود تا در ستون OldValues ثبت شود؛ خواندن باید حتماً قبل از
        // _next باشد وگرنه مقادیر جدید روی سطر نوشته شده‌اند. بهترین تلاش
        // است — هر خطایی نادیده گرفته می‌شود و جریان درخواست سالم می‌ماند.
        string? oldValues = null;
        var isWriteWithId = (context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                             || context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
                             || context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                            && ExtractId(context.Request.Path) is not null;
        if (isWriteWithId)
            oldValues = TryCaptureOldValues(dbContext, context.Request.Path);

        var originalBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            // کپی پاسخ به جریان اصلی
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // ثبت عملیات فقط برای درخواست‌های احراز هویت‌شده
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var userId = long.TryParse(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (long?)null;

                var action = context.Request.Method.ToUpperInvariant() switch
                {
                    "POST" => AuditActionType.Create,
                    "PUT" or "PATCH" => AuditActionType.Update,
                    "DELETE" => AuditActionType.Delete,
                    _ => AuditActionType.View
                };

                var sanitized = Truncate(SanitizeBody(requestBody));

                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Username = context.User.Identity?.Name,
                    ActionType = action,
                    EntityName = ExtractEntityName(context.Request.Path),
                    EntityId = ExtractId(context.Request.Path),
                    OldValues = oldValues,
                    NewValues = sanitized,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Module = "API",
                    Severity = AuditSeverity.Info
                });

                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // خطای ثبت لاگ هرگز نباید درخواست اصلی را خراب کند
            }
        }
    }

    /// <summary>حذف فیلدهای حساس از بدنه</summary>
    private static string? SanitizeBody(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            using var stream = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                WriteSanitized(writer, doc.RootElement);
            }
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return "بدنه غیرقابل تفسیر";
        }
    }

    /// <summary>بازنویسی بازگشتی حذف فیلدهای حساس</summary>
    private static void WriteSanitized(System.Text.Json.Utf8JsonWriter writer,
        System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    if (SensitiveFields.Any(f =>
                        prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WriteString(prop.Name, "***حذف شد***");
                    }
                    else
                    {
                        writer.WritePropertyName(prop.Name);
                        WriteSanitized(writer, prop.Value);
                    }
                }
                writer.WriteEndObject();
                break;
            case System.Text.Json.JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteSanitized(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// استخراج نام موجودیت از مسیر — چرخهٔ چهل‌وچهارم (گزارش مالک):
    /// «لاگ عملیات هنوز هم مقادیر قبلی رو نشون نمیده».
    /// ═══ ریشهٔ قطعی که چند چرخه پنهان ماند ═══
    /// پیش‌تر همیشه «قطعهٔ سوم» مسیر (parts[2]) نام موجودیت فرض می‌شد؛ اما
    /// بخش بزرگی از کنترلرهای برنامه مسیرهای «زیرمنبعی» دارند:
    ///   PUT /api/v1/hr/employees/5           → parts[2] = «hr»        ✗
    ///   PUT /api/v1/insurance/companies/7    → parts[2] = «insurance» ✗
    ///   PUT /api/v1/lookups/rooms/3          → parts[2] = «lookups»   ✗
    ///   PUT /api/v1/prescriptions/templates/9→ parts[2] = «prescriptions» ✗
    /// هیچ‌کدام به جدول دیتابیسی نگاشت نمی‌شدند ← OldValues همیشه تهی می‌ماند
    /// (و نام موجودیت ثبت‌شده در لاگ هم بی‌معنا بود — «hr» به‌جای «employees»).
    /// ═══ الگوی جدید: قطعهٔ «نزدیک‌ترین به شناسهٔ عددی» ═══
    /// شناسهٔ عددی آخرین مسیر پیدا می‌شود (ExtractId) و قطعهٔ «یک قدم پیش از آن»
    /// نام موجودیت است — «hr/employees/5» → employees، «insurance/companies/7»
    /// → companies. مسیرهای سادهٔ «/patients/5» هم دقیقاً مثل قبل کار می‌کنند.
    /// اگر شناسه‌ای در مسیر نبود، رفتار پیشین (parts[2]) حفظ می‌شود تا نام
    /// موجودیتِ درخواست‌های بدون‌شناسه (مانند users/me/...) همچنان ثبت شود.
    /// </summary>
    private static string? ExtractEntityName(PathString path)
    {
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: >= 3 }) return null;

        // نخستین قطعهٔ عددی از انتها (به‌جای api/v1) — همان شناسهٔ رکورد
        for (var i = parts.Length - 1; i >= 2; i--)
        {
            if (long.TryParse(parts[i], out _))
            {
                // یک قدم پیش از شناسه — قطعهٔ نام موجودیت زیرمنبع
                // (idIndex > 2 یعنی قطعهٔ نام، خودش جای parts[2] یا بعدش است)
                return i > 2 ? parts[i - 1] : parts[2];
            }
        }

        // بدون شناسه — رفتار پیشین (نام کنترلر/ماژول)
        return parts[2];
    }

    /// <summary>استخراج شناسه از مسیر — چرخهٔ سی‌وپنجم: «آخرین قطعهٔ عددی»</summary>
    /// <summary>
    /// پیش‌تر فقط «آخرین قطعهٔ مسیر» بررسی می‌شد؛ مسیرهای زیرمنبعی مثل
    /// PUT /api/v1/doctors/5/schedule قطعهٔ آخرشان «schedule» است → مقدار قبلی
    /// هرگز برایشان ثبت نمی‌شد (لاگ عملیات ویرایش برنامهٔ کاری، سهم بیمه و
    /// سایر زیرمنبع‌ها بدون OldValues می‌ماند). اکنون از انتها به ابتدا، نخستین
    /// قطعهٔ عددی یافت‌شده برگردانده می‌شود تا مسیرهای «/…/{id}/زیرمنبع» هم
    /// پوشش داده شوند؛ مسیرهای سادهٔ /{id} هم دقیقاً همان‌طور که بودند کار می‌کنند.
    /// </summary>
    private static long? ExtractId(PathString path)
    {
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: > 0 }) return null;
        // از انتها به ابتدا — نخستین قطعهٔ عددی همان شناسهٔ رکورد است
        // (قطعه‌های غیرعددی مثل «v1» یا «schedule» با long.TryParse رد می‌شوند)
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (long.TryParse(parts[i], out var id))
                return id;
        }
        return null;
    }

    /// <summary>برش متن به سقف مجاز — جلوگیری از باد کردن ستون‌های لاگ</summary>
    private static string? Truncate(string? value) =>
        value is null || value.Length <= MaxValuesLength
            ? value
            : value[..MaxValuesLength] + "… (بریده شد)";

    // ═══ چرخهٔ چهل‌وچهارم — پل تست ═══
    // دو متد «محض» استخراج نام موجودیت و شناسه برای تست‌های واحد نمایش داده
    // می‌شوند (AuditLogEntityNameExtractionTests) تا الگوی «قطعهٔ نزدیک به
    // شناسه» در آینده هم شکسته نشود؛ TryCaptureOldValues به DbContext وابسته
    // است و در تست‌های یکپارچگی پوشش داده می‌شود.

    /// <summary>
    /// استخراج قطعهٔ زیرمنبع از مسیر — چرخهٔ چهل‌وهفتم.
    /// الگوی هدف: «/api/v1/…/{والد}/{شناسه}/{زیرمنبع}» مثل
    /// roles/5/permissions یا doctors/7/schedule؛ نخستین قطعهٔ عددی از
    /// انتها پیدا می‌شود و اگر «بعد از آن» قطعهٔ غیرعدنی دیگری باشد،
    /// همان قطعه زیرمنبع است (تهی یعنی مسیر سادهٔ /{والد}/{شناسه}).
    /// </summary>
    private static string? ExtractSubResource(PathString path)
    {
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: > 0 }) return null;

        // نخستین قطعهٔ عددی از انتها (همان شناسهٔ رکورد)
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (long.TryParse(parts[i], out _))
            {
                // قطعهٔ بعد از شناسه — اگر موجود و غیرعددی باشد، زیرمنبع است
                // (قطعه‌های «قطع مسیر» مثل «permissions/3» را پوشش نمی‌دهد —
                // شناسهٔ انتهایی، همان شناسهٔ زیرمنبع می‌شود و والدِ آن، خودِ
                // زیرمنبع؛ این مسیرها در دامنهٔ این متد نیستند)
                return i + 1 < parts.Length ? parts[i + 1] : null;
            }
        }
        return null;
    }

    /// <summary>
    /// ثبت وضعیت فعلیِ «زیرمنبع‌های معنادار» — چرخهٔ چهل‌وهفتم (گزارش مالک:
    /// «دسترسی نقش ها رو عوض کردم و در مقادیر قبلی، آی دی و نام نقش و
    /// توضیحات نشون میده — اشتباهه»).
    /// خواندن با پرس‌وجوی LINQ بدون‌ردیابی (AsNoTracking) انجام می‌شود تا
    /// ChangeTracker هندلر آلوده نشود؛ خروجی JSON خوانا با برچسب‌های فارسی.
    /// مقدار تهی یعنی «این جفت والد/زیرمنبع شناخته نشد» → سقوط به سطر والد.
    /// </summary>
    private static string? TryCaptureSubResourceOldValues(
        ClinicProDbContext dbContext, string entitySegment, string subResource, long id)
    {
        try
        {
            // ═══ roles/{id}/permissions — فهرست مجوزهای فعلی نقش ═══
            if (subResource.Equals("permissions", StringComparison.OrdinalIgnoreCase)
                && entitySegment.Equals("roles", StringComparison.OrdinalIgnoreCase))
            {
                var role = dbContext.Roles
                    .AsNoTracking()
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        r.Name,
                        Permissions = r.Permissions
                            .Select(rp => new { rp.Permission.Id, rp.Permission.Name })
                            .OrderBy(p => p.Name)
                            .ToList()
                    })
                    .FirstOrDefault();
                if (role is null) return null;

                var json = JsonSerializer.Serialize(new
                {
                    roleName = role.Name,
                    permissionCount = role.Permissions.Count,
                    // نام مجوزها «کلید» فهم تغییرند؛ شناسه‌ها هم برای تطبیق دقیق
                    permissions = role.Permissions.Select(p => $"{p.Name} (#{p.Id})").ToList()
                }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                return Truncate(json);
            }

            // ═══ doctors/{id}/schedule — ردیف‌های فعلی برنامهٔ کاری هفتگی ═══
            if (subResource.Equals("schedule", StringComparison.OrdinalIgnoreCase)
                && entitySegment.Equals("doctors", StringComparison.OrdinalIgnoreCase))
            {
                // ستون‌های خام اول واکشی می‌شوند (قالب‌بندی TimeOnly در پروجکشن
                // به SQL ترجمه نمی‌شود) و سپس در حافظه به متن خوانا تبدیل می‌شوند
                var rawRows = dbContext.Set<DoctorSchedule>()
                    .AsNoTracking()
                    .Where(d => d.DoctorId == id && !d.IsDeleted)
                    .OrderBy(d => d.DayOfWeek)
                    .Select(d => new
                    {
                        d.DayOfWeek, d.StartTime, d.EndTime, d.BreakStart, d.BreakEnd,
                        d.RoomId, d.MaxPatients, d.IsActive
                    })
                    .ToList();
                if (rawRows.Count == 0) return null;

                var dayNames = new[] { "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه" };
                var json = JsonSerializer.Serialize(new
                {
                    weeklySchedule = rawRows.Select(d => new
                    {
                        day = d.DayOfWeek < dayNames.Length ? dayNames[d.DayOfWeek] : d.DayOfWeek.ToString(),
                        startTime = d.StartTime.ToString("HH:mm"),
                        endTime = d.EndTime.ToString("HH:mm"),
                        breakRange = d.BreakStart.HasValue
                            ? $"{d.BreakStart:HH:mm}-{d.BreakEnd:HH:mm}"
                            : null,
                        roomId = d.RoomId,
                        maxPatients = d.MaxPatients,
                        enabled = d.IsActive
                    }).ToList()
                }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                return Truncate(json);
            }

            return null; // زیرمنبع شناخته‌شده نبود — سقوط به سطر والد
        }
        catch
        {
            // بهترین تلاش — شکستِ ثبتِ زیرمنبع نباید مسیر والد را هم بپوشاند
            return null;
        }
    }

    /// <summary>نمایش داخلی ExtractEntityName برای پروژهٔ تست</summary>
    internal static string? ExtractEntityNameForTest(PathString path) => ExtractEntityName(path);

    /// <summary>نمایش داخلی ExtractId برای پروژهٔ تست</summary>
    internal static long? ExtractIdForTest(PathString path) => ExtractId(path);

    /// <summary>
    /// نرمال‌سازی نام برای مقایسه — حذف جداکننده‌ها/آندرلاین، جمع‌زدایی و کوچک‌سازی.
    /// «insurance-companies» و «InsuranceCompanies» هر دو «insurancecompanie» می‌شوند.
    /// </summary>
    private static string NormalizeName(string name) => name
        .Replace("-", "", StringComparison.OrdinalIgnoreCase)
        .Replace("_", "", StringComparison.OrdinalIgnoreCase)
        .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
        .TrimEnd('s', 'S')
        .ToLowerInvariant();

    /// <summary>
    /// یافتن جدول متناظر از نام قطعهٔ مسیر — چرخهٔ چهل‌وچهارم (گزارش مالک:
    /// «مقادیر قبلی هنوز نشان داده نمی‌شود»).
    /// ═══ چرا نگاشتِ قبلی شکست می‌خورد؟ ═══
    /// فقط «تطابق کامل» نام قطعه با نام جدول/نوع بررسی می‌شد؛ این نام‌های واقعی
    /// برنامه هرگز تطابق کامل ندارند:
    ///   «companies» (از insurance/companies/7) در برابر جدول «InsuranceCompanies»
    ///   «templates» (از prescriptions/templates/9) در برابر «PrescriptionTemplate»
    ///   «files»     (از imaging/files/3)        در برابر «ImagingFile»
    /// ═══ زنجیرهٔ چهارمرحله‌ای جدید ═══
    /// ۱) تطابق دوبستگی «والد + موجودیت»: والدِ قطعهٔ موجودیت به آن می‌چسبد —
    ///    insurance+companies → insurancecompanie ≡ InsuranceCompanies ✓
    ///    prescriptions+templates → prescriptiontemplate ≡ PrescriptionTemplate ✓
    ///    (این مرحله هم‌نام‌های مبهم را از هم جدا می‌کند — templates تنها می‌تواند
    ///    هم PrintTemplate باشد هم PrescriptionTemplate؛ والد، مرجع را قاطع می‌کند)
    /// ۲) تطابق کامل تک‌قطعه: employees ≡ Employees، rooms ≡ Rooms
    /// ۳) تطابق پسوندی: قطعه، «پایانِ» نام جدول باشد — companies → InsuranceCompanies،
    ///    care-plans → CarePlan؛ اگر چند جدول پسوند یکسان داشتند، «کوتاه‌ترین»
    ///    (نزدیک‌ترین به نام قطعه) انتخاب می‌شود
    /// ۴) واژه‌نامهٔ نام‌های مستعار برای موارد به‌هیچ‌وجه هم‌شکل‌نبودن (مثل
    ///    consumables → Medications که نام مسیر و نام جدول بی‌ارتباطند)
    /// همهٔ مقایسه‌ها پس از NormalizeName انجام می‌شوند (جداکننده و جمع بی‌اثر).
    /// </summary>
    private static (string? Table, string? Schema)? FindTargetTable(
        ClinicProDbContext dbContext, string entitySegment, string? parentSegment)
    {
        var entityNorm = NormalizeName(entitySegment);
        if (string.IsNullOrEmpty(entityNorm))
            return null;

        // نام‌های مستعار صریح — مواردی که هیچ الگوی خودکاریشان را پوشش نمی‌دهد
        // (نام مسیر API و نام جدول کاملاً از هم جدا هستند)
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // مسیر مصرفی‌های مستقل روی موجودیت Medications (قلمِ ItemKind=مصرفی) کار می‌کند
            ["consumables"] = "Medications",
            // مسیر عکس پرسنل/بیمار روی جدول‌های People
            ["people"] = "People"
        };

        string? aliasTable = null;
        string? aliasSchema = null;
        if (aliases.TryGetValue(entitySegment, out var wanted))
        {
            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                var table = entityType.GetTableName();
                if (table is null) continue;
                if (NormalizeName(table) == NormalizeName(wanted)
                    || NormalizeName(entityType.ClrType.Name) == NormalizeName(wanted))
                {
                    aliasTable = table;
                    aliasSchema = entityType.GetSchema();
                    break;
                }
            }
            if (aliasTable is not null)
                return (aliasTable, aliasSchema);
        }

        // نام دوبستگی «والد + موجودیت» — گام ۱ (گزاره‌های بالادستی گاهی بخشی
        // از نام جدول‌اند: InsuranceCompanies = insurance + companies)
        var joinedNorm = string.IsNullOrEmpty(parentSegment)
            ? null
            : NormalizeName(parentSegment + entitySegment);

        // گام ۲ و ۳ — تک‌قطعه: ابتدا تطابق کامل، سپس پسوندی (کوتاه‌ترین برنده)
        string? exactTable = null, exactSchema = null;
        string? suffixTable = null, suffixSchema = null;
        var suffixTableLength = int.MaxValue;

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is null) continue;

            var candidates = new[] { table, entityType.ClrType.Name };
            foreach (var candidate in candidates)
            {
                var candidateNorm = NormalizeName(candidate);

                // گام ۱ — دوبستگی
                if (joinedNorm is not null
                    && candidateNorm == joinedNorm
                    && exactTable is null)
                {
                    exactTable = table;
                    exactSchema = entityType.GetSchema();
                }

                // گام ۲ — تطابق کامل تک‌قطعه
                if (candidateNorm == entityNorm
                    && exactTable is null)
                {
                    exactTable = table;
                    exactSchema = entityType.GetSchema();
                }

                // گام ۳ — پسوندی؛ کوتاه‌ترین نام (نزدیک‌ترین به قطعه) برنده است
                if (candidateNorm.Length > entityNorm.Length
                    && candidateNorm.EndsWith(entityNorm, StringComparison.Ordinal)
                    && candidate.Length < suffixTableLength)
                {
                    suffixTable = table;
                    suffixSchema = entityType.GetSchema();
                    suffixTableLength = candidate.Length;
                }
            }

            // اگر گام ۱ یا ۲ همین‌جا وفق یافت، ادامهٔ پیمایش برای تطابق کامل
            // بی‌معناست ولی پسوندی ممکن است هنوز بهتر باشد — پیمایش ادامه می‌یابد
        }

        if (exactTable is not null)
            return (exactTable, exactSchema);
        if (suffixTable is not null)
            return (suffixTable, suffixSchema);
        return null;
    }

    /// <summary>
    /// خواندن وضعیت فعلی سطر هدف از دیتابیس «پیش از» اجرای هندلر —
    /// چرخهٔ بیست‌وششم (گزارش هجدهم مالک): بخش «مقادیر قبلی» جزئیات لاگ
    /// عملیات همیشه خالی بود چون هیچ‌جا OldValues پر نمی‌شد.
    /// چرخهٔ چهل‌وچهارم: نگاشت نام موجودیت به جدول از زنجیرهٔ چندلایهٔ
    /// FindTargetTable می‌گذرد (دوبستگی/کامل/پسوندی/مستعار) تا مسیرهای
    /// زیرمنبعی (hr/employees، insurance/companies، lookups/rooms و…)
    /// نیز پوشش داده شوند — پیش‌تر فقط تطابق کامل تک‌قطعه بررسی می‌شد و
    /// بخش بزرگی از مسیرهای برنامه بی‌نتیجه می‌ماندند.
    /// ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) — زیرمنبع‌های معنادار ═══
    /// «در لاگ عملیات مقادیر قبلی رو اشتباه ثبت میکنه. مثلاً من دسترسی
    /// نقش ها رو عوض کردم و در مقادیر قبلی مشخصات دیگه ای مثل آی دی و
    /// نام نقش و توضیحات فیلد نشون میده» — ریشه: مسیرِ ذخیرهٔ دسترسی‌های
    /// نقش «PUT roles/{id}/permissions» است؛ الگوی عمومی، سطرِ خودِ نقش
    /// (آی‌دی/نام/توضیحات) را به‌عنوان مقدار قبلی ثبت می‌کرد نه تغییر
    /// واقعی یعنی فهرست مجوزها. اکنون مسیرهای زیرمنبعِ شناخته‌شده به
    /// «وضعیت خودِ زیرمنبع» نگاشت می‌شوند (TryCaptureSubResourceOldValues):
    ///   roles/{id}/permissions → فهرست مجوزهای فعلی نقش (نام + شناسه)
    ///   doctors/{id}/schedule → ردیف‌های برنامهٔ کاری هفتگی فعلی پزشک
    /// سایر زیرمنبع‌ها به همان رفتار قبلی (سطر والد) برمی‌گردند.
    /// روش: نگاشت نام موجودیت مسیر به جدول متناظر در مدل EF، سپس
    /// SELECT TOP 1 * با ADO خام (بدون ورود به ChangeTracker تا سطرهای
    /// هندلر آلوده نشوند) و ساخت JSON ستون/مقدار با حذف ستون‌های حساس
    /// (هش رمز، توکن، …) و ستون‌های دودویی (RowVersion).
    /// </summary>
    private static string? TryCaptureOldValues(ClinicProDbContext dbContext, PathString path)
    {
        try
        {
            var entitySegment = ExtractEntityName(path);
            var id = ExtractId(path);
            if (string.IsNullOrEmpty(entitySegment) || id is null)
                return null;

            // ═══ چرخهٔ چهل‌وهفتم — زیرمنبع‌های معنادار ═══
            // اگر مسیر الگوی «/{والد}/{شناسه}/{زیرمنبع}» داشته باشد، نخست
            // تلاش می‌شود وضعیت خودِ زیرمنبع ثبت شود (مثلاً فهرست مجوزهای
            // نقش)؛ اگر این مسیر زیرمنبع شناخته‌شده نبود، سقوط به سطر والد.
            var subResource = ExtractSubResource(path);
            if (subResource is not null)
            {
                var captured = TryCaptureSubResourceOldValues(dbContext, entitySegment, subResource, id.Value);
                if (captured is not null)
                    return captured;
            }

            // والدِ قطعهٔ موجودیت — برای تطابق دوبستگی (گام ۱ زنجیره)؛
            // برای «insurance/companies/7» والدِ «companies» همان «insurance» است.
            // اگر قطعهٔ موجودیت خودش parts[2] باشد (مسیر ساده)، والدی نیست.
            var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var entityIndex = Array.IndexOf(parts!, entitySegment);
            var parentSegment = entityIndex > 2 ? parts![entityIndex - 1] : null;

            var target = FindTargetTable(dbContext, entitySegment, parentSegment);
            if (target is not { Table: { } tableName })
                return null; // مسیر به موجودیت مشخصی نگاشت نشد — بدون مقادیر قبلی
            var schema = target.Value.Schema;

            // خواندن سطر با ADO خام — بدون ردیابی تغییرات، بدون تداخل با هندلر
            var connection = dbContext.Database.GetDbConnection();
            var openedHere = false;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
                openedHere = true;
            }

            try
            {
                using var command = connection.CreateCommand();
                var idParam = command.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                command.Parameters.Add(idParam);
                var tableFull = string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{tableName}]";
                command.CommandText = $"SELECT TOP 1 * FROM {tableFull} WHERE [Id] = @id";

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                    return null; // سطر وجود ندارد — مثلاً درخواست ناموفق خواهد بود

                // ساخت JSON ستون/مقدار با حذف ستون‌های حساس و دودویی
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var column = reader.GetName(i);
                        if (SensitiveColumns.Any(f => column.Contains(f, StringComparison.OrdinalIgnoreCase)))
                        {
                            writer.WriteString(column, "***حذف شد***");
                            continue;
                        }

                        var value = reader.GetValue(i);
                        if (value is byte[])
                        {
                            writer.WriteString(column, "(دودویی)");
                            continue;
                        }

                        switch (value)
                        {
                            case DateTime dt:
                                writer.WriteString(column, dt.ToString("O"));
                                break;
                            case DBNull:
                                writer.WriteNull(column);
                                break;
                            case bool b:
                                writer.WriteBoolean(column, b);
                                break;
                            case int i32:
                                writer.WriteNumber(column, i32);
                                break;
                            case long i64:
                                writer.WriteNumber(column, i64);
                                break;
                            case short i16:
                                writer.WriteNumber(column, i16);
                                break;
                            case byte u8:
                                writer.WriteNumber(column, u8);
                                break;
                            case decimal dec:
                                writer.WriteNumber(column, dec);
                                break;
                            case double dbl:
                                writer.WriteNumber(column, dbl);
                                break;
                            case float flt:
                                writer.WriteNumber(column, flt);
                                break;
                            case Guid guid:
                                writer.WriteString(column, guid.ToString());
                                break;
                            default:
                                writer.WriteString(column, value.ToString());
                                break;
                        }
                    }
                    writer.WriteEndObject();
                }
                return Truncate(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
            }
            finally
            {
                if (openedHere)
                    connection.Close();
            }
        }
        catch
        {
            // بهترین تلاش — هر خطایی نباید درخواست اصلی را خراب کند
            return null;
        }
    }
}

/// <summary>بسط مسیر</summary>
internal static class AuditPathExtensions
{
    /// <summary>شروع با — بدون حساسیت حروف</summary>
    public static bool StartsWithsWith(this PathString path, string prefix) =>
        path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase);
}
