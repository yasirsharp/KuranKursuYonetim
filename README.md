# ☪️ Hafız Takip

> Kur'an kursu öğreticileri için çok kiracılı (multi-tenant) SaaS yönetim platformu.  
> Her kurs kendi bağımsız ortamında çalışır; ders, tekrar, devamsızlık, veli iletişimi  
> ve etüt koordinasyonu tek platformda yönetilir.

---

## İçindekiler

1. [Proje Tanımı](#1-proje-tanımı)
2. [Çözülen Problem](#2-çözülen-problem)
3. [Kullanıcı Rolleri & Hiyerarşi](#3-kullanıcı-rolleri--hiyerarşi)
4. [Tam Özellik Listesi](#4-tam-özellik-listesi)
5. [Multi-Tenant Mimari](#5-multi-tenant-mimari)
6. [Veri Modeli](#6-veri-modeli)
7. [Ekran Haritası](#7-ekran-haritası)
8. [Geliştirme Roadmap'i](#8-geliştirme-roadmapi)
9. [Teknoloji & Araçlar](#9-teknoloji--araçlar)
10. [Kurulum](#10-kurulum)
11. [Geliştirme Notları](#11-geliştirme-notları)

---

## 1. Proje Tanımı

**Hafız Takip**, birden fazla Kur'an kursunu tek platformdan yönetebilen,  
çok kiracılı (multi-tenant) bir SaaS uygulamasıdır.

Her kurs bağımsız bir **kiracı (tenant)** olarak sisteme dahil olur.  
Veriler, ayarlar ve kullanıcılar kurslar arasında tamamen izole çalışır.  
Her kursun kendi yöneticisi, hocaları, öğrencileri ve özel ayarları bulunur.

**Platform hedefi:** Küçük bir mahalle Kur'an kursundan çok şubeli  
büyük bir hafızlık merkezine kadar her ölçekte çalışmak.

```
Platform (Hafız Takip SaaS)
├── Kurs A — İstanbul Fatih Kur'an Kursu
│   ├── Yönetici: Ahmet Hoca
│   ├── 4 Öğretmen, 48 Öğrenci
│   └── Kendi ayarları, verileri, planı
├── Kurs B — Ankara Keçiören Hafızlık Merkezi
│   ├── Yönetici: Fatma Hanım
│   ├── 2 Öğretmen, 22 Öğrenci
│   └── Kendi ayarları, verileri, planı
└── Kurs C — Düzce Kur'an Kursu
    └── ...
```

---

## 2. Çözülen Problem

| Süreç | Mevcut Durum | Platform Sonrası |
|-------|-------------|-----------------|
| Yeni sayfa / ders kaydı | Kağıt defter | Anlık dijital kayıt |
| Sabah–öğle–akşam tekrar | Bellek / defter | Oturum bazlı kayıt |
| Ödev & devamsızlık kontrolü | Manuel liste | Otomatik filtreleme & uyarı |
| Veli bilgilendirme | Manuel SMS/WA | Şablon + tek tıkla gönderim |
| Etüt koordinasyonu | Sözlü iletişim | Dijital not sistemi |
| İlerleme haritası | Yok | Sayfa bazlı görsel takip |
| Çoklu kurs yönetimi | Ayrı ayrı defterler | Tek platform, izole kiracılar |
| Hoca performans takibi | Yok | İstatistik & raporlama |
| Ücret / kayıt takibi | Kağıt makbuz | Dijital ödeme logu |
| Duyuru & iletişim | Telefon zinciri | Merkezi duyuru sistemi |

---

## 3. Kullanıcı Rolleri & Hiyerarşi

```
🌐 Platform Super Admin
│   Platform genelini yönetir. Kiracıları, abonelikleri,
│   faturalamaları ve sistem ayarlarını görür.
│
└── 🏫 Kurs Yöneticisi (Tenant Admin)
    │   Kendi kursunu tam yetkiyle yönetir. Hoca ekler,
    │   ayarları düzenler, raporlara erişir.
    │
    ├── 👨‍🏫 Baş Hoca
    │   │   Öğrencilere ders kaydeder, etüt notu bırakır,
    │   │   veli bildirimi gönderir, yardımcı hocaları yönetir.
    │   │
    │   └── 🧑‍🏫 Yardımcı Hoca
    │           Etüt notlarını görür, tamamlandı işaretler,
    │           öğrenci ders geçmişine okuma erişimi vardır.
    │
    ├── 👪 Veli
    │       Yalnızca kendi çocuğunun raporunu görür.
    │       Devamsızlık, ders özeti, etüt bilgisi.
    │
    └── 📖 Öğrenci (İleri Aşama)
            Kendi ders geçmişini ve ilerleme haritasını görür.
```

### Rol Bazlı İzin Tablosu

| Özellik | Super Admin | Kurs Yöneticisi | Baş Hoca | Yrd. Hoca | Veli |
|---------|:-----------:|:---------------:|:--------:|:---------:|:----:|
| Kurs oluştur / sil | ✅ | ❌ | ❌ | ❌ | ❌ |
| Hoca ekle / çıkar | ❌ | ✅ | ❌ | ❌ | ❌ |
| Öğrenci ekle / düzenle | ❌ | ✅ | ✅ | ❌ | ❌ |
| Ders kaydı | ❌ | ✅ | ✅ | ❌ | ❌ |
| Tekrar oturumu | ❌ | ✅ | ✅ | ✅ | ❌ |
| Etüt notu ekle | ❌ | ✅ | ✅ | ❌ | ❌ |
| Etüt notu gör | ❌ | ✅ | ✅ | ✅ | ❌ |
| Veli bildirimi | ❌ | ✅ | ✅ | ❌ | ❌ |
| Tüm raporlar | ❌ | ✅ | ✅ | ❌ | ❌ |
| Kendi çocuğu | ❌ | ❌ | ❌ | ❌ | ✅ |
| Abonelik yönetimi | ✅ | ✅ | ❌ | ❌ | ❌ |
| Platform analytics | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## 4. Tam Özellik Listesi

---

### 4.1 Platform & Kiracı Yönetimi *(Super Admin)*

#### Kurs (Tenant) Onboarding
- Yeni kurs kaydı: kurs adı, şehir, adres, iletişim bilgisi
- Kurs logosu ve tema rengi yükleme
- Abonelik planı atama (Başlangıç / Standart / Profesyonel)
- Deneme süresi başlatma (14 gün ücretsiz)
- Kurs durum yönetimi: aktif / askıya alınmış / iptal edilmiş

#### Abonelik & Faturalama
- Plan yönetimi: öğrenci sayısı ve öğretmen sayısı bazlı limitler
- Aylık / yıllık abonelik seçimi
- Plan yükseltme / düşürme
- Fatura geçmişi ve PDF fatura
- Ödeme entegrasyonu (İyzico / Stripe)
- Gecikme bildirimi ve otomatik askıya alma

#### Platform Analytics *(Super Admin)*
- Toplam kiracı, aktif kullanıcı, günlük işlem sayısı
- MRR / ARR (aylık / yıllık yinelenen gelir)
- En aktif kurslar, büyüme grafikleri
- Süresi dolan abonelik uyarıları
- Sistem sağlık durumu (uptime, hata oranı)

---

### 4.2 Kurs İç Yönetimi *(Kurs Yöneticisi)*

#### Kurs Ayarları
- Kurs adı, logosu, iletişim bilgisi düzenleme
- Çalışma günleri ve saatleri tanımlama
- Resmi tatil ve özel tatil günleri ekleme
- Akşam etüdü saati ve lokasyonu
- Bildirim tercihleri (WhatsApp / SMS / e-posta)

#### Öğretmen Yönetimi
- Öğretmen daveti: telefon veya e-posta ile
- Rol atama: Baş Hoca / Yardımcı Hoca
- Öğretmen profili: ad, iletişim, uzmanlık alanı
- Öğretmen pasife alma / kurstan çıkarma
- Öğretmen ders saati ve öğrenci dağılımı raporu
- Öğretmen bazlı performans özeti

#### Sınıf & Grup Yönetimi
- Sınıf / grup oluşturma (Sabah Grubu, Hafızlık Sınıfı vb.)
- Öğrencileri gruba atama
- Gruba öğretmen atama
- Grup bazlı toplu ders girişi
- Grup değiştirme geçmişi

---

### 4.3 Öğrenci Yönetimi

#### Kayıt & Profil
- Ön kayıt formu (veliden dijital doldurulan)
- Kesin kayıt onaylama
- Öğrenci profili: ad, doğum tarihi, fotoğraf, TC kimlik no (isteğe bağlı)
- Veli bilgileri: anne adı, baba adı, birden fazla iletişim numarası
- Sağlık bilgisi notu (alerji, ilaç vb.)
- Ulaşım bilgisi (servise biniyor mu, durak)
- Öğrenci numarası / barkod

#### Hafızlık Seviye & İlerleme
- Başlangıç noktası: sure, sayfa, cüz
- Mevcut konum: sure, sayfa, cüz (otomatik güncellenen)
- 604 sayfa üzerinden görsel ilerleme çubuğu
- Cüz tamamlama tarihleri ve kutlama kaydı
- Tahmini bitiş tarihi (mevcut pace'e göre hesaplanmış)
- Geçmiş hız analizi (son 30 gün / 90 gün ortalama sayfa/gün)

#### Arşivleme & Transfer
- Öğrenciyi pasife alma (silme değil, arşivleme)
- Sınıf / grup transferi
- Hoca transferi (geçmiş korunur)
- Mezuniyet işlemi (tüm Kur'an'ı tamamlayanlar)
- Mezuniyet sertifikası oluşturma

---

### 4.4 Hafızlık Ders Takibi *(Kur'an Kursu'na özgü)*

#### Günlük Ders Kaydı
- **Yeni ders (Sabak):** Verilen yeni sayfa / ayet aralığı
- **Dünün dersi tekrarı (Sabki):** Bir gün önceki dersin tekrar kalitesi
- **Genel tekrar (Manzil):** Uzun vadeli tekrar edilmiş sayfalar
- Sayfa / ayet bazında kayıt (sadece sayı değil)
- Ders süresi (dakika bazında, isteğe bağlı)
- Ödev durumu: yapıldı / eksik / yapılmadı / mazeret
- Performans değerlendirmesi: Mükemmel / Çok İyi / İyi / Orta / Zayıf
- Hata türü notu: mahreç, med, tenvin, vakf, id'gam vb.
- Serbest not alanı
- Bir günde birden fazla öğrenci için hızlı toplu giriş modu

#### Tekrar Oturumu Takibi
- Sabah / öğle / akşam oturumu ayrı kaydı
- Oturum bazında sayfa aralığı (başlangıç – bitiş)
- Tekrar sayısı
- Kalite puanı (1–5)
- Öğrencinin kendi değerlendirmesi (ileride)

#### Mutashabihat (Benzer Ayetler) Takibi
- Karıştırılan benzer ayet çiftlerini işaretleme
- Mutashabihat geçmişi ve tekrar uyarısı
- En çok karıştırılan ayetler raporu

---

### 4.5 Devamsızlık Takibi

- Günlük yoklama: var / yok / geç geldi / erken ayrıldı
- Mazeret girişi: hastalık, aile, diğer
- Devamsızlık limiti uyarısı (özelleştirilebilir eşik)
- Devamsızlık otomatik veli bildirimi
- Aylık devamsızlık raporu
- Takvim görünümü (yeşil / kırmızı / sarı hücreli)
- Toplu yoklama ekranı (sınıf listesi, bir tıkla işaretle)

---

### 4.6 Sınav & Değerlendirme

- Sınav oluşturma: hafızlık sınavı, tecvid sınavı, okuma sınavı
- Sınav takvimi ve hatırlatma
- Sınav not girişi (100 üzerinden veya özelleştirilebilir)
- Sınav sonuç raporu ve sıralama
- Bekleme altındaki / başarısız öğrenci uyarısı
- Geçmiş sınav arşivi
- Sertifika oluşturma (cüz tamamlama, yıl sonu)

---

### 4.7 Veli İletişim & Bildirim

#### Veli Portalı
- Veliye özel giriş (telefon OTP)
- Yalnızca kendi çocuğunun verileri görünür
- Günlük ders özeti: yeni sayfa, tekrar, ödev durumu
- Devamsızlık takvimi
- Hoca notları
- Etüt bilgisi (bugün var mı, saati)
- Sınav sonuçları

#### Bildirim Sistemi
- Günlük otomatik rapor mesajı (WhatsApp / SMS)
- Devamsızlık anında bildirimi
- Ödev eksikliği uyarısı
- Sınav sonucu bildirimi
- Kurs genel duyuruları
- Bayram ve özel gün mesajı şablonları
- Bildirim gönderim logu (gönderildi / görüldü / hata)

#### WhatsApp Entegrasyonu
- Kısa dönem: manuel deep-link şablonları
- Orta dönem: WhatsApp Business API (onaylı şablon mesajlar)
- Uzun dönem: çift yönlü mesajlaşma, veli cevap takibi

---

### 4.8 Akşam Etüdü Koordinasyonu

- Öğrenci bazlı etüt notu oluşturma
- Not içeriği: hangi sayfalar tekrar edilecek, hata odakları
- Yardımcı hoca atama
- Bugünkü etüt listesi (yardımcı hoca görünümü)
- Tamamlandı / tamamlanamadı işaretleme
- Tamamlanmayan notun ertesi güne taşınması
- Etüt geçmişi arşivi
- Etüt saati ve mekan yönetimi

---

### 4.9 Duyuru & İç İletişim

- Kurs geneline duyuru yayınlama
- Hedefli duyuru: belirli sınıf veya velilere
- Duyuru zamanlama (ileri tarihli yayın)
- Duyuru onay akışı (yönetici onayı gerektiren)
- Hoca - yönetici mesajlaşma (dahili)
- Etkinlik duyurusu (hatim töreni, mezuniyet vb.)

---

### 4.10 Raporlama & Analitik

#### Öğrenci Raporları
- Haftalık ilerleme özeti (sayfa, tekrar, devamsızlık)
- Aylık performans raporu (PDF)
- Yıllık ilerleme grafiği
- Mutashabihat hata haritası
- Pace analizi (hız trendi)

#### Sınıf / Kurs Raporları
- Sınıf bazlı ortalama ilerleme
- En hızlı / en yavaş ilerleyen öğrenciler
- Ödev tamamlama oranı trendi
- Devamsızlık oranı raporu
- Hoca bazlı öğrenci başarı dağılımı

#### Yönetici Raporları
- Aylık kurs özeti
- Kayıt / mezuniyet istatistikleri
- Öğretmen ders yükü dengesi
- Veli bildirim etkinlik raporu

#### Dışa Aktarım
- PDF rapor çıktısı
- Excel / CSV dışa aktarım
- Toplu öğrenci verisi dışa aktarma

---

### 4.11 Ücret & Kayıt Yönetimi

- Kayıt ücreti tanımlama (yıllık / dönemsel / aylık)
- Öğrenci ödeme takibi
- Taksit planı oluşturma
- Ödeme hatırlatma bildirimi (veli)
- Ödeme gecikmesi uyarısı (yönetici)
- Burs / ücret muafiyeti tanımlama
- Ödeme logu ve makbuz
- Kasa raporu (gelir / gider özeti)
- Bağışçı / sponsor kaydı (burs veren kişi / kurum)

---

### 4.12 Takvim & Planlama

- Kurs çalışma takvimi
- Resmi tatil ve özel tatil ekleme
- Etüt takvimi
- Sınav takvimi
- Haftalık ders planı görünümü
- Öğretmen ders yükü takvimi
- Telafi dersi planlama
- Takvimi velilerle paylaşma

---

### 4.13 Offline & PWA

- Ders formu internet olmadan çalışır
- Tekrar oturumu offline kaydedilir
- Yoklama offline işaretlenebilir
- İnternet gelince Supabase'e otomatik senkronizasyon
- Çakışma yönetimi (aynı kayıt iki yerden girilmişse)
- Bağlantı durumu göstergesi
- "Ana ekrana ekle" kurulumu
- Uygulama versiyonu ve güncelleme bildirimi

---

### 4.14 Sistem & Güvenlik

- Supabase Auth: telefon OTP ile giriş
- JWT bazlı oturum yönetimi
- Row Level Security: her kiracı yalnızca kendi verisini görür
- Tenant-ID her sorguda zorunlu parametre
- Oturum zaman aşımı ve yenileme
- Rol değişikliği denetim logu
- Kişisel veri silme (KVKK uyumu)
- Veri yedekleme (Supabase otomatik backup)
- İki faktörlü doğrulama (2FA) — ileri aşama

---

## 5. Multi-Tenant Mimari

### Kiracı İzolasyon Modeli

Tüm tablolarda `tenant_id` kolonu bulunur.  
Supabase Row Level Security (RLS) her sorguda tenant filtresi uygular.  
Tek veritabanı, paylaşımlı şema — sektör standardı maliyet-verimli model.

```
Kullanıcı İsteği
      │
      ▼
  Supabase Auth
  (JWT içinde tenant_id)
      │
      ▼
  RLS Policy
  WHERE tenant_id = auth.jwt() → 'tenant_id'
      │
      ▼
  Yalnızca o kiracının verisi döner
```

### Tenant Onboarding Akışı

```
1. Kurs yöneticisi "Kurs Oluştur" formunu doldurur
2. Platform Super Admin onaylar (veya otomatik onaylanır)
3. Supabase'de tenant kaydı oluşturulur
4. Yöneticiye OTP ile giriş linki gönderilir
5. İlk giriş: kurs ayarları, öğretmen daveti
6. 14 gün ücretsiz deneme başlar
7. Deneme sonu: abonelik planı seçilir, ödeme alınır
```

### Abonelik Planları

| Plan | Öğrenci Limiti | Öğretmen Limiti | Özellikler |
|------|:--------------:|:---------------:|------------|
| **Başlangıç** | 30 | 2 | Temel ders takibi, veli WA bildirimi |
| **Standart** | 100 | 5 | + Devamsızlık, etüt, sınav, raporlar |
| **Profesyonel** | Sınırsız | Sınırsız | + Ücret takibi, analytics, PDF raporlar, API erişimi |
| **Kurumsal** | Çoklu şube | Çoklu şube | + Özel alan adı, öncelikli destek, SLA |

---

## 6. Veri Modeli

### Platform Seviyesi Tablolar

```sql
-- ─── Kiracılar (Kurslar) ───────────────────────────────────────────
CREATE TABLE tenants (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name           TEXT NOT NULL,                  -- "İstanbul Fatih Kur'an Kursu"
  slug           TEXT UNIQUE NOT NULL,           -- "fatih-kuran-kursu"
  city           TEXT,
  address        TEXT,
  phone          TEXT,
  email          TEXT,
  logo_url       TEXT,
  theme_color    TEXT DEFAULT '#1B5C3A',
  status         TEXT DEFAULT 'trial',           -- 'trial'|'active'|'suspended'|'cancelled'
  trial_ends_at  TIMESTAMPTZ,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Abonelikler ────────────────────────────────────────────────────
CREATE TABLE subscriptions (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  plan           TEXT NOT NULL DEFAULT 'starter', -- 'starter'|'standard'|'pro'|'enterprise'
  billing_cycle  TEXT DEFAULT 'monthly',          -- 'monthly'|'yearly'
  student_limit  INT DEFAULT 30,
  teacher_limit  INT DEFAULT 2,
  status         TEXT DEFAULT 'active',
  current_period_start TIMESTAMPTZ,
  current_period_end   TIMESTAMPTZ,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Fatura Logu ────────────────────────────────────────────────────
CREATE TABLE invoices (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  amount         NUMERIC(10,2) NOT NULL,
  currency       TEXT DEFAULT 'TRY',
  status         TEXT DEFAULT 'paid',             -- 'paid'|'pending'|'failed'
  issued_at      TIMESTAMPTZ DEFAULT now(),
  pdf_url        TEXT
);
```

### Kiracı Seviyesi Tablolar *(her birinde `tenant_id` zorunlu)*

```sql
-- ─── Kullanıcılar ───────────────────────────────────────────────────
CREATE TABLE users (
  id             UUID PRIMARY KEY REFERENCES auth.users(id),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  name           TEXT NOT NULL,
  phone          TEXT,
  email          TEXT,
  role           TEXT NOT NULL,
    -- 'tenant_admin'|'head_teacher'|'assistant_teacher'|'parent'|'student'
  is_active      BOOLEAN DEFAULT true,
  avatar_url     TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Sınıf / Gruplar ────────────────────────────────────────────────
CREATE TABLE groups (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  name           TEXT NOT NULL,                  -- "Hafızlık Sınıfı A"
  teacher_id     UUID REFERENCES users(id),
  session_time   TEXT,                           -- "Sabah 09:00"
  is_active      BOOLEAN DEFAULT true,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Öğrenciler ─────────────────────────────────────────────────────
CREATE TABLE students (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id       UUID NOT NULL REFERENCES tenants(id),
  teacher_id      UUID REFERENCES users(id),
  group_id        UUID REFERENCES groups(id),
  name            TEXT NOT NULL,
  birth_date      DATE,
  gender          TEXT,
  photo_url       TEXT,
  avatar_color    TEXT DEFAULT '#1B5C3A',
  -- Veli
  parent_name     TEXT,
  parent_phone    TEXT,
  parent_phone2   TEXT,
  parent_email    TEXT,
  parent_user_id  UUID REFERENCES users(id),
  -- Hafızlık konumu
  current_page    INT NOT NULL DEFAULT 1,
  current_juz     INT NOT NULL DEFAULT 1,
  current_surah   TEXT NOT NULL DEFAULT 'Fatiha',
  -- Kayıt
  enrollment_date DATE DEFAULT CURRENT_DATE,
  status          TEXT DEFAULT 'active',          -- 'active'|'archived'|'graduated'
  graduation_date DATE,
  notes           TEXT,
  created_at      TIMESTAMPTZ DEFAULT now()
);

-- ─── Ders Kayıtları ─────────────────────────────────────────────────
CREATE TABLE lessons (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id       UUID NOT NULL REFERENCES tenants(id),
  student_id      UUID NOT NULL REFERENCES students(id),
  teacher_id      UUID NOT NULL REFERENCES users(id),
  lesson_date     DATE NOT NULL,
  -- Sabak (yeni ders)
  new_page_start  INT,
  new_page_end    INT,
  new_pages       INT NOT NULL DEFAULT 0,
  -- Sabki (dünün tekrarı)
  sabki_quality   INT CHECK (sabki_quality BETWEEN 1 AND 5),
  -- Manzil (uzun vadeli tekrar)
  manzil_pages    TEXT,                           -- "50-70, 120-140" serbest metin
  -- Genel
  homework        TEXT NOT NULL DEFAULT 'yapıldı',
  performance     TEXT,
  duration_min    INT,                            -- ders süresi (dakika)
  note            TEXT,
  created_at      TIMESTAMPTZ DEFAULT now(),
  UNIQUE (student_id, lesson_date)
);

-- ─── Tekrar Oturumları ──────────────────────────────────────────────
CREATE TABLE repetition_sessions (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  lesson_id      UUID NOT NULL REFERENCES lessons(id) ON DELETE CASCADE,
  student_id     UUID NOT NULL REFERENCES students(id),
  session_time   TEXT NOT NULL,                   -- 'sabah'|'öğle'|'akşam'
  page_start     INT NOT NULL,
  page_end       INT NOT NULL,
  rep_count      INT NOT NULL DEFAULT 1,
  quality        INT CHECK (quality BETWEEN 1 AND 5),
  note           TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Mutashabihat (Benzer Ayetler) ──────────────────────────────────
CREATE TABLE mutashabihat_notes (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  surah_1        TEXT,
  ayah_1         INT,
  surah_2        TEXT,
  ayah_2         INT,
  note           TEXT,
  is_resolved    BOOLEAN DEFAULT false,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Devamsızlık ────────────────────────────────────────────────────
CREATE TABLE attendance (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  date           DATE NOT NULL,
  status         TEXT NOT NULL,                   -- 'present'|'absent'|'late'|'excused'
  excuse_reason  TEXT,
  note           TEXT,
  created_at     TIMESTAMPTZ DEFAULT now(),
  UNIQUE (student_id, date)
);

-- ─── Etüt Notları ───────────────────────────────────────────────────
CREATE TABLE etut_notes (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  from_teacher   UUID NOT NULL REFERENCES users(id),
  note_date      DATE NOT NULL,
  content        TEXT NOT NULL,
  is_done        BOOLEAN DEFAULT false,
  done_at        TIMESTAMPTZ,
  done_by        UUID REFERENCES users(id),
  carry_over     BOOLEAN DEFAULT false,           -- ertesi güne taşındı mı
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Sınavlar ───────────────────────────────────────────────────────
CREATE TABLE exams (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  name           TEXT NOT NULL,
  exam_type      TEXT,                            -- 'hafizlik'|'tecvid'|'okuma'
  exam_date      DATE NOT NULL,
  group_id       UUID REFERENCES groups(id),
  notes          TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE exam_results (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  exam_id        UUID NOT NULL REFERENCES exams(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  score          NUMERIC(5,2),
  max_score      NUMERIC(5,2) DEFAULT 100,
  notes          TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Veli Bildirim Logu ─────────────────────────────────────────────
CREATE TABLE parent_notifications (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  sent_by        UUID REFERENCES users(id),
  sent_at        TIMESTAMPTZ DEFAULT now(),
  type           TEXT DEFAULT 'daily',            -- 'daily'|'absence'|'exam'|'custom'
  channel        TEXT DEFAULT 'whatsapp',
  body           TEXT,
  status         TEXT DEFAULT 'sent'              -- 'sent'|'delivered'|'failed'
);

-- ─── Duyurular ──────────────────────────────────────────────────────
CREATE TABLE announcements (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  author_id      UUID REFERENCES users(id),
  title          TEXT NOT NULL,
  body           TEXT NOT NULL,
  target         TEXT DEFAULT 'all',              -- 'all'|'parents'|'teachers'|group_id
  publish_at     TIMESTAMPTZ DEFAULT now(),
  expires_at     TIMESTAMPTZ,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Ücret Kayıtları ────────────────────────────────────────────────
CREATE TABLE fee_records (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  student_id     UUID NOT NULL REFERENCES students(id),
  amount         NUMERIC(10,2) NOT NULL,
  due_date       DATE NOT NULL,
  paid_date      DATE,
  status         TEXT DEFAULT 'pending',          -- 'pending'|'paid'|'overdue'|'waived'
  waiver_reason  TEXT,                            -- burs, ihtiyaç vb.
  note           TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);

-- ─── Takvim Etkinlikleri ────────────────────────────────────────────
CREATE TABLE calendar_events (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id      UUID NOT NULL REFERENCES tenants(id),
  title          TEXT NOT NULL,
  event_type     TEXT,                            -- 'holiday'|'exam'|'event'|'etut'
  start_date     DATE NOT NULL,
  end_date       DATE,
  is_school_day  BOOLEAN DEFAULT true,
  note           TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);
```

### Row Level Security Politikaları

```sql
-- Tüm tablolar için genel şablon
-- (students tablosu örnek — diğerleri aynı mantık)

ALTER TABLE students ENABLE ROW LEVEL SECURITY;

-- Tenant kullanıcıları yalnızca kendi tenant'larını görür
CREATE POLICY "tenant_isolation" ON students
  FOR ALL USING (
    tenant_id = (
      SELECT tenant_id FROM users WHERE id = auth.uid()
    )
  );

-- Veliler yalnızca kendi çocuklarını görür
CREATE POLICY "parent_own_child" ON students
  FOR SELECT USING (
    parent_user_id = auth.uid()
  );

-- Yardımcı hocalar okuma izni
CREATE POLICY "assistant_read_only" ON students
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM users
      WHERE id = auth.uid()
        AND role = 'assistant_teacher'
        AND tenant_id = students.tenant_id
    )
  );
```

---

## 7. Ekran Haritası

```
App
│
├── /                          Landing Page (pazarlama)
├── /register                  Kurs Kayıt Formu (tenant onboarding)
│
├── /super-admin               Platform Yönetimi (Super Admin)
│   ├── /tenants               Kurs listesi, durum, plan
│   ├── /subscriptions         Abonelik ve fatura takibi
│   └── /analytics             Platform geneli istatistikler
│
└── /[kurs-slug]/              Kurs Uygulaması (tenant alanı)
    │
    ├── /login                 Giriş (OTP)
    │
    └── (Auth gerektiren)
        │
        ├── /dashboard         Ana Sayfa
        │   ├── Günlük istatistikler
        │   ├── Öğrenci kart listesi
        │   └── Etüt / devamsızlık uyarıları
        │
        ├── /students          Öğrenci Listesi
        │   ├── Arama, filtre, grup
        │   └── + Yeni Öğrenci
        │
        ├── /students/:id      Öğrenci Detay
        │   ├── [Tab] Profil & İlerleme
        │   ├── [Tab] Dersler (Sabak/Sabki/Manzil)
        │   ├── [Tab] Tekrarlar (Sabah/Öğle/Akşam)
        │   ├── [Tab] Devamsızlık
        │   ├── [Tab] Etüt Notları
        │   ├── [Tab] Sınavlar
        │   └── [Tab] Veli & Bildirim
        │
        ├── /students/:id/edit Öğrenci Düzenle
        │
        ├── /lessons           Günlük Ders Özeti
        ├── /attendance        Yoklama Ekranı
        ├── /etut              Akşam Etüdü
        ├── /parents           Veli Bildirim Merkezi
        ├── /exams             Sınavlar & Takvim
        ├── /announcements     Duyurular
        ├── /reports           Raporlar & Analitik
        ├── /fees              Ücret Takibi (Standart+)
        │
        ├── /settings          Kurs Ayarları (Yönetici)
        │   ├── Kurs bilgileri
        │   ├── Öğretmen yönetimi
        │   ├── Grup/Sınıf yönetimi
        │   ├── Çalışma takvimi
        │   └── Bildirim tercihleri
        │
        └── /parent-portal     Veli Portalı (ayrı giriş)
            ├── Çocuğum → Ders özeti
            ├── Devamsızlık takvimi
            ├── Sınav sonuçları
            └── Duyurular
```

---

## 8. Geliştirme Roadmap'i

---

### 🟦 AŞAMA 1 — Temel Altyapı

**Hedef:** Projeyi ayağa kaldır, multi-tenant çerçevesini ve tasarım sistemini kur.

- [ ] Vite + React projesi oluştur
- [ ] Tailwind CSS + özel renk/font değişkenleri
- [ ] Klasör yapısını oluştur
- [ ] React Router v6 kurulumu ve route yapısı
- [ ] Supabase projesi oluştur (geliştirme ortamı)
- [ ] `.env.local` ve Supabase client kurulumu (`src/services/supabase.js`)
- [ ] Tüm platform ve tenant tabloları + RLS politikaları
- [ ] Zustand store iskeletleri (`authStore`, `tenantStore`, `studentStore`)
- [ ] Ortak UI bileşenleri: `Button`, `Card`, `Input`, `Select`, `Badge`, `Avatar`, `Modal`, `Toast`, `Skeleton`, `ProgressBar`, `EmptyState`
- [ ] Layout bileşenleri: `BottomNav`, `TopBar`, `PageWrapper`, `TabBar`
- [ ] Mobil viewport ve PWA temel ayarları

---

### 🟦 AŞAMA 2 — Kimlik Doğrulama & Multi-Tenant Oturum

**Hedef:** Kullanıcı giriş yapsın, doğru tenant'a yönlensin, rol farkı uygulansın.

- [ ] Supabase Auth — telefon + OTP akışı
- [ ] `LoginScreen`: telefon → OTP → yönlendirme
- [ ] Giriş sonrası tenant belirleme (kullanıcının hangi kursa ait olduğu)
- [ ] `useAuth` hook'u: kullanıcı, rol, tenant bilgisi
- [ ] `authStore`: JWT'den tenant_id ve rol bilgisi parse etme
- [ ] Korumalı route'lar: rolsüz erişim engelleme
- [ ] Rol bazlı görünüm bileşeni (`<RoleGate role="head_teacher">`)
- [ ] Oturum kalıcılığı
- [ ] Çıkış yapma ve token temizleme

---

### 🟦 AŞAMA 3 — Tenant Onboarding & Kurs Yönetimi

**Hedef:** Yeni bir kurs platforma kaydolabilsin, temel ayarlarını yapabilsin.

- [ ] Landing page ve "Kurs Oluştur" formu
- [ ] `tenantService.js`: tenant oluşturma, ayarlar güncelleme
- [ ] Tenant onboarding akışı (kayıt → deneme başlatma → ilk giriş)
- [ ] Kurs Ayarları ekranı (`/settings`)
  - [ ] Kurs bilgileri (ad, logo, adres, iletişim)
  - [ ] Çalışma günleri ve saatleri
  - [ ] Tatil günü yönetimi
  - [ ] Etüt bilgileri
- [ ] Öğretmen yönetimi
  - [ ] Davet gönderme (OTP SMS)
  - [ ] Rol atama
  - [ ] Pasife alma
- [ ] Grup / sınıf yönetimi
  - [ ] Grup oluşturma, öğretmen atama, öğrenci atama

---

### 🟦 AŞAMA 4 — Öğrenci Yönetimi

**Hedef:** Öğrenci tam yaşam döngüsü yönetilebilsin.

- [ ] `studentService.js`: CRUD, arşivleme, mezuniyet
- [ ] `useStudents` hook'u
- [ ] `StudentListScreen`: liste + arama + grup filtresi + hız göstergesi
- [ ] `StudentFormScreen`: eksiksiz kayıt formu (profil + veli + hafızlık konumu)
- [ ] `StudentDetailScreen`: sekmeli detay ekranı
- [ ] `surahList.js`: 114 sure tam liste
- [ ] Öğrenci fotoğrafı yükleme (Supabase Storage)
- [ ] Arşivleme ve mezuniyet işlemi
- [ ] Veli kullanıcı bağlantısı (parent_user_id)

---

### 🟦 AŞAMA 5 — Ders & Tekrar Kaydı

**Hedef:** Sabak / Sabki / Manzil kayıt sistemi çalışsın.

- [ ] `lessonService.js` ve `repetitionService.js`
- [ ] `useLessons` hook'u
- [ ] Ders formu: Sabak (yeni sayfa aralığı), Sabki kalite, Manzil notları
- [ ] Tekrar oturumu formu: sabah / öğle / akşam ayrı kayıt
- [ ] Ödev durumu seçimi
- [ ] Performans değerlendirmesi
- [ ] Hata türü seçimi (mahreç, med, vakf vb.)
- [ ] Ders kaydedince `students.current_page` otomatik güncelleme
- [ ] Bugün kayıt girilmişse düzenleme modu
- [ ] Ders geçmişi listesi (öğrenci detay)
- [ ] `LessonListScreen`: tüm öğrencilerin günlük özeti
- [ ] Hızlı toplu giriş modu (bir sınıfın dersini arka arkaya girmek)

---

### 🟦 AŞAMA 6 — Devamsızlık Takibi

**Hedef:** Günlük yoklama dijital yapılsın, veli anında bilgilensin.

- [ ] `attendanceService.js`
- [ ] `AttendanceScreen`: sınıf listesi, tek tıkla yoklama
- [ ] Devamsızlık durumu: var / yok / geç geldi / mazeretli
- [ ] Mazeret notu ve nedeni
- [ ] Takvim görünümü (öğrenci bazlı)
- [ ] Devamsızlık limiti uyarısı (özelleştirilebilir)
- [ ] Yoklama sonrası devamsız öğrenciler için otomatik veli bildirimi tetikleme

---

### 🟦 AŞAMA 7 — Dashboard

**Hedef:** Ana sayfa günün tüm durumunu özetlesin.

- [ ] Günlük istatistik kartları (toplam öğrenci, işlenen ders, bekleyen, yeni sayfa)
- [ ] Devamsızlık uyarı bandı
- [ ] Etüt notu uyarı bandı
- [ ] Öğrenci kart listesi (ilerleme çubuklu, pace göstergeli)
- [ ] Son 7 günün özet trendi

---

### 🟦 AŞAMA 8 — Veli Bildirimi & Portalı

**Hedef:** Veli sisteme giriş yapabilsin, çocuğunu takip edebilsin.

- [ ] `whatsappBuilder.js`: günlük, haftalık, devamsızlık, sınav şablonları
- [ ] `ParentNotifyScreen`: tüm veliler, tekil WA gönderim + log
- [ ] `notificationService.js`: log kaydı
- [ ] Veli Portalı (`/parent-portal`)
  - [ ] Veli OTP girişi
  - [ ] Çocuğun günlük ders özeti
  - [ ] Devamsızlık takvimi
  - [ ] Etüt bilgisi
  - [ ] Duyurular

---

### 🟦 AŞAMA 9 — Akşam Etüdü

**Hedef:** Etüt koordinasyonu tam dijital.

- [ ] `etutService.js`: ekleme, listeleme, tamamlama, taşıma
- [ ] `EtutScreen`: bugünkü notlar, yardımcı hoca görünümü
- [ ] Tamamlandı / tamamlanamadı işaretleme + yapan kişi logu
- [ ] Tamamlanmayanları ertesi güne taşıma
- [ ] Etüt geçmiş arşivi

---

### 🟦 AŞAMA 10 — Sınav & Duyuru

**Hedef:** Sınavlar planlanabilsin, duyurular yayınlanabilsin.

- [ ] `examService.js` ve `announcementService.js`
- [ ] Sınav oluşturma ve takvim
- [ ] Not girişi ve sonuç raporu
- [ ] Duyuru yayınlama (hedefli)
- [ ] Duyuru bildirim entegrasyonu

---

### 🟦 AŞAMA 11 — Ücret & Takvim

**Hedef:** Kayıt ücretleri takip edilsin, kurs takvimi yönetilsin.

- [ ] `feeService.js` ve `calendarService.js`
- [ ] Ödeme logu ve durum takibi
- [ ] Ödeme hatırlatma bildirimi
- [ ] Kasa raporu
- [ ] Takvim ekranı (tatil, etkinlik, sınav görünümü)

---

### 🟦 AŞAMA 12 — Offline & PWA

**Hedef:** İnternet olmadan çalışsın.

- [ ] `vite-plugin-pwa` yapılandırması
- [ ] Service Worker: statik ve API önbellekleme
- [ ] `useOfflineQueue` hook'u + IndexedDB kuyruk
- [ ] Otomatik senkronizasyon ve çakışma yönetimi
- [ ] Bağlantı durumu banner'ı
- [ ] Uygulama güncelleme bildirimi

---

### 🟦 AŞAMA 13 — Raporlama & Analitik

**Hedef:** Öğretmen ve yönetici veri bazlı karar alabilsin.

- [ ] Recharts entegrasyonu
- [ ] Haftalık / aylık ilerleme grafikleri
- [ ] Devamsızlık trend raporu
- [ ] Ödev tamamlama oranı
- [ ] Öğretmen bazlı karşılaştırma
- [ ] PDF rapor çıktısı (`html2pdf.js` veya `@react-pdf/renderer`)
- [ ] Excel / CSV dışa aktarım

---

### 🟦 AŞAMA 14 — Platform Yönetimi & Super Admin

**Hedef:** Platform operasyonları merkezi panelden yönetilsin.

- [ ] Super Admin giriş ve korumalı alanı
- [ ] Kiracı listesi ve durum yönetimi
- [ ] Abonelik ve fatura yönetimi
- [ ] Platform geneli analytics
- [ ] Ödeme entegrasyonu (İyzico / Stripe)
- [ ] Gecikme yönetimi ve otomatik askıya alma

---

### 🟦 AŞAMA 15 — Üretim & Yayın

**Hedef:** Canlıya alım ve ilk gerçek kullanıcılar.

- [ ] Vercel deploy (GitHub otomatik CI/CD)
- [ ] Supabase üretim ortamı (ayrı proje)
- [ ] Ortam değişkeni yönetimi
- [ ] Sentry hata izleme
- [ ] Lighthouse denetimi
- [ ] Kullanıcı kabul testi (2–3 kurs pilot)
- [ ] Geri bildirim ve düzeltme sprinti

---

## 9. Teknoloji & Araçlar

| Katman | Teknoloji | Gerekçe |
|--------|-----------|---------|
| UI Framework | React 18 | Geniş ekosistem |
| Stil | Tailwind CSS | Utility-first, mobil öncelikli |
| State | Zustand | Basit, boilerplate az |
| Backend / DB | Supabase | Auth + PostgreSQL + Storage + Realtime |
| Kimlik Doğrulama | Supabase Auth (OTP) | Telefon numarası bazlı, sıfır parola |
| Build | Vite | Hızlı HMR |
| PWA | vite-plugin-pwa | Offline, kurulabilir |
| Grafik | Recharts | Saf React, hafif |
| Tarih | date-fns | Küçük bundle, Türkçe lokalizasyon |
| Toast | react-hot-toast | Basit bildirim |
| PDF | @react-pdf/renderer | Sunucu gerektirmez |
| Ödeme | İyzico (TR) / Stripe (global) | Abonelik yönetimi |
| Deploy | Vercel | Otomatik CI/CD |
| Hata İzleme | Sentry | Üretim hata takibi |

---

## 10. Kurulum

### Gereksinimler

- .NET 8 SDK
- PostgreSQL 14+ (`psql` dahil)
- Node.js 20+

### 1. Veritabanı

```powershell
$env:PGPASSWORD = 'postgres'
.\database\db.ps1 -Setup -Database HafizTakipDB
```

Şema SQL scriptlerinden kurulur (EF Migration / `EnsureCreated` kullanılmaz). Mevcut bir veritabanını güncellemek için `-Migrate`; ayrıntılar `database/README.md`'de.

### 2. Backend (ASP.NET Core, http://localhost:5200)

Geliştirme ayarları `backend/src/HafizTakip.WebAPI/appsettings.Development.json` içindedir: yerel bağlantı dizesi, yalnızca geliştirmeye özel JWT anahtarı, sabit OTP kodu `123456` ve SMS yerine konsol çıktısı. Üretimde bağlantı dizesi, JWT anahtarı ve NetGSM bilgileri ortam değişkenleriyle verilmelidir; geliştirme değerleriyle uygulama üretimde başlamaz.

```bash
dotnet run --project backend/src/HafizTakip.WebAPI --launch-profile http
```

Swagger: http://localhost:5200/swagger

### 3. Frontend (Vite + React, http://localhost:7071)

```bash
cd frontend/web
npm install
npm run dev
```

Kök dizindeki `start-dev.ps1` backend ve frontend'i birlikte başlatır.

### 4. Testler

```bash
dotnet test backend/src/HafizTakip.slnx
```

Entegrasyon testleri gerçek PostgreSQL üzerinde geçici bir veritabanı oluşturup sonunda siler. Çalışmaları için `HAFIZTAKIP_TEST_PG` ortam değişkeni gerekir (ör. `Host=localhost;Username=postgres;Password=postgres`); tanımlı değilse atlanırlar.

```bash
cd frontend/web
npx vitest run
```

---

## 11. Geliştirme Notları

### Multi-Tenant Temel Kurallar
- Her tablo mutlaka `tenant_id UUID NOT NULL` içerir
- Her Supabase sorgusuna `.eq('tenant_id', tenantId)` eklenir — otomatik RLS bunu da kontrol eder
- `tenant_id` JWT içinde `app_metadata.tenant_id` olarak saklanır
- Bileşenler doğrudan Supabase sorgusu yapmaz; yalnızca `services/` katmanı sorgular

### Kodlama Standartları
- Fonksiyonel bileşenler, hook tabanlı
- `PascalCase.jsx` — bileşenler; `camelCase.js` — utils / hooks / services
- Her servis fonksiyonu `try/catch` ile sarılmış
- Yükleme durumu: `isLoading` state — iskelet ekranla gösterilir
- Boş durum: açıklayıcı mesaj + aksiyon butonu

### Tasarım Sistemi
- Dominant: koyu yeşil `#1B5C3A`
- Vurgu: altın `#B8881A`
- Yüzey: krem `#F4EEE0`
- Font: başlıklar Playfair Display, metin Nunito
- Dokunma hedefleri minimum `44 × 44 px`
- Tüm renkler CSS değişken olarak tanımlanır

### Bilinen Kısıtlamalar
- Supabase ücretsiz: 50 OTP SMS/ay → üretimde Pro plan gerekir
- WhatsApp Business API: onay süreci uzun → başlangıç deep-link yeterli
- İyzico entegrasyonu: firma kaydı + başvuru gerektirir

---

*Proje başlangıç tarihi: Mayıs 2026*  
*Geliştirme modeli: Claude Opus 4.6 ile AI destekli iteratif geliştirme*  
*Mimari: Multi-Tenant SaaS · React · Supabase · Vercel*
