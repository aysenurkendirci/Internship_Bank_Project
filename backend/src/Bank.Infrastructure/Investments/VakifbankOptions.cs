namespace Bank.Infrastructure.Investments;

public sealed class VakifbankOptions
{
    public string BaseUrl { get; set; } = "";
    public string TokenPath { get; set; } = ""; 
    public string GrantType { get; set; } = "client_credentials";
    public string Scope { get; set; } = "";

    public string CurrencyRatesPath { get; set; } = "";
    public string CurrencyCalculatorPath { get; set; } = "";
    public string GoldPricesPath { get; set; } = "";

    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
