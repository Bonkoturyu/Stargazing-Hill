# ADR-0012: YamaPlayerの局所互換修正を版限定パッチで管理する

- 状態: Accepted
- 決定日: 2026-08-13
- 更新日: 2026-08-14

## Context

YamaPlayer 2.0.0-beta.7はEditor初期化時にVPM Resolverの更新確認をthread poolで開始する。確認中にPlay Modeへ移行してdomain/threadが終了すると、VCCの正常な`settings.json`読込中でも`ThreadAbortException`が発生し、`Failed to load settings`と`CheckUpdate failed`がConsoleへ赤エラーとして出る。ClientSim自体は初期化を完了するが、ワールド由来のエラーとの区別を妨げる。

YamaPlayerはVPM管理packageであり、本体をGitへ格納しない。直接修正だけではVCCの復元・更新で失われる。2026-08-13に公式VPM indexと公式GitHub releaseを確認し、公開最新版は2.0.0-beta.7だった。release後のdevelop 3 commitはRuntime/UI変更で、対象Editor更新確認コードの修正を含まない。

さらに、YamaPlayerのSDK build hookはEditor用 `PlaylistItem` をruntime `Playlist` Udonへbuild時だけ変換する。保存Sceneを使うClientSimでは変換前のためリストが空になり、Quest uploadでもbuild hookの実行順・Scene clone状態によってruntimeデータが欠けた。ワールド側でruntime Playlistを先に保存すると、上流hookが同じGameObjectへ二重追加するため、再利用処理も必要になった。

PCVR実機でもPlaylistとAutoPlayが空のままだった追加調査で、runtime Playlistの親がYamaPlayer root、Controllerがその兄弟であることを確認した。Controllerは`Start()`で自分の子からPlaylistを探索し、さらにbuild後処理でYamaPlayer rootから切り離されるため、兄弟に保存したPlaylistを実機で取得できない構造だった。

## Options

- エラーを無視する: Consoleの赤エラーを残し、ClientSim回帰判定を曖昧にするため不採用。
- YamaPlayer package全体をforkまたはGit管理する: 更新・差分・再配布境界が大きくなるため不採用。
- 自動更新確認だけを止める版限定patchを保持する: RuntimeとVCC更新を維持したまま、競合する非必須処理だけを除外できるため採用。
- YamaPlayer標準Playlist Editorを唯一の編集面とし、保存時に既存runtime componentへ反映する版限定patchを使う: 独自UI・独自設定形式を増やさずClientSimとupload buildの入力を同一にできるため採用。

## Decision

- `PackageManager`のstatic constructorからEditor起動時の`CheckUpdate().Forget()`登録だけを除去する。
- Playlist編集はYamaPlayer標準Inspectorまたは `YamaPlayer/Edit Playlist`だけを使い、保存Scene内の`PlaylistItem`を編集上の正本とする。Stargazing Hill独自の`music_list.txt`、同期menu、asset import hook、scene save hookは廃止する。
- 標準Playlist Editorの保存完了時に上流`PlaylistBuildProcess`を呼び、同じGameObjectの既存runtime `Playlist`を再利用して値をUdon backingへcopyする。
- PlaylistManagerとruntime PlaylistをControllerの子へ配置し、保存Scene・build後の切離し後ともControllerの探索範囲に維持する。
- 全Scene再生成では保存Sceneの標準エディター編集済みYamaPlayer GameObjectを複製し、別形式からPlaylistを再構築しない。
- `CheckUpdate()`、`UpdatePackage()`、VCCによるpackage管理、Runtime、Prefabは変更しない。
- 差分は`Tools/YamaPlayerPatches`、適用器は`Tools/Apply-YamaPlayerPatches.ps1`を正規経路とする。
- package版を厳密に2.0.0-beta.7へ限定し、元断片にも適用済み断片にも一致しない場合は書き換えず停止する。
- VCC復元・更新後、Unity起動前に再適用する。ローカル検証は適用済みでなければUnityを開始しない。

## Consequences

- Editor起動直後の自動最新版確認は行われない。更新はVCCで管理し、必要ならYamaPlayerの手動確認を使う。
- package本体と修正版はGitおよび再配布unitypackageへ含めない。
- Playlistデータは標準Editorの保存時に保存SceneのController子階層へ反映されるため、ClientSimとPC / Android / iOS buildが同じ値を読む。SDK hookの二重component生成を避け、build後にControllerだけが切り離されてもPlaylistが追従する。
- YamaPlayer更新時にはパッチが意図的に停止する。上流に同等修正があれば撤去し、未修正なら新版の元ソースを確認して新しい最小patchを追加する。

## Evidence

- 公式VPM index: https://vpm.kwxxw.net/index.json （確認日2026-08-13、対象版2.0.0-beta.7）
- 公式release: https://github.com/koorimizuw/YamaPlayer/releases/tag/2.0.0-beta.7 （確認日2026-08-13）
- 公式develop package version: https://github.com/koorimizuw/YamaPlayer/blob/develop/package.json （確認日2026-08-13）
- 対象元ファイルSHA-256: `08c5154f81e45f0dacb9bf994d76b3889e2296aa66ea64b70c22e0b0eb1059dd`
