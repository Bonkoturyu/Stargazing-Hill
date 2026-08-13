# クリーン環境からの復元

状態: `Confirmed`

確認日: 2026-08-13

## 前提

- Unity `2022.3.22f1`
- VRChat Creator Companion（VCC）
- 正規購入済み `UnyStylus_v1.3.unitypackage`

## 手順

1. リポジトリをcloneし、VCCのProjectsへ追加する。
2. VCCからプロジェクトを開く。`Packages/vpm-manifest.json` の固定版を使って、VRChat SDK、AudioLink、YamaPlayer、QvPen、VRWorld Toolkitを復元する。
3. 購入済み `UnyStylus_v1.3.unitypackage` をImportする。ビルダーが参照するPrefabは `Assets/Rasta/UnyStylus/UnyStylus.prefab` とする。
4. 初回importとUdonSharp compileが静止するまで待ち、一度Unityを終了して開き直す。初回import中にビルダーを実行しない。
5. 再起動後、Consoleのcompile errorが0件であることを確認する。
6. `python Tools/Validate-StargazingImplementation.py` を実行する。
7. Unityメニュー `Stargazing Hill/Build Complete World` を実行し、`Assets/StargazingHill/Scenes/StargazingHill.unity` を開く。
8. `Stargazing Hill/Validate Saved Scene` を実行する。

## Gitへ入れないもの

- `Library/`、`Temp/`、`Logs/`、`Obj/`、ビルド出力
- VPMが復元するpackage本体
- UdonSharp生成キャッシュ
- 購入品UnyStylusのpackageと展開本体

UnyStylusが未導入ならビルダーは意図的に停止する。購入品を代替ファイルで埋めたり、Gitへ再配布したりしない。Libraryなしの初回importと同時にbatchビルダーを呼ぶと、YamaPlayerのUdon extension登録が一時的に二重化することがある。import完了後の再起動で解消し、以後のbuildでは再現しない。

## 再配布用unitypackage

1. 上記手順で外部依存を復元し、`Stargazing Hill/Build Complete World` と保存Scene検証を通す。
2. Unityメニュー `Stargazing Hill/Export/Redistributable UnityPackage...` を実行する。
3. 出力されたpackageには `Assets/StargazingHill` だけが含まれ、YamaPlayer、QvPen、UnyStylus本体は含まれない。
4. 配布先ではVCC/VPMでYamaPlayerとQvPenを復元し、正規購入済みUnyStylus v1.3をImportしてからpackageを利用する。

自動生成はUnityのbatch modeで `StargazingHill.Editor.StargazingUnityPackageExporter.ExportForBatchMode` を呼び、`Build/StargazingHill-redistributable.unitypackage` を `python Tools/Validate-UnityPackage.py Build/StargazingHill-redistributable.unitypackage` で検査する。Unity標準の **Include dependencies** を使った書き出しは、外部packageを混入させるため再配布経路に使用しない。設計判断は [ADR 0010](adr/0010-redistributable-unitypackage-boundary.md) を正本とする。

## 検証境界

追跡ファイルだけの静的検証とUnity Editorでの生成・保存Scene検証は自動化する。購入品の再import、VCCのオンラインpackage解決、ClientSim、PC / Quest / iOS実機確認は環境依存のため、実施日時と結果を [TEST_PLAN.md](TEST_PLAN.md) に記録する。
