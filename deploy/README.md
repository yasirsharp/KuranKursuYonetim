# Yayın — test sunucusu

Test adresi: **https://hafiz.yasirsharp.com.tr** (Natro Windows Plesk, IIS + .NET 8; alt alan adı klasörü `/httpdocs/hafiz`)
Veritabanı: Neon PostgreSQL (Frankfurt), veritabanı adı `hafiztakip`.
Ortam: `Staging` — SMS ve e-posta gönderilmez, doğrulama kodu sabit `123456` (telefon veya e-posta ile giriş).
Canlı ortamda (`Production`) `Email:Provider = smtp` ve `Email:Smtp` (Host, Port, UseSsl, UserName, Password, FromAddress) ayarları zorunludur; aksi halde uygulama başlamaz.

## Güncelleme ("test edelim")

```powershell
pwsh ./deploy/veritabani-kur.ps1 -Migrate    # yeni migration varsa (önce)
pwsh ./deploy/sunucuya-yukle.ps1             # derle, paketle, FTP ile yükle
```

Mobil uygulama (Flutter) bu sunucuya bağlanır; test APK'sı GitHub Actions'taki **Mobil APK** iş akışında derlenir (bkz. `frontend/mobile/README.md`).

`sunucuya-yukle.ps1` uygulamayı yükleme süresince `app_offline.htm` ile bakım moduna alır. Hedef yalnızca
`/httpdocs/<alt-klasör>` olabilir; ana sitenin (`/httpdocs`) köküne yükleme reddedilir.

## Gizli ayarlar

Yerelde kök dizindeki `.env` dosyasından (depoya girmez), bulut/CI ortamında **ortam değişkenlerinden** okunur:

| Değişken | Açıklama |
|---|---|
| `FTP_HOST`, `FTP_USER`, `FTP_PASS` | Plesk FTP hesabı (FTPS) |
| `HAFIZ_FTP_REMOTE_DIR` | `/httpdocs/hafiz` |
| `HAFIZ_DB_CONNECTION` | Neon bağlantısı (`postgresql://…` veya Npgsql biçimi) |
| `HAFIZ_JWT_KEY` | Oturum imzalama anahtarı — sunucudakiyle **aynı** olmalı, yoksa açık oturumlar düşer. `.env` olmayan ortamda zorunlu |
| `HAFIZ_DB_NAME` | İsteğe bağlı, varsayılan `hafiztakip` |

Gereken araçlar: PowerShell 7 (`pwsh`), .NET 8 SDK, Node 20+, `psql` (yalnızca `veritabani-kur.ps1` için).
