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
- [ ] `IDashboardService` / `IDashboardRepository` dosyaları yer değiştirmiş, namespace uyuşmazlığı
- [ ] Tarihler JSON'da UTC işaretsiz (`Z` yok) dönüyor
- [ ] `CardsController`: aynı servis çağrısı iki kez yapılıyor, kullanılmayan `userId` parse'ı
- [x] Boş dosyalar, kişisel IDE/AI ayarları ve şablon kalıntıları temizlendi — log #007
- [ ] Test projeleri (xUnit) ve ilk testler
- [ ] `System.Security.Cryptography.Pkcs` açığı (NU1903), SqlClient üzerinden transitive geliyor

## Sprint 1 — Finansal domain modeli

- [ ] `Money` value object'ini güçlendir (para birimi, kuruş hassasiyeti, yuvarlama kuralı)
- [ ] Gelir, düzenli gider, abonelik, kredi kartı (ekstre kesim / son ödeme günü), taksit planı, planlanan harcama
- [ ] Tekrarlama kuralı (aylık, yıllık, ayın son günü vb.)

## Sonraki sprintler

2. CRUD API: girdi validasyonu (geçersiz istek şu an DB CHECK'e takılıp 500 dönüyor, 400 olmalı), audit kolonları, soft delete, optimistic concurrency
3. Nakit akışı projeksiyon motoru (3–12 ay, ekstre dönemi bazlı taksit dağıtımı)
4. Senaryo karşılaştırma (3 / 6 / 9 taksit what-if)
5. CSV içe aktarma ve deterministik sentetik veri üretici
6. Analitik: kategori trendleri, tekrarlayan ödeme tespiti, anomali tespiti, güven aralıklı tahmin
7. Angular ekranları
   - [ ] **Production build `main`'de bozuk**: `app.scss` bütçeyi aşıyor
   - [ ] `app.ts` / `app.component.ts` kopyası, `// ...existing code...` kalıntıları, `console.log`'lar
   - [ ] Para/tarih formatı için `tr-TR` locale8. Güvenlik ve KVKK: refresh token, rate limiting, loglarda PII maskeleme, audit log
