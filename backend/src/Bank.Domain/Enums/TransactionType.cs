namespace Bank.Domain.Enums;

public enum TransactionType
{
    InternalTransfer = 1,   // Havale (Kendi veya Banka İçi Hesaplar Arası)
    FastTransfer = 2,       // FAST (Fonu Anlık ve Sürekli Transfer)
    EftTransfer = 3,        // EFT (Başka Banka)
    IyzicoTopUp = 4,        // iyzico Sanal POS Bakiye Yükleme
    SplitBill = 5,          // Alman Usulü Hesap Bölüşümü
    RoundUpSavings = 6      // Otomatik Yuvarlama Kumbarası
}
