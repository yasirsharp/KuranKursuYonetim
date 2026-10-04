# 📱 Mobile Frontend Roadmap

> **Flutter · Dart · Riverpod · Dio**
> Hafız Takip projesinin Mobil uygulaması (Android & iOS) için geliştirme haritası ve işlem öncelikleri.

---

## 📋 İşlem Öncelikleri

| Aşama | Görev | Öncelik | Not |
|-------|-------|:-------:|-----|
| 1 | Proje İskeleti (Clean Arch.) | 🔴 P0 | Domain, Data, Presentation katmanları, Tema ayarları |
| 1 | Riverpod & Dio Kurulumu | 🔴 P0 | Interceptorler, token saklama, bağımlılık yönetimi (DI) |
| 2 | Auth Ekranları & Güvenlik | 🔴 P0 | Telefon + OTP ekranı, Token'ı SecureStorage'a yazma |
| 3 | Kurs Ayarları & Kadro | 🟠 P1 | Kurs bilgilerini düzenleme, hoca davet ekranları |
| 4 | Öğrenci CRUD | 🟠 P1 | Form validasyonları, liste cacheleme, ilerleme gösterimi |
| 5 | Ders & Tekrar Ekranları | 🟠 P1 | UX odaklı hızlı form girişleri. Offline öncelikli kayıt! |
| 6 | Devamsızlık Grid | 🟠 P1 | Tüm sınıfın pratik şekilde yoklamasının alınması |
| 7 | Offline Kuyruk Sistemi | 🟡 P2 | Hive veritabanı, bağlantı kontrolü ve arkaplan sync mekanizması |
| 8 | Dashboard | 🟡 P2 | Hızlı metrikler, bekleyen işler (etüt vs.) listesi |
| 9 | Veli Modülü & İletişim | 🟡 P2 | `url_launcher` ile WhatsApp entegrasyonu, bildirim merkezleri |
| 13 | Raporlar (Grafikler) | 🟢 P3 | `fl_chart` ile görsel raporlama ekranları |
| 15 | Yayın Hazırlıkları | 🟢 P3 | İmzalama, Store kuralları, İkon / Splash Screen hazırlıkları |

---

## 🚀 Aşama Aşama Geliştirme Planı

### Aşama 1: Çekirdek Kurulum (🔴 P0)
- **Görevler:**
  - Yeni bir Flutter projesi yaratılması.
  - Proje mimarisinin `core/`, `data/`, `presentation/`, `router/` klasörleriyle oluşturulması.
  - Tema (`AppTheme`, `AppColors`) web tarafıyla birebir aynı görünümde olacak şekilde kodlanması.
  - Riverpod, Dio, GoRouter ve diğer gerekli paketlerin `pubspec.yaml` dosyasına eklenmesi.
  - Ortak widget'ların (`AppButton`, `AppCard`, `LoadingSkeleton`) tasarlanması.

### Aşama 2: API İstekleri ve Kimlik Doğrulama (🔴 P0)
- **Görevler:**
  - JSON modellerinin (`freezed` veya `json_serializable`) oluşturulması.
  - Dio istemcisi üzerine Token, Hata yakalama (Loglama) interceptor'larının yazılması.
  - OTP giriş süreci, dönen JWT token'ın `flutter_secure_storage` ile cihazda güvenle saklanması.
  - Login durumuna göre GoRouter yönlendirmeleri.

### Aşama 3: Çekirdek Fonksiyonlar (🟠 P1)
- **Görevler:**
  - Öğrenci, Ders, Tekrar ve Yoklama ekranlarının kodlanması.
  - Öğretmenlerin günlük operasyonlarını sahadayken en hızlı yapabileceği şekilde kaydırma (swipe), dokunma (tap) bazlı optimize edilmiş UI deneyimi tasarlanması.
  - Kamera entegrasyonu ile öğrenci fotoğrafı eklenebilmesi (`image_picker`).

### Aşama 4: Offline-First Deneyim (🟡 P2)
- **Görevler:**
  - Veritabanı önbelleği için Hive/Isar kullanılması. Öğrenci listeleri gibi sık okunan verilerin bellekte tutulması.
  - **Offline Queue:** İnternet yokken yapılan ders kayıtlarının/yoklamaların kuyruğa alınması. İnternet gelir gelmez arka planda (`ConnectivityService` dinlenerek) sunucuyla senkronize edilmesi.

### Aşama 5: Veli ve Ekstra Modüller (🟡 P2 / 🟢 P3)
- **Görevler:**
  - WhatsApp entegrasyonları için Native URL açma eylemleri.
  - Sınav, Etüt ve Veli Portal (aynı uygulama üzerinden auth state'ine göre gösterilebilir) modülleri.
  - `fl_chart` ile raporların gösterimi.

---

## ⚠️ Dikkat Edilecekler
1. **Offline Yapı:** Mobil uygulama genellikle internet bağlantısı kısıtlı kurs binalarında kullanılabilir. Offline çalışma (cache okuma ve işlem kuyruklama) yeteneği kritik bir gereksinimdir.
2. **State Yönetimi:** Riverpod'da veriler olabildiğince parçalı çekilmeli. `ref.watch` kullanımları en alt kırılımda (örnek: sadece değişen text alanında) yapılarak gereksiz ekran yenilenmeleri önlenmeli.
3. **Senkronizasyon:** Çakışmaları çözmek için `updated_at` kontrolü (last-write-wins yaklaşımı) uygulanacak.
