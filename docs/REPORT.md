# Proje Raporu: Kişisel Finansal Yaşam Yönetim Platformu

Bu rapor, projeyi kısa yoldan anlamak isteyen biri için yazıldı. Ayrıntıların hepsi, karşılaştığım sırayla,
[mühendislik günlüğünde](ENGINEERING_LOG.md) duruyor. Burada en öğretici olanları topladım.

## Neyi çözmeye çalıştım

Bütçe uygulamaları genelde "geçen ay ne harcadım?" sorusunu cevaplıyor. Benim derdim bir sonraki soruydu: önümüzdeki aylarda taksitler,
abonelikler, kart ekstreleri ve düzenli giderler üst üste bindiğinde ne olacak? Bir bilgisayarı 3 yerine 6 taksitle alırsam bu nasıl
değişir? Proje bunu görünür kılıyor, ama bilerek hiçbir zaman tavsiye vermiyor. Karar kullanıcının.

## Nereden başladım

Elimde staj sırasında yazılmış küçük bir bankacılık uygulaması vardı: giriş, kayıt ve bir panel. İlk iş onu ayağa kaldırmaya
çalışmaktı. Gördüklerim şunlardı:

- `bin/obj` klasörleri dahil 185 gereksiz dosya git'teydi.
- JWT imzalama anahtarı `appsettings.json` içinde açık duruyordu.
- **Şifreler veritabanında düz metin saklanıyordu.** `PasswordHasher.Hash(p)` girdiyi aynen geri döndürüyordu.
- Hata yönetimi middleware'i yazılmış ama hiç bağlanmamıştı. Her hata stack trace ile istemciye gidiyordu.
- Veritabanı şeması repoda yoktu. Oracle paketleri sadece bir makinede vardı.
- Frontend'in production build'i hiç alınamıyordu.

Sprint 0'ın tamamı bunları kapatmakla geçti. Bu bana bir şey öğretti: yeni özellik eklemeden önce temelin sağlam olduğunu
doğrulamak gerekiyor.

## Yapılanlar (sprint sprint)

| Sprint | Konu | Öne çıkan |
|---|---|---|
| 0 | Temel ve hijyen | Düz metin şifreler PBKDF2'ye geçti, Oracle yerine SQL Server ve DbUp migration'ları, ProblemDetails ile hata yönetimi |
| 1 | Domain modeli | `Money`, kuruş kaybetmeden taksite bölme, ay sonu kaymayan tekrar kuralı, kart ekstre döngüsü |
| 2 | Kalıcılık ve API | Sahiplik kontrolü, `If-Match` ile eşzamanlılık, soft delete, gerçek SQL Server'a karşı entegrasyon testleri |
| 3 | Nakit akışı projeksiyonu | Kart harcamaları son ödeme gününde, ay içi en düşük bakiye |
| 4 | Taksit senaryoları | Ortak ufukta karşılaştırma, sıralama ve öneri yok |
| 5 | Ekstre import'u ve sentetik veri | Encoding, Türkçe sayı formatları, satır parmak izi ile mükerrer koruması |
| 6 | Analitik | Trend, tekrarlayan ödeme, anomali, tahmin ve bunların ölçülmesi |
| 7 | Web arayüzü | Bozuk build'in onarımı, sekiz ekran, bileşen tabanlı yapı |
| 8 | Güvenlik ve KVKK | Rate limiting, güvenlik başlıkları, maskeli log, token'da veri azaltma, CI |

## En öğretici on sorun

1. **Para birimi karışımı.** Panel, TL ve USD bakiyeleri doğrudan topluyordu (15.000 TL + 900 USD = "15.900"). Çözüm tipin içinde:
   `Money` farklı para birimlerinin toplanmasına izin vermiyor. Hata derlenmiyor değil, ama çalışma anında hemen patlıyor ve testle
   kilitli.
2. **Kaybolan kuruş.** 1.000 TL / 3 = 333,33 × 3 = 999,99. Kalan kuruş ilk taksite ekleniyor. Test tek bir örneği değil, "parçaların
   toplamı her zaman bütüne eşittir" değişmezini kontrol ediyor.
3. **Ay sonu kayması.** "Her ayın 31'i" diye tanımlanan bir ödeme, tarih bir önceki tarihten hesaplanınca Şubat'tan sonra kalıcı
   olarak 28'ine kayıyordu. Her tarihi başlangıçtan hesaplamak bunu çözdü. Testin bunu gerçekten yakaladığını, kodu bilerek bozup
   görerek doğruladım.
4. **Haziran'da iki taksit.** Testleri yazarken fark ettim: Mayıs taksidinin son ödeme günü Cumartesi'ye denk gelip 1 Haziran'a
   kayınca, Mayıs'ta hiç ödeme olmuyor, Haziran'da iki ödeme oluyor. Ay bazında bütçe yapan biri bunu göremez. Projenin varlık sebebinin
   küçük bir örneği.
5. **Türkçe sistemde çöken doğrulama.** `[Range(typeof(decimal), "0.01", …)]` sınırı sunucunun kültürüyle okuyordu. Türkçe'de "0.01"
   geçersiz sayı olduğu için her kart oluşturma 500 veriyordu. İngilizce bir test sunucusunda bu hata hiç görünmezdi.
6. **Türkçe I, iki kez.** Önce CSV başlıklarında (`TARİH` eşleşmiyordu), sonra mükerrer kontrolünde (`MİGROS` ile `MIGROS` farklı
   mağaza sanılıyordu). İlk seferde `ToLowerInvariant`'ın davranışını yanlış varsaymıştım. Tahmin yerine küçük bir programla ölçtüm ve
   iki yerdeki çözümü tek bir fonksiyonda birleştirdim.
7. **Sessizce yanlış tutar.** `"1.2.3"` gibi bozuk bir ekstre değeri hata vermek yerine 12,3 olarak okunuyordu. Bir finans uygulamasında
   en tehlikeli hata türü bu: hiçbir şey patlamıyor, sadece sayı yanlış.
8. **İki kahve.** Mükerrer kontrolü aynı gün aynı kafede yapılan iki gerçek alışverişi de silerdi. Parmak izine "dosyadaki aynı
   satırlar arasındaki sırası" eklendi. Aynı dosya tekrar yüklendiğinde hiçbir şey eklenmiyor, ama iki kahve iki kayıt olarak kalıyor.
9. **Veritabanı son savunma hattı olarak.** Testlerin işe yaradığını görmek için sahiplik kontrolünü koddan bilerek çıkardım.
   Uygulama başka kullanıcının kartına abonelik eklemeye izin verdi, ama veritabanındaki bileşik foreign key yine reddetti. Katmanlı
   savunmanın neden gerektiğini en iyi gösteren an buydu.
10. **"Bugün" hangi gün?** Sunucu UTC, kullanıcı İstanbul'da. Gece 00:00–03:00 arasında hem backend'de (`DateTime.UtcNow.Date`) hem
    frontend'de (`toISOString()`) aynı hata vardı: ikisi de dünü veriyordu. İkisi de testle kilitli.

## Ölçülen sonuçlar

**Testler:** 232 unit test, 47 entegrasyon testi (gerçek SQL Server), 25 frontend testi. Build sıfır uyarı ile geçiyor.

**Analitik değerlendirmesi** ([EVALUATION.md](research/EVALUATION.md)). Geliştirme sırasında bakılmamış 50 sentetik profilde yapıldı,
kabul kriterleri ölçümden önce yazıldı:

| Bileşen | Sonuç |
|---|---|
| Tekrarlayan ödeme tespiti | 450/450, yanlış pozitif yok |
| Anomali tespiti | 133/133, 17.022 normal işlemde yanlış alarm yok. Duyarlılık: değişken bir kategoride tipik tutarın ~8 katı %80 yakalanıyor |
| Harcama tahmini | MAPE %12,4. Gürültü tabanı (kâhin) %10,4. %95 aralık gerçekleşeni %95 oranında kapsıyor |

Önceden koyduğum "MAPE ≤ %10" kriteri karşılanmadı. Bunu gizlemek yerine nedenini ölçtüm: gerçek ortalamayı bilen kusursuz bir tahminci
bile %10,4'te kalıyor. Yani kriter bu veri için ulaşılamazdı.

## Araştırma sorularına kısa cevaplar

**Geçmiş ödeme davranışı ile gelecekteki yükümlülükler birlikte gösterildiğinde finansal baskı daha erken görünür olur mu?**
Evet. En net örnek: ayın 1'inde kira, 15'inde maaş olan bir kullanıcının ayı +25.000 TL ile kapanırken, 1–15 arası bakiyesi
−15.000 TL'de. Ay sonu özeti bunu göstermez, gün gün projeksiyon gösterir. Taksit senaryosu da aynı alışverişin 3 taksitte bakiyeyi
5 ay eksiye düşürdüğünü, 6 taksitte düşürmediğini alışverişten önce gösterebiliyor.

**Basit istatistiksel tahminler yeterli mi?** Tek bir sayı olarak orta düzeyde, bir aralık olarak güvenilir. Arayüz bu yüzden tahmini
"14.800 – 21.300 ₺" şeklinde gösteriyor.

## Bilerek bıraktıklarım

- **Refresh token.** Tasarım [ADR-0004](adr/0004-session-hardening.md)'te. Yarım bir oturum mekanizması, hiç olmamasından daha riskli
  olduğu için acele etmedim.
- **Resmi tatiller, döviz kurları, kategori bazlı yasal taksit sınırları.** Hepsi belgelerde açıkça kapsam dışı olarak yazılı.
- **Görsel tasarım.** Ekranlar işlevsel ve bileşenlere ayrılmış durumda. Görsel tasarım Google Stitch'te yapılacak
  ([prompt](design/STITCH_PROMPT.md)).
- Ekranlar henüz bir tarayıcıda gözle kontrol edilmedi.

## Çıkardığım dersler

- **"Testler geçiyor" ile "testler koruyor" aynı şey değil.** Birkaç kez kodu bilerek bozdum. Bir keresinde ADR'de yazılı bir
  varsayımı hiçbir testin korumadığını böyle buldum.
- **Önce ölç, sonra düzelt.** Türkçe I sorununda, beklentim yanlış olduğu için iki kez ve testlerde iki kez kod değil benim hesabım
  hatalıydı. Her seferinde gerçek davranışı ölçmek, tahminle düzeltmekten daha hızlı oldu.
- **Kriteri önceden yaz.** Analitik sonuçlarını tartışılabilir kılan şey, eşiklerin ölçümden önce yazılmış olması ve ayar ile test
  verisinin ayrı tutulması oldu.
- **Küçük örneklem yanıltır.** 10 profille %17,6 çıkan tahmin hatası 50 profilde %12,4 oldu.
