using System.Globalization;
using System.Text;
using Bank.Domain.Common;
using Microsoft.VisualBasic.FileIO;

namespace Bank.Application.Imports;

/// <summary>
/// Reads bank statement CSV exports into <see cref="StatementLine"/>s. Every problem is reported
/// with its line number; the caller decides whether a file with errors is accepted.
/// </summary>
/// <remarks>
/// Real exports differ in ways this class absorbs:
/// <list type="bullet">
///   <item>Encoding: UTF-8, or Windows-1254 when the file was saved from Excel on a Turkish system.</item>
///   <item>Delimiter: ';' (common, since ',' is the decimal separator in Turkish), ',' or tab.</item>
///   <item>Headers: Turkish or English, any casing; amount as one signed column or as debit/credit columns.</item>
///   <item>Amounts and dates in Turkish or international formats (see <see cref="StatementAmount"/>).</item>
/// </list>
/// </remarks>
public static class StatementCsvParser
{
    public const int MaxLines = 10_000;
    private const int MaxDescriptionLength = 200;
    private const int MaxCategoryLength = 50;

    private static readonly string[] DateFormats =
        ["dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss"];

    private static readonly string[] DateHeaders = ["tarih", "islem tarihi", "date", "transaction date"];
    private static readonly string[] DescriptionHeaders = ["aciklama", "islem aciklamasi", "description", "details"];
    private static readonly string[] AmountHeaders = ["tutar", "islem tutari", "amount"];
    private static readonly string[] DebitHeaders = ["borc", "debit"];
    private static readonly string[] CreditHeaders = ["alacak", "credit"];
    private static readonly string[] CategoryHeaders = ["kategori", "category"];

    static StatementCsvParser()
    {
        // Windows-1254 is not available on .NET Core until the code page provider is registered.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static StatementParseResult Parse(byte[] content)
    {
        var text = Decode(content);
        var errors = new List<StatementLineError>();
        var lines = new List<StatementLine>();

        var headerLine = text.Split('\n', 2)[0];
        if (string.IsNullOrWhiteSpace(headerLine))
            return Fail(1, "Dosya boş veya başlık satırı yok.");

        using var reader = new TextFieldParser(new StringReader(text))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        reader.SetDelimiters(DetectDelimiter(headerLine));

        var columns = Columns.From(reader.ReadFields() ?? []);
        if (columns.Error is { } headerError)
            return Fail(1, headerError);

        while (!reader.EndOfData)
        {
            var lineNumber = (int)reader.LineNumber;
            string[]? fields;
            try
            {
                fields = reader.ReadFields();
            }
            catch (MalformedLineException)
            {
                errors.Add(new(lineNumber, "Satır okunamadı (kapanmamış tırnak işareti olabilir)."));
                continue;
            }

            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
                continue;

            if (lines.Count + errors.Count >= MaxLines)
                return Fail(lineNumber, $"Dosya en fazla {MaxLines} satır içerebilir.");

            var parsed = ParseLine(lineNumber, fields, columns);
            if (parsed.Line is not null)
                lines.Add(parsed.Line);
            else
                errors.Add(new(lineNumber, parsed.Error!));
        }

        if (lines.Count == 0 && errors.Count == 0)
            return Fail(1, "Dosyada işlem satırı bulunamadı.");

        return new StatementParseResult(lines, errors);
    }

    private static (StatementLine? Line, string? Error) ParseLine(int lineNumber, string[] fields, Columns columns)
    {
        string? Field(int? index) => index is { } i && i < fields.Length ? fields[i] : null;

        if (!DateTime.TryParseExact(Field(columns.Date), DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return (null, $"Tarih okunamadı: '{Field(columns.Date)}'.");

        var description = Field(columns.Description)?.Trim();
        if (string.IsNullOrEmpty(description))
            return (null, "Açıklama boş.");
        if (description.Length > MaxDescriptionLength)
            return (null, $"Açıklama en fazla {MaxDescriptionLength} karakter olabilir.");

        if (!TryReadAmount(fields, columns, out var amount, out var amountError))
            return (null, amountError);
        if (amount == 0m)
            return (null, "Tutar sıfır olamaz.");

        var category = Field(columns.Category)?.Trim();
        if (category?.Length > MaxCategoryLength)
            return (null, $"Kategori en fazla {MaxCategoryLength} karakter olabilir.");

        return (new StatementLine(lineNumber, DateOnly.FromDateTime(date), description, amount,
            string.IsNullOrEmpty(category) ? null : category), null);
    }

    private static bool TryReadAmount(string[] fields, Columns columns, out decimal amount, out string? error)
    {
        string? Field(int? index) => index is { } i && i < fields.Length ? fields[i] : null;
        error = null;

        if (columns.Amount is not null)
        {
            if (StatementAmount.TryParse(Field(columns.Amount), out amount))
                return true;
            error = $"Tutar okunamadı: '{Field(columns.Amount)}'.";
            return false;
        }

        // Debit/credit layout: one of the two columns is filled per line.
        var debitText = Field(columns.Debit);
        var creditText = Field(columns.Credit);
        decimal debit = 0m, credit = 0m;

        if (!string.IsNullOrWhiteSpace(debitText) && !StatementAmount.TryParse(debitText, out debit))
            error = $"Borç tutarı okunamadı: '{debitText}'.";
        else if (!string.IsNullOrWhiteSpace(creditText) && !StatementAmount.TryParse(creditText, out credit))
            error = $"Alacak tutarı okunamadı: '{creditText}'.";

        amount = Math.Abs(credit) - Math.Abs(debit);
        return error is null;
    }

    private static string Decode(byte[] content)
    {
        var utf8Bom = Encoding.UTF8.GetPreamble();
        if (content.AsSpan().StartsWith(utf8Bom))
            return Encoding.UTF8.GetString(content, utf8Bom.Length, content.Length - utf8Bom.Length);

        try
        {
            // Strict decoding throws on byte sequences that are not valid UTF-8 ...
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            // ... which is what a Turkish Excel export (Windows-1254) looks like.
            return Encoding.GetEncoding(1254).GetString(content);
        }
    }

    private static string DetectDelimiter(string headerLine)
        => new[] { ";", ",", "\t" }.MaxBy(d => headerLine.Count(c => c == d[0]))!;

    private static StatementParseResult Fail(int lineNumber, string message)
        => new([], [new StatementLineError(lineNumber, message)]);

    private sealed record Columns(int? Date, int? Description, int? Amount, int? Debit, int? Credit, int? Category, string? Error)
    {
        public static Columns From(string[] headers)
        {
            var normalized = headers.Select(TextFolding.Fold).ToList();
            int? Find(string[] names) => normalized.FindIndex(names.Contains) is var i and >= 0 ? i : null;

            var date = Find(DateHeaders);
            var description = Find(DescriptionHeaders);
            var amount = Find(AmountHeaders);
            var debit = Find(DebitHeaders);
            var credit = Find(CreditHeaders);
            var category = Find(CategoryHeaders);

            string? error = null;
            if (date is null || description is null)
                error = "Başlık satırında 'Tarih' ve 'Açıklama' kolonları bulunmalıdır.";
            else if (amount is null && (debit is null || credit is null))
                error = "Başlık satırında 'Tutar' kolonu ya da 'Borç' ve 'Alacak' kolonları bulunmalıdır.";

            return new Columns(date, description, amount, debit, credit, category, error);
        }
    }
}
