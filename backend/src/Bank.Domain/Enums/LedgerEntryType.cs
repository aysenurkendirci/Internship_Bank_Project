namespace Bank.Domain.Enums;

/// <summary>
/// Çift Taraflı Muhasebe (Double-Entry Bookkeeping) Defter Kaydı Türü:
/// Debit (Borç): Hesaptan çıkan/azalan para hareketi.
/// Credit (Alacak): Hesaba giren/artan para hareketi.
/// </summary>
public enum LedgerEntryType
{
    Debit = 1,  // Borç (Hesaptan Çıkış)
    Credit = 2  // Alacak (Hesaba Giriş)
}
