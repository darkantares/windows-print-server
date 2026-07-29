param(
    [string]$ServiceName = "LocalPrintService",
    [string]$DisplayName = "Local Print Service",
    [string]$Description = "Universal Print Service for Windows - Local Print Server",
    [int]$HttpPort = 5200,
    [int]$HttpsPort = 5201
)

$ErrorActionPreference = "Stop"

$publishDir = Join-Path $PSScriptRoot "..\src\LocalPrintService.Api\bin\Release\net8.0\win-x64\publish"
$exePath = Join-Path $publishDir "LocalPrintService.Api.exe"

Write-Host "=== Local Print Service - Installation ===" -ForegroundColor Cyan
Write-Host ""

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $exePath)) {
    Write-Host "Publish not found. Building..." -ForegroundColor Yellow
    $sln = Join-Path $PSScriptRoot "..\LocalPrintService.sln"
    & dotnet publish $sln -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o (Split-Path $publishDir)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Service '$ServiceName' already exists. Stopping..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Installing service '$DisplayName'..." -ForegroundColor Green
New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic `
    -ServiceType LocalSystem

Write-Host "Configuring firewall rules..." -ForegroundColor Green

$rules = @(
    @{ Name = "LocalPrintService-HTTP"; Port = $HttpPort; Description = "HTTP" },
    @{ Name = "LocalPrintService-HTTPS"; Port = $HttpsPort; Description = "HTTPS" }
)

foreach ($rule in $rules) {
    $existing = Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue
    if ($existing) { Remove-NetFirewallRule -DisplayName $rule.Name }
    New-NetFirewallRule -DisplayName $rule.Name `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $rule.Port `
        -Action Allow `
        -Profile Private `
        -Description "Allow $($rule.Description) traffic for Local Print Service on port $($rule.Port)"
}

Write-Host "Starting service..." -ForegroundColor Green
Start-Service -Name $ServiceName

Start-Sleep -Seconds 3
$service = Get-Service -Name $ServiceName
if ($service.Status -eq "Running") {
    Write-Host ""
    Write-Host "=== Installation Complete ===" -ForegroundColor Green
    Write-Host "Service:    $DisplayName"
    Write-Host "Status:     Running"
    Write-Host "HTTP:       http://localhost:$HttpPort"
    Write-Host "HTTPS:      https://localhost:$HttpsPort"
    Write-Host "Domain:     https://print.local:$HttpsPort"
    Write-Host "Swagger:    https://localhost:$HttpsPort/swagger"
    Write-Host "Health:     https://localhost:$HttpsPort/api/health"
} else {
    Write-Host "Service failed to start. Check Windows Event Log." -ForegroundColor Red
    exit 1
}
