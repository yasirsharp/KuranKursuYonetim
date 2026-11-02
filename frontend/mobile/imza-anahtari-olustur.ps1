<#
.SYNOPSIS
  Android uygulaması için kalıcı imza anahtarını üretir (bir kez çalıştırılır).

.DESCRIPTION
  Aynı anahtarla imzalanan yeni sürüm, telefondaki eski sürümün üstüne kurulabilir. Anahtar kaybolursa
  uygulamayı kuran herkesin onu silip yeniden kurması gerekir; bu yüzden:
    - Anahtar depo klasörünün DIŞINA yazılır (varsayılan: Belgeler\HafizTakip-imza).
    - Var olan bir anahtarın üzerine asla yazılmaz.
    - Şifre ve anahtar ekrana yazılmaz; GitHub'a eklenmek üzere sırayla panoya kopyalanır.
  Java gerekmez: anahtar .NET ile PKCS12 biçiminde üretilir (RSA 4096, SHA-256, 30 yıl geçerli).

  GitHub'da eklenecek gizli bilgiler (Settings -> Secrets and variables -> Actions):
    ANDROID_KEYSTORE_BASE64    -> anahtar dosyası (base64)
    ANDROID_KEYSTORE_PASSWORD  -> şifre

.EXAMPLE
  pwsh ./frontend/mobile/imza-anahtari-olustur.ps1
#>
param(
    [string]$OutDir = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'HafizTakip-imza')
)

$ErrorActionPreference = 'Stop'
$keystorePath = Join-Path $OutDir 'hafiz-takip-imza.p12'
$passwordPath = Join-Path $OutDir 'SIFRE-gizli-tutun.txt'

if (Test-Path $keystorePath) {
    throw "Zaten bir imza anahtarı var: $keystorePath`nÜzerine yazılmaz (eski anahtarla imzalanan uygulamalar güncellenemez hale gelir). GitHub'a yeniden eklemek için bu dosyayı kullanın."
}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# Şifre: yalnızca harf ve rakam (Gradle ve GitHub'da kaçış sorunu olmasın), 32 karakter
$alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'.ToCharArray()
$password = -join (1..32 | ForEach-Object { $alphabet[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($alphabet.Length)] })

$rsa = [System.Security.Cryptography.RSA]::Create(4096)
$request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
    'CN=Hafiz Takip, O=Hafiz Takip, C=TR', $rsa,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
$cert = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddYears(30))
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
[IO.File]::WriteAllBytes($keystorePath, $bytes)
[IO.File]::WriteAllText($passwordPath,
    "Hafız Takip Android imza anahtarı şifresi (kimseyle paylaşmayın):`r`n$password`r`n`r`nAnahtar: $keystorePath`r`n" +
    "Bu klasörü bir USB belleğe veya güvenli bir buluta yedekleyin. Kaybolursa uygulama güncellenemez.`r`n",
    (New-Object Text.UTF8Encoding $true))

$base64 = [Convert]::ToBase64String($bytes)
$fingerprint = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($cert.RawData))

Write-Host ''
Write-Host 'İmza anahtarı üretildi.' -ForegroundColor Green
Write-Host "  Klasör: $OutDir"
Write-Host "  Parmak izi (SHA-256): $fingerprint"
Write-Host '  Bu klasörü hemen bir USB belleğe veya güvenli bir yere yedekleyin.' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Şimdi GitHub sayfasını açın:' -ForegroundColor Cyan
Write-Host '  https://github.com/yasirsharp/KuranKursuYonetim/settings/secrets/actions/new'
Write-Host ''

$canCopy = $IsWindows -and [bool](Get-Command Set-Clipboard -ErrorAction SilentlyContinue)
if ($canCopy) {
    Set-Clipboard -Value $base64
    Write-Host '1) Anahtar panoya kopyalandı.' -ForegroundColor Cyan
    Write-Host '   Name: ANDROID_KEYSTORE_BASE64   Secret: Ctrl+V   -> Add secret'
    Read-Host '   Ekledikten sonra Enter' | Out-Null

    Set-Clipboard -Value $password
    Write-Host '2) Şifre panoya kopyalandı. Sayfayı yeniden açın (New repository secret).' -ForegroundColor Cyan
    Write-Host '   Name: ANDROID_KEYSTORE_PASSWORD   Secret: Ctrl+V   -> Add secret'
    Read-Host '   Ekledikten sonra Enter' | Out-Null

    Set-Clipboard -Value ' '  # gizli bilgi panoda kalmasın
} else {
    $base64Path = Join-Path $OutDir 'anahtar-base64.txt'
    [IO.File]::WriteAllText($base64Path, $base64)
    Write-Host "Pano kullanılamadı. GitHub'a eklenecek değerler bu dosyalarda:" -ForegroundColor Yellow
    Write-Host "  ANDROID_KEYSTORE_BASE64   -> $base64Path (ekledikten sonra bu dosyayı silin)"
    Write-Host "  ANDROID_KEYSTORE_PASSWORD -> $passwordPath"
}

Write-Host ''
Write-Host "Tamam. Bundan sonraki APK'lar bu anahtarla imzalanacak." -ForegroundColor Green
Write-Host 'Telefonda eski (geçici anahtarlı) sürüm varsa son bir kez kaldırıp yeni sürümü kurun; sonraki güncellemeler üstüne kurulur.'
