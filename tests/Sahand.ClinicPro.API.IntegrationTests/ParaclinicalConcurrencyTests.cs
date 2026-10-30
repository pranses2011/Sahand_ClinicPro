using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Application.Common.Behaviors;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class ParaclinicalConcurrencyTests
{
    [Fact]
    public async Task Concurrency_exception_rolls_back_pipeline_instead_of_committing()
    {
        var transaction = new Mock<ITransactionManager>();
        var behavior = new TransactionBehavior<SubmitImagingReportCommand, Result>(transaction.Object,
            NullLogger<TransactionBehavior<SubmitImagingReportCommand, Result>>.Instance);
        var command = new SubmitImagingReportCommand(new ImagingReportDto(1, null, "یافته", null, null));
        RequestHandlerDelegate<Result> next = _ => Task.FromException<Result>(new DbUpdateConcurrencyException());
        await FluentActions.Invoking(() => behavior.Handle(command, next, default)).Should().ThrowAsync<DbUpdateConcurrencyException>();
        transaction.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Controllers_return_conflict_after_transaction_exception()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<SubmitImagingReportCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        mediator.Setup(x => x.Send(It.IsAny<UpdateImagingOrderStatusCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        mediator.Setup(x => x.Send(It.IsAny<CreateDentalPlanCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        mediator.Setup(x => x.Send(It.IsAny<RecordPhysioSessionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var controller = new ParaclinicalController(mediator.Object, Mock.Of<IUnitOfWork>(),
            Mock.Of<IWebHostEnvironment>(), Mock.Of<IConfiguration>());
        (await controller.SubmitReport(new ImagingReportDto(1, null, "یافته", null, null))).Should().BeOfType<ConflictObjectResult>();
        (await controller.UpdateOrderStatus(new UpdateImagingOrderStatusCommand(1, ImagingOrderStatus.Performed))).Should().BeOfType<ConflictObjectResult>();
        (await controller.CreateDentalPlan(new CreateDentalPlanCommand(1, null, 0, 0, "{}"))).Should().BeOfType<ConflictObjectResult>();
        (await controller.RecordSession(new RecordPhysioSessionCommand(1, null, 1, null, 20, 0))).Should().BeOfType<ConflictObjectResult>();
    }
}
