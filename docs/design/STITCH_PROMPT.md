# Stitch prompt'u

Aşağıdaki metin Google Stitch'e yapıştırılacak. Stitch İngilizce talimatla daha tutarlı çalıştığı için prompt İngilizce, arayüz
etiketleri Türkçe.

Hepsini tek seferde vermek yerine önce baştan LAYOUT'un sonuna kadar olan kısmı ve bir ekranı ver. Beğenince aynı oturumda diğer ekranları
tek tek iste.

```text
Design a responsive web app called "NeoBank" (desktop first, must also work at 375px mobile width).
All UI text must be in Turkish. Use Turkish number and date formats: ₺15.000,00 and 7 Kasım 2026.

PRODUCT
A retail bank's web app with a personal finance planner. Customers see their accounts, send money and manage their
cards. The planner shows their FUTURE cash flow: salary, rent, bills, subscriptions, credit card statements and
installments for the next 3-12 months, and lets them compare installment options before a purchase. The planner never
gives financial advice; it only makes the customer's own obligations visible. The tone is calm, trustworthy and
clear: a bank, not a game.

GENERAL STYLE
- Light theme. Off-white page background (#f6f8fc), white rounded cards (24px radius, 1px very light border, soft shadow).
- Font: Inter. Bold, compact headings; numbers in tabular figures.
- Primary color blue (#2563eb). Neutral slate greys for text.
- Red is reserved for "balance goes below zero", failed money operations and irreversible actions. It must always appear
  with an icon and text, never color alone.
- Chart series colors in this fixed order: blue #2a78d6, orange #eb6834, aqua #1baf7a, yellow #eda100, pink #e87ba4, green #008300.
  Thin 2px lines, bars at most 24px wide with 4px rounded tops, light horizontal gridlines, no 3D, no gradients in charts.
- Generous whitespace, 8px spacing grid, accessible contrast, visible focus states, large touch targets on mobile.
- Every list needs an empty state (one sentence telling the user what to do next) and every page a loading and an error state.

LAYOUT
Left sidebar on desktop: logo "NeoBank", then the menu in two labelled groups, and at the bottom the user name,
"Hesap ayarları" and "Çıkış". On mobile the menu becomes a horizontally scrollable strip at the top. Menu items with icons:
Bankacılık: Ana Sayfa · Hesaplarım · Para transferi · Banka kartlarım
Planlama: Nakit akışı · Taksit senaryosu · Gelir ve giderler · Kartlar ve taksitler · İşlem geçmişi · Analiz

SCREENS

1. Giriş (login) and Kayıt (register)
   Split screen: brand panel on the left, form on the right. Login fields: "T.C. Kimlik No", "Şifre", button "GİRİŞ YAP",
   link "Kayıt Ol". Show a green success notice ("Hesabınız oluşturuldu. Giriş yapabilirsiniz.") and a session-expired
   info notice variant. Register fields: Ad, Soyad, T.C. Kimlik No, E-posta, Telefon, Şifre, membership toggle
   "Bireysel / Kurumsal", button "KAYIT OL". Field-level error messages under inputs.

2. Ana Sayfa (dashboard)
   Greeting "Merhaba, Ayşe". Big total figure "Toplam varlık (TL hesaplar)" with a primary button "Para gönder".
   Section "Önümüzdeki 3 ay" with a "Nakit akışına git" link and the same three stat tiles as the cash-flow screen.
   Horizontal row of bank cards (card type, **** **** **** 4242, balance, Sanal/Fiziksel tag). "Son işlemler" list
   (icon in/out, title, category · date, amount, green for incoming). "Hesaplarım" column: account type, balance in its
   own currency, IBAN, status badge "Aktif".

3. Hesaplarım
   Header with button "Yeni hesap aç". Account cards: type ("Vadesiz hesap" or "Döviz hesabı"), balance in its own
   currency, IBAN in groups of four (TR33 0006 1005 1978 6457 8413 26) with a copy icon, status badge (Aktif, Dondurulmuş,
   Kapalı) and links "Hesap hareketleri" and "Para gönder".
   "Yeni hesap aç" opens a small dialog: Para birimi (TRY, USD, EUR), button "Hesabı aç".
   Account statement view: account summary on top, then rows (date and time, description, other party's IBAN, amount with
   + in green or - in dark grey, "İşlem sonrası bakiye"), pager "Önceki 1 / 4 Sonraki".

4. Para transferi
   A focused single-column form with three states.
   Form: Gönderen hesap (select showing account type, IBAN ending and available balance), Alıcı IBAN (auto-grouped in
   fours), Alıcı adı soyadı (helper text: "Hesap sahibinin adıyla aynı olmalıdır"), Tutar (with the account's currency),
   Açıklama, button "Devam et". Inline errors: "Alıcı IBAN geçerli değil." under the field.
   Confirmation: read-only summary of sender, recipient name, IBAN, amount and description, a sentence saying the transfer
   cannot be taken back, buttons "Gönder" and "Düzenle". While sending, the button shows a spinner and is disabled.
   Result: success receipt with a check icon, Tutar, Alıcı, IBAN, Açıklama, Tarih, İşlem no, "Yeni bakiye", and links
   "Yeni transfer" and "Hesap hareketleri". Failure notice variants, with icon and text:
   "Hesap bakiyesi yetersiz.", "Alıcı adı, IBAN'ın sahibiyle eşleşmiyor.", "Bu IBAN'a ait bir hesap bulunamadı."

5. Banka kartlarım
   Cards issued by the bank; different from the credit cards the user tracks in "Kartlar ve taksitler".
   Card visual (Banka/Kredi kartı, **** **** **** 4242, Sanal/Fiziksel, status badge Aktif, Geçici kapalı, İptal) next to a
   settings panel: switches "Temassız ödeme" and "İnternet alışverişi", fields "Günlük limit" and "Aylık limit", button
   "Kaydet". Error example: "Aylık limit günlük limitten küçük olamaz."
   Separate action: "Kartı geçici kapat" with a short confirmation; when blocked the card visual is dimmed and the action
   becomes "Kartı aç". A cancelled card is read-only and shows no actions.

6. Hesap ayarları
   Section "Verilerim": one sentence explaining the file contains everything held about the user, button "Verilerimi indir".
   Section "Oturum": button "Çıkış yap".
   Danger section "Hesabımı sil", visually separated with a red outline: a short list of what is deleted (kişisel finans
   kayıtları, kişisel bilgiler) and what is kept for the legal period (hesap hareketleri, kimliğinizle ilişkilendirilmeden),
   field "Şifreniz", red button "Hesabımı kalıcı olarak sil", and a confirmation dialog stating this cannot be undone.
   Error variants: "Şifre doğrulanamadı." and "Hesaplarınızda bakiye var. Hesabınızı silmeden önce paranızı başka bir hesaba aktarın."

7. Nakit akışı (the most important planning screen)
   Header with segmented control "3 ay / 6 ay / 12 ay".
   Three stat tiles: "Başlangıç bakiyesi", "Dönemin en düşük bakiyesi" (with date; red + warning icon when negative),
   "Bakiyenin eksiye düştüğü ay: 2 / 6" with subtitle "Ay sonu artıda olsa bile ay içinde".
   Card "Tahmini bakiye": a step line chart of the daily balance with a thin red zero line.
   Card "Aylık gelir ve çıkış": grouped bars (blue = Gelir, orange = Çıkış) per month.
   Card "Aylık özet": table with columns Ay, Gelir, Çıkış, Kart ödemeleri, Net, Ay sonu, Ay içi en düşük, Yük oranı, Durum
   (Durum shows "⚠ Eksiye düşüyor" in red or "Sorun yok" in grey).
   Card "Hareketler": dated list (date, description, type badge Gelir/Gider/Abonelik/Kart ekstresi, amount). A credit card
   statement row expands to show its lines, e.g. "Laptop (1/3) ₺333,34", "Netflix ₺229,99".

8. Taksit senaryosu
   Form card: Kart (select), "Ne alacaksınız?", Alışveriş tarihi, and a dynamic list of options, each row
   "[3] taksit, [9.000] ₺ toplam" with "Kaldır"; links "+ Seçenek ekle" and "İlk fiyatı tüm seçeneklere kopyala";
   button "Karşılaştır". Helper text: prices may differ per option.
   Result card "Karşılaştırma": comparison table, first row "Almasam" (grey), then one row per option with a color swatch:
   Aylık taksit, Toplam, Fiyat farkı, En yüksek aylık çıkış, En düşük bakiye (+date), Eksiye düşülen ay (⚠ 5 ay in red),
   Dönem sonu bakiye. Options are NOT ranked and no option is highlighted as best.
   Chart card "Aylara göre en düşük bakiye": one line per option plus a grey baseline, red zero line, legend.
   Footer info note: "Bu karşılaştırma bir öneri değildir…".

9. Gelir ve giderler
   Filter tabs: Tümü · Gelir · Gider · Abonelik. Button "Kalem ekle". List rows: type badge (green Gelir, grey Gider,
   violet Abonelik), name, "Konut · Her ay · Başlangıç 1 Oca 2026 · Bonus kartından", amount, "Düzenle", "Sil".
   Inline add/edit form card: Ad, Tür, Kategori, Tutar, Para birimi, Sıklık, Her kaç dönemde bir, Başlangıç,
   Bitiş (isteğe bağlı), Ödeme kaynağı ("Hesaptan (kendi tarihinde)" or "Bonus (son ödeme gününde)"), hidden for Gelir.

10. Kartlar ve taksitler
   Section "Kartlarım": card tiles (name, "Kesim günü: 20 · Son ödeme: kesimden 10 gün sonra", limit), add/edit form.
   Section "Taksitli alışverişler": rows (description, card · date · 3 taksit, total, "Takvim", "Sil"); "Takvim" expands a
   small table # / Ekstre / Son ödeme / Tutar.

11. İşlem geçmişi
   Upload card: primary button "CSV ekstre yükle", secondary "12 aylık demo verisi oluştur", one line of format help.
   Result notice: "120 satır işlendi: 117 yeni işlem eklendi, 3 satır zaten kayıtlıydı." Error notice variant with a
   bullet list "Satır 3: Tarih okunamadı…".
   List card: date filters (Başlangıç, Bitiş, Filtrele), "540 işlem" count, rows (date, description, category badge,
   amount), pager "Önceki 1 / 11 Sonraki".

12. Analiz
   Four stacked cards:
   - "Kategorilere göre harcama": horizontal bar chart of totals + month-by-month table with Pay and Son ay change.
   - "Düzenli ödemeler": rows "NETFLIX.COM · Sabit tutarlı · ayın 5. günü civarı · 12 ayda görüldü · sıradaki: 5 Nis 2026",
     amount, and either a green "Takipte" badge or a "Takibe al" link.
   - "Olağandışı harcamalar": rows "TEKNOSA · 18 Eki 2025: Tüm harcamalar için tipik tutarın (612 ₺) 24 katı", amount.
   - "Önümüzdeki 3 ayın günlük harcama tahmini": three tiles "Nisan 2026: ₺14.800 – ₺21.300, beklenen ₺18.050",
     explanatory text, and a collapsible "Yöntemler geçmişte ne kadar isabetliydi?" table.

COMPONENTS TO KEEP CONSISTENT
Page header (title + one-line subtitle + actions), stat tile, notice (error/info/success, optional bullet list), empty state,
primary/secondary/danger buttons, text link button, form field (label above, error below), switch, badge, confirmation dialog,
data table, pager, chart card.
```
