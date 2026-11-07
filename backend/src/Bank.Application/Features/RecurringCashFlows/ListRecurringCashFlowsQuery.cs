using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using Bank.Domain.CashFlows;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.RecurringCashFlows;

/// <param name="Kind">Optional filter, e.g. <c>Subscription</c> to review all subscriptions.</param>
public sealed record ListRecurringCashFlowsQuery(string? Kind) : IRequest<IReadOnlyList<RecurringCashFlowResponse>>;

public sealed class ListRecurringCashFlowsQueryValidator : AbstractValidator<ListRecurringCashFlowsQuery>
{
    public ListRecurringCashFlowsQueryValidator()
    {
        RuleFor(q => q.Kind)
            .IsEnumName(typeof(CashFlowKind), caseSensitive: true)
            .When(q => q.Kind is not null)
            .WithMessage("Geçersiz tür filtresi.");
    }
}

public sealed class ListRecurringCashFlowsQueryHandler
    : IRequestHandler<ListRecurringCashFlowsQuery, IReadOnlyList<RecurringCashFlowResponse>>
{
    private readonly IRecurringCashFlowRepository _items;
    private readonly ICurrentUser _currentUser;

    public ListRecurringCashFlowsQueryHandler(IRecurringCashFlowRepository items, ICurrentUser currentUser)
    {
        _items = items;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RecurringCashFlowResponse>> Handle(ListRecurringCashFlowsQuery query, CancellationToken ct)
    {
        var filter = query.Kind is null ? (CashFlowKind?)null : Enum.Parse<CashFlowKind>(query.Kind);
        var items = await _items.ListAsync(_currentUser.UserId, filter, ct);
        return items.Select(RecurringCashFlowMapping.ToResponse).ToList();
    }
}
