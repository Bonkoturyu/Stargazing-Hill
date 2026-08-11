# リスクと未解決事項

確認日: 2026-08-11

| 状態 | 項目 | 解決条件 |
|---|---|---|
| Confirmed | HYG星表と派生Meshの配布条件 | v4.1、原本commit/hash、CC BY-SA 4.0、加工工程と表示を記録済み |
| Provisional | YamaPlayer 2.0.0-beta.7 | 採用・依存固定・距離減衰実装済み。PC/Android/iOS実機検証はOpen |
| Provisional | QvPen 3.3.15 | 公式VPM依存として導入済み。上流packageに明示ライセンスファイルがないため、package本体は追跡せず公式配布から復元する |
| Provisional | UnyStylus v1.3 | 購入済みVN3素材としてローカル導入。本体は再配布せず、clone後の購入済みpackage再導入手順と実機動作を確認する |
| Open | QvPenとUnyStylusの併設負荷 | PC/Android/iOSで描画、同期、UI、メモリを測定し、必要なら片方をプラットフォーム別に無効化する |
| Open | iOSで利用可能なShaderと動画経路 | 対象Unity/SDK版でBuild & Test |
| Open | 天文計算の許容誤差 | 基準値と見た目上の合格基準を定義 |
| Open | 流星群データの一次出典 | URL、確認日、年次変化の扱いを記録 |
| Provisional | Quest/iOS性能予算 | 基準シーンのProfilerと実機測定から確定 |
| Open | 月相表示をMVPに含めるか | 視覚効果、負荷、工数を比較して決定 |
