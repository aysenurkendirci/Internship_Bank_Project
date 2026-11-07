using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Application.Features.Auth;
using Bank.Contracts.Auth;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Bank.UnitTests.Application;

public sealed class AuthCommandTests
{
    private const string TcNo = "12345678901";
    private const string StoredHash = "stored-hash";

    private readonly IAuthRepository _repo = Substitute.For<IAuthRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokens = Substitute.For<ITokenGenerator>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly ISessionPolicy _policy = Substitute.For<ISessionPolicy>();
    private readonly RecordingLogger<LoginCommandHandler> _log = new();
    private readonly LoginCommandHandler _login;
    private readonly RegisterCommandHandler _register;

    private static readonly UserRow ExistingUser = new()
    {
        UserId = 7,
        TcNo = TcNo,
        FirstName = "Ayşe",
        LastName = "Yılmaz",
        Email = "ayse@example.com"
    };

    public AuthCommandTests()
    {
        _policy.RefreshTokenLifetime.Returns(TimeSpan.FromHours(8));
        var issuer = new RefreshTokenIssuer(_refreshTokens, _policy, TimeProvider.System);
        _login = new LoginCommandHandler(_repo, _hasher, _tokens, issuer, _log);
        _register = new RegisterCommandHandler(_repo, _hasher, _tokens, issuer, new RecordingLogger<RegisterCommandHandler>());
        _tokens.Generate(Arg.Any<long>()).Returns("jwt");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAndRecordsLogin()
    {
        GivenExistingUserWithPassword("correct");

        var response = await Login(new LoginRequest(TcNo, "correct"));

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
            () => Login(new LoginRequest(TcNo, "wrong")));

        await _repo.DidNotReceive().UpdateLastLoginAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // Unknown users and wrong passwords must be indistinguishable to the caller,
    // otherwise the endpoint reveals which identity numbers are registered.
    [Fact]
    public async Task Login_UnknownUserAndWrongPassword_FailIdentically()
    {
        GivenExistingUserWithPassword("correct");

        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => Login(new LoginRequest(TcNo, "wrong")));
        var unknownUser = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => Login(new LoginRequest("99999999999", "anything")));

        Assert.Equal(wrongPassword.Message, unknownUser.Message);
    }

    [Fact]
    public async Task Register_WithExistingTcNo_ThrowsConflictAndCreatesNothing()
    {
        _repo.GetUserByTcOrDefaultAsync(TcNo, Arg.Any<CancellationToken>()).Returns(ExistingUser);

        await Assert.ThrowsAsync<ConflictException>(() => _register.Handle(new RegisterCommand(NewRegistration()), default));

        await _repo.DidNotReceive().CreateUserAsync(
            Arg.Any<RegisterRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        _hasher.Hash("P@ssw0rd!").Returns("hashed-value");
        _repo.CreateUserAsync(Arg.Any<RegisterRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(ExistingUser);

        await _register.Handle(new RegisterCommand(NewRegistration()), default);

        await _repo.Received(1).CreateUserAsync(
            Arg.Any<RegisterRequest>(), "hashed-value", Arg.Any<CancellationToken>());
    }

    // KVKK: security events are logged, but an identity number never appears in full.
    [Fact]
    public async Task SecurityEvents_AreLogged_WithTheIdentityNumberMasked()
    {
        GivenExistingUserWithPassword("correct");

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(new LoginRequest(TcNo, "wrong")));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(new LoginRequest("99999999990", "x")));
        await Login(new LoginRequest(TcNo, "correct"));

        Assert.Equal(3, _log.Messages.Count);
        Assert.Contains(_log.Messages, m => m.Contains("wrong password") && m.Contains("123******01"));
        Assert.Contains(_log.Messages, m => m.Contains("unknown identity number") && m.Contains("999******90"));
        Assert.DoesNotContain(_log.Messages, m => m.Contains(TcNo) || m.Contains("99999999990"));
    }

    private async Task<AuthResponse> Login(LoginRequest credentials)
        => (await _login.Handle(new LoginCommand(credentials), default)).Response;

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

/// <summary>Keeps the rendered log lines so tests can check what would reach the log store.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
