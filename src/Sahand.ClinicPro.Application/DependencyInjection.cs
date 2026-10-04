using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Common.Behaviors;

namespace Sahand.ClinicPro.Application;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه کاربرد (Application) در کانتینر تزریق وابستگی.
/// این متد باید در تمام پروژه‌های میزبان (API و LicensePortal) فراخوانی شود.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت سرویس‌های لایه کاربرد شامل MediatR (الگوی CQRS)،
    /// AutoMapper (نگاشت اشیاء)، FluentValidation (اعتبارسنجی)
    /// و چهار رفتار خط لوله (لاگ، عملکرد، اعتبارسنجی، تراکنش).
    /// </summary>
    /// <param name="services">کالکشن سرویس‌های برنامه</param>
    /// <returns>همان کالکشن برای زنجیره‌سازی فراخوانی‌ها</returns>
    /// <remarks>
    /// ترتیب ثبت رفتارها مهم است — به ترتیب اجرای:
    /// لاگ‌گذاری سپس عملکرد سپس اعتبارسنجی و در پایان تراکنش.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // اسمبلی جاری — محل قرارگیری تمام Command/Query/Validator/Profile ها
        var assembly = Assembly.GetExecutingAssembly();

        // ثبت MediatR — فرستنده پیام‌ها با خط لوله رفتارها
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // ترتیب اجرای رفتارها: لاگ سپس عملکرد سپس اعتبارسنجی و در پایان تراکنش
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // ثبت AutoMapper — پروفایل‌های نگاشت DTO و موجودیت
        services.AddAutoMapper(assembly);

        // ثبت FluentValidation — اعتبارسنج‌های فارسی برای هر Command/Query
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
