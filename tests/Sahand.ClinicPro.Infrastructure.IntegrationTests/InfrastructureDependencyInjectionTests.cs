using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های یکپارچگی ثبت سرویس‌های لایه زیرساخت — گام ۱.
/// صحت امضای متد AddInfrastructure را تأیید می‌کند؛ ثبت‌های واقعی
/// (DbContext، ریپازیتوری، سرویس‌های امنیتی) در گام ۳ و ۴ اضافه و
/// تست‌های کامل در گام ۳۰ نوشته می‌شوند.
/// </summary>
public class InfrastructureDependencyInjectionTests
{
    /// <summary>
    /// متد AddInfrastructure باید همان نمونه کالکشن سرویس را برگرداند
    /// تا زنجیره ثبت سرویس‌ها در Program.cs ممکن بماند.
    /// </summary>
    [Fact]
    public void AddInfrastructure_Should_Return_The_Same_ServiceCollection()
    {
        // آماده‌سازی: کالکشن سرویس و پیکربندی ساختگی
        var services = new ServiceCollection();
        var configurationMock = new Mock<IConfiguration>();

        // عملیات: ثبت سرویس‌های زیرساخت
        var result = services.AddInfrastructure(configurationMock.Object);

        // نتیجه مورد انتظار: خروجی همان نمونه ورودی باشد
        result.Should().BeSameAs(services,
            because: "قرارداد متدهای الحاقی ثبت سرویس است");
    }
}
