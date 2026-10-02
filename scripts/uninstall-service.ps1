param(
    [string]$ServiceName = "LocalPrintService",
    [switch]$KeepEnvironment
)

$ErrorActionPreference = "Continue"

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
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    Write-Host "Removing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Service removed." -ForegroundColor Green
} else {
    Write-Host "Service '$ServiceName' not found." -ForegroundColor Yellow
}

$ruleNames = @("LocalPrintService-HTTP", "LocalPrintService-HTTPS", "localprintservice.api.exe")
foreach ($ruleName in $ruleNames) {
    $rules = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    foreach ($rule in $rules) {
        Write-Host "Removing firewall rule '$ruleName'..." -ForegroundColor Yellow
        Remove-NetFirewallRule -Id $rule.Id -ErrorAction SilentlyContinue
        Write-Host "Firewall rule removed." -ForegroundColor Green
    }
}

if (-not $KeepEnvironment) {
    $domain = "print.local"
    $hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
    $pattern = '^\s*\S+\s+' + [regex]::Escape($domain) + '\s*$'

    $lines = @(Get-Content -LiteralPath $hostsPath -ErrorAction SilentlyContinue)
    $matched = @($lines | Where-Object { $_ -match $pattern })

    if ($matched.Count -eq 0) {
        Write-Host "Hosts file: nothing to remove." -ForegroundColor Green
    } else {
        $kept = @($lines | Where-Object { $_ -notmatch $pattern })

        # Guardas: jamas escribir un hosts vacio o sin cambios
        if ($kept.Count -eq 0 -or $kept.Count -ge $lines.Count) {
            Write-Host "Hosts file: aborted (invalid result, file untouched)." -ForegroundColor Yellow
        } else {
            $backup = "$hostsPath.lps.bak"
            try {
                Copy-Item -LiteralPath $hostsPath -Destination $backup -Force -ErrorAction Stop

                # Escritura atomica: primero temporal, solo se sustituye si tiene contenido
                $tmp = "$hostsPath.lps.tmp"
                Set-Content -LiteralPath $tmp -Value $kept -Encoding Default -ErrorAction Stop
                if ((Get-Item -LiteralPath $tmp).Length -eq 0) {
                    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
                    throw "temporary file is empty"
                }

                Copy-Item -LiteralPath $tmp -Destination $hostsPath -Force -ErrorAction Stop
                Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
                Write-Host "Hosts entry '$domain' removed. Backup: $backup" -ForegroundColor Green
            } catch {
                Remove-Item -LiteralPath "$hostsPath.lps.tmp" -Force -ErrorAction SilentlyContinue
                Write-Host "Hosts file: left untouched ($($_.Exception.Message))." -ForegroundColor Yellow
            }
        }
    }

    $certs = @(
        Get-ChildItem -Path Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
            Where-Object { $_.FriendlyName -like "*Local Print Service*" -or ($_.DnsNameList -contains $domain) }
        Get-ChildItem -Path Cert:\CurrentUser\Root -ErrorAction SilentlyContinue |
            Where-Object { $_.FriendlyName -like "*Local Print Service*" -or ($_.DnsNameList -contains $domain) }
    )

    foreach ($cert in $certs) {
        $thumb = $cert.Thumbprint
        Remove-Item -Path "Cert:\CurrentUser\My\$thumb" -ErrorAction SilentlyContinue
        Remove-Item -Path "Cert:\CurrentUser\Root\$thumb" -ErrorAction SilentlyContinue
        Write-Host "Certificate removed: $thumb" -ForegroundColor Green
    }
    if ($certs.Count -eq 0) { Write-Host "Certificates: nothing to remove." -ForegroundColor Green }
}

Write-Host ""
Write-Host "=== Uninstallation Complete ===" -ForegroundColor Green
