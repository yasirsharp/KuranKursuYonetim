# 🌐 Web Frontend Developer Rules

> **Vite · React · TypeScript · Vanilla CSS**
> Bu dosya web frontend geliştirme kurallarını tanımlar.

---

## 1. TypeScript Zorunluluğu

- **Strict Mode aktiftir** — `tsconfig.json` içinde `strict: true`
- **`any` tipi KESİNLİKLE YASAKTIR** — Tüm değişkenler, parametreler ve dönüş tipleri açıkça belirtilmelidir
- **Backend DTO eşleşmesi** — Her backend DTO'nun karşılığı `src/types/` altında interface olarak tanımlanmalıdır
- **Type assertion (`as`) yerine type guard tercih edilmelidir**

---

## 2. Dosya ve İsimlendirme Kuralları

> ⚠️ Bu kurallar `.agents/AGENTS.md` ile tam tutarlıdır.

- **Her bileşen kendi dosyasında** — Bir dosyaya birden fazla `export default` bileşen YASAKTIR
- **Dosya adı = Bileşen adı** — `DashboardPage.tsx` dosyası `DashboardPage` bileşenini içermelidir
- **Keyfi önek yasak** — `Phase1...`, `Temp...`, `All...` gibi gruplama önekleri YASAKTIR
- **Maksimum dosya boyutu** — 200 satırı aşan dosyalar alt bileşenlere bölünmelidir

---

## 3. Klasör Yapısı

```
src/
├── api/          # Backend API çağrıları (Axios wrapper'lar)
├── components/   # Paylaşılan UI bileşenleri
│   ├── ui/       # Atomik bileşenler (Button, Card, Input)
│   └── shared/   # Ortak bileşenler (StatCard, Avatar)
├── context/      # React Context'ler
├── hooks/        # Custom React Hook'lar
├── layouts/      # Rol bazlı layout'lar (AuthLayout, TeacherLayout)
├── pages/        # Layout'a göre gruplanmış sayfalar
│   ├── auth/     # Giriş sayfaları
│   ├── teacher/  # Öğretmen paneli sayfaları
│   ├── admin/    # Kurs yönetim sayfaları
│   └── platform/ # Süper admin sayfaları
├── services/     # İş mantığı servisleri (Logger)
├── types/        # TypeScript interface'ler
├── utils/        # Yardımcı fonksiyonlar
└── styles/       # CSS dosyaları
```

---

## 4. Loglama Kuralı

- **`console.log`, `console.warn`, `console.error` doğrudan KULLANILMAZ**
- **Tüm loglar `Logger` servisi üzerinden yapılır:**

```typescript
import { Logger } from '../services/logger';
const log = Logger.create('BileşenAdı');

log.debug('metodAdı', 'Mesaj', { opsiyonel: 'veri' });
log.info('metodAdı', 'Mesaj');
log.warn('metodAdı', 'Mesaj');
log.error('metodAdı', 'Mesaj', error);
```

---

## 5. API Çağrı Kuralları

- **Doğrudan `axios.get()` veya `fetch()` kullanılmaz** — Tüm çağrılar `src/api/` altındaki fonksiyonlar üzerinden yapılır
- **API client** (`src/api/client.ts`) JWT token'ı otomatik ekler ve loglar
- **Hata yönetimi** — 401 hatası otomatik olarak login'e yönlendirir
- **Ortam değişkeni** — API URL `VITE_API_URL` üzerinden yönetilir (development'ta Vite proxy kullanılır)

---

## 6. CSS Kuralları

- **Tailwind CSS KULLANILMAZ** — Vanilla CSS + CSS Variables
- **Tema değişkenleri** `src/styles/globals.css` içinde `:root` altında tanımlıdır
- **Inline style yerine CSS sınıfları tercih edilir**
- **Glassmorphism ve gradient efektler** `.glass-card`, `.gradient-green` gibi hazır utility sınıflarıyla uygulanır

---

## 7. State Yönetimi

- **Global state**: Sadece `AuthContext` (kullanıcı oturumu) global tutulur
- **Veri state**: Backend'den gelen veriler sayfa seviyesinde `useState` + `useEffect` ile yönetilir
- **Gereksiz global state'ten kaçınılır** — Her sayfanın verisini kendi hook'u çeker

---

## 8. Diğer Katmanları Tanıma

### Backend Hakkında
- **.NET 8 Web API**, PostgreSQL, multi-tenant
- **Auth**: Telefon + OTP → JWT token
- **API format (KRİTİK)**: 
  - **GET istekleri (Veri Okuma)**: `{ success, message, data }` formatında döner (`IDataResult<T>`). Veriyi okurken *her zaman* `response.data.data` kullanılmalıdır.
  - **POST/PUT/DELETE istekleri (İşlem)**: Sadece `{ success, message }` döner (`IResult`), `data` alanı YOKTUR.
- **Roller**: `super_admin > tenant_admin > head_teacher > assistant_teacher > parent`
- **Tenant izolasyonu**: JWT token içindeki `tenant_id` claim'i ile otomatik

### Mobile Hakkında
- Flutter + Riverpod + Dio
- Aynı backend API'yi kullanır
- Offline-first mimari (mobilde)

---

*Son güncelleme: Ağustos 2026*
