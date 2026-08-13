<#
.SYNOPSIS
    Applies the verified UnyStylus v1.3 mobile shader compatibility patch.
#>
[CmdletBinding()]
param(
    [ValidateSet('Apply', 'Check', 'Restore')]
    [string]$Mode = 'Apply'
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$files = @(
    @{
        Path = 'Assets/Rasta/UnyStylus/Shader/rounded_trail_for_uny_stylus.shader'
        Original = '5BD3AA00E719F7E083DB37E2262D390F7CB72B1663AB67E208A5B73A7B238A04'
        OriginalNoBom = '2F913CE11BB1E22A1E9D779F5855B0CC590DB6731046708E127072BBFF37EB17'
        Patched = 'AAB6048726C6DDB9CAF50A939696AE5BB66961E3FF6E2543647D35D19559B470'
    },
    @{
        Path = 'Assets/Rasta/UnyStylus/Shader/rounded_trail_for_uny_stylus_selected.shader'
        Original = 'E1725425FC753C255B04E8197E843DF8EC6325E63C8365DBB37D9929B61D0CCB'
        OriginalNoBom = 'E0C3F3A6F9DA69F2A3A165D01D17F8DDB31ACEFA5798444D5219CDD8D25140E6'
        Patched = 'C2EFC448479CA919090C3D65ECFC18227AD527289C8A6B3C3E5BAAFED746510C'
    }
)

foreach ($entry in $files) {
    $path = Join-Path $projectPath $entry.Path
    if (-not (Test-Path -LiteralPath $path)) { throw "UnyStylus v1.3 is missing: $($entry.Path)" }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash
    if ($Mode -eq 'Check') {
        if ($hash -ne $entry.Patched) { throw "UnyStylus patch is missing or source changed: $($entry.Path)" }
        continue
    }
    if ($Mode -eq 'Apply') {
        if ($hash -eq $entry.Patched) { continue }
        if ($hash -ne $entry.Original -and $hash -ne $entry.OriginalNoBom) {
            throw "Unverified UnyStylus source; refusing to patch: $($entry.Path)"
        }
        $text = [IO.File]::ReadAllText($path)
        $text = $text.Replace('UNITY_TRANSFER_FOG(o, o.vertex);', '{ UNITY_TRANSFER_FOG(o, o.vertex); }')
    } else {
        if ($hash -eq $entry.Original) { continue }
        if ($hash -ne $entry.Patched) { throw "Unverified UnyStylus source; refusing to restore: $($entry.Path)" }
        $text = [IO.File]::ReadAllText($path)
        $text = $text.Replace('{ UNITY_TRANSFER_FOG(o, o.vertex); }', 'UNITY_TRANSFER_FOG(o, o.vertex);')
    }
    # The purchased v1.3 sources use a UTF-8 BOM. Apply writes the verified normalized
    # patched form; Restore deliberately puts the BOM back so the upstream hash is exact.
    $encoding = [Text.UTF8Encoding]::new($Mode -eq 'Restore')
    [IO.File]::WriteAllText($path, $text, $encoding)
    $expected = if ($Mode -eq 'Apply') { $entry.Patched } else { $entry.Original }
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash -ne $expected) {
        throw "UnyStylus patch result did not match the verified hash: $($entry.Path)"
    }
}

Write-Output "UnyStylus v1.3 mobile shader patch state: $Mode."
