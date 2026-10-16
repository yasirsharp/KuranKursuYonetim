# 🔧 Backend Roadmap

> **.NET 8 · N-Tier Architecture · REST API**
> Hafız Takip projesinin Backend (C# .NET Core) katmanı için geliştirme haritası ve işlem öncelikleri.

---

## 📋 İşlem Öncelikleri

| Aşama | Görev | Öncelik | Not |
|-------|-------|:-------:|-----|
| 1 | Proje İskeleti (N-Tier) | 🔴 P0 | API, Business, DataAccess, Entities, Core katmanlarının kurulması |
| 1 | DbContext & Global Filter | 🔴 P0 | EF Core kurulumu, `ITenantEntity` arayüzü ve global query filter entegrasyonu |
| 1 | Hata Yakalama (Middleware) | 🔴 P0 | `ExceptionHandlingMiddleware` ve ortak response (`IResult`) yapıları |
| 2 | JWT ve Auth Katmanı | 🔴 P0 | Token üretimi, OTP entegrasyonu (NetGSM/Twilio) |
| 2 | Tenant Middleware | 🔴 P0 | HTTP isteğindeki token'dan `tenant_id`'yi çıkarıp içeriğe enjekte etme |
| 3 | Tenant & Kullanıcı CRUD | 🟠 P1 | Kurs oluşturma, hoca ekleme, `tenant_admin` yetki işleri |
| 4 | Öğrenci & Grup CRUD | 🟠 P1 | Öğrenci yaşam döngüsü ve sınıf yönetimi |
| 5 | Ders & Tekrar Modülü | 🟠 P1 | İş mantığı en ağır yer. Atomic işlem ve sayfa hesaplamaları |
| 6 | Yoklama Modülü | 🟠 P1 | Toplu giriş, devamsızlık sınırı algoritmaları |
| 7 | Dashboard & İstatistik | 🟡 P2 | Veriyi özetleyen ve formatlayan aggregation endpointleri |
| 8 | Veli Bildirimi | 🟡 P2 | WhatsApp entegrasyon servisi ve asenkron loglama |
| 13 | PDF/Excel Raporlama | 🟢 P3 | Rapor veri endpointleri |
| 14 | Super Admin Modülü | 🔵 P4 | Cross-tenant sorgulama yetkisi olan özel servisler |

---

## 🚀 Aşama Aşama Geliştirme Planı

### Aşama 1: Çekirdek Altyapı ve Veri Erişimi (🔴 P0) - [BÜYÜK ORANDA TAMAMLANDI]
- **Görevler:**
  - Solution ve 5 projenin (API, Core, Entities, DataAccess, Business) oluşturulması.
  - Entity sınıflarının yazılması (`ITenantEntity` ve `BaseEntity` kalıtımları).
  - EF Core `DbContext` yapılandırması (Kayıtlar `database/00_full_schema.sql` SQL Script-First ile yapılıyor, Migration YASAKTIR).
  - `GenericRepository` deseninin uygulanması.
  - Standardize edilmiş dönüş tiplerinin (`IDataResult<T>`) tanımlanması.
- **Kritik Noktalar:**
  - Katmanlar arası bağımlılıklara (API -> Business -> DataAccess) kesinlikle dikkat edilecek. API asla veritabanına doğrudan gitmeyecek.
  - Core katmanında DataAccess katmanı için temel repository bulunacağı için User, Tenant, Subscription, Invoice entity sınıfları Core katmanında entity clasörü altında bulunacak.

### Aşama 2: Güvenlik, Kimlik Doğrulama ve Multi-Tenant (🔴 P0)
- **Görevler:**
  - `TenantResolutionMiddleware` yazılarak Auth token'dan `tenant_id` alınacak.
  - Rol hiyerarşisine dayalı Policy bazlı yetkilendirmeler eklenecek.
  - OTP gönderimi, doğrulaması ve JWT token yaratılmasını üstlenen `AuthService` kodlanacak.
- **Kritik Noktalar:**
  - Tenant çözülmeden geçilen hiçbir HTTP isteği Business katmanına veri sağlamamalı. Güvenlik açığı yaratır.

### Aşama 3: Temel Operasyonlar (Öğrenci, Ders, Yoklama) (🟠 P1)
- **Görevler:**
  - `StudentsController`, `LessonsController` ve `AttendanceController` oluşturulacak.
  - İlgili servis (Business) sınıflarında DTO - Entity dönüşümleri ve FluentValidation kuralları işletilecek.
  - Özellikle "Ders Kaydı" sonrasında öğrencinin ezber konumunu (cüz/sayfa/sure) hesaplayıp güncelleyen mantık `LessonService` içinde transactional olarak çözülecek.
- **Kritik Noktalar:**
  - Validasyonlar API Controller seviyesinde değil, Business katmanında/FluentValidation ile yapılacak.
  - Toplu işlemler (Bulk insert) için veritabanı performanslı özel endpoint'ler yazılacak.

### Aşama 4: Raporlama ve İleri Seviye Özellikler (🟡 P2 / 🟢 P3)
- **Görevler:**
  - Akşam etüdü ve sınav modülleri.
  - Dashboard için gerekli özet istatistik servisleri (N+1 problemi olmadan).
  - Veli bilgilendirmeleri için arka planda (örneğin Hangfire veya BackgroundService ile) çalışan entegrasyonlar.

### Aşama 5: Platform Yönetimi (🔵 P4)
- **Görevler:**
  - `SuperAdmin` rollü kullanıcılar için tenant filtresini devre dışı bırakan, global abonelikleri takip eden özel uç noktalar hazırlanacak.

---

## 📌 Bekleyen Kontroller (TODO)

Proje sahibinin daha sonra ele alacağı, canlıya çıkmadan önce kapatılması gereken işler:

| # | Konu | Ne yapılacak | Ne zamana kadar | İlgili dosyalar |
|---|------|--------------|-----------------|-----------------|
| 1 | Sure başlangıç sayfaları | Bir hocanın Türkiye ayetberkenar mushafından 114 surenin başlangıç sayfasını kontrol etmesi; farklar `02_quran_reference.sql`'e işlenip `db.ps1 -Migrate` çalıştırılır. Cüz eşlemesi formüle dayandığı için kesindir. | Ezber haritası / akıllı tekrar (Sprint 6–7) canlıya çıkmadan | `database/KURAN_REFERANS_KONTROL.md`, `database/02_quran_reference.sql` |
| 2 | NetGSM SMS entegrasyonu | NetGSM hesabı açılıp onaylı mesaj başlığı alınacak; kullanıcı kodu, şifre ve başlık ortam değişkeniyle verilip gerçek gönderim uçtan uca denenecek. Geliştirme ortamı SMS yerine konsol kullanır. | Canlıya çıkış (Sprint 5 sonrası) | `backend/src/HafizTakip.Core/Utilities/Sms/NetGsmSmsSender.cs`, `appsettings.json` → `Sms` |

---

## ⚠️ Dikkat Edilecekler
1. **Asenkron Programlama:** Tüm veritabanı ve I/O işlemleri `async/await` kullanılarak asenkron çalışmalı, `.Result` yasaktır.
2. **N+1 Problemi:** LINQ sorgularında `Include` kullanımları optimize edilmeli.
3. **DTO Kullanımı:** Entity nesneleri doğrudan API üzerinden dönülmemeli, araya DTO (Data Transfer Object) konulmalıdır.
4. **Tenant Sistemi:** Multi-tenant SaaS yapısı nedeniyle her sorguda `tenant_id` kontrolü zorunludur.
5. **API Design:** API tasarımı RESTful prensiplerine uygundur; GET/POST/PUT/PATCH/DELETE kullanılır (bkz. `DEVELOPER_RULES.md` §4). Barındırma ortamı PUT/PATCH/DELETE isteklerini engellerse (bazı paylaşımlı IIS sunucularında WebDAV modülü nedeniyle görülür), istemci `POST` + `X-HTTP-Method-Override` header'ı gönderir ve backend'de `UseHttpMethodOverride` middleware'i açılır. Endpoint tasarımı bu nedenle değiştirilmez.
