param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$apiProject = Join-Path $PSScriptRoot "..\src\LocalPrintService.Api\LocalPrintService.Api.csproj"
$outputDir = Join-Path $PSScriptRoot "..\src\LocalPrintService.Api\bin\$Configuration\net8.0\win-x64\publish"

Write-Host "=== Local Print Service - Publish ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Publishing for Windows x64 (single-file, self-contained)..." -ForegroundColor Yellow
& dotnet publish $apiProject -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $outputDir
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