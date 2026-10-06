using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Billing.Shifts;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests.Billing;

/// <summary>
/// چرخه سیزدهم (B2): بستن شیفت صندوق — اختلاف‌سنجی سیستم↔شمارش فیزیکی + رسید
/// </summary>
public class CloseShiftCommandTests
{
    private static (CloseShiftCommandHandler Handler, ClinicProDbContext Ctx) MakeHandler()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"shift-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);

        static Mock<IRepository<T>> Repo<T>(ClinicProDbContext c) where T : Sahand.ClinicPro.Domain.Common.BaseEntity
        {
            var mock = new Mock<IRepository<T>>();
            mock.Setup(r => r.QueryableNoTracking).Returns(c.Set<T>().AsNoTracking());
            mock.Setup(r => r.QueryableTracking).Returns(c.Set<T>());
            mock.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns<T, CancellationToken>((e, _) => { c.Set<T>().Add(e); return Task.FromResult(e); });
            return mock;
        }

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<CashRegister>()).Returns(Repo<CashRegister>(ctx).Object);
        uow.Setup(u => u.Repository<Payment>()).Returns(Repo<Payment>(ctx).Object);
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async _ => await ctx.SaveChangesAsync(_) > 0);

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(c => c.UserId).Returns(7L);
        current.SetupGet(c => c.Username).Returns("صندوقدار تست");

        return (new CloseShiftCommandHandler(uow.Object, current.Object), ctx);
    }

    private static Payment Pay(long id, decimal amount, PaymentMethod method, DateTime at) => new()
    {
        Id = id,
        Amount = amount,
        Method = method,
        PaymentDate = at
    };

    /// <summary>بستن با مطابقت کامل — اختلاف صفر و وضعیت سبز</summary>
    [Fact]
    public async Task CloseShift_MatchingCount_ZeroDifference()
    {
        var (handler, ctx) = MakeHandler();
        var open = new CashRegister
        {
            Id = 1, RegisterName = "صندوق اصلی",
            OpenedAt = DateTime.UtcNow.AddHours(-8), Status = CashRegisterStatus.Open
        };
        ctx.CashRegisters.Add(open);
        ctx.Payments.AddRange(
            Pay(10, 400_000m, PaymentMethod.Cash, DateTime.UtcNow.AddHours(-7)),
            Pay(11, 600_000m, PaymentMethod.Card, DateTime.UtcNow.AddHours(-6)),
            Pay(12, 200_000m, PaymentMethod.Online, DateTime.UtcNow.AddHours(-5)));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CloseShiftCommand(new CloseShiftDto(400_000m, 600_000m, null, null)),
            CancellationToken.None)).Value;

        result.NetDifference.Should().Be(0m);
        result.DifferenceState.Should().Contain("مطابقت");
        result.DifferenceColor.Should().Be("#2E7D32");
        result.ReceiptNumber.Should().StartWith("SHR-");
        result.PaymentCount.Should().Be(3);
    }

    /// <summary>کم‌شمارش نقد — اختلاف منفی و وضعیت قرمز</summary>
    [Fact]
    public async Task CloseShift_UnderCount_ShowsShortage()
    {
        var (handler, ctx) = MakeHandler();
        ctx.CashRegisters.Add(new CashRegister
        {
            Id = 2, RegisterName = "صندوق", OpenedAt = DateTime.UtcNow.AddHours(-8),
            Status = CashRegisterStatus.Open
        });
        ctx.Payments.Add(Pay(20, 500_000m, PaymentMethod.Cash, DateTime.UtcNow.AddHours(-7)));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CloseShiftCommand(new CloseShiftDto(450_000m, 0m, null, null)),
            CancellationToken.None)).Value;

        result.CashDifference.Should().Be(-50_000m);
        result.DifferenceState.Should().Contain("کم‌شمارش");
        result.DifferenceColor.Should().Be("#C62828");
    }

    /// <summary>اضافه‌شمارش — وضعیت زرد</summary>
    [Fact]
    public async Task CloseShift_OverCount_ShowsSurplus()
    {
        var (handler, ctx) = MakeHandler();
        ctx.CashRegisters.Add(new CashRegister
        {
            Id = 3, RegisterName = "صندوق", OpenedAt = DateTime.UtcNow.AddHours(-8),
            Status = CashRegisterStatus.Open
        });
        ctx.Payments.Add(Pay(30, 300_000m, PaymentMethod.Cash, DateTime.UtcNow.AddHours(-7)));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CloseShiftCommand(new CloseShiftDto(310_000m, 0m, null, null)),
            CancellationToken.None)).Value;

        result.NetDifference.Should().Be(10_000m);
        result.DifferenceState.Should().Contain("اضافه‌شمارش");
        result.DifferenceColor.Should().Be("#F9A825");
    }

    /// <summary>پس از بستن — صندوق Closed با شمارش‌ها و رسید ثبت می‌شود</summary>
    [Fact]
    public async Task CloseShift_PersistsRegisterState()
    {
        var (handler, ctx) = MakeHandler();
        ctx.CashRegisters.Add(new CashRegister
        {
            Id = 4, RegisterName = "صندوق", OpenedAt = DateTime.UtcNow.AddHours(-8),
            Status = CashRegisterStatus.Open
        });
        ctx.Payments.AddRange(
            Pay(40, 100_000m, PaymentMethod.Cash, DateTime.UtcNow.AddHours(-7)),
            Pay(41, 250_000m, PaymentMethod.Card, DateTime.UtcNow.AddHours(-6)));
        await ctx.SaveChangesAsync();

        await handler.Handle(
            new CloseShiftCommand(new CloseShiftDto(100_000m, 250_000m, 9L, "تحویل به شیفت عصر")),
            CancellationToken.None);

        var reg = await ctx.CashRegisters.AsNoTracking().FirstAsync(r => r.Id == 4);
        reg.Status.Should().Be(CashRegisterStatus.Closed);
        reg.ClosedAt.Should().NotBeNull();
        reg.CountedCash.Should().Be(100_000m);
        reg.CountedCard.Should().Be(250_000m);
        reg.ReceiptNumber.Should().NotBeNull();
        reg.HandoverToUserId.Should().Be(9L);
        reg.HandoverNote.Should().Be("تحویل به شیفت عصر");
        reg.ClosedByName.Should().Be("صندوقدار تست");
    }

    /// <summary>بدون صندوق باز — صندوق ضمنی ساخته و بسته می‌شود</summary>
    [Fact]
    public async Task CloseShift_NoOpenRegister_CreatesImplicit()
    {
        var (handler, ctx) = MakeHandler();
        ctx.Payments.Add(Pay(50, 80_000m, PaymentMethod.Cash, DateTime.UtcNow.AddHours(-4)));
        await ctx.SaveChangesAsync();

        var result = (await handler.Handle(
            new CloseShiftCommand(new CloseShiftDto(80_000m, 0m, null, null)),
            CancellationToken.None)).Value;

        result.SystemTotal.Should().Be(80_000m);
        (await ctx.CashRegisters.CountAsync()).Should().Be(1);
        var reg = await ctx.CashRegisters.FirstAsync();
        reg.Status.Should().Be(CashRegisterStatus.Closed);
    }

    /// <summary>تاریخچه — شیفت‌های بسته‌شده با وضعیت برمی‌گردند</summary>
    [Fact]
    public async Task GetShifts_ReturnsClosedShifts()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"shifts-hist-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);
        ctx.CashRegisters.AddRange(
            new CashRegister
            {
                RegisterName = "صندوق", Status = CashRegisterStatus.Closed,
                ClosedAt = DateTime.UtcNow, ReceiptNumber = "SHR-0001", Difference = 0m
            },
            new CashRegister
            {
                RegisterName = "صندوق", Status = CashRegisterStatus.Open,
                OpenedAt = DateTime.UtcNow.AddHours(-8)
            });
        await ctx.SaveChangesAsync();

        var uow = new Mock<IUnitOfWork>();
        var repo = new Mock<IRepository<CashRegister>>();
        repo.Setup(r => r.QueryableNoTracking).Returns(ctx.Set<CashRegister>().AsNoTracking());
        uow.Setup(u => u.Repository<CashRegister>()).Returns(repo.Object);

        var handler = new GetShiftsQueryHandler(uow.Object);
        var result = (await handler.Handle(new GetShiftsQuery(), CancellationToken.None)).Value;

        result.Should().HaveCount(1);
        result[0].ReceiptNumber.Should().Be("SHR-0001");
    }
}
