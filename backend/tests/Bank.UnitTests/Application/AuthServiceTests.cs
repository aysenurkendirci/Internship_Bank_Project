using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Application.Services;
using Bank.Contracts.Auth;
using NSubstitute;

namespace Bank.UnitTests.Application;

public sealed class AuthServiceTests
{
    private const string TcNo = "12345678901";
    private const string StoredHash = "stored-hash";

    private readonly IAuthRepository _repo = Substitute.For<IAuthRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokens = Substitute.For<ITokenGenerator>();
    private readonly AuthService _sut;

    private static readonly UserRow ExistingUser = new()
    {
        UserId = 7,
        TcNo = TcNo,
        FirstName = "Ayşe",
        LastName = "Yılmaz",
        Email = "ayse@example.com"
    };

    public AuthServiceTests()
    {
        _sut = new AuthService(_repo, _hasher, _tokens);
        _tokens.Generate(Arg.Any<long>(), Arg.Any<string?>()).Returns("jwt");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAndRecordsLogin()
    {
        GivenExistingUserWithPassword("correct");

        var response = await _sut.LoginAsync(new LoginRequest(TcNo, "correct"));

        Assert.Equal(7, response.UserId);
        Assert.Equal("Ayşe Yılmaz", response.FullName);
        Assert.Equal("jwt", response.Token);
        await _repo.Received(1).UpdateLastLoginAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WithWrongPassword_FailsWithoutRecordingLogin()
    {
        GivenExistingUserWithPassword("correct");

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _sut.LoginAsync(new LoginRequest(TcNo, "wrong")));

        await _repo.DidNotReceive().UpdateLastLoginAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // Unknown users and wrong passwords must be indistinguishable to the caller,
    // otherwise the endpoint reveals which identity numbers are registered.
    [Fact]
    public async Task Login_UnknownUserAndWrongPassword_FailIdentically()
    {
        GivenExistingUserWithPassword("correct");

        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _sut.LoginAsync(new LoginRequest(TcNo, "wrong")));
        var unknownUser = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _sut.LoginAsync(new LoginRequest("99999999999", "anything")));

        Assert.Equal(wrongPassword.Message, unknownUser.Message);
    }

    [Fact]
    public async Task Register_WithExistingTcNo_ThrowsConflictAndCreatesNothing()
    {
        _repo.GetUserByTcOrDefaultAsync(TcNo, Arg.Any<CancellationToken>()).Returns(ExistingUser);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(NewRegistration()));

        await _repo.DidNotReceive().CreateUserAsync(
            Arg.Any<RegisterRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        _hasher.Hash("P@ssw0rd!").Returns("hashed-value");
        _repo.CreateUserAsync(Arg.Any<RegisterRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(ExistingUser);

        await _sut.RegisterAsync(NewRegistration());

        await _repo.Received(1).CreateUserAsync(
            Arg.Any<RegisterRequest>(), "hashed-value", Arg.Any<CancellationToken>());
    }

    private void GivenExistingUserWithPassword(string password)
    {
        _repo.GetUserByTcOrDefaultAsync(TcNo, Arg.Any<CancellationToken>()).Returns(ExistingUser);
        _repo.GetCredentialsAsync(ExistingUser.UserId, Arg.Any<CancellationToken>())
             .Returns(new CredentialRow { UserId = ExistingUser.UserId, PasswordHash = StoredHash });
        _hasher.Verify(Arg.Any<string>(), StoredHash).Returns(call => call.ArgAt<string>(0) == password);
    }

    private static RegisterRequest NewRegistration()
        => new(TcNo, "Ayşe", "Yılmaz", "ayse@example.com", "5550000000", "P@ssw0rd!", "Personal");
}
