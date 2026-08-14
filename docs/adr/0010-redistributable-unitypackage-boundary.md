# ADR-0010: 再配布用unitypackageの所有境界

- 状態: Accepted
- 決定日: 2026-08-13

## Context

保存SceneはYamaPlayer、QvPen、購入済みUnyStylusを参照する。しかし、YamaPlayerとQvPenはVPMで復元する第三者packageであり、UnyStylusは購入者ごとに正規packageをImportする素材である。Sceneの依存を再帰的に含める通常の書き出し操作は、これらのpackage本体まで配布物へ混入させる危険がある。

Unity 2022.3の `AssetDatabase.ExportPackage` は、`ExportPackageOptions.IncludeDependencies` を指定したときに依存関係を含める。確認日2026-08-13、対象版2022.3.22f1。[Unity 2022.3 ExportPackageOptions](https://docs.unity3d.com/ja/2022.3/ScriptReference/ExportPackageOptions.html)

## Decision

- 再配布対象の所有境界を `Assets/StargazingHill` のみに固定し、ローカルのベイク入力 `SourceDownloads` は除外する。
- `StargazingUnityPackageExporter` が所有境界内のディレクトリとアセットを明示的に列挙し、`ExportPackageOptions.Default` で書き出す。
- `IncludeDependencies` は使用しない。
- 選択パスが所有境界を外れた場合、または `Assets/Rasta`、YamaPlayer、QvPenのpackageパスが選択された場合は書き出しを停止する。
- 配布物には依存復元手順 `Assets/StargazingHill/README_UNITYPACKAGE.md` を必ず含める。
- 生成後は `Tools/Validate-UnityPackage.py` でarchive内の全pathnameを検査する。

## Consequences

YamaPlayer、QvPen、UnyStylusの本体はunitypackageへ同梱されず、配布先ではVCC/VPMと正規購入品から別途復元する必要がある。Sceneは外部Prefab参照を持つため、依存を復元してからImport・利用する。メニュー `Stargazing Hill/Build & Export/Redistributable UnityPackage...` を配布用package作成の正規経路とし、Unity標準の依存込みExportは使用しない。

## GitHub Release自動化（2026-08-14追記）

- `.github/workflows/release-unitypackage.yml` をtag `v*` と既存tagを指定する手動実行の正規経路とする。
- CIではUnity EditorやUnity licenseを要求せず、追跡済み `.meta` の明示listからUnityPackageを構築する。
- `Tools/Validate-UnityPackage.py` の検査を通過したpackageだけを、SHA-256一覧とともにZIP化する。
- ZIPとchecksumを同じtagのGitHub Releaseへ添付する。
- 使用Actionはcommit SHAへ固定し、job権限はRelease更新に必要な `contents: write` だけとする。
- Budget/Billing制限中はworkflowが起動できないため、最初のtag releaseとclean projectへのimport結果は `Pending Evidence` とする。
