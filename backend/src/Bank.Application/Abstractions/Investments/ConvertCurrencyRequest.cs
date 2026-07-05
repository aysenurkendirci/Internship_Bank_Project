namespace Bank.Application.Abstractions.Investments;

public sealed class ConvertCurrencyRequest
{
    public string SourceCurrencyCode { get; set; } = "";
    public decimal SourceAmount { get; set; }
    public string TargetCurrencyCode { get; set; } = "";
}
