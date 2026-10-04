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
