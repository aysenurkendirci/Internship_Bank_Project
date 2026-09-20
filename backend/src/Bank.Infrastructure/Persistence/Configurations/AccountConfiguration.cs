using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);

        // Kullanıcı silinirse, onun hesapları da silinsin (Cascade).
        builder.HasOne(account => account.User)
               .WithMany(user => user.Accounts)
               .HasForeignKey(account => account.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // IBAN Value Object Dönüşümü
        builder.Property(account => account.Iban)
               .HasConversion(
                   ibanValue => ibanValue.Value, 
                   value => Iban.Create(value))
               .IsRequired()
               .HasMaxLength(30);

        builder.Property(account => account.AccountNumber).IsRequired().HasMaxLength(20);
        
        // Bakiye alanında virgülden sonra kaç hane olacağını (18 tam sayı, 2 ondalık) belirtiyoruz.
        builder.Property(account => account.Balance).HasColumnType("decimal(18,2)");
        
        builder.Property(account => account.Currency).IsRequired().HasMaxLength(3); // Örn: TRY, USD

        // Kilit Mekanizması (Concurrency Token)
        builder.Property(account => account.RowVersion).IsRowVersion();
    }
}
