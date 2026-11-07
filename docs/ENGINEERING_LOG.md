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
kullanıcı bunu fark etmez. Bu, projenin çıkış sorusunun ("gelecekteki yük daha erken görünür
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
hiç göstermez. Test: `Month_ClosingPositive_CanStillDipBelowZeroMidMonth`. Bu, projenin çıkış sorusunun ("finansal
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
verilen `Random` örnekleri tam da bu amaçla eski algoritmayı koruyor. Testler, demo ve değerlendirmedeki ölçümler tekrarlanabilir.

**Aynı yoldan geçen veri.** Üretilen satırlar CSV import'unun kullandığı parmak izi ve toplu yazma yolundan geçiyor
(`POST /api/finance/transactions/demo?months=12&seed=42`). İki faydası var:
- Ayrı bir yazma yolu yok, kod tekrarı yok.
- **Aynı seed ile ikinci çağrı hiçbir şey eklemiyor.** Bu, entegrasyon testiyle doğrulandı.

Import kaydında kaynak `Synthetic` olarak işaretleniyor, böylece demo verisi gerçek ekstrelerden her zaman ayırt edilebilir.

**Commit:** `feat(finance): generate reproducible synthetic transaction history`

---

## #033 — Analitik temeli ve kategori trendleri

**Sprint:** 6

**İhtiyaç.** "Kategori bazında harcama trendleri": hangi kategoriye ne kadar harcanıyor, bu tutar artıyor mu?

**Temel yapı taşları.**
- `HistoryEntry`: analitiğin gördüğü haliyle bir geçmiş işlem (tarih, açıklama, işaretli tutar, kategori). Kategorisi
  olmayanlar tek bir "Kategorisiz" grubunda toplanıyor.
- `YearMonth`: analitiğin temel birimi olan takvim ayı. `(Year, Month)` ikilisini her yerde elle taşımak yerine
  karşılaştırılabilir bir değer tipi.
- `Statistics`: medyan, MAD, standart sapma ve en küçük kareler eğimi. Sonraki analizörlerin hepsi bunları kullanacak,
  bu yüzden her biri tek bir yerde tanımlı ve tek bir yerde test ediliyor.

**Kategori trendi.** `CategoryTrendAnalyzer` her kategori için şunları üretiyor:
- aylık harcama (harcama olmayan ay sıfır olarak, eksik bırakılmadan)
- toplam ve aylık ortalama
- toplam içindeki pay
- son ayın bir önceki aya göre değişimi
- dönem boyunca **en küçük kareler eğimi** (TL/ay)

Sadece çıkışlar harcama sayılıyor, gelir hariç.

**Neden iki ayrı trend ölçüsü?** Aydan aya değişim tek bir aya duyarlı: tek bir büyük market alışverişi "+%40" gösterebilir.
Eğim bütün dönemi kullandığı için gürültüye daha dayanıklı. İkisi birlikte hem "son ayda ne oldu" hem de "genel gidişat
ne" sorularını cevaplıyor.

**Bilinen sınır.** Doğalgaz gibi mevsimsel kategorilerde eğim yanıltıcı olabilir. Ekim–Mart arası bakılırsa artış,
Mart–Ağustos arası bakılırsa düşüş görünür. Mevsimsellik ayrıştırması için en az iki yıllık veri gerekir. Bu sınır API
dokümantasyonunda ve raporda belirtilecek.

**Commit:** `feat(domain): add category spending trends`

---

## #034 — Tekrarlayan ödeme tespiti

**Sprint:** 6

**İhtiyaç.** Kullanıcılar aboneliklerinin ve faturalarının hepsini elle girmez. Çoğu zaman unuttukları aboneliklerin farkında
bile değildirler. Geçmişten "bunları her ay düzenli ödüyorsun" diye önerebilmek, projeksiyonun (#025) eksiksiz olması için şart.

**Kurallar.** Aynı yönde (gelir veya gider) bir satıcı şu koşulları sağlarsa tekrarlayan sayılıyor:
1. En az **3 ayda** görülmüş ve **ayda en fazla bir kez**. Market alışverişi bu kuralla eleniyor.
2. İlk ve son göründüğü ay arasındaki ayların **en az %75'inde** var. Atlanan bir ay tolere ediliyor.
3. Ayın günü tutarlı: gün değerlerinin **MAD'ı ≤ 3**. Maaşın hafta sonu kaymasıyla 13–15 arasında oynaması tolere ediliyor.
   Ortalama yerine medyan ve MAD kullanıldığı için tek bir istisnai gün sonucu bozmuyor.

Tutarlar medyanın ±%5'i içindeyse **sabit** (abonelik, kira), değilse **değişken** (fatura). Satıcı adları #031'deki Türkçe katlama
ile karşılaştırılıyor. `TextFolding` saf bir metin kuralı olduğu ve artık domain'de de gerektiği için Application'dan Domain'e
taşındı (`git mv` ile, geçmiş korundu).

**Değerlendirme yöntemi (overfitting'e karşı).**
- Eşikler sentetik profillerin **1–10** seed'leriyle ayarlandı.
- Son değerlendirme **daha önce hiç bakılmamış 101–110** seed'lerinde yapıldı.
- Kabul eşiği (precision ve recall ≥ 0,95) test yazılmadan **önce** belirlendi.

Eşiği sonuca göre ayarlamak, sonucu olduğundan iyi gösterirdi.

**Sonuç.**

| Veri seti | Doğru pozitif | Yanlış pozitif | Kaçırılan | Precision | Recall |
|---|---|---|---|---|---|
| Ayar (seed 1–10) | 90 | 0 | 0 | 1,00 | 1,00 |
| **Test (seed 101–110)** | 90 | 0 | 0 | **1,00** | **1,00** |

Kira, spor salonu ve abonelikler **sabit**, elektrik, doğalgaz ve su **değişken** olarak doğru sınıflandırıldı. Haftalık İstanbulkart
dolumu ve market alışverişleri (ayda birden fazla) doğru şekilde elendi.

**Sınır (raporda açıkça yazılmalı).** Sentetik veri gerçek veriden daha düzenli: abonelikler her ay tam aynı gün ve tutarla
geliyor. %100 sonucu gerçek dünya performansı olarak sunulmamalı. Bu yüzden gerçek hayattaki bozulmalar ayrı testlerle denendi:
- **zam gelen abonelik** → hâlâ tespit ediliyor, "değişken" olarak
- **atlanan bir ay** → tolere ediliyor
- **farklı yazımlar** (`MİGROS`/`MIGROS`) → tek seri
- **rastgele günlerde aylık harcama** → elendi
- **aynı ay içinde iade + tekrar çekim** → **tespit edilemiyor**. Bu bilinen bir sınır ve bir testle belgelendi. İadeleri eşleyip
  düşen bir ön işleme adımı ile çözülebilir.

**Commit:** `feat(domain): detect recurring payments in transaction history`

---

## #035 — Anomali tespiti ve duyarlılık analizi

**Sprint:** 6

**İhtiyaç.** "Beklenmeyen/sıra dışı harcamaları işaretleme." Kullanıcıya "bu harcama senin için alışılmadık" diyebilmek. Bir yargı
olarak değil, bir açıklama olarak: "Market kategorisindeki tipik tutarının 12 katı".

**Yöntem: Iglewicz ve Hoaglin'in değiştirilmiş z-skoru.** `M = 0,6745 × (ln x − medyan) / MAD`, `M > 3,5` ise işaretleniyor.

| Karar | Gerekçe |
|---|---|
| Ortalama/standart sapma değil, **medyan/MAD** | Tek bir 30.000 TL'lik harcama ortalamayı o kadar yukarı çeker ki kendini normal gösterir ("masking"). Medyan etkilenmez. |
| **Log ölçek** | Harcamalar sağa çarpık. 100 → 200 TL, 10.000 → 10.100 TL'den daha büyük bir sapmadır. |
| Eşik **3,5** | Literatürde önerilen değer. Veriye göre **ayarlanmadı**. |
| Kategori içi kıyas, ≥10 örnek yoksa **tüm harcamalarla** kıyas | Yılda iki kez görülen bir kategori için kategori içi istatistik anlamsız. |
| **Tekrarlayan ödemeler hariç** (#034) | 20.000 TL kira büyük ama beklenen bir ödeme. |
| Sadece **yukarı yönlü** sapmalar | Alışılmadık derecede küçük bir harcama kullanıcıya bir şey söylemez. |
| MAD = 0 durumu | Değerlerin yarısından fazlası aynıysa (sabit İstanbulkart dolumu) MAD sıfır olur. Ortalama mutlak sapmaya düşülüyor. Bu durum ayrı bir testle kilitlendi. |

**Değerlendirme 1: etiketli anomaliler** (seed 101–110, kabul eşiği önceden ≥ 0,95). **36/36 yakalandı, 0 yanlış pozitif.**

**Ama bu sonuç yanıltıcı derecede kolay.** Sentetik anomaliler 12–35 bin TL, tipik harcama ise yaklaşık 600 TL. Bunları her yöntem bulur.
Asıl soru şu: *yöntem ne kadar küçük bir sapmayı yakalayabiliyor?*

**Değerlendirme 2: duyarlılık analizi.** Temiz sentetik geçmişlere (seed 1–10) her ay, market medyanının **k katı** büyüklüğünde bir
market alışverişi enjekte edildi. 120 enjeksiyon, 3.386 normal işlem:

| Eşik | k=2 | k=3 | k=4 | k=5 | k=8 | Yanlış pozitif |
|---|---|---|---|---|---|---|
| 2,0 | 0,00 | 0,50 | 1,00 | 1,00 | 1,00 | 0 / 3.386 |
| 2,5 | 0,00 | 0,00 | 0,60 | 1,00 | 1,00 | 0 / 3.386 |
| 3,0 | 0,00 | 0,00 | 0,00 | 0,50 | 1,00 | 0 / 3.386 |
| **3,5** | 0,00 | 0,00 | 0,00 | 0,00 | **0,90** | 0 / 3.386 |

**Yorum.**
- Varsayılan eşikte yöntem, **değişkenliği yüksek bir kategoride ancak tipik tutarın yaklaşık 8 katını** güvenilir şekilde yakalıyor.
- Bu bir hata değil, matematiğin sonucu. Yakalanabilen en küçük oran `exp(eşik × MAD / 0,6745)`. Sentetik market harcamaları 250–1.400 TL
  arasında (log-MAD ≈ 0,4), yani zaten doğal olarak 5,6 kat değişiyor. Değişkenliği düşük bir kategoride (ör. sabit ulaşım dolumu)
  çok daha küçük sapmalar yakalanıyor (`IdenticalTypicalAmounts_StillLetAnOutlierThrough`).
- **Hiçbir eşikte yanlış pozitif çıkmadı.** Ama bu sentetik verinin sınırlı (uniform) dağılımından kaynaklanıyor. Gerçek harcamaların uzun
  kuyrukları olduğu için düşük eşikler gerçekte yanlış alarm üretecektir. Bu yüzden varsayılan literatürdeki temkinli 3,5'te bırakıldı.
  Eşik, ileride bir "hassasiyet" ayarı olarak kullanıcıya açılabilecek şekilde parametreleştirildi.

**Hata ayıklama notu.** Bir test 485 TL beklerken 500 TL buldu. Kod doğruydu, beklenti yanlıştı: medyanı 12 market tutarı üzerinden
hesaplamıştım, ama taban çizgisi işaretlenen alışverişin kendisini de içeriyor (13 değer). Medyanın bu ekstra değere rağmen sadece
7. sıraya kayması, robust istatistiğin neden seçildiğini de gösteriyor.

**Commit:** `feat(domain): flag unusually large purchases with a robust z-score`

---

## #036 — Harcama tahmini: basit yöntemler yeterli mi?

**Sprint:** 6

**Soru.** "Basit istatistiksel tahminler kişisel nakit akışı planlamasında yeterli olabilir mi?"

**Neyi tahmin ediyoruz?** Projeksiyon (#025) sadece bilinen sabit kalemleri içeriyor: kira, maaş, taksit. Market, restoran ve ulaşım gibi
günlük değişken harcamaları göstermediği için **olduğundan iyimser**. `EverydaySpending` bu harcamaların aylık toplamını çıkarıyor:
tekrarlayan ödemeler (#034) hariç, çünkü projeksiyon onları zaten kesin olarak biliyor; tek seferlik anomaliler (#035) hariç, çünkü
"tipik ayı" bozarlar.

**Yöntemler.** Bilinçli olarak basit ve açıklanabilir: **naif** (geçen ay), **hareketli ortalama** (son 3 ay), **doğrusal trend**
(son 6 aya en küçük kareler doğrusu).

**Yöntem seçimi ve belirsizlik: rolling-origin backtest.** Her geçmiş ay, sadece ondan önceki aylar kullanılarak tahmin ediliyor ve
gerçekleşenle kıyaslanıyor. En düşük MAPE'li yöntem kullanılıyor. "Neden bu yöntem?" sorusunun cevabı bir sayı. %95 aralık:
`tahmin ± 1,96 × RMSE × √h`. √h genişlemesi hataların bağımsız olduğunu varsayan bir yaklaşım. Bu yüzden ölçülen kapsama oranıyla
birlikte raporlanıyor.

**"Yeterli" tanımı ölçümden önce kondu:** MAPE ≤ %10 **ve** %95 aralığın kapsama oranı ≥ %80.

**Sonuç** (test seed'leri 101–110, 24 aylık geçmiş, 7.–24. aylar için tek adımlı tahmin):

| | MAPE | %95 aralık kapsama |
|---|---|---|
| Naif | %18,9 | %96 |
| **Hareketli ortalama** (10 profilin 8'inde seçildi) | **%17,6** | **%97** |
| Doğrusal trend | %22,4 | %97 |
| *Kâhin (gerçek beklenen değeri biliyor)* | *%12,0* | – |

**Yorum.**
1. **Önceden konan MAPE ≤ %10 eşiği karşılanmadı.** Bunu gizlemek yerine nedenini ölçtüm. **Kâhin referansı:** her ayın gerçek
   beklenen değeri 300 bağımsız profilin ortalamasıyla yaklaşık olarak bulundu. Gerçek beklenen değeri bilen kusursuz bir tahminci bile
   **%12** hata yapıyor. Bu, market alışverişlerinin sayısındaki ve tutarındaki rastgelelikten gelen **indirgenemez gürültü**. Yani
   %10 eşiği bu veri için ulaşılamaz bir hedefti. Benim önceden koyduğum kriter gerçekçi değildi.
2. Basit hareketli ortalama bu tabanın yaklaşık 5,6 puan üzerinde. Daha karmaşık yöntemlerin kazanabileceği alan bu 5,6 puanla sınırlı.
3. **Aralıklar iyi kalibre** (%95 hedefe karşı %96–97). Önceden konan ikinci kriter karşılandı ve bir testle kilitlendi.
4. **Ürün için sonuç:** basit yöntemler tek bir sayı olarak "gelecek ay 18.400 TL harcayacaksın" demekte orta düzeyde kalıyor. Ama
   "15.000 ile 22.000 TL arasında harcayacaksın" demekte **güvenilir**. Planlama için doğru olan, noktasal tahmin değil aralık göstermek.
5. Doğrusal trend, enflasyona rağmen en kötü sonucu verdi. 6 aylık pencerede gürültü eğimi yanıltıyor. Basit trend kestirimi,
   gürültülü kısa serilerde riskli.
6. Ayar seed'lerinde (1–10) en iyi MAPE %11,7, test seed'lerinde %17,6. 10 profil varyansı güvenilir ölçmek için az. Değerlendirme
   raporunda daha fazla profil kullanılacak.

**Hata ayıklama notu.** Bir test 204 bekledi, kod 196 buldu. Elle yeniden hesaplandı (eğim 330/17,5, kesişim 82,86) ve doğru değerin 196
olduğu görüldü. Kod değil beklenti yanlıştı. Testteki beklenen değerin yanına hesabın kendisi yorum olarak yazıldı.

**Commit:** `feat(domain): forecast everyday spending with backtested simple methods`

---

## #037 — Analitik API'si

**Sprint:** 6

**Kapsam.** `api/finance/analytics` altında dört salt-okunur endpoint:

| Endpoint | Ne döndürüyor |
|---|---|
| `category-trends?months=6` | Son N tamamlanmış ayın kategori bazında harcaması, payı, değişimi, eğimi |
| `recurring-payments` | Tespit edilen tekrarlayan ödemeler, sabit/değişken ayrımı, bir sonraki beklenen tarih ve **zaten takipte mi?** bilgisi |
| `anomalies?months=6` | Son N aydaki olağandışı harcamalar ve açıklamaları ("Tüm harcamalardaki tipik tutarın 24 katı") |
| `forecast?months=3` | Günlük harcama tahmini, %95 aralığı ve üç yöntemin backtest sonuçları |

**Tasarım kararları.**
- **Sadece tamamlanmış aylar.** Ayın 10'unda bu ayın market harcamasını geçen ayın tamamıyla kıyaslamak "%70 düşüş" gibi yanlış bir
  tablo çizer. Trend ve tahmin son tamamlanmış ayda bitiyor.
- **Anomali taban çizgisi 12 ay.** "Son 1 ayın anomalileri" istense bile neyin olağandışı olduğu kullanıcının bir yıllık alışkanlıklarına
  göre belirleniyor. Sadece birkaç haftalık veriyle karar verilmiyor.
- **Kısa geçmişte tahmin yok.** 6 aydan az tamamlanmış ay varsa 400 dönüyor ve gerekçe açıkça yazılıyor. Az veriyle üretilmiş güvenilmez
  bir tahmin, tahmin olmamasından daha kötü. Geçmiş, ilk işlemin olduğu aydan başlıyor, öncesi sıfır harcamalı aylarla doldurulmuyor.
- **"Takipte mi?" eşleşmesi.** Kullanıcının yazdığı ad ("Netflix") ile ekstre metni ("NETFLIX.COM") nadiren aynı. Türkçe katlama
  sonrası birinin diğerini içermesi yeterli sayılıyor. Takipte olmayan tekrarlayan ödemeler, projeksiyona eklenmesi gereken adaylar.
  Unutulmuş bir abonelik projeksiyonu yanıltır. Bu bir sezgisel kural (heuristic). Yanlış eşleşme ihtimali belgelendi.
- **Tavsiye yok.** Controller dokümantasyonunda açıkça yazılı: analitik desenleri tarif ediyor, finansal ürün veya karar önermiyor.

**Doğrulama (6 entegrasyon testi, gerçek DB, uçtan uca: üretici → import → depolama → analitik → HTTP).**
- Trendler tam olarak Eylül 2025 – Şubat 2026 aylarını kapsıyor. En büyük kategori Konut.
- Netflix "sabit, 229,99" olarak bulundu. "Netflix" adlı bir kalem eklenince **takipte** görünüyor. Spotify takipte değil.
  Doğalgaz "değişken", maaş "gelir".
- **İşaretlenen her anomali, üreticinin enjekte ettiği bir elektronik alışverişi.** Başka hiçbir şey işaretlenmedi.
- Tahmin 11 tamamlanmış aya dayanıyor ve Mart–Mayıs 2026 için aralık veriyor. 3 aylık geçmişle 400 dönüyor.
- Başka bir kullanıcı hiçbir şey görmüyor.

**Commit:** `feat(finance): expose analytics endpoints`

---

## #038 — Değerlendirme raporu ve yeniden üretilebilirlik

**Sprint:** 6

**Sorun.** #034–#036'daki ölçümler geçici bir keşif programıyla yapılmıştı. "Sonuçlar şunlar" demek yetmez, "sonuçlar
şu komutla yeniden üretilir" demek gerekir. Ayrıca #036'daki tahmin ölçümü 10 profile dayanıyordu.

**Çözüm.**
- `backend/tools/Bank.Evaluation`: rapordaki **bütün** tabloları üreten bir konsol aracı
  (`dotnet run --project tools/Bank.Evaluation -c Release`). Çözüme dahil olduğu için her build'de derleniyor, kod
  değiştikçe çürüyemiyor.
- Nihai değerlendirme **50 test profili** (101–150) ile yapıldı.
- [`docs/analytics/EVALUATION.md`](analytics/EVALUATION.md): veri, yöntem, önceden belirlenen kriterler, sonuçlar, ürünün iki
  sorusuna cevaplar ve sonuçların sınırları.

**#036'ya düzeltme.** 50 profille tahmin sonuçları belirgin şekilde farklı çıktı:

| | 10 profil (#036) | 50 profil |
|---|---|---|
| Seçilen yöntemin MAPE'i | %17,6 | **%12,4** |
| Kâhin | %12,0 | **%10,4** |
| Fark | 5,6 puan | **~2 puan** |
| Aralık kapsama | %97 | **%95** |

Profiller arası sapma ±%8,4 puan. 101–110 alt kümesi şanssız bir örneklemdi. #036 kaydı değiştirilmedi. Mühendislik
günlüğü, o anda ne bilindiğinin kaydıdır. Düzeltme burada yapıldı. Ders: değişkenliği yüksek ölçümlerde örneklem boyutu,
sonucun kendisi kadar önemli.

**Değişmeyen sonuçlar.** Tekrarlayan ödeme tespiti 450/450 (0 yanlış pozitif), etiketli anomaliler 133/133 (17.022 normal
işlemde 0 yanlış pozitif). Duyarlılık: varsayılan eşikte market medyanının 8 katı %80, 6 katı %50 oranında yakalanıyor.

**Commit:** `docs(research): add reproducible evaluation of the analytics`

---

## #039 — Frontend: bozuk production build'in onarımı ve temizlik

**Sprint:** 7

**Sorun.** `ng build` başından beri başarısızdı (#008'de tespit edilmişti): `app.scss exceeded maximum budget (12.79 kB > 8 kB)`.

**Kök neden.** `main.ts` `AppComponent`'i başlatıyordu, ama yanında hiç kullanılmayan ikinci bir kök bileşen (`App`: `app.ts`,
`app.html`, `app.scss`) duruyordu. Onun **bileşen** stilinde `@tailwind base/components/utilities` direktifleri vardı. Angular her
`.ts` dosyasını derlediği için Tailwind'in bütün CSS'i o bileşenin stiline gömülüyor ve bileşen stil bütçesini aşıyordu. Hata ölü
bir dosyadan geliyordu. Bütçeyi yükseltmek belirtiyi gizlerdi, nedeni ortadan kaldırmak gerekiyordu.

**Bulunan diğer kalıntılar ve yapılanlar.**

| Bulgu | Yapılan |
|---|---|
| İkinci kök bileşen ve "Hello, bank-app" bekleyen şablon testi | Silindi |
| `AuthService` ile `AuthApi` aynı işi yapıyordu. `AuthService` `{ email, password }` gönderiyordu, backend `tcNo` bekliyor | `AuthService` silindi, `AuthApi` kullanılıyor. `LoginRequest` modeli backend'e uyduruldu |
| `http://localhost:5164` dört ayrı yerde sabit kodluydu. Proxy dosyası vardı ama kullanılmıyordu | Bütün istekler göreli (`/api/...`). Dev server proxy'si `angular.json`'a bağlandı. Canlıda API aynı origin'den sunulur, yani bundle'a host adı gömülmüyor ve CORS gerekmiyor |
| Interceptor her istekte token varlığını, **guard ise token'ın kendisini** `console.log` ile basıyordu | Kaldırıldı. Tarayıcı konsolu, eklentilerin ve ekran paylaşımının görebildiği bir yer |
| Kullanılmayan `app.config.ts`, `// ...existing code...` kalıntıları | `main.ts` artık Angular'ın standart `appConfig` kalıbını kullanıyor |
| `angular.json` var olmayan `ui` ve `data-access` kütüphane projelerini tanımlıyordu. `tsconfig` paths ve Tailwind content listesi de onları gösteriyordu | Kaldırıldı |
| Tailwind v3 (kullanılan) ile `@tailwindcss/cli` v4 (kullanılmayan) ve `ng-packagr` aynı anda kuruluydu | Kullanılmayan paketler kaldırıldı |
| Proje zoneless (Angular 21 varsayılanı) ama bileşenler durumu düz alanlarda tutuyordu | `appConfig`'e not düşüldü. Yeni bileşenler signal kullanacak, eskiler sonraki commit'lerde dönüştürülecek |

**Doğrulama.** `ng build bank-app` ilk kez başarılı: başlangıç paketi 296 KB (aktarım 80 KB), hata ve uyarı yok.

**Commit:** `fix(web): repair the production build and remove dead code`

---

## #040 — Web: oturum yönetimi, süresi dolan token ve hata mesajları

**Sprint:** 7

**Sorunlar.**
1. Token'a üç ayrı yerden (interceptor, guard, login) doğrudan `localStorage` ile erişiliyordu. Oturum durumunun tek bir sahibi yoktu.
2. Token 60 dakika sonra geçersiz oluyor (#009), ama bu durum hiç ele alınmıyordu. API 401 döndüğünde kullanıcı boş veya bozuk bir
   ekranda kalıyordu.
3. Hata mesajları `err.error.message` alanından okunuyordu. API ise Sprint 0'dan beri RFC 9457 ProblemDetails dönüyor
   (`detail`, `errors`). Kullanıcı her hatada genel bir metin görüyordu.
4. Kayıt formunda `markAllAsTouched()` geçersizlik kontrolünden **sonra** çağrılıyordu. Form geçersizken fonksiyon erken döndüğü için
   kullanıcı hangi alanın hatalı olduğunu hiç görmüyordu, buton sadece "çalışmıyordu".

**Çözümler.**
- **`SessionService`:** oturumun tek sahibi, signal tabanlı. Saklama yeri değişirse (ör. HttpOnly cookie) tek bir dosya değişecek.
  Token'ın `localStorage`'da durmasının XSS riski sınıf belgesinde açıkça yazılı. Güvenlik sprint'inde (Sprint 8) ele alınacak.
- **401 yönetimi:** interceptor, token'lı bir isteğe 401 gelirse oturumu kapatıp kullanıcıyı `returnUrl` ve "oturum süresi doldu"
  mesajıyla login'e yönlendiriyor. Login'in kendi 401'i (yanlış şifre) bu kurala takılmıyor, çünkü o anda token yok.
- **Açık yönlendirme (open redirect) koruması:** girişten sonra `returnUrl`'e sadece `/` ile başlayan, `//` ile başlamayan
  uygulama içi yollar için gidiliyor. Aksi hâlde `?returnUrl=https://saldirgan.com` içeren bir bağlantı, kullanıcıyı başarılı
  girişin ardından sahte bir siteye taşıyabilirdi. Bu, oltalama saldırılarında sık kullanılan bir tekniktir.
- **`problemMessage()`:** ProblemDetails'ten kullanıcıya gösterilecek metni çıkarıyor. Sırasıyla `detail`, ilk alan hatası, son
  olarak çağıranın verdiği varsayılan metin. Ağ hatasında (status 0) "Sunucuya ulaşılamadı" gösteriliyor. 500'lerde sunucunun
  genel başlığı yerine bağlama uygun bir metin. `problemErrors()` import'un satır hatalarını ve alan hatalarını tek listeye düzleştiriyor.
- Login'de sadece alanların dolu olup olmadığı kontrol ediliyor. "TC 11 haneli olmalı" gibi format kuralları kaldırıldı (backend
  ile aynı ilke, #019).
- Bileşenler signal'e geçirildi, şablonlar yeni kontrol akışını (`@if`) kullanıyor. Hata kutularına `role="alert"` eklendi, ekran
  okuyucular hatayı duyuruyor.
- Kayıt formu `nonNullable` yapıldı. Böylece gereksiz bir tip dönüşümüne (`as ...`) ihtiyaç kalmadı.

**Doğrulama.**
- `problemMessage`/`problemErrors` için 7 Vitest testi: detail, alan hataları, ağ hatası, 500, HTTP dışı hata, import satır listesi.
- Backend ve Angular dev server birlikte çalıştırıldı. Dev server üzerinden gönderilen login isteği proxy ile backend'e ulaştı (200).
  Yanlış şifrede gelen 401'in `detail` alanı, arayüzün göstereceği metnin ta kendisi.

**Commit:** `feat(web): centralise the session and show API error details`

---

## #041 — Web: uygulama kabuğu, Türkçe format ve sahte arayüz öğelerinin kaldırılması

**Sprint:** 7

**Sorunlar.**
1. **Çalışmayan, yanıltıcı öğeler.** Yan menüde "PRO PLAN – %2 Nakit İade Alın – Şimdi Yükselt" kutusu, panelde "Para Yükle",
   "Para Gönder" butonları, işlevsiz bir "İşlem ara" kutusu ve hiçbir yere gitmeyen "Tümünü Gör" bağlantıları vardı. Olmayan bir
   kampanyayı ("%2 nakit iade") göstermek, #008'deki uydurma "%32 faiz" ile aynı sınıfta bir sorun. Çalışmayan butonlar da kullanıcı
   güvenini zedeler.
2. **Formatlar İngilizceydi.** Tutarlar `₺15,000.00`, tarihler "7 November" gibi görünüyordu. Para sembolü elle yazılıp `number`
   pipe'ı ile birleştiriliyordu.
3. **Font hiç yüklenmiyordu.** CSS `font-family: 'Inter'` diyordu ama `index.html` sadece ikon fontunu yüklüyordu. Tarayıcı sessizce
   başka bir fonta düşüyordu.
4. Çıkış yapmanın bir yolu yoktu. Mobilde menü tamamen gizleniyordu.

**Çözümler.**
- `registerLocaleData(tr)`, `LOCALE_ID = 'tr'`, `DEFAULT_CURRENCY_CODE = 'TRY'`. Bütün tutarlar artık `currency` pipe'ı ile
  biçimleniyor (`₺15.000,00`), döviz hesapları kendi para birimiyle. Tarihler Türkçe.
- Kabuk: navigasyon, sayfalar eklendikçe büyüyen tipli bir `NavItem` listesi. Kullanıcı adı ve **Çıkış** butonu eklendi. Mobilde menü
  yatay kaydırılabilir bir şerit oluyor, gizlenmiyor.
- Sahte öğeler kaldırıldı. Kart üzerindeki sabit "NEO" yazısı yerine kartın gerçek tipi (Banka kartı / Kredi kartı) gösteriliyor.
- Panel signal'lere geçirildi. Hata durumunda `problemMessage` ile anlamlı bir mesaj gösteriliyor.
- Kullanılmayan stil dosyaları (`.activeLink`, `.transition-hover`, Tailwind'in kendi sınıfını kopyalayan `.flex-shrink-0`, hiçbir yerde
  kullanılmayan `elegant-gradient-*`) silindi. Inter fontu yükleniyor, sayfa başlığı ve viewport meta etiketi düzeltildi.

**Commit:** `feat(web): add the app shell with Turkish formatting and drop non-functional UI`

---

## #042 — Web: finans API katmanı

**Sprint:** 7

**Kapsam.** Backend'in finans kontratlarının TypeScript karşılıkları (`finance.models.ts`) ve kaynak başına bir API servisi:
`CreditCardsApi`, `RecurringCashFlowsApi`, `InstallmentPlansApi`, `PlanningApi` (projeksiyon ve senaryo), `TransactionsApi`,
`AnalyticsApi`.

**Kurallar.**
- **Ekran bileşenleri `HttpClient`'ı görmez.** URL'ler, query parametreleri, multipart yükleme ve `If-Match` başlığı sadece bu katmanda.
  Bir endpoint değişirse tek bir dosya değişiyor.
- **Tarihler string olarak kalıyor (`IsoDate`).** `"2026-03-25"`'i `new Date()` ile çevirmek, tarayıcı bunu UTC gece yarısı olarak
  yorumladığı için bazı saat dilimlerinde bir önceki güne kaydırır. Takvim tarihi bir saat değil, bir gündür. Gösterim sırasında
  `DatePipe` ile biçimleniyor.
- **`If-Match` tırnaklı gönderiliyor** (`"AAAAAAAAB9Q="`). HTTP'de entity tag'leri tırnaklıdır ve backend iki biçimi de kabul ediyor (#021).

**Testler (Vitest + `HttpTestingController`).**
- güncellemede version'ın tırnaklı `If-Match` başlığı olarak gönderilmesi
- CSV'nin `file` alanında multipart olarak yüklenmesi
- sadece dolu tarih filtrelerinin query'ye eklenmesi

**Commit:** `feat(web): add typed clients for the finance API`

---

## #043 — Web: kart, taksit ve düzenli kalem ekranları

**Sprint:** 7

**Kapsam.**
- **Kartlar ve taksitler** (`/dashboard/cards`): kart ekleme, düzenleme ve silme. Taksitli alışveriş ekleme ve silme. Her planın
  ekstre ve son ödeme tarihli taksit takvimi.
- **Düzenli gelir ve giderler** (`/dashboard/recurring`): gelir, gider ve abonelik ekleme, düzenleme ve silme. Türe göre filtre
  sekmeleri.

**Kararlar.**
- **Ortak görsel dil.** `styles.scss`'e `@layer components` ile `.panel`, `.btn-primary`, `.field`, `.input`, `.notice-error` gibi
  sınıflar eklendi. Her sayfada aynı uzun Tailwind dizilerini tekrar etmek yerine görünüm tek yerden yönetiliyor.
- **Eşzamanlılık arayüzde de görünür.** Düzenleme isteği kaydın `version` değeriyle gidiyor (`If-Match`). 409 gelirse API'nin
  mesajı ("siz düzenlerken değiştirildi") gösteriliyor, form kapanıyor ve güncel veri yeniden yükleniyor. Kullanıcı eski veriyle
  tekrar denemeye zorlanmıyor.
- **Geçersiz seçenek sunulmuyor.** Tür "Gelir" seçildiğinde "Ödeme kaynağı (kart)" alanı gizleniyor. Backend bu kombinasyonu zaten
  reddediyor, arayüz kullanıcıyı o hataya hiç götürmüyor. Kart seçenekleri paranın ne zaman çıkacağını da söylüyor:
  "Hesaptan (kendi tarihinde)" / "Bonus (son ödeme gününde)".
- **Silme onayı.** Geri alınamayan işlemler (soft delete olsa da kullanıcı açısından geri alınamaz) onay istiyor. Kullanımdaki kartın
  silinememesi (409) API mesajıyla açıklanıyor.

**Yakalanan iki küçük ama gerçek hata.**
1. **UTC tarih tuzağı, frontend'de.** "Bugünün tarihi" varsayılanı için yaygın `new Date().toISOString().slice(0, 10)` kalıbı
   UTC'ye çevirir. Türkiye'de 00:00–03:00 arasında **dünü** verir (#026'daki backend sorununun ikizi). `toIsoDate()` yerel takvim
   tarihini biçimlendiriyor. Gece yarısını 30 dakika geçe durumu bir testle kilitlendi.
2. **Türkçe ekler.** İlk şablonlarda `"her ayın {{ gün }}'i"` ve `"{{ tarih }}'den"` yazmıştım. Türkçede ek sayının okunuşuna göre
   değişir: "20'si", "1'i", "2026'dan". Sabit ek her durumda yanlış sonuç üretiyordu. Ek kurallarını kodlamak yerine (KISS) ifadeler
   ek gerektirmeyecek şekilde yazıldı: "Kesim günü: 20", "Başlangıç 5 Mar 2026".

**Doğrulama ve sınırı.** Production build (strict template kontrolüyle) ve 12 Vitest testi geçiyor. Sayfalar lazy-load ile ayrı chunk'lara
bölünüyor (kart sayfası 3,5 KB, sıkıştırılmış). Geliştirme ortamında tarayıcı otomasyonu olmadığı için ekranlar **görsel olarak
doğrulanmadı**. Akışların bağlı olduğu API davranışı entegrasyon testleriyle (#024) güvence altında.

**Commit:** `feat(web): manage cards, installment plans and recurring items`

---

## #044 — Web: nakit akışı zaman çizelgesi

**Sprint:** 7

**Kapsam.** `/dashboard/cash-flow`: projeksiyon motorunun (#025) arayüzü. 3, 6 veya 12 ay seçilebiliyor.

**Bilgi tasarımı: her verinin işine göre bir form.**

| Veri | Form | Gerekçe |
|---|---|---|
| Başlangıç bakiyesi, dönemin en düşük bakiyesi, eksiye düşülen ay sayısı | Stat kartları | Tek başlık sayılarıdır, grafik gerekmez |
| Gün gün bakiye | Tek serili basamak çizgi, sıfırda kırmızı çizgi | Değişim zaman içinde. Bakiye harekete kadar sabit kaldığı için basamak çizgi. Tek seri olduğu için lejant yok, başlık adlandırıyor |
| Aylık gelir ve çıkış | Gruplu sütun, iki seri, tek eksen | Aynı birimde iki büyüklük. İkinci bir y-ekseni yok |
| Aylık özet | Tablo | Ayrıntı ve aynı zamanda grafiklerin erişilebilir karşılığı |
| Hareketler | Liste. Kart ekstresi açıldığında satırları görünüyor | "Bu 563 TL neden?" sorusunun cevabı |

**Renk.** Seriler kategorik paletin ilk iki slotu (mavi, turuncu). Renkler göz kararı değil, bir doğrulama scriptiyle seçildi:
renk körlüğü ayrımı ΔE 24,7 (eşik 8), normal görüşte ΔE 33,6, beyaz zemine karşı kontrast ≥ 3:1. Kırmızı sadece "sıfırın altı"
durumu için ayrıldı ve **hiçbir zaman tek başına kullanılmıyor**: tabloda ikon ve "Eksiye düşüyor" metniyle birlikte gösteriliyor.

**Ana sorunun arayüzdeki karşılığı.** Üçüncü stat kartı "Bakiyenin eksiye düştüğü ay" sayısını, altında "Ay sonu artıda olsa
bile ay içinde" notuyla gösteriyor. Tabloda "Ay sonu" ve "Ay içi en düşük" kolonları yan yana. Ayı artıda kapatıp ortasında eksiye
düşen kullanıcı bunu ilk bakışta görüyor (ADR-0003 §5).

**Teknik kararlar.**
- **`ChartComponent`:** ECharts için ince bir sarmalayıcı. Grafiği ilk render'dan sonra oluşturuyor (boyutlu bir eleman gerekir),
  girdi değişince seçenekleri yeniden uyguluyor, `ResizeObserver` ile kabın boyutunu izliyor ve bileşen yok edilirken grafiği temizliyor.
- **Tree-shaking:** `echarts` paketinin tamamı yerine sadece çizgi, sütun ve gerekli bileşenler içe aktarılıyor. Paket tam sürümle
  sabitlendi (`6.1.0`).
- **Paket boyutu ödünleşimi:** grafik kodu nakit akışı chunk'ını 168 KB'a (sıkıştırılmış) çıkarıyor. Sayfa lazy-load edildiği için
  başlangıç paketi 84 KB'ta kalıyor, grafik kodu sadece grafikli sayfa açılınca iniyor.
- **Gün gün bakiye istemcide hesaplanıyor** (`runningBalance`): API hareketleri zaten sıralı gönderiyor (aynı gün çıkışlar önce,
  ADR-0003 §4). Kuruşlar tamsayı olarak toplanıyor, uzun toplamlarda kayan nokta kayması olmuyor. 3 testle kilitlendi.
- Grafiklerin `aria-label`'ı içerik özetini veriyor ("En düşük bakiye −15.000 ₺, 1 Nisan 2026").

**Doğrulama.** Production build ve 15 Vitest testi geçiyor. Görsel doğrulama tarayıcı otomasyonu olmadığı için yapılamadı.

**Commit:** `feat(web): add the cash-flow timeline`

---

## #045 — Web: taksit senaryosu karşılaştırma

**Sprint:** 7

**Kapsam.** `/dashboard/scenarios`: "Bu bilgisayarı 3 yerine 6 taksitle alırsam ne olur?" sorusunun arayüzü (#027–#028).

**Akış.** Kart, açıklama ve tarih girilir. Seçenekler satır satır eklenir (en fazla 6, varsayılan 3 ve 6 taksit). **Her seçeneğin kendi
toplam fiyatı var.** Formda bunun nedeni açıklanıyor ("Satıcılar uzun vadede genellikle daha yüksek fiyat ister"). "İlk fiyatı tüm
seçeneklere kopyala" kısayolu fiyat aynıysa tekrar yazmayı önlüyor.

**Sonuç ekranı.**
- **Tablo:** "Almasam" satırı ve her seçenek için aylık taksit, toplam, en ucuz seçeneğe göre fiyat farkı, en yüksek aylık çıkış, en
  düşük bakiye ve tarihi, eksiye düşülen ay sayısı, dönem sonu bakiye.
- **Grafik:** seçeneklere göre aylık en düşük bakiye ve sıfır çizgisi.
- **Öneri yok.** Satırlar girildiği sırada, sıralama veya "en iyi" işareti yok. Altta açık bir not: "Bu karşılaştırma bir öneri
  değildir." Proje kapsamının (karar destek, tavsiye değil) arayüzdeki karşılığı.

**Renk ve erişilebilirlik.** Seçenekler doğrulanmış kategorik paletin ilk 6 slotunu sırasıyla alıyor. Doğrulama scripti zorunlu bütün
kontrollerden geçti (en kötü komşu renk körlüğü ayrımı ΔE 9,1, eşik 8). Ama 3 rengin beyaz zemine karşı kontrastı 3:1'in altında kaldı
(uyarı). Yönerge bu durumda görünür etiket veya tablo istiyor: seri kimliği tablodaki renk karesi ve metinle, lejantla, 4 veya daha az
seride çizgi sonundaki etiketle veriliyor. Baz senaryo kategorik bir renk almıyor, nötr gri. "Eksiye düşme" yine ikon ve metinle gösteriliyor.

**Paket boyutu.** İkinci grafikli sayfa gelince derleyici ECharts'ı iki sayfanın **paylaştığı** ayrı bir chunk'a taşıdı. Sayfa kodları
yaklaşık 4 KB'a indi. Grafik kodu bir kez iniyor.

**Gözden geçirmede yakalanan.** Baz satırın etiketi "Almazsam" yazılmıştı, "Almasam" olarak düzeltildi.

**Commit:** `feat(web): compare installment options before buying`

---

## #046 — Web: işlem geçmişi, CSV yükleme ve demo verisi

**Sprint:** 7

**Kapsam.** `/dashboard/transactions`: CSV ekstre yükleme, tek tuşla 12 aylık demo verisi, tarih filtreli ve sayfalı işlem listesi.

**Kararlar.**
- **Sonuç her zaman sayılarla söyleniyor:** "120 satır işlendi: 117 yeni işlem eklendi, 3 satır zaten kayıtlıydı." Mükerrer koruması
  (#031) kullanıcıya görünür. Aynı ekstreyi tekrar yüklemenin güvenli olduğu böylece anlaşılıyor.
- **Reddedilen dosyada satır listesi:** API'nin "ya hep ya hiç" cevabındaki satır hataları ("Satır 3: Tarih okunamadı") madde madde
  gösteriliyor. Kullanıcı dosyayı nerede düzelteceğini biliyor.
- **2 MB sınırı istemcide de kontrol ediliyor.** API zaten reddediyor (413), ama kullanıcı büyük bir dosyanın yüklenmesini
  bekledikten sonra hata görmesin diye.
- Dosya seçimi sıfırlanıyor. Kullanıcı dosyayı düzeltip **aynı adla** tekrar seçebiliyor (tarayıcı aynı dosya için `change` olayını
  tekrar tetiklemez).
- Kabul edilen format sayfada tek cümleyle anlatılıyor: gerekli kolonlar, ayraçlar, kodlama.

**Commit:** `feat(web): import statements and browse transaction history`

---

## #047 — Web: bileşen tabanlı yapı ve analitik paneli

**Sprint:** 7

**Karar: container / presentational ayrımı.** İlk yazılan sayfalar tek ve büyük birer şablondu: veri yükleme, durum, form ve gösterim aynı
dosyadaydı. Frontend iki katmana ayrılıyor:

| Katman | Sorumluluk | Kural |
|---|---|---|
| **Sayfa (container)** | API çağrısı, durum (signal), kullanıcı eylemlerinin sonucu | Görsel ayrıntı az |
| **Sunum bileşeni** | Sadece gösterim | Sadece `input()` alır, `output()` ile olay bildirir, API'yi bilmez, `OnPush` |
| **Ortak UI** (`shared/ui`) | Bütün sayfalarda aynı parçalar | `PageHeader`, `Notice`, `StatTile`, `EmptyState` |

**Neden.**
- **Tek sorumluluk:** bir bileşen ya veri yönetir ya gösterir.
- **Test edilebilirlik:** sunum bileşenleri API veya mock servis olmadan, sadece girdi verilerek test ediliyor
  (`RecurringPaymentsComponent`: takipteki ödemeye buton gösterilmiyor, butona basınca doğru ödeme bildiriliyor).
- **Tasarım değişikliğine dayanıklılık:** görsel tasarım daha sonra Google Stitch'te yapılacak. Parçalara ayrılmış ekranlarda yeni tasarım
  bileşen bileşen uygulanabilir, veri ve davranış kodu etkilenmez.
- **Tutarlılık:** hata kutusu, boş durum ve stat kartı her sayfada aynı bileşen. `StatTile` kritik değeri her zaman ikonla da gösteriyor
  (renk tek başına anlam taşımıyor). Bu bir testle kilitlendi.

**Analitik paneli** (`/dashboard/analytics`) bu yapıyla yazıldı. Container dört analizi yüklüyor, dört sunum bileşeni gösteriyor:
- `CategoryTrends`: kategori toplamları (tek serili yatay sütun) ve aylık tablo.
- `RecurringPayments`: tespit edilen düzenli ödemeler. Takipte olmayanlar için **"Takibe al"**: tek tıkla düzenli kalem oluşturuluyor
  (sabit tutarlı → abonelik, değişken → gider, gelen → gelir) ve projeksiyona giriyor. Unutulmuş abonelik böylece nakit akışında
  görünüyor. Analiz ile planlama arasındaki köprü bu.
- `Anomalies`: "Tüm harcamalar için tipik tutarın (612 ₺) 24 katı" gibi açıklamalarla.
- `SpendingForecast`: tahmin **aralık olarak** gösteriliyor ("14.800 – 21.300 ₺"). Değerlendirmenin sonucu (#038) arayüze yansıdı: basit
  yöntemler aralıkta güvenilir, tek sayıda orta düzeyde. Yöntemlerin geçmiş isabeti katlanabilir bir tabloda.

**Bölümler bağımsız yükleniyor.** 6 aydan az geçmiş varsa tahmin 400 döner (#037). Bu durumda sadece tahmin bölümü bilgi notu gösteriyor,
diğer üç analiz çalışmaya devam ediyor.

**Doğrulama.** Production build. 19 Vitest testi (4 yeni bileşen testi dahil).

**Commit:** `feat(web): add the analytics page as composed components`

---

## #048 — Bütün sayfaları bileşenlere ayırdım

**Sprint:** 7

Analitik panelini container/presentational düzeniyle yazdıktan sonra (#047) diğer sayfalar göze batmaya başladı: her biri tek bir
dosyada form, liste, durum yönetimi ve API çağrısını birlikte taşıyordu. Kart sayfasının şablonu 150 satırı geçmişti. Tasarım da sonradan
Stitch'te yapılacağı için bu dağınıklığı şimdi toplamak mantıklıydı. Yeni tasarım geldiğinde onu parça parça uygulayabilmek istiyorum.

Ayırdığım parçalar:

| Sayfa | Bileşenler |
|---|---|
| Nakit akışı | `ProjectionSummary`, `BalanceChart`, `MonthlyFlowChart`, `MonthlySummaryTable`, `ProjectionMovements` |
| Taksit senaryosu | `ScenarioForm`, `ScenarioComparisonTable`, `ScenarioChart` |
| Kartlar | `CardForm`, `CardList`, `InstallmentPlanForm`, `InstallmentPlanList` |
| Düzenli kalemler | `RecurringItemForm`, `RecurringItemList` |
| İşlem geçmişi | `StatementUpload`, `TransactionList` |

Her sayfada kural aynı: sayfa API'yi çağırıp durumu tutuyor, alt bileşenler `input()` ile veri alıp `output()` ile olay bildiriyor.
Formlar kendi `FormGroup`'larını yönetip hazır isteği yukarı gönderiyor. 409 sonrası yeniden yükleme gibi davranışlar sayfada kaldı,
çünkü bunlar veriyle ilgili kararlar.

Yol üstünde birkaç küçük şeyi de topladım:
- Ay adlarını üç farklı yerde ayrı `Intl.DateTimeFormat` ile üretiyordum. Hepsi `formatMonth()`'a geçti.
- Senaryodaki "fiyat farkı en ucuz seçeneğe göre" ve "seçenekler sıralanmaz" kuralları bileşenin içinde kayboluyordu.
  `toComparisonRows()` diye düz bir fonksiyona çıktı ve artık kendi testi var.
- "3 ayda bir", "Her hafta" gibi ifadeler form ve listede tekrar ediyordu. `recurring-labels.ts`'e taşındı.

Ekranların görünüşü değişmedi, sadece içleri düzenlendi. Build ve 25 frontend testi geçiyor.

**Commit'ler:** `refactor(web): split the cash-flow page…`, `…break the scenario page…`, `…split cards page…`,
`…split recurring items page…`, `refactor(web): split transactions page into upload and list`

---

## #049 — Giriş denemelerine sınır ve güvenlik başlıkları

**Sprint:** 8

Sprint 8'e en çok canımı sıkan açıkla başladım: login ucu, bir saldırganın şifre ya da TC listesi denemesine hiçbir sınır koymuyordu.
#006'da "bilinmeyen TC" ile "yanlış şifre" aynı cevabı veriyor hale gelmişti, bu yüzden kimin kayıtlı olduğu sızmıyor. Ama aynı
kullanıcı için dakikada binlerce şifre denemek hâlâ mümkündü.

**Rate limiting.** .NET'in kendi `RateLimiter`'ı ile `/api/auth` altındaki uçlara IP başına sabit pencereli bir sınır koydum
(varsayılan dakikada 10 deneme, ikisi de `appsettings` üzerinden ayarlanabiliyor). Sınır aşılınca 429, `Retry-After` başlığı ve
diğer hatalarla aynı biçimde Türkçe bir ProblemDetails dönüyor. Ek paket gerekmedi.

Bir not düştüm, çünkü canlıda kolayca atlanabilecek bir ayrıntı: bölümleme `RemoteIpAddress`'e göre. Uygulama bir reverse proxy'nin
arkasına konursa bütün istekler proxy'nin adresinden geliyormuş gibi görünür ve tek bir kişi herkesi kilitleyebilir. O durumda önce
forwarded header'ların açılması gerekiyor. Bu, sınıfın belgesinde yazılı.

**Testlerde bir pürüz.** Entegrasyon testleri onlarca kullanıcıyı aynı "adresten" saniyeler içinde kaydediyor. Limit açıldığında
hepsi 429'a takılırdı. Ana test host'unda limiti çok yüksek tuttum. Limiti test eden sınıf ise `WithWebHostBuilder` ile limiti 3 olan
**ayrı bir host** açıyor: 3 deneme 401, 4. deneme 429. Testler birbirini etkilemiyor.

**Güvenlik başlıkları.** Kişisel finans verisi dönen bir API için temel set:
- `Cache-Control: no-store`: bakiye ve işlemler ortak bir bilgisayarda, çıkış yaptıktan sonra tarayıcı önbelleğinde kalmasın.
- `X-Content-Type-Options: nosniff`: tarayıcı JSON cevabı başka bir türmüş gibi yorumlamasın.
- `X-Frame-Options: DENY` ve `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`: API'nin döndüğü hiçbir şey bir
  çerçeveye gömülmek ya da script çalıştırmak için değil. Sadece geliştirmede açık olan Swagger script'e ihtiyaç duyduğu için CSP'nin
  dışında.
- `Referrer-Policy: no-referrer`: URL'lerdeki id'ler başka sitelere sızmasın.

Başlıklar `OnStarting` ile ekleniyor, yani 401 gibi hata cevaplarında da var. Bu da bir testle kontrol ediliyor.

**Commit:** `feat(security): throttle auth endpoints and add security headers`

---

## #050 — Giriş denemeleri artık kayıt altında, ama TC maskeli

**Sprint:** 8

Rate limit tek başına yetmiyor. Biri sınırın hemen altında, yavaş yavaş deniyorsa bunu ancak kayıtlara bakarak görebilirsiniz. Şimdiye
kadar başarısız bir giriş hiçbir iz bırakmıyordu.

`AuthService` artık her denemeyi yapılandırılmış bir güvenlik olayı olarak log'luyor: başarılı giriş, bilinmeyen TC ile deneme, yanlış
şifre, kayıt ve zaten kayıtlı TC ile tekrar kayıt girişimi. İstemciye hâlâ tek ve aynı cevap gidiyor (#006). Log ise başarısızlığın gerçek
nedenini tutuyor, çünkü "bu TC'ye yüz kere yanlış şifre girildi" ile "yüz farklı TC deneniyor" çok farklı iki saldırı.

Buradaki asıl karar şuydu: log'a ne yazılmalı? Log'ları veritabanından çok daha fazla kişi ve sistem görüyor: operasyon ekibi, log
toplama servisleri, destek. TC kimlik numarası KVKK kapsamında kişisel veri. Log'a açık yazılırsa, veritabanında ne kadar
korunduğunun bir önemi kalmaz. `PersonalData.MaskTcKimlikNo` numaranın sadece ilk üç ve son iki hanesini bırakıyor (`100******46`):
olayları birbirine bağlamaya yetiyor, kişiyi tanımlamaya yetmiyor. Başarılı girişlerde zaten kullanıcı id'si var, orada TC hiç yazılmıyor.

Test tarafında bir `RecordingLogger` yazdım. Log'a gerçekte ne düşeceğini yakalıyor. Test üç olayın log'landığını, nedenlerin doğru
yazıldığını ve **hiçbir satırda tam TC geçmediğini** kontrol ediyor. Bu son kontrol, ileride biri hata ayıklarken
`req.TcNo`'yu log'a eklerse test kırılsın diye var.

**Commit:** `feat(security): log sign-in attempts with masked identity numbers`

---

## #051 — Token'dan e-posta çıktı

**Sprint:** 8

JWT'ler imzalanır ama şifrelenmez. Token'ın ortasındaki parça düz base64url: token'ı gören herkes, örneğin header'ları log'layan bir
proxy, tarayıcı eklentisi ya da bir hata ekranı, içini okuyabilir. Bizim token'ımız kullanıcının e-posta adresini iki ayrı claim
olarak taşıyordu. Baktığımda API'nin bu bilgiye hiç ihtiyacı yoktu: `ICurrentUser.Email` hiçbir yerde okunmuyordu.

KVKK'nın veri minimizasyonu ilkesi tam olarak bu durum için var. Token artık sadece kullanıcı id'sini taşıyor. `ITokenGenerator`
imzası `Generate(userId)` oldu ve kullanılmayan `Email` özelliği `ICurrentUser`'dan silindi. Ad soyad zaten token'da değil, login
cevabında ayrı geliyor.

Bir entegrasyon testi gerçek bir kayıt yapıp dönen token'ın payload'ını çözüyor ve içinde e-posta, TC ve ad olmadığını doğruluyor.
İleride biri "şunu da token'a koyalım" derse test hatırlatacak.

**Commit:** `refactor(security): keep personal data out of the access token`

---

## #052 — CI pipeline'ı

**Sprint:** 8

Şimdiye kadar "build temiz, testler geçiyor" demem benim makinemde çalıştırdığım komutlara dayanıyordu. Bunun bir kurala dönüşmesi için
`.github/workflows/ci.yml` ekledim:

- **Backend:** Release build `-warnaserror` ile, yani tek bir uyarı bile build'i kırıyor. Sprint 0'dan beri koruduğum sıfır uyarı
  standardı artık bir kontrol. Ardından `dotnet list package --vulnerable` ile bilinen açık taraması (#011'deki gibi bir şey tekrar
  sızarsa yakalansın diye), unit testler ve **gerçek bir SQL Server container'ına karşı** entegrasyon testleri. Bağlantı, testlerin
  zaten desteklediği `INTEGRATION_TESTS_DB` değişkeniyle veriliyor (#024).
- **Frontend:** `npm ci`, production build ve Vitest testleri.

Container'ın `sa` şifresi workflow dosyasında açık duruyor. Bu bilinçli: container sadece o job süresince yaşayan, dışarıya kapalı
geçici bir veritabanı. Korunacak bir sır değil ve dosyada da bu şekilde not edildi.

**Doğrulama ve sınırı.** Backend adımlarının aynısını yerelde çalıştırdım: Release build ve `-warnaserror` 0 uyarı, açık taraması temiz.
YAML sözdizimi doğrulandı. Repo henüz bir uzak sunucuya push edilmediği için workflow **GitHub üzerinde hiç koşmadı**. İlk push'ta
kontrol edilmesi gereken bir şey: SQL Server container'ının sağlık kontrolü (`mssql-tools18` yolu imaj sürümüne göre değişebilir).

**Commit:** `ci: build and test backend and frontend on every push`

---

## #053 — Health check uçları

**Sprint:** 8

Canlıya çıkacak her servis, onu çalıştıran ortama "ayaktayım" ve "trafik alabilirim" diyebilmeli. Bu ikisi farklı sorular ve
karıştırılmaları gerçek bir operasyon hatasına yol açıyor:

- `/health/live`: süreç ayakta mı? Hiçbir bağımlılığa bakmıyor. Veritabanı geçici olarak gitti diye container'ın yeniden
  başlatılmasını istemiyoruz. Yeniden başlatmak sorunu çözmez, sadece yükü artırır.
- `/health/ready`: trafik alabilir mi? Veritabanına gerçekten bağlanıp `SELECT 1` çalıştırıyor. Bağlanamazsa 503 ve sadece
  "Unhealthy" yazıyor. Bağlantı ayrıntısı veya hata mesajı dışarı sızmıyor, o bilgi sunucu log'unda kalıyor.

İkisi de kimlik doğrulamasız, çünkü onları çağıran şey bir load balancer, kullanıcı değil.

Testte en çok önemsediğim senaryo, readiness'in **gerçekten** başarısız olabildiğini görmekti. Her zaman "Healthy" dönen bir health
check, hiç olmamasından daha kötü, çünkü yanlış bir güven veriyor. Ayrı bir host'a hiçbir şeyin dinlemediği bir bağlantı dizesi verdim:
ready 503 döndü, live 200'de kaldı.

**Commit:** `feat(ops): add liveness and readiness health checks`

---

## #054 — Değişiklik geçmişi: temporal tablolar

**Sprint:** 8

Soft delete (#020) silinen kaydı koruyordu, ama güncellenen bir kaydın eski hâli kayboluyordu. Kartın limiti 50.000'den 75.000'e
çıkınca 50.000 hiçbir yerde kalmıyordu. Bir bankada "bu kaydı ne zaman, neyden neye değiştirdi?" sorusu bir denetim sorusudur ve
cevapsız kalmamalı.

İlk aklıma gelen, her repository metodunun yanına bir "audit tablosuna yaz" satırı eklemekti. Bundan vazgeçtim: bu tür kod, yeni bir
güncelleme metodu yazıldığında en kolay unutulan satırdır. Ayrıca aynı transaction'da yazılmazsa audit kaydı ile asıl veri birbirinden
kopabilir.

Bunun yerine SQL Server'ın **system-versioned temporal table** özelliğini kullandım (`V004`). Kart, düzenli kalem ve taksit planı
tablolarında her `UPDATE` ve `DELETE`'te eski satırı veritabanı kendisi, aynı transaction içinde geçmiş tablosuna kopyalıyor. Soft
delete de bir güncelleme olduğu için o da kayda giriyor. Uygulama kodunda tek satır değişmedi. Dönem kolonları `HIDDEN`, bu yüzden
mevcut sorgular hiç etkilenmedi.

Migration'ı önce içinde veri olan geliştirme veritabanına uyguladım: mevcut satırlar varsayılan değerlerle sorunsuz dönüştü.
İki entegrasyon testi ekledim:
- Kart oluşturuldu, limiti değiştirildi, silindi. Geçmiş tablosunda iki eski sürüm duruyor: 50.000 ve 75.000.
- `FOR SYSTEM_TIME AS OF` ile değişiklikten önceki an sorgulandı. Kart o anda hâlâ 50.000 limitliydi.

İşlem geçmişi tablosu (`finance.Transactions`) bilerek temporal yapılmadı: o satırlar ekstreden gelen değişmez kayıtlar, hiç
güncellenmiyorlar.

**Commit:** `feat(db): keep a full change history of user finance data`

---

## #055 — "Verilerimi indir": KVKK erişim hakkı

**Sprint:** 8

KVKK'nın 11. maddesi kişiye, hakkında hangi verilerin işlendiğini öğrenme hakkı veriyor. Bir bankada bu talep çoğu zaman bir dilekçe ve
haftalarca süren bir süreç demek. Burada tek bir istekle karşılanabiliyor: `GET /api/me/data-export` sistemin kullanıcı hakkında tuttuğu
her şeyi tek bir JSON dosyası olarak indiriyor: profil, kartlar, düzenli kalemler, taksit planları ve bütün işlem geçmişi.

Birkaç küçük karar:
- **Mevcut servisler yeniden kullanıldı.** Dışa aktarım, API'nin zaten döndüğü biçimleri (`CreditCardResponse` vb.) kullanıyor. Ayrı
  bir eşleme kodu yok ve dışa aktarılan veri ekranda görülenle bire bir aynı.
- **Tam TC dosyada var.** Log'larda maskeliyim (#050), ama burada istenen tam olarak kişinin kendi verisi ve kimliği doğrulanmış bir
  kanaldan kendisine gidiyor. Maskelemek hakkın özünü boşaltırdı.
- **Her dışa aktarım log'lanıyor.** Veri sistemden çıkıyor. Kimin ne zaman aldığı bilinmeli.
- Tarih aralığı için `1900-01-01` alt sınırı: `DateOnly.MinValue` SQL'e `datetime` olarak gittiğinde (alt sınır 1753) hata verirdi.
  Bunu yazarken fark ettim ve kodda not olarak bıraktım.

Üç entegrasyon testi var: kendi kartı ve içe aktarılmış işlemi dosyada, cevap `attachment` olarak geliyor, **başka bir kullanıcının
dışa aktarımında bu verilerin hiçbiri yok** ve giriş yapmadan 401 dönüyor.

Erişim hakkının kardeşi olan silme hakkı (unutulma hakkı) henüz yok. Soft delete'li ve temporal tablolu bir yapıda "silme"nin ne
anlama geleceği (anonimleştirme mi, fiziksel silme mi, geçmiş tablolar ne olacak) ayrı bir karar gerektiriyor.

**Commit:** `feat(privacy): let users export all their personal data`

---

## #056 — Silme hakkı: önce karar, sonra kod

**Sprint:** 8

#055'in sonunda ertelediğim soruya döndüm. Silme hakkını bir `DELETE /api/me` ucuyla karşılamak kolay görünüyordu, ama sistemin iki
özelliği buna izin vermiyor. Soft delete yüzünden "silinen" kayıt yerinde duruyor. Temporal tablolar yüzünden de kaydın her eski hâli
geçmiş tablosunda. Versioning açıkken SQL Server geçmiş tablosundaki satırları silmeye izin vermiyor, yani uygulama kendi yetkisiyle bu
veriyi silemiyor bile.

Verileri kimin ürettiğine göre ayırınca karar netleşti ([ADR-0005](adr/0005-right-to-erasure.md)):
- Kullanıcının kendi girdiği finans verisini saklamak için bir yükümlülük yok. Bu veri geçmişiyle birlikte, ayrı yetkili bir bakım
  prosedüründe fiziksel olarak silinecek.
- Bankacılık çekirdeğindeki mali kayıtlar yasal süre boyunca saklanmak zorunda. Orada silme yerine kişisel alanlar anonimleştirilecek.

Her şeyi anonimleştirmek daha basit olurdu, ama "Kira – Kadıköy" gibi bir açıklama anonimleştirildikten sonra da kişiyi tanımlayabilir.
ADR'yi "Önerildi" durumunda bıraktım. Saklama süreleri hukuki bir konu ve onları tahmin edip koda gömmek istemedim.

**Commit:** `docs(adr): propose how to handle the right to erasure`

---

## #057 — Servislerden command/query handler'larına

**Sprint:** 9

Para transferini yazmaya başlamadan önce application katmanının şeklini değiştirdim. Transfer; doğrulama, idempotency ve transaction
isteyen ilk gerçek yazma işlemi olacak. Bu işleri servis servis elle tekrar etmek yerine tek bir boru hattında toplamak istedim
([ADR-0006](adr/0006-cqrs-with-mediatr.md)).

Ne değişti:
- Her uç artık tek bir command ya da query'ye karşılık geliyor. `Features/<Alan>/<Senaryo>.cs` dosyasında istek kaydı ve handler'ı
  birlikte duruyor. Controller'lar sadece HTTP'yi biliyor ve isteği `ISender`'a veriyor. On servis sınıfı ve arayüzleri kalktı.
- Pipeline'da iki behavior var. `ValidationBehavior` FluentValidation validator'larını handler'dan önce çalıştırıyor.
  `LoggingBehavior` her isteğin süresini ölçüyor ve 500 ms'yi aşanları uyarı olarak logluyor. Log'a isteğin içeriği değil, sadece tip
  adı yazılıyor, çünkü login command'ı şifre taşıyor.
- Controller'lardaki `[Range]` attribute'ları (projeksiyon süresi, sayfa boyutu, analiz süreleri) ve serviste kalan "başlangıç tarihi
  bitiş tarihinden sonra olamaz" kontrolü validator'lara taşındı. İstek gövdesinin şekli ise data annotation'larla HTTP kenarında
  kalıyor, aynı kural iki yerde yazılmıyor.
- Validator hatası, ASP.NET'in kendi model doğrulamasıyla aynı `errors` biçiminde dönüyor. Web istemcisinde tek satır değişmedi.

Birkaç küçük karar:
- **MediatR 12.5.0'da sabit.** 13'ten sonraki sürümler ticari lisans / RPL-1.5'e geçmiş. NuGet önbelleğindeki 14.2.0'ın lisans dosyasını
  okuyunca fark ettim. 12.5.0, Apache-2.0 ile yayınlanan son sürüm.
- **Handler'lar birbirini çağırmıyor.** KVKK dışa aktarımı eskiden üç servisi çağırıyordu. Şimdi aynı eşlemeleri (`CreditCardMapping`
  vb.) doğrudan kullanıyor. Handler'dan handler çağırmak, pipeline'ı iç içe ikinci kez çalıştırır ve bağımlılıkları gizler.
- **Bir isim çakışması.** Dışa aktarım için ilk açtığım `Features.PersonalData` namespace'i, TC maskeleyen `PersonalData` sınıfını
  gölgeledi: C#'ta kapsayan namespace'teki isim, `using` ile gelen isimden önce bulunuyor. Klasör `Features.Privacy` oldu.
- MediatR 12.5'te `next` delegesi bir `CancellationToken` alıyor. Behavior'lar artık isteğin token'ını iletiyor. Bunu, delegeyi
  parametresiz çağıran testi yazarken derleyici hatasından öğrendim.

Refactor sırasında 55 HTTP entegrasyon testi hiç değişmedi ve her adımda yeşil kaldı. Değişen tek test sınıfı, `AuthService`'i doğrudan
kullanan unit testti (`AuthCommandTests` oldu). Yeni testler: geçersiz bir isteğin handler'a hiç ulaşmadığı ve hata biçimi.

**Commit'ler:** `feat(application): add a MediatR pipeline with validation and timing`, `refactor(auth): ...`, `refactor(finance): ...`,
`refactor(planning): ...`, `refactor(history): ...`, `refactor(analytics): ...`

---

## #058 — Para transferi: idempotency, kilitler ve çift taraflı kayıt

**Sprint:** 9

Bankacılık tarafının en önemli eksiği buydu: hesaplar vardı ama aralarında para hareket edemiyordu. Transferi yazarken üç soruya
cevap aradım.

**1. İstek iki kez gelirse ne olur?** Mobil uygulama "Gönder"e basıldıktan sonra cevabı alamazsa isteği tekrarlar. Sunucu ilkini
işlemişse para iki kez gider. Çözüm `Idempotency-Key` başlığı. İstemci her transfer için bir anahtar üretiyor ve tekrarlarda aynısını
gönderiyor. `payments.Transfers` tablosunda `(UserId, IdempotencyKey)` benzersiz. Aynı anahtarla gelen ikinci istek, yeni bir transfer
yerine ilkinin sonucunu `Idempotent-Replayed: true` başlığıyla alıyor. Anahtarın yanında isteğin SHA-256 özeti de saklanıyor. Aynı
anahtar farklı bir tutarla gelirse 422 dönüyor. Eski sonucu döndürmek, yeni isteğin hiç çalışmadığını gizlerdi. Özet hesaplanırken
alıcı adı katlanıyor ve tutar sabit biçimde yazılıyor, böylece "ayse yilmaz" ile "AYŞE YILMAZ" aynı istek sayılıyor.

**2. Aynı bakiyeye iki istek aynı anda gelirse?** 100 TL'lik hesaptan aynı anda gelen iki 80 TL'lik transfer, kilit olmadan ikisi de
"bakiye yeterli" görüp geçebilir. Hesap satırları transaction boyunca `UPDLOCK` ile kilitleniyor. İkinci istek, birincinin commit'ini
bekliyor ve güncel bakiyeyi görüyor. Idempotency anahtarı da `UPDLOCK, HOLDLOCK` ile kilitleniyor. `HOLDLOCK` satır henüz yokken
de anahtar aralığını kilitliyor, böylece aynı anahtarla eşzamanlı gelen tekrarlar birbirini bekliyor.

**3. A→B ve B→A aynı anda olursa?** İki transfer hesapları ters sırada kilitlerse birbirini sonsuza kadar bekler (deadlock). Hesaplar
her zaman id'ye göre artan sırada, tek tek kilitleniyor.

Testler bu üç durumu gerçekten yarıştırıyor: 100 TL'ye 30'ar TL'lik on paralel transfer (tam üçü geçmeli), on çift ters yönlü paralel
transfer (hepsi geçmeli, toplam korunmalı) ve aynı anahtarla beş eşzamanlı tekrar (para bir kez gitmeli). Testlerin gerçekten koruduğunu
görmek için kilit ipuçlarını koddan bilerek sildim: üç test de kırıldı.

Kayıt tarafında her transfer `payments.LedgerEntries`'e bir borç ve bir alacak olarak yazılıyor, her satırda işlem sonrası bakiye var.
Veritabanı her transfer için tek borç ve tek alacak olmasını, bakiyenin hiçbir zaman eksiye düşmemesini constraint olarak da koruyor.
Domain tarafında `FundsTransfer` iki hesabı da kontrol etmeden hiçbir bakiyeyi değiştirmiyor. İlk sürümde alıcı hesap dondurulmuşsa,
gönderen bellekte borçlandırıldıktan sonra hata atılıyordu. Bunu birim testi yakaladı.

Bir de alıcı adı kontrolü var: FAST ve EFT'de olduğu gibi yazılan ad hesap sahibiyle eşleşmezse transfer reddediliyor. Mod-97'yi geçen
ama yanlış kişiye ait bir IBAN'a karşı son kontrol bu. IBAN'ın kendisi ise hiçbir hesaba bakılmadan, kontrol basamaklarıyla doğrulanıyor.

Transaction, `ITransactionalCommand` ile işaretlenen command'lar için MediatR pipeline'ında açılıyor. Handler normal biterse commit
ediliyor, herhangi bir exception'da geri alınıyor. Repository'ler açık transaction'ı istek kapsamındaki `DbSession`'dan okuyor.

**Commit'ler:** `feat(domain): model accounts, Turkish IBANs and transfer rules`, `build(db): add transfers and a double-entry ledger`,
`feat(application): run marked commands inside one database transaction`, `feat(payments): open accounts and transfer money between them`

---

## #059 — Kart ayarları: diğer branch'ten uyarlama

**Sprint:** 9

GitHub'daki Eylül çizgisinde kartlar için temassız/internet kullanımı ayarı vardı. Kodu doğrudan almak yerine bu mimariye uyarladım.
Oradaki `Card` entity'si kuralları setter'larla değiştiriyordu (`ToggleContactless`). Burada `BankCard` domain nesnesi kuralları kendisi
koruyor: iptal edilmiş kart değiştirilemez ve geri açılamaz, aylık limit günlük limitten küçük olamaz. Bloke etme ve bloke kaldırma da
eklendi, ikisi de güvenlik olayı olarak loglanıyor.

O entity'nin bilerek almadığım bir kısmı da vardı: tam kart numarası ve CVV tutuyordu. PCI DSS, CVV'nin yetkilendirmeden sonra hiçbir
şekilde saklanmamasını şart koşuyor. Bu projede baştan beri sadece son dört hane tutuluyor.

**Commit:** `feat(cards): let customers change card settings and block a card`

---

## #060 — Transactional outbox ve RabbitMQ

**Sprint:** 9

Transfer olduğunda başka sistemlerin (bildirim, dolandırıcılık izleme) haberdar olması gerekiyor. İlk akla gelen, transferden sonra
mesajı doğrudan RabbitMQ'ya göndermek. Ama bunun iki kötü sonu var: transfer commit edilir, broker o an çalışmıyordur ve olay kaybolur.
Ya da mesaj gider, ardından transaction geri alınır ve olmamış bir transfer duyurulmuş olur.

Outbox ile olay, transferle aynı transaction'da `messaging.OutboxMessages` tablosuna yazılıyor. Bir arka plan servisi bekleyen satırları
sırayla RabbitMQ'ya iletiyor. Testler, reddedilen bir transferin olay bırakmadığını ve tekrarlanan isteğin ikinci bir olay üretmediğini
doğruluyor.

Kararlar:
- **En az bir kez teslim.** Yayıncı mesajı gönderip satırı işaretlemeden önce durursa mesaj tekrar gider. Tüketiciler `MessageId` ile
  tekrarları ayıklamalı. Tam olarak bir kez teslim, broker ile veritabanı arasında dağıtık transaction ister ve buna değmez.
- **Publisher confirms.** Mesaj ancak broker onayladıktan sonra gönderilmiş sayılıyor.
- **Sıra korunuyor.** Bir mesaj gönderilemezse batch orada duruyor. Sonraki olayları önce göndermek sıralarını bozardı.
- **Birden fazla API örneği.** Satırlar `UPDLOCK, READPAST` ile alınıyor: iki yayıncı aynı satırı göndermek yerine işi paylaşıyor.
- **Olayda kişisel veri yok.** `TransferCompleted` sadece id ve tutar taşıyor. Ad veya IBAN'a ihtiyaç duyan tüketici onu kendisi istiyor,
  böylece kişisel veri her kuyruğa kopyalanmıyor.

Bu makinede Docker olmadığı için gerçek bir broker'la deneyemedim. Yayıncının testi `RABBITMQ_HOST` tanımlıysa çalışıyor. CI'da bir
RabbitMQ container'ı başlatılıyor, ilk push'ta orada koşacak. Outbox'ın geri kalanı (yazma, sıra, hata sonrası tekrar) sahte bir
yayıncıyla gerçek SQL Server'a karşı test ediliyor.

**Commit'ler:** `build(db): add a transactional outbox table`, `feat(messaging): publish transfer events to RabbitMQ through the outbox`

---

## #061 — Yapısal log ve correlation id

**Sprint:** 9

Bir kullanıcı "transferim hata verdi" dediğinde, o isteğin log satırlarını bulmanın bir yolu yoktu. Artık her istek bir
`X-Correlation-Id` alıyor. Bu id isteğin ürettiği her log satırında, cevap başlığında ve hata cevabının gövdesinde (`correlationId`) var.
Kullanıcının hata ekranında gördüğü id ile destek ekibi doğrudan ilgili satırlara gidebilir.

Çağıran taraf (bir gateway ya da mobil uygulama) kendi id'sini gönderirse korunuyor, böylece bir istek birden fazla sistemde aynı id ile
izlenebiliyor. Ama sadece bir id'ye benziyorsa: harf, rakam ve `._-`, 8–64 karakter. Başka her şey yenisiyle değiştiriliyor, aksi
hâlde başlığa yazılan metin log'a enjekte edilebilirdi.

Log'lar Serilog ile production'da satır başına bir JSON nesnesi olarak yazılıyor. Böylece bir log deposunda `CorrelationId` ya da
`TransferId` alanına göre aranabiliyor. Development'ta okunabilir metin olarak kalıyor.

**Commit:** `feat(api): structured logging with a correlation id per request`

---

## #062 — Docker Compose ve proxy arkasında rate limit

**Sprint:** 9

`docker compose up --build` SQL Server, RabbitMQ, migration'ları çalıştıran tek seferlik bir container, API ve nginx arkasındaki web
uygulamasını ayağa kaldırıyor. API'nin dışarıya açık portu yok, migration'lar başarıyla bitmeden başlamıyor. Gizli değerler commit
edilmeyen bir `.env` dosyasından geliyor (`.env.example` hangilerinin gerektiğini listeliyor).

Compose dosyasını yazarken bir hata yakaladım: nginx arkasında her istek proxy'nin adresinden geliyor. IP başına çalışan giriş sınırı
(#049) bütün kullanıcıları tek kişi sayardı ve on başarısız denemeden sonra herkes kilitlenirdi. API artık `ReverseProxy:Enabled`
açıkken istemci adresini `X-Forwarded-For`'dan okuyor. Ayar varsayılan olarak kapalı, çünkü API'ye doğrudan erişilebilen bir ortamda
bu başlığa güvenmek, saldırganın kendi rate limit kovasını seçmesine izin verirdi. Compose'da açık, çünkü orada API'ye sadece nginx
ulaşabiliyor.

Docker bu makinede kurulu olmadığı için compose'u çalıştıramadım. CI her push'ta image'ları build ediyor, bozuk bir Dockerfile orada
ortaya çıkacak. Sistemin bir bütün olarak container'larda ayağa kalktığını henüz görmedim.

**Commit'ler:** `feat(api): read the client address from a trusted reverse proxy`, `build: run the whole system with docker compose`
