<#
.SYNOPSIS
  NetGSM OTP SMS hesabını sunucuya yüklemeden dener: .env'deki bilgilerle verilen numaraya deneme SMS'i gönderir.

.DESCRIPTION
  .env'de gerekenler: HAFIZ_NETGSM_USERCODE, HAFIZ_NETGSM_PASSWORD, HAFIZ_NETGSM_HEADER
  Uygulamadaki gönderimle aynı servisi (POST https://api.netgsm.com.tr/sms/send/otp) ve aynı biçimi kullanır.
  Hata kodları anlamıyla yazılır. NetGSM panelinde API için IP kısıtlaması açtıysanız, betiği çalıştırdığınız
  bilgisayarın IP adresi de izinli olmalıdır (sunucu için ayrıca sunucunun IP adresi).

.EXAMPLE
  pwsh ./deploy/sms-dene.ps1 -Telefon 05321112233
#>
param(
    [Parameter(Mandatory = $true)][string]$Telefon
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
$cfg = Read-DotEnv (Join-Path (Split-Path $PSScriptRoot -Parent) '.env')
$netgsm = Get-NetGsmSettings $cfg
if (-not $netgsm) { throw '.env dosyasında HAFIZ_NETGSM_USERCODE, HAFIZ_NETGSM_PASSWORD ve HAFIZ_NETGSM_HEADER tanımlı değil.' }

# Uygulamadaki PhoneNumberHelper.Normalize ile aynı: 5XXXXXXXXX
$digits = ($Telefon -replace '\D', '')
if ($digits.Length -eq 12 -and $digits.StartsWith('90')) { $digits = $digits.Substring(2) }
elseif ($digits.Length -eq 11 -and $digits.StartsWith('0')) { $digits = $digits.Substring(1) }
if ($digits -notmatch '^5\d{9}$') { throw "Geçerli bir cep telefonu girin (ör. 05321112233). Girilen: $Telefon" }

# OTP kanalı Türkçe karakter desteklemez
$message = 'Hafiz Takip deneme mesaji. Kod: 123456. Bu SMS geldiyse NetGSM ayarlari dogru.'
$xml = [System.Xml.Linq.XDocument]::new(
    [System.Xml.Linq.XElement]::new('mainbody',
        [System.Xml.Linq.XElement]::new('header',
            [System.Xml.Linq.XElement]::new('usercode', $netgsm.UserCode),
            [System.Xml.Linq.XElement]::new('password', $netgsm.Password),
            [System.Xml.Linq.XElement]::new('msgheader', $netgsm.Header)),
        [System.Xml.Linq.XElement]::new('body',
            [System.Xml.Linq.XElement]::new('msg', [System.Xml.Linq.XCData]::new($message)),
            [System.Xml.Linq.XElement]::new('no', $digits))))
$body = '<?xml version="1.0" encoding="UTF-8"?>' + $xml.ToString([System.Xml.Linq.SaveOptions]::DisableFormatting)

$meanings = @{
    '20'  = 'Mesaj metni veya boyu uygun değil.'
    '30'  = "Kullanıcı adı/şifre yanlış, API erişim izni kapalı ya da bu bilgisayarın IP adresi NetGSM'de izinli değil (panel: Ayarlar > API işlemleri)."
    '40'  = 'Gönderici adı (başlık) onaylı değil veya yanlış yazılmış.'
    '41'  = 'Gönderici adı (başlık) onaylı değil veya yanlış yazılmış.'
    '50'  = 'Telefon numarası geçersiz.'
    '60'  = 'Hesapta OTP SMS paketi tanımlı değil veya paket bitti.'
    '70'  = 'İstek parametreleri hatalı.'
    '73'  = 'İstek parametreleri reddedildi (şifrede & veya < olmamalı).'
    '80'  = 'Gönderim sınırı aşıldı (dakikada en çok 100).'
    '100' = 'NetGSM sistem hatası; biraz sonra tekrar deneyin.'
}

Write-Host "NetGSM'e gönderiliyor: başlık '$($netgsm.Header)', numara 0$($digits.Substring(0,3)) *** ** $($digits.Substring(8))" -ForegroundColor Cyan
$response = Invoke-WebRequest -Uri 'https://api.netgsm.com.tr/sms/send/otp' -Method Post `
    -ContentType 'text/xml; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body)) -SkipHttpErrorCheck
$text = [string]$response.Content
try {
    $main = ([xml]$text).SelectSingleNode('//main')
    $code = [string]$main.code
    $jobId = [string]$main.jobID
    $netgsmError = [string]$main.error
} catch {
    throw "NetGSM yanıtı okunamadı (HTTP $($response.StatusCode)): $($text.Substring(0, [Math]::Min(200, $text.Length)))"
}

if ($code -eq '0' -or $code -eq '00' -or (-not $code -and $jobId)) {
    Write-Host "Başarılı: NetGSM gönderimi kabul etti (iş no $jobId). SMS 3 dakika içinde gelmelidir." -ForegroundColor Green
} else {
    $meaning = if ($meanings.ContainsKey($code)) { $meanings[$code] } else { 'Bilinmeyen yanıt.' }
    Write-Host "Başarısız: NetGSM kodu $code — $meaning" -ForegroundColor Red
    if ($netgsmError) { Write-Host "NetGSM'in mesajı: $netgsmError" -ForegroundColor DarkYellow }
    exit 1
}
