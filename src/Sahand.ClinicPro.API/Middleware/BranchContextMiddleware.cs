using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور بافت شعبه — گام U17 چندشعبه‌ای:
/// شناسه شعبه از هدر X-Branch-Id خوانده و روی DbContext تنظیم می‌شود
/// تا فیلتر سراسری پرس‌وجوها داده‌های همان شعبه را برگرداند
/// </summary>
public class BranchContextMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>سازنده با delegate بعدي</summary>
    public BranchContextMiddleware(RequestDelegate next) => _next = next;

    /// <summary>اجرای میدلور — تنظیم شعبه جاری روی DbContext از سرویس کاربر فعلی</summary>
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        if (currentUser.BranchId is { } branchId)
        {
            // DbContext scoped است — همان نمونه‌ای که کنترلر تزریق می‌شود
            var dbContext = context.RequestServices
                .GetService(typeof(ClinicProDbContext)) as ClinicProDbContext;
            if (dbContext is not null)
                dbContext.CurrentBranchId = branchId;
        }

        await _next(context);
    }
}
