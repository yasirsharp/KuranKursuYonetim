# 🔧 Backend Developer Rules

> **.NET 8 · N-Tier Architecture · REST API**
> Bu dosya backend geliştirme kurallarını tanımlar. Tüm ekip üyeleri bu kuralları bilmelidir.

---

## 1. Proje Yapısı (N-Tier)

```
HafizTakip.sln
├── HafizTakip.API/              → Presentation Layer (Controllers, Middleware)
├── HafizTakip.Business/         → Business Logic Layer (Services, DTOs, Validators)
├── HafizTakip.DataAccess/       → Data Access Layer (Repositories, DbContext, Migrations)
├── HafizTakip.Entities/         → Entity Layer (Models, Enums, Interfaces)
└── HafizTakip.Core/             → Cross-Cutting (Results, Security, Utilities)
```

### Katman Bağımlılık Kuralı
```
API → Business → DataAccess → Entities
                                  ↑
API → Core ←←←←←←←←←←←←←←←←←←←←┘
```
- **API** yalnızca **Business** katmanını çağırır, doğrudan repository kullanmaz.
- **Business** yalnızca **DataAccess** katmanını çağırır.
- **Entities** hiçbir katmana bağımlı değildir.
- **Core** hiçbir katmana bağımlı değildir, herkes Core'u kullanabilir.

---

## 2. Multi-Tenant Kuralları

> ⚠️ **MUTLAK KURAL:** Bu bir multi-tenant SaaS uygulamasıdır. Kiracı izolasyonu en kritik güvenlik gereksinimidir.

### 2.1 Entity Kuralları
```csharp
// Her tenant-scoped entity bu interface'i implemente ETMEK ZORUNDADIR
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

// Platform-seviye entity'ler (Tenant, Subscription, Invoice) hariç
// TÜM entity'ler ITenantEntity'dir.
```

### 2.2 Middleware
```csharp
// TenantResolutionMiddleware her istekte çalışır:
// 1. JWT'den tenant_id claim'ini çıkarır
// 2. HttpContext.Items["TenantId"] olarak saklar
// 3. DbContext'e global query filter olarak enjekte eder
```

### 2.3 Repository Kuralları
- **Her sorgu** otomatik olarak `tenant_id` filtresi içermelidir (Global Query Filter)
- Repository'de **asla** `tenant_id` filtresi olmadan veri çekilmez
- Cross-tenant veri erişimi yalnızca **Super Admin** rolü ile ve **açıkça belirtilmiş** endpoint'lerden yapılabilir

### 2.4 DbContext Global Filter
```csharp
// HafizTakipDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Tüm ITenantEntity tabloları için otomatik filtre
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(e => ((ITenantEntity)e).TenantId == _currentTenantId);
        }
    }
}
```

---

## 3. Kodlama Standartları

### 3.1 Naming Conventions
| Öğe | Format | Örnek |
|-----|--------|-------|
| Sınıf/Interface | PascalCase | `StudentService`, `IStudentService` |
| Metod | PascalCase | `GetStudentByIdAsync` |
| Property | PascalCase | `FirstName`, `TenantId` |
| Private field | _camelCase | `_studentRepository` |
| Parameter | camelCase | `studentId`, `tenantId` |
| Constant | PascalCase | `MaxStudentLimit` |
| Enum | PascalCase | `UserRole.HeadTeacher` |
| Controller | PascalCase + Controller | `StudentsController` |

> ⚠️ **DOSYA İSİMLENDİRME KURALI:**
> - **Her Sınıf Kendi Dosyasına:** Birden fazla sınıf (örneğin birden fazla Validator) ASLA aynı dosya içine yazılamaz. Her sınıf kendi adını taşıyan ayrı bir dosyada olmalıdır.
> - **Birebir Eşleşme:** Dosya adı, içindeki sınıf adıyla **birebir aynı** olmalıdır (`CreateExamValidator` sınıfı `CreateExamValidator.cs` dosyasında olmalıdır).
> - **Gelişi Güzel Önek Yasaktır:** Dosya isimlerine `Phase1...`, `Phase2...`, `All...` gibi keyfi gruplama önekleri eklemek KESİNLİKLE YASAKTIR.

### 3.2 Async/Await
```csharp
// ✅ DOĞRU: Tüm I/O işlemleri async olmalı
public async Task<IDataResult<Student>> GetByIdAsync(Guid id)

// ❌ YANLIŞ: .Result veya .Wait() kullanılmaz
var student = _repo.GetByIdAsync(id).Result; // YASAK!
```

### 3.3 Response Format
```csharp
// Tüm API yanıtları standart Result wrapper ile döner
public interface IResult
{
    bool Success { get; }
    string Message { get; }
}

public interface IDataResult<T> : IResult
{
    T Data { get; }
}

// Controller'da:
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _studentService.GetByIdAsync(id);
    if (!result.Success)
        return BadRequest(result);
    return Ok(result);
}
```

### 3.4 Error Handling
```csharp
// ExceptionHandlingMiddleware tüm hataları yakalar
// Service'lerde try/catch kullanılır, exception fırlatılmaz
// Business kuralı ihlali → ErrorResult döner, exception fırlatılmaz

// ✅ DOĞRU
if (student == null)
    return new ErrorDataResult<Student>("Öğrenci bulunamadı");

// ❌ YANLIŞ
if (student == null)
    throw new NotFoundException("Öğrenci bulunamadı");
```

### 3.5 Validation
```csharp
// FluentValidation kullanılır
// Her DTO için ayrı Validator sınıfı
public class CreateStudentDtoValidator : AbstractValidator<CreateStudentDto>
{
    public CreateStudentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CurrentPage).InclusiveBetween(1, 604);
        RuleFor(x => x.CurrentJuz).InclusiveBetween(1, 30);
    }
}
```

---

## 4. API Tasarım Kuralları

### 4.1 URL Yapısı
```
/api/{resource}             → Collection
/api/{resource}/{id}        → Single item
/api/{resource}/{id}/{sub}  → Sub-resource

Örnekler:
GET    /api/students              → Liste
GET    /api/students/{id}         → Tek öğrenci
POST   /api/students              → Oluştur
PUT    /api/students/{id}         → Güncelle
DELETE /api/students/{id}         → Sil
PATCH  /api/students/{id}/archive → Kısmi güncelleme

GET    /api/students/{id}/lessons → Öğrencinin dersleri
POST   /api/attendance/bulk       → Toplu işlem
```

### 4.2 Query Parameters
```
?page=1&pageSize=20          → Sayfalama
?search=Yusuf                → Arama
?groupId=xxx                 → Filtreleme
?sortBy=name&sortDir=asc     → Sıralama
?date=2026-05-11             → Tarih filtresi
```

### 4.3 HTTP Status Kodları
| Kod | Kullanım |
|-----|----------|
| 200 | Başarılı GET/PUT/PATCH |
| 201 | Başarılı POST (oluşturma) |
| 204 | Başarılı DELETE |
| 400 | Validasyon hatası |
| 401 | Kimlik doğrulama hatası |
| 403 | Yetki hatası (rol yetersiz) |
| 404 | Kaynak bulunamadı |
| 409 | Çakışma (duplicate) |
| 500 | Sunucu hatası |

### 4.4 DTO Kuralları
```csharp
// Request ve Response DTO'ları ayrılır
// Entity doğrudan API'ye dönülmez!

// ✅ DOĞRU
public class StudentResponseDto { ... }    // API yanıtı
public class CreateStudentDto { ... }      // Oluşturma isteği
public class UpdateStudentDto { ... }      // Güncelleme isteği
public class StudentListItemDto { ... }    // Liste görünümü (hafif)

// ❌ YANLIŞ
return Ok(studentEntity);  // Entity doğrudan dönülmez!
```

---

## 5. Güvenlik Kuralları

### 5.1 JWT Token Yapısı
```json
{
  "sub": "user-uuid",
  "tenant_id": "tenant-uuid",
  "role": "head_teacher",
  "name": "Ahmet Hoca",
  "exp": 1234567890
}
```

### 5.2 Rol Hiyerarşisi
```
super_admin > tenant_admin > head_teacher > assistant_teacher > parent > student
```

### 5.3 Yetkilendirme
```csharp
// Policy-based authorization
[Authorize(Policy = "RequireHeadTeacher")]  // head_teacher ve üstü
[HttpPost]
public async Task<IActionResult> CreateLesson(...)

// Tenant-scoped: kullanıcı sadece kendi tenant'ının verisine erişir
// Bu middleware tarafından otomatik sağlanır
```

### 5.4 Kimlik Doğrulama Akışı (Auth Flow)
```text
1. İstemci (Web/Mobil) /api/auth/send-otp endpoint'ine telefon numarası gönderir.
2. AuthService kullanıcıyı veritabanında (tenant filtresi olmadan - IgnoreQueryFilters ile) bulur.
3. Bulursa bir OTP üretir, Cache'e yazar ve (gerçekte) SMS atar.
4. İstemci OTP kodunu alır, /api/auth/verify-otp endpoint'ine telefon ve kod ile gönderir.
5. AuthService Cache'deki kodu doğrular.
6. Doğrulama başarılıysa TokenHelper aracılığıyla JWT token üretilir.
7. Üretilen token response içine `TenantId` bilgisi de eklenerek (frontend'in bilmesi için) dönülür.
8. İstemci token'ı saklar ve sonraki tüm isteklere `Authorization: Bearer <token>` olarak ekler.
9. TenantResolutionMiddleware bu token'dan `tenant_id`'yi çıkarır ve DbContext'e enjekte eder.
```

---

## 6. Diğer Katmanları Tanıma

### 6.1 Frontend (Web) Hakkında
- **Tech:** React + TypeScript + Vite
- **API İletişimi:** Axios ile REST çağrıları
- **Auth:** JWT token `Authorization: Bearer {token}` header'ında gönderilir
- **Beklentiler:** Tüm yanıtlar JSON, tutarlı `{ success, message, data }` formatı
- **CORS:** `http://localhost:5173` (dev), production URL'si (prod)

### 6.2 Frontend (Mobil) Hakkında
- **Tech:** Flutter + Dart
- **API İletişimi:** Dio ile REST çağrıları
- **Auth:** JWT token secure storage'da saklanır, her istekte header'a eklenir
- **Offline:** İnternetsiz yapılan işlemler kuyruklanır, bağlantı gelince senkronize edilir
- **Beklentiler:** Aynı API endpoint'leri, ekstra mobil-özel endpoint gerekmez

### 6.3 Veritabanı Hakkında
- **ORM:** Entity Framework Core (**Script-First** — Migration KULLANILMAZ)
- **Şema yönetimi:** `database/` dizinindeki SQL scriptleri tek kaynak (single source of truth)
- **Entity oluşturma sırası:** Önce SQL script yazılır → sonra C# entity oluşturulur
- **Her tablo:** `Id` (UUID PK), `CreatedAt`, `UpdatedAt` alanları zorunlu
- **Tenant tabloları:** `TenantId` (UUID FK) zorunlu
- **Soft delete:** `IsDeleted` + `DeletedAt` alanları (fiziksel silme yapılmaz)
- **ENUM kullanılmaz:** Tüm enum değerleri `VARCHAR` olarak saklanır, validasyon uygulama katmanında yapılır

> ⚠️ **MUTLAK KURAL: SQL Script-First Yaklaşımı**
>
> 1. Yeni tablo veya kolon eklerken **önce** `database/` dizinine SQL scripti yazılır
> 2. **Ardından** backend entity'si SQL scriptindeki tablo adı, kolon adları ve veri tipleri
>    birebir eşleşecek şekilde oluşturulur/güncellenir
> 3. EF Core Migration (`dotnet ef migrations add`) **ASLA KULLANILMAZ**
> 4. Veritabanı şeması değişiklikleri `database/` klasöründeki scriptler üzerinden yönetilir
> 5. Entity property adları → snake_case DB kolon adlarıyla `UseSnakeCaseNamingConvention()` ile otomatik eşleşir

---

## 7. Git Kuralları

### 7.1 Branch Stratejisi
```
main              → Production
develop           → Geliştirme birleştirme
feature/xxx       → Yeni özellik
bugfix/xxx        → Hata düzeltme
hotfix/xxx        → Acil düzeltme
```

### 7.2 Commit Mesajı Formatı
```
feat(students): öğrenci CRUD API eklendi
fix(auth): token yenileme hatası düzeltildi
refactor(lessons): ders servisi optimize edildi
docs(readme): API dökümantasyonu güncellendi
```

---

## 8. Ortam Değişkenleri

```json
// appsettings.json yapısı
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "hafiz-takip-api",
    "Audience": "hafiz-takip-clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  },
  "SmsSettings": {
    "Provider": "netgsm",
    "ApiKey": "...",
    "SenderName": "HAFIZ TAKIP"
  }
}
```

---

*Son güncelleme: Temmuz 2026*
