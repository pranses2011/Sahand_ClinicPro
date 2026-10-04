# -*- coding: utf-8 -*-
"""گام ۲۹ — امنیت‌پردازی: میدلور لاگ عملیات + سرآیندهای امنیتی + بهینه‌سازی"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content):
    full = os.path.join(API, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ میدلور لاگ عملیات ═══════════
write("Middleware/AuditLogMiddleware.cs", """
using System.Security.Claims;
using System.Text.Json;
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
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

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

                var sanitized = SanitizeBody(requestBody);

                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Username = context.User.Identity?.Name,
                    ActionType = action,
                    EntityName = ExtractEntityName(context.Request.Path),
                    EntityId = ExtractId(context.Request.Path),
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
}

/// <summary>بسط مسیر</summary>
internal static class AuditPathExtensions
{
    /// <summary>شروع با — بدون حساسیت حروف</summary>
    public static bool StartsWithsWith(this PathString path, string prefix) =>
        path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase);
}
""")

# ═══════════ سرآیندهای امنیتی + فشرده‌سازی ═══════════
write("Middleware/SecurityHeadersMiddleware.cs", """
namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور سرآیندهای امنیتی — محافظت استاندارد وب
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>سازنده با نقطه بعدی</summary>
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    /// <summary>افزودن سرآیندهای امنیتی به پاسخ</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            context.Response.Headers.XContentTypeOptions = "nosniff";

        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            context.Response.Headers.XFrameOptions = "DENY";

        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

        if (!context.Response.Headers.ContainsKey("X-Powered-By"))
            context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}
""")

print("✅ گام ۲۹ — بخش ۱ (میدلورها) کامل شد")
