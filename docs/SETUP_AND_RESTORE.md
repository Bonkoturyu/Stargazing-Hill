# クリーン環境からの復元

状態: `Confirmed`

確認日: 2026-08-13

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
9. Unityメニュー `Stargazing Hill/Build Complete World` を実行し、`Assets/StargazingHill/Scenes/StargazingHill.unity` を開く。
10. `Stargazing Hill/Validate Saved Scene` を実行する。

## Gitへ入れないもの

- `Library/`、`Temp/`、`Logs/`、`Obj/`、ビルド出力
- VPMが復元するpackage本体
- UdonSharp生成キャッシュ
- 購入品UnyStylusのpackageと展開本体

UnyStylusが未導入ならビルダーは意図的に停止する。購入品を代替ファイルで埋めたり、Gitへ再配布したりしない。Libraryなしの初回importと同時にbatchビルダーを呼ぶと、YamaPlayerのUdon extension登録が一時的に二重化することがある。import完了後の再起動で解消し、以後のbuildでは再現しない。

YamaPlayer 2.0.0-beta.7には、Editor起動時のVPM自動更新確認がPlay Mode移行と競合し、VCC `settings.json` 読込threadのabortを赤エラーとして出す場合がある。ローカルパッチは自動確認の起動だけを止め、VCC更新と手動確認は残す。適用・確認・復元・新版対応は [`Tools/YamaPlayerPatches/README.md`](../Tools/YamaPlayerPatches/README.md) を参照する。

UnyStylus v1.3の2 shaderはAndroid / iOSのGLES3で `unityFogFactor` を同じgeometry関数内に再定義する。ローカルパッチは各fog macro呼出しにblock scopeを付けるだけで、色・形状・描画式は変えない。SHA-256照合、適用、確認、復元は [`Tools/UnyStylusPatches/README.md`](../Tools/UnyStylusPatches/README.md) を正規手順とする。

## 再配布用unitypackage

1. 上記手順で外部依存を復元し、`Stargazing Hill/Build Complete World` と保存Scene検証を通す。
2. Unityメニュー `Stargazing Hill/Export/Redistributable UnityPackage...` を実行する。
3. 出力されたpackageには `Assets/StargazingHill` だけが含まれ、YamaPlayer、QvPen、UnyStylus本体は含まれない。
4. 配布先ではVCC/VPMでYamaPlayerとQvPenを復元し、正規購入済みUnyStylus v1.3をImportしてからpackageを利用する。

自動生成はUnityのbatch modeで `StargazingHill.Editor.StargazingUnityPackageExporter.ExportForBatchMode` を呼び、`Build/StargazingHill-redistributable.unitypackage` を `python Tools/Validate-UnityPackage.py Build/StargazingHill-redistributable.unitypackage` で検査する。Unity標準の **Include dependencies** を使った書き出しは、外部packageを混入させるため再配布経路に使用しない。設計判断は [ADR 0010](adr/0010-redistributable-unitypackage-boundary.md) を正本とする。

## 検証境界

追跡ファイルだけの静的検証とUnity Editorでの生成・保存Scene検証は自動化する。購入品の再import、VCCのオンラインpackage解決、ClientSim、PC / Quest / iOS実機確認は環境依存のため、実施日時と結果を [TEST_PLAN.md](TEST_PLAN.md) に記録する。
