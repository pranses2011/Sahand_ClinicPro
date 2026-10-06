using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// نگاشتگر‌های Mapperly (تولید کد زمان کامپایل)، FluentValidation (اعتبارسنجی)
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

        // ثبت نگاشتگر‌های Mapperly — تولید کد در زمان کامپایل (گام V11)
        services.AddScoped<Common.Mappings.PatientMapper>();
        services.AddScoped<Common.Mappings.DoctorMapper>();

        // موتور تسویه حساب بیمه — چرخه ۱۳ (درخواست ۲-۱)
        services.AddScoped<Features.Insurance.Fractionment.FractionmentCalculator>();

        // اتصال بلادرنگ — پیش‌فرض بی‌اثر؛ در API با SignalRRealtimeNotifier بازنویسی می‌شود (گام R1)
        services.TryAddScoped<Interfaces.IRealtimeNotifier, Interfaces.NullRealtimeNotifier>();

        // ثبت FluentValidation — اعتبارسنج‌های فارسی برای هر Command/Query
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
