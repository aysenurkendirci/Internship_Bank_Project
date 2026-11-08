# ADR-0007: Transferlerde idempotency ve kilitleme

**Durum:** Kabul edildi

## Bağlam

Para transferi iki ayrı sorunla karşılaşıyor ve ikisi de sessizce yanlış sonuç üretebiliyor:

1. **Tekrarlanan istek.** İstemci "Gönder"e basar, cevap ağda kaybolur, istemci isteği tekrarlar. Sunucu ilkini işlediyse para iki kez gider.
2. **Eşzamanlı istek.** Aynı bakiyeye iki transfer aynı anda gelirse ikisi de "bakiye yeterli" görüp geçebilir.

## Karar

**Idempotency-Key başlığı zorunlu.** İstemci her transfer için bir anahtar üretir ve tekrarlarda aynısını gönderir.
- `(UserId, IdempotencyKey)` benzersiz. Aynı anahtarla ikinci istek yeni transfer yerine ilkinin sonucunu döner (`200`, `Idempotent-Replayed: true`).
- Anahtarın yanında isteğin SHA-256 özeti saklanır. Aynı anahtar farklı bir istekle gelirse `422` döner: eski sonucu dönmek, yeni isteğin hiç çalışmadığını gizlerdi.
- Özet, alıcı adını harf büyüklüğü, boşluk ve Türkçe harflerden bağımsız (`TextFolding`), tutarı sabit biçimde yazarak hesaplar. Aynı istek biraz farklı yazılsa bile aynı sayılır.
- Anahtarlar kullanıcıya aittir: iki kullanıcı aynı anahtarı kullanabilir.

**Kilitleme veritabanında, transaction içinde.**
- Anahtar aranırken `UPDLOCK, HOLDLOCK`: satır henüz yokken de anahtar aralığı kilitlenir, aynı anahtarla eşzamanlı gelen tekrarlar birbirini bekler ve ilki commit olunca onu bulur.
- Hesap satırları `UPDLOCK` ile okunur. İkinci transfer ilkinin commit'ini bekler ve güncel bakiyeyi görür.
- Hesaplar her zaman **id'ye göre artan sırada** kilitlenir. A→B ve B→A aynı anda gelse bile ikisi de önce küçük id'yi kilitler, birbirini bekleyen bir döngü oluşmaz.
- Bakiye `Balance >= 0` constraint'iyle de korunur. Uygulama katmanı yanılsa bile veritabanı eksi bakiyeyi reddeder.

## Değerlendirilen alternatifler

- **Optimistic concurrency (RowVersion) ile yeniden deneme.** Kişisel finans kayıtlarında bunu kullanıyoruz, ama para transferinde çakışma anında yeniden denemek yerine beklemek daha basit ve kullanıcıya hata göstermiyor.
- **Serializable izolasyon.** Tüm transaction'ı serileştirir, gereğinden fazla bloklar ve deadlock'u artırır.
- **Sadece uygulama içinde kilit (`lock`, dağıtık kilit).** Birden fazla API örneğinde çalışmaz.

## Sonuçlar

- Testler yarışı gerçekten üretir: 100 TL'ye 30'ar TL'lik on paralel transfer (tam üçü geçer), ters yönlü paralel transferler, aynı anahtarla beş eşzamanlı tekrar. Kilit ipuçları koddan silindiğinde üç test de kırılır.
- Veritabanı deadlock'u yine de bir kurtarma yolu gerektirir: SQL Server bir döngü bulursa bir tarafı kurban seçer (`1205`). Bu durumda
  transaction'ın yeniden denenmesi güvenlidir, çünkü hiçbir şey commit edilmemiştir. `TransactionBehavior` bu hatada komutu en fazla
  üç kez çalıştırır (kısa, artan bekleme ile). Diğer hatalar yeniden denenmez.
