# 🗄️ Database Roadmap

> **SQL Server / PostgreSQL · Entity Framework Core · Code-First**
> Hafız Takip projesinin veritabanı katmanı (DB) için geliştirme haritası ve işlem öncelikleri.

---

## 📋 İşlem Öncelikleri

| Aşama | Görev | Öncelik | Not |
|-------|-------|:-------:|-----|
| 1 | Platform Tabloları | 🔴 P0 | `tenants`, `subscriptions`, `invoices` (tenant_id yok) |
| 1 | Tenant Tabloları | 🔴 P0 | Kalan tüm tablolar (`tenant_id` zorunlu) |
| 1 | RLS (Row Level Security) | 🔴 P0 | Supabase/PostgreSQL kullanılıyorsa tenant izolasyonu için policy'ler |
| 1 | Index Optimizasyonu | 🔴 P0 | Her tabloya `IX_{table}_tenant_id` eklenmesi |
| 1 | Seed Data (Sabit Veriler) | 🟠 P1 | 114 Sure, Cüz-Sayfa eşleştirmeleri, sabit roller |
| 2 | Veritabanı Şeması | 🔴 P0 | SQL Script-First ile (00_full_schema.sql) tabloların DB'ye yansıtılması |
| 4 | UNIQUE Constraints | 🟠 P1 | `UNIQUE(student_id, lesson_date)`, `UNIQUE(student_id, date)` vb. eklenmesi |
| 12 | Çakışma Yönetim Alanları | 🟡 P2 | Offline-first için `updated_at` alanlarının trigger ile otomatik güncellenmesi |
| 13 | View / Materialized View | 🟢 P3 | Raporlama için aggregate view'lar oluşturulması (isteğe bağlı, performansa göre) |

---

## 🚀 Aşama Aşama Geliştirme Planı

### Aşama 1: Temel Altyapı ve Tablo Kurulumu (🔴 P0)
- **Görevler:**
  - `tenants`, `subscriptions`, `invoices` gibi platform düzeyindeki tabloları oluşturun.
  - `users`, `students`, `groups`, `lessons` gibi kiracı düzeyindeki (tenant_scoped) tüm tabloları oluşturun.
  - Her tenant düzeyindeki tabloya `tenant_id` sütununu zorunlu yabancı anahtar (FK) olarak ekleyin.
  - Sabit veri tohumlamasını (Seed Data) hazırlayın: Kur'an sure listesi, cüz eşleştirmeleri.
- **Kritik Noktalar:**
  - `tenant_id` olmadan hiçbir tenant verisi oluşturulmamalı. 
  - İsimlendirme kurallarına (snake_case) harfiyen uyulmalı.

### Aşama 2: Kısıtlamalar ve İndeksleme (🔴 P0)
- **Görevler:**
  - Global `tenant_id` indekslerini tanımlayın (Örn: `CREATE INDEX IX_students_tenant_id ON students(tenant_id);`).
  - Benzersiz (Unique) alan kısıtlamalarını ekleyin. Örneğin bir öğrenci aynı gün içinde sadece bir kez derse/yoklamaya kaydedilebilir.
  - Soft delete için `is_deleted` ve `deleted_at` alanlarını tüm veri odaklı tablolara uygulayın.
- **Kritik Noktalar:**
  - Veri bütünlüğü için veritabanı seviyesinde `UNIQUE` constraint'ler şarttır, sadece backend koduna güvenilmemelidir.

### Aşama 3: Güvenlik ve İzolasyon (🔴 P0 / 🟠 P1)
- **Görevler:**
  - Eğer doğrudan PostgreSQL veya Supabase özellikleri kullanılıyorsa, **Row Level Security (RLS)** politikalarını yazın.
  - API katmanından gelen isteklerde, tenant_id'nin zorla enjekte edilebilmesi için veritabanı kullanıcı (role) yapısını veya EF Core filtre altyapısını planlayın.
- **Kritik Noktalar:**
  - Kiracılar arası veri sızıntısını engellemek projenin **en hayati** kısmıdır.

### Aşama 4: Raporlama ve Optimizasyon (🟡 P2 / 🟢 P3)
- **Görevler:**
  - Çoklu tablo birleşimleri (Dashboard ve Raporlar için) yavaşlamaya başlarsa SQL View veya Materialized View yazın.
  - Sorgu profillemesi yaparak eksik indeksleri (örneğin composite index: `tenant_id` + `date`) tespit edip ekleyin.

---

## ⚠️ Dikkat Edilecekler
1. **Fiziksel Silme Yok:** KVKK gibi istisnalar hariç hiçbir tablo satırı fiziksel olarak silinmez. Soft-delete uygulanır.
2. **Cross-Tenant FK:** Farklı tenant'lara ait verilerin birbiriyle ilişkilenmesine izin verilmez.
3. **Veritabanı Yönetim Disiplini (SQL Script-First):** EF Core Migration KESİNLİKLE KULLANILMAYACAKTIR. Tüm tablo, kolon, constraint değişiklikleri `database/` altındaki SQL scriptleri ile yapılmalı, ardından ilgili backend entity class'ları güncellenmelidir. Her değişiklik script üzerinden izlenebilir olmalıdır.
