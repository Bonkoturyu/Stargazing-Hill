# クリーン環境からの復元

状態: `Confirmed`

確認日: 2026-08-14

## 前提

- Unity `2022.3.22f1`
- VRChat Creator Companion（VCC）
- 正規購入済み `UnyStylus_v1.3.unitypackage`

## 手順

1. リポジトリをcloneし、VCCのProjectsへ追加する。
2. VCCのManage Projectで、`Packages/vpm-manifest.json` の固定版を使ってVRChat SDK、AudioLink、YamaPlayer、QvPen、VRWorld Toolkitを復元する。
3. Unityを起動する前に、リポジトリrootで `powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1` を実行する。YamaPlayerをVCCで復元・更新した場合も同じコマンドを再実行する。
4. VCCからプロジェクトを開き、購入済み `UnyStylus_v1.3.unitypackage` をImportする。ビルダーが参照するPrefabは `Assets/Rasta/UnyStylus/UnyStylus.prefab` とする。
5. Unityを閉じ、`powershell -ExecutionPolicy Bypass -File Tools/Apply-UnyStylusPatches.ps1` を実行する。UnyStylusを再importした場合も再実行する。
6. 初回importとUdonSharp compileが静止するまで待ち、一度Unityを終了して開き直す。初回import中にビルダーを実行しない。
7. 再起動後、Consoleのcompile errorが0件であることを確認する。
8. `python Tools/Validate-StargazingImplementation.py` を実行する。
9. `Assets/StargazingHill/Scenes/StargazingHill.unity` を開く。clone直後の通常利用では全Scene再生成は不要。
10. `Stargazing Hill/Validate Saved Scene` を実行する。

## 生成・保守メニュー

| 目的 | メニュー | 注意 |
|---|---|---|
| 保存Sceneを検証 | `Stargazing Hill/Validate Saved Scene` | 公開・upload・package出力前に実行する |
| 説明パネルをHierarchyで選択 | `Stargazing Hill/Content/Information Panel/Select in Hierarchy` | 手編集の入口。生成階層を置換しない |
| 説明パネルだけ検証 | `Stargazing Hill/Content/Information Panel/Validate` | 5言語、参照、階層、座標を検査する |
| 説明パネルの保存済み座標を適用 | `Stargazing Hill/Content/Information Panel/Apply Saved Readability Layout` | 内容や子階層を再生成せず、確定済み配置だけを戻す |
| 設定ボードを再生成 | `Stargazing Hill/Content/Settings Board/Rebuild...` | `World/SettingsSystem`とラジオ連携を置換する。説明・ピクニック生成後に実行する |
| 生成機能を一括更新 | `Stargazing Hill/Advanced/Generated Content/Upgrade All Generated Features...` | Mesh、外部連携、各パネル、ピクニックをversioned sourceから置換する |
| Sceneを全再生成 | `Stargazing Hill/Advanced/Generated Content/Rebuild Complete World (Destructive)...` | 未保存・未captureの手修正を失う可能性があるため、必要時だけ使う |
| YamaPlayerのPlaylistを編集 | Inspectorの「プレイリストを編集する」または `YamaPlayer/Edit Playlist` | YamaPlayer標準機能。編集後はSceneを保存する |
| 現在のピクニック配置を生成側へ保存 | `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...` | Sceneを保存後、`PicnicLayout.json`を更新する |
| 保存配置からピクニックを再生成 | `Stargazing Hill/Content/Picnic/Rebuild from Saved Layout...` | 現在の`PicnicSpot`を置換する |
| 配布packageを作成 | `Stargazing Hill/Build & Export/Redistributable UnityPackage...` | 保存Scene検証を自動実行し、外部依存を含めない |

ピクニックをScene上で手調整した場合は、Sceneを保存し、配置保存メニューを実行してから、Sceneと `Assets/StargazingHill/Editor/Data/PicnicLayout.json` の両方をcommitする。Anchor Transformに加えて各 `Model` 子のlocal Transformも保存されるため、clone先の全再生成でも同じ配置を復元できる。

### InformationSystemの編集境界

`World/InformationSystem/WorldInformationPanel` は次の目的別グループに分かれる。親Transformはidentityで、子の見た目の座標を変えない。

```text
WorldInformationPanel
├─ Visual
│  ├─ PanelSheet
│  ├─ Descriptions      # TitleCanvasと5言語本文
│  └─ Presence          # 人数、端末アイコン、入退室履歴
└─ Controls
   ├─ LanguageToggle
   ├─ DebugPanelToggle
   └─ Observatory       # 見出し、前後、選択地点、22地点一覧
```

文言や見た目の調整は該当グループ内で行う。`Advanced/Generated Content/Rebuild InformationSystem (Replaces Children)...` は `InformationSystem` 全体を生成コードの値で置換するため、手編集を残したい場合は使わない。Editor起動時の自動置換は行わない。生成値自体を変更する場合は `WorldInformationPanelInstaller.cs` も更新し、再生成後に `Content/Information Panel/Validate` を通す。

### SettingsSystemの編集境界

`World/SettingsSystem` は `WorldSettingsSystemInstaller.cs` が生成するローカル機能である。ボードの座標・ボタン、5方向ミラー、ナイトモードsphere、木の表示ボタン、ラジオの追加Speakerを一括管理する。手作業で子を変更しても再生成時に失われるため、恒久変更はinstallerへ反映する。ピクニックのラジオ参照を使うので、個別再生成はInformationSystemとPicnicSpotが存在する状態で行う。

### YamaPlayerの編集境界

Playlistの正本は保存Scene内のYamaPlayer標準 `PlaylistItem` とし、Inspectorの「プレイリストを編集する」または `YamaPlayer/Edit Playlist` から編集する。`music_list.txt`、Stargazing Hill独自のPlaylist Editor、自動同期hookは使用しない。編集後はYamaPlayer Playlist Editorの保存とScene保存を行う。`Rebuild Complete World` は保存Sceneの標準エディター編集済みYamaPlayerを複製するため、Playlist、AutoPlay、module設定を独自形式へ変換しない。

## Gitへ入れないもの

- `Library/`、`Temp/`、`Logs/`、`Obj/`、ビルド出力
- VPMが復元するpackage本体
- UdonSharp生成キャッシュ
- 購入品UnyStylusのpackageと展開本体

UnyStylusが未導入ならビルダーは意図的に停止する。購入品を代替ファイルで埋めたり、Gitへ再配布したりしない。Libraryなしの初回importと同時にbatchビルダーを呼ぶと、YamaPlayerのUdon extension登録が一時的に二重化することがある。import完了後の再起動で解消し、以後のbuildでは再現しない。

YamaPlayer 2.0.0-beta.7には、Editor起動時のVPM自動更新確認がPlay Mode移行と競合し、VCC `settings.json` 読込threadのabortを赤エラーとして出す場合がある。ローカルパッチは自動確認の起動だけを止め、VCC更新と手動確認は残す。適用・確認・復元・新版対応は [`Tools/YamaPlayerPatches/README.md`](../Tools/YamaPlayerPatches/README.md) を参照する。

UnyStylus v1.3の2 shaderはAndroid / iOSのGLES3で `unityFogFactor` を同じgeometry関数内に再定義する。ローカルパッチは各fog macro呼出しにblock scopeを付けるだけで、色・形状・描画式は変えない。SHA-256照合、適用、確認、復元は [`Tools/UnyStylusPatches/README.md`](../Tools/UnyStylusPatches/README.md) を正規手順とする。

## 再配布用unitypackage

1. 上記手順で外部依存を復元し、`Stargazing Hill/Validate Saved Scene`を通す。全再生成を試験する公開候補では、事前にSceneを退避してから`Stargazing Hill/Advanced/Generated Content/Rebuild Complete World (Destructive)...`も確認する。
2. Unityメニュー `Stargazing Hill/Build & Export/Redistributable UnityPackage...` を実行する。
3. 出力されたpackageには `Assets/StargazingHill` だけが含まれ、YamaPlayer、QvPen、UnyStylus本体は含まれない。
4. 配布先ではVCC/VPMでYamaPlayerとQvPenを復元し、正規購入済みUnyStylus v1.3をImportしてからpackageを利用する。

自動生成はUnityのbatch modeで `StargazingHill.Editor.StargazingUnityPackageExporter.ExportForBatchMode` を呼び、`Build/StargazingHill-redistributable.unitypackage` を `python Tools/Validate-UnityPackage.py Build/StargazingHill-redistributable.unitypackage` で検査する。Unity標準の **Include dependencies** を使った書き出しは、外部packageを混入させるため再配布経路に使用しない。設計判断は [ADR 0010](adr/0010-redistributable-unitypackage-boundary.md) を正本とする。

### GitHub Releaseから取得する

tag `v1.2.3` のpush時、`.github/workflows/release-unitypackage.yml` が `StargazingHill-1.2.3.zip` を作り、GitHub Releaseへ添付する。ZIPには次を含める。

- `StargazingHill-1.2.3.unitypackage`
- `SHA256SUMS.txt`

手動実行ではGitHub Actionsの **Run workflow** から、既に存在する `v*` tagを `release_tag` に入力する。tagはworkflowを含む既定branch `main` のcommitへ付ける。実行前にActionsが有効で、Release作成用の `contents: write` がworkflow jobへ限定されていることを確認する。

このCI経路も `Assets/StargazingHill` だけを対象とし、archive検査を通過しなければReleaseを更新しない。YamaPlayer、QvPen、購入品UnyStylusはZIP内のunitypackageに含まれないため、import前に本章冒頭の手順で別途復元する。

## 検証境界

追跡ファイルだけの静的検証とUnity Editorでの生成・保存Scene検証は自動化する。購入品の再import、VCCのオンラインpackage解決、ClientSim、PC / Quest / iOS実機確認は環境依存のため、実施日時と結果を [TEST_PLAN.md](TEST_PLAN.md) に記録する。
