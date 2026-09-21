using Microsoft.EntityFrameworkCore;
using Bank.Domain.Entities;

namespace Bank.Infrastructure.Persistence;

public sealed class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    // Veritabanı Tablolarımız (Domain Entity'leri)
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

//override ederek ef core override mantığını,özel kurallara göre değiştirdik
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Bu komut, projede oluşturduğumuz (UserConfiguration, AccountConfiguration vb.) 
        // tüm IEntityTypeConfiguration arayüzünü uygulayan sınıfları bulur ve otomatik olarak uygular.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankDbContext).Assembly);
    }
}
