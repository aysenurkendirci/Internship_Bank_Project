using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardNumber).IsRequired().HasMaxLength(16);
        builder.Property(c => c.CardHolderName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ExpiryDate).IsRequired().HasMaxLength(5); // MM/YY
        builder.Property(c => c.Cvv).IsRequired().HasMaxLength(3);

        builder.Property(c => c.DailyLimit).HasColumnType("decimal(18,2)");
        builder.Property(c => c.MonthlyLimit).HasColumnType("decimal(18,2)");

        // Kart hesaba aittir, hesap silinirse kart da silinir (Cascade)
        builder.HasOne(c => c.Account)
               .WithMany(a => a.Cards)
               .HasForeignKey(c => c.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
