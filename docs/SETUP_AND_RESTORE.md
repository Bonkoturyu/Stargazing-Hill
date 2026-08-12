# クリーン環境からの復元

状態: `Confirmed`

確認日: 2026-08-12

## 前提

- Unity `2022.3.22f1`
- VRChat Creator Companion（VCC）
- 正規購入済み `UnyStylus_v1.3.unitypackage`

## 手順

1. リポジトリをcloneし、VCCのProjectsへ追加する。
2. VCCからプロジェクトを開く。`Packages/vpm-manifest.json` の固定版を使って、VRChat SDK、AudioLink、YamaPlayer、QvPen、VRWorld Toolkitを復元する。
3. 購入済み `UnyStylus_v1.3.unitypackage` をImportする。ビルダーが参照するPrefabは `Assets/Rasta/UnyStylus/UnyStylus.prefab` とする。
4. UnityのimportとUdonSharp compileが完了し、Consoleのcompile errorが0件であることを確認する。
5. `python Tools/Validate-StargazingImplementation.py` を実行する。
6. Unityメニュー `Stargazing Hill/Build Complete World` を実行し、`Assets/StargazingHill/Scenes/StargazingHill.unity` を開く。
7. `Stargazing Hill/Validate Saved Scene` を実行する。

## Gitへ入れないもの

- `Library/`、`Temp/`、`Logs/`、`Obj/`、ビルド出力
- VPMが復元するpackage本体
- UdonSharp生成キャッシュ
- 購入品UnyStylusのpackageと展開本体

UnyStylusが未導入ならビルダーは意図的に停止する。購入品を代替ファイルで埋めたり、Gitへ再配布したりしない。

## 検証境界

追跡ファイルだけの静的検証とUnity Editorでの生成・保存Scene検証は自動化する。購入品の再import、VCCのオンラインpackage解決、ClientSim、PC / Quest / iOS実機確認は環境依存のため、実施日時と結果を [TEST_PLAN.md](TEST_PLAN.md) に記録する。
