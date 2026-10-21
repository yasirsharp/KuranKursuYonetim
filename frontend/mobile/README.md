# Hafız Takip — Mobil (Flutter)

Hocalar ve kurs müdürleri için Android/iOS uygulaması. Aynı backend'i kullanır (`/api/...`).

## Bu sürümde

- E-posta + tek kullanımlık kod ile giriş (adres birden çok kursta kayıtlıysa kurs seçimi), oturum cihazda güvenli depoda saklanır ve kendiliğinden yenilenir.
- **Öğrencilerim:** bugün kimin dinlettiği, hızlı ders kaydı (sayfa önerili, 5 performans düğmesi, kaydedince sıradaki öğrenci açılır).
- **Yoklama:** herkes "Geldi" başlar, dokundukça Gelmedi → Geç → İzinli; aynı gün tekrar açılıp düzeltilebilir.
- **Öğrenci detayı:** ilerleme, son dersler, son yoklamalar.
- Veli/öğrenci rolleri ve onay bekleyen kurslar için bilgi ekranı.

Henüz yok: bağlantısız kayıt kuyruğu, bildirimler, müdür özeti (bkz. yol haritası, "Mobil plan" sekmesi).

## Test APK'sı

Her `frontend/mobile` değişikliğinde GitHub Actions'taki **Mobil APK** iş akışı analiz + test çalıştırır ve test sunucusuna (`https://hafiz.yasirsharp.com.tr`) bağlı bir APK üretir. APK, çalıştırmanın sayfasındaki **Artifacts** bölümünden indirilir. Elle başlatmak için: Actions → Mobil APK → Run workflow (farklı bir API adresi verilebilir).

Test sunucusunda e-posta gönderilmez; giriş kodu **123456**. Deneme hesapları `database/01_seed_data.sql` içinde (ör. `ahmet.hoca@merkezkurs.com`, `mudur@merkezkurs.com`).

Test APK'ları debug anahtarıyla imzalanır; yeni APK kurulmazsa önce eski uygulamayı kaldırın.

## Geliştirme

```bash
flutter pub get
flutter analyze
flutter test --concurrency=1
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000   # Android emülatöründen yerel backend
```

Derleme ayarları (`--dart-define`): `API_BASE_URL` (varsayılan test sunucusu), `SHOW_TEST_HINTS=false` (giriş ekranındaki test kodu notunu gizler).

Kurallar için `DEVELOPER_RULES.md`, yol haritası için `ROADMAP.md`.
