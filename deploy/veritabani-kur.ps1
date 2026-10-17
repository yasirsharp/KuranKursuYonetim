<#
.SYNOPSIS
  Uzak PostgreSQL'de (ör. Neon) Hafız Takip veritabanını kurar veya günceller.

.DESCRIPTION
  Bağlantı .env'deki HAFIZ_DB_CONNECTION'dan okunur (URI veya Npgsql biçimi). Uygulamanın veritabanı
  HAFIZ_DB_NAME (varsayılan: hafiztakip) adıyla ayrı açılır; bağlantıdaki varsayılan veritabanına
  (ör. neondb) dokunulmaz. İşi database\db.ps1 yapar (adı 'hafiz' ile başlamayan veritabanını reddeder).

.EXAMPLE
  .\deploy\veritabani-kur.ps1 -Setup           # ilk kurulum: şema + Kur'an verisi + test verisi
  .\deploy\veritabani-kur.ps1 -Setup -NoSeed   # test verisi olmadan
  .\deploy\veritabani-kur.ps1 -Migrate         # sonraki sürümler: yalnızca yeni migration'lar
  .\deploy\veritabani-kur.ps1 -Status
#>
param(
    [switch]$Setup,
    [switch]$Migrate,
    [switch]$Status,
    [switch]$NoSeed,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
$root = Split-Path $PSScriptRoot -Parent
$cfg = Read-DotEnv (Join-Path $root '.env')
if ([string]::IsNullOrWhiteSpace($cfg['HAFIZ_DB_CONNECTION'])) { throw ".env içinde HAFIZ_DB_CONNECTION eksik." }

$dbName = if ($cfg['HAFIZ_DB_NAME']) { $cfg['HAFIZ_DB_NAME'] } else { 'hafiztakip' }
$info = Get-PgConnectionInfo $cfg['HAFIZ_DB_CONNECTION'] -Database $dbName

$env:PGPASSWORD = $info.Password
$env:PGSSLMODE = $info.SslMode.ToLowerInvariant()
try {
    $dbArgs = @{ Database = $info.Database; DbHost = $info.Host; Port = $info.Port; User = $info.Username }
    if ($Setup) { $dbArgs.Setup = $true }
    if ($Migrate) { $dbArgs.Migrate = $true }
    if ($Status) { $dbArgs.Status = $true }
    if ($NoSeed) { $dbArgs.NoSeed = $true }
    if ($Force) { $dbArgs.Force = $true }
    & (Join-Path $root 'database/db.ps1') @dbArgs
} finally {
    Remove-Item Env:\PGPASSWORD, Env:\PGSSLMODE -ErrorAction SilentlyContinue
}
