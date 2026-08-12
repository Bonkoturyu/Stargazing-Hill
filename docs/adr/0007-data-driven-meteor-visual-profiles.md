# ADR 0007: 流星の速度と光度階級をデータ駆動で描画する

- 状態: Accepted
- 決定日: 2026-08-12

## Context

初期実装は全流星が同じ青白い線で、強制プレビュー時だけ幅2.4倍・長さ1.6倍にしていた。そのため通常流星、明るい流星、火球の差がなく、実写の流星群より均一な発光棒に見えた。また、11群をcatalog化していても群ごとの速度差は描画へ反映されていなかった。

## Decision

- `MeteorShowerCatalog` にIMO 2026 Table 5の対地速度 `V∞` と光度分布指標 `r` を追加する。
- `V∞` を共通式へ渡し、速い群ほど表示時間を短く、移動距離と尾を長くする。群ごとのコード分岐は作らない。
- `r` と決定的sampleから Normal / Bright / Fireball の3階級を選ぶ。`r` から各階級確率への変換は天文観測値そのものではなく、低い `r` ほど明るい流星の比率を増やす演出上の近似とする。
- 3階級は同じtextureless Additive Quad shaderを使い、Materialだけで白い核、先頭フレア、先細りの尾、残光の強さを変える。
- 強制プレビューも自然発生と同じ寸法・速度式を使う。開始時の1本だけFireball階級へ固定して3段階表現を確実に検査できるようにし、残り19本は同じ階級式を使う。デバッグ専用の太さ・長さ補正は行わない。
- タイムラプス映像の長時間露光や同時多発本数は再現せず、リアルタイム映像の継続時間と明るさ分布を優先する。

## Consequences

- 追加流星群は `V∞` と `r` を含むcatalog行の追加で同じ描画式を利用できる。
- 同一hour Event IDは全クライアントで同じ階級、速度、長さを再計算でき、同期変数を追加しない。
- 4 Quad / 3 Materialのままで、大量Particle、Light、texture samplingを追加しない。
- `r` から火球確率への対応は `Provisional` であり、実機の見え方と将来の観測資料に基づいて調整できる。

## Evidence

- IMO Meteor Shower Calendar 2026 Table 5: https://imo.net/files/meteor-shower/cal2026.pdf （確認日2026-08-12、`V∞` と `r`）
- ペルセウス座流星群の火球: https://www.youtube.com/shorts/YmOyKSUKrZs （確認日2026-08-12、白い核・明るい先頭・長い尾の視覚参照）
- しし座流星群タイムラプス: https://www.youtube.com/shorts/hyvBPI7uZ2g （確認日2026-08-12、放射点へ収束する細い軌跡の構図参照。露光時間と本数は非採用）
- しぶんぎ座流星群リアルタイム映像: https://www.youtube.com/watch?v=qFr3UIn1tew （確認日2026-08-12、継続時間と明暗分布の参照）
- ふたご座流星群高感度映像: https://www.youtube.com/watch?v=Zfeqavtui7U （確認日2026-08-12、群内の見え方のばらつき参照。高感度由来の明るさは直接転写しない）
