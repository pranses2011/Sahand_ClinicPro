using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست تشخیصی موقت چرخهٔ ۴۳ — بازتولید «خطای غیرمنتظره» ورود کالا
/// (POST consumables/inventory → StockInCommandHandler) روی دیتابیس
/// درون‌حافظه‌ای تا خطاهای سطح #C هندلر جدا از خطاهای SQL Server دیده شوند.
/// </summary>
public class Cycle43StockInDiagnostics
{
    private static (UnitOfWork UoW, ClinicProDbContext Context) CreateUnitOfWork()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"StockInDiag_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), context);
    }

    [Fact]
    public async Task StockIn_CreatesBatchAndTransaction_WithTempKeyFixup()
    {
        var (uow, context) = CreateUnitOfWork();

        // قلم مصرفی نمونه
        var med = new Medication
        {
            GenericName = "سرنگ ۲ سی سی",
            GenericNameFa = "سرنگ ۲ سی سی",
            ItemKind = MedicationItemKind.Consumable,
            IsActive = true
        };
        await uow.Repository<Medication>().AddAsync(med);
        await uow.SaveEntitiesAsync();

        // چرخهٔ ۶۷: هندلر اکنون ILogger هم می‌گیرد (سند حسابداری ورود کالا) — در تست، لاگر تهی
        var handler = new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler>.Instance);
        var result = await handler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommand(
                med.Id, "B-100", 100m, 5000m, 6000m, "1406/05/20", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: "داده معتبر است");
        (await context.PharmacyInventory.CountAsync()).Should().Be(1);
        (await context.PharmacyTransactions.CountAsync()).Should().Be(1);

        // گارد حیاتی تشخیصی: FK تراکنش باید به شناسهٔ واقعی بچ «مهاجرت» کرده باشد؛
        // اگر مقدار موقت (منفی) در آن می‌ماند، SQL Server خطای FK می‌داد
        var savedBatch = await context.PharmacyInventory.FirstAsync();
        var savedTx = await context.PharmacyTransactions.FirstAsync();
        savedTx.InventoryId.Should().Be(savedBatch.Id,
            because: "انتشار مقدار کلید موقت به FK باید پس از ذخیره انجام شود");
        savedTx.InventoryId.Should().BeGreaterThan(0,
            because: "شناسهٔ منفی موقت EF نباید به دیتابیس سرریز کند");
    }

    [Fact]
    public async Task StockIn_WithoutExpiry_CreatesBatchWithoutDate()
    {
        var (uow, context) = CreateUnitOfWork();
        var med = new Medication { GenericName = "گاز استریل", GenericNameFa = "گاز استریل", ItemKind = MedicationItemKind.Consumable, IsActive = true };
        await uow.Repository<Medication>().AddAsync(med);
        await uow.SaveEntitiesAsync();

        // چرخهٔ ۶۷: هندلر اکنون ILogger هم می‌گیرد (سند حسابداری ورود کالا) — در تست، لاگر تهی
        var handler = new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler(uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommandHandler>.Instance);
        var result = await handler.Handle(
            new Sahand.ClinicPro.Application.Features.Pharmacy.StockInCommand(
                med.Id, null, 50m, 1000m, 1200m, "۱۴۰۶/۰۵/۲۰", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// چرخهٔ ۴۳ — گارد هشدار EF Core «ItemKind … has no configured sentinel value»:
    /// مدل زنده باید Sentinel غیرتهی برای ItemKind داشته باشد (Sentinel=Drug چون
    /// پیش‌فرض دیتابیس همان Drug است و رفتار ذخیره‌سازی ذره‌ای تغییر نمی‌کند).
    /// </summary>
    [Fact]
    public void ItemKind_Has_Sentinel_In_Runtime_Model()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlServer("Server=placeholder;Database=placeholder;TrustServerCertificate=True")
            .Options;

        using var context = new ClinicProDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Sahand.ClinicPro.Domain.Entities.Clinical.Medication));
        entity.Should().NotBeNull();
        var property = entity!.FindProperty(nameof(Sahand.ClinicPro.Domain.Entities.Clinical.Medication.ItemKind));
        property.Should().NotBeNull();
        property!.Sentinel.Should().NotBeNull(
            because: "بدون Sentinel، EF Core هشدار «database-generated default will always be used» در هر راه‌اندازی سرور ثبت می‌کند");
    }
}
