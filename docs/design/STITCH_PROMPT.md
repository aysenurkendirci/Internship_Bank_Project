# Google Stitch tasarım prompt'u

Ekranların görsel tasarımı Google Stitch'te yapılacak. Aşağıdaki metin olduğu gibi Stitch'e yapıştırılabilir. Stitch İngilizce
talimatlarla daha tutarlı çalıştığı için prompt İngilizce, ama arayüz metinlerinin Türkçe olması istendi ve gerçek etiketler
prompt'un içinde. Tasarımlar geldiğinde her ekran, kodda zaten ayrı duran bileşenlere (`docs/ENGINEERING_LOG.md` #048) parça parça
uygulanacak.

Tek seferde bütün uygulama yerine ekran ekran ilerlemek daha iyi sonuç verir: önce "Genel stil" + bir ekran, beğenilirse aynı
oturumda diğer ekranlar.

---

```text
Design a responsive web app called "NeoBank – Kişisel Finans" (desktop first, must also work at 375px mobile width).
All UI text must be in Turkish. Use Turkish number and date formats: ₺15.000,00 and 7 Kasım 2026.

PRODUCT
A personal finance planner, not a spending tracker. It shows the user's FUTURE cash flow: salary, rent, bills,
subscriptions, credit card statements and installments for the next 3–12 months, and lets them compare installment
options before a purchase. It never gives financial advice or recommendations; it only makes the user's own obligations
visible. The tone is calm, trustworthy and clear — a bank, not a game.

GENERAL STYLE
- Light theme. Off-white page background (#f6f8fc), white rounded cards (24px radius, 1px very light border, soft shadow).
- Font: Inter. Bold, compact headings; numbers in tabular figures.
- Primary color blue (#2563eb). Neutral slate greys for text.
- Red is reserved ONLY for "balance goes below zero" and must always appear with a warning icon and text, never color alone.
- Chart series colors in this fixed order: blue #2a78d6, orange #eb6834, aqua #1baf7a, yellow #eda100, pink #e87ba4, green #008300.
  Thin 2px lines, bars at most 24px wide with 4px rounded tops, light horizontal gridlines, no 3D, no gradients in charts.
- Generous whitespace, 8px spacing grid, accessible contrast, visible focus states, large touch targets on mobile.
- Every list needs an empty state (one sentence telling the user what to do next) and every page a loading and an error state.

LAYOUT
Left sidebar on desktop (logo "NeoBank", menu, user name and "Çıkış" at the bottom); on mobile the menu becomes a horizontally
scrollable strip at the top. Menu items with icons:
Ana Sayfa · Nakit akışı · Taksit senaryosu · Gelir ve giderler · Kartlar ve taksitler · İşlem geçmişi · Analiz

SCREENS

1. Giriş (login) and Kayıt (register)
   Split screen: brand panel on the left, form on the right. Login fields: "T.C. Kimlik No", "Şifre", button "GİRİŞ YAP",
   link "Kayıt Ol". Show a green success notice ("Hesabınız oluşturuldu. Giriş yapabilirsiniz.") and a session-expired
   info notice variant. Register fields: Ad, Soyad, T.C. Kimlik No, E-posta, Telefon, Şifre, membership toggle
   "Bireysel / Kurumsal", button "KAYIT OL". Field-level error messages under inputs.

2. Ana Sayfa (dashboard)
   Greeting "Merhaba, Ayşe". Big total figure "Toplam varlık (TL hesaplar)". Horizontal row of bank cards (card type,
   **** **** **** 4242, balance, Sanal/Fiziksel tag). "Son işlemler" list (icon in/out, title, category · date, amount,
   green for incoming). "Hesaplarım" column: account type, balance in its own currency, IBAN, status badge "Aktif".

3. Nakit akışı (the most important screen)
   Header with segmented control "3 ay / 6 ay / 12 ay".
   Three stat tiles: "Başlangıç bakiyesi", "Dönemin en düşük bakiyesi" (with date; red + warning icon when negative),
   "Bakiyenin eksiye düştüğü ay: 2 / 6" with subtitle "Ay sonu artıda olsa bile ay içinde".
   Card "Tahmini bakiye": a step line chart of the daily balance with a thin red zero line.
   Card "Aylık gelir ve çıkış": grouped bars (blue = Gelir, orange = Çıkış) per month.
   Card "Aylık özet": table with columns Ay, Gelir, Çıkış, Kart ödemeleri, Net, Ay sonu, Ay içi en düşük, Yük oranı, Durum
   (Durum shows "⚠ Eksiye düşüyor" in red or "Sorun yok" in grey).
   Card "Hareketler": dated list (date, description, type badge Gelir/Gider/Abonelik/Kart ekstresi, amount). A credit card
   statement row expands to show its lines, e.g. "Laptop (1/3) ₺333,34", "Netflix ₺229,99".

4. Taksit senaryosu
   Form card: Kart (select), "Ne alacaksınız?", Alışveriş tarihi, and a dynamic list of options, each row
   "[3] taksit, [9.000] ₺ toplam" with "Kaldır"; links "+ Seçenek ekle" and "İlk fiyatı tüm seçeneklere kopyala";
   button "Karşılaştır". Helper text: prices may differ per option.
   Result card "Karşılaştırma": comparison table, first row "Almasam" (grey), then one row per option with a color swatch:
   Aylık taksit, Toplam, Fiyat farkı, En yüksek aylık çıkış, En düşük bakiye (+date), Eksiye düşülen ay (⚠ 5 ay in red),
   Dönem sonu bakiye. Options are NOT ranked and no option is highlighted as best.
   Chart card "Aylara göre en düşük bakiye": one line per option plus a grey baseline, red zero line, legend.
   Footer info note: "Bu karşılaştırma bir öneri değildir…".

5. Gelir ve giderler
   Filter tabs: Tümü · Gelir · Gider · Abonelik. Button "Kalem ekle". List rows: type badge (green Gelir, grey Gider,
   violet Abonelik), name, "Konut · Her ay · Başlangıç 1 Oca 2026 · Bonus kartından", amount, "Düzenle", "Sil".
   Inline add/edit form card: Ad, Tür, Kategori, Tutar, Para birimi, Sıklık, Her kaç dönemde bir, Başlangıç,
   Bitiş (isteğe bağlı), Ödeme kaynağı ("Hesaptan (kendi tarihinde)" or "Bonus (son ödeme gününde)") — hidden for Gelir.

6. Kartlar ve taksitler
   Section "Kartlarım": card tiles (name, "Kesim günü: 20 · Son ödeme: kesimden 10 gün sonra", limit), add/edit form.
   Section "Taksitli alışverişler": rows (description, card · date · 3 taksit, total, "Takvim", "Sil"); "Takvim" expands a
   small table # / Ekstre / Son ödeme / Tutar.

7. İşlem geçmişi
   Upload card: primary button "CSV ekstre yükle", secondary "12 aylık demo verisi oluştur", one line of format help.
   Result notice: "120 satır işlendi: 117 yeni işlem eklendi, 3 satır zaten kayıtlıydı." Error notice variant with a
   bullet list "Satır 3: Tarih okunamadı…".
   List card: date filters (Başlangıç, Bitiş, Filtrele), "540 işlem" count, rows (date, description, category badge,
   amount), pager "Önceki 1 / 11 Sonraki".

8. Analiz
   Four stacked cards:
   - "Kategorilere göre harcama": horizontal bar chart of totals + month-by-month table with Pay and Son ay change.
   - "Düzenli ödemeler": rows "NETFLIX.COM · Sabit tutarlı · ayın 5. günü civarı · 12 ayda görüldü · sıradaki: 5 Nis 2026",
     amount, and either a green "Takipte" badge or a "Takibe al" link.
   - "Olağandışı harcamalar": rows "TEKNOSA · 18 Eki 2025 — Tüm harcamalar için tipik tutarın (612 ₺) 24 katı", amount.
   - "Önümüzdeki 3 ayın günlük harcama tahmini": three tiles "Nisan 2026: ₺14.800 – ₺21.300, beklenen ₺18.050",
     explanatory text, and a collapsible "Yöntemler geçmişte ne kadar isabetliydi?" table.

COMPONENTS TO KEEP CONSISTENT
Page header (title + one-line subtitle + actions), stat tile, notice (error/info, optional bullet list), empty state,
primary/secondary/danger buttons, text link button, form field (label above, error below), badge, data table,
chart card.
```
