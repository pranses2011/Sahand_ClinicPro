namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// قرارداد بذرافشان داده اولیه — همه بذرها هنگام راه‌اندازی به ترتیب اجرا می‌شوند
/// </summary>
public interface IDataSeeder
{
    /// <summary>ترتیب اجرای بذر</summary>
    int Order { get; }

    /// <summary>اجرای بذرافشان — باید تکرارپذیر باشد</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
