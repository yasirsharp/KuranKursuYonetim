# 🗄️ Database Developer Rules

> **PostgreSQL · Entity Framework Core · SQL Script-First**
> Bu dosya veritabanı tasarım ve yönetim kurallarını tanımlar. Tüm ekip üyeleri bu kuralları bilmelidir.

---

## 1. Genel Mimari

### 1.1 Veritabanı Stratejisi
- **Tek veritabanı, paylaşımlı şema** (shared schema) — maliyet-verimli multi-tenant model
- Tüm kiracıların verisi aynı tablolarda, `tenant_id` ile izole
- Platform-seviye tablolar (tenants, subscriptions, invoices) tenant_id içermez

### 1.2 Tablo Kategorileri
```
Platform Tabloları (tenant_id YOK):
  ├── tenants
  ├── subscriptions
  └── invoices

Tenant Tabloları (tenant_id ZORUNLU):
  ├── users
  ├── groups
  ├── students
  ├── lessons
  ├── repetition_sessions
  ├── mutashabihat_notes
  ├── attendance
  ├── etut_notes
  ├── exams
  ├── exam_results
  ├── parent_notifications
  ├── announcements
  ├── fee_records
  └── calendar_events
```

---

## 2. Naming Conventions

### 2.1 Tablo İsimleri
| Kural | Örnek |
|-------|-------|
| **snake_case** (küçük harf, alt çizgi) | `students`, `etut_notes` |
| **Çoğul** isim | `students` (~~student~~) |
| **Bağlantı tabloları**: iki tablo adı birleşik | `student_groups`, `exam_results` |

### 2.2 Kolon İsimleri
| Kural | Örnek |
|-------|-------|
| **snake_case** | `first_name`, `tenant_id` |
| **Primary key** | `id` (UUID) |
| **Foreign key** | `{tablo_tekil}_id` → `student_id`, `teacher_id` |
| **Boolean** | `is_` prefix → `is_active`, `is_done`, `is_deleted` |
| **Tarih** | `_at` suffix → `created_at`, `deleted_at` |
| **Tarih (sadece gün)** | `_date` suffix → `lesson_date`, `birth_date` |

### 2.3 Index İsimleri
```sql
IX_{tablo}_{kolon}           → IX_students_tenant_id
IX_{tablo}_{kolon1}_{kolon2} → IX_lessons_student_id_lesson_date
UQ_{tablo}_{kolon}           → UQ_tenants_slug
```

---

## 3. Zorunlu Alanlar

### 3.1 Her Tabloda Olması Gereken Alanlar
```sql
-- Temel alanlar (TÜM tablolar)
id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),  -- veya NEWID()
created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
updated_at     TIMESTAMPTZ,

-- Tenant tabloları (platform tabloları HARİÇ)
tenant_id      UUID NOT NULL REFERENCES tenants(id),

-- Soft delete (önerilen)
is_deleted     BOOLEAN NOT NULL DEFAULT false,
deleted_at     TIMESTAMPTZ
```

### 3.2 Soft Delete Kuralı
```
⚠️ FİZİKSEL SİLME YAPILMAZ (KVKK hariç)

Silme işlemi:
  1. is_deleted = true
  2. deleted_at = now()

Sorgularda:
  WHERE is_deleted = false (EF Core Global Query Filter ile otomatik)

KVKK veri silme talebi:
  Gerçek DELETE, ancak audit log tutulur
```

---

## 4. Multi-Tenant Kuralları

### 4.1 Tenant İzolasyonu
```sql
-- ⚠️ MUTLAK KURAL: Tenant tabloları her zaman tenant_id filtresi ile sorgulanır

-- ✅ DOĞRU
SELECT * FROM students WHERE tenant_id = @tenantId AND is_deleted = false;

-- ❌ YANLIŞ (tenant filtresi yok!)
SELECT * FROM students WHERE name LIKE '%Yusuf%';
```

### 4.2 Foreign Key Kuralları
```sql
-- Tenant-scoped FK'ler: aynı tenant içinde referans
-- Cross-tenant FK YASAKTIR

-- ✅ DOĞRU: students.teacher_id → users.id (aynı tenant)
-- ❌ YANLIŞ: students.teacher_id → başka tenant'ın user'ı
```

### 4.3 Index Stratejisi
```sql
-- Her tenant tablosunda tenant_id üzerinde index ZORUNLU
CREATE INDEX IX_students_tenant_id ON students(tenant_id);

-- Sık sorgulanan alanlar için composite index
CREATE INDEX IX_lessons_tenant_student_date 
  ON lessons(tenant_id, student_id, lesson_date);

CREATE INDEX IX_attendance_tenant_date 
  ON attendance(tenant_id, date);
```

---

## 5. Veri Modeli Detayları

### 5.1 Kur'an Yapısı (Sabit Veri)
```
Toplam: 114 Sure, 604 Sayfa, 30 Cüz

Sayfa aralıkları:
  Cüz 1:  Sayfa 1–21
  Cüz 2:  Sayfa 22–41
  ...
  Cüz 30: Sayfa 582–604

İlerleme hesabı:
  Yüzde = (current_page / 604) * 100
```

### 5.2 Enum Değerleri (String olarak saklanır)
```sql
-- User Roles
'super_admin' | 'tenant_admin' | 'head_teacher' | 'assistant_teacher' | 'parent' | 'student'

-- Tenant Status
'trial' | 'active' | 'suspended' | 'cancelled'

-- Student Status
'active' | 'archived' | 'graduated'

'whatsapp' | 'sms' | 'email'

-- Notification Status
'sent' | 'delivered' | 'failed'

-- Fee Status
'pending' | 'paid' | 'overdue' | 'waived'
```

### 5.3 Unique Constraints
```sql
-- Önemli benzersizlik kuralları
UNIQUE (tenant_id, slug)                    → tenants tablosunda (global unique de olabilir)
UNIQUE (student_id, lesson_date)            → Günde bir ders kaydı
UNIQUE (student_id, date)                   → Günde bir yoklama kaydı
UNIQUE (tenant_id, email)                   → Tenant içinde unique email (users)
```

---

## 6. Migration Kuralları (SQL Script-First)

> ⚠️ **MUTLAK KURAL: SQL Script-First Yaklaşımı**
> EF Core Migration (`dotnet ef migrations add`) **KESİNLİKLE KULLANILMAYACAKTIR**.

### 6.1 Yeni Tablo veya Kolon Ekleme Akışı
1. `database/migrations/` altında numaralı yeni bir artımlı script açılır (örn: `003_schedule.sql`). Bu script mevcut veritabanlarını günceller (CREATE/ALTER, gerekiyorsa veri dönüşümü).
2. Aynı değişiklik `00_full_schema.sql` dosyasına da işlenir; bu dosya her zaman şemanın **son halini** gösterir ve sıfırdan kurulumda kullanılır.
3. Backend projesindeki ilgili C# Entity sınıfı (örn: `YeniTablo.cs`), SQL scriptiyle birebir eşleşecek şekilde güncellenir.
4. Entity property isimleri PascalCase, veritabanı kolon isimleri snake_case olmalıdır (EF Core `UseSnakeCaseNamingConvention()` bunu otomatik eşleştirir).
5. Tüm enum tipleri veritabanında `VARCHAR(50)` olarak tanımlanmalı, enum değer kısıtlamaları (validasyon) uygulama katmanında (C# tarafında) yapılmalıdır.

### 6.2 SQL Script Best Practices
- Her değişiklik için `database/migrations/NNN_aciklama.sql` formatında yeni dosya açın; uygulanmış bir migration dosyası sonradan değiştirilmez.
- Scriptler tekrar çalıştırılabilir olmalı: `IF NOT EXISTS` / `ADD COLUMN IF NOT EXISTS` / `DROP ... IF EXISTS` kullanın.
- Tablolara her zaman `id`, `created_at`, `updated_at`, `is_deleted`, `deleted_at` alanlarını ekleyin.
- Tenant tablolarına mutlaka `tenant_id` ekleyin.

### 6.3 Seed Data
```csharp
// Seed data yalnızca sabit referans veriler için
// - Sure listesi (114 sure)
// - Cüz-sayfa eşleştirmesi
// - Varsayılan roller
// - Demo/test tenant (sadece development ortamında)
```

---

## 7. Performans Kuralları

### 7.1 Sorgu Optimizasyonu
```csharp
// ✅ Gerekli alanları seç (Select projection)
var students = await _context.Students
    .Where(s => s.TenantId == tenantId)
    .Select(s => new StudentListItemDto
    {
        Id = s.Id,
        Name = s.Name,
        CurrentPage = s.CurrentPage
    })
    .ToListAsync();

// ❌ Tüm entity'yi çekme (özellikle listeler için)
var students = await _context.Students.ToListAsync();
```

### 7.2 Sayfalama
```csharp
// Tüm liste endpoint'leri sayfalama desteklemeli
.Skip((page - 1) * pageSize)
.Take(pageSize)

// Varsayılan: page=1, pageSize=20, max pageSize=100
```

### 7.3 N+1 Problemi
```csharp
// ✅ Include ile eager loading
var students = await _context.Students
    .Include(s => s.Lessons.Where(l => l.LessonDate == today))
    .ToListAsync();

// ❌ Loop içinde sorgu (N+1 problem)
foreach (var student in students)
{
    student.Lessons = await _context.Lessons
        .Where(l => l.StudentId == student.Id)
        .ToListAsync();  // HER ÖĞRENCİ İÇİN AYRI SORGU!
}
```

---

## 8. Diğer Katmanları Tanıma

### 8.1 Backend
- EF Core, SQL Script-First — şema `database/` scriptlerinden kurulur, C# entity'leri bu şemayı birebir yansıtır
- Global Query Filter ile tenant izolasyonu otomatik
- Soft delete filtresi otomatik uygulanır

### 8.2 Frontend (Web)
- API'den gelen veriler TypeScript tipleriyle eşleşmeli
- Tarihler ISO 8601 formatında (`2026-05-11T00:00:00Z`)
- UUID string olarak gönderilir/alınır
- Sayfalama yanıtı: `{ data: T[], totalCount: number, page: number, pageSize: number }`

### 8.3 Frontend (Mobil)
- Aynı API endpoint'leri kullanılır
- Offline durumda yerel veritabanına yazılır, sync sırasında API'ye gönderilir
- Çakışma çözümü: `updated_at` timestamp karşılaştırması (last-write-wins veya merge)

---

## 9. Yedekleme & Güvenlik

### 9.1 Yedekleme
- Günlük otomatik yedekleme
- Point-in-time recovery aktif
- Yedekler farklı bölgede (disaster recovery)

### 9.2 Veri Güvenliği
- Hassas veriler (TC kimlik, telefon) şifreli saklanma düşünülebilir
- Connection string asla koda yazılmaz (environment variable / secret manager)
- Veritabanı erişimi yalnızca backend üzerinden (doğrudan DB erişimi yasak)
- SQL injection koruması: parameterized queries (EF Core bunu otomatik sağlar)

---

*Son güncelleme: Temmuz 2026*
