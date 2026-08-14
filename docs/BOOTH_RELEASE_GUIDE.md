# BOOTH配布準備

状態: `In Progress`

確認日: 2026-08-14

この文書は、BOOTHへ登録する顧客向けZIPを作るための作業正本である。価格、サポート範囲、商品ページ文面はまだ `Open` とする。

## 配布できる範囲

- Stargazing Hill固有部分はMIT Licenseで、販売を含む利用が可能。配布物には著作権表示とMIT本文を残す。
- HYG由来データはCC BY-SA 4.0。package内の `Editor/Data/NOTICE.md` とライセンスを保持する。
- Poly HavenとTiny TreatsはCC0、Noto Sans CJKはSIL Open Font License 1.1。隣接NOTICE・ライセンスを保持する。
- YamaPlayer、QvPen、購入品UnyStylusの本体は商品へ同梱しない。

状態は `Confirmed`。権利の正本は [legal/THIRD_PARTY_ASSETS.md](legal/THIRD_PARTY_ASSETS.md) と [legal/THIRD_PARTY_DEPENDENCIES.md](legal/THIRD_PARTY_DEPENDENCIES.md) とする。

## 購入者へ明示する依存

- Unity `2022.3.22f1`
- VRChat Creator Companion / VRChat Worlds SDK
- YamaPlayer `2.0.0-beta.7` とQvPen `3.3.15` をVCC/VPMから別途復元
- UnyStylus `v1.3` は購入者自身による正規購入とimportが必要

特にUnyStylusは有料の別商品であり、Stargazing Hillの販売物には含まれないことを商品ページの購入前に読める位置へ記載する。状態は `Confirmed`。

## 顧客向けZIPの作成

1. Unityで保存済みSceneを開き、`Stargazing Hill/World/Validate Saved Scene` を実行する。
2. `Stargazing Hill/Build & Export/Redistributable UnityPackage...` から `Build/StargazingHill-redistributable.unitypackage` を出力する。
3. 次を実行する。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Prepare-BoothRelease.ps1 `
  -UnityPackage Build/StargazingHill-redistributable.unitypackage `
  -Version 1.0.0
```

`Build/BOOTH/StargazingHill-1.0.0-BOOTH.zip` に次を収録する。

- `StargazingHill-1.0.0.unitypackage`
- 5言語の `README.md`
- `LICENSE.txt`
- `NOTICE.md`
- `SHA256SUMS.txt`

scriptはunitypackageのpathname境界を先に検査し、YamaPlayer、QvPen、UnyStylus、ローカルbake inputが混入したpackageを拒否する。

## 販売開始前の完了条件

- `Confirmed`: 2026-08-14に現在worktreeから `0.1.0-draft` のunitypackageとBOOTH ZIPを生成し、pathname境界とSHA-256を検査した。販売version確定時に同じ手順で再生成する。
- `Pending Evidence`: clean Unity 2022.3.22f1 projectへimportし、VPM依存と購入済みUnyStylus復元後に保存Scene validationを通す。
- `Pending Evidence`: Windows / Android / iOSの保存Scene、UdonSharp、Shader、プレイヤー、描画機能を確認する。
- `Open`: 商品名、version、価格、サポート範囲、更新方針、返金条件、商品ページ文面を確定する。
