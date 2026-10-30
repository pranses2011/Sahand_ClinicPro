using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Application.Features.Billing.Commands.RecordPayment;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class RequestIdConflictTests
{
    [Theory]
    [InlineData(2601, "duplicate index 'IX_Payment_RequestId'", RequestIdConflict.PaymentIndex, true)]
    [InlineData(2627, "constraint 'ix_invoice_requestid'", RequestIdConflict.InvoiceIndex, true)]
    [InlineData(2601, "duplicate index 'IX_Reception_RequestId'", RequestIdConflict.ReceptionIndex, true)]
    [InlineData(2601, "duplicate index 'IX_Payment_PaymentNumber'", RequestIdConflict.PaymentIndex, false)]
    [InlineData(2601, "duplicate index 'IX_Payment_RequestId_Extra'", RequestIdConflict.PaymentIndex, false)]
    [InlineData(547, "constraint 'IX_Payment_RequestId'", RequestIdConflict.PaymentIndex, false)]
    public void Only_exact_request_id_unique_index_is_classified(int number, string message, string index, bool expected)
    {
        Assert.Equal(expected, RequestIdConflict.IsRelevantSqlError(number, message, index));
    }

    [Fact]
    public async Task Other_database_errors_are_not_misreported_as_payment_replay_conflicts()
    {
        var failure = new DbUpdateException("unrelated database error");
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<RecordPaymentCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var controller = new PaymentsController(mediator.Object);
        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Record(new RecordPaymentDto(1, 1, 1, PaymentMethod.Cash)));
        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task Other_database_errors_are_not_misreported_as_invoice_replay_conflicts()
    {
        var failure = new DbUpdateException("unrelated database error");
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateInvoiceCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var controller = new InvoicesController(mediator.Object);
        var dto = new CreateInvoiceDto(1, null, null, InvoiceType.Other, null, 0, new());
        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Create(dto));
        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task Reception_concurrency_is_conflict_but_other_database_errors_propagate()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateReceptionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("concurrent change"));
        var controller = new ReceptionsController(mediator.Object, Mock.Of<ICurrentUserService>());
        var dto = new CreateReceptionDto(1, 1, null, null, null, null);
        Assert.IsType<ConflictObjectResult>(await controller.Create(dto));
        var failure = new DbUpdateException("unrelated database error");
        mediator.Setup(m => m.Send(It.IsAny<CreateReceptionCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Create(dto));
        Assert.Same(failure, thrown);
    }
}
