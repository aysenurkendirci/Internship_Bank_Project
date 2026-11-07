# Mühendislik Günlüğü

Projede karşılaşılan problemler ve nasıl çözüldükleri. Her kayıt aynı yapıyı izler:

- **Sorun** — ne gözlemlendi
- **Analiz** — neden oluyor, etkisi ne
- **Çözüm** — ne yapıldı
- **Neden bu yol** — alternatifler ve tercih gerekçesi
- **Commit** — ilgili commit(ler)

Mimari ölçekteki kararlar ayrıca [`adr/`](adr/) altında Architecture Decision Record olarak tutulur.

---

## #001 — Build çıktıları ve OS dosyaları versiyon kontrolündeydi

**Sprint:** 0

**Sorun.** Her lokal build'den sonra `git status` onlarca değişmiş dosya gösteriyordu.
Gerçek kod değişiklikleri bu gürültünün içinde kayboluyordu.

**Analiz.** Repo kökünde `.gitignore` yoktu (sadece `frontend/` altında vardı). Bu yüzden
backend projelerinin `bin/` ve `obj/` klasörleri (180 dosya: DLL, PDB, NuGet cache) commit'lenmişti.
Bunlar her build'de yeniden üretilen, makineye özgü dosyalar. Ayrıca macOS `.DS_Store` dosyaları
ve bir iCloud yer tutucu dosyası (`.DashboardSummaryResponse.cs.icloud`) da takip ediliyordu.
Sonuçları: kod incelemesi zorlaşıyor, merge conflict riski artıyor, repo boyutu şişiyor.

**Çözüm.** Kök dizine .NET çıktılarını, lokal secret dosyalarını ve OS/bulut senkronizasyon
artıklarını kapsayan bir `.gitignore` eklendi. Zaten takip edilen 185 dosya `git rm --cached`
ile index'ten çıkarıldı. Diskteki dosyalara dokunulmadı.

**Neden bu yol.** Geçmişi `git filter-repo` ile yeniden yazmak da bir seçenekti, ama paylaşılan bir
geçmişi yeniden yazmak gereksiz risk getirir. Kazanç sadece repo boyutu olurdu. İleriye dönük
temizlik yeterli.

**Commit:** `1ecb24d`

---

## #002 — JWT imzalama anahtarı kaynak koddaydı

**Sprint:** 0

**Sorun.** `appsettings.json` içinde `Jwt:Key` düz metin olarak duruyordu.

**Analiz.** HMAC-SHA256 ile imzalanan JWT'lerde anahtarı bilen herkes, istediği kullanıcı adına
geçerli token üretebilir. Yani repoyu okuyabilen herkes sisteme herkes olarak girebilir.
Gerçek bankacılık uygulamalarında secret'lar asla repoya girmez: secret yönetimi bir
denetim (audit) maddesidir.

**Çözüm.** Anahtar `appsettings.json`'dan silindi. Lokal geliştirmede
`dotnet user-secrets` ile sağlanıyor. Canlı benzeri ortamlarda ortam değişkeni veya vault
(Azure Key Vault, HashiCorp Vault) kullanılır. `Program.cs` anahtar yoksa uygulamayı
başlatmadan hata veriyor (fail-fast). Böylece eksik konfigürasyon sessizce gözden kaçmaz.
Kurulum adımları README'ye eklendi.

**Neden bu yol.** Boş string bırakmak yerine anahtarı tamamen sildim. Çünkü `?? throw` kontrolü
`null`'u yakalar ama boş string'i yakalamaz. Eski anahtar git geçmişinde kaldığı için
"ele geçirilmiş" kabul edilir ve tekrar kullanılmaz.

**Commit:** `680c1a5`

---

## #003 — Veritabanı şeması hiçbir yerde versiyonlanmıyordu

**Sprint:** 0

**Sorun.** Uygulama Oracle'daki `PKG_AUTH` ve `PKG_DASHBOARD` paketlerini çağırıyordu ama bu paketlerin
ve tabloların tanımı repoda yoktu. Proje sadece bir makinede çalışıyordu.

**Analiz.** Şemanın kod ile birlikte versiyonlanmaması şu anlama geliyor: eski bir commit'e
dönüldüğünde o commit'in beklediği şema geri gelmiyor, yeni katılan biri projeyi ayağa
kaldıramıyor, şemada neyin neden değiştiği bilinmiyor. Oracle şeması kurtarılamadığı için
veritabanı seçimi de yeniden değerlendirildi (bkz. [ADR-0002](adr/0002-sql-server-instead-of-oracle.md)).

**Çözüm.**
- Veritabanı SQL Server oldu. Şema, DbUp ile çalışan ayrı bir konsol projesine (`Bank.Database`) taşındı.
- `V001__baseline_banking_schema.sql` mevcut kodun beklediği dört tabloyu (Users, Accounts, Cards,
  Transactions) yeniden oluşturuyor. Kolonlar repository kodundaki eşlemelerden ve frontend'in
  beklediği değerlerden (`Inbound`/`Outbound`, `Personal`/`Corporate`) türetildi.
- Scriptler assembly'ye gömülü. Migrator her zaman kendi derlendiği script setini çalıştırıyor.
- Migrator iki kez çalıştırılarak doğrulandı. İkinci çalıştırma hiçbir şey yapmadı (idempotent).

**Neden bu yol.**
- *Migration'ı API açılışında çalıştırmak* daha az adım gerektirirdi. Ancak o durumda API'nin DB kullanıcısı
  DDL yetkisine sahip olmak zorunda kalır, ve birden fazla instance aynı anda başlarsa yarış durumu oluşur.
  Ayrı migrator, bankalardaki "deploy pipeline'ında DB adımı" pratiğine karşılık geliyor.
- Şema tasarımında bankacılık kuralları uygulandı: para için `DECIMAL(19,4)` (kayan nokta yuvarlama
  hatası yok), UTC `DATETIME2`, `CHECK` constraint'leri ile durum alanlarının korunması, kart
  numarasının sadece son 4 hanesinin saklanması (PCI DSS), işlem tutarının her zaman pozitif olup
  yönün ayrı kolonda tutulması.

**Commit:** `a1945d3` (karar), `build(db): add DbUp migrator with baseline SQL Server schema` (uygulama)

---

## #004 — Veri erişim katmanı: Oracle'dan SQL Server'a geçiş

**Sprint:** 0

**Sorun.** Infrastructure katmanı Oracle'a özgü yapılara bağlıydı (`OracleDynamicParameters`, RefCursor
çıkış parametreleri, `PKG_*` prosedür çağrıları). Bunların yanında üç sorun daha vardı:
- Dashboard tek bir istek için **5 ayrı bağlantı açıp 5 ayrı sorgu** çalıştırıyordu.
- `CancellationToken` Dapper'a iletilmiyordu. Kullanıcı sayfadan çıksa bile sorgular çalışmaya devam ediyordu.
- `TotalWealth` farklı para birimlerindeki bakiyeleri (TL + USD) doğrudan topluyordu.

**Analiz.** Her bağlantı açılışı ve her sorgu turu (round trip) gecikme ekler. 5 sorgu, ağ gecikmesini
5 kez öder. İptal edilmeyen sorgular yük altında bağlantı havuzunu gereksiz yere meşgul eder.
Para birimi karışımı ise bir doğruluk hatası: 15.000 TL + 900 USD için "15.900" göstermek yanlış bir bilgi.

**Çözüm.**
- `OracleExecutor` sarmalayıcısı kaldırıldı. Repository'ler `SqlConnectionFactory`'den bağlantı alıp
  Dapper'ı doğrudan kullanıyor. Ek bir soyutlama katmanı değer katmıyordu (KISS).
- Dashboard'un beş sorgusu tek bir batch'te `QueryMultipleAsync` ile çalışıyor: tek bağlantı, tek round trip.
- Tüm sorgular `CommandDefinition` ile `CancellationToken` taşıyor.
- `TotalWealth` sadece aktif TRY hesaplarını topluyor. Döviz çevrimi kur verisi gerektirdiği için ayrı bir iş.
- Kayıt işlemi `INSERT ... OUTPUT INSERTED.*` ile yeni satırı aynı sorguda döndürüyor (önceden ayrıca SELECT yapılıyordu).
- Sorgu sonuçları sadece repository içinde kullanılan private row sınıflarına eşleniyor.

**Doğrulama.** API gerçek SQL Server'a karşı çalıştırıldı: kayıt, giriş ve dashboard doğru döndü,
token'sız istek 401 aldı.

**Smoke test sırasında bulunanlar (backlog'a eklendi):**
- Mükerrer kayıt **500 ve tam stack trace** dönüyor. `ErrorHandlingMiddleware` pipeline'a hiç eklenmemiş.
- Veritabanında şifre düz metin duruyor (`PasswordHash = "P@ssw0rd!"`).
- `createdAt` JSON'da `Z` (UTC) eki olmadan dönüyor.

**Commit:** `refactor(infrastructure): move data access from Oracle to SQL Server`

---

## #005 — Şifreler veritabanında düz metin saklanıyordu

**Sprint:** 0 · **Önem:** Kritik

**Sorun.** `PasswordHasher.Hash(p)` girdiyi olduğu gibi döndürüyordu, `Verify` ise düz `==` karşılaştırması
yapıyordu. Smoke test sırasında `Users.PasswordHash` kolonunda `P@ssw0rd!` değeri görüldü.

**Analiz.** Veritabanına okuma erişimi olan herkes (DBA, yedek dosyası, SQL injection açığı) tüm
kullanıcıların şifresini doğrudan görebilir. Kullanıcılar şifrelerini başka sitelerde de kullandığı için
zarar bu uygulamayla sınırlı kalmaz. Ayrıca `==` karşılaştırması zamanlama saldırısına açık.

**Çözüm.** `PasswordHasher` artık ASP.NET Core Identity'nin `PasswordHasher<T>` sınıfına delege ediyor:
- PBKDF2 (HMAC-SHA512, yüksek iterasyon) ve her şifre için rastgele salt kullanıyor.
- Karşılaştırmayı sabit zamanlı yapıyor.
- Çıktı formatı versiyonlu. İleride iterasyon sayısı artırılırsa eski hash'ler geçerliliğini koruyor.

`IPasswordHasher` arayüzü değişmedi. Application katmanı hiçbir şey fark etmedi. Dependency Inversion
sayesinde değişiklik tek bir sınıfla sınırlı kaldı.

**Neden bu yol.**
- Kendi PBKDF2 kodumu yazmak da mümkündü (`Rfc2898DeriveBytes.Pbkdf2` +
  `CryptographicOperations.FixedTimeEquals`). Ancak salt boyutu, format ve versiyonlama gibi detayları
  doğru yapmak hataya açık. Framework'ün test edilmiş uygulamasını kullanmak daha güvenli
  ("don't roll your own crypto").
- BCrypt/Argon2 harici paket gerektiriyor. PBKDF2 ise NIST onaylı ve .NET'in içinde geliyor.
- Lokal veritabanındaki test kullanıcısı silindi. Düz metin satırlar yeni formatta doğrulanamaz. Canlı
  bir sistemde bu, "bir sonraki girişte yeniden hash'le" şeklinde kademeli bir geçiş gerektirirdi.

**Doğrulama.** Kayıt sonrası DB'de `AQAAAAIAAYag...` biçiminde 84 karakterlik hash görüldü. Doğru şifre ile
giriş 200 döndü. Yanlış şifre şu an 500 dönüyor: servis doğru exception'ı fırlatıyor, ama
`ErrorHandlingMiddleware` bağlı olmadığı için 401'e çevrilmiyor (sıradaki iş).

**Commit:** `fix(security): hash passwords with PBKDF2 instead of storing plain text`

---

## #006 — Hata yönetimi: stack trace sızıntısı ve kullanıcı numaralandırma

**Sprint:** 0

**Sorun.**
1. Herhangi bir hata, istemciye **500 ve tam stack trace** olarak dönüyordu (dosya yolları, sınıf adları, istek başlıkları dahil).
2. Login'de bilinmeyen TC için 404, yanlış şifre için 401 dönüyordu.
3. Mükerrer kayıt 409 yerine 500 dönüyordu.

**Analiz.**
- Bir `ErrorHandlingMiddleware` yazılmıştı ama `Program.cs`'te hiç pipeline'a eklenmemişti, yani ölü koddu.
- Eklenmiş olsaydı bile iki tasarım sorunu vardı:
  - `InvalidOperationException` → 409 eşlemesi yapıyordu. Bu exception'ı .NET'in kendisi de her türlü
    programlama hatasında fırlatır. Bir bug "çakışma" gibi görünür ve gizlenirdi.
  - 500 durumunda `ex.Message` istemciye gönderiliyordu. SQL hata mesajları tablo ve kolon adlarını sızdırır.
- 404 / 401 ayrımı, saldırganın hangi TC kimlik numaralarının sistemde kayıtlı olduğunu tek tek
  denemesine izin veriyor (user enumeration). Bankacılıkta bu bir kişisel veri sızıntısı (KVKK).

**Çözüm.**
- Application katmanına kendi exception hiyerarşisi eklendi: `AppException` ve ondan türeyen
  `NotFoundException`, `ConflictException`, `AuthenticationFailedException`. İstemciye sadece bu tiplerin
  mesajı gösteriliyor. Diğer her şey genel bir 500 olarak dönüyor ve loglanıyor.
- Elle yazılmış middleware silindi. Yerine .NET'in standart mekanizması `IExceptionHandler` +
  `AddProblemDetails()` kullanıldı. Yanıtlar RFC 9457 formatında (`application/problem+json`) ve bir
  `traceId` içeriyor. Destek ekibi bu id ile log'daki ilgili isteği bulabiliyor.
- Login'de bilinmeyen TC ve yanlış şifre aynı `AuthenticationFailedException`'ı fırlatıyor: aynı durum kodu,
  aynı mesaj.
- Eşzamanlı iki kayıt isteği "var mı?" kontrolünü birlikte geçebilir (TOCTOU yarışı). Bu durumda
  gerçek garanti veritabanındaki UNIQUE constraint. Repository, SQL Server'ın 2627/2601 hatalarını
  `ConflictException`'a çeviriyor.
- `CancellationToken` artık controller'dan repository'ye kadar her katmanda taşınıyor.
- Kullanılmayan `GetUserByTcAsync` kaldırıldı (YAGNI). `AuthController` namespace'siz duruyordu, düzeltildi.

**Neden bu yol.** Exception'ları `Result<T>` gibi bir tiple döndürmek de bir seçenekti. Ancak mevcut kod tabanı
exception tabanlı ve az sayıda beklenen hata türü var. Küçük ve net bir exception hiyerarşisi daha az kod
ve daha az kavram demek (KISS).

**Doğrulama.** Gerçek API'ye karşı: mükerrer kayıt → 409, yanlış şifre → 401, bilinmeyen TC → 401 (aynı mesaj),
DB constraint hatası → içinde SQL detayı olmayan genel 500. Son senaryo, girdi validasyonunun eksik
olduğunu da gösterdi. Geçersiz istek 400 almalı, bu iş Sprint 2'ye eklendi.

**Commit:** `fix(api): return problem details and stop leaking internals on errors`

---

## #007 — Repoda kişisel ayarlar, boş dosyalar ve şablon kalıntıları

**Sprint:** 0

**Sorun.** Takip edilen dosyalar tek tek incelendiğinde şunlar bulundu:
- Kök `.vscode/` klasöründe başka bir makineden kalma C/C++ debug ayarları vardı. İçinde o makinenin
  kullanıcı adını içeren mutlak dosya yolları geçiyordu.
- Kişisel AI asistanı yapılandırmaları (`copilot-instructions.md`, `mcp.json`) commit'lenmişti.
- 12 adet **tamamen boş** `.cs` dosyası vardı (`Money`, `User`, `Card`, `ApiResponse`…). Ayrıca Domain
  klasöründe namespace'i `Bank.Contracts` olan bir dosya duruyordu.
- `Bank.Api.http` şablondan kalmıştı ve var olmayan `/weatherforecast` adresini çağırıyordu.
- Tailwind lint ayarı (`settings.json`) kaynak kod klasörü `src/` içindeydi.

**Analiz.** Boş dosyalar kodu okuyan birine "domain modeli var" izlenimi veriyor ama aslında hiçbir şey
içermiyor. Kişisel IDE ve araç ayarları her geliştiricide farklıdır. Commit'lenirlerse çakışma
çıkarır ve kişisel bilgileri (dosya yolları, kullanıcı adları) sızdırırlar.

**Çözüm.**
- Boş ve şablon dosyalar silindi. Domain modeli Sprint 1'de testleriyle birlikte gerçekten yazılacak.
- Kişisel ayarlar takipten çıkarıldı (diskte duruyorlar) ve `.gitignore`'a eklendi.
- Ekipçe paylaşılması anlamlı olan ayarlar (`frontend/.vscode/` içindeki extensions, launch, tasks ve
  lint ayarı) repoda kaldı.

**Not.** İlk denemede `.github/copilot-instructions.md` deseni `frontend/` altındaki dosyayı yakalamadı.
Ortasında `/` olan gitignore desenleri kök dizine sabitlenir. `**/` önekiyle düzeltildi ve
`git check-ignore -v` ile doğrulandı.

**Commit:** `chore: remove personal tooling config, empty files and template leftovers`

---

## #008 — Dashboard API'si sunum kararları ve uydurma değerler döndürüyordu

**Sprint:** 0

**Sorun.**
- `DashboardRepository` hesap tipinden Türkçe etiket ("Vadesiz TL"), ikon adı (`bank`, `trend`, `wallet`)
  ve açıklama metni üretiyordu.
- Her vadeli hesap için "%32 Faiz" yazıyordu. Bu değer hiçbir veriye dayanmıyordu.
- `WealthChangeRate` her kullanıcı için sabit `0.024` dönüyordu. Ekranda "+2.4%" olarak gösteriliyordu.
- Frontend tüm bakiyelerin önüne `₺` koyuyordu, yani 900 USD "₺900" olarak görünüyordu.
- Frontend statü rengini `'Aktif'` değerine göre seçiyordu ama API bu değeri hiç döndürmüyordu.

**Analiz.** Repository'nin görevi veriyi getirmek. Ekranda nasıl görüneceğine karar vermek onun
sorumluluğu değil (Single Responsibility). İkon adının API'de olması, web'e özgü bir kararın
kontrata sızması demek: iOS uygulaması SF Symbols kullanacak ve `trend` gibi bir değer onun için anlamsız.
Asıl ciddi sorun ise uydurma değerler. Bir finans uygulamasında gerçek olmayan bir faiz oranı ya da
varlık değişimi göstermek kullanıcıyı yanıltır. Proje tanımı da bunu açıkça yasaklıyor: sistem sadece
kullanıcının kendi verisini görselleştirecek.

**Çözüm.**
- `AccountItem` artık ham veri taşıyor: `AccountType`, `CurrencyCode`, `Iban`, `Balance`, `Status`.
- `WealthChangeRate` kontrattan çıkarıldı. Gerçek değişim oranı bakiye geçmişi gerektiriyor. Bu,
  Sprint 6'daki trend analizinde gerçek veriyle hesaplanacak.
- Etiket ve ikon eşlemesi frontend'de tek bir dosyada (`account-display.ts`) toplandı. Eşleme
  `Record<AccountType, …>` tipiyle yazıldı. Yeni bir hesap tipi eklenip eşlemesi unutulursa kod derlenmiyor.
- Bakiyeler hesabın kendi para birimiyle gösteriliyor (`currency` pipe + `currencyCode`).

**Neden bu yol.** Etiketleri API'de tutup çoklu dil desteği eklemek de mümkündü. Ancak etiket, ikon ve renk
platforma özgü kararlar. API'nin kontratı ne kadar ham ve kararlı olursa web ve iOS istemcileri o kadar
bağımsız gelişebilir.

**Ek bulgu.** Doğrulama sırasında frontend production build'inin **bu değişiklikten önce de** başarısız
olduğu görüldü: `app.scss` boyut bütçesini aşıyor. Değişiklikler geri alınarak aynı hata tekrar üretildi.
Web sprint'inin ilk işi olarak backlog'a eklendi.

**Commit:** `refactor(dashboard): return raw account data and drop fabricated figures`

---

## #009 — Yapısal düzeltmeler: dosya düzeni, DI kompozisyonu, yok sayılan ayar

**Sprint:** 0

Tek tek küçük ama birlikte kod tabanının okunabilirliğini ve doğruluğunu etkileyen sorunlar.

| Sorun | Etki | Çözüm |
|---|---|---|
| `IDashboardRepository.cs` içinde `IDashboardService` vardı, `IDashboardService.cs` içinde de tersi. `IAuthService` ise namespace'iyle uyuşmayan bir klasördeydi. | Bir tipi dosya adından bulmak imkânsızdı. | Her interface namespace'iyle aynı klasöre, kendi adını taşıyan dosyaya taşındı. Geçmiş korunsun diye `git mv` kullanıldı. |
| `CardsController` her istekte dashboard'u **iki kez** yüklüyor, parse ettiği `userId`'yi kullanmıyordu. | Her kart isteği iki kat DB yükü demekti. | Tek çağrıya indirildi. Kullanıcı zaten `ICurrentUser` üzerinden çözülüyor. |
| Tüm DI kayıtları `Program.cs`'teydi. | API projesi her katmanın somut sınıflarını bilmek zorundaydı (Dependency Inversion ihlali). | `AddApplication()` ve `AddInfrastructure()` extension metotları eklendi. `Program.cs` artık sadece katmanları bir araya getiriyor (composition root). |
| `Jwt:ExpiresMinutes = 60` tanımlıydı ama kod token'ı sabit **6 saat** geçerli üretiyordu. | Konfigürasyon yok sayılıyordu. Çalınan bir token 6 kat daha uzun süre kullanılabilirdi. | Ayar okunuyor, eksikse uygulama açılmıyor. Üretilen token decode edilerek `exp - nbf = 60 dk` doğrulandı. |

**Öğrenilen.** "Ayar var" ile "ayar kullanılıyor" aynı şey değil. Güvenlik açısından kritik
konfigürasyonlar (token süresi, anahtar) uygulamanın gerçek çıktısı üzerinden doğrulanmalı.
Bu yüzden token'ı decode edip sürenin gerçekten 60 dakika olduğunu kontrol ettim.

**Commit'ler:**
`refactor(application): align interface files with their names and namespaces`,
`fix(api): stop loading the dashboard twice in CardsController`,
`refactor(api): let each layer register its own services`,
`fix(auth): honour the configured JWT lifetime`

---

## #010 — Zaman damgaları saat dilimi bilgisi olmadan dönüyordu

**Sprint:** 0

**Sorun.** API, `createdAt` değerini `"2026-11-07T12:32:47.839"` biçiminde, sonunda `Z` olmadan döndürüyordu.

**Analiz.** Veritabanı zamanları UTC olarak tutuyor (`SYSUTCDATETIME()`). Ancak `DATETIME2` tipi saat dilimi
bilgisi taşımıyor, bu yüzden Dapper değeri `DateTimeKind.Unspecified` olarak okuyor. ISO 8601'de ofseti
olmayan bir zaman **yerel saat** kabul edilir. Tarayıcı bu değeri Türkiye saati sanıyor ve her işlem
gerçekte olduğundan 3 saat önce yapılmış gibi görünüyor. Banka hareketlerinde yanlış saat, kullanıcının
bir işlemi tanımamasına ve gereksiz itirazlara yol açar.

**Çözüm.** API'nin JSON ayarlarına `UtcDateTimeConverter` eklendi. Bu converter tüm `DateTime` değerlerini
UTC olarak işaretleyip `Z` ekiyle yazıyor. Gelen değerleri de UTC'ye çeviriyor.

**Neden bu yol.**
- *Her mapping'de `DateTime.SpecifyKind` çağırmak*: ilk unutulan sorguda hata geri gelir.
- *Dapper type handler*: Dapper, `DateTime` gibi temel tiplerde okuma sırasında kendi dönüşümünü
  kullanıyor. Özel handler okuma tarafında güvenilir şekilde devreye girmiyor.
- *Kontratları `DateTimeOffset`'e çevirmek*: doğru ama her DTO'yu ve frontend modelini etkiliyor.
  Sistemde tek bir saat dilimi (UTC) olduğu için ofset taşımak ek bilgi sağlamıyor.

Kural ("sistemdeki her zaman UTC'dir") tek bir yerde, API sınırında uygulanıyor. Yeni eklenen her
endpoint bu kuralı otomatik olarak alıyor.

**Doğrulama.** `/api/transactions/recent` yanıtında `"createdAt":"2026-11-07T12:50:22.459Z"` görüldü.

**Commit:** `fix(api): serialise timestamps as UTC`

---

## #011 — Transitive bağımlılıkta yüksek önem dereceli güvenlik açığı

**Sprint:** 0

**Sorun.** Build her seferinde NU1903 (yüksek) ve NU1902 (orta) uyarıları veriyordu:
`System.Security.Cryptography.Pkcs` 9.0.x, GHSA-846f-f764-76mx ve GHSA-2fmv-rv2c-59x7.

**Analiz.** `dotnet nuget why` ile zincir çıkarıldı: paketi doğrudan kullanmıyoruz, `Microsoft.Data.SqlClient`
getiriyor (API tarafında 7.1.1, migrator tarafında DbUp üzerinden 6.1.4). SqlClient'ın daha yeni bir
sürümü olmadığı için üst paketi yükseltmek bir seçenek değildi.

**Çözüm.** Infrastructure ve Database projelerine yamalı sürüm (10.0.14) doğrudan referans olarak eklendi.
NuGet'in "en yakın kazanır" kuralı gereği transitive sürüm bununla eziliyor. Referansın yanına neden orada
olduğunu ve ne zaman kaldırılabileceğini açıklayan bir yorum yazıldı. Yorum olmazsa biri onu
"kullanılmayan paket" sanıp silebilir.

**Doğrulama.** `dotnet list package --vulnerable --include-transitive` artık tüm projeler için temiz.
Build 0 uyarı veriyor. Migrator ve login uçtan uca çalıştırıldı.

**Neden önemli.** Uyarılar sürekli göründüğünde ekip onları görmezden gelmeye başlar ve gerçekten yeni
bir uyarı çıktığında fark edilmez. Sıfır uyarılı bir build bu yüzden tutulmaya değer bir standart.

**Commit:** `build(deps): override vulnerable transitive Pkcs package`

---

## #012 — Projede hiç test yoktu

**Sprint:** 0

**Sorun.** Sprint 0'da düzeltilen hataların (düz metin şifre, user enumeration, UTC'siz tarih) geri
gelmesini engelleyen hiçbir şey yoktu. Bir refactor sırasında herhangi biri sessizce geri dönebilirdi.

**Çözüm.** `backend/tests/Bank.UnitTests` (xUnit + NSubstitute) eklendi. Klasör yapısı katmanları izliyor
(`Application/`, `Infrastructure/`, `Api/`). İlk testler, bu sprintte düzeltilen davranışları kilitliyor:

| Test sınıfı | Neyi koruyor |
|---|---|
| `AuthServiceTests` | Bilinmeyen TC ve yanlış şifre **aynı** hatayı veriyor. Başarısız girişte "son giriş" güncellenmiyor. Mükerrer kayıtta kullanıcı oluşturulmuyor. Repository'ye şifrenin hash'i gidiyor. |
| `PasswordHasherTests` | Hash düz metni içermiyor. Aynı şifre her seferinde farklı hash üretiyor (salt). Yanlış şifreler reddediliyor. |
| `UtcDateTimeConverterTests` | DB'den gelen `Unspecified` tarih `Z` ile yazılıyor. Ofsetli girdi UTC'ye çevriliyor. |

**Testlerin gerçekten koruduğunun doğrulanması (mutation testing).** Hiç başarısız olmamış bir test,
bir şeyi koruduğunu kanıtlamaz. Bu yüzden eski hatalar kodda geçici olarak geri getirildi:
- `PasswordHasher` tekrar düz metin döndürecek hale getirildi → 2 test başarısız oldu.
- Login'de bilinmeyen kullanıcı için tekrar `NotFoundException` fırlatıldı → 1 test başarısız oldu.

Kod geri yüklendi, 14 testin hepsi geçti.

**Neden bu yol.**
- Mock kütüphanesi olarak NSubstitute seçildi. Söz dizimi kısa ve okunabilir. FluentAssertions 8 sürümünden
  itibaren ticari lisansa geçtiği için yerleşik `Assert` kullanıldı.
- Test isimleri `Metot_Durum_BeklenenSonuç` kalıbında. Testin neyi koruduğu adından okunabiliyor.
- Veritabanına gerçekten bağlanan entegrasyon testleri, repository sayısı arttığında (Sprint 2) ayrı bir
  projede gelecek.

**Commit:** `test: add unit test project covering the sprint 0 fixes`

---

## #013 — Para, para birimiyle birlikte taşınmalı

**Sprint:** 1

**Sorun.** Kod tabanında tutarlar çıplak `decimal` olarak dolaşıyordu. Dashboard'da TL ve USD bakiyelerinin
toplanması (bkz. #004) bu tasarımın doğal sonucuydu: tip sistemi iki farklı para birimini toplamayı
engellemiyordu.

**Çözüm.** Domain katmanına iki value object eklendi:
- `Currency`: desteklenen ISO 4217 kodları (TRY, USD, EUR). Bilinmeyen bir kod `DomainException` fırlatıyor.
- `Money`: tutar ve para birimi bir arada. Farklı para birimleri arasında toplama, çıkarma ve
  karşılaştırma `DomainException` fırlatıyor. `record` olduğu için değer eşitliği var ve değiştirilemez (immutable).

**Tasarım kararları.**
- **`decimal`, asla `double`.** `0.1 + 0.2` ikili kayan noktada `0.30000000000000004` olur. Bir testle
  `decimal`'in bunu tam hesapladığı kilitlendi.
- **Yuvarlama açıkça çağrılıyor.** Hesaplama sırasında tam hassasiyet korunuyor, kuruşa yuvarlama sadece
  `Round()` ile yapılıyor. Her işlemde otomatik yuvarlama, ara adımlarda hata biriktirir.
- **Yuvarlama kuralı: yarım değerler sıfırdan uzağa (2.345 → 2.35).** .NET'in varsayılanı "banker's rounding"
  (2.345 → 2.34). Ekstre ve dekontta kullanıcının beklediği ise okulda öğretilen yuvarlama. Testlerde iki
  yöntemin farklı sonuç verdiği vakalar özellikle seçildi. İlk yazılan `2.335` vakası iki yöntemde de
  aynı sonucu verdiği için hiçbir şeyi ayırt etmiyordu, `0.125` ile değiştirildi.
- **Sadece iki kuruş haneli para birimleri.** JPY gibi alt birimi olmayan para birimleri "kuruşa yuvarla"
  kuralını bozar. Desteklenen liste bu yüzden bilinçli olarak dar tutuldu.
- **`Money` bir `class` (`record`), `struct` değil.** `default(Money)` bir struct için para birimi `null`
  olan geçersiz bir değer üretirdi. Sınıfta ise her nesne kurucudan geçmek zorunda.

**Commit:** `feat(domain): add Money and Currency value objects`

---

## #014 — Taksitlere bölerken kuruş kaybı

**Sprint:** 1

**Sorun.** Bir tutarı taksitlere bölmenin naif yolu kuruş kaybettirir: 1.000 TL / 3 = 333,33 TL,
3 × 333,33 = **999,99 TL**. Kaybolan 1 kuruş, ödeme takvimi toplamının satın alma tutarını
tutmamasına yol açar. Muhasebede bu bir mutabakat (reconciliation) hatasıdır.

**Çözüm.** `Money.Allocate(parts)`:
1. Toplam önce kuruşa yuvarlanıyor.
2. Pay, kuruşa **aşağı** (sıfıra doğru) yuvarlanıyor: 333,33.
3. Kalan (`toplam - pay × parça`) ilk paya ekleniyor: **333,34 + 333,33 + 333,33 = 1.000,00**.

**Neden ilk taksit?** Türkiye'de bankaların kart taksitlerinde yaygın uygulaması, yuvarlama farkının ilk
taksite eklenmesidir. Farkı son taksite eklemek de geçerli bir alternatif. Önemli olan kuralın tek bir
yerde tanımlı ve tutarlı olması.

**Test stratejisi.** Tek bir örnek yerine bir **değişmez (invariant)** test edildi: hangi tutar ve parça sayısı
verilirse verilsin, payların toplamı yuvarlanmış toplama eşit olmalı ve her pay tam kuruş olmalı. Uç
durumlar: kuruşun parça sayısından az olduğu durum (0,05 TL / 3 → 0,03 + 0,01 + 0,01) ve iade gibi negatif tutarlar.

**Commit:** `feat(domain): split money into cent-exact shares`

---

## #015 — Tekrarlayan ödemeler: ay sonu kayması

**Sprint:** 1

**Sorun.** Maaş, kira, abonelik gibi düzenli ödemeler "her ayın X'i" şeklinde tanımlanır. Ama her ayın
31'i yoktur. Asıl tuzak şu: bir sonraki tarihi bir **önceki tarihten** hesaplayan naif bir uygulama
`31 Oca → 28 Şub → 28 Mar → 28 Nis …` üretir. Şubat'tan sonra ödeme yılın geri kalanında ayın 28'ine
kayar ve nakit akışı takvimi gerçeği yansıtmaz.

**Çözüm.** `RecurrenceRule` (sıklık, aralık, başlangıç, opsiyonel bitiş) her tekrarı **başlangıç tarihinden**
hesaplıyor: n. tekrar = `StartDate.AddMonths(n × Interval)`. .NET'in `AddMonths` metodu geçersiz günü
ayın son gününe sabitliyor, böylece sonuç `31 Oca → 28 Şub → 31 Mar → 30 Nis` oluyor.
Aynı yaklaşım 29 Şubat'ta başlayan yıllık tekrarları da doğru ele alıyor: artık yıl olmayan yıllarda
28 Şubat'a düşüyor, artık yıllarda tekrar 29'a dönüyor.

**Doğrulama.** Kod geçici olarak "önceki tarihten hesapla" şekline çevrildi. Test tam olarak beklenen
hatayı gösterdi (`Beklenen 31.03.2026, Gerçek 28.03.2026, 28.04.2026, 28.05.2026`). Kod geri yüklendi.

**Kapsam kararları (YAGNI).**
- Sıklıklar: haftalık, aylık, yıllık ve "her N'de bir" aralığı. "Ayın son iş günü" gibi kurallar şu an
  gereksinimlerde yok.
- Tatil ve hafta sonu kaydırması tekrarlama kuralının değil, ödemeyi yapan kurumun kuralı. Bu yüzden
  kredi kartı son ödeme gününde ayrıca ele alınacak.

**Commit:** `feat(domain): add recurrence rule for repeating payments`

---

## #016 — Gelir, gider ve abonelik için tek model

**Sprint:** 1

**İhtiyaç.** Kullanıcı maaşını, kirasını, faturalarını ve aboneliklerini sisteme girebilmeli. Bunların hepsi
"belirli bir tutar, belirli bir tekrarla" ödeniyor.

**Tasarım.** Gelir, gider ve abonelik için üç ayrı sınıf yazmak yerine tek bir `RecurringCashFlow` sınıfı ve
`CashFlowKind` (Income / Expense / Subscription) kullanıldı. Davranışları aynı olduğu için üç sınıf sadece kod
tekrarı üretirdi (KISS, DRY). Abonelik ayrı bir tür olarak tutuldu, çünkü kullanıcıya "aboneliklerin toplamı"
ayrıca gösterilecek ve iptal edilebilir harcamalar olarak gruplanacaklar.

**Kurallar (invariant'lar).** Nesne geçersiz bir durumda oluşturulamıyor:
- Tutar her zaman pozitif. Yön `Kind` alanından geliyor. Bu, veritabanındaki `Transactions` tablosuyla
  aynı kural. Negatif tutara izin verilseydi "negatif gider" yönü ikinci kez tersine çevirirdi.
- Gelir bir kredi kartına bağlanamaz.
- Ad ve kategori boş olamaz, kenar boşlukları temizleniyor.

**Kredi kartına bağlı kalemler.** Kartla ödenen bir abonelik, paranın hesaptan çıktığı gün değil, kartın son
ödeme günü nakit akışını etkiler. `CreditCardId` bu bilgiyi taşıyor. Sprint 3'teki projeksiyon motoru,
kartlı kalemleri ilgili ekstreye aktaracak.

**Çıktı.** `OccurrencesBetween` her tekrar için `CashFlowOccurrence` üretiyor. Tutar burada kullanıcının
bakış açısından işaretli: gelir pozitif, çıkışlar negatif. Projeksiyon motoru bu değerleri doğrudan toplayabilecek.

**Commit:** `feat(domain): model recurring income, expenses and subscriptions`

---

## #017 — Kredi kartı harcaması paranın çıktığı gün değildir

**Sprint:** 1

**Sorun.** Çoğu bütçe uygulaması kartla yapılan harcamayı harcama gününde "para çıktı" olarak gösterir.
Gerçekte para, **ekstrenin son ödeme gününde** hesaptan çıkar. 25 Mart'ta yapılan bir harcama, kesim günü
20 olan bir kartta Nisan ekstresine girer ve Mayıs başında ödenir. Projenin temel sorusu ("gelecek aylarda
nakit akışım nasıl olacak?") bu ayrım olmadan yanlış cevaplanır.

**Çözüm.** `CreditCard` ekstre döngüsünü modelliyor:
- `StatementDateFor(harcamaTarihi)`: kesim günü dahil öncesindeki harcamalar bu ayın ekstresine, sonrakiler
  gelecek ayın ekstresine. Aralık → Ocak yıl geçişi dahil.
- `NextStatementDate`: kesim günü 31 olan kart Şubat'ta 28'inde, Mart'ta tekrar 31'inde kesiyor. Tarih her
  seferinde `StatementDay`'den hesaplandığı için kısa bir ay sonraki kesimleri kaydırmıyor (#015'teki
  tuzağın aynısı).
- `DueDateFor(kesimTarihi)`: kesim + `PaymentDueDays`. Cumartesi veya Pazar'a denk gelirse Pazartesi'ye kayıyor.

**Kapsam kararları.**
- **Resmi tatiller modellenmedi.** Tatil takvimi (dini bayramlar her yıl değişir) ayrı bir veri kaynağı
  gerektiriyor. Sınıf belgesinde bu sınır açıkça yazılı. Projeksiyon bir karar destek aracı olduğu için
  tatil kaynaklı birkaç günlük kaymanın aylık görünüme etkisi sınırlı.
- **Son ödeme süresi kart bazında parametre** (1–30 gün). Bankalar arasında farklılık gösterdiği için sabit
  kodlanmadı.
- **Kesim günü yapılan harcama o ekstreye dahil.** Bu kural bankadan bankaya ve işlem saatine göre değişebilir.
  Tek ve açık bir kural seçilip testle kilitlendi.

**Test notu.** Hafta sonu testleri için tarihlerin hangi güne denk geldiği önceden kontrol edildi
(15 Nis 2026 + 10 = Cumartesi, 16 Nis + 10 = Pazar, ikisi de 27 Nis Pazartesi'ye kaymalı).

**Commit:** `feat(domain): model credit card statement and due dates`

---

## #018 — Taksit planı: parçaları birleştirmek

**Sprint:** 1

**İhtiyaç.** "Bu bilgisayarı 3 yerine 6 taksitle alırsam ne olur?" sorusunun cevabı, her taksidin **hangi
ekstreye** ve **hangi son ödeme gününe** düştüğünü bilmeyi gerektiriyor.

**Çözüm.** `InstallmentPlan.ScheduleOn(card)` önceki üç parçayı birleştiriyor:
- Tutarlar `Money.Allocate` ile bölünüyor (#014). Taksitlerin toplamı her zaman satın alma tutarına eşit.
- İlk taksit, harcamanın düştüğü ekstreye (#017), her sonraki taksit bir sonraki ekstreye yazılıyor.
- Son ödeme günleri kartın kuralıyla hesaplanıyor (hafta sonu kaydırması dahil).

Tutarlılık kontrolleri: plan başka bir karta uygulanamıyor, kartın para biriminden farklı bir tutar reddediliyor.

**Testlerden çıkan gerçek hayat bulgusu.** 25 Mart 2026'da 3 taksitle yapılan 1.000 TL'lik bir alışverişte
Mayıs taksidinin son ödeme günü (30 Mayıs) Cumartesi'ye denk geliyor ve 1 Haziran'a kayıyor. Haziran
taksidi de 30 Haziran'da. Sonuç: **Haziran'da iki taksit ödemesi, Mayıs'ta hiç yok.** Ay bazında bütçe yapan bir
kullanıcı bunu fark etmez. Bu, projenin araştırma sorusunun ("gelecekteki yük daha erken görünür
hale getirilebilir mi?") küçük ama somut bir örneği. Bu durum ayrı bir testle kilitlendi.

**Kapsam kararları.**
- Üst sınır olarak 36 taksit **sadece bir akıl sağlığı kontrolü**. Gerçekte düzenleyici kurum, taksit sayısını
  harcama kategorisine göre sınırlıyor. Bu kategori bazlı sınırlar modellenmedi ve sınıf belgesinde bunu
  açıkça belirttim.
- Vade farkı veya faiz modellenmedi. Taksitli alışverişlerde tutar satıcıyla belirlenir. Kredi kartı faizi
  borç devrettiğinde (asgari ödeme) devreye girer ve bu ayrı bir senaryo.

**Commit:** `feat(domain): schedule card installments on statement dates`
