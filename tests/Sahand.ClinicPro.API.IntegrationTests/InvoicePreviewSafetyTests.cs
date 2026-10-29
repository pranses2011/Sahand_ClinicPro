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
        var quote = await handler.PreviewAsync(new PreviewInvoiceQuery(dto), default);
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
    [Fact]
    public async Task Changed_service_tariff_blocks_issuance_after_preview()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "QUOTE-02", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000002" };
        var service = new Service { ServiceName = "خدمت", DefaultPrice = 100, IsActive = true };
        db.AddRange(patient, service); await db.SaveChangesAsync();
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<CreateInvoiceCommandHandler>.Instance);
        var dto = new CreateInvoiceDto(patient.Id, null, null, InvoiceType.Other, null, 0,
            new() { new InvoiceItemDto(service.Id, "خدمت", 1, 100, 0, null, 0) });
        var quote = await handler.PreviewAsync(new PreviewInvoiceQuery(dto), default);
        quote.IsSuccess.Should().BeTrue();
        service.DefaultPrice = 200;
        await db.SaveChangesAsync();
        var result = await handler.Handle(new CreateInvoiceCommand(dto with { ExpectedQuote = quote.Value.QuoteVersion }), default);
        result.Error.Code.Should().Be("BILLING.TARIFF_CHANGED");
        (await db.Set<Invoice>().CountAsync()).Should().Be(0);
    }
    [Fact]
    public async Task Invoice_request_replays_before_tariff_revalidation_and_rejects_other_content()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "QUOTE-REPLAY", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000002" };
        var service = new Service { ServiceName = "خدمت", DefaultPrice = 100, IsActive = true };
        db.AddRange(patient, service); await db.SaveChangesAsync();
        var handler = new CreateInvoiceCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<CreateInvoiceCommandHandler>.Instance);
        var dto = new CreateInvoiceDto(patient.Id, null, null, InvoiceType.Other, null, 0,
            new() { new InvoiceItemDto(service.Id, "خدمت", 1, 100, 0, null, 0) });
        var quote = await handler.PreviewAsync(new PreviewInvoiceQuery(dto), default);
        quote.IsSuccess.Should().BeTrue();
        var request = dto with { ExpectedQuote = quote.Value.QuoteVersion, RequestId = Guid.NewGuid() };
        var first = await handler.Handle(new CreateInvoiceCommand(request), default);
        first.IsSuccess.Should().BeTrue();
        service.DefaultPrice = 200; await db.SaveChangesAsync();
        var repeat = await handler.Handle(new CreateInvoiceCommand(request), default);
        repeat.IsSuccess.Should().BeTrue(); repeat.Value.Should().Be(first.Value);
        var conflict = await handler.Handle(new CreateInvoiceCommand(request with { Description = "تغییر" }), default);
        conflict.Error.Code.Should().Be("BILLING.REQUEST_CONFLICT");
        (await db.Set<Invoice>().CountAsync()).Should().Be(1);
        (await handler.Handle(new CreateInvoiceCommand(request with { RequestId = Guid.Empty }), default)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Idempotent_invoice_endpoint_rejects_missing_or_empty_key()
    {
        var mediator = new Moq.Mock<MediatR.IMediator>(Moq.MockBehavior.Strict);
        var controller = new Sahand.ClinicPro.API.Controllers.InvoicesController(mediator.Object);
        var dto = new CreateInvoiceDto(1, null, null, InvoiceType.Other, null, 0,
            new() { new InvoiceItemDto(null, "خدمت", 1, 100, 0, null, 0) });
        (await controller.CreateIdempotent(dto)).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        (await controller.CreateIdempotent(dto with { RequestId = Guid.Empty })).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        mediator.VerifyNoOtherCalls();
    }
}
