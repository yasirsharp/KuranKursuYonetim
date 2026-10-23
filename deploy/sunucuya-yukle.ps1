<#
.SYNOPSIS
  Hafız Takip'i Plesk (Windows/IIS) alt alan adlarına FTP ile yükler: API ve web ayrı alt alan adlarındadır.

.DESCRIPTION
  Kök dizindeki .env dosyasından okunur (depoya girmez):
    FTP_HOST, FTP_USER, FTP_PASS   -> FTP bağlantısı (FTPS)
    HAFIZ_DB_CONNECTION            -> PostgreSQL bağlantı cümlesi
    HAFIZ_JWT_KEY                  -> oturum imzalama anahtarı; yoksa üretilip .env'e eklenir
                                      (her yüklemede aynı kalmalı, yoksa açık oturumlar düşer)
  Test ortamında (Staging) varsayılanı olan, gerekirse .env ile değiştirilebilen ayarlar:
    HAFIZ_FTP_API_DIR  = /hafiztakip.api.yasirsharp.com.tr   HAFIZ_API_URL = https://hafiztakip.api.yasirsharp.com.tr
    HAFIZ_FTP_WEB_DIR  = /hafiztakip.yasirsharp.com.tr       HAFIZ_WEB_URL = https://hafiztakip.yasirsharp.com.tr
  Güvenlik: yalnızca adı "hafiz" ile başlayan klasörlere (FTP ana dizininde veya /httpdocs altında) yüklenir;
  ana site ve diğer alt alan adları korunur.

  Akış: paket hazırlanır (deploy\paket-olustur.ps1) -> API: app_offline.htm yüklenir (IIS uygulamayı durdurur,
  kilitli DLL'ler serbest kalır), dosyalar yüklenir, app_offline.htm silinir -> web: önce dosyalar,
  en son index.html ve web.config (yarım sürüm görünmesin).

.EXAMPLE
  .\deploy\sunucuya-yukle.ps1               # test ortamı (Staging), sabit doğrulama kodu
  .\deploy\sunucuya-yukle.ps1 -SkipUpload   # yalnızca paketi hazırla
#>
param(
    [ValidateSet('Staging', 'Production')]
    [string]$Ortam = 'Staging',
    [switch]$SkipUpload
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
$cfg = Read-DotEnv $envFile

# Test ortamının alt alan adları (canlı ortamda .env'de açıkça verilmelidir)
if ($Ortam -eq 'Staging') {
    $defaults = @{
        HAFIZ_FTP_API_DIR = '/hafiztakip.api.yasirsharp.com.tr'; HAFIZ_API_URL = 'https://hafiztakip.api.yasirsharp.com.tr'
        HAFIZ_FTP_WEB_DIR = '/hafiztakip.yasirsharp.com.tr';     HAFIZ_WEB_URL = 'https://hafiztakip.yasirsharp.com.tr'
    }
    foreach ($key in $defaults.Keys) { if ([string]::IsNullOrWhiteSpace($cfg[$key])) { $cfg[$key] = $defaults[$key] } }
}
if (-not [string]::IsNullOrWhiteSpace($cfg['HAFIZ_FTP_REMOTE_DIR'])) {
    Write-Host "Not: HAFIZ_FTP_REMOTE_DIR artık kullanılmıyor (API ve web ayrı klasörlere yüklenir); .env'den silebilirsiniz." -ForegroundColor Yellow
}

foreach ($key in 'FTP_HOST', 'FTP_USER', 'FTP_PASS', 'HAFIZ_DB_CONNECTION', 'HAFIZ_FTP_API_DIR', 'HAFIZ_FTP_WEB_DIR', 'HAFIZ_API_URL', 'HAFIZ_WEB_URL') {
    if ([string]::IsNullOrWhiteSpace($cfg[$key])) { throw "$key eksik (.env dosyasında veya ortam değişkeni olarak verin)." }
}

# Hedef yalnızca adı "hafiz" ile başlayan bir klasör olabilir: FTP ana dizininde (Plesk alt alan adı klasörü)
# veya /httpdocs altında. Ana site (/httpdocs kökü) ve diğer alt alan adları korunur.
function Get-SafeRemoteDir([string]$Key) {
    $dir = '/' + $cfg[$Key].Trim().Trim('/')
    if ($dir -notmatch '^/(httpdocs/)?hafiz[A-Za-z0-9._-]*$') {
        throw "$Key 'hafiz' ile başlayan bir alt alan adı klasörü olmalı (ör. /hafiztakip.yasirsharp.com.tr); şu an: $dir"
    }
    return $dir
}
$apiDir = Get-SafeRemoteDir 'HAFIZ_FTP_API_DIR'
$webDir = Get-SafeRemoteDir 'HAFIZ_FTP_WEB_DIR'
if ($apiDir -eq $webDir) { throw 'API ve web aynı klasöre yüklenemez (HAFIZ_FTP_API_DIR ile HAFIZ_FTP_WEB_DIR farklı olmalı).' }
foreach ($key in 'HAFIZ_API_URL', 'HAFIZ_WEB_URL') {
    if ($cfg[$key] -notmatch '^https://[A-Za-z0-9.-]+/?$') { throw "$key 'https://alan.adi' biçiminde olmalı; şu an: $($cfg[$key])" }
}

# Oturum anahtarı sabit kalsın: yoksa üret ve .env'e ekle. .env yoksa (bulut/CI) üretmek açık oturumları
# düşürür ve anahtar kaybolur; orada HAFIZ_JWT_KEY ortam değişkeni zorunludur.
if ([string]::IsNullOrWhiteSpace($cfg['HAFIZ_JWT_KEY']) -and -not (Test-Path $envFile)) {
    throw 'HAFIZ_JWT_KEY eksik: .env olmayan ortamda sunucudaki anahtar ortam değişkeni olarak verilmelidir.'
}
if ([string]::IsNullOrWhiteSpace($cfg['HAFIZ_JWT_KEY'])) {
    $bytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $cfg['HAFIZ_JWT_KEY'] = [Convert]::ToBase64String($bytes)
    Add-Content -Path $envFile -Value "`r`n# Hafız Takip oturum anahtarı (sunucuya her yüklemede aynı kalmalı)`r`nHAFIZ_JWT_KEY=$($cfg['HAFIZ_JWT_KEY'])" -Encoding UTF8
    Write-Host "HAFIZ_JWT_KEY üretildi ve .env'e eklendi." -ForegroundColor Yellow
}

# 1) Paket: sunucudaki .NET 8 (ASP.NET Core) çalışma zamanını kullanır — Natro sunucusunda kurulu
#    (diğer .NET sitelerin de böyle çalışıyor). Kurulu olmayan bir sunucuda -Runtime win-x64 verin.
#    Bağlantı .env'de Neon URI'si olabilir; uygulamaya Npgsql biçiminde ve HAFIZ_DB_NAME veritabanıyla verilir.
$dbName = if ($cfg['HAFIZ_DB_NAME']) { $cfg['HAFIZ_DB_NAME'] } else { 'hafiztakip' }
$connectionString = ConvertTo-NpgsqlConnectionString (Get-PgConnectionInfo $cfg['HAFIZ_DB_CONNECTION'] -Database $dbName)
& (Join-Path $PSScriptRoot 'paket-olustur.ps1') -Ortam $Ortam -Runtime '' `
    -ConnectionString $connectionString -JwtKey $cfg['HAFIZ_JWT_KEY'] `
    -ApiUrl $cfg['HAFIZ_API_URL'] -WebOrigin $cfg['HAFIZ_WEB_URL']
$appDir = Join-Path $PSScriptRoot 'out/app'
$webPackageDir = Join-Path $PSScriptRoot 'out/web'
if (-not (Test-Path (Join-Path $appDir 'web.config'))) { throw 'Paket hazırlanamadı (API web.config yok).' }
if (-not (Test-Path (Join-Path $webPackageDir 'index.html'))) { throw 'Paket hazırlanamadı (web index.html yok).' }

if ($SkipUpload) {
    Write-Host "Yükleme atlandı (-SkipUpload). Paket: $(Join-Path $PSScriptRoot 'out')" -ForegroundColor Magenta
    return
}

# 2) FTP yardımcıları (FTPS)
$credential = New-Object System.Net.NetworkCredential($cfg['FTP_USER'], $cfg['FTP_PASS'])
$baseUrl = "ftp://$($cfg['FTP_HOST'])"

function New-FtpRequest([string]$path, [string]$method) {
    $request = [System.Net.FtpWebRequest]::Create("$baseUrl$path")
    $request.Method = $method
    $request.Credentials = $credential
    $request.EnableSsl = $true
    $request.UseBinary = $true
    $request.KeepAlive = $false
    return $request
}

function Send-FtpFile([string]$localPath, [string]$remotePath) {
    $request = New-FtpRequest $remotePath ([System.Net.WebRequestMethods+Ftp]::UploadFile)
    $content = [IO.File]::ReadAllBytes($localPath)
    $request.ContentLength = $content.Length
    $stream = $request.GetRequestStream()
    try { $stream.Write($content, 0, $content.Length) } finally { $stream.Close() }
    $request.GetResponse().Close()
}

function New-FtpDirectory([string]$remotePath) {
    try { (New-FtpRequest $remotePath ([System.Net.WebRequestMethods+Ftp]::MakeDirectory)).GetResponse().Close() }
    catch { } # klasör zaten varsa hata verir
}

function Remove-FtpFile([string]$remotePath) {
    try { (New-FtpRequest $remotePath ([System.Net.WebRequestMethods+Ftp]::DeleteFile)).GetResponse().Close() }
    catch { }
}

# 3) Klasör yükleme: önce alt klasörler, sonra dosyalar; -LastFiles en sona kalır
function Publish-FtpFolder([string]$localDir, [string]$remoteDir, [string[]]$LastFiles = @()) {
    New-FtpDirectory $remoteDir
    $dirs = Get-ChildItem $localDir -Recurse -Directory | Sort-Object { $_.FullName.Length }
    foreach ($d in $dirs) {
        New-FtpDirectory ($remoteDir + '/' + $d.FullName.Substring($localDir.Length + 1).Replace('\', '/'))
    }

    $files = @(Get-ChildItem $localDir -Recurse -File | Sort-Object { $LastFiles -contains $_.Name })
    $i = 0
    foreach ($f in $files) {
        $i++
        $relative = $f.FullName.Substring($localDir.Length + 1).Replace('\', '/')
        Write-Progress -Activity "Yükleniyor ($remoteDir)" -Status $relative -PercentComplete ([int](100 * $i / $files.Count))
        $attempt = 0
        while ($true) {
            try { Send-FtpFile $f.FullName "$remoteDir/$relative"; break }
            catch {
                $attempt++
                if ($attempt -ge 3) { throw "Yüklenemedi: $relative — $($_.Exception.Message)" }
                Start-Sleep -Seconds 3
            }
        }
    }
    Write-Progress -Activity "Yükleniyor ($remoteDir)" -Completed
    return $files.Count
}

# 4) API: bakım modu -> dosyalar -> bakım modundan çık
New-FtpDirectory $apiDir
$offline = Join-Path ([IO.Path]::GetTempPath()) 'app_offline.htm'
[IO.File]::WriteAllText($offline, '<!doctype html><meta charset="utf-8"><title>Güncelleniyor</title><p style="font-family:sans-serif;text-align:center;margin-top:20vh">Hafız Takip güncelleniyor, lütfen birkaç dakika sonra tekrar deneyin.</p>', (New-Object Text.UTF8Encoding $false))
Send-FtpFile $offline "$apiDir/app_offline.htm"
Write-Host "API bakım modunda; uygulamanın durması bekleniyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

$apiCount = Publish-FtpFolder $appDir $apiDir
Remove-FtpFile "$apiDir/app_offline.htm"
Write-Host "API yüklendi: $apiCount dosya -> $apiDir ($($cfg['HAFIZ_API_URL']))" -ForegroundColor Green

# 5) Web: index.html ve web.config en son (eski sayfa, yeni dosyalar yerine oturana kadar çalışmaya devam eder)
$webCount = Publish-FtpFolder $webPackageDir $webDir -LastFiles @('index.html', 'web.config')
Write-Host "Web yüklendi: $webCount dosya -> $webDir ($($cfg['HAFIZ_WEB_URL']))" -ForegroundColor Green
