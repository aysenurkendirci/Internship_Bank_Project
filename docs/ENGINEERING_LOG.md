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

**Neden ilk taksit?** Farkın hangi taksite ekleneceği bankadan bankaya değişebilir. İlk veya son taksit
ikisi de geçerli seçenekler. Bu projede ilk taksit seçildi: kullanıcı farkı en baştan görür ve sonraki
taksitler eşit kalır. Önemli olan kuralın tek bir yerde tanımlı, belgelenmiş ve testle kilitlenmiş olması.

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

---

## #019 — Girdi doğrulaması: geçersiz istekler 500 alıyordu

**Sprint:** 2

**Sorun.** `membership: "Gold"` gibi geçersiz bir kayıt isteği hiçbir doğrulamaya takılmadan veritabanına
kadar gidiyor, `CHECK` constraint'e çarpıp **500** dönüyordu (bkz. #006). Kullanıcı neyi yanlış girdiğini
öğrenemiyordu ve her geçersiz istek log'da "sunucu hatası" olarak görünüyordu.

**Çözüm.**
- İstek kontratlarına DataAnnotations eklendi. `[ApiController]` geçersiz istekleri controller'a ulaşmadan
  alan bazlı hatalarla **400 (ValidationProblemDetails)** olarak döndürüyor.
- **TC kimlik numarası için checksum doğrulaması** (`[TcKimlikNo]`). "11 hane" kontrolü, var olamayacak
  numaraları da kabul eder. TCKN'nin son iki hanesi ilk dokuz haneden hesaplanan kontrol haneleri.
  Algoritma, ASCII dışı rakamları da içeren testlerle kilitlendi (`char.IsDigit` Arapça rakamları kabul
  ederdi, bu yüzden `char.IsAsciiDigit` kullanıldı).
- Domain kurallarının ihlali (`DomainException`) artık **400** dönüyor. Mesajları son kullanıcı için yazıldığı
  için istemciye gösteriliyor.
- Kural uyumu: şifre en az 8 karakter, telefon zorunlu. Frontend kayıt formu da aynı kurallara getirildi.
  Önceden frontend 6 karakteri kabul ediyordu ve telefon boş gidebiliyordu.
- Login isteğinde sadece alanların dolu olup olmadığı kontrol ediliyor. Formatı kontrol etmek, saldırgana
  hangi alanın yanlış olduğunu söylerdi (#006 ile aynı mantık).

**Doğrulama sırasında bulunan sızıntı.** Bozuk JSON gönderildiğinde cevap
`"The JSON value could not be converted to Bank.Contracts.Auth.RegisterRequest. Path: $.firstName | BytePositionInLine: 40"`
şeklinde iç tip adını ve parser detayını içeriyordu. `AllowInputFormatterExceptionMessages = false` ile
genel bir mesaja çevrildi.

**Bir yanlış alarm.** İlk denemede geçerli istek de 400 aldı. Sebep API değil, test komutuydu: Windows'taki
bash `Ayşe` içindeki `ş` harfini UTF-8 olarak göndermemişti. Test gövdeleri UTF-8 dosyadan
(`--data-binary @file`) gönderilerek doğrulandı. Hatanın kaynağını kanıtlamadan API'yi "düzeltmeye"
girişmemek önemliydi.

**Commit:** `feat(api): validate request payloads and map domain errors to 400`

---

## #020 — Kişisel finans şeması ve veritabanı seviyesinde sahiplik garantisi

**Sprint:** 2

**İhtiyaç.** Sprint 1'deki domain modellerinin (kart, düzenli kalem, taksit planı) kalıcı olarak saklanması.

**Tasarım kararları.**
- **Ayrı `finance` şeması.** Kullanıcının kendi girdiği finans verisi, bankacılık çekirdeğinden (`dbo.Accounts`,
  `dbo.Cards`) ayrı tutuldu. `dbo.Cards` (bankanın verdiği kart) ile `finance.CreditCards` (kullanıcının takip
  ettiği herhangi bir bankanın kartı) farklı kavramlar. Ayrı şema bu sınırı görünür kılıyor.
- **Her tabloda bankacılık standardı kolonlar:**
  - `UserId`: her sorgu sahibine göre filtrelenecek.
  - `CreatedAt` / `UpdatedAt`: denetim izi.
  - `DeletedAt`: soft delete. Finansal kayıtlar fiziksel olarak silinmez, geçmiş analizler ve denetim için korunur.
  - `RowVersion`: SQL Server'ın her güncellemede otomatik değiştirdiği değer. Optimistic concurrency için
    kullanılacak (#022).
- **Domain kuralları `CHECK` constraint olarak tekrarlandı.** Bu kasıtlı bir tekrar. Veritabanına sadece uygulama
  yazmaz, ileride import scriptleri veya elle düzeltmeler de yazacak. Son savunma hattı veritabanı.
- **Filtered index'ler** (`WHERE DeletedAt IS NULL`): sorgular neredeyse her zaman silinmemiş kayıtları istiyor.
  İndeks sadece onları içerdiği için daha küçük kalıyor.

**Kritik karar: çapraz kullanıcı referansı.** Bir kullanıcı, başka bir kullanıcının kart ID'sini tahmin edip kendi
aboneliğine bağlamaya çalışabilir (IDOR'un bir türevi). Bunu sadece uygulama kodunda kontrol etmek, ilk
unutulan kontrol noktasında açık bırakır. Çözüm:
- `CreditCards` üzerinde `UNIQUE (CreditCardId, UserId)`.
- Kartı referans eden tablolarda **bileşik foreign key** `(CreditCardId, UserId) → CreditCards (CreditCardId, UserId)`.

Böylece bir satırın referans ettiği kartın aynı kullanıcıya ait olması **veritabanı tarafından garanti ediliyor**.

**Doğrulama.** Migration uygulandıktan sonra doğrudan SQL ile denendi:
- A kullanıcısının, B'nin kartına bağlı abonelik eklemesi → `FK_RecurringCashFlows_CreditCards` ihlali, reddedildi.
- Karta bağlı gelir eklemek → `CK_RecurringCashFlows_IncomeNotOnCard` ihlali, reddedildi.

**Not.** Doğrulama sırasında `sqlcmd` "QUOTED_IDENTIFIER" hatası verdi. Filtered index'li tablolara yazmak
`QUOTED_IDENTIFIER ON` gerektiriyor. `sqlcmd` varsayılan olarak `OFF` çalışıyor, SqlClient ise `ON`. Uygulamayı
etkilemiyor, elle çalıştırılan scriptlerde `sqlcmd -I` kullanılmalı. README'ye not düşüldü.

**Commit:** `build(db): add finance schema for cards, recurring items and installments`

---

## #021 — Kredi kartı CRUD: sahiplik, eşzamanlılık ve bir kültür hatası

**Sprint:** 2

**Kapsam.** `api/finance/credit-cards` üzerinde listeleme, getirme, oluşturma, güncelleme ve silme. Bu ilk CRUD,
sonraki kaynakların da izleyeceği kalıbı belirliyor.

**Katmanlar.**
- **Controller:** HTTP'ye özgü kararlar (201 + `Location`, 204, `If-Match` zorunluluğu).
- **Service:** kontrat ↔ domain eşlemesi ve iş kuralları.
- **Repository:** SQL ↔ domain eşlemesi.

Domain sınıfı (`CreditCard`) ne HTTP'yi ne SQL'i biliyor.

**Sahiplik (IDOR koruması).** Repository'nin her metodu `userId` alıyor ve her sorgu `WHERE UserId = @UserId`
içeriyor. Başka kullanıcının kartı için **403 değil 404** dönüyor. 403, "böyle bir kayıt var ama senin değil"
bilgisini sızdırır.

**Optimistic concurrency.** Senaryo: kullanıcı kartı iki sekmede açıyor, birinde limiti, diğerinde kesim gününü
değiştiriyor. Önlem alınmazsa son kaydedilen, öncekinin değişikliğini haberi olmadan siler ("lost update").
- Cevaplar kaydın `version` değerini (`ROWVERSION`'ın base64 hâli) içeriyor.
- `PUT` bu değeri HTTP standardındaki **`If-Match`** başlığında istiyor. Başlık yoksa **428 Precondition Required**
  dönüyor, istemci versiyonu "unutarak" veri ezemiyor.
- `UPDATE ... WHERE RowVersion = @ExpectedVersion` koşullu çalışıyor. Hiçbir satır etkilenmezse servis
  ayrım yapıyor: kayıt yoksa 404, kayıt var ama sürüm eskiyse **409**.

Version'ın body yerine başlıkta taşınması, POST ve PUT'un aynı istek DTO'sunu kullanmasını sağladı.

**Silme.** Soft delete. Karta bağlı aktif bir taksit veya abonelik varsa 409 dönüyor. Kontrol ve silme
**aynı `UPDATE ... WHERE NOT EXISTS (...)`** sorgusunda. Ayrı bir "kullanılıyor mu?" sorgusu ile silme arasına
yeni bir taksit girebilirdi (TOCTOU).

**Para birimi değişikliği.** Kartın para birimi, bağlı kalemler varken değiştirilemiyor. Aksi hâlde TRY taksitleri
USD bir karta bağlı kalır ve `InstallmentPlan.ScheduleOn` hata verir.

**Bulunan hata: Türkçe sistemde çöken doğrulama.** İlk smoke testte kart oluşturma **500** verdi. Log:
`0.01 is not a valid value for Decimal`. `[Range(typeof(decimal), "0.01", ...)]`, sınır değerini **sunucunun
kültürüyle** parse ediyordu. Türkçe'de ondalık ayracı virgül olduğu için `"0.01"` geçersizdi. Bu hata
İngilizce bir CI sunucusunda hiç görünmez, Türkiye'deki bir üretim sunucusunda ilk istekte ortaya çıkardı.
Çözüm: `ParseLimitsInInvariantCulture = true` ve `ConvertValueInInvariantCulture = true`.

**Doğrulama.** 13 senaryo iki kullanıcıyla gerçek API'ye karşı çalıştırıldı:
- oluşturma → 201
- başka kullanıcı listeleme → boş, getirme/güncelleme/silme → 404
- `If-Match` yok → 428
- güncel version → 200, eski version → 409, bozuk version → 400
- geçersiz body → 400 (Türkçe alan hataları)
- silme → 204, sonra 404, satır DB'de `DeletedAt` ile duruyor

Bu senaryolar sprint sonunda otomatik entegrasyon testlerine dönüştürülecek.

**Commit:** `feat(finance): add credit card CRUD with ownership and concurrency checks`

---

## #022 — Gelir / gider / abonelik CRUD ve kart referansının doğrulanması

**Sprint:** 2

**Kapsam.** `api/finance/recurring-cash-flows`: #021'deki kalıpla (sahiplik, `If-Match`, soft delete) CRUD.
Abonelik yönetimi için `GET ?kind=Subscription` filtresi.

**Kart referansı üç katmanda korunuyor.**

| Katman | Neyi yakalıyor | Neden orada |
|---|---|---|
| Domain (`RecurringCashFlow`) | Gelirin karta bağlanması | Saf iş kuralı, veriye ihtiyaç yok |
| Servis | Kartın bu kullanıcıya ait ve **silinmemiş** olması, para biriminin kartla aynı olması | Kullanıcıya anlamlı mesaj vermek için. Veritabanı soft delete'i ve para birimini göremez |
| Veritabanı | Başka kullanıcının kartı (bileşik FK, #020) | Uygulamada bir kontrol unutulsa bile son savunma |

Başka kullanıcının kartı seçildiğinde cevap **"Seçilen kart bulunamadı"** oluyor. "Bu kart size ait değil" demek,
kartın var olduğunu sızdırırdı.

**Diğer kararlar.**
- `StartDate` kontratta `DateOnly?` + `[Required]`. Değer tipi olsaydı, gönderilmediğinde sessizce `0001-01-01`
  olur ve doğrulamadan geçerdi.
- `DATE` kolonları repository'de `DateTime` olarak okunup `DateOnly`'ye çevriliyor. Dapper'ın `DateOnly` desteği
  sürümden sürüme değiştiği için en taşınabilir yol bu.
- Karta bağlı ödemenin para birimi kartınkiyle aynı olmalı. Gerçekte yurt dışı abonelikler TL karta döviz
  olarak yansıyabilir, ama kur dönüşümü projeksiyonu tahmine dayalı hâle getirir. Bu, bilinçli olarak kapsam dışı
  bırakıldı.

**Bilinen sınır.** Servis kartın silinmediğini kontrol ettikten sonra ve kayıt eklenmeden önce kart silinirse, silinmiş
karta bağlı bir kayıt oluşabilir. Kart silme sorgusu referans kontrolünü atomik yapıyor (#021), ama tersi yönde bir
kilit yok. Kişisel finans kullanımında aynı kullanıcının bu iki işlemi milisaniyeler içinde yapması pratikte beklenmiyor.
Gerekirse `UPDLOCK` ile kapatılabilir.

**Doğrulama.** Gerçek API'ye karşı:
- kendi kartına abonelik → 201
- karta bağlı gelir → 400
- başka kullanıcının kartı → 400 "Seçilen kart bulunamadı"
- TRY karta USD ödeme → 400
- başlangıç tarihi yok → 400, bitiş başlangıçtan önce → 400
- `?kind=Subscription` sadece aboneliği döndürdü, geçersiz filtre → 400
- **kullanımdaki kartı silmek → 409**. Abonelik silindikten sonra kart silindi → 204
- aynı version ile ikinci güncelleme → 409

**Commit:** `feat(finance): add CRUD for recurring income, expenses and subscriptions`

---

## #023 — Taksit planları: hesaplanan takvim, saklanmayan veri

**Sprint:** 2

**Kapsam.** `api/finance/installment-plans`: listeleme, getirme, oluşturma, silme. Cevap, her taksidin ekstre
tarihini, son ödeme gününü ve tutarını içeriyor.

**Karar 1: takvim saklanmıyor, her okumada hesaplanıyor.** Tabloda sadece planın kendisi (tutar, taksit sayısı,
alışveriş tarihi, kart) duruyor. Taksit satırlarını ayrı bir tabloda saklamak da mümkündü. Ancak o durumda
kartın kesim günü değiştiğinde saklanan tarihler eskir ve senkronize edilmeleri gerekir. Türetilebilir veriyi
saklamamak bu tutarsızlık sınıfını tamamen ortadan kaldırıyor. Hesaplama maliyeti ihmal edilebilir: en fazla 36 satır.

**Karar 2: `PUT` yok.** Gerçek hayatta taksitli bir alışverişin taksit sayısı veya tutarı sonradan değişmez.
Yanlış girilmiş bir plan silinip yeniden ekleniyor. Güncelleme olmadığı için version mekanizması da bu kaynakta
gereksiz (YAGNI). `ROWVERSION` kolonu tabloda duruyor, ihtiyaç olursa kullanılabilir.

**Karar 3: para birimi istekte yok.** Plan, kartın para biriminde oluşturuluyor. İstemcinin farklı bir para birimi
gönderip tutarsızlık yaratma ihtimali hiç oluşmuyor.

**Liste performansı.** Planlar listelenirken her plan için ayrı kart sorgusu yapılmıyor (N+1 sorunu). Kullanıcının
kartları tek sorguyla çekilip sözlüğe konuyor.

**Doğrulama.** Gerçek API'ye karşı:
- 25 Mart'ta 3 taksitle 1.000 TL → takvim Sprint 1'deki domain testiyle **birebir aynı**
  (333,34 / 333,33 / 333,33, Mayıs taksidi 1 Haziran'a kaymış).
- **Kartın kesim günü 20 → 15 yapıldı, plan tekrar okundu:** takvim otomatik olarak yeni kesim günlerine geçti.
  15 Nisan + 10 = Cumartesi olduğu için son ödeme günü 27 Nisan Pazartesi oldu.
- Başka kullanıcının kartı → 400, başka kullanıcının planı → 404, 37 taksit → 400.
- Planı olan kartı silmek → 409. Plan silindikten sonra liste boş döndü.

**Commit:** `feat(finance): add installment plans with computed payment schedules`

---

## #024 — Entegrasyon testleri: elle yapılan smoke testlerin otomatikleştirilmesi

**Sprint:** 2

**Sorun.** #021–#023'teki 30'u aşkın senaryo elle (curl) doğrulanmıştı. Elle yapılan test bir kez çalışır, sonraki
değişiklikte tekrarlanmaz.

**Çözüm.** `Bank.IntegrationTests` projesi:
- `WebApplicationFactory<Program>` API'yi bellekte ayağa kaldırıyor. İstekler gerçek pipeline'dan geçiyor:
  routing, JWT doğrulama, model doğrulama, exception handler, Dapper, SQL Server.
- **Gerçek SQL Server, ayrı veritabanı.** `BankDb_IntegrationTests` her koşuda silinip, migrator ile sıfırdan kuruluyor.
  Bunun için migrator `DatabaseMigrator` sınıfına çıkarıldı. Testler, canlıya giden scriptlerin aynısını
  çalıştırıyor, yani migration'lar da her seferinde test ediliyor.
- **Mock yok.** #021'deki kültür hatası ve #020'deki constraint'ler ancak gerçek bileşenlerle yakalanabilir.
- Her test kendi kullanıcısını oluşturuyor, bu yüzden testler birbirine bağımlı değil. Test kullanıcıları için
  checksum'ı geçerli TCKN'ler bir sayaçtan üretiliyor.

**Kapsanan senaryolar (17 test).**
- **Auth:** kayıt → giriş → korumalı endpoint; token yoksa 401; geçersiz kayıt 400 ve alan hataları;
  mükerrer kayıt 409; bilinmeyen kullanıcı ile yanlış şifre aynı cevabı alıyor.
- **Kart:** oluşturma (201 + `Location`); başka kullanıcı göremiyor, değiştiremiyor, silemiyor (404) ve
  verinin değişmediği doğrulanıyor; güncel version → 200, **eski version → 409 ve yeni veri korunuyor**;
  `If-Match` yok → 428; geçersiz değerler → 400; soft delete.
- **İlişkiler:** başka kullanıcının kartına abonelik → 400; `?kind=Subscription` filtresi; kullanımdaki kart
  silinemiyor, bağlı plan silinince siliniyor; taksit takvimi domain testindeki değerlerle aynı; kartın
  kesim günü değişince takvim yeniden hesaplanıyor.

**Testlerin koruduğunun doğrulanması.**
1. *İzolasyon:* 17 test kullanıcısının hepsi test veritabanında. Geliştirme veritabanına sızan kayıt sıfır.
2. *Mutation:* `CreditCardRepository`'den `UserId` filtresi ve `RowVersion` koşulu kaldırıldı → **3 test kırıldı**
   (sahiplik, eski version, başka kullanıcının kartına abonelik). Kod geri yüklendi, 17/17 geçti.

**Mutation deneyinden çıkan bulgu.** Sahiplik filtresi kaldırıldığında başka kullanıcının kartına abonelik ekleme
isteği servis kontrolünü geçti, ama **veritabanındaki bileşik foreign key (#020) eklemeyi yine reddetti**. Cevap
400 yerine 500 oldu, yani istemci için kusursuz değil. Ama veri bütünlüğü korundu. Katmanlı savunmanın neden
gerektiğini gösteren somut bir örnek: uygulama kodundaki bir hata, veri sızıntısına dönüşmedi.

**Commit:** `test: add API integration tests against a real SQL Server database`

---

## #025 — Nakit akışı projeksiyon motoru

**Sprint:** 3

**İhtiyaç.** Projenin ana sorusu: "Önümüzdeki aylarda taksitler, abonelikler, kart ekstreleri ve düzenli giderler
birlikte nasıl bir yük oluşturacak?" Bütçe uygulamalarının çoğu geçmişi özetler. Bu motor geleceği gösteriyor.

**Çözüm.** `CashFlowProjector` saf bir domain servisi. Girdi olarak düzenli kalemleri, kartları, taksit planlarını,
başlangıç bakiyesini ve ufku (ay sayısı) alıyor. Çıktıları:
- **Tarihli hareketler:** maaş, kira, kartsız abonelikler kendi tarihinde. **Kart harcamaları ekstrede toplanıp tek
  bir kart ödemesi olarak son ödeme gününde.** Ekstrenin satırları da listeleniyor.
- **Aylık özet:** gelir, toplam çıkış, kart ödemeleri, net, ay sonu bakiyesi, **ayın en düşük bakiyesi ve tarihi**,
  ödeme yükü oranı.
- **Dışarıda bırakılan kalemler:** başka para birimindekiler, açıkça listeleniyor.

Varsayımlar ayrı bir karar kaydında: [ADR-0003](adr/0003-cash-flow-projection-assumptions.md).

**Neden "en düşük bakiye"?** Kira 1'inde, maaş 15'inde olan bir kullanıcı için Mart ayı **+25.000 TL net** ile
kapanıyor, ama bakiye 1–15 Mart arasında **−15.000 TL**'de. Sadece ay sonu rakamlarına bakan bir özet bu baskıyı
hiç göstermez. Test: `Month_ClosingPositive_CanStillDipBelowZeroMidMonth`. Bu, araştırma sorusunun ("finansal
baskı daha erken görünür hale getirilebilir mi?") doğrudan bir cevabı.

**Testler (14).** Her varsayım bir testle kilitlendi:
- kart aboneliği harcama gününde değil son ödeme gününde
- ufuk öncesi harcama ufuk içinde ödeniyorsa dahil
- aynı ekstredeki abonelik ve taksit tek ödeme olarak birleşiyor (229,99 + 333,34 = 563,33)
- Sprint 1'de bulunan Haziran'da çift taksit durumu aylık özette görünüyor (Nisan 333,34 / Mayıs 0 / Haziran 666,66)
- ödeme yükü oranı ve gelirsiz ay için `null`
- ay ortasında başlayan ufuk
- bakiyenin aylar arası taşınması
- para birimi dışlaması
- bilinmeyen kart
- ufuk sınırları

**Mutation testinin bulduğu eksik.**
- Geriye bakış süresi 0'a indirildi → 3 test kırıldı. Bu varsayım iyi korunuyor.
- Aynı gün sıralaması ters çevrildi (gelir önce) → **hiçbir test kırılmadı.** ADR'de yazılı bir varsayımı hiçbir
  test korumuyordu. `SameDay_OutflowsAreAppliedBeforeIncome` eklendi. Aynı gün 10.000 TL maaş ve 15.000 TL kira
  ile en düşük bakiyenin −15.000 olduğunu doğruluyor. Mutation tekrarlandığında test kırıldı.

Bu deney, "testler geçiyor" ile "testler koruyor" arasındaki farkı somut olarak gösterdi.

**Commit:** `feat(domain): add cash-flow projection engine`

---

## #026 — Projeksiyon API'si: "bugün" hangi gün?

**Sprint:** 3

**Kapsam.** `GET /api/finance/projection?months=1..12` (varsayılan 6). Kullanıcının kartlarını, düzenli kalemlerini,
taksit planlarını ve aktif TL hesaplarının toplam bakiyesini toplayıp `CashFlowProjector`'a veriyor. Servisin işi sadece
veri toplamak ve sonucu kontrata çevirmek. Bütün projeksiyon kuralları domain'de kalıyor.

**Sorun 1: sunucu saati ile müşteri günü farklı.** Sunucular UTC çalışır. Türkiye UTC+3'tür. Gece 00:30'da İstanbul'da
yeni gün başlamışken UTC'de hâlâ önceki günün 21:30'u. `DateTime.UtcNow.Date` kullanılsaydı, her gece 00:00–03:00
arasında projeksiyon bir gün geriden başlardı ve kullanıcının zaten geçmiş bir günü "gelecek" olarak görünürdü.
- *Çözüm:* `BusinessDate.Today` tarihi `Europe/Istanbul` saat diliminde hesaplıyor. Sınır durumlar testlerle kilitlendi:
  31 Mart 21:30 UTC → 1 Nisan, 20:59 UTC → 31 Mart.

**Sorun 2: saate bağlı kod test edilemez.** `DateTime.Now` doğrudan çağrılsaydı, projeksiyon testi bugün geçer, yarın
başka sonuç üretirdi.
- *Çözüm:* Uygulama saati .NET'in `TimeProvider` soyutlamasından alıyor (`TimeProvider.System` olarak kayıtlı).
  Entegrasyon testlerinde API'nin saati Microsoft'un `FakeTimeProvider`'ı ile 31 Mart 2026'ya sabitlendi. Kendi
  sahte saat sınıfımı yazmak yerine resmi paket tercih edildi.

**Başlangıç bakiyesi.** Aktif TL hesaplarının toplamı. Henüz bir hesap API'si olmadığı için entegrasyon testi bakiyeyi
doğrudan SQL ile ekliyor. Bu, testte açıkça yorumla belirtildi.

**Doğrulama (entegrasyon testi, gerçek DB).** Hesapta 5.000 TL, ayın 1'inde 20.000 TL kira, 15'inde 45.000 TL maaş,
25 Mart'ta 3 taksitli 1.000 TL laptop ile 3 aylık projeksiyon:
- Ufuk 1 Nisan – 30 Haziran.
- Kart ödemeleri: Nisan 333,34 / Mayıs 0 / Haziran 666,66.
- **Nisan net olarak artıda kapanıyor, ama 1 Nisan'da bakiye −15.000 TL'ye düşüyor** (`BalanceGoesNegative = true`,
  `IsDeficit = false`).
- 30 Nisan'daki kart ödemesinin içeriği "Laptop (1/3)".
- Başka bir kullanıcının projeksiyonunda bu verilerin hiçbiri görünmüyor.
- `months=0` ve `months=13` → 400.

**Commit:** `feat(finance): expose cash-flow projection endpoint`

---

## #027 — Taksit senaryosu karşılaştırma motoru

**Sprint:** 4

**İhtiyaç.** "Bu bilgisayarı 3 yerine 6 taksitle alırsam ne olur?" Kullanıcı bir alışverişi gerçekleştirmeden önce
seçeneklerin gelecekteki nakit akışına etkisini görmek istiyor.

**Çözüm.** `ScenarioComparer` projeksiyon motorunu yeniden kullanıyor:
1. Alışveriş olmadan bir kez çalıştırıyor (**baz senaryo**).
2. Her seçenek için girdiye o seçeneğin hipotetik taksit planını ekleyip tekrar çalıştırıyor.
3. Her sonuç için olguları çıkarıyor: eksiye düşülen ay sayısı, en düşük bakiye ve tarihi, açık veren ay sayısı,
   en yüksek aylık çıkış, en yüksek yük oranı, dönem sonu bakiyesi.

Motor saf bir fonksiyon olduğu (#025) için senaryo hiçbir şeyi veritabanına yazmadan, sadece girdiyi değiştirerek
hesaplanabildi. `record` + `with` ifadesi çağıranın girdisini değiştirmiyor, bu bir testle kilitlendi.

**Tasarım kararları.**
- **Her seçeneğin kendi toplam tutarı var.** Satıcılar taksit sayısı arttıkça genellikle vade farkı ekler
  (ör. 3 taksit 9.000 TL, 6 taksit 9.450 TL). Hepsi için aynı fiyatı varsaymak uzun vadeli seçenekleri olduğundan iyi gösterir.
- **Bütün seçenekler aynı ufukta, en uzun seçeneğin son taksitine kadar karşılaştırılıyor.**
- **Sistem öneri yapmıyor.** "En iyi seçenek" diye bir alan yok. Proje kapsamı açık: karar destek ve simülasyon,
  tavsiye değil. Her seçenek için olgular yan yana sunuluyor, kararı kullanıcı veriyor.
- En fazla 6 seçenek. Aynı seçenek iki kez verilemez, geçmiş tarihli alışveriş kabul edilmez. 24 ayı aşan seçenekler
  reddediliyor.

**Örnek (unit test, değerler kod çalıştırılmadan önce elle hesaplandı).** Bakiye 1.000 TL; her ay 1'inde 20.000 TL
maaş, 2'sinde 18.400 TL gider. Kullanıcı ayda 1.600 TL artırıyor. 9.000 TL'lik laptop:

| | Aylık taksit | En yüksek aylık çıkış | Eksiye düşülen ay | En düşük bakiye |
|---|---|---|---|---|
| Almadan | – | 18.400 | 0 | – |
| 3 taksit | 3.000 | 21.400 | **5** | **−3.200** |
| 6 taksit | 1.500 | 19.900 | **0** | pozitif |

3 taksit aylık birikimi aştığı için bakiye Nisan'da eksiye düşüyor ve ancak Eylül'de toparlanıyor. 6 taksit birikimin
altında kalıyor. Aynı fiyatta iki seçenek de dönem sonunda aynı bakiyeyi bırakıyor. Fark sadece **yükün zamana dağılımı**.

**Mutation testi: ufuk neden uzatılıyor?** Ufuk en uzun seçeneğe göre uzatılmayıp sadece istenen 3 ayda bırakıldığında
**5 test kırıldı**. En öğreticisi `SamePrice_LeavesTheSameBalanceOnceEveryPlanIsPaidOff`: kısa ufukta 6 taksitlik seçenek,
son 3 taksiti pencerenin dışında kaldığı için **daha fazla para bırakıyormuş gibi** görünüyor. Bu, kullanıcıyı doğrudan
yanlış karara itecek bir ölçüm hatası olurdu.

**Commit:** `feat(domain): compare installment options for a planned purchase`

---

## #028 — Senaryo API'si ve ortak veri yükleyici

**Sprint:** 4

**Kapsam.** `POST /api/finance/scenarios/installments`. Planlanan bir kart alışverişini ve 1–6 ödeme seçeneğini
(taksit sayısı + toplam tutar) alıyor. Baz senaryo ve her seçenek için metrikleri ve aylık özetleri döndürüyor.

**Neden POST?** Senaryo bir kaynak oluşturmuyor ve hiçbir şey kaydetmiyor, ama karmaşık bir girdi (seçenek listesi)
alıyor. Bunu query string'e sığdırmak okunaksız olurdu. Hesaplama amaçlı POST, REST'te kabul gören bir kalıp. Cevap
`201` değil `200`.

**Refactor: tekrar eden kodun çıkarılması (DRY).** Projeksiyon servisi ile senaryo servisi aynı veriye ihtiyaç duyuyor:
kartlar, kalemler, planlar, bakiye ve "yarın". Bu toplama işi `ProjectionInputLoader`'a, kontrat eşlemesi de
`ProjectionMapper`'a taşındı. İki servis de artık birkaç satırdan ibaret ve sadece domain'i çağırıp sonucu eşliyor.

**Güvenlik.** Kart ID'si istekten geliyor ama domain'e sadece kullanıcının kendi kartları veriliyor. Başka kullanıcının
kartı `ScenarioComparer`'da "Senaryodaki kart bulunamadı" ile reddediliyor (400). Ayrı bir sahiplik kontrolü yazmaya
gerek kalmadı.

**Doğrulama (entegrasyon, gerçek DB).** #027'deki "dar bütçe" senaryosu API üzerinden kuruldu: hesap, maaş ve gider
kalemleri, kart. API, domain testindeki elle hesaplanmış değerleri **birebir** üretti:
- 3 taksit: 5 ay eksi bakiye, en düşük −3.200 TL.
- 6 taksit: 0 ay. Baz senaryo: 0 ay.
- Ufuk Eylül sonuna uzadı.

Ek testler:
- senaryo sonrası kullanıcının taksit planı listesi boş (hiçbir şey kaydedilmiyor)
- başka kullanıcının kartı → 400
- seçeneksiz istek → 400
- 0 taksitli seçenek → 400

**Commit:** `feat(finance): expose installment scenario comparison endpoint`

---

## #029 — İşlem geçmişi şeması ve import denetim kaydı

**Sprint:** 5

**İhtiyaç.** Analitik özellikler (kategori trendi, tekrarlayan ödeme ve anomali tespiti) geçmiş işlem verisi gerektiriyor.
Kullanıcı bu veriyi diğer bankalardan CSV ekstresi olarak getirecek veya demo için sentetik olarak üretecek.

**Neden mevcut `dbo.Transactions` değil?** O tablo bankanın kendi hesaplarına (`AccountId`) bağlı. Başka bir bankanın
ekstresindeki satırın bu sistemde bir hesabı yok. İki kavramı tek tabloya zorlamak ya nullable bir `AccountId`
ya da sahte hesaplar gerektirirdi.

**Tasarım.**
- `finance.ImportBatches`: her yükleme için bir satır (kim, ne zaman, hangi dosya, kaç satır, kaçı mükerrer).
  - Denetim izi sağlıyor.
  - Bir dosyadan gelen her şeyi tanımlayabiliyor (ileride "bu yüklemeyi geri al").
  - `ImportedRows + DuplicateRows = TotalRows` kuralı bir `CHECK` constraint ile garanti altında.
- `finance.Transactions`: tutar pozitif, yön ayrı kolonda (`dbo.Transactions` ile aynı kural). Kategori opsiyonel,
  çünkü CSV'lerin çoğunda kategori yok.
- **Mükerrer koruması veritabanında:** `(UserId, Fingerprint)` üzerinde `UNIQUE` index. Uygulama mükerrer satırları zaten
  ayıklayacak, ama eşzamanlı iki yükleme gibi durumlarda son garanti veritabanı. Index sadece silinmemiş satırları
  kapsıyor (filtered), böylece silinen bir import tekrar yüklenebiliyor.
- Analitik sorgular için `(UserId, BookingDate)` index'i. Tutar, yön ve kategori `INCLUDE` ile index'te taşınıyor, böylece
  sorgular tabloya gitmeden cevaplanabiliyor (covering index).

**Commit:** `build(db): add transaction history and import batch tables`

---

## #030 — Banka ekstresi CSV parser'ı: gerçek dünya formatları

**Sprint:** 5

**İhtiyaç.** Kullanıcılar başka bankalardan aldıkları CSV ekstrelerini yükleyebilmeli. "CSV" tek bir format değil.

**Karşılanan farklılıklar.**

| Sorun | Nasıl ele alındı |
|---|---|
| **Encoding.** Türkçe Excel'den kaydedilen CSV UTF-8 değil **Windows-1254** olur. Yanlış okunursa "Ş" bozulur | BOM varsa UTF-8. Yoksa *katı* UTF-8 çözümleme denenir, geçersiz byte dizisinde 1254'e düşülür. .NET Core'da 1254 için `CodePagesEncodingProvider` kaydı gerekiyor |
| **Ayraç.** Ondalık ayracı virgül olduğu için Türk bankaları genellikle `;` kullanır | Başlık satırında en sık geçen `;`, `,` veya tab seçiliyor |
| **Tırnaklı alanlar.** `"Migros, Kadıköy"` | Kendi parser'ımı yazmak yerine BCL'deki RFC 4180 uyumlu `TextFieldParser` kullanıldı. Ek paket gerekmiyor |
| **Başlıklar.** "Tarih"/"Date", "İşlem Açıklaması", büyük-küçük harf | Normalize edilip eşanlamlı listelerle eşleştiriliyor |
| **Borç/Alacak kolonları.** Bazı bankalar tek bir işaretli tutar yerine iki kolon kullanır | İki düzen de destekleniyor, tek işaretli tutara çevriliyor |
| **Tutar formatları.** `1.234,56`, `-250,00 TL`, `₺ 1.500`, `1,234.56`, sondaki eksi `250,00-`, Excel'in NBSP binlik ayracı | `StatementAmount`: sondan gelen ayraçtan sonra 1–2 hane varsa o ondalık ayracı |
| **Tarihler.** `05.03.2026`, `5.3.2026`, `05/03/2026`, `2026-03-05`, saatli | `ParseExact` ile kabul edilen formatların açık listesi |

Hatalı satırlar **satır numarasıyla** raporlanıyor. Dosyanın kabul edilip edilmeyeceğine çağıran karar veriyor (#031).

**Testlerin yakaladığı iki hata.**
1. **`"1.2.3"` sessizce 12,3 oluyordu.** Son ayraçtan sonra tek hane olduğu için ondalık sayılıyor, kalan noktalar da
   atılıyordu. Bozuk bir ekstre değeri hata vermek yerine **yanlış bir tutara** dönüşüyordu. Bir finans uygulamasında bu
   en tehlikeli hata türü. Çözüm: binlik grupları tam üç haneli olmalı, ondalık ayracından farklı karakter olmalı ve
   tek tür olmalı. `"1.2.3"`, `"12,3,4"`, `"1,234.5.6"` artık reddediliyor.
2. **Türkçe İ problemi.** İlk kodda `"TARİH".ToLowerInvariant()` sonucunun `"tari̇h"` (i + birleşik nokta) olduğunu
   **varsaydım** ve o birleşik noktayı silen bir satır yazdım. Testler kırıldı. Tahminle düzeltmek yerine küçük bir
   programla gerçek davranışı ölçtüm: .NET'in invariant dönüşümü `İ` harfini **hiç değiştirmiyor** (`"TARİH"` → `"tarİh"`,
   U+0130 aynen kalıyor). Öte yandan `tr-TR` kültürüyle dönüştürmek de `"DESCRIPTION"`'ı `"descrıptıon"` (noktasız ı)
   yapıyor. Çözüm: NFC normalizasyonu, invariant küçük harf ve Türkçe harflerin (`İ`, `ı` dahil) açıkça ASCII'ye
   katlanması. Yanlış varsayımı içeren kod yorumu da düzeltildi.

**Küçük ama önemli bir temizlik.** Kaynak kodda `"\u00A0"` yazılmış sanılan yerde aslında **görünmez bir NBSP karakteri**
vardı. Ekranda iki aynı `Replace(" ", "")` çağrısı gibi görünüyordu. Kodu okuyan biri "gereksiz tekrar" diye birini
silebilir ve Excel'in binlik ayracı sessizce bozulurdu. Açık `\u00A0` kaçış dizisine çevrildi ve kaynakta literal NBSP
kalmadığı bir script ile doğrulandı.

**Testler:** `StatementAmountTests` (20 vaka) ve `StatementCsvParserTests` (12 test). Windows-1254 testi metni gerçekten
1254 ile kodlayıp parser'a veriyor.

**Commit:** `feat(import): parse bank statement CSV exports`

---

## #031 — Ekstre import'u: mükerrer koruması, toplu yazma ve "ya hep ya hiç"

**Sprint:** 5

**Kapsam.**
- `POST /api/finance/transactions/import`: multipart CSV, en fazla 2 MB ve 10.000 satır.
- `GET /api/finance/transactions?from=&to=&page=&pageSize=`: sayfalı geçmiş.

**Karar 1: Satır parmak izi.** Aynı ekstrenin tekrar yüklenmesi veya Ocak–Mart ile Mart–Mayıs ekstrelerinin çakışması
mükerrer kayıt üretmemeli. Ama aynı gün aynı kafede iki 85,50 TL'lik kahve **iki gerçek işlemdir**. Sadece
"tarih + tutar + açıklama" ile ikincisi mükerrer sanılıp atılırdı. Parmak izi şu şekilde hesaplanıyor:

`SHA-256(tarih | tutar | katlanmış açıklama | bu satırın dosyadaki aynı satırlar arasındaki sırası)`

Böylece:
- İki kahve 1. ve 2. sıra olduğu için ayrı kayıt oluyor.
- Aynı dosya tekrar yüklendiğinde aynı sıralar, yani aynı parmak izleri çıkıyor ve hepsi mükerrer sayılıyor.
- Çakışan ekstrede ortak gün aynı sıralarla geldiği için sadece yeni satırlar ekleniyor.

Varsayım: bir ekstre bir günü ya tamamen içerir ya hiç içermez. Bankalar ekstreyi gün sınırında kestiği için bu geçerli.

**Karar 2: Toplu yazma.** 10.000 satır için 10.000 ayrı `INSERT` yerine:
1. `SqlBulkCopy` ile oturuma özel geçici tabloya (`#Staging`) yükleme.
2. Mükerrer sayımı `UPDLOCK, HOLDLOCK` ile. Aynı dosyanın eşzamanlı iki yüklemesi, kontrol ile ekleme arasına giremiyor.
3. Tek bir `INSERT ... SELECT ... WHERE NOT EXISTS`.

Hepsi tek transaction'da. Veritabanındaki `UNIQUE (UserId, Fingerprint)` index'i (#029) son garanti.

**Karar 3: Ya hep ya hiç.** Tek bir hatalı satır bile dosyanın tamamını reddediyor. Cevap 400 ve satır numaralı bir
hata listesi içeriyor (en fazla 50 hata). Kısmi import, kullanıcıyı hangi satırların girip hangilerinin girmediğini
tahmin etmeye zorlar. Bankacılıkta toplu işlemlerde (ör. EFT dosyaları) de genel yaklaşım budur.

**Testin yakaladığı hata: Türkçe I, ikinci kez.** Parmak izinde açıklamayı `ToUpperInvariant()` ile normalize ediyordum.
`"Kadıköy".ToUpperInvariant()` sonucu `"KADıKÖY"`: noktasız ı değişmiyor. Bir banka `MİGROS`, diğeri `MIGROS` yazarsa
çakışan ekstrede **aynı işlem iki kez** kaydedilirdi. #030'daki başlık katlama mantığı ortak bir `TextFolding.Fold`
fonksiyonuna çıkarıldı ve iki yerde de kullanılıyor. Aynı problemi iki ayrı yerde iki ayrı şekilde çözmek yerine tek
çözüm, tek test seti (DRY).

**Kaynak koddaki görünmez karakterler.** Yazım sırasında `\u00A0` ve `\u0307` kaçış dizileri kaynakta görünmez
karakterlere dönüşmüştü (#030). Bütün `.cs` dosyaları bir script ile tarandı. Kalan tek eşleşme bir dosya başındaki
UTF-8 BOM'du (.NET şablonunun standart çıktısı, zararsız).

**Doğrulama (8 entegrasyon testi, gerçek DB).**
- 4 satırlık ekstre (iki aynı kahve dahil) → 4 kayıt.
- Aynı dosya tekrar → 0 yeni, 4 mükerrer.
- Çakışan ekstre → sadece yeni satır eklendi (1 yeni, 3 mükerrer).
- Hatalı dosya → 400, "Satır 3", "Satır 4" hataları, **hiçbir satır kaydedilmedi**.
- Windows-1254 dosya → "ŞOK MARKET ÇAĞLAYAN" doğru kaydedildi.
- Başka kullanıcı → 0 kayıt görüyor.
- Tarih filtresi ve sayfalama.
- 2 MB üstü dosya → 413.

**Commit:** `feat(finance): import statement CSVs into transaction history`

---

## #032 — Deterministik sentetik işlem geçmişi

**Sprint:** 5

**İhtiyaç.**
1. **Demo:** yeni bir kullanıcı boş bir hesapla analitik ekranlarını göremez.
2. **Ölçüm:** Sprint 6'daki tekrarlayan ödeme ve anomali tespiti, "doğru cevabı" bilinen veriyle test edilmeli.

Gerçek kişisel finans verisi açık veri setlerinde neredeyse yok. Proje tanımı da sentetik profilleri öneriyor.

**Çözüm.** `SyntheticHistoryGenerator`, İstanbul'da maaşlı çalışan bir kişinin geçmişini üretiyor. Her desen bilinçli
olarak seçildi ve testle kilitlendi:

| Desen | Neden var | Test |
|---|---|---|
| Maaş 15'inde, hafta sonuysa **önceki iş günü** | Bordro pratiği | 15 Şubat 2025 Cumartesi → 14 Şubat |
| Kira, abonelikler, internet: sabit gün, sabit tutar | Tekrarlayan ödeme tespitinin bulması gerekenler | Netflix 12 ay boyunca 5'inde, 229,99 |
| Doğalgaz kışın ~8 kat yüksek | Mevsimsellik. Trend analizi bunu enflasyondan ayırt etmeli | Ocak > 3 × Temmuz |
| Market, restoran ve faturalarda aylık ~%2,5 artış | Enflasyon. Kategori trendinde görünmeli | Son çeyrek ortalaması > ilk çeyrek |
| Ayda %20 ihtimalle 12–35 bin TL elektronik alışverişi, `IsAnomaly = true` | Anomali tespiti için **ground truth** | Normal harcamanın en az 5 katı |

**Belirlenimcilik (determinism).** Aynı `seed` her zaman aynı geçmişi üretiyor, bu bir testle kilitlendi. .NET'te seed
verilen `Random` örnekleri tam da bu amaçla eski algoritmayı koruyor. Testler, demo ve tezdeki deneyler tekrarlanabilir.

**Aynı yoldan geçen veri.** Üretilen satırlar CSV import'unun kullandığı parmak izi ve toplu yazma yolundan geçiyor
(`POST /api/finance/transactions/demo?months=12&seed=42`). İki faydası var:
- Ayrı bir yazma yolu yok, kod tekrarı yok.
- **Aynı seed ile ikinci çağrı hiçbir şey eklemiyor.** Bu, entegrasyon testiyle doğrulandı.

Import kaydında kaynak `Synthetic` olarak işaretleniyor, böylece demo verisi gerçek ekstrelerden her zaman ayırt edilebilir.

**Commit:** `feat(finance): generate reproducible synthetic transaction history`
