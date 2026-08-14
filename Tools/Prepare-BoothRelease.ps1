<#
.SYNOPSIS
    Builds the customer-facing BOOTH ZIP from a validated Stargazing Hill unitypackage.

.DESCRIPTION
    This script does not create the unitypackage and never gathers Unity dependencies. Export the package
    through Stargazing Hill/Build & Export/Redistributable UnityPackage... first, then pass that file here.
    The result contains the unitypackage, five-language package README, license, notice, and SHA-256 list.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools/Prepare-BoothRelease.ps1 -UnityPackage Build/StargazingHill-redistributable.unitypackage -Version 1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPackage,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot 'Build/BOOTH'
}

$resolvedPackage = (Resolve-Path -LiteralPath $UnityPackage).Path
& python (Join-Path $PSScriptRoot 'Validate-UnityPackage.py') $resolvedPackage
if ($LASTEXITCODE -ne 0) { throw 'UnityPackage validation failed.' }

$productName = "StargazingHill-$Version"
$stagingDirectory = Join-Path $OutputDirectory $productName
$zipPath = Join-Path $OutputDirectory "$productName-BOOTH.zip"
if (Test-Path -LiteralPath $stagingDirectory) {
    throw "Staging directory already exists: $stagingDirectory"
}
if (Test-Path -LiteralPath $zipPath) {
    throw "Output ZIP already exists: $zipPath"
}

New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
$packageName = "$productName.unitypackage"
$packagedUnityPackage = Join-Path $stagingDirectory $packageName
Copy-Item -LiteralPath $resolvedPackage -Destination $packagedUnityPackage
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/StargazingHill/README_UNITYPACKAGE.md') -Destination (Join-Path $stagingDirectory 'README.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $stagingDirectory 'LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $projectRoot 'NOTICE.md') -Destination (Join-Path $stagingDirectory 'NOTICE.md')

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagedUnityPackage).Hash.ToLowerInvariant()
Set-Content -Encoding ascii -LiteralPath (Join-Path $stagingDirectory 'SHA256SUMS.txt') -Value "$hash  $packageName"

Compress-Archive -CompressionLevel Optimal -Path (Join-Path $stagingDirectory '*') -DestinationPath $zipPath
Write-Host "BOOTH ZIP: $zipPath" -ForegroundColor Green
Write-Host "SHA-256:  $hash" -ForegroundColor Green
