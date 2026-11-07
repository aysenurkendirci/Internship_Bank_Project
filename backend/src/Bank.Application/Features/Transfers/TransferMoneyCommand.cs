using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Behaviors;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Payments;
using Bank.Domain.Accounts;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Transfers;

/// <param name="IdempotencyKey">
/// Chosen by the client and sent again on every retry of the same transfer. A lost response followed by a retry
/// must not send the money twice.
/// </param>
public sealed record TransferMoneyCommand(TransferRequest Transfer, string? IdempotencyKey)
    : IRequest<TransferResult>, ITransactionalCommand;

/// <param name="Replayed">True when the key was seen before and the stored result is returned instead of a new transfer.</param>
public sealed record TransferResult(TransferResponse Transfer, bool Replayed);

public sealed class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
{
    public TransferMoneyCommandValidator()
    {
        RuleFor(c => c.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency-Key başlığı zorunludur.")
            .MaximumLength(64).WithMessage("Idempotency-Key en fazla 64 karakter olabilir.")
            .Matches("^[A-Za-z0-9._:-]+$").WithMessage("Idempotency-Key yalnızca harf, rakam ve . _ : - içerebilir.");

        RuleFor(c => c.Transfer.TargetIban)
            .Must(iban => Iban.TryParse(iban, out _)).WithMessage("Alıcı IBAN geçerli değil.")
            .OverridePropertyName("targetIban");
    }
}

public sealed class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, TransferResult>
{
    private readonly ITransferRepository _transfers;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TransferMoneyCommandHandler> _logger;

    public TransferMoneyCommandHandler(ITransferRepository transfers, ICurrentUser currentUser, ILogger<TransferMoneyCommandHandler> logger)
    {
        _transfers = transfers;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TransferResult> Handle(TransferMoneyCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var request = command.Transfer;
        var key = command.IdempotencyKey!;
        var requestHash = Hash(request);

        // The key is locked from here on: a retry arriving while this transfer runs waits, then finds it.
        var earlier = await _transfers.FindByIdempotencyKeyAsync(userId, key, ct);
        if (earlier is not null)
        {
            if (!earlier.RequestHash.AsSpan().SequenceEqual(requestHash))
                throw new IdempotencyKeyReusedException();

            return new TransferResult(TransferMapping.ToResponse(earlier), Replayed: true);
        }

        var sourceId = request.SourceAccountId!.Value;
        var targetId = await _transfers.FindAccountIdAsync(Iban.Parse(request.TargetIban), ct)
                       ?? throw new DomainException("Bu IBAN'a ait bir hesap bulunamadı.");

        var accounts = await _transfers.LockAccountsAsync([sourceId, targetId], ct);
        var source = accounts.SingleOrDefault(a => a.Id == sourceId && a.UserId == userId)
                     ?? throw new NotFoundException("Gönderen hesap bulunamadı.");
        var target = accounts.Single(a => a.Id == targetId);

        var amount = new Money(request.Amount, source.Currency);
        var postings = FundsTransfer.Execute(source, target, amount, request.RecipientName);

        var stored = await _transfers.AddAsync(
            new NewTransfer(userId, key, requestHash, source, target, amount, request.Description.Trim(), postings), ct);

        _logger.LogInformation(
            "Transfer {TransferId}: {Amount} from account {SourceAccountId} to account {TargetAccountId}",
            stored.TransferId, amount, source.Id, target.Id);

        return new TransferResult(TransferMapping.ToResponse(stored), Replayed: false);
    }

    /// <summary>
    /// Fingerprint of what the client asked for. Names are folded and the amount is written invariantly, so the
    /// same request typed slightly differently on a retry ("ayse yilmaz", 100 vs 100.00) still counts as the same.
    /// </summary>
    private static byte[] Hash(TransferRequest request)
    {
        var canonical = string.Join('\n',
            request.SourceAccountId!.Value.ToString(CultureInfo.InvariantCulture),
            Iban.Parse(request.TargetIban).Value,
            TextFolding.Fold(request.RecipientName),
            request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            request.Description.Trim());

        return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    }
}

internal static class TransferMapping
{
    public static TransferResponse ToResponse(StoredTransfer t)
        => new(
            t.TransferId,
            t.SourceAccountId,
            t.SourceIban,
            t.TargetIban,
            t.RecipientName,
            t.Amount,
            t.CurrencyCode,
            t.Description,
            t.SourceBalanceAfter,
            t.CreatedAt);
}
