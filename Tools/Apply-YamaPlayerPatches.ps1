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
if (-not (Test-Path -LiteralPath $patchPath)) { throw "Patch file is missing: $patchRelativePath" }

$transforms = @(
    @{
        Path = 'Packages/net.kwxxw.yama-stream/Editor/Package/PackageManager.cs'
        Original = @'
      EditorApplication.delayCall += () =>
      {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
          CheckUpdate().Forget();
        }
      };
'@
        Patched = @'
      // Stargazing Hill: package updates are managed through VCC. Starting the VPM resolver
      // automatically can race with Play Mode/domain shutdown while VCC settings are being read.
'@
    },
    @{
        Path = 'Packages/net.kwxxw.yama-stream/Editor/Playlist/PlaylistBuildProcess.cs'
        Original = @'
          var udonPlaylist = item.gameObject.AddUdonSharpComponent<Playlist>();
'@
        Patched = @'
          // Stargazing Hill persists runtime Playlists so ClientSim and uploaded builds use the same data.
          // Reuse that component when present instead of creating a duplicate during the SDK build hook.
          var udonPlaylist = item.GetComponent<Playlist>();
          if (udonPlaylist == null) udonPlaylist = item.gameObject.AddUdonSharpComponent<Playlist>();
'@
    },
    @{
        Path = 'Packages/net.kwxxw.yama-stream/Editor/Playlist/PlaylistBuildProcess.cs'
        Original = @'
          udonPlaylist.SetProgramVariable("_urls", urls);

          results.Add(udonPlaylist);
'@
        Patched = @'
          udonPlaylist.SetProgramVariable("_urls", urls);
          UdonSharpEditorUtility.CopyProxyToUdon(udonPlaylist);
          UnityEditor.EditorUtility.SetDirty(udonPlaylist);

          results.Add(udonPlaylist);
'@
    },
    @{
        Path = 'Packages/net.kwxxw.yama-stream/Editor/Playlist/PlaylistEditorWindow.cs'
        Original = @'
      if (_player != null)
      {
        EditorUtility.SetDirty(_player.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_player.gameObject.scene);
      }
'@
        Patched = @'
      if (_player != null)
      {
        // Keep the standard YamaPlayer Playlist Editor as the single authoring surface while also
        // refreshing the persisted runtime Playlist components used by ClientSim.
        new PlaylistBuildProcess().Process();
        EditorUtility.SetDirty(_player.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_player.gameObject.scene);
      }
'@
    }
)

foreach ($transform in $transforms) {
    $sourcePath = Join-Path $projectPath $transform.Path
    if (-not (Test-Path -LiteralPath $sourcePath)) { throw "YamaPlayer source is missing: $($transform.Path)" }
    $raw = [IO.File]::ReadAllText($sourcePath)
    $usesCrLf = $raw.Contains("`r`n")
    $text = $raw.Replace("`r`n", "`n")
    $original = $transform.Original.Replace("`r`n", "`n").Trim("`r", "`n")
    $patched = $transform.Patched.Replace("`r`n", "`n").Trim("`r", "`n")
    $hasOriginal = $text.Contains($original)
    $hasPatched = $text.Contains($patched)
    if ($hasOriginal -eq $hasPatched) {
        throw "YamaPlayer source matches neither or both verified fragments: $($transform.Path)"
    }
    if ($Mode -eq 'Check') {
        if (-not $hasPatched) { throw "YamaPlayer $($package.version) patch is not applied: $($transform.Path)" }
        continue
    }
    $from = if ($Mode -eq 'Apply') { $original } else { $patched }
    $to = if ($Mode -eq 'Apply') { $patched } else { $original }
    if (-not $text.Contains($from)) { continue }
    $text = $text.Replace($from, $to)
    if ($usesCrLf) { $text = $text.Replace("`n", "`r`n") }
    [IO.File]::WriteAllText($sourcePath, $text, [Text.UTF8Encoding]::new($false))
}

Write-Output "YamaPlayer $($package.version) compatibility patch state: $Mode."
