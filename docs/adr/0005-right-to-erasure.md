# ADR-0005: KVKK silme hakkı nasıl karşılanacak

Durum: Kabul edildi ve uygulandı. Yasal saklama sürelerinin kesin değerleri hukuki görüşle netleşecek (aşağıya bakın).

## Bağlam

KVKK, kişinin verilerinin silinmesini veya anonim hâle getirilmesini isteyebilmesini öngörüyor. Erişim hakkı tek bir uçla
karşılanabildi. Silme hakkı bu kadar basit değil, çünkü sistemin iki özelliği "sil" komutunu kendiliğinden geçersiz kılıyor:

1. Soft delete. Kullanıcının "sil" dediği kayıtlar fiziksel olarak duruyor, sadece `DeletedAt` işaretleniyor.
2. Temporal tablolar. Her güncelleme ve silme, kaydın eski hâlini bir geçmiş tablosuna kopyalıyor. Ana tablodan silinen bir
   kart, geçmiş tablosunda yaşamaya devam ediyor. SQL Server geçmiş tablolarına doğrudan yazmaya da izin vermiyor.

Üstelik sistemdeki verilerin hepsi aynı statüde değil:

| Veri | Kim üretti | Yasal saklama yükümlülüğü |
|---|---|---|
| Bankacılık çekirdeği (`dbo.Accounts`, `dbo.Transactions`, `dbo.Cards`) | Banka | Var: mali kayıtlar mevzuat gereği yıllarca saklanmak zorunda |
| Kişisel finans verisi (`finance.*`: kartlar, düzenli kalemler, taksit planları, ekstre import'ları) | Kullanıcının kendisi | Yok: kullanıcının kendi planlama notları |
| Güvenlik log'ları | Sistem | Sınırlı süre. TC zaten maskeli |

## Karar

1. Kişisel finans verisi fiziksel olarak ve geçmişiyle birlikte silinir. Kullanıcının kendi girdiği planlama verisini saklamak için
bir yükümlülük yok, saklamaya devam etmek ise KVKK'nın "gerekli olduğu süre kadar" ilkesine aykırı olur. Silme tek bir veritabanı
prosedüründe, tek bir transaction içinde yapılır:
- Temporal tablolarda `SYSTEM_VERSIONING` geçici olarak kapatılır.
- Kullanıcının satırları hem ana hem geçmiş tablolardan silinir, ardından `finance.Transactions` ve `finance.ImportBatches` temizlenir.
- `SYSTEM_VERSIONING` aynı geçmiş tablolarıyla yeniden açılır.

Prosedür uygulama kullanıcısının değil, ayrı ve yetkileri sınırlı bir bakım kullanıcısının yetkisiyle çalışır (versioning'i kapatabilmek
yüksek bir yetki).

2. Bankacılık çekirdeği anonimleştirilir, silinmez. Mali kayıtlar yasal saklama süresi boyunca durmak zorunda. `dbo.Users`
satırındaki kişisel alanlar (ad, soyad, e-posta, telefon) anonim değerlerle değiştirilir. TC, saklama süresi boyunca mevzuatın gerektirdiği
kişi eşleşmesi için ayrı ve erişimi kısıtlı bir yerde tutulur. Süre dolunca o kayıt da silinir.

3. Talep anında oturumlar kapanır ve hesap girişe kapatılır. Silme talebi ile işlenmesi arasında hesap kullanılamaz.

4. Silme talebi ve sonucu kayda geçer, ama kaydın kendisi kişisel veri içermez (kullanıcı id'si ve zaman damgası yeterli).

## Değerlendirilen alternatif

*Her şeyi anonimleştirmek, hiçbir şeyi silmemek.* Tek tip ve basit bir yaklaşım. Ancak kişisel finans verisi (işlem açıklamaları,
"Kira – Kadıköy" gibi notlar) anonimleştirildikten sonra da kişiyi dolaylı olarak tanımlayabilir. Saklama yükümlülüğü olmayan veride
silmek daha güvenli ve daha dürüst.

## Uygulama

Karar maddeleri bire bir uygulandı: `compliance.EraseUser` prosedürü, `DELETE /api/me` ucu, "silinen kullanıcının hiçbir
verisi, geçmiş tabloları dahil, kalmadı" entegrasyon testi.

Uygulamada verilen ayrıntı kararlar:
- Prosedür `EXECUTE AS OWNER` çalışır. Uygulamanın girişi tablolar üzerinde `ALTER` yetkisine sahip olmak zorunda kalmıyor, sadece
  prosedürde `EXECUTE` hakkı yeterli. Ayrı bir bakım kullanıcısı gerektiren ilk planın yerine bu daha sade ve aynı güvenceyi veriyor.
- Geçmiş tablosu silmeleri dinamik SQL. Versioning açıkken SQL Server, geçmiş tablosuna yazılan bir `DELETE`'i derleme anında
  bile reddediyor.
- Bakiyesi olan hesabı olan kullanıcı silinemez (`409`). Parayı önce başka hesaba aktarması gerekir. Kapatılan hesapta kalan para
  ortadan kaybolurdu. Prosedür bunu transaction içinde bir kez daha kontrol ediyor.
- TC kimlik no ayrı tutuluyor (`compliance.ErasedIdentities`), `dbo.Users`'ta yer tutucu bir değerle değişiyor. Kullanıcı satırı
  silinmiyor, çünkü hesaplar ve ledger ona bağlı.
- Eski token'lar da kapanıyor. Erişim token'ı süresi dolana kadar geçerli olurdu, bu yüzden JWT doğrulaması silinmiş kullanıcıyı
  reddediyor. Cevap kısa süre bellekte tutuluyor (en fazla 30 sn gecikme). Yenileme token'ları silinir.
- Şifre yeniden isteniyor ve uç, giriş uçlarıyla aynı hız sınırına tabi: açık kalmış bir oturum tek başına bir hesabı yok etmeye
  yetmemeli ve bu uç bir şifre deneme yolu olmamalı.

Hâlâ hukuki görüş bekleyen: bankacılık çekirdeği kayıtlarının ne kadar süre saklanacağı ve süre dolunca bu kayıtların (hesap, ledger,
`compliance.ErasedIdentities`) silinmesi. Süreler koda gömülmedi, bu temizlik işi henüz yazılmadı.

## Sonuçlar

- Kişisel finans verisi geri dönüşsüz silinir. Silme ve geçmiş tablolarının temizliği tek transaction'da yapıldığı için yarım kalmış bir
  silme durumu oluşmaz.
- Bir silme ancak şifre yeniden girilirse ve hesaplarda para yoksa çalışır. Silinen kullanıcının eski token'ları birkaç saniye içinde
  reddedilir.
- Çekirdek kayıtların saklama süresi dolunca temizlenmesi, süreler hukuki görüşle netleşene kadar bekliyor.
