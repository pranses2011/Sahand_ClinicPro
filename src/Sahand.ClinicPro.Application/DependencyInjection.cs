using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Sahand.ClinicPro.Application;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه کاربرد (Application) در کانتینر تزریق وابستگی.
/// این متد باید در تمام پروژه‌های میزبان (API و LicensePortal) فراخوانی شود.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت سرویس‌های لایه کاربرد شامل MediatR (الگوی CQRS)،
    /// AutoMapper (نگاشت اشیاء) و FluentValidation (اعتبارسنجی).
    /// </summary>
    /// <param name="services">کالکشن سرویس‌های برنامه</param>
    /// <returns>همان کالکشن برای زنجیره‌سازی فراخوانی‌ها</returns>
    /// <remarks>
    /// تمام سرویس‌ها به‌صورت خودکار از اسمبلی جاری شناسایی می‌شوند؛
    /// بنابراین هر Handler یا Validator یا Profile جدید، بدون هیچ
    /// تغییری در این فایل، به‌صورت خودکار ثبت خواهد شد (گام‌های ۵ به بعد).
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // اسمبلی جاری — محل قرارگیری تمام Command/Query/Validator/Profile ها
        var assembly = Assembly.GetExecutingAssembly();

        // ثبت MediatR — فرستنده پیام‌ها + خط لوله رفتارها
        // (اعتبارسنجی، لاگ‌گذاری، تراکنش و عملکرد در گام ۵ اضافه می‌شوند)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // ثبت AutoMapper — پروفایل‌های نگاشت DTO ↔ موجودیت
        services.AddAutoMapper(assembly);

        // ثبت FluentValidation — اعتبارسنج‌های فارسی برای هر Command/Query
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
