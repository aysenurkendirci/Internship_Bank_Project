# ADR-0005: KVKK silme hakkı nasıl karşılanacak

**Durum:** Önerildi

## Bağlam

KVKK, kişinin verilerinin silinmesini veya anonim hâle getirilmesini isteyebilmesini öngörüyor. Erişim hakkı (#055) tek bir uçla
karşılanabildi. Silme hakkı bu kadar basit değil, çünkü sistemin iki özelliği "sil" komutunu kendiliğinden geçersiz kılıyor:

1. **Soft delete (#020).** Kullanıcının "sil" dediği kayıtlar fiziksel olarak duruyor, sadece `DeletedAt` işaretleniyor.
2. **Temporal tablolar (#054).** Her güncelleme ve silme, kaydın eski hâlini bir geçmiş tablosuna kopyalıyor. Ana tablodan silinen bir
   kart, geçmiş tablosunda yaşamaya devam ediyor. SQL Server geçmiş tablolarına doğrudan yazmaya da izin vermiyor.

Üstelik sistemdeki verilerin hepsi aynı statüde değil:

| Veri | Kim üretti | Yasal saklama yükümlülüğü |
|---|---|---|
| Bankacılık çekirdeği (`dbo.Accounts`, `dbo.Transactions`, `dbo.Cards`) | Banka | Var: mali kayıtlar mevzuat gereği yıllarca saklanmak zorunda |
| Kişisel finans verisi (`finance.*`: kartlar, düzenli kalemler, taksit planları, ekstre import'ları) | Kullanıcının kendisi | Yok: kullanıcının kendi planlama notları |
| Güvenlik log'ları | Sistem | Sınırlı süre. TC zaten maskeli (#050) |

## Karar

**1. Kişisel finans verisi fiziksel olarak ve geçmişiyle birlikte silinir.** Kullanıcının kendi girdiği planlama verisini saklamak için
bir yükümlülük yok, saklamaya devam etmek ise KVKK'nın "gerekli olduğu süre kadar" ilkesine aykırı olur. Silme tek bir veritabanı
prosedüründe, tek bir transaction içinde yapılır:
- Temporal tablolarda `SYSTEM_VERSIONING` geçici olarak kapatılır.
- Kullanıcının satırları hem ana hem geçmiş tablolardan silinir, ardından `finance.Transactions` ve `finance.ImportBatches` temizlenir.
- `SYSTEM_VERSIONING` aynı geçmiş tablolarıyla yeniden açılır.

Prosedür uygulama kullanıcısının değil, ayrı ve yetkileri sınırlı bir bakım kullanıcısının yetkisiyle çalışır (versioning'i kapatabilmek
yüksek bir yetki).

**2. Bankacılık çekirdeği anonimleştirilir, silinmez.** Mali kayıtlar yasal saklama süresi boyunca durmak zorunda. `dbo.Users`
satırındaki kişisel alanlar (ad, soyad, e-posta, telefon) anonim değerlerle değiştirilir. TC, saklama süresi boyunca mevzuatın gerektirdiği
kişi eşleşmesi için ayrı ve erişimi kısıtlı bir yerde tutulur. Süre dolunca o kayıt da silinir.

**3. Talep anında oturumlar kapanır ve hesap girişe kapatılır.** Silme talebi ile işlenmesi arasında hesap kullanılamaz.

**4. Silme talebi ve sonucu kayda geçer**, ama kaydın kendisi kişisel veri içermez (kullanıcı id'si ve zaman damgası yeterli).

## Değerlendirilen alternatif

*Her şeyi anonimleştirmek, hiçbir şeyi silmemek.* Tek tip ve basit bir yaklaşım. Ancak kişisel finans verisi (işlem açıklamaları,
"Kira – Kadıköy" gibi notlar) anonimleştirildikten sonra da kişiyi dolaylı olarak tanımlayabilir. Saklama yükümlülüğü olmayan veride
silmek daha güvenli ve daha dürüst.

## Sonuçlar

- Uygulama: bir migration (prosedür ve bakım rolü), `DELETE /api/me` ucu (yeniden şifre doğrulamasıyla) ve "silinen kullanıcının
  hiçbir verisi, geçmiş tabloları dahil, kalmadı" entegrasyon testi.
- Yasal saklama sürelerinin kesin değerleri ve hangi kayıtlara uygulandığı bir hukuk görüşüyle netleştirilmeden bu ADR "Önerildi"
  durumunda kalır.
