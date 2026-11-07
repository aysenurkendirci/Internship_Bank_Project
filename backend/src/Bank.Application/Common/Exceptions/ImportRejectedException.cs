namespace Bank.Application.Common.Exceptions;

/// <summary>A file that cannot be imported as a whole; <see cref="Errors"/> points to the offending lines.</summary>
public sealed class ImportRejectedException : AppException
{
    public IReadOnlyList<string> Errors { get; }

    public ImportRejectedException(IReadOnlyList<string> errors)
        : base("Dosyada hatalı satırlar var. Hiçbir satır kaydedilmedi; hataları düzeltip tekrar yükleyin.")
        => Errors = errors;
}
