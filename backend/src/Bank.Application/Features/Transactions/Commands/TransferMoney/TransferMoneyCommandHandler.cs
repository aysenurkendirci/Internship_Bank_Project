using Bank.Application.Abstractions.Data;
using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;
using MediatR;

namespace Bank.Application.Features.Transactions.Commands.TransferMoney;

public sealed class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferMoneyCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        // 1. Hesapları getir
        var senderAccount = await _accountRepository.GetByIdWithUserAsync(request.SenderAccountId, cancellationToken);
        var receiverAccount = await _accountRepository.GetByIdWithUserAsync(request.ReceiverAccountId, cancellationToken);

        if (senderAccount is null) throw new DomainException("Gönderen hesap bulunamadı.");
        if (receiverAccount is null) throw new DomainException("Alıcı hesap bulunamadı.");

        // 2. Güvenlik Kontrolü: Gönderen hesap gerçekten login olan kullanıcıya mı ait?
        if (senderAccount.UserId != request.SenderUserId)
            throw new DomainException("Bu hesaptan transfer yapma yetkiniz yok.");

        // 3. Çekilecek miktarı Money objesine çevir
        var transferMoney = Money.Create(request.Amount, senderAccount.Currency);

        // 4. Bakiye Düşme ve Ekleme (Domain Kuralları çalışır, yetersiz bakiye vb. hatalar fırlatılır)
        senderAccount.Withdraw(transferMoney);
        receiverAccount.Deposit(transferMoney);

        // 5. İşlem (Transaction) Kaydı Oluştur
        var transaction = new Transaction(
            senderAccount.Id,
            receiverAccount.Id,
            request.Amount,
            senderAccount.Currency,
            TransactionType.InternalTransfer,
            "Transfer",
            request.Description
        );

        // 6. Çift Taraflı Muhasebe Kaydı (Ledger Entries)
        // Gönderen hesaba BORÇ (Debit) yazılır
        var debitEntry = new LedgerEntry(
            transaction.Id, // Bu aşamada Id 0'dır ama EF Core ilişkiyi kendi çözer
            senderAccount.Id,
            LedgerEntryType.Debit,
            request.Amount,
            senderAccount.Currency,
            $"Giden Transfer: {receiverAccount.User.FirstName} {receiverAccount.User.LastName} ({request.Description})"
        );

        // Alıcı hesaba ALACAK (Credit) yazılır
        var creditEntry = new LedgerEntry(
            transaction.Id,
            receiverAccount.Id,
            LedgerEntryType.Credit,
            request.Amount,
            receiverAccount.Currency,
            $"Gelen Transfer: {senderAccount.User.FirstName} {senderAccount.User.LastName} ({request.Description})"
        );

        transaction.LedgerEntries.Add(debitEntry);
        transaction.LedgerEntries.Add(creditEntry);

        // 7. Veritabanına kaydet
        await _accountRepository.UpdateAsync(senderAccount, cancellationToken);
        await _accountRepository.UpdateAsync(receiverAccount, cancellationToken);
        await _accountRepository.AddTransactionAsync(transaction, cancellationToken);

        // 8. Tüm değişiklikleri tek bir paket (Transaction) olarak işle.
        // Eğer burada bir sorun çıkarsa, hiçbiri veritabanına yansımaz (Rollback).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
