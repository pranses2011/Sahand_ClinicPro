using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Application.Features.Pharmacy;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های گرداننده‌های انبارگردانی داروخانه — گام P2 (چرخه ششم)
/// چرخه کامل: شروع با عکس فوری ← ثبت شمارش ← تکمیل ← تأیید و اعمال مغایرت
/// </summary>
public class StocktakingHandlerTests
{
    /// <summary>ساخت UnitOfWork روی دیتابیس درون‌حافظه‌ای</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"StocktakingDb_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), context);
    }

    /// <summary>کاربر جاری شبیه‌سازی‌شده</summary>
    private static ICurrentUserService CurrentUser(long userId = 7)
        => Mock.Of<ICurrentUserService>(s => s.UserId == userId);

    /// <summary>کاشت یک دارو + سه بچ موجودی نمونه</summary>
    private static async Task<List<PharmacyInventory>> SeedInventoryAsync(ClinicProDbContext context)
    {
        var med = new Medication { GenericName = "Acetaminophen", BrandName = "استامینوفن کدئین" };
        context.Medications.Add(med);
        await context.SaveChangesAsync();

        var inventories = new List<PharmacyInventory>
        {
            new() { MedicationId = med.Id, BatchNumber = "B-100", Quantity = 50m, ReorderLevel = 10m, SellPrice = 5_000m },
            new() { MedicationId = med.Id, BatchNumber = "B-101", Quantity = 30m, ReorderLevel = 10m, SellPrice = 5_000m },
            new() { MedicationId = med.Id, BatchNumber = "B-102", Quantity = 0m, ReorderLevel = 5m, SellPrice = 5_000m }
        };
        context.PharmacyInventory.AddRange(inventories);
        await context.SaveChangesAsync();
        return inventories;
    }

    /// <summary>اجرای کل چرخه تا وضعیت تکمیل‌شده با یک شمارش اختصاصی</summary>
    private static async Task<long> RunToCompletedAsync(UnitOfWork uow, long inventoryId, decimal counted)
    {
        var start = await new StartStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new StartStocktakingCommand("تست"), CancellationToken.None);
        start.IsSuccess.Should().BeTrue("شروع جلسه باید موفق باشد");

        var session = await uow.Repository<StockTaking>().QueryableNoTracking
            .Include(s => s.Items).FirstAsync();

        await new RecordCountCommandHandler(uow).Handle(
            new RecordCountCommand(session.Id, session.Items.First(i => i.InventoryId == inventoryId).Id, counted, null),
            CancellationToken.None);

        await new FinalizeStocktakingCommandHandler(uow).Handle(
            new FinalizeStocktakingCommand(session.Id), CancellationToken.None);

        return session.Id;
    }

    /* ═══ شروع جلسه ═══ */

    [Fact]
    public async Task Start_CreatesSession_WithInventorySnapshot()
    {
        var (uow, context) = CreateUnitOfWork();
        await SeedInventoryAsync(context);
        var handler = new StartStocktakingCommandHandler(uow, CurrentUser());

        var result = await handler.Handle(new StartStocktakingCommand("انبارگردانی پایان فصل"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("موجودی کافی برای شمارش وجود دارد");
        result.Value.TotalItems.Should().Be(3, "عکس فوری از هر سه بچ ثبت می‌شود");
        result.Value.TakingNumber.Should().StartWith("ST-");

        var session = await context.StockTakings.Include(s => s.Items).FirstAsync();
        session.Status.Should().Be(StockTakingStatus.InProgress);
        session.PerformedBy.Should().Be(7, "کاربر شروع‌کننده ثبت می‌شود");
        session.Items.Should().OnlyContain(i => i.SystemQuantity == 50m || i.SystemQuantity == 30m || i.SystemQuantity == 0m,
            "موجودی سیستم باید عکس فوری دقیق باشد");
        session.Items.Should().OnlyContain(i => !i.IsCounted, "هیچ قلمی هنوز شمارش نشده است");
    }

    [Fact]
    public async Task Start_Blocked_WhenAnotherSession_InProgress()
    {
        var (uow, context) = CreateUnitOfWork();
        await SeedInventoryAsync(context);
        var handler = new StartStocktakingCommandHandler(uow, CurrentUser());

        var first = await handler.Handle(new StartStocktakingCommand(null), CancellationToken.None);
        var second = await handler.Handle(new StartStocktakingCommand(null), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse("تک‌جلسه باز هم‌زمان مجاز است");
        second.Error.Code.Should().Be("OPEN_SESSION_EXISTS");
    }

    [Fact]
    public async Task Start_Fails_WhenNoInventoryExists()
    {
        var (uow, _) = CreateUnitOfWork();
        var handler = new StartStocktakingCommandHandler(uow, CurrentUser());

        var result = await handler.Handle(new StartStocktakingCommand(null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("بدون موجودی، انبارگردانی بی‌معناست");
        result.Error.Code.Should().Be("NO_INVENTORY");
    }

    /* ═══ ثبت شمارش و تکمیل ═══ */

    [Fact]
    public async Task RecordCount_UpdatesItem_WithCountedFlag()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        var session = await RunStartedSessionAsync(uow);

        var item = session.Items.First(i => i.InventoryId == invs[0].Id);
        var result = await new RecordCountCommandHandler(uow).Handle(
            new RecordCountCommand(session.Id, item.Id, 47m, "دو عدد شکسته"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await context.StockTakingItems.AsNoTracking().FirstAsync(i => i.Id == item.Id);
        updated.CountedQuantity.Should().Be(47m);
        updated.IsCounted.Should().BeTrue("پرچم شمارش فعال می‌شود");
        updated.Notes.Should().Be("دو عدد شکسته");
        updated.Difference.Should().Be(-3m, "شمارش ۴۷ در برابر سیستم ۵۰ = کسری ۳");
    }

    /// <summary>کمکی: شروع جلسه و بازگرداندن آن با اقلام دنبال‌شده</summary>
    private static async Task<StockTaking> RunStartedSessionAsync(UnitOfWork uow)
    {
        var start = await new StartStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new StartStocktakingCommand(null), CancellationToken.None);
        start.IsSuccess.Should().BeTrue();
        return await uow.Repository<StockTaking>().QueryableNoTracking.Include(s => s.Items).FirstAsync();
    }

    [Fact]
    public async Task RecordCount_Rejected_WhenSessionNotInProgress()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        var sessionId = await RunToCompletedAsync(uow, invs[0].Id, 50m);

        var session = await uow.Repository<StockTaking>().QueryableNoTracking.Include(s => s.Items)
            .FirstAsync(s => s.Id == sessionId);
        var item = session.Items.First(i => i.InventoryId == invs[0].Id);

        var result = await new RecordCountCommandHandler(uow).Handle(
            new RecordCountCommand(sessionId, item.Id, 40m, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("پس از تکمیل، ثبت شمارش مجاز نیست");
        result.Error.Code.Should().Be("NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Finalize_Rejected_WhenNothingCounted()
    {
        var (uow, context) = CreateUnitOfWork();
        await SeedInventoryAsync(context);
        var session = await RunStartedSessionAsync(uow);

        var result = await new FinalizeStocktakingCommandHandler(uow)
            .Handle(new FinalizeStocktakingCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("بدون هیچ شمارشی تکمیل بی‌معناست");
        result.Error.Code.Should().Be("NO_COUNTS");
    }

    /* ═══ تأیید و اعمال مغایرت ═══ */

    [Fact]
    public async Task Approve_AppliesDifference_AndCreatesAdjustmentTransaction()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        // سیستم ۵۰ → شمارش ۴۶ = کسری ۴
        var sessionId = await RunToCompletedAsync(uow, invs[0].Id, 46m);

        var result = await new ApproveStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new ApproveStocktakingCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdjustedItems.Should().Be(1, "فقط یک قلم مغایرت داشت");
        result.Value.TotalShortage.Should().Be(4m);
        result.Value.TotalSurplus.Should().Be(0m);

        // موجودی بچ اصلاح شده است
        var inventory = await context.PharmacyInventory.AsNoTracking().FirstAsync(i => i.Id == invs[0].Id);
        inventory.Quantity.Should().Be(46m, "موجودی به مقدار شمارش‌شده اصلاح می‌شود");

        // تراکنش اصلاح با علامت منفی ثبت شده است
        var txn = await context.PharmacyTransactions.AsNoTracking()
            .SingleAsync(t => t.ReferenceId == sessionId && t.TransactionType == PharmacyTransactionType.Adjustment);
        txn.Quantity.Should().Be(-4m, "کسری با علامت منفی");
        txn.InventoryId.Should().Be(invs[0].Id);
        txn.ReferenceType.Should().Be(PharmacyReferenceType.StockTaking);

        // جلسه تأیید شده است
        var session = await context.StockTakings.AsNoTracking().FirstAsync(s => s.Id == sessionId);
        session.Status.Should().Be(StockTakingStatus.Approved);
        session.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_CreatesPositiveTransaction_ForSurplus()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        // سیستم ۳۰ → شمارش ۳۵ = زیادندی ۵
        var sessionId = await RunToCompletedAsync(uow, invs[1].Id, 35m);

        var result = await new ApproveStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new ApproveStocktakingCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSurplus.Should().Be(5m);
        result.Value.TotalShortage.Should().Be(0m);

        var txn = await context.PharmacyTransactions.AsNoTracking()
            .SingleAsync(t => t.ReferenceId == sessionId && t.TransactionType == PharmacyTransactionType.Adjustment);
        txn.Quantity.Should().Be(5m, "زیادندی با علامت مثبت");
    }

    [Fact]
    public async Task Approve_Rejected_WhenSessionStillInProgress()
    {
        var (uow, context) = CreateUnitOfWork();
        await SeedInventoryAsync(context);
        var session = await RunStartedSessionAsync(uow);

        var result = await new ApproveStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new ApproveStocktakingCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("ابتدا باید تکمیل شود");
        result.Error.Code.Should().Be("NOT_COMPLETED");
    }

    [Fact]
    public async Task Approve_NoTransaction_WhenCountedEqualsSystem()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        // شمارش برابر سیستم → هیچ مغایرتی اعمال نمی‌شود
        var sessionId = await RunToCompletedAsync(uow, invs[0].Id, 50m);

        var result = await new ApproveStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new ApproveStocktakingCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdjustedItems.Should().Be(0, "مغایرت صفر = بدون اصلاح");
        (await context.PharmacyTransactions.CountAsync(t => t.ReferenceId == sessionId))
            .Should().Be(0, "تراکنشی برای قلم برابر ثبت نمی‌شود");
    }

    /* ═══ لغو جلسه ═══ */

    [Fact]
    public async Task Cancel_SoftDeletes_OnlyInProgressSession()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        var session = await RunStartedSessionAsync(uow);

        var cancel = await new CancelStocktakingCommandHandler(uow)
            .Handle(new CancelStocktakingCommand(session.Id), CancellationToken.None);
        cancel.IsSuccess.Should().BeTrue();

        var deleted = await context.StockTakings.IgnoreQueryFilters().FirstAsync(s => s.Id == session.Id);
        deleted.IsDeleted.Should().BeTrue("لغو = حذف نرم");

        // پس از لغو، شروع جلسه جدید مجاز است
        var restart = await new StartStocktakingCommandHandler(uow, CurrentUser())
            .Handle(new StartStocktakingCommand(null), CancellationToken.None);
        restart.IsSuccess.Should().BeTrue("قفل تک‌جلسه با لغو آزاد می‌شود");
    }

    [Fact]
    public async Task Cancel_Rejected_WhenSessionCompleted()
    {
        var (uow, context) = CreateUnitOfWork();
        var invs = await SeedInventoryAsync(context);
        var sessionId = await RunToCompletedAsync(uow, invs[0].Id, 45m);

        var result = await new CancelStocktakingCommandHandler(uow)
            .Handle(new CancelStocktakingCommand(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("جلسه تکمیل‌شده قابل لغو نیست");
        result.Error.Code.Should().Be("NOT_IN_PROGRESS");
    }
}
