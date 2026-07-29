param(
    [string]$Domain = "print.local",
    [string]$Ip = "127.0.0.1",
    [int]$HttpsPort = 5201,
    [int]$HttpPort = 5200
)

$ErrorActionPreference = "Stop"

Write-Host "=== Local Print Service - Domain & SSL Setup ===" -ForegroundColor Cyan
Write-Host ""

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    exit 1
}

# 1. Add to hosts file
Write-Host "[1/4] Configuring hosts file..." -ForegroundColor Yellow
$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
$hostsContent = Get-Content $hostsPath -Raw
if ($hostsContent -notmatch [regex]::Escape($Domain)) {
    Add-Content -Path $hostsPath -Value "`n$Ip`t$Domain"
    Write-Host "  Added $Domain -> $Ip" -ForegroundColor Green
} else {
    Write-Host "  $Domain already in hosts file" -ForegroundColor Green
}

# 2. Create SSL certificate
Write-Host "[2/4] Creating SSL certificate..." -ForegroundColor Yellow
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.DnsNameList -contains $Domain } | Select-Object -First 1
if (-not $cert) {
    $cert = New-SelfSignedCertificate -DnsName $Domain, "localhost" -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(10) -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -FriendlyName "Local Print Service - $Domain" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")
    Write-Host "  Certificate created: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "  Certificate already exists: $($cert.Thumbprint)" -ForegroundColor Green
}

# 3. Install in Trusted Root
Write-Host "[3/4] Installing certificate in Trusted Root..." -ForegroundColor Yellow
$rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
$rootStore.Open("ReadWrite")
$exists = $rootStore.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
if (-not $exists) {
    $rootStore.Add($cert)
    Write-Host "  Certificate installed in Trusted Root" -ForegroundColor Green
} else {
    Write-Host "  Certificate already in Trusted Root" -ForegroundColor Green
}
$rootStore.Close()

# 4. Export PFX
Write-Host "[4/4] Exporting certificate..." -ForegroundColor Yellow
$certDir = Join-Path $PSScriptRoot "..\certs"
if (-not (Test-Path $certDir)) { New-Item -ItemType Directory -Path $certDir -Force | Out-Null }
$pfxPath = Join-Path $certDir "print-local.pfx"
$password = ConvertTo-SecureString -String "LocalPrint2024!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password -Force
Write-Host "  Exported to: $pfxPath" -ForegroundColor Green

# 5. Verify
Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Domain:  https://$Domain"
Write-Host "Local:   https://localhost:$HttpsPort"
Write-Host "HTTP:    http://localhost:$HttpPort"
Write-Host ""
Write-Host "Certificate Thumbprint: $($cert.Thumbprint)"
Write-Host ""
Write-Host "You can now run the service with:" -ForegroundColor Yellow
Write-Host "  dotnet run --project src\LocalPrintService.Api"
