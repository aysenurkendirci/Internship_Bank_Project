using System.Text.Json;
using Bank.Api.Serialization;

namespace Bank.UnitTests.Api;

public sealed class UtcDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new UtcDateTimeConverter() } };

    [Fact]
    public void Write_UnspecifiedKind_IsMarkedAsUtc()
    {
        // This is what Dapper returns for DATETIME2 columns.
        var fromDatabase = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(fromDatabase, Options);

        Assert.Equal("\"2026-01-15T09:30:00Z\"", json);
    }

    [Fact]
    public void Write_LocalKind_IsConvertedToUtc()
    {
        var local = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Local);

        var json = JsonSerializer.Serialize(local, Options);

        Assert.Equal(JsonSerializer.Serialize(local.ToUniversalTime()), json);
        Assert.EndsWith("Z\"", json);
    }

    [Fact]
    public void Read_ValueWithOffset_IsNormalisedToUtc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2026-01-15T12:30:00+03:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc), value);
    }
}
