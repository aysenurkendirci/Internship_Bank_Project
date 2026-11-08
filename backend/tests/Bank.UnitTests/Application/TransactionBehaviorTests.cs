using Bank.Application.Abstractions.Persistence;
using Bank.Application.Common.Behaviors;
using Bank.Application.Features.Transfers;
using Bank.Contracts.Payments;
using NSubstitute;

namespace Bank.UnitTests.Application;

public sealed class TransactionBehaviorTests
{
    private sealed class Deadlock : Exception;

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransactionScope _scope = Substitute.For<ITransactionScope>();
    private readonly TransactionBehavior<TransferMoneyCommand, string> _behavior;

    private static readonly TransferMoneyCommand Command =
        new(new TransferRequest(1, "TR330006100519786457841326", "Ayşe Yılmaz", 10m, "Test"), "key");

    public TransactionBehaviorTests()
    {
        _unitOfWork.BeginAsync(Arg.Any<CancellationToken>()).Returns(_scope);
        _unitOfWork.IsRetryable(Arg.Any<Exception>()).Returns(call => call.Arg<Exception>() is Deadlock);
        _behavior = new TransactionBehavior<TransferMoneyCommand, string>(_unitOfWork);
    }

    [Fact]
    public async Task Success_CommitsOnce()
    {
        var result = await _behavior.Handle(Command, _ => Task.FromResult("done"), default);

        Assert.Equal("done", result);
        await _scope.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeadlockVictim_IsRunAgain_AndOnlyTheSuccessfulRunCommits()
    {
        var runs = 0;

        var result = await _behavior.Handle(Command, _ =>
        {
            runs++;
            return runs < 3 ? throw new Deadlock() : Task.FromResult("done");
        }, default);

        Assert.Equal(("done", 3), (result, runs));
        await _unitOfWork.Received(3).BeginAsync(Arg.Any<CancellationToken>());
        await _scope.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KeepsFailing_GivesUpAfterTheLastAttempt()
    {
        var runs = 0;

        await Assert.ThrowsAsync<Deadlock>(() => _behavior.Handle(Command, _ =>
        {
            runs++;
            throw new Deadlock();
        }, default));

        Assert.Equal(TransactionBehavior<TransferMoneyCommand, string>.MaxAttempts, runs);
        await _scope.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OtherFailures_AreNotRetried()
    {
        var runs = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(Command, _ =>
        {
            runs++;
            throw new InvalidOperationException("rule broken");
        }, default));

        Assert.Equal(1, runs);
        await _scope.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandsThatAreNotTransactional_BypassTheTransaction()
    {
        var behavior = new TransactionBehavior<string, string>(_unitOfWork);

        var result = await behavior.Handle("plain", _ => Task.FromResult("done"), default);

        Assert.Equal("done", result);
        await _unitOfWork.DidNotReceive().BeginAsync(Arg.Any<CancellationToken>());
    }
}
