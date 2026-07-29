param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$sln = Join-Path $PSScriptRoot "..\LocalPrintService.sln"
$outputDir = Join-Path $PSScriptRoot "..\src\LocalPrintService.Api\bin\$Configuration\net8.0\win-x64\publish"

Write-Host "=== Local Print Service - Publish ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Building solution..." -ForegroundColor Yellow
& dotnet build $sln -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing for Windows x64..." -ForegroundColor Yellow
& dotnet publish $sln -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o $outputDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Publish Complete ===" -ForegroundColor Green
Write-Host "Output: $outputDir"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Install service: .\scripts\install-service.ps1"
Write-Host "  2. Create installer: Open installer\LocalPrintService.iss with Inno Setup"
