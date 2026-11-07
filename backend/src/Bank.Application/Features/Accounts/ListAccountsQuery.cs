using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Payments;
using MediatR;

namespace Bank.Application.Features.Accounts;

public sealed record ListAccountsQuery : IRequest<IReadOnlyList<AccountResponse>>;

public sealed class ListAccountsQueryHandler : IRequestHandler<ListAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public ListAccountsQueryHandler(IAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AccountResponse>> Handle(ListAccountsQuery query, CancellationToken ct)
        => (await _accounts.ListAsync(_currentUser.UserId, ct)).Select(AccountMapping.ToResponse).ToList();
}

internal static class AccountMapping
{
    public static AccountResponse ToResponse(AccountRow a)
        => new(a.AccountId, a.Iban, a.AccountType, a.CurrencyCode, a.Balance, a.Status);
}
