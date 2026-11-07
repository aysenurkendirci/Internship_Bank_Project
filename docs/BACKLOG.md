# Backlog

## Sprint 0 — Temel ve repo hijyeni

- [x] Kök `.gitignore`, build çıktılarını takipten çıkar — log #001
- [x] JWT secret'ını kaynak koddan çıkar — log #002
- [x] Şemayı versiyonla: SQL Server + DbUp, baseline `V001` — ADR-0002, log #003
- [x] Infrastructure katmanını Oracle'dan SQL Server'a taşı — log #004
- [x] **Şifreler düz metin saklanıyordu**: PBKDF2 ile hash — log #005
- [x] `CancellationToken`'ı repository → Dapper'a kadar ilet — log #004
- [x] `CancellationToken`'ı controller → service → repository zincirinde eksiksiz ilet (AuthService) — log #006
- [x] Hatalar 500 + stack trace dönüyordu: `IExceptionHandler` + ProblemDetails — log #006
- [x] Login'de bilinmeyen TC için 404 dönüyordu (user enumeration), artık 401 — log #006
- [x] Dashboard sunum mantığı istemciye taşındı, uydurma `%32 Faiz` / `0.024` kaldırıldı — log #008
- [x] Interface dosyaları adları ve namespace'leriyle hizalandı — log #009
- [x] DI kayıtları katmanlara dağıtıldı (`AddApplication`, `AddInfrastructure`) — log #009
- [x] JWT süresi konfigürasyonu yok sayılıyordu (6 saat yerine 60 dk) — log #009
- [x] Tarihler JSON'da UTC işaretiyle (`Z`) dönüyor — log #010
- [x] `CardsController`: çift servis çağrısı ve kullanılmayan claim parse kaldırıldı — log #009
- [x] Boş dosyalar, kişisel IDE/AI ayarları ve şablon kalıntıları temizlendi — log #007
- [x] xUnit test projesi, Sprint 0 düzeltmelerini kilitleyen 14 test — log #012
- [x] `System.Security.Cryptography.Pkcs` açığı kapatıldı, build 0 uyarı — log #011

## Sprint 1 — Finansal domain modeli

- [x] `Money` ve `Currency` value object'leri — log #013
- [x] Gelir, düzenli gider, abonelik (`RecurringCashFlow`) — log #016
- [x] Kredi kartı ekstre döngüsü: kesim, son ödeme, hafta sonu kaydırma — log #017
- [x] Taksit planı: ekstre bazlı takvim, kuruşsuz bölme — log #018

**Sprint 1 retrospektifi**
- Teslim: `Money`, `Currency`, `RecurrenceRule`, `RecurringCashFlow`, `CreditCard`, `InstallmentPlan`. Domain katmanı dış bağımlılık içermiyor.
- 81 test. Ay sonu kayması ve user enumeration testleri mutation ile doğrulandı.
- İyi giden: her kural önce testle tanımlandı. Hafta sonu kaydırması, ayda iki taksit çakışmasını ortaya çıkardı.
- Bir sonraki sprintte dikkat: tatil takvimi ve kategori bazlı taksit sınırları bilinçli olarak kapsam dışı. Projeksiyon ekranında kullanıcıya bunun söylenmesi gerekiyor.
- Planlanan büyük harcama (`PlannedExpense`) Sprint 4'teki senaryo karşılaştırmasıyla birlikte modellenecek.
- [x] Tekrarlama kuralı: haftalık/aylık/yıllık, ay sonu kayması olmadan — log #015

## Sprint 2 — Kalıcılık ve CRUD API

- [x] Girdi doğrulaması (400), TCKN checksum, `DomainException` → 400 — log #019
- [x] `V002`: `finance` şeması, audit, soft delete, `ROWVERSION`, bileşik FK ile sahiplik garantisi — log #020
- [x] Kredi kartı CRUD: sahiplik, `If-Match` ile eşzamanlılık, atomik soft delete — log #021
- [x] Gelir / gider / abonelik CRUD, `?kind=` filtresi, kart referansı doğrulaması — log #022
- [x] Taksit planları, her okumada hesaplanan ödeme takvimi — log #023
- [x] Entegrasyon testleri (17): kullanıcı izolasyonu, eşzamanlılık, doğrulama, ilişkiler — log #024

**Sprint 2 retrospektifi**
- Teslim: `finance` şeması; kart, düzenli kalem ve taksit planı API'leri; 17 entegrasyon testi. Toplam 110 test.
- İyi giden: her endpoint önce elle, sonra otomatik testle doğrulandı. Türkçe kültür hatası (`0.01`) ve iç tip adı sızıntısı bu sayede yakalandı.
- Mutation deneyi, veritabanı constraint'lerinin uygulama hatasına karşı gerçekten koruma sağladığını gösterdi.
- Bilinen sınır: kart silme ile karta kayıt ekleme arasında dar bir yarış penceresi var (log #022).
- Sırada: Sprint 3, nakit akışı projeksiyon motoru.

## Sprint 3 — Nakit akışı projeksiyonu

- [x] Projeksiyon motoru: kart ödemeleri son ödeme gününde, en düşük bakiye, yük oranı — ADR-0003, log #025
- [x] Projeksiyon API'si, İstanbul saatine göre iş günü, `TimeProvider` — log #026

**Sprint 3 retrospektifi**
- Teslim: projeksiyon motoru ve API'si. Toplam 130 test (109 unit, 21 entegrasyon).
- Araştırma sorusuna ilk somut cevap: ayı artıda kapatan bir kullanıcının ay içinde eksiye düştüğü gün tespit edilebiliyor.
- Mutation testi, ADR'de yazılı olup testle korunmayan bir varsayımı (aynı gün sıralaması) ortaya çıkardı.
- Sırada: Sprint 4, senaryo karşılaştırma ("3 yerine 6 taksit alırsam ne olur?"). Motor saf bir fonksiyon olduğu için senaryo, girdiye eklenen geçici bir plan olarak çalıştırılabilecek.

## Sprint 4 — Senaryo karşılaştırma

- [x] Taksit seçeneklerini karşılaştırma motoru: ortak ufuk, baz senaryo, öneri yok — log #027
- [x] Senaryo API'si; ortak `ProjectionInputLoader` ile tekrar kaldırıldı — log #028

**Sprint 4 retrospektifi**
- Teslim: senaryo motoru ve API'si. Toplam 148 test (122 unit, 26 entegrasyon).
- Saf projeksiyon motoru (Sprint 3) sayesinde senaryo özelliği yeni kural yazılmadan, sadece girdi değiştirilerek kuruldu. Bu, motorun I/O'suz tasarlanmasının somut getirisi.
- Mutation testi, ortak ufuk kararının kullanıcıyı yanlış seçeneğe yöneltecek bir ölçüm hatasını önlediğini gösterdi.
- Kapsam dışı: peşin nakit ödeme seçeneği (karttan değil hesaptan), kart limiti kontrolü. İkisi de ileride `PurchaseOption`'a eklenebilir.
- Sırada: Sprint 5, CSV içe aktarma ve sentetik veri.

## Sprint 5 — İşlem geçmişi: CSV ve sentetik veri

- [x] `V003`: `finance.Transactions`, `finance.ImportBatches`, parmak izi ile mükerrer koruması — log #029
- [x] CSV parser: encoding, ayraç, Borç/Alacak, Türkçe tutar/tarih, Türkçe İ — log #030
- [x] Import (parmak izi, bulk copy, ya hep ya hiç) ve sayfalı listeleme — log #031
- [x] Deterministik sentetik veri üretici, ground truth anomaliler — log #032

**Sprint 5 retrospektifi**
- Teslim: işlem geçmişi şeması, CSV import, sayfalı listeleme, sentetik veri. Toplam 223 test (186 unit, 37 entegrasyon).
- Türkçe I problemi iki kez yakalandı: önce başlıklarda, sonra parmak izinde. İkincisinde aynı çözüm ortak bir fonksiyona çıkarıldı.
- `"1.2.3"` gibi bozuk değerlerin sessizce yanlış bir tutara dönüşmesi, finans uygulamasındaki en tehlikeli hata türü. Test olmasa fark edilmeyecekti.
- Süreç notu: araç zinciri ` ` gibi kaçış dizilerini görünmez karakterlere çeviriyordu. Kaynak taraması bir kontrol adımı olarak eklendi.
- Sırada: Sprint 6, analitik (kategori trendleri, tekrarlayan ödeme tespiti, anomali tespiti, tahmin).

## Sprint 6 — Analitik

- [x] Analitik temeli (`HistoryEntry`, `YearMonth`, `Statistics`) ve kategori trendleri — log #033
- [x] Tekrarlayan ödeme tespiti; test seti precision/recall 1,00 (sentetik veri, sınırları belgeli) — log #034
- [x] Anomali tespiti (robust z-skoru), test seti 36/36, duyarlılık analizi — log #035
- [x] Tahmin: 3 basit yöntem, backtest ile seçim, kalibre aralık; kâhin referansı ile gürültü tabanı — log #036
- [x] Analitik API'si: trendler, tekrarlayan ödemeler (takip durumu), anomaliler, tahmin — log #037
- [x] Değerlendirme raporu ve yeniden üretme aracı, 50 test profili — log #038, `docs/research/EVALUATION.md`

**Sprint 6 retrospektifi**
- Teslim: kategori trendi, tekrarlayan ödeme tespiti, anomali tespiti, tahmin, analitik API'si, değerlendirme raporu. Toplam 270 test (227 unit, 43 entegrasyon).
- Ayar/test ayrımı ve önceden belirlenen kriterler sayesinde sonuçlar dürüst: karşılanmayan kriter (MAPE ≤ %10) gizlenmedi, nedeni (gürültü tabanı) ölçüldü.
- İki kez test beklentisi yanlış çıktı (medyan 500, eğim 196). İkisinde de kod değil hesap düzeltildi ve hesap teste yorum olarak yazıldı.
- 10 profilden 50 profile geçince tahmin hatası %17,6'dan %12,4'e indi. Küçük örneklemle sonuç raporlamamak gerekiyor.
- Sırada: Sprint 7, Angular ekranları.

## Sprint 7 — Web arayüzü

- [x] Production build onarıldı; ölü kod, kopyalar, sabit URL'ler, token log'ları kaldırıldı — log #039
- [x] Oturum yönetimi, 401 → login (`returnUrl`, open-redirect korumalı), ProblemDetails mesajları — log #040
- [x] Uygulama kabuğu, çıkış, `tr-TR` para/tarih formatı, sahte öğeler kaldırıldı — log #041
- [x] Finans API katmanı: tipli modeller, kaynak başına servis, `If-Match` — log #042
- [x] Kartlar, taksitler ve düzenli kalemler ekranları; 409'da yeniden yükleme — log #043
- [x] Nakit akışı zaman çizelgesi: stat kartları, bakiye grafiği, aylık tablo, ekstre detayı — log #044
- [x] Taksit senaryosu: seçenek başına fiyat, karşılaştırma tablosu, en düşük bakiye grafiği, öneri yok — log #045
- [x] İşlem geçmişi: CSV yükleme (satır hataları), demo verisi, filtre ve sayfalama — log #046
- [x] Ortak UI bileşenleri ve container/presentational yapı; analitik paneli ("Takibe al" köprüsü) — log #047
- [x] Bütün sayfalar container + sunum bileşenlerine ayrıldı — log #048
- [x] Google Stitch için tasarım prompt'u — `docs/design/STITCH_PROMPT.md`

**Sprint 7 retrospektifi**
- `main`'de hiç çalışmayan production build'den, sekiz ekranlı ve bileşen tabanlı bir arayüze geldik. 25 frontend testi.
- En çok zaman alan şey, eski kodun sessiz hataları oldu: hiç kullanılmayan kök bileşen, token basan log'lar, form hatalarını hiç göstermeyen kayıt sayfası.
- Türkçe ekler ("20'si", "2026'dan") küçük ama gerçek bir sorundu. Ek gerektirmeyen ifadeler seçmek, ek kuralları kodlamaktan daha sağlam.
- Eksik kalan: ekranlar bir tarayıcıda gözle kontrol edilmedi. Görsel tasarım Stitch'ten gelince yapılacak.

## Sprint 8 — Güvenlik ve KVKK

- [x] Giriş uçlarında IP başına rate limiting, güvenlik başlıkları, `no-store` — log #049
- [x] Giriş denemelerinin güvenlik kaydı, TC maskeli; log'da tam TC geçmediği testle kilitli — log #050
- [x] Token'da sadece kullanıcı id'si; e-posta çıkarıldı — log #051
- [ ] Refresh token + HttpOnly cookie: tasarım kararı ADR-0004'te, uygulama sonraki adım
- [x] CI: warnings-as-errors, açık taraması, SQL Server container ile entegrasyon testleri, web build ve testleri — log #052
- [x] `/health/live` ve `/health/ready` — log #053
- [x] Temporal tablolarla kart, düzenli kalem ve taksit planlarının tam değişiklik geçmişi — log #054
- [x] KVKK erişim hakkı: kişisel verilerin tamamını JSON olarak dışa aktarma — log #055
- [ ] KVKK silme hakkı: yaklaşım ADR-0005'te önerildi (finans verisi geçmişiyle silinir, bankacılık çekirdeği anonimleştirilir); hukuk görüşü bekliyor

**Sprint 8 retrospektifi**
- Giriş uçları artık kaba kuvvete karşı sınırlı, her deneme iz bırakıyor, log'larda ve token'da kişisel veri yok.
- En değerli testler "olmaması gereken" şeyleri kontrol edenler oldu: log'da tam TC yok, token'da e-posta yok. Bunlar ileride sessizce bozulabilecek şeyler.
- Değişiklik geçmişini uygulama koduna değil veritabanına bırakmak (temporal tablolar), "audit kaydı yazmayı unutma" riskini tamamen ortadan kaldırdı.
- Refresh token bilerek ertelendi. Yarım bir oturum mekanizması (rotation var, reuse detection yok gibi) hiç olmamasından daha riskli. Plan ADR-0004'te.

## Sonraki sprintler

