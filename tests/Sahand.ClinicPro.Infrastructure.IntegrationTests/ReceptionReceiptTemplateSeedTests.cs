using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Reporting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class ReceptionReceiptTemplateSeedTests
{
    private static ClinicProDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"ReceiptTemplate_{Guid.NewGuid():N}").Options;
        var db = new ClinicProDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Upgrade_sets_original_receipt_as_default_only_once()
    {
        using var db = NewContext();
        var previous = new PrintTemplate { TemplateName = "انتخاب قبلی", Type = PrintTemplateType.ReceptionReceipt,
            HtmlBody = "<p>custom</p>", IsActive = true, IsDefault = true };
        db.PrintTemplates.Add(previous);
        await db.SaveChangesAsync();
        var seeder = new PrintTemplateSeeder(db);
        await seeder.SeedAsync();
        var original = await db.PrintTemplates.SingleAsync(t => t.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker);
        original.IsDefault.Should().BeTrue();
        previous.IsDefault.Should().BeFalse();

        // پس از اولین ارتقا، انتخاب دوبارهٔ کاربر در اجراهای بعدی محفوظ است.
        original.IsDefault = false;
        previous.IsDefault = true;
        await db.SaveChangesAsync();
        await seeder.SeedAsync();
        previous.IsDefault.Should().BeTrue();
        original.IsDefault.Should().BeFalse();
        (await db.PrintTemplates.CountAsync(t => t.HtmlBody == BuiltInPrintTemplates.ReceptionReceiptMarker)).Should().Be(1);
    }

    [Fact]
    public async Task Fresh_install_makes_original_receipt_default()
    {
        using var db = NewContext();
        await new PrintTemplateSeeder(db).SeedAsync();
        var defaults = await db.PrintTemplates.Where(t => t.Type == PrintTemplateType.ReceptionReceipt && t.IsDefault).ToListAsync();
        defaults.Should().ContainSingle().Which.HtmlBody.Should().Be(BuiltInPrintTemplates.ReceptionReceiptMarker);
    }
}
