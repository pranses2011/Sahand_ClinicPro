namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// هلپر نشانی سرور لایسنس — چرخه هجدهم (گزارش دوازدهم مالک).
/// خطای «نشانی پنل لایسنس تنظیم نشده است — License:PortalUrl» ریشهٔ
/// خالی‌بودن تنظیم بود؛ از این پس نشانی پیش‌فرض رسمی محصول در کد جایگزین
/// می‌شود و کاربر می‌تواند از صفحهٔ فعال‌سازی آن را عوض کند.
/// منطق خالص و بدون وابستگی است تا با تست واحد گارد شود.
/// </summary>
public static class LicensePortalHelper
{
    /// <summary>نشانی رسمی سرور لایسنس محصول (ریشه)</summary>
    public const string DefaultPortalRoot = "https://clinic-license.ea-fixer.ir";

    /// <summary>نشانی کامل endpoint عمومی سرور لایسنس (api.php)</summary>
    public const string DefaultPortalApiUrl = DefaultPortalRoot + "/api.php";

    /// <summary>
    /// نرمال‌سازی نشانی واردشدهٔ کاربر به endpoint کامل api.php —
    /// اگر کاربر فقط ریشه را بدهد (مثل clinic-license.ea-fixer.ir)،
    /// «/api.php» به آن الصاق می‌شود؛ اگر نشانی کامل به api.php داده
    /// شود دست‌نخورده برمی‌گردد.
    /// </summary>
    /// <param name="input">نشانی ورودی — خالی مجاز است (پیش‌فرض برمی‌گردد)</param>
    public static string NormalizeApiUrl(string? input)
    {
        var url = (input ?? string.Empty).Trim();
        if (url.Length == 0)
            return DefaultPortalApiUrl;

        // بدون طرح؟ — https ضمیمه می‌شود
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        // ریشه داده شده است نه endpoint — api.php الصاق می‌شود
        if (!url.EndsWith(".php", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith(".asp", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
        {
            url = url.TrimEnd('/') + "/api.php";
        }

        return url;
    }

    /// <summary>آیا نشانی ورودی یک نشانی http/https معتبر است؟</summary>
    public static bool IsValidHttpUrl(string? input)
    {
        var url = (input ?? string.Empty).Trim();
        if (url.Length == 0) return false;
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
