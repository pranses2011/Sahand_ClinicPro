using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

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

    /// <summary>استخراج نام موجودیت از مسیر مثل patients از /api/v1/patients/5</summary>
    private static string? ExtractEntityName(PathString path)
    {
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: >= 3 } ? parts[2] : null;
    }

    /// <summary>استخراج شناسه از انتهای مسیر</summary>
    private static long? ExtractId(PathString path)
    {
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: > 0 }) return null;
        return long.TryParse(parts[^1], out var id) ? id : null;
    }

    /// <summary>برش متن به سقف مجاز — جلوگیری از باد کردن ستون‌های لاگ</summary>
    private static string? Truncate(string? value) =>
        value is null || value.Length <= MaxValuesLength
            ? value
            : value[..MaxValuesLength] + "… (بریده شد)";

    /// <summary>
    /// خواندن وضعیت فعلی سطر هدف از دیتابیس «پیش از» اجرای هندلر —
    /// چرخهٔ بیست‌وششم (گزارش هجدهم مالک): بخش «مقادیر قبلی» جزئیات لاگ
    /// عملیات همیشه خالی بود چون هیچ‌جا OldValues پر نمی‌شد.
    /// روش: نگاشت نام موجودیت مسیر (مثل users یا insurance-companies) به
    /// جدول متناظر در مدل EF، سپس SELECT TOP 1 * با ADO خام (بدون ورود به
    /// ChangeTracker تا سطرهای هندلر آلوده نشوند) و ساخت JSON ستون/مقدار
    /// با حذف ستون‌های حساس (هش رمز، توکن، …) و ستون‌های دودویی (RowVersion).
    /// </summary>
    private static string? TryCaptureOldValues(ClinicProDbContext dbContext, PathString path)
    {
        try
        {
            var entitySegment = ExtractEntityName(path);
            var id = ExtractId(path);
            if (string.IsNullOrEmpty(entitySegment) || id is null)
                return null;

            // نرمال‌سازی نام: حذف جداکننده‌ها و «s» جمع — insurance-companies → insurancecompanie
            var normalized = entitySegment.Replace("-", "", StringComparison.OrdinalIgnoreCase)
                .Replace("_", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('s', 'S')
                .ToLowerInvariant();

            // یافتن جدول متناظر از مدل EF — با مقایسهٔ نام جدول یا نام نوع موجودیت
            string? tableName = null;
            string? schema = null;
            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                var table = entityType.GetTableName();
                if (table is null) continue;

                var candidates = new[]
                {
                    table,
                    entityType.ClrType.Name
                };
                foreach (var candidate in candidates)
                {
                    var candidateNormalized = candidate
                        .Replace("-", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("_", "", StringComparison.OrdinalIgnoreCase)
                        .TrimEnd('s', 'S')
                        .ToLowerInvariant();
                    if (candidateNormalized != normalized) continue;
                    tableName = table;
                    schema = entityType.GetSchema();
                    break;
                }

                if (tableName is not null) break;
            }

            if (tableName is null)
                return null; // مسیر به موجودیت مشخصی نگاشت نشد — بدون مقادیر قبلی

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
