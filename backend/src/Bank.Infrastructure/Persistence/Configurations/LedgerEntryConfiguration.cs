using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);
        
        builder.Property(l => l.Description).HasMaxLength(255);

        // Ledger Entry (Muhasebe Kaydı), Hesap ve İşlem ile bağlantılıdır.
        builder.HasOne(l => l.Account)
               .WithMany(a => a.LedgerEntries)
               .HasForeignKey(l => l.AccountId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Transaction)
               .WithMany(t => t.LedgerEntries)
               .HasForeignKey(l => l.TransactionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
