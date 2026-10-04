using Hangfire.Dashboard;

namespace Sahand.ClinicPro.API;

/// <summary>
/// فیلتر دسترسی داشبورد Hangfire — فقط کاربران احراز هویت‌شده
/// در گام ۲۹ به نقش ادمین محدود می‌شود
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    /// <summary>اجازه فقط برای کاربر احراز هویت‌شده</summary>
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated ?? false;
    }
}
