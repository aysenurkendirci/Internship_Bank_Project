# ADR-0003: Nakit akışı projeksiyonunun varsayımları

**Durum:** Kabul edildi

## Bağlam

Projenin çıkış noktası şu soru: geçmiş ödeme davranışı ile gelecekteki yükümlülükler birlikte gösterildiğinde
finansal baskı daha erken görünür hale getirilebilir mi? Bunun için kullanıcının bilinen gelir, gider, abonelik,
kart ve taksitlerinden gelecek aylar için bir nakit akışı takvimi üretiliyor.

Bir projeksiyon ancak varsayımları kadar doğrudur. Bu belge, motorun (`CashFlowProjector`) bilinçli olarak yaptığı
varsayımları ve nedenlerini kayıt altına alıyor.

## Kararlar

### 1. Kart harcaması, ekstrenin son ödeme gününde hesaptan çıkar

Karttan yapılan her harcama (taksitler ve karta bağlı düzenli ödemeler) ait olduğu ekstrede toplanır. Hesaptan
**tek bir "kart ödemesi" olarak, son ödeme gününde** düşer. Ekstrenin içeriği (`Charges`) ayrıca listelenir.

*Neden:* Paranın hesaptan gerçekten çıktığı an budur. Harcama gününü esas almak, kart ödemelerini yanlış aya yazar.

### 2. Kullanıcının ekstrenin tamamını ödediği varsayılır

Asgari ödeme, borç devri ve kart faizi modellenmez.

*Neden:* Asgari ödeme bir kullanıcı kararıdır. Faiz hesaplaması ise bankaya ve dönem içi bakiyeye bağlıdır. Sistem
karar destek aracıdır, kullanıcının davranışını tahmin etmez. Tam ödeme varsayımı, kullanıcının gerçek yükünü
gösteren en sade referanstır.

### 3. Kart harcamaları için üç ay geriye bakılır

Projeksiyon 1 Mart'ta başlasa bile 31 Ocak'taki bir kart harcaması 2 Mart'ta ödenir ve projeksiyona girmelidir.
Bir harcamanın hesaba yansıması en fazla yaklaşık iki ay sürer: ekstreye kadar en fazla bir ay, son ödemeye kadar
en fazla 30 gün, artı hafta sonu kayması. Üç aylık geriye bakış bunu güvenli bir payla kapsar.

### 4. Aynı gün içinde çıkışlar girişlerden önce işlenir

Aynı gün hem maaş hem kira varsa, hangisinin önce hesaba geçeceği bankaya ve saate bağlıdır. Motor **kötü
senaryoyu** seçer: önce çıkışlar işlenir.

*Neden:* Projeksiyonun amacı finansal baskıyı erken göstermek. Riski olduğundan düşük göstermektense yüksek
göstermek daha güvenli. Bankacılıkta risk tarafında durmak genel bir ilkedir.

### 5. Ay sonu bakiyesi yerine "en düşük bakiye" de raporlanır

Kira ayın 1'inde, maaş 15'inde ise bakiye ayın ortasında eksiye düşüp ay sonunda artıya dönebilir. Sadece ay
sonu bakiyesini gösteren bir özet bu baskıyı gizler. Her ay için en düşük bakiye ve tarihi hesaplanır.

### 6. Ödeme yükü oranı = ayın toplam çıkışı / ayın geliri

Kart ödemeleri dahil bütün çıkışlar paydaya girer. Geliri olmayan bir ay için oran tanımsızdır (`null`). Sıfıra
bölme veya yanıltıcı bir sonsuz değer üretilmez.

### 7. Tek para birimi

Projeksiyon, başlangıç bakiyesinin para biriminde (TRY) yapılır. Başka para birimindeki kalemler kur tahmini
gerektirdiği için **dışarıda bırakılır ve `ExcludedItems` listesinde açıkça raporlanır**. Sonuç hiçbir zaman eksik
olduğu halde tamammış gibi davranmaz.

### 8. Başlangıç bakiyesi aktif TRY hesaplarının toplamıdır ve projeksiyon yarından başlar

Bugünün hareketlerinin mevcut bakiyeye zaten yansıdığı varsayılır. Bugünü dahil etmek, aynı kalemi iki kez saymak
anlamına gelebilir.

## Kapsam dışı

- Resmi tatiller (hafta sonu kaydırması var, bayram tatilleri yok)
- Döviz kurları
- Değişken tutarlı faturalar (elektrik, doğalgaz). Bunlar sabit bir tahmini tutarla girilebilir. İstatistiksel
  tahmin Sprint 6'da ele alınacak.

## Sonuçlar

- Motor saf bir fonksiyon: aynı girdi her zaman aynı çıktıyı üretir. Saat, veritabanı veya ağ bağımlılığı yok.
  Bu sayede her varsayım birim testle kilitlenebildi.
- Varsayımlar değişirse (örneğin asgari ödeme senaryosu eklenirse) bu belge güncellenir.
