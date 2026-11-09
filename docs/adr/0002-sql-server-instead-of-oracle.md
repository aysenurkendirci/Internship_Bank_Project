# ADR-0002: Oracle yerine SQL Server, şema yönetimi DbUp ile

Durum: Kabul edildi · Yerine geçtiği karar: [ADR-0001](0001-database-schema-migrations.md)

## Bağlam

ADR-0001 hazırlanırken Oracle şemasının repoya alınamadığı görüldü. Tablolar ve `PKG_AUTH`,
`PKG_DASHBOARD` paketleri sadece bir geliştiricinin lokal ortamındaydı, istemci araçları da kurulu
değildi. Şema zaten sıfırdan yazılacağı için veritabanı seçimi yeniden değerlendirildi.

Kriterler:

1. Geliştirme makinesinde ek kurulum gerektirmeden çalışması
2. .NET ekosistemiyle uyum (sürücü, araçlar, dokümantasyon)
3. Bankacılık sektöründe karşılığı olması
4. Şemanın repodan tek komutla kurulabilmesi

## Seçenekler

| Seçenek | Değerlendirme |
|---|---|
| Oracle'da kalmak | Sektörde güçlü, ama mevcut şema kayıp. Lokal kurulum ve istemci araçları ayrıca gerekiyor. |
| SQL Server | Makinede kurulu (2022 Developer), Windows Authentication ile şifresiz bağlantı kuruluyor, Microsoft.Data.SqlClient birinci sınıf .NET sürücüsü. Türkiye'de birçok banka ve katılım bankasında kullanılıyor. |
| PostgreSQL | Güçlü ve açık kaynak, ama ek kurulum gerektiriyor ve bu projede belirgin bir avantaj sunmuyor. |

## Karar

1. Veritabanı: SQL Server.
2. Veri erişimi: Dapper + parametreli SQL, sorgular repository sınıflarında duruyor.
   - Oracle'daki stored procedure yaklaşımı bırakıldı. Gerekçe: sorgu ile onu kullanan kod aynı
     commit'te, aynı code review'da görünüyor. Bir sorgu değişikliği için ayrı bir veritabanı
     deploy'u gerekmiyor (KISS).
   - Tüm değerler parametre olarak geçiliyor (SQL injection'a karşı). String birleştirerek SQL kurulmaz.
   - EF Core da değerlendirildi. Projenin hesaplama ağırlıklı kısmı (nakit akışı projeksiyonu)
     zaten bellekte, saf domain kodunda çalışacak. Veri erişimi basit okuma/yazmalardan oluşuyor.
     Mevcut Dapper altyapısını korumak daha az değişiklik demek.
3. Şema yönetimi: DbUp (ADR-0001'deki gerekçeler geçerli):
   - Scriptler `backend/src/Bank.Database/Migrations/` altında `V001__aciklama.sql` biçiminde.
     Proje içine gömülü kaynak (embedded resource) olarak derleniyor, uygulamayla aynı versiyonda dağıtılıyor.
   - Çalışmış bir script asla değiştirilmez. Değişiklik her zaman yeni bir script olarak eklenir.
   - Migration'ları API değil, ayrı bir konsol uygulaması (`Bank.Database`) çalıştırır.
     API'nin veritabanı kullanıcısı DDL yetkisine ihtiyaç duymaz (least privilege).

## Şema kuralları

- Para tutarları `DECIMAL(19,4)`. `FLOAT`/`REAL` kullanılmaz, çünkü ikili kayan noktalı sayılar
  0.1 gibi ondalık değerleri tam temsil edemez.
- Zaman damgaları UTC olarak `DATETIME2` tipinde (`SYSUTCDATETIME()`).
- Durum/tip kolonlarında `CHECK` constraint kullanılıyor. Geçersiz veri uygulama hatası olsa bile tabloya giremez.
- Kart numarasının tamamı saklanmıyor, sadece son 4 hane (PCI DSS veri minimizasyonu).

## Sonuçlar

- `Oracle.ManagedDataAccess.Core` ve `Dapper.Oracle` bağımlılıkları kaldırılıyor.
- Lokal kurulum: `dotnet run --project src/Bank.Database` boş bir veritabanını güncel şemaya getirir.
