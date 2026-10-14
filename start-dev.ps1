# ============================================================
# Hafiz Takip - Backend & Frontend Baslatma Scripti
# ============================================================
# Bu script, backend (ASP.NET Core) ve frontend (Vite + React)
# sunucularini ayni anda baslatir.
#
# Kullanim: PowerShell'de proje kok dizininden calistirin:
#   .\start-dev.ps1
#
# Durdurmak icin: Her iki pencereyi de kapatin veya Ctrl+C
# ============================================================

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$backendPath = Join-Path $projectRoot "backend\src\HafizTakip.WebAPI"
$frontendPath = Join-Path $projectRoot "frontend\web"

Write-Host ""
Write-Host "  Hafiz Takip - Gelistirme Ortami" -ForegroundColor Green
Write-Host "  =====================================" -ForegroundColor Green
Write-Host ""

# -- 1. Frontend bagimlilik kontrol --
if (-not (Test-Path (Join-Path $frontendPath "node_modules"))) {
    Write-Host "  [PKG] Frontend bagimliliklari yukleniyor..." -ForegroundColor Yellow
    Push-Location $frontendPath
    npm install
    Pop-Location
    Write-Host "  [OK] Bagimliliklar yuklendi." -ForegroundColor Green
}

# -- 2. Backend'i yeni bir pencerede baslat --
Write-Host "  [API] Backend baslatiliyor (http://localhost:5200)..." -ForegroundColor Cyan
$backendProcess = Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$backendPath'; Write-Host '[API] Backend (ASP.NET Core) baslatiliyor...' -ForegroundColor Cyan; dotnet run --launch-profile http"
) -PassThru

# -- 3. Frontend'i yeni bir pencerede baslat --
Write-Host "  [WEB] Frontend baslatiliyor (http://localhost:7071)..." -ForegroundColor Cyan
Start-Sleep -Seconds 2
$frontendProcess = Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$frontendPath'; Write-Host '[WEB] Frontend (Vite + React) baslatiliyor...' -ForegroundColor Cyan; npm run dev"
) -PassThru

Write-Host ""
Write-Host "  [OK] Her iki sunucu da baslatildi!" -ForegroundColor Green
Write-Host ""
Write-Host "  Backend  : http://localhost:5200" -ForegroundColor White
Write-Host "  Swagger  : http://localhost:5200/swagger" -ForegroundColor White
Write-Host "  Frontend : http://localhost:7071" -ForegroundColor White
Write-Host ""
Write-Host "  Durdurmak icin acilan terminal pencerelerini kapatin." -ForegroundColor Yellow
Write-Host ""
