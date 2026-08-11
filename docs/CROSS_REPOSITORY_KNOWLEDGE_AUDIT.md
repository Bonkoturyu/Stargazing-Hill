# Cross-Repository Knowledge Audit

確認日: 2026-08-11

## 調査対象

| リポジトリ | 確認commit | 主な参照領域 |
|---|---|---|
| `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` | `11a52acb00a9579178ee678a6d5d1ba1f327eb7e` | VRChat運用、星空実装、ADR、AI設定 |
| `Dungeon-Architect-AI-Preproduction` | `749759bd398858035fd1e8d196b6b398f225c843` | 状態ラベル、証拠管理、文書ルーティング、Skills |
| `Pocket-Dragon-Dungeon` | `7346ced89b7da7d62ca4a8fdc05e45f2fce69d12` | AI協業、ライセンス証拠、仕様書構成 |
| `BYTE_DUEL_BAD_SECTOR` | `929518e9ed586cbfdb0fd27114506e8dcc109b40` | 番号付き仕様、検証ラダー、横断監査 |
| `TimerUtility` | `c272a1d50af3f680dfa3f86534bd231dff87712b` | 親/SubAgent責任分界、委譲パケット、ADR |

## 採用

- `Confirmed`: ルート規約を短くし、詳細を正本文書へルーティングする。
- `Confirmed`: 状態ラベル、情報源の信頼順位、ADR、試験・権利記録を導入する。
- `Confirmed`: 難タスクを完了条件と検証境界へ分解するSkillを導入する。
- `Confirmed`: SubAgentは最大1体、深さ1、親が要件・統合・最終検証を保持する。
- `Confirmed`: 星は極小Quadを一つのMeshへまとめ、Additive Unlitで描画する設計を参考にする。
- `Confirmed`: カタログ座標の全天球Meshと、観測時刻・地点によるランタイム回転を分離する。
- `Provisional`: Udonの時刻差は直接減算せず、SDKが提供する差分計算APIを優先する。実装時に現行SDKで再確認する。
- `Provisional`: 2D AudioSource、Station、World-space Canvas、TMP、モバイルShaderの既知注意点を実装ガイドへ移す。導入時に現行SDKと実機で再確認する。

## 見送り・保留

- `Pending Evidence`: HYGのCSV、派生Mesh、既存ベーカー、Shaderの直接コピー。元データと派生物のライセンス、表示義務、share-alike範囲を確定するまで取り込まない。
- `Out of scope`: Claude CLI委譲Skill。現在のCodex/SubAgent構成で代替でき、外部CLI依存を増やさない。
- `Out of scope`: 他プロジェクト固有のモデル名、個人PC設定、シーン構成、製品固有ルール。
- `Provisional`: 他リポジトリのQuest向け数値目安や古いSDK回避策。現行公式資料と実機測定なしに規則化しない。

## 反映先

採用事項は `AGENTS.md`、`.codex/`、`.agents/skills/`、[AI_COLLABORATION.md](AI_COLLABORATION.md)、[VRCHAT_IMPLEMENTATION_GUIDE.md](VRCHAT_IMPLEMENTATION_GUIDE.md)、`docs/adr/`、`docs/legal/` に反映した。
