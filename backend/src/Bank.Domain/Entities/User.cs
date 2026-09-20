using Bank.Domain.ValueObjects;

namespace Bank.Domain.Entities;

/// <summary>
/// DDD Aggregate Root: Banka Kullanıcısı / Müşterisi (FinGen Genç Müşteri)
/// Encapsulation (Kapsülleme) ilkelerine uygun şekilde tasarlanmıştır.
/// </summary>
public sealed class User
{
    public long Id { get; private set; }
    public TcNo TcNo { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string MembershipTier { get; private set; } = "FinGen Young";
    public int FinGenScore { get; private set; } = 100;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Navigation Properties
    public ICollection<Account> Accounts { get; private set; } = new List<Account>();
    public ICollection<SavingsGoal> SavingsGoals { get; private set; } = new List<SavingsGoal>();

    private User() { } // EF Core için private ctor

    public User(string tcNo, string firstName, string lastName, string email, string phone, string passwordHash)
    {
        TcNo = ValueObjects.TcNo.Create(tcNo);
        FirstName = firstName;
        LastName = lastName;
        Email = ValueObjects.Email.Create(email);
        Phone = phone;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void AddFinGenScore(int points)
    {
        if (points > 0)
            FinGenScore += points;
    }
}
