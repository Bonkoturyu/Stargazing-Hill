<#
.SYNOPSIS
    Runs every check that guards this project, in the order that fails cheapest first.

.DESCRIPTION
    The GitHub Actions workflow only runs the Python validation. That validation reads source text and asset
    hashes; it never compiles the project and never loads UdonSharp, so it cannot see a script that does not
    compile, a UdonSharpBehaviour with no program asset, or a scene that generates without its debug panel.
    Those need a real Unity run, which is what this script adds.

    Run this before pushing. When GitHub Actions minutes are available the workflow still runs the Python
    layer on the server, so this script is the superset rather than a replacement.

.PARAMETER Unity
    Path to Unity.exe. Defaults to the editor version this project is pinned to.

.PARAMETER SkipBuild
    Skips scene regeneration and validates the committed scene instead. Faster, but it will not catch a
    builder change that stops producing a valid scene.

.EXAMPLE
    pwsh Tools/Run-LocalChecks.ps1
    pwsh Tools/Run-LocalChecks.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
    [string]$Unity = "D:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$logDirectory = Join-Path $projectPath 'Logs'
if (-not (Test-Path $logDirectory)) { New-Item -ItemType Directory -Path $logDirectory | Out-Null }

$results = [System.Collections.Generic.List[object]]::new()
$failed = $false

function Add-Result([string]$name, [bool]$ok, [string]$detail) {
    $script:results.Add([pscustomobject]@{ Check = $name; Result = $(if ($ok) { 'PASS' } else { 'FAIL' }); Detail = $detail })
    if (-not $ok) { $script:failed = $true }
}

# 1. The package is VPM-managed, so verify its required local compatibility patch before Unity starts.
Write-Host '[1/6] YamaPlayer local patch...' -ForegroundColor Cyan
$patchOutput = & (Join-Path $projectPath 'Tools/Apply-YamaPlayerPatches.ps1') -Mode Check 2>&1
if ($?) {
    Add-Result 'YamaPlayer local patch' $true ($patchOutput | Select-Object -Last 1)
} else {
    Add-Result 'YamaPlayer local patch' $false ($patchOutput | Select-Object -Last 3 | Out-String).Trim()
    $results | Format-Table -AutoSize
    exit 1
}

$unyOutput = & (Join-Path $projectPath 'Tools/Apply-UnyStylusPatches.ps1') -Mode Check 2>&1
if ($?) {
    Add-Result 'UnyStylus mobile shader patch' $true ($unyOutput | Select-Object -Last 1)
} else {
    Add-Result 'UnyStylus mobile shader patch' $false ($unyOutput | Select-Object -Last 3 | Out-String).Trim()
    $results | Format-Table -AutoSize
    exit 1
}

# 2. Static validation. Seconds, no Unity, so it runs before the Editor checks.
Write-Host '[2/6] Static validation (Python)...' -ForegroundColor Cyan
$pythonOutput = & python (Join-Path $projectPath 'Tools/Validate-StargazingImplementation.py') 2>&1
if ($LASTEXITCODE -eq 0) {
    Add-Result 'Static validation' $true 'Tools/Validate-StargazingImplementation.py'
} else {
    Add-Result 'Static validation' $false ($pythonOutput | Select-Object -Last 3 | Out-String).Trim()
}

if (-not (Test-Path $Unity)) {
    Add-Result 'Unity checks' $false "Unity not found at $Unity. Pass -Unity <path>."
    $results | Format-Table -AutoSize
    exit 1
}

function Invoke-UnityCheck([string]$label, [string]$method, [string]$logName) {
    $logPath = Join-Path $logDirectory $logName
    $arguments = @(
        '-batchmode', '-nographics', '-quit',
        '-projectPath', $projectPath,
        '-executeMethod', $method,
        '-logFile', $logPath
    )
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -NoNewWindow -PassThru
    if ($process.ExitCode -eq 0) {
        Add-Result $label $true $logName
        return
    }

    # Unity reports the useful line as a thrown exception or a compiler error; surface just that.
    $reason = Select-String -Path $logPath -Pattern 'error CS\d+|Exception:|Aborting batchmode' |
        Select-Object -First 3 -ExpandProperty Line
    if (-not $reason) { $reason = "exit code $($process.ExitCode); see Logs/$logName" }
    Add-Result $label $false (($reason | Out-String).Trim())
}

# 3. Compilation and UdonSharp program assets. A missing program asset only throws at scene-build time,
#    so this is checked directly rather than inferred.
Write-Host '[3/6] UdonSharp program assets (Unity)...' -ForegroundColor Cyan
Invoke-UnityCheck 'UdonSharp program assets' 'StargazingHill.Editor.StargazingChecks.CheckUdonSharpProgramAssetsForBatchMode' 'LocalCheck-UdonSharp.log'

# 4. Scene generation. Catches a builder that produces an invalid world, which validation alone would miss
#    because it would happily validate the previously committed scene.
if ($SkipBuild) {
    Write-Host '[4/6] Scene build... skipped (-SkipBuild)' -ForegroundColor DarkYellow
    Add-Result 'Scene build' $true 'skipped (-SkipBuild)'
} else {
    Write-Host '[4/6] Scene build (Unity)...' -ForegroundColor Cyan
    Invoke-UnityCheck 'Scene build' 'StargazingHill.Editor.StargazingWorldBuilder.BuildForBatchMode' 'LocalCheck-Build.log'
}

# 5. Scene validation and the sky/meteor numeric tests against the scene on disk.
Write-Host '[5/6] Scene validation and sky/meteor tests (Unity)...' -ForegroundColor Cyan
Invoke-UnityCheck 'Scene validation' 'StargazingHill.Editor.StargazingWorldBuilder.ValidateForBatchMode' 'LocalCheck-Validate.log'
Write-Host '[6/6] Sky and meteor numeric tests (Unity)...' -ForegroundColor Cyan
Invoke-UnityCheck 'Sky and meteor tests' 'StargazingHill.Editor.StargazingWorldBuilder.TestSkyAndMeteorForBatchMode' 'LocalCheck-SkyMeteor.log'

Write-Host ''
$results | Format-Table -AutoSize

if ($failed) {
    Write-Host 'Local checks FAILED.' -ForegroundColor Red
    exit 1
}
Write-Host 'Local checks passed.' -ForegroundColor Green
exit 0
