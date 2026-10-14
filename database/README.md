# 🗄️ Veritabanı Scriptleri

PostgreSQL · SQL Script-First (EF Core Migration kullanılmaz — bkz. `DEVELOPER_RULES.md`)

## Dosya Düzeni

| Dosya | Amaç |
|-------|------|
| `00_full_schema.sql` | Şemanın **güncel son hali**. Sıfırdan kurulumda kullanılır. |
| `01_seed_data.sql` | Geliştirme ortamı demo verisi (yalnızca development). |
| `migrations/NNN_aciklama.sql` | Mevcut bir veritabanını bir sonraki şema sürümüne taşıyan artımlı scriptler. |

## Sıfırdan Kurulum (development)

```bash
psql -U postgres -c "CREATE DATABASE \"HafizTakipDB\";"
psql -U postgres -d HafizTakipDB -f database/00_full_schema.sql
psql -U postgres -d HafizTakipDB -f database/01_seed_data.sql
```

`00_full_schema.sql` tüm migration'ları zaten içerdiği için sıfırdan kurulumda `migrations/` klasörünü ayrıca çalıştırmaya gerek yoktur.

## Mevcut Veritabanını Güncelleme

`migrations/` altındaki scriptleri, veritabanına daha önce uygulanmamış olanlardan başlayarak **numara sırasıyla** çalıştırın:

```bash
psql -U postgres -d HafizTakipDB -f database/migrations/001_xxx.sql
```

## Kurallar

- Her şema değişikliği hem yeni bir `migrations/NNN_*.sql` dosyasına hem de `00_full_schema.sql` dosyasına işlenir.
- Uygulanmış bir migration dosyası sonradan değiştirilmez; düzeltme için yeni bir migration açılır.
- Scriptler tekrar çalıştırılabilir yazılır (`IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, `DROP ... IF EXISTS`).
- C# entity'leri scriptler yazıldıktan **sonra** güncellenir.
