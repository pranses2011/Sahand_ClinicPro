using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Lookups;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Billing;

/// <summary>
/// رگرسیون خطای مالک روی SQL Server واقعی: «The INSERT statement conflicted
/// with the FOREIGN KEY constraint FK_Tariffs_Services_ServiceId» هنگام ثبت
/// خدمت جدید. تست‌های InMemory این خطا را هرگز نمی‌بینند چون آن پروایدر کلید
/// خارجی را کنترل نمی‌کند؛ این تست روی SQLite رابطه‌ای (با FK فعال) هندلر
/// ساخت خدمت را اجرا می‌کند تا درج تعرفه حتماً با شناسهٔ واقعی خدمت انجام شود.
/// </summary>
public class ServiceTariffCreateSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ClinicProDbContext _ctx;
    private readonly CreateServiceTariffCommandHandler _handler;

    public ServiceTariffCreateSqliteTests()
    {
        // اتصال بازِ درون‌حافظه‌ای — تا پایان تست زنده می‌ماند
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlite(_connection)
            .Options;
        _ctx = new ClinicProDbContext(options);
        _ctx.Database.EnsureCreated();
        var mediator = new Mock<MediatR.IMediator>().Object;
        _handler = new CreateServiceTariffCommandHandler(new UnitOfWork(_ctx, mediator));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateService_OnRelationalDb_Should_LinkTariffToRealServiceId()
    {
        // دو ساخت پیاپی بدون کد — دقیقاً سناریوی گزارش مالک
        var first = await _handler.Handle(
            new CreateServiceTariffCommand("خدمت آزمایشی یک", null, 150000m, 0m, 100m, null, 20000m),
            CancellationToken.None);
        first.IsSuccess.Should().BeTrue(
            because: $"ساخت خدمت روی دیتابیس رابطه‌ای باید موفق باشد — خطا: {first.Error?.Message}");
        first.Value.Should().BeGreaterThan(0, because: "شناسهٔ واقعی باید از دیتابیس برگشته باشد");

        var second = await _handler.Handle(
            new CreateServiceTariffCommand("خدمت آزمایشی دو", null, 250000m, 0m, 100m, null, 30000m),
            CancellationToken.None);
        second.IsSuccess.Should().BeTrue(
            because: $"ساخت خدمت دوم هم باید موفق باشد — خطا: {second.Error?.Message}");

        // ردیف‌های تعرفه باید دقیقاً به همان خدمت‌های ساخته‌شده وصل باشند
        var tariffs = await _ctx.Set<Tariff>().AsNoTracking().ToListAsync();
        tariffs.Should().HaveCount(2);
        tariffs.Select(t => t.ServiceId).Should().BeEquivalentTo(new[] { first.Value, second.Value },
            because: "تعرفه نباید با شناسهٔ موقت/ناموجود ذخیره شود (ریشهٔ خطای کلید خارجی)");

        var services = await _ctx.Set<Service>().AsNoTracking().ToListAsync();
        services.Should().HaveCount(2);
        services.Select(s => s.ServiceCode).Distinct().Should().HaveCount(2,
            because: "کدهای خودکار باید یکتا باشند");
    }

    [Fact]
    public async Task RelationalDb_Should_EnforceTariffForeignKey_SanityCheck()
    {
        // اثبات اینکه این محیط تست واقعاً کلید خارجی را کنترل می‌کند؛ بدون این
        // گارد، تست بالا روی پروایدر سهل‌گیر می‌توانست همیشه سبزِ کاذب باشد
        _ctx.Set<Tariff>().Add(new Tariff { ServiceId = 999999, Year = 1405, Price = 1000m });
        var act = async () => await _ctx.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>(
            because: "درج تعرفه با خدمت ناموجود باید با خطای کلید خارجی رد شود");
    }
}
