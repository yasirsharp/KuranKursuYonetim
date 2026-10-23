<#
.SYNOPSIS
  Sunucuya yüklenecek yayın paketini hazırlar: API + React uygulaması + veritabanı betikleri.

.DESCRIPTION
  İki düzen desteklenir:
    -ApiUrl ve -WebOrigin verilirse (ayrı alan adları): web ayrı bir sitedir ve API'ye ApiUrl'den bağlanır;
       API yalnızca WebOrigin'den gelen tarayıcı isteklerini kabul eder (CORS).
    Verilmezse (tek alan adı): React uygulaması API'nin wwwroot klasöründen sunulur.

  Çıktı: deploy\out\
    app\        -> API'nin alt alan adının kök dizinine yüklenir (tek alan adında web de içindedir)
    web\        -> (ayrı alan adlarında) web alt alan adının kök dizinine yüklenir
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
    [string]$JwtKey = '',

    # Ayrı alan adları: API adresi (web bu adrese bağlanır) ve web sitesinin adresi (CORS'a eklenir)
    [string]$ApiUrl = '',
    [string]$WebOrigin = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $PSScriptRoot 'out'
$app = Join-Path $out 'app'
$web = Join-Path $out 'web'
$splitHosts = [bool]$ApiUrl
if ($splitHosts -and -not $WebOrigin) { throw '-ApiUrl verildiğinde -WebOrigin de verilmelidir.' }
$ApiUrl = $ApiUrl.TrimEnd('/')
$WebOrigin = $WebOrigin.TrimEnd('/')

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
    # Ayrı alan adlarında web, API'ye tam adresle bağlanır; tek alan adında aynı adresten (göreli)
    $env:VITE_API_URL = $ApiUrl
    Invoke-Native 'Frontend derleniyor' { npm run build }
} finally {
    Remove-Item Env:\VITE_SHOW_DEV_HINTS -ErrorAction SilentlyContinue
    Remove-Item Env:\VITE_API_URL -ErrorAction SilentlyContinue
    Pop-Location
}

# 2) API (web.config içine ASPNETCORE_ENVIRONMENT yazılır)
$publishArgs = @('publish', (Join-Path $root 'backend/src/HafizTakip.WebAPI/HafizTakip.WebAPI.csproj'),
    '-c', 'Release', '-o', $app, "-p:EnvironmentName=$Ortam", '-nologo', '-v', 'q')
if ($Runtime) { $publishArgs += @('-r', $Runtime, '--self-contained', 'true') }
Invoke-Native 'API yayınlanıyor' { dotnet @publishArgs }

# Geliştirme ayarları (yerel bağlantı, geliştirme JWT anahtarı) pakete girmez
Remove-Item (Join-Path $app 'appsettings.Development.json') -ErrorAction SilentlyContinue

# 3) React uygulaması: ayrı alan adlarında kendi klasörüne (IIS için SPA yönlendirmesiyle),
#    tek alan adında API'nin wwwroot klasörüne
if ($splitHosts) {
    Copy-Item (Join-Path $root 'frontend/web/dist') $web -Recurse
    Copy-Item (Join-Path $PSScriptRoot 'web.config') (Join-Path $web 'web.config')
} else {
    Copy-Item (Join-Path $root 'frontend/web/dist') (Join-Path $app 'wwwroot') -Recurse
}

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
if ($splitHosts) { $settings.Cors = [ordered]@{ AllowedOrigins = @($WebOrigin) } }
if ($Ortam -eq 'Staging') {
    $settings.Otp = [ordered]@{ FixedCode = '123456'; ResendCooldownSeconds = 10 }
    $settings.Sms = [ordered]@{ Provider = 'console' }
    $settings.Email = [ordered]@{ Provider = 'console' }
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

3) app klasörünün TAMAMINI API alt alan adının kök dizinine yükleyin (web.config dahil).
   Panelde .NET (ASP.NET Core) 8 desteğini açın; uygulama başlangıç dosyası: HafizTakip.WebAPI.dll
$(if ($splitHosts) { "   web klasörünün TAMAMINI web alt alan adının kök dizinine yükleyin (web.config dahil; IIS URL Rewrite gerekir).
   Web, API'ye $ApiUrl adresinden bağlanır; API yalnızca $WebOrigin kaynağına izin verir." })

4) Web adresi açıldığında giriş ekranı gelmelidir.
   Sorun olursa: app\logs (web.config'te stdoutLogEnabled=true yapılarak) veya panelin hata günlüğü.
"@
[IO.File]::WriteAllText((Join-Path $out 'BENIOKU.txt'), $readme, (New-Object Text.UTF8Encoding $true))

Write-Host ""
Write-Host "Paket hazır: $out" -ForegroundColor Green
if (-not $ConnectionString) {
    Write-Host "Not: app\appsettings.$Ortam.json içinde bağlantı cümlesi boş — sunucuda doldurulmalı." -ForegroundColor Yellow
}
