# ADR-0001: Oracle şemasını versiyonlu migration scriptleriyle yönetmek

Durum: Yerine geçildi → [ADR-0002](0002-sql-server-instead-of-oracle.md)

## Bağlam

Uygulama Oracle üzerinde PL/SQL paketleri (`PKG_AUTH`, `PKG_DASHBOARD` …) ve Dapper ile çalışıyor.
Ancak tablo ve paket tanımları repoda yok, sadece geliştiricinin lokal veritabanında duruyor. Bunun sonuçları:

- Proje başka bir makinede ayağa kaldırılamıyor.
- Şemada neyin, ne zaman, neden değiştiği izlenemiyor.
- Kod ile şema arasındaki uyum garanti değil: bir commit'e geri dönmek, o commit'in beklediği şemayı geri getirmiyor.

"Oracle mı, migration mı?" bir seçim değil. Oracle veritabanı, migration ise şemanın
versiyonlanma yöntemi. Karar verilmesi gereken soru, Oracle şemasının nasıl versiyonlanacağı.

## Seçenekler

| Seçenek | Artı | Eksi |
|---|---|---|
| DbUp (+ `dbup-oracle`) | .NET-native, saf SQL scriptleri, sıfır yeni runtime, öğrenmesi kolay | Rollback scriptleri elle yazılır |
| Liquibase | Bankalarda çok yaygın, rollback desteği | Java runtime gerektirir, XML/YAML changelog yükü |
| Flyway | Yaygın, sade, SQL tabanlı | Java runtime; bazı Oracle özellikleri ücretli sürümde |
| EF Core Migrations | Otomatik üretim | PL/SQL paket mimarisiyle uyumsuz, ORM'e geçiş gerektirir |
| Elle script çalıştırmak | Araç yok | Hangi scriptin çalıştığı bilinmez, mevcut sorunun ta kendisi |

## Karar

DbUp kullanılacak (KISS: mevcut stack içinde kalıyor, scriptler saf Oracle SQL/PL-SQL olarak kalıyor).

- Scriptler `database/migrations/` altında `V001__create_users.sql` biçiminde numaralanır.
- Bir kez çalışmış script asla değiştirilmez. Düzeltme her zaman yeni bir script olarak gelir (append-only).
- PL/SQL paketleri `CREATE OR REPLACE` olduğu için `database/packages/` altında tutulur ve her çalıştırmada yeniden derlenir (idempotent).
- Çalışan scriptler DbUp'ın `SCHEMAVERSIONS` tablosunda kayıtlı tutulur.
- Migration'lar ayrı bir konsol projesiyle (`Bank.Database`) çalıştırılır. API açılışta şema değiştirmez:
  uygulama kullanıcısının DDL yetkisi olmamalıdır (least privilege).

## Sonuçlar

- Mevcut lokal şemanın DDL'i bir kez dışarı alınıp `V001` baseline olarak kaydedilecek.
- Yeni geliştiriciler tek komutla boş bir Oracle'ı güncel şemaya getirebilecek.
