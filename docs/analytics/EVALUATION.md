# Analitik Bileşenlerin Değerlendirmesi

Bu belge, platformun analitik bileşenlerinin (tekrarlayan ödeme tespiti, anomali tespiti, harcama tahmini) nasıl
değerlendirildiğini ve sonuçlarını özetler. Bütün sayılar tek bir komutla yeniden üretilebilir:

```bash
cd backend
dotnet run --project tools/Bank.Evaluation -c Release
```

## 1. Veri

Gerçek kişisel finans verisi açık veri setlerinde neredeyse bulunmaz ve kişisel veri olduğu için toplanması KVKK
kapsamında ayrıca tasarım gerektirir. Bu yüzden değerlendirme, deterministik bir sentetik profil üreticisiyle
(`SyntheticHistoryGenerator`) yapıldı. Profil, İstanbul'da maaşlı çalışan bir kişiyi modelliyor:

| Desen | Modelleme |
|---|---|
| Maaş | Ayın 15'i. Hafta sonuna denk gelirse önceki iş günü |
| Sabit ödemeler | Kira, spor salonu, iki dijital abonelik, internet: sabit gün, sabit tutar |
| Değişken faturalar | Elektrik (±%20), su (±%15), doğalgaz (mevsimsel, kış/yaz oranı ≈ 8) |
| Günlük harcama | Market (haftada ~2,5 kez, 250–1.400 TL), restoran (haftada ~1,2 kez), ulaşım dolumu (haftalık) |
| Enflasyon | Değişken harcamalarda aylık ~%2,5 artış |
| Anomaliler | Ayda %20 ihtimalle 12.000–35.000 TL elektronik alışverişi. Etiketli (ground truth) |

Her profil bir `seed` ile üretilir ve aynı seed her zaman aynı veriyi verir.

## 2. Yöntem

Ayar / test ayrımı. Bileşenler geliştirilirken yalnızca 1–10 numaralı profiller incelendi. 101–150 numaralı
50 profil geliştirme sırasında hiç kullanılmadı ve yalnızca nihai değerlendirmede kullanıldı. Bu ayrım, eşiklerin
değerlendirme verisine göre ayarlanmasını (overfitting) engellemek için yapıldı.

Önceden belirlenen kabul kriterleri. Ölçümden önce yazıldı:

| Bileşen | Kriter |
|---|---|
| Tekrarlayan ödeme tespiti | precision ≥ 0,95 ve recall ≥ 0,95 |
| Anomali tespiti (etiketli) | precision ≥ 0,95 ve recall ≥ 0,95 |
| Harcama tahmini | MAPE ≤ %10 ve %95 aralık kapsama oranı ≥ %80 |

## 3. Sonuçlar

### 3.1 Tekrarlayan ödeme tespiti

| Profiller | Doğru pozitif | Yanlış pozitif | Kaçırılan | Precision | Recall |
|---|---|---|---|---|---|
| Ayar (1–10) | 90 | 0 | 0 | 1,000 | 1,000 |
| Test (101–150) | 450 | 0 | 0 | 1,000 | 1,000 |

Abonelikler ve kira "sabit", faturalar "değişken" olarak doğru sınıflandırıldı. Haftalık ve ayda birden çok yapılan
harcamalar doğru şekilde elendi. Kriter karşılandı.

### 3.2 Anomali tespiti: etiketli anomaliler

| Profiller | Doğru pozitif | Yanlış pozitif | Kaçırılan | Normal çıkış sayısı |
|---|---|---|---|---|
| Ayar (1–10) | 24 | 0 | 0 | 3.386 |
| Test (101–150) | 133 | 0 | 0 | 17.022 |

Kriter karşılandı. Ancak etiketli anomaliler tipik harcamanın 20–60 katı olduğundan bu test kolaydır. Asıl soru
duyarlılıktır (3.3).

### 3.3 Anomali tespiti: duyarlılık analizi

Temiz profillere her ay, market medyanının k katı tutarında bir market alışverişi eklendi (profil başına 12, toplam
600 enjeksiyon). Tablo, farklı eşiklerde yakalanma oranını (recall) gösteriyor:

| Eşik | k=2 | k=3 | k=4 | k=5 | k=6 | k=8 | Yanlış pozitif |
|---|---|---|---|---|---|---|---|
| 2,0 | 0,00 | 0,64 | 1,00 | 1,00 | 1,00 | 1,00 | 0 / 17.022 |
| 2,5 | 0,00 | 0,02 | 0,66 | 0,94 | 1,00 | 1,00 | 0 / 17.022 |
| 3,0 | 0,00 | 0,00 | 0,10 | 0,62 | 0,80 | 1,00 | 0 / 17.022 |
| 3,5 (varsayılan) | 0,00 | 0,00 | 0,00 | 0,10 | 0,50 | 0,80 | 0 / 17.022 |

### 3.4 Günlük harcama tahmini

24 aylık geçmiş, 7.–24. aylar için tek adımlı rolling-origin backtest, 50 test profili:

| Yöntem | MAPE (ortalama ± profiller arası sapma) | %95 aralık kapsama | Backtest ile seçilme |
|---|---|---|---|
| Naif (geçen ay) | %15,3 ± %6,8 | %96 | 5 / 50 |
| Hareketli ortalama (3 ay) | %12,8 ± %8,4 | %95 | 40 / 50 |
| Doğrusal trend (6 ay) | %15,3 ± %11,9 | %96 | 5 / 50 |
| Profil başına seçilen | %12,4 | %95 | |
| *Kâhin (gerçek beklenen değer)* | *%10,4* | | |

Kâhin referansı. Her ayın gerçek beklenen değeri, 300 bağımsız profilin ortalamasıyla yaklaşık olarak bulundu.
Gerçek beklenen değeri bilen kusursuz bir tahminci bile %10,4 hata yapıyor. Bu, verinin indirgenemez gürültüsü.

## 4. Projenin iki sorusuna cevaplar

S1. Geçmiş ödeme davranışı ve gelecekteki yükümlülükler birlikte gösterildiğinde finansal baskı daha erken
görünür hale getirilebilir mi?

Evet, en az iki somut mekanizmayla:
1. Ay içi en düşük bakiye. Kirası ayın 1'inde, maaşı 15'inde olan bir kullanıcı için ay net +25.000 TL ile kapanırken
   bakiye 1–15 arasında −15.000 TL'de kalabiliyor. Ay sonu özeti bunu göstermez. Projeksiyon gösteriyor (ADR-0003 §5).
2. Ekstre ve son ödeme günü bazlı kart ödemeleri. Hafta sonu kayması nedeniyle bir taksitin bir sonraki aya kayması,
   o ayda çift ödeme oluşturabiliyor. Harcama gününü esas alan bir bütçe bunu göremez.

Taksit senaryosu karşılaştırması, aynı alışverişin 3 taksitte bakiyeyi 5 ay eksiye düşürdüğünü, 6 taksitte hiç
düşürmediğini, alışveriş yapılmadan önce gösterebiliyor.

S2. Basit istatistiksel tahminler kişisel nakit akışı planlamasında yeterli olabilir mi?

Kısmen, ve doğru biçimde sunulduğunda evet:
- Noktasal tahmin. Önceden konan MAPE ≤ %10 kriteri karşılanmadı (%12,4). Ama kâhin bile %10,4'te kalıyor. Yani
  kriter bu veri için ulaşılamazdı ve basit hareketli ortalama gürültü tabanına yalnızca ~2 puan uzak. Daha karmaşık
  yöntemlerin kazanabileceği alan bu 2 puanla sınırlı.
- Aralık tahmini. %95 aralık gerçekleşen değeri %95 oranında kapsıyor, yani kusursuz kalibre. Kriter karşılandı.
- Sonuç. Basit yöntemler, "gelecek ay X TL harcayacaksın" gibi tek bir sayı vermek için orta düzeyde, ama "X ile Y
  arasında harcayacaksın" demek için güvenilir. Planlama arayüzü noktasal tahmin yerine aralık göstermeli.
- Doğrusal trend, verideki enflasyona rağmen hareketli ortalamadan kötü ve en değişken sonucu verdi. Kısa ve gürültülü
  serilerde trend kestirimi riskli.

## 5. Sonuçların sınırları

1. Sentetik veri gerçekten daha düzenli. Abonelikler her ay tam aynı gün ve tutarla geliyor, harcama tutarları sınırlı
   (uniform) dağılıyor. Tespit sonuçları (precision/recall = 1,0) gerçek dünya performansı olarak sunulmamalı. Gerçek
   bozulmalar (zam, atlanan ay, yazım farkları, aynı ayda iade + tekrar çekim) ayrıca birim testlerle denendi. Sonuncusu
   bilinen bir sınırdır.
2. Yanlış pozitif oranı muhtemelen olduğundan düşük. Sentetik tutarların uzun kuyruğu yok. Gerçek veride düşük eşikler
   (2,0–2,5) yanlış alarm üretecektir. Bu yüzden varsayılan eşik literatürdeki temkinli 3,5'te bırakıldı.
3. Tek profil tipi. Sonuçlar maaşlı, düzenli gelirli bir kişi için. Serbest çalışan veya düzensiz gelirli profiller
   değerlendirilmedi.
4. Küçük örneklem uyarısı. Geliştirme sırasında 10 profille yapılan bir ön ölçüm, tahmin hatasını %17,6 olarak
   göstermişti. 50 profilde bu değer %12,4. Profiller arası sapma (±%8,4) yüksek olduğu için az sayıda profille yapılan
   ölçümler yanıltıcı olabilir.

## 6. Kapsam notu

Sistem bir karar destek ve simülasyon aracıdır. Hiçbir bileşen kredi, yatırım veya harcama tavsiyesi vermez: senaryo
karşılaştırması seçenekleri sıralamaz, anomali tespiti bir harcamayı "yanlış" ilan etmez, yalnızca kullanıcının kendi
alışkanlıklarına göre açıklar.
