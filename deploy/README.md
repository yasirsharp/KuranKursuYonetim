# Yayın — test sunucusu

Test ortamı iki alt alan adındadır (Natro Windows Plesk, IIS + .NET 8):

| | Adres | FTP klasörü (ana dizin) |
|---|---|---|
| Web (React) | https://hafiztakip.yasirsharp.com.tr | `/hafiztakip.yasirsharp.com.tr` |
| API (.NET) | https://hafiztakip.api.yasirsharp.com.tr | `/hafiztakip.api.yasirsharp.com.tr` |

Web ve mobil uygulama API'ye `hafiztakip.api` adresinden bağlanır; API tarayıcı isteklerini yalnızca web adresinden kabul eder (CORS).
API alt alan adının kendi SSL sertifikası olmalıdır (`*.yasirsharp.com.tr` sertifikası iki seviyeli alt alan adını kapsamaz).
Veritabanı: Neon PostgreSQL (Frankfurt), veritabanı adı `hafiztakip`.
Ortam: `Staging`. Gönderim `.env`'deki bilgilere göre kurulur (bkz. [SMS ve e-posta](#sms-ve-e-posta-gönderimi)):

| `.env`'de | SMS | E-posta | Doğrulama kodu | Giriş sayfasındaki "test ortamı" ipucu |
|---|---|---|---|---|
| NetGSM ve SMTP yok | gönderilmez (loga yazılır) | gönderilmez | sabit `123456` | var |
| Yalnız NetGSM | NetGSM OTP SMS | gönderilmez | sabit `123456` | var |
| NetGSM ve SMTP | NetGSM OTP SMS | SMTP | rastgele, 3 dakika | yok |

Canlı ortamda (`Production`) NetGSM ve SMTP zorunludur: paket betiği bunlar olmadan paket üretmez, uygulama da `Sms:Provider = netgsm` ve `Email:Provider = smtp` olmadan başlamaz.

## Güncelleme ("test edelim")

```powershell
pwsh ./deploy/veritabani-kur.ps1 -Migrate    # yeni migration varsa (önce)
pwsh ./deploy/sunucuya-yukle.ps1             # derle, paketle, FTP ile yükle
```

Natro FTP'si `.dll` dosyalarını reddediyor (550). O durumda `pwsh ./deploy/sunucuya-yukle.ps1 -Zip` kullanılır:
betik API ve web'i ZIP olarak gönderir, bakım modunu açar ve her ZIP için Plesk Dosya Yöneticisi'nde
**Extract Files** ("Replace existing files" işaretli) yapılmasını bekler; Enter'a basılınca bakım modunu ve ZIP'i kaldırır.

API IIS'te **out-of-process** çalışır: aynı uygulama havuzundaki diğer ASP.NET Core uygulamalarıyla çakışmaz
(in-process modda havuz başına tek uygulama çalışır, diğerleri HTTP 500.35 verir).

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
| `HAFIZ_NETGSM_USERCODE`, `HAFIZ_NETGSM_PASSWORD`, `HAFIZ_NETGSM_HEADER` | NetGSM OTP SMS: API kullanıcı adı (abone no), API şifresi, onaylı gönderici adı. Üçü birlikte verilir |
| `HAFIZ_SMTP_HOST`, `HAFIZ_SMTP_USER`, `HAFIZ_SMTP_PASS`, `HAFIZ_SMTP_FROM` | E-posta gönderimi (dördü birlikte). `HAFIZ_SMTP_PORT` varsayılan `587`, `HAFIZ_SMTP_SSL` varsayılan `true` |

## SMS ve e-posta gönderimi

Doğrulama kodları SMS'te **NetGSM OTP SMS** kanalıyla gider (toplu SMS kuyruğuna girmez, en geç 3 dakikada ulaşır).
Kurs başvurusunun onay/red bildirimi e-postayla gider; SMS yalnızca kod içindir.

NetGSM panelinde:
1. **OTP SMS paketi** tanımlı olmalı (normal SMS paketi bu kanalda geçmez; kod 60).
2. **Gönderici adı (başlık)** onaylı olmalı; `HAFIZ_NETGSM_HEADER` panelde göründüğü gibi yazılır (kod 40/41).
3. **Ayarlar → API İşlemleri**: API erişimi açık bir kullanıcı. Şifresinde `&` ve `<` olmamalı — NetGSM bu karakterli
   isteği doğru kaçışlansa bile reddediyor (kod 73); betik böyle bir şifreyi baştan reddeder.
4. API için **IP kısıtlaması** açıksa izinli IP'lere sunucunun IP adresi (`94.73.149.118`) ve denemeyi yapacağınız
   bilgisayarın IP adresi eklenir (kod 30).

Bilgileri `.env`'e yazdıktan sonra yüklemeden önce deneyin (kendi telefonunuza bir deneme SMS'i gelir):

```powershell
pwsh ./deploy/sms-dene.ps1 -Telefon 05XXXXXXXXX
```

Başarısızsa betik NetGSM'in hata kodunu ve ne yapılacağını yazar. Sunucuda gönderim başarısız olursa
uygulama logunda yalnızca NetGSM'in kodu ve mesajı görünür (telefon numarası ve kod loglanmaz).

NetGSM kuralları: Türkçe karakter desteklenmez (uygulama mesajı otomatik olarak `ı→i, ş→s…` çevirir),
mesaj tek SMS boyunda olmalı, dakikada en çok 100 gönderim, OTP paketi 1 yıl geçerlidir.

Gereken araçlar: PowerShell 7 (`pwsh`), .NET 8 SDK, Node 20+, `psql` (yalnızca `veritabani-kur.ps1` için).
