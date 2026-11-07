using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bank.Api.Serialization;

/// <summary>
/// Every timestamp in this system is stored and processed in UTC, but values read from
/// DATETIME2 columns arrive with <see cref="DateTimeKind.Unspecified"/> and would be written
/// without an offset. Clients then interpret them as local time and shift them by the
/// user's UTC offset. This converter enforces the UTC convention at the API boundary.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        writer.WriteStringValue(utc);
    }
}
