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
