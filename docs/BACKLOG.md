# Backlog

## Sprint 0 — Temel ve repo hijyeni

- [x] Kök `.gitignore`, build çıktılarını takipten çıkar — log #001
- [x] JWT secret'ını kaynak koddan çıkar — log #002
- [x] Şemayı versiyonla: SQL Server + DbUp, baseline `V001` — ADR-0002, log #003
- [x] Infrastructure katmanını Oracle'dan SQL Server'a taşı — log #004
- [ ] **Şifreler düz metin saklanıyor**: `PasswordHasher` gerçek hash kullanmalı (PBKDF2)
- [x] `CancellationToken`'ı repository → Dapper'a kadar ilet — log #004
- [ ] `CancellationToken`'ı controller → service → repository zincirinde eksiksiz ilet (AuthService)
- [ ] `DashboardRepository`'deki sunum mantığını (etiket, ikon, sabit `0.024m`) doğru katmana taşı
- [ ] `IDashboardService` / `IDashboardRepository` dosyaları yer değiştirmiş, namespace uyuşmazlığı
- [ ] `ErrorHandlingMiddleware` pipeline'a eklenmemiş: hatalar 500 + stack trace dönüyor (bilgi sızıntısı)
- [ ] Tarihler JSON'da UTC işaretsiz (`Z` yok) dönüyor
- [ ] Login'de bilinmeyen TC için 404 dönüyor: kullanıcı varlığı sızıyor (user enumeration), 401 olmalı
- [ ] Test projeleri (xUnit) ve ilk testler
- [ ] `System.Security.Cryptography.Pkcs` açığı (NU1903), SqlClient üzerinden transitive geliyor

## Sprint 1 — Finansal domain modeli

- [ ] `Money` value object'ini güçlendir (para birimi, kuruş hassasiyeti, yuvarlama kuralı)
- [ ] Gelir, düzenli gider, abonelik, kredi kartı (ekstre kesim / son ödeme günü), taksit planı, planlanan harcama
- [ ] Tekrarlama kuralı (aylık, yıllık, ayın son günü vb.)

## Sonraki sprintler

2. CRUD API: validasyon, ProblemDetails, audit kolonları, soft delete, optimistic concurrency
3. Nakit akışı projeksiyon motoru (3–12 ay, ekstre dönemi bazlı taksit dağıtımı)
4. Senaryo karşılaştırma (3 / 6 / 9 taksit what-if)
5. CSV içe aktarma ve deterministik sentetik veri üretici
6. Analitik: kategori trendleri, tekrarlayan ödeme tespiti, anomali tespiti, güven aralıklı tahmin
7. Angular ekranları
8. Güvenlik ve KVKK: refresh token, rate limiting, loglarda PII maskeleme, audit log
