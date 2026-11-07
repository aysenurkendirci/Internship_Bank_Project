# ADR-0008: Transferler çift taraflı kayıtla tutulur

**Durum:** Kabul edildi

## Bağlam

Bir bakiyenin neden o değerde olduğunu açıklayabilmek, bankacılıkta temel bir gereklilik: denetim, itiraz ve hata ayıklama buna dayanır.
`Accounts.Balance` sütununu güncellemek yetmez, çünkü bu sütun yalnızca sonucu söyler, nedenini söylemez ve yanlış güncellenmişse fark edilmez.

## Karar

- Her transfer `payments.LedgerEntries` tablosuna **bir borç (Debit) ve bir alacak (Credit)** satırı olarak yazılır. Satırlar aynı tutarı taşır, yani para hiçbir zaman oluşmaz ya da kaybolmaz.
- Her satır, işlemden sonraki bakiyeyi (`BalanceAfter`) taşır. Hesap ekstresi doğrudan bu tablodan okunur.
- Kayıtlar eklenir, hiçbir zaman güncellenmez ya da silinmez. Hata düzeltmesi, ters yönde yeni bir transfer olarak yapılır.
- Kurallar domain'de (`FundsTransfer`): aynı hesap, aynı para birimi, tam kuruş, aktif hesaplar, yeterli bakiye ve alıcı adının hesap sahibiyle eşleşmesi. İki hesap da kontrol edilmeden hiçbir bakiye değiştirilmez.
- Veritabanı kuralı tekrar eder: `(TransferId, Side)` benzersiz, tutar pozitif, bakiye eksi olamaz. Uygulama hatası olsa bile tablo tutarsız duruma geçemez.
- `dbo.Transactions` müşteriye dönük hareket listesi (panel) olarak yazılmaya devam eder. Gerçeğin kaynağı ledger'dır.

## Değerlendirilen alternatif

- **Sadece bakiye sütunu ve işlem listesi.** Basit, ama bakiye ile hareketlerin toplamı ayrışırsa hangisinin doğru olduğu söylenemez.
- **Event sourcing (bakiye = olayların toplamı).** Daha güçlü, ama bu ölçekte gereksiz karmaşıklık. Ledger + bakiye sütunu + constraint yeterli güvenceyi daha az maliyetle veriyor.

## Sonuçlar

- Bir testin SQL'i, her transferin tam iki satırı olduğunu ve borç/alacağın eşit olduğunu doğrular.
- Bakiye sütunu bir önbellek gibi davranır: ledger'dan yeniden hesaplanıp karşılaştırılabilir (ileride bir uzlaştırma işi olarak eklenebilir).
