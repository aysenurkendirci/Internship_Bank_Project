# ADR-0006: Application katmanında CQRS (MediatR + FluentValidation)

**Durum:** Kabul edildi

## Bağlam

Application katmanı şimdiye kadar özellik başına bir servis sınıfıyla ilerledi (`CreditCardService`, `AnalyticsService` vb.).
Servisler küçük ve testliydi, ama üç sorun birikmeye başlamıştı:

1. **Okuma ve yazma aynı sınıfta.** `CreditCardService` hem listeleme hem güncelleme yapıyor. Bağımlılıkları (`TimeProvider`,
   logger, ikinci bir repository) her metot için değil, en çok isteyen metot için alınıyor.
2. **Ortak işler dağınık.** Sorgu parametresi kontrolleri controller'larda `[Range]` attribute'u olarak, tarih aralığı kontrolü
   servisin içinde, süre ölçümü hiçbir yerde değil.
3. **Para transferi geliyor (Sprint 9).** Transfer gibi yazma işlemleri doğrulama, idempotency ve transaction ister. Bu işleri her
   servise elle yazmak yerine tek bir boru hattında toplamak gerekiyor.

## Karar

- Her kullanım senaryosu bir **command** (durum değiştirir) ya da **query** (sadece okur) ve tek bir handler'ı var. Bir senaryo
  bir dosya: `Features/<Alan>/<Senaryo>.cs` içinde istek kaydı ve handler'ı birlikte duruyor.
- Controller'lar sadece HTTP'yi biliyor: isteği command/query'ye çevirip `ISender`'a veriyor.
- Ortak işler MediatR pipeline behavior'ları olarak yazılıyor:
  - `LoggingBehavior`: her isteğin süresini ölçer, 500 ms'yi aşanları uyarı olarak loglar. İsteğin içeriğini değil, sadece tip
    adını loglar (TC, şifre ve tutarlar log'a girmez).
  - `ValidationBehavior`: isteğe ait FluentValidation validator'larını handler'dan önce çalıştırır. Hata 400 ve ASP.NET'in kendi
    model doğrulamasıyla aynı `errors` biçiminde döner.
- **Doğrulama sınırı:** İstek gövdesinin şekli (zorunlu alan, uzunluk, izinli değerler) contract'lardaki data annotation'larla HTTP
  kenarında kontrol edilmeye devam ediyor. FluentValidation, kullanım senaryosuna ait kurallar için: sorgu aralıkları, alanları
  birbiriyle karşılaştıran kurallar, gövdeden gelmeyen girdiler. Aynı kural iki yerde yazılmıyor.
- **Okuma tarafı domain'e girmek zorunda değil.** Query handler'lar gerektiğinde repository'den doğrudan ekranın ihtiyacı olan
  satırları alabilir. Yazma tarafı her zaman domain nesneleri üzerinden geçiyor, iş kuralları orada.

## Paket ve lisans

- **MediatR 12.5.0.** 13 ve sonrası sürümler Apache-2.0'dan ticari lisans / RPL-1.5 ikilisine geçti. 12.5.0 Apache-2.0 ile yayınlanan
  son sürüm ve bu proje için ihtiyaç duyulan her şeyi (request/handler, pipeline behavior) içeriyor. Lisans değişikliği bilinçli bir
  sürüm sabitlemesi gerektirdiği için bu ADR'de yazılı. İleride sürüm yükseltilecekse lisans koşulu yeniden değerlendirilmeli.
- **FluentValidation 12.1.1** (Apache-2.0).

## Değerlendirilen alternatifler

- **Servislerle devam etmek.** Bugünkü boyutta işliyordu, ama transfer ile birlikte gelecek ortak işler (doğrulama, idempotency,
  transaction) her servise ayrı ayrı dağılacaktı.
- **MediatR olmadan kendi `ICommandHandler<T>` arayüzlerimiz.** Mümkün, ama dispatcher ve decorator kaydını kendimiz yazmış olurduk.
  12.5.0 sabitlemesiyle lisans sorunu olmadan aynı sonucu daha az kodla alıyoruz.
- **Ayrı okuma veritabanı / event sourcing.** Tek veritabanlı bir monolit için gereksiz. CQRS burada kodun ayrılması anlamında,
  verinin ayrılması anlamında değil.

## Sonuçlar

- Servis arayüzleri ve sınıfları kalkıyor. Mevcut HTTP entegrasyon testleri (55 adet) hiç değişmeden refactor'ün güvenlik ağı oluyor.
- Yeni bir kullanım senaryosu eklemek tek bir dosya ve gerekiyorsa bir validator demek.
- Bir handler'ın başka bir handler'ı `ISender` ile çağırması yasak. Paylaşılan okuma mantığı (ör. projeksiyon girdisi) ayrı bir
  sınıfta kalır ve iki handler onu doğrudan kullanır.
