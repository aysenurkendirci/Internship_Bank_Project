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
- [ ] Taksit planı ve ödeme takvimi endpoint'i
- [ ] Entegrasyon testleri: kullanıcı izolasyonu, eşzamanlılık, doğrulama

## Sonraki sprintler

3. Nakit akışı projeksiyon motoru (3–12 ay, ekstre dönemi bazlı taksit dağıtımı)
4. Senaryo karşılaştırma (3 / 6 / 9 taksit what-if)
5. CSV içe aktarma ve deterministik sentetik veri üretici
6. Analitik: kategori trendleri, tekrarlayan ödeme tespiti, anomali tespiti, güven aralıklı tahmin
7. Angular ekranları
   - [ ] **Production build `main`'de bozuk**: `app.scss` bütçeyi aşıyor
   - [ ] `app.ts` / `app.component.ts` kopyası, `// ...existing code...` kalıntıları, `console.log`'lar
   - [ ] Para/tarih formatı için `tr-TR` locale8. Güvenlik ve KVKK: refresh token, rate limiting, loglarda PII maskeleme, audit log
