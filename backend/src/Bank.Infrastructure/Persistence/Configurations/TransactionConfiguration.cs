using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.TransactionReference).IsRequired().HasMaxLength(50);
        
        // Eşsiz kilit anahtarı (Idempotency Key) indeksleniyor ki veritabanı aynı anahtardan iki tane eklenmesine izin vermesin.
        builder.HasIndex(t => t.IdempotencyKey).IsUnique();
        
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        
        builder.Property(t => t.Category).HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(255);
    }
}
