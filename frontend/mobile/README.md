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

Her `frontend/mobile` değişikliğinde GitHub Actions'taki **Mobil APK** iş akışı analiz + test çalıştırır ve test sunucusunun API'sine (`https://hafiztakip.api.yasirsharp.com.tr`) bağlı bir APK üretir. APK ayrıca `mobil-test` GitHub sürümüne `hafiz-takip.apk` adıyla yüklenir; hocalar web sitesindeki **/mobil** sayfasından (giriş ekranında "📱 Telefon uygulamasını indir") indirip adım adım kurar. Çalıştırma sayfasının **Artifacts** bölümünden de indirilebilir. Elle başlatmak için: Actions → Mobil APK → Run workflow (farklı bir API adresi verilebilir).

Test sunucusunda e-posta gönderilmez; giriş kodu **123456**. Deneme hesapları `database/01_seed_data.sql` içinde (ör. `ahmet.hoca@merkezkurs.com`, `mudur@merkezkurs.com`).

### İmza anahtarı

Yeni sürümün eskisinin üstüne kurulabilmesi için her APK aynı anahtarla imzalanmalıdır. Anahtar bir kez üretilir:

```powershell
pwsh ./frontend/mobile/imza-anahtari-olustur.ps1
```

Betik anahtarı depo dışına (Belgeler\HafizTakip-imza) yazar, var olanın üzerine yazmaz ve GitHub'a eklenecek iki gizli bilgiyi sırayla panoya kopyalar: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD` (Settings → Secrets and variables → Actions). Klasörü yedekleyin: anahtar kaybolursa uygulamayı kuran herkesin silip yeniden kurması gerekir. Gizli bilgiler yoksa iş akışı uyarı verip geçici (debug) anahtarla imzalar. Her derlemenin imza parmak izi `mobil-test` sürüm notunda yazar.

## Geliştirme

```bash
flutter pub get
flutter analyze
flutter test --concurrency=1
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000   # Android emülatöründen yerel backend
```

Derleme ayarları (`--dart-define`): `API_BASE_URL` (varsayılan test sunucusu), `SHOW_TEST_HINTS=false` (giriş ekranındaki test kodu notunu gizler).

Kurallar için `DEVELOPER_RULES.md`, yol haritası için `ROADMAP.md`.
