# ADR-0010: Frontend katmanları

Durum: Kabul edildi

## Bağlam

Ekranlar çoğaldıkça bağımlılıklar dağılmaya başladı. `core`, API modellerini import ediyordu, ana sayfa nakit akışı özelliğinin içindeki bir bileşeni kullanıyordu. Renkler Tailwind ayarında, grafik temasında ve bileşenlerin içinde ayrı ayrı yazılıydı.

## Karar

Bağımlılıklar tek yöne gider:

- `core`: oturum, HTTP yardımcıları, sayfa tabanı. Uygulamanın başka hiçbir yerini bilmez.
- `data-access`: API istemcileri ve modeller. Yalnızca `core`'u bilir.
- `shared/ui`: veriden habersiz görsel parçalar. `shared/finance` gibi alanlar API modellerini bilebilir.
- `layout`: uygulama çerçevesi.
- `features`: ekranlar. Başka bir özelliğin klasörüne giremez. İki özellik aynı şeye ihtiyaç duyuyorsa o şey `shared`'a taşınır.

`frontend/scripts/check-layers.mjs` bu kuralları CI'da denetler.

Tekrar eden işler taban sınıflara alındı: API istemcileri için `BaseApi`, sayfalardaki yükleme ve hata durumu için `BasePage`, formlar için `BaseForm`.

Sayfa veriyi çeker ve değişiklikleri yürütür. Alt bileşenler yalnızca input alır ve output verir.

Renklerin tek kaynağı `src/design/tokens.json`. Tailwind ve grafik teması aynı dosyayı okur.

## Sonuçlar

- Yeni bir sayfa yalnızca kendi verisini ve ekranını yazar.
- Renk değişikliği tek dosyada yapılır. Şablonlarda hazır Tailwind paleti ya da hex kullanılmaz.
- Kalıtım derinleşirse sorun çıkarır. Bu yüzden taban sınıflar tek seviyedir ve yalnızca durum tutar.
