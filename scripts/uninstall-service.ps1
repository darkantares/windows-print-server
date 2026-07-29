param(
    [string]$ServiceName = "LocalPrintService"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Local Print Service - Uninstallation ===" -ForegroundColor Cyan
Write-Host ""

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping service '$ServiceName'..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2

    Write-Host "Removing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Service removed." -ForegroundColor Green
} else {
    Write-Host "Service '$ServiceName' not found." -ForegroundColor Yellow
}

$ruleNames = @("LocalPrintService-HTTP", "LocalPrintService-HTTPS")
foreach ($ruleName in $ruleNames) {
    $rule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if ($rule) {
        Write-Host "Removing firewall rule '$ruleName'..." -ForegroundColor Yellow
        Remove-NetFirewallRule -DisplayName $ruleName
        Write-Host "Firewall rule removed." -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Uninstallation Complete ===" -ForegroundColor Green
