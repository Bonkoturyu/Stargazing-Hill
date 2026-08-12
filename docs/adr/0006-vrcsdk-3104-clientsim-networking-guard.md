# ADR 0006: SDK 3.10.4 ClientSimのUdon networking初期化順序を局所guardする

- 状態: Accepted
- 決定日: 2026-08-12

## Context

VRChat Worlds SDK 3.10.4のClientSimでPlay Modeへ入ると、`UdonManager.Initialize()` がScene内Udonを登録・初期化した後、`UdonManager.OnSceneLoaded()` が同じUdonの `IsNetworkingSupported = true` を実行し、初期化後setterを禁止する `InvalidOperationException` が発生した。この場合、対象Udonはnetworking無効のまま初期化され得る。公式VPM package本体はGit追跡外であり、直接編集だけではpackage復元後に失われる。

## Options

- 例外を既知のClientSim警告として無視する: networking無効初期化を残すため不採用。
- `com.vrchat.worlds` 全体をforkまたはリポジトリへ格納する: 差分、更新、配布の負担が大きいため不採用。
- Editor側からSDK 3.10.4の既知断片だけを検証して局所guardを再適用する: 変更範囲と撤去条件を限定できるため採用。

## Decision

`VrcSdk3104ClientSimGuard` をEditor初期化時に実行する。

- package versionが厳密に3.10.4であることを確認する。
- `UdonManager.Initialize()` の初回登録前に、未初期化Udonへnetworkingを有効化する。
- `UdonManager.OnSceneLoaded()` では未初期化時だけ同setterを呼ぶ。
- 上流ソース断片が完全一致しない場合は書き換えず警告する。
- VPM管理package本体はGit追跡せず、guardのソースと検証だけを追跡する。

## Consequences

- ClientSimの単一PlayセッションでUdon VM経由の流星デバッグを、networking初期化例外なしで検証できる。
- VPM restore後にも同じ限定修正が再適用される。
- Editorがpackageソースを一度更新するため、初回起動時に追加のscript compileが発生する。
- SDK更新時は自動適用されない。上流が同等修正を含むか確認し、含む場合はguardと本ADRを撤去する。

## Evidence

- VRChat SDK 3.10.4 release: https://github.com/vrchat/packages/releases/tag/3.10.4 （確認日 2026-08-12）
- 対象版: `Packages/vpm-manifest.json` の `com.vrchat.worlds` 3.10.4
- 回帰試験: `StargazingHill.Editor.ClientSimMeteorDebugVerifier.RunForBatchMode`
