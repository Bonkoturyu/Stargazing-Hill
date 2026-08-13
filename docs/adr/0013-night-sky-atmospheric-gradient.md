# ADR-0013: 常夜の暗さを保つ地平線gradientで空気遠近を加える

- 状態: Accepted
- 決定日: 2026-08-13

## Context

従来の背景はほぼ一様な黒で、地面端と空の境界が硬く、星の明るさは保てても現実の夜空にある低空の空気層が感じられなかった。PCだけでなくQuest / AndroidとiOSでも同じ構図を成立させる必要がある。

参照実装 `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae` は、天頂と地平線の別色、地面側の暗色、遠景色を用いて純黒の断絶を避けている。2026-08-13のClaude Opus 5設計レビューでも、星のcontrastを保つため天頂を現状の暗さに固定し、青は低空へ限定する案が推奨された。

## Decision

- textureを追加せず、`StargazingHill/NightSkyGradient` skyboxで天頂・地平線・地面の三色を補間する。
- 初期値は天頂 `(0.006, 0.009, 0.020)`、地平線 `(0.020, 0.050, 0.095)`、地面 `(0.004, 0.006, 0.012)`、falloff `3.0`、ground fade `0.12` とする。
- 低bit深度でのbandingを抑える微小ditherを入れる。星・月・流星は従来のtransparent / additive描画を上に重ねる。
- `AmbientMode.Flat` と同系色のlinear fogを使用し、±250mへ延長した地面の遠端を夜空へ溶かす。
- Shader Model 3.0、single-pass stereo macro、texture sampleなしとし、PC / Android / iOS共通Materialを使う。

## Consequences

- 地平線に控えめな青みが生まれ、地面端と純黒背景の境界が目立ちにくくなる。
- 天頂色を明るくしないため、恒星のcontrastと常夜の印象を維持できる。
- mobile実機では黒階調、banding、fog距離、GPU時間を継続確認する。

## Evidence

- 参照: `Assets/BonkotuWorld/Materials/Night/Shaders/BkW_NightSky.shader`、`Assets/BonkotuWorld/Editor/NightSkyBaker.cs`（上記commit、確認日2026-08-13）
- Unity 2022.3.22f1 Direct3D 11で説明パネル・デバッグパネル背景を1280×720描画し、星のcontrastと低空の青みを目視確認（2026-08-13）
