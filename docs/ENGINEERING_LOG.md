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

**Sprint:** 0 · **Tarih:** 2026-11-07

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

**Sprint:** 0 · **Tarih:** 2026-11-07

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
