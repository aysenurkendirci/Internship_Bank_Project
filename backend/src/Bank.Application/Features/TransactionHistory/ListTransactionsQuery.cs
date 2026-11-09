using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.TransactionHistory;

/// <summary>One page of imported statement lines, newest first.</summary>
public sealed record ListTransactionsQuery(DateOnly? From, DateOnly? To, int Page, int PageSize)
    : IRequest<PagedResponse<HistoryTransactionResponse>>;

public sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa 1 veya daha büyük olmalıdır.");
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200).WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
        RuleFor(q => q.From)
            .LessThanOrEqualTo(q => q.To)
            .When(q => q.From is not null && q.To is not null)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}

public sealed class ListTransactionsQueryHandler
    : IRequestHandler<ListTransactionsQuery, PagedResponse<HistoryTransactionResponse>>
{
    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;

    public ListTransactionsQueryHandler(ITransactionHistoryRepository history, ICurrentUser currentUser)
    {
        _history = history;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<HistoryTransactionResponse>> Handle(ListTransactionsQuery query, CancellationToken ct)
    {
        var (items, total) = await _history.ListAsync(
            _currentUser.UserId, query.From, query.To, query.Page, query.PageSize, ct);

        return new PagedResponse<HistoryTransactionResponse>(
            items.Select(t => new HistoryTransactionResponse(t.Id, t.Date, t.Description, t.Amount, t.CurrencyCode, t.Category)).ToList(),
            query.Page,
            query.PageSize,
            total);
    }
}
