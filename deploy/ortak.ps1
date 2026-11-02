# Yayın betiklerinin ortak yardımcıları (dot-source ile yüklenir). Gizli değerleri ekrana yazmaz.

# Yayın betiklerinin kullandığı ayarlar. .env'de olmayanlar ortam değişkenlerinden alınır
# (bulut/CI ortamında .env yoktur; değerler ortamın gizli değişkenleri olarak verilir).
$script:DeploySettingKeys = @(
    'FTP_HOST', 'FTP_USER', 'FTP_PASS',
    'HAFIZ_FTP_API_DIR', 'HAFIZ_FTP_WEB_DIR', 'HAFIZ_API_URL', 'HAFIZ_WEB_URL', 'HAFIZ_FTP_REMOTE_DIR',
    'HAFIZ_DB_CONNECTION', 'HAFIZ_DB_NAME', 'HAFIZ_JWT_KEY', 'HAFIZ_PLATFORM_ADMIN_PASSWORD',
    'HAFIZ_NETGSM_USERCODE', 'HAFIZ_NETGSM_PASSWORD', 'HAFIZ_NETGSM_HEADER',
    'HAFIZ_SMTP_HOST', 'HAFIZ_SMTP_PORT', 'HAFIZ_SMTP_SSL', 'HAFIZ_SMTP_USER', 'HAFIZ_SMTP_PASS', 'HAFIZ_SMTP_FROM'
)

function Read-DotEnv([string]$Path) {
    $cfg = @{}
    if (Test-Path $Path) {
        Get-Content $Path -Encoding UTF8 | Where-Object { $_ -match '=' -and -not $_.TrimStart().StartsWith('#') } | ForEach-Object {
            $parts = $_.Split('=', 2)
            $cfg[$parts[0].Trim().TrimStart([char]0xFEFF)] = $parts[1].Trim()
        }
    }
    foreach ($key in $script:DeploySettingKeys) {
        $value = [Environment]::GetEnvironmentVariable($key)
        if ([string]::IsNullOrWhiteSpace($cfg[$key]) -and -not [string]::IsNullOrWhiteSpace($value)) { $cfg[$key] = $value.Trim() }
    }
    return $cfg
}

<#
  PostgreSQL bağlantı bilgisini çözer. İki biçim kabul edilir:
    postgresql://kullanici:sifre@host[:port]/veritabani?sslmode=require   (Neon/Supabase'in verdiği)
    Host=...;Port=...;Database=...;Username=...;Password=...              (Npgsql)
  -Database verilirse veritabanı adı onunla değiştirilir.
  Neon'un "-pooler" (PgBouncer) adresi doğrudan adrese çevrilir: şema kurulumu ve EF Core için daha uygun.
#>
function Get-PgConnectionInfo([string]$Value, [string]$Database = '') {
    $info = [ordered]@{ Host = ''; Port = 5432; Database = ''; Username = ''; Password = ''; SslMode = 'Prefer' }

    if ($Value -match '^postgres(ql)?://') {
        $uri = [Uri]$Value
        $userInfo = $uri.UserInfo.Split(':', 2)
        $info.Host = $uri.Host
        if ($uri.Port -gt 0) { $info.Port = $uri.Port }
        $info.Database = [Uri]::UnescapeDataString($uri.AbsolutePath.TrimStart('/'))
        $info.Username = [Uri]::UnescapeDataString($userInfo[0])
        if ($userInfo.Count -gt 1) { $info.Password = [Uri]::UnescapeDataString($userInfo[1]) }
        if ($uri.Query -match '(?i)sslmode=([a-z-]+)') { $info.SslMode = $Matches[1] }
    } else {
        foreach ($pair in $Value.Split(';')) {
            $kv = $pair.Split('=', 2)
            if ($kv.Count -ne 2) { continue }
            switch -Regex ($kv[0].Trim()) {
                '^(?i)(host|server)$' { $info.Host = $kv[1].Trim() }
                '^(?i)port$' { $info.Port = [int]$kv[1].Trim() }
                '^(?i)(database|db)$' { $info.Database = $kv[1].Trim() }
                '^(?i)(username|user id|user)$' { $info.Username = $kv[1].Trim() }
                '^(?i)password$' { $info.Password = $kv[1] }
                '^(?i)ssl ?mode$' { $info.SslMode = $kv[1].Trim() }
            }
        }
    }

    if ($Database) { $info.Database = $Database }
    $info.Host = $info.Host -replace '-pooler\.', '.'
    if ($info.Host -match '\.neon\.tech$' -and $info.SslMode -notmatch '(?i)require|verify') { $info.SslMode = 'Require' }
    if (-not $info.Host -or -not $info.Username) { throw 'Veritabanı bağlantı bilgisi çözülemedi (host/kullanıcı yok).' }
    return $info
}

function ConvertTo-NpgsqlConnectionString($Info) {
    $ssl = switch -Regex ($Info.SslMode) {
        '(?i)^verify-full$' { 'VerifyFull' }
        '(?i)^verify-ca$' { 'VerifyCA' }
        '(?i)^require$' { 'Require' }
        '(?i)^disable$' { 'Disable' }
        default { 'Prefer' }
    }
    return "Host=$($Info.Host);Port=$($Info.Port);Database=$($Info.Database);Username=$($Info.Username);Password=$($Info.Password);SSL Mode=$ssl;Timeout=30;Command Timeout=60"
}

<#
  .env'deki gönderim bilgilerini paket ayarına çevirir. Bir grubun hiçbiri yoksa $null (gönderim yok);
  bir kısmı varsa eksikleri söyleyen hata verir (yarım ayarla yayın yapılmasın).
#>
function Get-NetGsmSettings($cfg) {
    $keys = 'HAFIZ_NETGSM_USERCODE', 'HAFIZ_NETGSM_PASSWORD', 'HAFIZ_NETGSM_HEADER'
    $missing = @($keys | Where-Object { [string]::IsNullOrWhiteSpace($cfg[$_]) })
    if ($missing.Count -eq $keys.Count) { return $null }
    if ($missing.Count -gt 0) { throw "NetGSM ayarı eksik: $($missing -join ', ')" }
    # Denendi: NetGSM, şifresinde & veya < olan isteği (doğru kaçışlansa da) 73 koduyla reddediyor
    if ($cfg['HAFIZ_NETGSM_PASSWORD'] -match '[&<]') {
        throw "HAFIZ_NETGSM_PASSWORD içinde & veya < var; NetGSM bu karakterli şifreyi reddediyor. NetGSM panelinde API kullanıcısına bu karakterler olmadan yeni şifre verin."
    }
    return [ordered]@{ UserCode = $cfg['HAFIZ_NETGSM_USERCODE']; Password = $cfg['HAFIZ_NETGSM_PASSWORD']; Header = $cfg['HAFIZ_NETGSM_HEADER'] }
}

function Get-SmtpSettings($cfg) {
    $keys = 'HAFIZ_SMTP_HOST', 'HAFIZ_SMTP_USER', 'HAFIZ_SMTP_PASS', 'HAFIZ_SMTP_FROM'
    $missing = @($keys | Where-Object { [string]::IsNullOrWhiteSpace($cfg[$_]) })
    if ($missing.Count -eq $keys.Count) { return $null }
    if ($missing.Count -gt 0) { throw "SMTP ayarı eksik: $($missing -join ', ')" }
    return [ordered]@{
        Host        = $cfg['HAFIZ_SMTP_HOST']
        Port        = if ($cfg['HAFIZ_SMTP_PORT']) { [int]$cfg['HAFIZ_SMTP_PORT'] } else { 587 }
        UseSsl      = $cfg['HAFIZ_SMTP_SSL'] -ne 'false'
        UserName    = $cfg['HAFIZ_SMTP_USER']
        Password    = $cfg['HAFIZ_SMTP_PASS']
        FromAddress = $cfg['HAFIZ_SMTP_FROM']
        FromName    = 'Hafız Takip'
    }
}
