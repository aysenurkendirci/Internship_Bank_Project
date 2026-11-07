# ADR-0009: Olaylar transactional outbox ile yayınlanır

**Durum:** Kabul edildi

## Bağlam

Bir transfer tamamlandığında bildirim ve dolandırıcılık izleme gibi başka sistemlerin haberdar olması gerekir. Mesajı transferden sonra
doğrudan RabbitMQ'ya göndermek iki kötü sonuca açık:

- Transfer commit edilir, broker o an kapalıdır: **olay kaybolur.**
- Mesaj gider, ardından transaction geri alınır: **olmamış bir transfer duyurulur.**

Veritabanı ile broker arasında dağıtık transaction (2PC) bu sorunu çözerdi ama operasyonel olarak ağır ve kırılgan.

## Karar

- Olay, değişikliği yapan transaction'ın içinde `messaging.OutboxMessages` tablosuna yazılır. Olay ancak değişiklik commit edilirse vardır.
- Bir arka plan servisi bekleyen satırları `Sequence` sırasıyla RabbitMQ'ya iletir ve `PublishedAt` ile işaretler.
- **En az bir kez teslim.** Yayıncı mesajı gönderip satırı işaretlemeden durursa mesaj tekrar gider. Tüketiciler `MessageId` ile tekrarları ayıklar. Tam bir kez teslim, 2PC gerektirir ve buna değmez.
- **Publisher confirms:** mesaj ancak broker onayladıktan sonra gönderilmiş sayılır.
- **Sıra korunur:** bir mesaj gönderilemezse batch orada durur. Sonraki olayları önce göndermek, aralarındaki sırayı bozardı.
- **Birden fazla örnek:** satırlar `UPDLOCK, READPAST` ile alınır, iki yayıncı aynı satırı göndermek yerine işi paylaşır.
- **Olayda kişisel veri yok.** `TransferCompleted` yalnızca id ve tutar taşır. Ad ya da IBAN'a ihtiyaç duyan tüketici onu ister, böylece kişisel veri her kuyruğa kopyalanmaz (KVKK veri minimizasyonu).
- Broker kapalıyken bile uygulama çalışır: olaylar tabloda birikir ve yayın açılınca gönderilir. Yayın `Messaging:Enabled` ile açılır.

## Değerlendirilen alternatifler

- **Doğrudan yayın (transferden sonra).** Yukarıdaki iki kayıp durumuna açık.
- **Debezium gibi değişiklik yakalama (CDC).** Aynı garantiyi verir, ama ek altyapı ister. Bu ölçekte bir poll döngüsü yeterli.
- **Dağıtık transaction.** Ağır ve kırılgan.

## Sonuçlar

- Testler reddedilen bir transferin olay bırakmadığını, tekrarlanan isteğin ikinci olay üretmediğini, yayıncının sırayı koruduğunu ve broker hatasında satırın bekleyip sonra gittiğini doğrular.
- Yayıncının gerçek bir broker'a karşı testi yalnızca `RABBITMQ_HOST` tanımlıysa çalışır (CI bir RabbitMQ container'ı başlatır).
- Eski, yayınlanmış satırların bir süre sonra temizlenmesi gerekecek. Henüz yazılmadı.
