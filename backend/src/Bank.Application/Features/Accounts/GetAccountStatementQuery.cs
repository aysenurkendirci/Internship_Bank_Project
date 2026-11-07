using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Contracts.Payments;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.Accounts;

/// <summary>The account's movements as recorded in the ledger, newest first.</summary>
public sealed record GetAccountStatementQuery(long AccountId, int Page, int PageSize)
    : IRequest<PagedResponse<StatementEntryResponse>>;

public sealed class GetAccountStatementQueryValidator : AbstractValidator<GetAccountStatementQuery>
{
    public GetAccountStatementQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa 1 veya daha büyük olmalıdır.");
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200).WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
    }
}

public sealed class GetAccountStatementQueryHandler
    : IRequestHandler<GetAccountStatementQuery, PagedResponse<StatementEntryResponse>>
{
    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public GetAccountStatementQueryHandler(IAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<StatementEntryResponse>> Handle(GetAccountStatementQuery query, CancellationToken ct)
    {
        // Someone else's account answers exactly like a missing one, so ids cannot be probed.
        if (!await _accounts.BelongsToAsync(_currentUser.UserId, query.AccountId, ct))
            throw new NotFoundException("Hesap bulunamadı.");

        var (items, total) = await _accounts.ListStatementAsync(query.AccountId, query.Page, query.PageSize, ct);

        return new PagedResponse<StatementEntryResponse>(
            items.Select(e => new StatementEntryResponse(
                e.EntryId, e.TransferId, e.Side, e.Amount, e.CurrencyCode, e.BalanceAfter, e.Description, e.CounterpartyIban, e.CreatedAt))
                .ToList(),
            query.Page,
            query.PageSize,
            total);
    }
}
