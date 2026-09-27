using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Entities;
using Bank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bank.Infrastructure.Repositories;

public sealed class CardRepository : ICardRepository
{
    private readonly BankDbContext _context;

    public CardRepository(BankDbContext context)
    {
        _context = context;
    }

    public async Task<Card?> GetByIdWithAccountAsync(long cardId, CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);
    }

    public Task UpdateAsync(Card card, CancellationToken cancellationToken = default)
    {
        _context.Cards.Update(card);
        return Task.CompletedTask;
    }
}
