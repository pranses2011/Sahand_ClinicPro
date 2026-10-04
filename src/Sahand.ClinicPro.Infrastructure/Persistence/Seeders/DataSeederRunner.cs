using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// اجراکننده بذرها — همه IDataSeederها را به ترتیب اجرا می‌کند
/// خطای هر بذر ثبت می‌شود اما راه‌اندازی برنامه را متوقف نمی‌کند
/// </summary>
public static class DataSeederRunner
{
    /// <summary>اجرای همه بذرهای ثبت‌شده به ترتیب Order</summary>
    public static async Task RunAllAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClinicProDbContext>>();
        var sp = scope.ServiceProvider;

        // ClinicProDbContext صرفاً برای گرفتن نوع لاگر استفاده شده است
        var seeders = sp.GetServices<IDataSeeder>().OrderBy(s => s.Order).ToList();

        foreach (var seeder in seeders)
        {
            try
            {
                await seeder.SeedAsync(cancellationToken);
                logger.LogInformation("بذرافشان {Seeder} با موفقیت اجرا شد", seeder.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "خطا در اجرای بذرافشان {Seeder}", seeder.GetType().Name);
            }
        }
    }
}
