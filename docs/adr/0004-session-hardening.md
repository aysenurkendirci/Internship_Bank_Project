# ADR-0004: Oturum güvenliğini sertleştirme planı

**Durum:** Kabul edildi, uygulaması ertelendi

## Bağlam

Şu anki oturum modeli basit: girişte 60 dakikalık bir JWT dönüyor ve web istemcisi onu `localStorage`'da tutuyor. Sprint 8'de
giriş uçlarına rate limit kondu (#049), denemeler maskeli olarak log'lanıyor (#050) ve token'dan kişisel veri çıkarıldı (#051).
Ama iki zayıf nokta duruyor:

1. **`localStorage` XSS'e açık.** Sayfaya bir şekilde script enjekte edilirse token okunup başka bir yere gönderilebilir. Angular'ın
   şablon kaçışları ve sıkı CSP bu riski düşürüyor ama sıfırlamıyor.
2. **Token geri alınamıyor.** Çalınan bir token, süresi dolana kadar (60 dakika) geçerli. Çıkış yapmak sadece tarayıcıdaki kopyayı siliyor.

## Karar

Bankacılık uygulamalarında yaygın olan iki token'lı modele geçilecek:

| | Erişim token'ı | Yenileme token'ı |
|---|---|---|
| Ömür | 5–10 dakika | birkaç saat (kayan) |
| Nerede | Sadece bellekte (Angular servisinde bir signal) | `HttpOnly; Secure; SameSite=Strict` cookie, path `/api/auth/refresh` |
| Script okuyabilir mi | Evet, ama sekme kapanınca yok olur | Hayır |
| Sunucuda | Saklanmaz | Hash'i `dbo.RefreshTokens` tablosunda: kullanıcı, oluşturulma, son kullanma, iptal zamanı, yerine geçen token |

Akış:
- Her yenilemede eski yenileme token'ı iptal edilip yenisi verilir (rotation).
- **Daha önce kullanılmış bir yenileme token'ı tekrar gelirse** bu çalınma belirtisidir ve o kullanıcının bütün oturumları iptal edilir
  (reuse detection).
- Çıkış, sunucudaki yenileme token'ını iptal eder. Böylece çalınmış bir kopya da işe yaramaz.
- Frontend'deki 401 yönetimi (#040), login'e göndermeden önce bir kez `/refresh` dener.
- Cookie kullanıldığı için `/refresh` ve `/logout` uçlarına CSRF koruması gerekir: `SameSite=Strict` ve ek olarak özel bir başlık
  kontrolü.

## Neden şimdi değil

Bu değişiklik hem backend'de (yeni tablo, uçlar, rotation ve reuse kuralları) hem frontend'de (bellekte token, sessiz yenileme,
eşzamanlı isteklerin tek bir yenilemeyi beklemesi) oturumun tamamına dokunuyor. Yarım uygulanmış bir oturum mekanizması, olmamasından
daha tehlikeli: örneğin rotation var ama reuse detection yoksa, çalınan token sonsuza kadar yenilenebilir. Doğru yapılıp uçtan uca test
edilene kadar mevcut model korunuyor. Mevcut modelin riskleri `SessionService` belgesinde açıkça yazılı.

## Sonuçlar

- Mevcut durumda token ömrü kısa tutuluyor (60 dakika, konfigürasyondan okunuyor).
- Uygulama sırasında bu belgedeki akış bire bir izlenecek ve "kullanılmış token tekrar gelince bütün oturumlar kapanır" senaryosu bir
  entegrasyon testiyle kilitlenecek.
