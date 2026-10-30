using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;


namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست جامع جریان لوازم مصرفی — چرخهٔ ۴۶ (دو گزارش مالک هم‌زمان):
/// «افزودن دسته‌جمعی اقلام مصرفی درست کار نمی‌کند و موجودی اقلام جدید
/// بروز نمی‌شود» + «در پذیرش موجودی اقلام مصرفی کم نمی‌شود».
/// زنجیرهٔ کامل همان مسیر واقعی برنامه بازتولید می‌شود تا هر حلقه‌ای که
/// رفتارش با گزارش مالک ناسازگار است جداگانه مشخص شود:
///   تعریف قلم جدید → ورود دسته‌جمعی چند ردیف → فهرست موجودی (باید بروز باشد)
///   → مصرف در پذیرش (موجودی باید کم شود) → فهرست پذیرش (موجودی جدید).
/// دیتابیس درون‌حافظه‌ای است تا رفتار هندلرها مستقل از SQL Server سنجیده شود.
/// </summary>
public class Cycle46ConsumablesFlowTests
{
    /// <summary>ساخت واحد کار روی دیتابیس درون‌حافظه‌ای یکتا برای هر تست</summary>
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"ConsumablesFlow_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), context);
    }

    [Fact]
    public async Task FullFlow_BulkStockIn_ShowsInInventory_And_Consume_DecreasesIt()
    {
        // ═══ گام ۱ — تعریف سه قلم مصرفی جدید (مثل دکمهٔ «قلم جدید» صفحهٔ مصرفی‌ها) ═══
        var (uow, context) = CreateUnitOfWork();
        // چرخهٔ ۶۰ — کش مرجع تزریق می‌شود (بی‌اثر برای تست یا واقعی؛
        // رفتار گردانندهٔ درج با هر دو یکسان است)
        var createHandler = new Sahand.ClinicPro.Application.Features.Lookups.CreatePharmacyItemCommandHandler(
            uow, new Sahand.ClinicPro.Application.Common.Caching.NoopReferenceCache());

        var createdIds = new List<long>();
        foreach (var (nameFa, form) in new[]
                 {
                     ("سرنگ ۵ سی‌سی", "ست تزریقی"),
                     ("گاز استریل ۱۰×۱۰", "پارچه‌ای"),
                     ("دستکش معاینه سایز M", "لاتکس")
                 })
        {
            var created = await createHandler.Handle(
                new Sahand.ClinicPro.Application.Features.Lookups.CreatePharmacyItemCommand(
                    nameFa, nameFa, MedicationItemKind.Consumable, form),
                CancellationToken.None);
            created.IsSuccess.Should().BeTrue($"قلم «{nameFa}» باید ساخته شود");
            createdIds.Add(created.Value);
        }

        // ═══ گام ۲ — ورود دسته‌جمعی (فرم چرخهٔ ۴۵: چند ردیف پشت‌سرهم) ═══
        // چرخهٔ ۶۷: هندلر اکنون ILogger هم می‌گیرد (سند حسابداری ورود کالا) — در تست، لاگر تهی
        var stockInHandler = new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<
                Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler>.Instance);
        // سه ردیف اول: هر قلم یک بچ؛ ردیف چهارم: بچ دوم برای قلم اول (ادغام قلم‌محور)
        var lines = new (long medId, string? batch, decimal qty)[]
        {
            (createdIds[0], "B-001", 100m),
            (createdIds[1], "B-002", 50m),
            (createdIds[2], null, 200m),
            (createdIds[0], "B-003", 40m)
        };
        foreach (var (medId, batch, qty) in lines)
        {
            var result = await stockInHandler.Handle(
                new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommand(
                    medId, batch, qty, 5000m, 6000m, "1406/05/20", null,
                    ExpectedItemKind: MedicationItemKind.Consumable),
                CancellationToken.None);
            // گزارش مالک: «افزودن دسته‌جمعی درست کار نمی‌کند» — اگر هر ردیفی
                // شکست بخورد اینجا قرمز می‌شود و پیام خطای سرور همان ریشهٔ گزارش است
            result.IsSuccess.Should().BeTrue($"ردیف ورود کالای قلم {medId} باید موفق باشد؛ خطا: {string.Join(",", result.Error.Message)}");
        }

        // ═══ گام ۳ — فهرست موجودی باید بلافاصله اقلام جدید را با جمع درست نشان دهد ═══
        var inventoryHandler = new Sahand.ClinicPro.Application.Features.Pharmacy.GetInventoryQueryHandler(uow);
        var inventory = await inventoryHandler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.GetInventoryQuery(
                LowStock: null, ItemKind: MedicationItemKind.Consumable),
            CancellationToken.None);

        inventory.IsSuccess.Should().BeTrue();
        inventory.Value.Should().HaveCount(3, "سه قلم مصرفیِ تازه‌ساخته با موجودی باید دیده شوند");
        inventory.Value.Single(i => i.ItemId == createdIds[0]).TotalQuantity.Should().Be(140m,
            "قلم اول دو بچ (۱۰۰+۴۰) دارد — ادغام قلم‌محور باید جمع کند");
        inventory.Value.Single(i => i.ItemId == createdIds[1]).TotalQuantity.Should().Be(50m);
        inventory.Value.Single(i => i.ItemId == createdIds[2]).TotalQuantity.Should().Be(200m);

        // ═══ گام ۴ — پذیرش نمونه (منشأ مصرف) ═══
        var reception = new Reception
        {
            PatientId = 1,
            ReceptionDate = DateTime.UtcNow,
            ReceptionNumber = "RC-1001"
        };
        await uow.Repository<Reception>().AddAsync(reception);
        await uow.SaveEntitiesAsync(CancellationToken.None);

        // ═══ گام ۵ — مصرف در پذیرش: ۳۰ عدد از قلم اول (بیش از بچ اول → FEFO دو بچ) ═══
        // چرخهٔ ۶۷: هندلر اکنون ILogger هم می‌گیرد (سند حسابداری هزینهٔ مصرف) — در تست، لاگر تهی
        var consumeHandler = new Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<
                Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler>.Instance);
        var consumed = await consumeHandler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommand(
                reception.Id,
                new List<Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeItemDto>
                {
                    new(createdIds[0], 120m) // از هر دو بچ می‌کشد (۱۰۰ + ۲۰)
                },
                UserId: 7),
            CancellationToken.None);

        // گزارش مالک: «در پذیرش موجودی کم نمی‌شود» — اگر فرمان شکست بخورد اینجا
        // ریشه با پیام فارسی خودش ظاهر می‌شود؛ اگر موفق باشد باید جایگزینی مقدار ثبت شود
        consumed.IsSuccess.Should().BeTrue($"مصرف باید موفق باشد؛ خطا: {string.Join(",", consumed.Error.Message)}");
        consumed.Value!.TotalCost.Should().Be(120m * 6000m, "قیمت فروش هر دو بچ یکسان است");
        consumed.Value!.Items.Should().ContainSingle(i => i.Quantity == 120m);

        // ═══ گام ۶ — موجودی پس از مصرف: باید ۲۰ باقی مانده باشد (۱۴۰ − ۱۲۰) ═══
        var inventoryAfter = await inventoryHandler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.GetInventoryQuery(
                LowStock: null, ItemKind: MedicationItemKind.Consumable),
            CancellationToken.None);
        inventoryAfter.Value.Single(i => i.ItemId == createdIds[0]).TotalQuantity
            .Should().Be(20m, "مصرف ۱۲۰ از ۱۴۰ باید به‌طور اتمیک کم شده باشد");

        // چهار تراکنش ورود (۴ ردیف دسته‌جمعی) + دو برداشت FEFO از دو بچ = ۶
        (await context.PharmacyTransactions.CountAsync()).Should().Be(6,
            "۴ ورود + ۲ برداشت FEFO از دو بچ");
        (await context.PharmacyTransactions.CountAsync(t =>
            t.TransactionType == Sahand.ClinicPro.Domain.Enums.PharmacyTransactionType.StockOut))
            .Should().Be(2, "دو برداشت — یکی از هر بچ");

        // ═══ گام ۷ — فهرست پذیرش (GET consumables) باید موجودی باقی‌مانده را ببیند ═══
        var listHandler = new Sahand.ClinicPro.Application.Features.Pharmacy.GetConsumablesQueryHandler(uow);
        var options = await listHandler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.GetConsumablesQuery(null),
            CancellationToken.None);
        options.IsSuccess.Should().BeTrue();
        options.Value!.Should().HaveCount(3);
        options.Value!.Single(o => o.MedicationId == createdIds[0]).TotalQuantity
            .Should().Be(20m, "کمبوی پذیرش باید موجودی پس از مصرف را نشان دهد (۲۰)");
    }
    [Fact]
    public async Task Consumables_use_expiring_active_batches_first_and_count_all_batches()
    {
        var (uow, context) = CreateUnitOfWork();
        var medication = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
        { GenericName = "FEFO", GenericNameFa = "قلم تست", ItemKind = MedicationItemKind.Consumable, IsActive = true };
        var reception = new Reception { PatientId = 1, ReceptionNumber = "FEFO", Status = ReceptionStatus.Admitted };
        context.AddRange(medication, reception);
        await context.SaveChangesAsync();
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var undated = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory { MedicationId = medication.Id, Quantity = 1, SellPrice = 10 };
        var dated = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory { MedicationId = medication.Id, Quantity = 1, SellPrice = 20, ExpiryDate = future };
        var inactive = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory { MedicationId = medication.Id, Quantity = 100, SellPrice = 50, IsActive = false };
        context.AddRange(undated, dated, inactive);
        context.AddRange(Enumerable.Range(0, 201).Select(_ => new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory
            { MedicationId = medication.Id, Quantity = 1, SellPrice = 10 }));
        await context.SaveChangesAsync();
        var listing = await new Sahand.ClinicPro.Application.Features.Pharmacy.GetConsumablesQueryHandler(uow)
            .Handle(new(null), default);
        listing.Value.Single().TotalQuantity.Should().Be(203);
        var handler = new Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler>.Instance);
        var result = await handler.Handle(new(reception.Id, new() { new(medication.Id, 1) }, null), default);
        result.IsSuccess.Should().BeTrue();
        dated.Quantity.Should().Be(0);
        undated.Quantity.Should().Be(1);
        inactive.Quantity.Should().Be(100);
        result.Value.TotalCost.Should().Be(20);
    }

    [Fact]
    public async Task Consumables_reject_duplicate_items_and_cancelled_reception()
    {
        var (uow, context) = CreateUnitOfWork();
        var reception = new Reception { PatientId = 1, ReceptionNumber = "CANCEL", Status = ReceptionStatus.Cancelled };
        context.Add(reception);
        await context.SaveChangesAsync();
        var handler = new Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler>.Instance);
        (await handler.Handle(new(reception.Id, new() { new(1, 1), new(1, 2) }, null), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(reception.Id, new() { new(1, 1) }, null), default)).IsSuccess.Should().BeFalse();
        context.PharmacyTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Invalid_or_short_later_item_leaves_no_pending_inventory_or_transactions()
    {
        var (uow, db) = CreateUnitOfWork();
        var first = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
            { GenericName = "first", ItemKind = MedicationItemKind.Consumable, IsActive = true };
        var drug = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
            { GenericName = "drug", ItemKind = MedicationItemKind.Drug, IsActive = true };
        var inactive = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
            { GenericName = "inactive", ItemKind = MedicationItemKind.Consumable, IsActive = false };
        var shortage = new Sahand.ClinicPro.Domain.Entities.Clinical.Medication
            { GenericName = "shortage", ItemKind = MedicationItemKind.Consumable, IsActive = true };
        var reception = new Reception { PatientId = 1, ReceptionNumber = "SAFE", Status = ReceptionStatus.Admitted };
        db.AddRange(first, drug, inactive, shortage, reception); await db.SaveChangesAsync();
        var stock = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory { MedicationId = first.Id, Quantity = 5, SellPrice = 100 };
        var shortStock = new Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyInventory { MedicationId = shortage.Id, Quantity = 1, SellPrice = 100 };
        db.AddRange(stock, shortStock); await db.SaveChangesAsync();
        var handler = new Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Application.Features.Pharmacy.ConsumeStockCommandHandler>.Instance);
        foreach (var secondId in new[] { drug.Id, inactive.Id, shortage.Id })
        {
            var failure = await handler.Handle(new(reception.Id, new() { new(first.Id, 2), new(secondId, 2) }), default);
            failure.IsFailure.Should().BeTrue();
            stock.Quantity.Should().Be(5);
            db.ChangeTracker.Entries<Sahand.ClinicPro.Domain.Entities.Pharmacy.PharmacyTransaction>().Should().BeEmpty();
            await db.SaveChangesAsync();
            stock.Quantity.Should().Be(5);
        }
        foreach (var qty in new[] { 0.001m, 100000000m, 0m })
        {
            var failure = await handler.Handle(new(reception.Id, new() { new(first.Id, qty) }), default);
            failure.Error.Code.Should().Be("PHARMACY.CONSUME.QTY");
        }
        var valid = await handler.Handle(new(reception.Id, new() { new(first.Id, 1.25m) }), default);
        valid.IsSuccess.Should().BeTrue();
        stock.Quantity.Should().Be(3.75m);
    }

}
