using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Payments;
using MediatR;

namespace Bank.Application.Features.Transfers;

/// <summary>A transfer the current user sent. The recipient sees the same money in their account statement.</summary>
public sealed record GetTransferQuery(long Id) : IRequest<TransferResponse>;

public sealed class GetTransferQueryHandler : IRequestHandler<GetTransferQuery, TransferResponse>
{
    private readonly ITransferRepository _transfers;
    private readonly ICurrentUser _currentUser;

    public GetTransferQueryHandler(ITransferRepository transfers, ICurrentUser currentUser)
    {
        _transfers = transfers;
        _currentUser = currentUser;
    }

    public async Task<TransferResponse> Handle(GetTransferQuery query, CancellationToken ct)
    {
        var transfer = await _transfers.GetAsync(_currentUser.UserId, query.Id, ct)
                       ?? throw new NotFoundException("Transfer bulunamadı.");
        return TransferMapping.ToResponse(transfer);
    }
}
