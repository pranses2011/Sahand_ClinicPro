using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sahand.ClinicPro.Infrastructure;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه زیرساخت (Infrastructure)
/// در کانتینر تزریق وابستگی برنامه.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت سرویس‌های زیرساخت: DbContext اصلی، ریپازیتوری‌ها، UnitOfWork،
    /// سرویس‌های امنیتی، پیامک، یکپارچه‌سازی‌های دولتی و کارهای پس‌زمینه.
    /// </summary>
    /// <param name="services">کالکشن سرویس‌های برنامه</param>
    /// <param name="configuration">پیکربندی برنامه (appsettings و غیره)</param>
    /// <returns>همان کالکشن برای زنجیره‌سازی فراخوانی‌ها</returns>
    /// <remarks>
    /// در گام ۱ این متد به‌صورت اسکلت آماده شده است؛ ثابت‌های اصلی
    /// مطابق نقشه ۳۱ گامی در گام‌های بعدی به آن اضافه می‌شوند.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 📌 گام ۳: ثبت ClinicProDbContext + ریپازیتوری‌ها + UnitOfWork
        //          + اینترسپتورهای Audit/SoftDelete + Seederها
        // 📌 گام ۴: ثبت سرویس‌های احراز هویت (JWT + BCrypt) و RBAC
        // 📌 گام ۲۳: ثبت کارخانه پنل‌های پیامک + صف پردازش Hangfire
        // 📌 گام ۲۴: ثبت سرویس‌های اتصال به سپاس، سیب، ناب، سینا و نسخه الکترونیک
        // 📌 گام ۲۵: ثبت سرویس اعتبارسنجی لایسنس + Heartbeat روزانه
        return services;
    }
}
