<#
.SYNOPSIS
    Hafız Takip veritabanı kurulum ve migration aracı (PostgreSQL, SQL script-first).

.DESCRIPTION
    -Setup    Veritabanını oluşturur ve sıfırdan kurar: 00_full_schema.sql, 02_quran_reference.sql,
              ardından 01_seed_data.sql (geliştirme verisi; -NoSeed ile atlanır).
              Veritabanı zaten varsa -Force verilmedikçe hiçbir şey yapmaz.
    -Migrate  Mevcut veritabanını günceller: önce 02_quran_reference.sql (referans verisi,
              tekrar çalıştırılabilir), sonra schema_migrations tablosunda kayıtlı olmayan
              migrations/*.sql dosyalarını numara sırasıyla, her birini tek transaction içinde uygular.
    -Status   Uygulanmış ve bekleyen migration'ları listeler.

    Güvenlik: yalnızca adı "hafiz" ile başlayan veritabanlarında çalışır.
    Parola için PGPASSWORD ortam değişkeni kullanılır; tanımlı değilse psql sorar.

.EXAMPLE
    .\database\db.ps1 -Setup -Database HafizTakipDB
.EXAMPLE
    .\database\db.ps1 -Setup -Database HafizTakipDB -Force
.EXAMPLE
    .\database\db.ps1 -Migrate -Database HafizTakipDB
.EXAMPLE
    .\database\db.ps1 -Status -Database HafizTakipDB
#>
[CmdletBinding()]
param(
    [switch]$Setup,
    [switch]$Migrate,
    [switch]$Status,
    [Parameter(Mandatory = $true)][string]$Database,
    [string]$DbHost = 'localhost',
    [int]$Port = 5432,
    [string]$User = 'postgres',
    [switch]$Force,
    [switch]$NoSeed,
    [string]$PsqlPath
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (@($Setup, $Migrate, $Status | Where-Object { $_ }).Count -ne 1) {
    throw 'Tam olarak bir işlem seçin: -Setup, -Migrate veya -Status.'
}
if ($Database -notmatch '^(?i)hafiz') {
    throw "Güvenlik: bu araç yalnızca adı 'hafiz' ile başlayan veritabanlarında çalışır (verilen: '$Database')."
}

# ── psql ─────────────────────────────────────────────────────────
if (-not $PsqlPath) {
    $cmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($cmd) {
        $PsqlPath = $cmd.Source
    } else {
        $PsqlPath = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue |
            Sort-Object { [int]($_.Directory.Parent.Name -replace '\D', '') } -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $PsqlPath -or -not (Test-Path $PsqlPath)) {
    throw 'psql bulunamadı. PATH''e ekleyin veya -PsqlPath ile verin.'
}

# psql NOTICE mesajlarını stderr'e yazar; PowerShell 5.1 bunları hata sayabildiği için çağrı sırasında
# ErrorActionPreference gevşetilir. Gerçek hatalar ON_ERROR_STOP + çıkış kodu ile yakalanır.
function Invoke-Psql([string]$Db, [string[]]$Arguments) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { & $PsqlPath -h $DbHost -p $Port -U $User -d $Db -v ON_ERROR_STOP=1 -q @Arguments }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "psql hata verdi (çıkış kodu $LASTEXITCODE)." }
}

# Tırnaklı tanımlayıcı içeren SQL'i (ör. "HafizTakipDB") stdin üzerinden gönderir:
# Windows PowerShell 5.1 native komut argümanlarındaki çift tırnakları siler.
function Invoke-SqlText([string]$Db, [string]$Sql) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $Sql | & $PsqlPath -h $DbHost -p $Port -U $User -d $Db -v ON_ERROR_STOP=1 -q -f - }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "psql hata verdi (çıkış kodu $LASTEXITCODE)." }
}

function Get-Scalar([string]$Db, [string]$Sql) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $value = & $PsqlPath -h $DbHost -p $Port -U $User -d $Db -v ON_ERROR_STOP=1 -At -c $Sql }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "psql hata verdi (çıkış kodu $LASTEXITCODE)." }
    return $value
}

function Test-DatabaseExists {
    return (Get-Scalar 'postgres' "SELECT 1 FROM pg_database WHERE datname = '$Database'") -eq '1'
}

function Invoke-SqlFile([string]$Path, [switch]$SingleTransaction) {
    Write-Host "  -> $([IO.Path]::GetFileName($Path))"
    $arguments = @('-f', $Path)
    if ($SingleTransaction) { $arguments = @('-1') + $arguments }
    Invoke-Psql $Database $arguments
}

function Get-MigrationFiles {
    return Get-ChildItem (Join-Path $root 'migrations') -Filter '*.sql' | Sort-Object Name
}

function Get-AppliedVersions {
    Invoke-Psql $Database @('-c', 'CREATE TABLE IF NOT EXISTS schema_migrations (version VARCHAR(100) PRIMARY KEY, applied_at TIMESTAMPTZ NOT NULL DEFAULT now());')
    return @(Get-Scalar $Database 'SELECT version FROM schema_migrations ORDER BY version')
}

Write-Host "Veritabanı: $Database ($User@${DbHost}:$Port)" -ForegroundColor Cyan

# ── Setup ────────────────────────────────────────────────────────
if ($Setup) {
    if (Test-DatabaseExists) {
        if (-not $Force) {
            throw "'$Database' zaten var. Silip yeniden kurmak için -Force kullanın; güncellemek için -Migrate."
        }
        Write-Host "  '$Database' siliniyor (-Force)..." -ForegroundColor Yellow
        Invoke-SqlText 'postgres' "DROP DATABASE `"$Database`" WITH (FORCE);"
    }

    Invoke-SqlText 'postgres' "CREATE DATABASE `"$Database`" ENCODING 'UTF8';"
    Invoke-SqlFile (Join-Path $root '00_full_schema.sql') -SingleTransaction
    Invoke-SqlFile (Join-Path $root '02_quran_reference.sql') -SingleTransaction
    if (-not $NoSeed) {
        Invoke-SqlFile (Join-Path $root '01_seed_data.sql') -SingleTransaction
    }
    Write-Host 'Kurulum tamamlandı.' -ForegroundColor Green
}

# ── Migrate ──────────────────────────────────────────────────────
if ($Migrate) {
    if (-not (Test-DatabaseExists)) { throw "'$Database' bulunamadı. Önce -Setup ile kurun." }

    $applied = Get-AppliedVersions
    Invoke-SqlFile (Join-Path $root '02_quran_reference.sql') -SingleTransaction

    $pending = Get-MigrationFiles | Where-Object { $applied -notcontains $_.BaseName }
    if (-not $pending) {
        Write-Host 'Bekleyen migration yok.' -ForegroundColor Green
    }
    foreach ($file in $pending) {
        Invoke-SqlFile $file.FullName -SingleTransaction
        Invoke-Psql $Database @('-c', "INSERT INTO schema_migrations (version) VALUES ('$($file.BaseName)') ON CONFLICT DO NOTHING;")
    }
    if ($pending) { Write-Host "$(@($pending).Count) migration uygulandı." -ForegroundColor Green }
}

# ── Status ───────────────────────────────────────────────────────
if ($Status) {
    if (-not (Test-DatabaseExists)) { throw "'$Database' bulunamadı." }
    $applied = Get-AppliedVersions
    foreach ($file in Get-MigrationFiles) {
        $state = if ($applied -contains $file.BaseName) { 'uygulandı' } else { 'BEKLİYOR' }
        Write-Host ('  {0,-40} {1}' -f $file.BaseName, $state)
    }
}
