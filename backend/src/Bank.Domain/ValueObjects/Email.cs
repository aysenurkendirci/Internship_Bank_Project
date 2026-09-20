using System.Text.RegularExpressions;
using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// DDD Value Object: Email
/// E-posta adresi doğrulama ve biçimlendirme.
/// </summary>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailRegex.IsMatch(value))
            throw new DomainException($"Geçersiz e-posta adresi: {value}");

        Value = value.Trim().ToLowerInvariant();
    }

    public static Email Create(string value) => new(value);

    public override string ToString() => Value;
}
