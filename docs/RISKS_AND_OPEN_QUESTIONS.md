# リスクと未解決事項

確認日: 2026-08-12

| 状態 | 項目 | 解決条件 |
|---|---|---|
| Confirmed | HYG星表と派生Meshの配布条件 | v4.1、原本commit/hash、CC BY-SA 4.0、加工工程と表示を記録済み |
| Provisional | YamaPlayer 2.0.0-beta.7 | 採用・依存固定・距離減衰実装済み。PC/Android/iOS実機検証はOpen |
| Confirmed | クリーンclone初回import順序 | import完了前のbatchビルダー実行ではYamaPlayer extensionが一時的に二重登録される。初回import後にUnityを再起動する復元手順でC# / Udon / Scene生成Pass |
| Provisional | QvPen 3.3.15 | 公式VPM依存として導入済み。上流packageに明示ライセンスファイルがないため、package本体は追跡せず公式配布から復元する |
| Confirmed | UnyStylus v1.3の復元手順 | 購入済みVN3素材としてローカル導入。本体は再配布せず、`SETUP_AND_RESTORE.md` に期待Prefabと手順を記録済み。実機動作は別項目 |
| Open | QvPenとUnyStylusの併設負荷 | PC/Android/iOSで描画、同期、UI、メモリを測定し、必要なら片方をプラットフォーム別に無効化する |
| Open | iOSで利用可能なShaderと動画経路 | 対象Unity/SDK版でBuild & Test |
| Confirmed | 月位置の許容誤差 | USNO APIの東京5日時を基準に、高度・方位とも0.10°以内（実測最大0.0495°）。静的CIとUnity試験でPass |
| Confirmed | 流星群データの一次出典 | IMO Meteor Shower Calendar 2026 Table 5、確認日2026-08-12。主要11群をcatalog化 |
| Provisional | Quest/iOS性能予算 | 基準シーンのProfilerと実機測定から確定 |
| Provisional | Jacaranda一本木の実機描画負荷 | 原本約386万三角形を46.6万三角形へ削減済み。PC / Quest / iOS実機でGPU時間・メモリを測定し、必要なら遠距離LODを追加 |
| Out of scope | 月相表示 | 初期MVPでは位置のみ。将来の視覚効果としてBacklogへ保持 |
