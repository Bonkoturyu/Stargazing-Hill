<#
.SYNOPSIS
    Applies the verified local compatibility patch for the installed YamaPlayer version.

.DESCRIPTION
    YamaPlayer is restored by VCC and is intentionally not tracked by this repository. A VCC
    restore or update therefore replaces local package edits. This script selects a patch only
    when package.json has an explicitly supported version and refuses to guess when upstream
    source no longer matches.

.PARAMETER Mode
    Apply (default): apply the selected patch, or succeed if already applied.
    Check: require the selected patch to already be applied without changing files.
    Restore: reverse the selected patch, or succeed if already restored.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1
    powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1 -Mode Check
    powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1 -Mode Restore
#>
[CmdletBinding()]
param(
    [ValidateSet('Apply', 'Check', 'Restore')]
    [string]$Mode = 'Apply'
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$packageJsonPath = Join-Path $projectPath 'Packages/net.kwxxw.yama-stream/package.json'

if (-not (Test-Path -LiteralPath $packageJsonPath)) {
    throw 'YamaPlayer is not restored. Resolve net.kwxxw.yama-stream with VCC first.'
}

$package = Get-Content -Raw -Encoding UTF8 -LiteralPath $packageJsonPath | ConvertFrom-Json
$patches = @{
    '2.0.0-beta.7' = 'Tools/YamaPlayerPatches/2.0.0-beta.7-disable-editor-auto-update.patch'
}

$patchRelativePath = $patches[$package.version]
if (-not $patchRelativePath) {
    throw "YamaPlayer $($package.version) has no verified Stargazing Hill patch. Check upstream before adding one."
}

$patchPath = Join-Path $projectPath $patchRelativePath
if (-not (Test-Path -LiteralPath $patchPath)) {
    throw "Patch file is missing: $patchRelativePath"
}

function Test-GitPatch([switch]$Reverse) {
    $arguments = @('apply')
    if ($Reverse) { $arguments += '--reverse' }
    $arguments += @('--check', '--', $patchPath)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & git @arguments 2>$null
    $succeeded = $LASTEXITCODE -eq 0
    $ErrorActionPreference = $previousErrorActionPreference
    return $succeeded
}

Push-Location $projectPath
try {
    $isApplied = Test-GitPatch -Reverse
    $canApply = Test-GitPatch

    if ($isApplied -and $canApply) {
        throw 'Patch state is ambiguous: both forward and reverse checks succeeded.'
    }

    if ($Mode -eq 'Check') {
        if (-not $isApplied) {
            if ($canApply) {
                throw "YamaPlayer $($package.version) patch is not applied. Run: powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1"
            }
            throw 'YamaPlayer source matches neither the verified original nor patched form.'
        }
        Write-Output "YamaPlayer $($package.version) patch is applied."
        exit 0
    }

    if ($Mode -eq 'Restore') {
        if (-not $isApplied) {
            if ($canApply) {
                Write-Output "YamaPlayer $($package.version) is already in its upstream form."
                exit 0
            }
            throw 'YamaPlayer source matches neither the verified original nor patched form.'
        }
        & git apply --reverse -- $patchPath
        if ($LASTEXITCODE -ne 0) { throw 'Failed to restore the YamaPlayer source.' }
        Write-Output "Restored YamaPlayer $($package.version) to its upstream form."
        exit 0
    }

    if ($isApplied) {
        Write-Output "YamaPlayer $($package.version) patch is already applied."
        exit 0
    }
    if (-not $canApply) {
        throw 'YamaPlayer source matches neither the verified original nor patched form.'
    }

    & git apply -- $patchPath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to apply the YamaPlayer patch.' }
    Write-Output "Applied the YamaPlayer $($package.version) editor update-check patch."
}
finally {
    Pop-Location
}
