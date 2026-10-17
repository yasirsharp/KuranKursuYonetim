<#
.SYNOPSIS
  Sunucuya yüklenecek yayın paketini hazırlar: API + React uygulaması (tek alan adı) + veritabanı betikleri.

.DESCRIPTION
  Çıktı: deploy\out\
    app\        -> sunucudaki uygulama klasörüne (alt alan adının kök dizini) yüklenir
    database\   -> veritabanını kurmak için SQL betikleri (sırayla: 00, 02, 01)
    BENIOKU.txt -> kurulum adımları

  -Ortam Staging (varsayılan): test sunucusu. SMS gönderilmez, doğrulama kodu sabittir (123456),
                               giriş sayfasında "test ortamı" ipucu görünür.
  -Ortam Production          : canlı. Gerçek SMS (NetGSM) ve rastgele kod zorunludur.

  appsettings.<Ortam>.json pakete rastgele bir JWT anahtarıyla üretilir; bağlantı cümlesini sunucuda
  (veya yüklemeden önce) doldurun. Bu dosya gizlidir: depoya eklenmez, kimseyle paylaşılmaz.

.EXAMPLE
  .\deploy\paket-olustur.ps1
  .\deploy\paket-olustur.ps1 -Runtime win-x64 -ConnectionString "Host=...;Database=...;Username=...;Password=..."
#>
param(
    [ValidateSet('Staging', 'Production')]
    [string]$Ortam = 'Staging',

    # Boş: sunucuda .NET 8 çalışma zamanı kurulu olmalı (çoğu panel). win-x64 / linux-x64: çalışma zamanı paketin içinde gelir.
    [ValidateSet('', 'win-x64', 'linux-x64')]
    [string]$Runtime = '',

    [string]$ConnectionString = '',

    # Boş: rastgele üretilir. Sunucuya her yüklemede aynı anahtar verilmezse açık oturumlar düşer.
    [string]$JwtKey = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $PSScriptRoot 'out'
$app = Join-Path $out 'app'

function Invoke-Native([string]$Title, [scriptblock]$Command) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Title başarısız (çıkış kodu $LASTEXITCODE)." }
}

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $app | Out-Null

# 1) Frontend
Push-Location (Join-Path $root 'frontend/web')
try {
    if (-not (Test-Path 'node_modules')) { Invoke-Native 'npm ci' { npm ci } }
    $env:VITE_SHOW_DEV_HINTS = if ($Ortam -eq 'Staging') { 'true' } else { 'false' }
    Invoke-Native 'Frontend derleniyor' { npm run build }
} finally {
    Remove-Item Env:\VITE_SHOW_DEV_HINTS -ErrorAction SilentlyContinue
    Pop-Location
}

# 2) API (web.config içine ASPNETCORE_ENVIRONMENT yazılır)
$publishArgs = @('publish', (Join-Path $root 'backend/src/HafizTakip.WebAPI/HafizTakip.WebAPI.csproj'),
    '-c', 'Release', '-o', $app, "-p:EnvironmentName=$Ortam", '-nologo', '-v', 'q')
if ($Runtime) { $publishArgs += @('-r', $Runtime, '--self-contained', 'true') }
Invoke-Native 'API yayınlanıyor' { dotnet @publishArgs }

# Geliştirme ayarları (yerel bağlantı, geliştirme JWT anahtarı) pakete girmez
Remove-Item (Join-Path $app 'appsettings.Development.json') -ErrorAction SilentlyContinue

# 3) React uygulaması API'nin wwwroot klasörüne
Copy-Item (Join-Path $root 'frontend/web/dist') (Join-Path $app 'wwwroot') -Recurse

# 4) Ortam ayarları (gizli): JWT anahtarı verilmediyse rastgele
if (-not $JwtKey) {
    $keyBytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($keyBytes)
    $JwtKey = [Convert]::ToBase64String($keyBytes)
}
$settings = [ordered]@{
    ConnectionStrings = [ordered]@{ PostgreSqlConnection = $ConnectionString }
    TokenOptions      = [ordered]@{ SecurityKey = $JwtKey }
    Logging           = [ordered]@{ LogLevel = [ordered]@{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning' } }
}
if ($Ortam -eq 'Staging') {
    $settings.Otp = [ordered]@{ FixedCode = '123456'; ResendCooldownSeconds = 10 }
    $settings.Sms = [ordered]@{ Provider = 'console' }
}
$settingsJson = $settings | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $app "appsettings.$Ortam.json"), $settingsJson, (New-Object Text.UTF8Encoding $false))

# 5) Veritabanı betikleri
$db = Join-Path $out 'database'
New-Item -ItemType Directory -Path $db | Out-Null
Copy-Item (Join-Path $root 'database/00_full_schema.sql'), (Join-Path $root 'database/02_quran_reference.sql') $db
if ($Ortam -eq 'Staging') { Copy-Item (Join-Path $root 'database/01_seed_data.sql') $db }
Copy-Item (Join-Path $root 'database/migrations') (Join-Path $db 'migrations') -Recurse

# 6) Kurulum notu
$readme = @"
Hafız Takip — yayın paketi ($Ortam)

1) Veritabanı (PostgreSQL 13+; boş bir veritabanı):
   database\00_full_schema.sql  -> şema
   database\02_quran_reference.sql -> Kur'an referans verisi
$(if ($Ortam -eq 'Staging') { "   database\01_seed_data.sql    -> örnek kurs, kullanıcılar ve platform yöneticisi (yalnızca test)" })
   Sıra önemlidir. Sonraki sürümlerde yalnızca database\migrations içindeki yeni betikler çalıştırılır.

2) app\appsettings.$Ortam.json içindeki ConnectionStrings:PostgreSqlConnection değerini doldurun:
   Host=SUNUCU;Port=5432;Database=VERITABANI;Username=KULLANICI;Password=SIFRE;SSL Mode=Prefer

3) app klasörünün TAMAMINI alt alan adının kök dizinine yükleyin (web.config dahil).
   Panelde .NET (ASP.NET Core) 8 desteğini açın; uygulama başlangıç dosyası: HafizTakip.WebAPI.dll

4) https://ALT-ALAN-ADI/ açıldığında giriş ekranı gelmelidir.
   Sorun olursa: app\logs (web.config'te stdoutLogEnabled=true yapılarak) veya panelin hata günlüğü.
"@
[IO.File]::WriteAllText((Join-Path $out 'BENIOKU.txt'), $readme, (New-Object Text.UTF8Encoding $true))

Write-Host ""
Write-Host "Paket hazır: $out" -ForegroundColor Green
if (-not $ConnectionString) {
    Write-Host "Not: app\appsettings.$Ortam.json içinde bağlantı cümlesi boş — sunucuda doldurulmalı." -ForegroundColor Yellow
}
