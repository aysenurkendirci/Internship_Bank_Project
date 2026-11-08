# Notlar

Projede beni uğraştıran şeyler. Hepsini yazmadım, sadece bir daha karşıma çıkarsa hatırlamak istediklerimi.

## Şifreler düz metindi

Eski kodu ayağa kaldırmaya çalışırken fark ettim: `PasswordHasher.Hash(p)` girdiyi aynen geri döndürüyordu. JWT anahtarı da
`appsettings.json` içindeydi. Şifreleri PBKDF2'ye geçirdim, anahtarı user-secrets'a taşıdım. Eski anahtar git geçmişinde duruyor, onu
yanmış sayıyorum.

## Para birimleri birbirine toplanıyordu

Panel TL ve USD bakiyeleri düz topluyordu, 15.000 TL + 900 USD ekranda "15.900" oluyordu. `Money` tipi artık farklı para birimlerini
toplamayı reddediyor.

## Kaybolan kuruş

1.000 TL'yi 3 taksite bölünce 333,33 x 3 = 999,99 ediyor. Kalan kuruşu ilk taksite ekledim. Testte tek bir örneğe bakmak yerine
parçaların toplamının her zaman bütüne eşit olduğunu kontrol ediyorum.

## Ayın 31'i

Her ayın 31'inde olan bir ödemeyi bir önceki tarihten hesaplayınca Şubat'tan sonra kalıcı olarak 28'ine kayıyordu. Her tarihi başlangıç
tarihinden hesaplıyorum. Testin bunu yakaladığından emin olmak için kodu bilerek bozup denedim.

## Haziran'da iki taksit

Test yazarken denk geldim. Mayıs taksidinin son ödeme günü Cumartesi'ye düşüp 1 Haziran'a kayınca Mayıs boş kalıyor, Haziran'da iki
ödeme oluyor. Ay ay bütçe yapan biri bunu göremez.

## Türkçe sunucuda çöken validasyon

`[Range(typeof(decimal), "0.01", ...)]` sınırı sunucu kültürüyle okuyordu. tr-TR'de "0.01" geçersiz sayı olduğu için her kart oluşturma
500 veriyordu. İngilizce makinede hiç görünmeyecek bir hata. `ParseLimitsInInvariantCulture` ile çözüldü.

## Türkçe I, iki kez

CSV başlığında `TARİH` eşleşmedi, sonra mükerrer kontrolünde `MİGROS` ile `MIGROS` farklı mağaza sanıldı. `ToLowerInvariant`'ın İ harfine
ne yaptığını yanlış biliyordum, küçük bir programla bakınca gördüm: olduğu gibi bırakıyor. İkisi için tek bir `TextFolding` yazdım.

## "1.2.3" = 12,3

Bozuk bir ekstre tutarı hata vermek yerine 12,3 olarak okunuyordu. Hiçbir şey patlamıyor, sadece sayı yanlış. Binlik gruplarını artık
kontrol ediyorum.

## İki kahve

Aynı gün aynı kafeden iki kahve alınca mükerrer kontrolü ikincisini siliyordu. Parmak izine, dosyadaki aynı satırlar arasındaki sırayı
ekledim. Aynı dosya tekrar yüklenince bir şey eklenmiyor, iki kahve de kalıyor.

## "Bugün" hangi gün

Sunucu UTC, kullanıcı İstanbul'da. Gece 00:00 ile 03:00 arasında hem backend (`DateTime.UtcNow.Date`) hem frontend (`toISOString()`) dünü
veriyordu.

## Para transferi

Üç şeye takıldım. Cevabı alamayan istemci isteği tekrar edince para iki kez gitmesin diye `Idempotency-Key` var. Aynı bakiyeye aynı anda
iki istek gelince ikisi de geçmesin diye hesap satırlarını `UPDLOCK` ile kilitliyorum. A→B ve B→A birbirini beklemesin diye de hesapları
hep küçük id'den başlayarak kilitliyorum.

Kilitleri koddan silip testleri çalıştırdım, üç eşzamanlılık testi kırıldı. O zaman testlerin işe yaradığına ikna oldum.

## Proxy arkasında rate limit

Docker Compose'u yazarken fark ettim: nginx arkasında bütün istekler aynı IP'den geliyor, on hatalı girişten sonra herkes kilitlenirdi.
`X-Forwarded-For`'u okuyorum ama sadece ayarla açılıyor, API'ye doğrudan erişilebiliyorsa bu başlığa güvenmek yanlış olur.

## Silme hakkı ve temporal tablolar

Geçmiş tablosuna yazan `DELETE`'i SQL Server prosedür oluşturulurken reddetti (13560), versioning açıkken derlemeye bile izin vermiyor.
Dinamik SQL'e çevirdim. Bir de silinen kullanıcının eski token'ı hâlâ çalışıyordu, JWT doğrulamasına kontrol ekledim. En fazla 30 saniye
gecikmeli, her istekte veritabanına gitmek istemedim.

## Reddedilen token ve transaction

Kullanılmış bir refresh token tekrar gelince bütün oturumları kapatıyorum ama ilk yazdığımda exception atsam transaction geri alınıp
kapatma da gidecekti. Handler `null` dönüyor, 401'i controller atıyor.

## Aralıklı kırılan test

`FOR SYSTEM_TIME AS OF` testi arada bir kırılıyordu. Dapper `DateTime`'ı `datetime` olarak gönderiyor, bu 3 ms'ye yuvarlanıyor, yuvarlama da
hemen ardından gelen güncellemeyi geçebiliyor. Parametreyi `datetime2` yaptım. CI'daki kırmızının sebebi bu muydu henüz bilmiyorum.
