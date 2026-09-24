using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Data;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using MediatR;

namespace Bank.Application.Features.Accounts.Commands.CreateAccount;

internal sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, long>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Simulate generating Account Number and IBAN for the new account
        string accountNumber = new Random().Next(10000000, 99999999).ToString();
        string iban = $"TR{new Random().Next(10, 99)}00062000000{accountNumber}";

        // Create entity
        var account = new Account(
            request.UserId,
            accountNumber,
            iban,
            (AccountType)request.Type, // Assuming AccountType is an enum accessible here
            request.Currency
        );

        // Save
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
