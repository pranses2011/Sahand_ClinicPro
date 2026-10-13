namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// گارد یکسان‌سازی صفحه‌بندی — چرخهٔ ۶۲ نقشهٔ راه (P-03).
/// </summary>
/// <remarks>
/// «صفحه‌بندی اجباری در endpointهای پرترافیک»: دو ریشهٔ خرابی در درخواست‌های
/// دست‌ساز/معیوب:
///   ۱) صفحه/اندازهٔ صفر یا منفی → Skip منفی → استثنای ترجمهٔ EF؛
///   ۲) اندازهٔ خیلی بزرگ (مثلاً ۱۰۰٬۰۰۰) → یک پاسخ چند‌مگابایتی و قفل
///      شبکه/حافظهٔ کلاینت — همان «بار شبکه»ای که نقشهٔ راه هدف گرفت.
/// این کلاس در گرداننده‌ها/کنترلرها به‌کار می‌رود تا هر فهرست، پیش از
/// Skip/Take از همین مرزهای یکسان بگذرد؛ سقف پیش‌فرض ۲۰۰ ردیف است
/// (بزرگ‌ترین جدول‌های برنامه در ۲۰۰ ردیفِ تازه‌تر نیاز کاربر را پاس می‌کند).
/// </remarks>
public static class PagingGuard
{
    /// <summary>سقف پیش‌فرض اندازهٔ صفحه — هر endpoint می‌تواند سقف خود را پایین‌تر بگذارد</summary>
    public const int DefaultMaxPageSize = 200;

    /// <summary>حداقل شمارهٔ صفحه معتبر</summary>
    public const int MinPageNumber = 1;

    /// <summary>
    /// یکسان‌سازی شمارهٔ صفحه — کمتر از ۱ → ۱.
    /// </summary>
    public static int Page(int page) => page < MinPageNumber ? MinPageNumber : page;

    /// <summary>
    /// یکسان‌سازی اندازهٔ صفحه — کمتر از ۱ → پیش‌فرض ۲۰؛ بیشتر از سقف → سقف.
    /// </summary>
    public static int Size(int pageSize, int max = DefaultMaxPageSize)
    {
        if (pageSize < 1) return 20;
        return pageSize > max ? max : pageSize;
    }

    /// <summary>
    /// یکسان‌سازی زوج صفحه/اندازه در یک فراخوانی — خروجی آمادهٔ Skip/Take.
    /// </summary>
    public static (int Page, int Size) Clamp(int page, int pageSize, int max = DefaultMaxPageSize)
        => (Page(page), Size(pageSize, max));

    /// <summary>
    /// محاسبهٔ افست Skip — همیشه نامنفی (پس از Clamp نیازی به گارد نیست ولی
    /// برای فراخواننده‌های مستقیم بدون Clamp ایمن است).
    /// </summary>
    public static int Skip(int page, int pageSize) => Math.Max(0, (Page(page) - 1) * Math.Max(1, pageSize));
}
