# YamaPlayerローカルパッチ

状態: `Confirmed`

VPMが復元するYamaPlayer本体はGitへ格納せず、再適用に必要な差分だけをこのディレクトリで管理する。

## 現在のパッチ

| 対象版 | パッチ | 目的 |
|---|---|---|
| `2.0.0-beta.7` | `2.0.0-beta.7-disable-editor-auto-update.patch` | Editor起動時のVPM自動更新確認を止め、Play Mode移行時にVCC設定読込threadがabortされる競合を避ける |

変更対象は `Packages/net.kwxxw.yama-stream/Editor/Package/PackageManager.cs` のstatic constructorだけである。`CheckUpdate()`と`UpdatePackage()`は残すため、YamaPlayerの手動確認とVCCによる更新は利用できる。Runtimeコード、Prefab、動画再生処理は変更しない。

## 使い方

VCCでYamaPlayerを復元または更新した後、Unityを起動する前にリポジトリrootで実行する。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1
```

適用済み確認と上流状態への復元:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1 -Mode Check
powershell -ExecutionPolicy Bypass -File Tools/Apply-YamaPlayerPatches.ps1 -Mode Restore
```

スクリプトは`package.json`の版を厳密に照合する。同じ版への再実行は何も変更せず成功し、未対応版または検証済み断片と異なるソースには書き込まず失敗する。

## YamaPlayer更新時

1. 先に旧パッチを`-Mode Restore`で戻し、VCCで更新する。
2. 上流で自動更新確認の競合が直っているか確認する。直っていれば旧パッチを新しい版へ流用しない。
3. 修正がまだ必要なら、新版の元ソースに対する最小差分を新しい`.patch`として追加する。
4. `Apply-YamaPlayerPatches.ps1`の版とpatch対応表を追加し、適用・二重適用・復元・不一致時停止を確認する。
5. `docs/legal/THIRD_PARTY_DEPENDENCIES.md`、ADR、テスト記録を同じ変更で更新する。

設計判断と撤去条件は[ADR 0012](../../docs/adr/0012-yamaplayer-editor-update-check-patch.md)を正本とする。
