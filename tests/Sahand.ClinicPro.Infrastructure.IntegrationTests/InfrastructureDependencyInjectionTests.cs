using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی ثبت سرویس‌های لایه زیرساخت — گام ۳.
/// ثبت DbContext و ریپازیتوری‌ها و UnitOfWork را در کانتینر واقعی بررسی می‌کند؛
/// تست‌های کامل دیتابیس در گام ۳۰ با SQL Server واقعی نوشته می‌شوند.
/// </summary>
public class InfrastructureDependencyInjectionTests
{
    /// <summary>
    /// ساخت پیکربندی درون‌حافظه‌ای با رشته اتصال آزمایشی
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=localhost;Database=SahandClinicProDb;Trusted_Connection=True;TrustServerCertificate=True"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    /// <summary>
    /// متد AddInfrastructure باید همان نمونه کالکشن سرویس را برگرداند
    /// تا زنجیره ثبت سرویس‌ها در Program.cs ممکن بماند.
    /// </summary>
    [Fact]
    public void AddInfrastructure_Should_Return_The_Same_ServiceCollection()
    {
        // آماده‌سازی: کالکشن سرویس و پیکربندی با رشته اتصال
        var services = new ServiceCollection();

        // عملیات: ثبت سرویس‌های زیرساخت
        var result = services.AddInfrastructure(BuildConfiguration());

        // نتیجه مورد انتظار: خروجی همان نمونه ورودی باشد
        result.Should().BeSameAs(services,
            because: "قرارداد متدهای الحاقی ثبت سرویس است");
    }

    /// <summary>
    /// ثبت DbContext و UnitOfWork و ریپازیتوری تخصصی بیمار باید در کانتینر ممکن باشد.
    /// </summary>
    [Fact]
    public void AddInfrastructure_Should_Register_Persistence_Services()
    {
        // آماده‌سازی: ثبت کامل سرویس‌ها مانند ترکیب برنامه (Application سپس Infrastructure)
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(BuildConfiguration());

        // عملیات: ساخت ارائه‌دهنده سرویس
        var provider = services.BuildServiceProvider();

        // نتیجه مورد انتظار: انواع کلیدی زیرساخت قابل رزولو باشند
        provider.GetRequiredService<ClinicProDbContext>().Should().NotBeNull(
            because: "DbContext اصلی باید ثبت شده باشد");
        provider.GetRequiredService<IUnitOfWork>().Should().NotBeNull(
            because: "واحد کار باید ثبت شده باشد");
        provider.GetRequiredService<IPatientRepository>().Should().NotBeNull(
            because: "ریپازیتوری تخصصی بیمار باید ثبت شده باشد");
    }
}
