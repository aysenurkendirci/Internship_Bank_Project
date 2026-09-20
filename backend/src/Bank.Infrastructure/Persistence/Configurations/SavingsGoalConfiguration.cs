using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bank.Domain.Entities;

namespace Bank.Infrastructure.Persistence.Configurations;

public sealed class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Title).IsRequired().HasMaxLength(100);
        builder.Property(s => s.IconName).HasMaxLength(50);
        
        builder.Property(s => s.TargetAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.CurrentAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(3);

        // Kullanıcı silinirse, birikim hedefleri de silinir.
        builder.HasOne(s => s.User)
               .WithMany(u => u.SavingsGoals)
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
