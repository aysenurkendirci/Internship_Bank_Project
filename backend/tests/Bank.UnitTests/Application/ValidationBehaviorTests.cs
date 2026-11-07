using Bank.Application.Common.Behaviors;
using Bank.Application.Features.TransactionHistory;
using FluentValidation;

namespace Bank.UnitTests.Application;

public sealed class ValidationBehaviorTests
{
    private readonly ValidationBehavior<ListTransactionsQuery, string> _behavior =
        new([new ListTransactionsQueryValidator()]);

    [Fact]
    public async Task InvalidRequest_NeverReachesTheHandler()
    {
        var handlerCalled = false;
        var query = new ListTransactionsQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 1), Page: 0, PageSize: 50);

        var error = await Assert.ThrowsAsync<ValidationException>(() => _behavior.Handle(query, _ =>
        {
            handlerCalled = true;
            return Task.FromResult("handled");
        }, default));

        Assert.False(handlerCalled);
        Assert.Equal(["Page", "From"], error.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public async Task ValidRequest_IsPassedOn()
    {
        var query = new ListTransactionsQuery(From: null, To: null, Page: 1, PageSize: 50);

        Assert.Equal("handled", await _behavior.Handle(query, _ => Task.FromResult("handled"), default));
    }
}
