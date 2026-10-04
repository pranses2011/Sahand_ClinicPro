using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Identity;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Interceptors;
using Sahand.ClinicPro.Infrastructure.Persistence.Repositories;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;
using Sahand.ClinicPro.Infrastructure.Services;

namespace Sahand.ClinicPro.Infrastructure;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه زیرساخت در کانتینر تزریق وابستگی
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت DbContext اصلی با SQL Server، اینترسپتورها، ریپازیتوری‌ها و بذرها
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // رشته اتصال — از appsettings یا متغیر محیطی
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("رشته اتصال DefaultConnection یافت نشد");

        // DbContext با اینترسپتورهای حسابرسی و حذف نرم
        services.AddDbContext<ClinicProDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.CommandTimeout(60);
            });
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        // اینترسپتورها و سرویس کاربر فعلی
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // واحد کار و ریپازیتوری‌ها
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();

        // سرویس‌های انتزاعی Application پیاده‌سازی‌شده در زیرساخت
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddSingleton<IDateTimeService, Services.DateTimeService>();
        services.AddSingleton<IEncryptionService, Services.Security.EncryptionService>();

        // بذرهای داده اولیه
        services.AddTransient<IDataSeeder, RoleSeeder>();
        services.AddTransient<IDataSeeder, PermissionSeeder>();
        services.AddTransient<IDataSeeder, AdminSeeder>();
        services.AddTransient<IDataSeeder, BasicDataSeeder>();
        services.AddTransient<IDataSeeder, AccountingAccountSeeder>();

        return services;
    }

    /// <summary>
    /// اجرای بذرها پس از راه‌اندازی — خطاها ثبت می‌شوند اما برنامه را متوقف نمی‌کنند
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            await DataSeederRunner.RunAllAsync(serviceProvider);
        }
        catch
        {
            // در محیط بدون دیتابیس (تست یا CI) بی‌صدا عبور می‌کنیم —
            // خطای واقعی توسط لاگر داخل RunAllAsync ثبت شده است
        }
    }
}
