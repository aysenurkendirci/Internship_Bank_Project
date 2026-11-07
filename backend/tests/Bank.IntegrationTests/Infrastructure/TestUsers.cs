using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bank.Contracts.Auth;

namespace Bank.IntegrationTests.Infrastructure;

/// <summary>Registers fresh users so every test owns its data and tests never depend on each other.</summary>
public static class TestUsers
{
    public const string Password = "P@ssw0rd!";

    private static int _sequence;

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var registration = NewRegistration();

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    public static RegisterRequest NewRegistration()
    {
        var n = Interlocked.Increment(ref _sequence);
        return new RegisterRequest(
            NewTcKimlikNo(n),
            "Test",
            "Kullanıcı",
            $"user{n}-{Guid.NewGuid():N}@example.com",
            "5550000000",
            Password,
            "Personal");
    }

    /// <summary>Builds a checksum-valid identity number from a sequence so registrations never collide.</summary>
    public static string NewTcKimlikNo(int sequence)
    {
        var first9 = (100_000_000 + sequence).ToString();
        var d = first9.Select(c => c - '0').ToArray();

        var tenth = (((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;

        return $"{first9}{tenth}{eleventh}";
    }
}
