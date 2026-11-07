using Bank.Infrastructure.Security;

namespace Bank.UnitTests.Infrastructure;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_NeverContainsThePlainPassword()
    {
        var hash = _sut.Hash("P@ssw0rd!");

        Assert.DoesNotContain("P@ssw0rd!", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // A random salt per hash means equal passwords cannot be spotted in the database.
        Assert.NotEqual(_sut.Hash("P@ssw0rd!"), _sut.Hash("P@ssw0rd!"));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("P@ssw0rd!");

        Assert.True(_sut.Verify("P@ssw0rd!", hash));
    }

    [Theory]
    [InlineData("p@ssw0rd!")]
    [InlineData("P@ssw0rd")]
    [InlineData("")]
    public void Verify_WrongPassword_ReturnsFalse(string attempt)
    {
        var hash = _sut.Hash("P@ssw0rd!");

        Assert.False(_sut.Verify(attempt, hash));
    }
}
