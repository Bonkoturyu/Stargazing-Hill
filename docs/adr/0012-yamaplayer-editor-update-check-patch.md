# ADR-0012: YamaPlayer Editor自動更新確認を版限定パッチで停止する

- 状態: Accepted
- 決定日: 2026-08-13

## Context

YamaPlayer 2.0.0-beta.7はEditor初期化時にVPM Resolverの更新確認をthread poolで開始する。確認中にPlay Modeへ移行してdomain/threadが終了すると、VCCの正常な`settings.json`読込中でも`ThreadAbortException`が発生し、`Failed to load settings`と`CheckUpdate failed`がConsoleへ赤エラーとして出る。ClientSim自体は初期化を完了するが、ワールド由来のエラーとの区別を妨げる。

YamaPlayerはVPM管理packageであり、本体をGitへ格納しない。直接修正だけではVCCの復元・更新で失われる。2026-08-13に公式VPM indexと公式GitHub releaseを確認し、公開最新版は2.0.0-beta.7だった。release後のdevelop 3 commitはRuntime/UI変更で、対象Editor更新確認コードの修正を含まない。

## Options

- エラーを無視する: Consoleの赤エラーを残し、ClientSim回帰判定を曖昧にするため不採用。
- YamaPlayer package全体をforkまたはGit管理する: 更新・差分・再配布境界が大きくなるため不採用。
- 自動更新確認だけを止める版限定patchを保持する: RuntimeとVCC更新を維持したまま、競合する非必須処理だけを除外できるため採用。

## Decision

- `PackageManager`のstatic constructorからEditor起動時の`CheckUpdate().Forget()`登録だけを除去する。
- `CheckUpdate()`、`UpdatePackage()`、VCCによるpackage管理、Runtime、Prefabは変更しない。
- 差分は`Tools/YamaPlayerPatches`、適用器は`Tools/Apply-YamaPlayerPatches.ps1`を正規経路とする。
- package版を厳密に2.0.0-beta.7へ限定し、元断片にも適用済み断片にも一致しない場合は書き換えず停止する。
- VCC復元・更新後、Unity起動前に再適用する。ローカル検証は適用済みでなければUnityを開始しない。

## Consequences

- Editor起動直後の自動最新版確認は行われない。更新はVCCで管理し、必要ならYamaPlayerの手動確認を使う。
- package本体と修正版はGitおよび再配布unitypackageへ含めない。
- YamaPlayer更新時にはパッチが意図的に停止する。上流に同等修正があれば撤去し、未修正なら新版の元ソースを確認して新しい最小patchを追加する。

## Evidence

- 公式VPM index: https://vpm.kwxxw.net/index.json （確認日2026-08-13、対象版2.0.0-beta.7）
- 公式release: https://github.com/koorimizuw/YamaPlayer/releases/tag/2.0.0-beta.7 （確認日2026-08-13）
- 公式develop package version: https://github.com/koorimizuw/YamaPlayer/blob/develop/package.json （確認日2026-08-13）
- 対象元ファイルSHA-256: `08c5154f81e45f0dacb9bf994d76b3889e2296aa66ea64b70c22e0b0eb1059dd`
