using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.Common.Behaviors;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

public class TransactionBehaviorTests
{
    private sealed record ExampleCommand : IRequest<Result<long>>;
    private sealed record ExampleQuery : IRequest<Result<long>>;

    [Fact]
    public async Task Domain_failure_rolls_back_instead_of_committing_intermediate_saves()
    {
        var transaction = new Mock<ITransactionManager>();
        var behavior = new TransactionBehavior<ExampleCommand, Result<long>>(transaction.Object,
            NullLogger<TransactionBehavior<ExampleCommand, Result<long>>>.Instance);
        var failure = Result<long>.Failure(new Error("TEST", "failure"));
        RequestHandlerDelegate<Result<long>> next = _ => Task.FromResult(failure);

        (await behavior.Handle(new ExampleCommand(), next, default)).Should().BeSameAs(failure);
        transaction.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Successful_command_commits()
    {
        var transaction = new Mock<ITransactionManager>();
        var behavior = new TransactionBehavior<ExampleCommand, Result<long>>(transaction.Object,
            NullLogger<TransactionBehavior<ExampleCommand, Result<long>>>.Instance);
        RequestHandlerDelegate<Result<long>> next = _ => Task.FromResult(Result<long>.Success(42));

        (await behavior.Handle(new ExampleCommand(), next, default)).Value.Should().Be(42);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelled_request_still_attempts_rollback_without_cancelled_token()
    {
        var transaction = new Mock<ITransactionManager>();
        var behavior = new TransactionBehavior<ExampleCommand, Result<long>>(transaction.Object,
            NullLogger<TransactionBehavior<ExampleCommand, Result<long>>>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RequestHandlerDelegate<Result<long>> next = _ => Task.FromException<Result<long>>(new OperationCanceledException());

        await FluentActions.Invoking(() => behavior.Handle(new ExampleCommand(), next, cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        transaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Queries_do_not_start_a_transaction()
    {
        var transaction = new Mock<ITransactionManager>(MockBehavior.Strict);
        var behavior = new TransactionBehavior<ExampleQuery, Result<long>>(transaction.Object,
            NullLogger<TransactionBehavior<ExampleQuery, Result<long>>>.Instance);
        RequestHandlerDelegate<Result<long>> next = _ => Task.FromResult(Result<long>.Success(7));

        (await behavior.Handle(new ExampleQuery(), next, default)).Value.Should().Be(7);
        transaction.VerifyNoOtherCalls();
    }
}
