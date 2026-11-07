using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TransactionImportTests
{
    private const string ImportUrl = "/api/finance/transactions/import";
    private const string ListUrl = "/api/finance/transactions";

    private const string March = """
        Tarih;Açıklama;Tutar;Kategori
        01.03.2026;KİRA ÖDEMESİ;-20.000,00;Konut
        05.03.2026;KAHVE DÜNYASI;-85,50;Kafe
        05.03.2026;KAHVE DÜNYASI;-85,50;Kafe
        15.03.2026;MAAŞ;45.000,00;Maaş
        """;

    private readonly ApiFactory _factory;

    public TransactionImportTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Import_StoresEveryLineIncludingIdenticalOnes()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var result = await ImportAsync(client, March);
        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(ListUrl);

        Assert.Equal(new[] { 4, 4, 0 }, new[] { result.TotalRows, result.ImportedRows, result.DuplicateRows });
        Assert.Equal(4, page!.TotalCount);
        Assert.Equal(2, page.Items.Count(t => t.Description == "KAHVE DÜNYASI"));
        Assert.Equal(-20_000m, page.Items.Single(t => t.Category == "Konut").Amount);
    }

    [Fact]
    public async Task ImportingTheSameFileAgain_AddsNothing()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await ImportAsync(client, March);

        var second = await ImportAsync(client, March);
        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(ListUrl);

        Assert.Equal(0, second.ImportedRows);
        Assert.Equal(4, second.DuplicateRows);
        Assert.Equal(4, page!.TotalCount);
    }

    [Fact]
    public async Task OverlappingStatement_OnlyAddsTheNewLines()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await ImportAsync(client, March);

        var overlapping = await ImportAsync(client, """
            Tarih;Açıklama;Tutar
            05.03.2026;KAHVE DÜNYASI;-85,50
            05.03.2026;KAHVE DÜNYASI;-85,50
            15.03.2026;MAAŞ;45.000,00
            02.04.2026;ELEKTRİK;-640,20
            """);

        Assert.Equal(1, overlapping.ImportedRows);
        Assert.Equal(3, overlapping.DuplicateRows);
    }

    [Fact]
    public async Task FileWithInvalidLines_IsRejectedAsAWholeWithLineNumbers()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await PostCsvAsync(client, """
            Tarih;Açıklama;Tutar
            01.03.2026;KİRA;-20.000,00
            31.02.2026;GEÇERSİZ;-10
            05.03.2026;BOZUK;1.2.3
            """);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(ListUrl);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = body.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(2, errors.Count);
        Assert.StartsWith("Satır 3:", errors[0]);
        Assert.StartsWith("Satır 4:", errors[1]);
        Assert.Equal(0, page!.TotalCount);
    }

    [Fact]
    public async Task Windows1254File_IsImportedWithTurkishLetters()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var client = await _factory.CreateAuthenticatedClientAsync();

        await ImportAsync(client, "Tarih;Açıklama;Tutar\n05.03.2026;ŞOK MARKET ÇAĞLAYAN;-89,90\n", Encoding.GetEncoding(1254));
        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(ListUrl);

        Assert.Equal("ŞOK MARKET ÇAĞLAYAN", Assert.Single(page!.Items).Description);
    }

    [Fact]
    public async Task History_IsPrivateToItsOwner()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();
        await ImportAsync(owner, March);

        var page = await stranger.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(ListUrl);

        Assert.Equal(0, page!.TotalCount);
    }

    [Fact]
    public async Task List_FiltersByDateAndPages()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await ImportAsync(client, March);

        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>(
            $"{ListUrl}?from=2026-03-02&to=2026-03-31&page=1&pageSize=2");

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(new DateOnly(2026, 3, 15), page.Items[0].Date);
    }

    [Fact]
    public async Task FileOverTwoMegabytes_IsRefused()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var huge = "Tarih;Açıklama;Tutar\n" + string.Concat(Enumerable.Repeat("05.03.2026;" + new string('X', 180) + ";-1,00\n", 12_000));

        var response = await PostCsvAsync(client, huge);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    private static async Task<ImportResultResponse> ImportAsync(HttpClient client, string csv, Encoding? encoding = null)
    {
        var response = await PostCsvAsync(client, csv, encoding);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResultResponse>())!;
    }

    private static Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string csv, Encoding? encoding = null)
    {
        var file = new ByteArrayContent((encoding ?? new UTF8Encoding(false)).GetBytes(csv));
        file.Headers.ContentType = new("text/csv");
        var form = new MultipartFormDataContent { { file, "file", "ekstre.csv" } };
        return client.PostAsync(ImportUrl, form);
    }
}
