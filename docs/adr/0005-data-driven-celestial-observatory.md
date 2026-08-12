# ADR 0005: 観測地と天文現象をデータ駆動にする

- 状態: Accepted
- 決定日: 2026-08-12

## Context

星、月、流星群が東京固定値や流星群ごとの分岐を個別に持つと、別地点対応や11群の更新で値が食い違う。月は地心位置だけでは最大約1度の視差を残すため、見た目にも無視できない。

## Decision

- 観測地を `ObservatoryProfile`（ID、表示名、緯度、東経）へ集約し、星、月、流星の全系統へ同じ値を渡す。
- 流星群を `MeteorShowerCatalog` の並列配列へ集約し、群ごとのコード分岐を作らない。
- 2026年の主要11群はIMO `Meteor Shower Calendar 2026` Table 5を正本とする。
- 月は主要摂動を含む低コスト軌道近似に、扁平地球上の観測者位置を使ったtopocentric parallaxを適用する。
- 月の合格基準は、東京の年内5基準日時でUSNO Celestial Navigation APIの高度・方位との差が各0.10度以内とする。
- ネットワークUTCから決定的に再計算できるため、天体位置を同期変数にしない。

## Consequences

- 東京以外もprofileの緯度・東経だけで切り替えられる。
- 追加流星群はcatalogへの行追加で実装できる。
- 年次極大時刻や日々の放射点移動は将来catalogを拡張できるが、2026 MVPでは極大日の代表放射点と三角形活動カーブを使う。
- 月相は位置精度と独立しており、初期MVPの対象外とする。

## Evidence

- IMO: https://imo.net/files/meteor-shower/cal2026.pdf （確認日 2026-08-12、2026 calendar、Table 5）
- USNO API: https://aa.usno.navy.mil/data/api （確認日 2026-08-12、Celestial Navigation Data）
