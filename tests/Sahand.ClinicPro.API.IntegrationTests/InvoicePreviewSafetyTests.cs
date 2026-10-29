using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class InvoicePreviewSafetyTests
{
    [Fact]
    public async Task Preview_is_read_only_and_changed_input_cannot_be_issued()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "QUOTE-01", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        db.Add(patient); await db.SaveChangesAsync();
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<CreateInvoiceCommandHandler>.Instance);
        var item = new InvoiceItemDto(null, "خدمت", 1, 101, 10, null, 0);
        var dto = new CreateInvoiceDto(patient.Id, null, null, InvoiceType.Other, null, 5, new() { item });
        var quote = await handler.Handle(new PreviewInvoiceQuery(dto), default);
        quote.IsSuccess.Should().BeTrue();
        quote.Value.SubTotal.Should().Be(101);
        quote.Value.DiscountAmount.Should().Be(10);
        quote.Value.Tax.Should().Be(5);
        quote.Value.PatientPayable.Should().Be(96);
        (await db.Set<Invoice>().CountAsync()).Should().Be(0);
        var stale = dto with { Items = new() { item with { UnitPrice = 102 } }, ExpectedQuote = quote.Value.QuoteVersion };
        (await handler.Handle(new CreateInvoiceCommand(stale), default)).Error.Code.Should().Be("BILLING.QUOTE_CHANGED");
        (await db.Set<Invoice>().CountAsync()).Should().Be(0);
        var result = await handler.Handle(new CreateInvoiceCommand(dto with { ExpectedQuote = quote.Value.QuoteVersion }), default);
        result.IsSuccess.Should().BeTrue();
        var invoice = await db.Set<Invoice>().SingleAsync();
        invoice.RemainingAmount.Should().Be(quote.Value.PatientPayable);
    }
}
