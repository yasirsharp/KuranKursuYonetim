# Yayın — test sunucusu

Test ortamı iki alt alan adındadır (Natro Windows Plesk, IIS + .NET 8):

| | Adres | FTP klasörü (ana dizin) |
|---|---|---|
| Web (React) | https://hafiztakip.yasirsharp.com.tr | `/hafiztakip.yasirsharp.com.tr` |
| API (.NET) | https://hafiztakip.api.yasirsharp.com.tr | `/hafiztakip.api.yasirsharp.com.tr` |

Web ve mobil uygulama API'ye `hafiztakip.api` adresinden bağlanır; API tarayıcı isteklerini yalnızca web adresinden kabul eder (CORS).
API alt alan adının kendi SSL sertifikası olmalıdır (`*.yasirsharp.com.tr` sertifikası iki seviyeli alt alan adını kapsamaz).
Veritabanı: Neon PostgreSQL (Frankfurt), veritabanı adı `hafiztakip`.
Ortam: `Staging` — SMS ve e-posta gönderilmez, doğrulama kodu sabit `123456` (telefon veya e-posta ile giriş).
Canlı ortamda (`Production`) `Email:Provider = smtp` ve `Email:Smtp` (Host, Port, UseSsl, UserName, Password, FromAddress) ayarları zorunludur; aksi halde uygulama başlamaz.

## Güncelleme ("test edelim")

```powershell
pwsh ./deploy/veritabani-kur.ps1 -Migrate    # yeni migration varsa (önce)
pwsh ./deploy/sunucuya-yukle.ps1             # derle, paketle, FTP ile yükle
```

Mobil uygulama (Flutter) bu sunucuya bağlanır; test APK'sı GitHub Actions'taki **Mobil APK** iş akışında derlenir (bkz. `frontend/mobile/README.md`).

`sunucuya-yukle.ps1` önce API'yi (yükleme süresince `app_offline.htm` ile bakım modunda), sonra web'i yükler;
web'de `index.html` ve `web.config` en son gönderilir. Hedef yalnızca adı `hafiz` ile başlayan bir klasör olabilir
(FTP ana dizininde veya `/httpdocs` altında); ana site ve diğer alt alan adları korunur.
Web klasöründeki `web.config` IIS URL Rewrite modülünü kullanır (Plesk'te kuruludur).

## Gizli ayarlar

Yerelde kök dizindeki `.env` dosyasından (depoya girmez), bulut/CI ortamında **ortam değişkenlerinden** okunur:

| Değişken | Açıklama |
|---|---|
| `FTP_HOST`, `FTP_USER`, `FTP_PASS` | Plesk FTP hesabı (FTPS) |
| `HAFIZ_FTP_API_DIR`, `HAFIZ_FTP_WEB_DIR` | İsteğe bağlı; test ortamında varsayılan yukarıdaki klasörler (canlıda zorunlu) |
| `HAFIZ_API_URL`, `HAFIZ_WEB_URL` | İsteğe bağlı; test ortamında varsayılan yukarıdaki adresler (canlıda zorunlu) |
| `HAFIZ_DB_CONNECTION` | Neon bağlantısı (`postgresql://…` veya Npgsql biçimi) |
| `HAFIZ_JWT_KEY` | Oturum imzalama anahtarı — sunucudakiyle **aynı** olmalı, yoksa açık oturumlar düşer. `.env` olmayan ortamda zorunlu |
| `HAFIZ_DB_NAME` | İsteğe bağlı, varsayılan `hafiztakip` |

Gereken araçlar: PowerShell 7 (`pwsh`), .NET 8 SDK, Node 20+, `psql` (yalnızca `veritabani-kur.ps1` için).
