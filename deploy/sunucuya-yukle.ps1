<#
.SYNOPSIS
  Hafız Takip'i Plesk (Windows/IIS) alt alan adına FTP ile yükler.

.DESCRIPTION
  Kök dizindeki .env dosyasından okunur (depoya girmez):
    FTP_HOST, FTP_USER, FTP_PASS   -> FTP bağlantısı (FTPS)
    HAFIZ_FTP_REMOTE_DIR           -> alt alan adının klasörü, ör. /httpdocs/hafiz
                                      (/httpdocs kökü ana siteye aittir; oraya yükleme reddedilir)
    HAFIZ_DB_CONNECTION            -> PostgreSQL bağlantı cümlesi
    HAFIZ_JWT_KEY                  -> oturum imzalama anahtarı; yoksa üretilip .env'e eklenir
                                      (her yüklemede aynı kalmalı, yoksa açık oturumlar düşer)

  Akış: paket hazırlanır (deploy\paket-olustur.ps1) -> app_offline.htm yüklenir (IIS uygulamayı durdurur,
  kilitli DLL'ler serbest kalır) -> dosyalar yüklenir -> app_offline.htm silinir (uygulama yeniden başlar).

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
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
if (-not (Test-Path $envFile)) { throw ".env bulunamadı: $envFile" }

$cfg = @{}
Get-Content $envFile -Encoding UTF8 | Where-Object { $_ -match '=' -and -not $_.TrimStart().StartsWith('#') } | ForEach-Object {
    $parts = $_.Split('=', 2)
    $cfg[$parts[0].Trim()] = $parts[1].Trim()
}

foreach ($key in 'FTP_HOST', 'FTP_USER', 'FTP_PASS', 'HAFIZ_FTP_REMOTE_DIR', 'HAFIZ_DB_CONNECTION') {
    if ([string]::IsNullOrWhiteSpace($cfg[$key])) { throw ".env içinde $key eksik." }
}

# Hedef yalnızca /httpdocs altındaki bir alt klasör olabilir (ana siteyi ezmemek için)
$remoteDir = '/' + $cfg['HAFIZ_FTP_REMOTE_DIR'].Trim().Trim('/')
if ($remoteDir -notmatch '^/httpdocs/[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)*$') {
    throw "HAFIZ_FTP_REMOTE_DIR '/httpdocs/<alt-klasör>' biçiminde olmalı (şu an: $remoteDir). Ana site köküne yükleme yapılmaz."
}

# Oturum anahtarı sabit kalsın: yoksa üret ve .env'e ekle
if ([string]::IsNullOrWhiteSpace($cfg['HAFIZ_JWT_KEY'])) {
    $bytes = New-Object byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $cfg['HAFIZ_JWT_KEY'] = [Convert]::ToBase64String($bytes)
    Add-Content -Path $envFile -Value "`r`n# Hafız Takip oturum anahtarı (sunucuya her yüklemede aynı kalmalı)`r`nHAFIZ_JWT_KEY=$($cfg['HAFIZ_JWT_KEY'])" -Encoding UTF8
    Write-Host "HAFIZ_JWT_KEY üretildi ve .env'e eklendi." -ForegroundColor Yellow
}

# 1) Paket: Windows IIS için kendi çalışma zamanıyla (sunucuda .NET 8 kurulu olmasa da çalışır)
& (Join-Path $PSScriptRoot 'paket-olustur.ps1') -Ortam $Ortam -Runtime 'win-x64' `
    -ConnectionString $cfg['HAFIZ_DB_CONNECTION'] -JwtKey $cfg['HAFIZ_JWT_KEY']
$appDir = Join-Path $PSScriptRoot 'out\app'
if (-not (Test-Path (Join-Path $appDir 'web.config'))) { throw 'Paket hazırlanamadı (web.config yok).' }

if ($SkipUpload) {
    Write-Host "Yükleme atlandı (-SkipUpload). Paket: $appDir" -ForegroundColor Magenta
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

# 3) Bakım modu -> dosyalar -> bakım modundan çık
New-FtpDirectory $remoteDir
$offline = Join-Path $env:TEMP 'app_offline.htm'
[IO.File]::WriteAllText($offline, '<!doctype html><meta charset="utf-8"><title>Güncelleniyor</title><p style="font-family:sans-serif;text-align:center;margin-top:20vh">Hafız Takip güncelleniyor, lütfen birkaç dakika sonra tekrar deneyin.</p>', (New-Object Text.UTF8Encoding $false))
Send-FtpFile $offline "$remoteDir/app_offline.htm"
Write-Host "Bakım modu açıldı; uygulamanın durması bekleniyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

$files = Get-ChildItem $appDir -Recurse -File
$dirs = Get-ChildItem $appDir -Recurse -Directory | Sort-Object { $_.FullName.Length }
foreach ($d in $dirs) {
    New-FtpDirectory ($remoteDir + '/' + $d.FullName.Substring($appDir.Length + 1).Replace('\', '/'))
}

$i = 0
foreach ($f in $files) {
    $i++
    $relative = $f.FullName.Substring($appDir.Length + 1).Replace('\', '/')
    Write-Progress -Activity 'Yükleniyor' -Status $relative -PercentComplete ([int](100 * $i / $files.Count))
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
Write-Progress -Activity 'Yükleniyor' -Completed

Remove-FtpFile "$remoteDir/app_offline.htm"
Write-Host "Yükleme tamamlandı: $($files.Count) dosya -> $remoteDir" -ForegroundColor Green
