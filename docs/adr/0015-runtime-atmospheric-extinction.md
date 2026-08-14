# ADR-0015: 回転する天球へ実行時の大気消散を適用する

- 状態: Accepted
- 決定日: 2026-08-14

## Context

星Meshは肉眼限界の調整として`mag <= 6.8`を採用しているが、従来のShaderは地平線fadeだけで、低空ほど長い空気層を通って暗くなる大気消散を計算していなかった。そのため、背景には低空の青い空気感がある一方、星と流星の輝度変化が一致していなかった。

参照実装 `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae` の `NightStarMeshBaker` は、肉眼限界`6.8等級`、消散係数`0.23 mag/airmass`、`airmass ≈ 1 / sin(altitude)`、最低正弦`0.05`を使用する。ただし参照側は特定時刻の地平線上だけを生成するため、消散をEditor bakeできる。本ワールドは現在時刻に合わせて全天球Mesh自体を回転するため、生成時の高度へ焼くと時刻変化に追従できない。

## Options

- 消散なしで従来の地平線fadeだけを使う: 最軽量だが、低空の背景と光源の見え方が一致しない。
- 生成時に参照式をvertex colorへ焼く: runtime負荷は増えないが、回転する全天球では誤った星が暗いままになる。
- Shaderで現在の高度から消散を求める: 頂点計算が増えるが、星と流星の現在位置へ正しく適用できる。

## Decision

- `StargazingAtmosphere.cginc`へ共通式を置き、星と流星のvertex shaderから利用する。
- $X=1/\max(\sin altitude,0.05)$、$A=0.23(X-1)$、$T=10^{-0.4A}$を採用する。Shaderでは同値な`exp2(-1.32877124 * A)`を使う。
- 星の肉眼限界は従来通りEditorで`mag <= 6.8`を選別する。runtimeでは透過率を掛け、低空の暗い星が知覚上見えにくくなるようにする。
- 星は約15°、流星は約12°まで別途smoothstepで地平線fadeする。
- 流星は肉眼限界で切らず、Normal / Bright / Fireballの全発光要素へ同じ透過率を掛ける。天頂での既存profileは変更しない。

## Consequences

- 現在時刻で回転する天球でも、低空に来た星がその場で暗くなる。
- 背景gradient、fog、恒星、流星が同じ「低空ほど空気の影響が強い」という見た目になる。
- texture sample、追加Renderer、Udon同期は増えない。星・流星とも消散はvertex段階で計算する。
- 簡易airmass式は地平線直近の厳密な大気モデルではないため、最低正弦で安定化する。PC / Android / iOS実機で低空の可読性とGPU時間を確認する。

## Evidence

- 参照: https://github.com/Bonkoturyu/VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH （commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae`、`Assets/BonkotuWorld/Editor/NightStarMeshBaker.cs`、確認日2026-08-14）
- 現行実装確認: `Starfield.shader`は従来smoothstepだけで消散式なし、`BuildStarMesh()`は`mag <= 6.8`を選別済み（2026-08-14）
