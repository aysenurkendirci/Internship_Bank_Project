using Bank.Application.Common.Exceptions;

namespace Bank.Application.Common;

/// <summary>Converts row versions to and from the opaque string clients see.</summary>
public static class RowVersion
{
    public static string Encode(byte[] version) => Convert.ToBase64String(version);

    /// <summary>
    /// Accepts the value with or without surrounding quotes, since HTTP If-Match headers quote entity tags.
    /// </summary>
    public static byte[] Decode(string value)
    {
        try
        {
            return Convert.FromBase64String(value.Trim().Trim('"'));
        }
        catch (FormatException)
        {
            throw new BadRequestException("Kayıt sürümü geçersiz.");
        }
    }
}
