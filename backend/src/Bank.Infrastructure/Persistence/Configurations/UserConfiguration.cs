using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 1. Primary Key (Birincil Anahtar)
        builder.HasKey(user => user.Id);

        // 2. Özel Tip Dönüşümleri (Value Conversions)
        builder.Property(user => user.TcNo)
               .HasConversion(
                   tcNo => tcNo.Value, 
                   value => TcNo.Create(value))
               .IsRequired()
               .HasMaxLength(11);

        builder.Property(user => user.Email)
               .HasConversion(
                   emailAddress => emailAddress.Value, 
                   value => Email.Create(value))
               .IsRequired();

        // 3. Normal Alanların Ayarlanması
        builder.Property(user => user.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(user => user.LastName).IsRequired().HasMaxLength(100);
        builder.Property(user => user.Phone).IsRequired().HasMaxLength(20);
        builder.Property(user => user.PasswordHash).IsRequired();
    }
}
