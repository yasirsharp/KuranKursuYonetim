# 🗄️ Veritabanı Scriptleri

PostgreSQL · SQL Script-First (EF Core Migration ve `EnsureCreated` kullanılmaz — bkz. `DEVELOPER_RULES.md`)

## Dosya Düzeni

| Dosya | Amaç |
|-------|------|
| `00_full_schema.sql` | Şemanın **güncel son hali**. Sıfırdan kurulumda kullanılır; içerdiği migration'ları `schema_migrations` tablosuna uygulanmış olarak yazar. |
| `02_quran_reference.sql` | Kur'an referans verisi (114 sure, 604 sayfa, cüz eşlemesi). Tüm ortamlarda çalışır, tekrar çalıştırılabilir. |
| `01_seed_data.sql` | Geliştirme ortamı demo verisi (yalnızca development). |
| `migrations/NNN_aciklama.sql` | Mevcut bir veritabanını bir sonraki şema sürümüne taşıyan artımlı scriptler. |
| `db.ps1` | Kurulum ve migration aracı (aşağıda). |
| `KURAN_REFERANS_KONTROL.md` | Sure başlangıç sayfalarının bir hoca tarafından mushaftan kontrolü için liste. |

## db.ps1

`psql` PATH'te değilse `C:\Program Files\PostgreSQL\<sürüm>\bin` otomatik bulunur. Parola `PGPASSWORD` ortam değişkeninden okunur (tanımlı değilse psql sorar).
Güvenlik için yalnızca adı `hafiz` ile başlayan veritabanlarında çalışır.

```powershell
$env:PGPASSWORD = 'postgres'

# Sıfırdan kurulum: 00_full_schema → 02_quran_reference → 01_seed_data
.\database\db.ps1 -Setup -Database HafizTakipDB

# Var olan veritabanını silip yeniden kurma
.\database\db.ps1 -Setup -Database HafizTakipDB -Force

# Demo verisi olmadan kurulum
.\database\db.ps1 -Setup -Database HafizTakipDB -NoSeed

# Mevcut veritabanını güncelleme: 02_quran_reference → bekleyen migration'lar
.\database\db.ps1 -Migrate -Database HafizTakipDB

# Hangi migration'lar uygulandı?
.\database\db.ps1 -Status -Database HafizTakipDB
```

`-Migrate`, `schema_migrations` tablosunda kaydı olmayan `migrations/*.sql` dosyalarını numara sırasıyla, her birini tek transaction içinde uygular. Bir migration hata verirse o migration'ın hiçbir değişikliği kalıcı olmaz ve sonrakiler çalıştırılmaz.

## Yeni Şema Değişikliği Ekleme

1. `migrations/` altına sıradaki numarayla yeni bir dosya açın (ör. `006_tenant_settings.sql`). Script tekrar çalıştırılabilir olmalı (`IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, `DROP ... IF EXISTS`).
2. Aynı değişikliği `00_full_schema.sql`'e işleyin ve dosyanın sonundaki `schema_migrations` listesine yeni sürümü ekleyin.
3. C# entity'lerini script'e uygun şekilde güncelleyin.
4. Uygulanmış bir migration dosyası sonradan değiştirilmez; düzeltme için yeni bir migration açılır.

Doğrulama: sıfırdan kurulan (`-Setup`) ve eski bir veritabanından güncellenen (`-Migrate`) iki veritabanının şeması birebir aynı olmalıdır.

## Kur'an Referans Verisi

Mushaf düzeni **Türkiye ayetberkenar hafız mushafı (604 sayfa)**. Cüz eşlemesi formüldür (cüz 1 = s.1–21, her cüz 20 sayfa, cüz 30 = s.582–604).
Sure başlangıç sayfaları taslaktır; `KURAN_REFERANS_KONTROL.md` ile kontrol edilip düzeltmeler `02_quran_reference.sql`'e işlenmelidir. Düzeltme sonrası `-Migrate` çalıştırmak, ders kayıtlarındaki sure/cüz bilgisini de günceller.
