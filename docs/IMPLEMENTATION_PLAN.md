# 実装計画

状態: `In Progress`

## Phase 0: 基盤

- [x] Unity / VRChat SDK / UdonSharpの版を固定する（Unity 2022.3.22f1、VRChat SDK 3.10.4）。
- [x] HYG、Poly Haven、YamaPlayer、QvPen、UnyStylusの依存・権利記録を整える。
- [ ] 空の基準シーンでPC・Android・iOS向けBuild & Test経路を確認する。

完了条件: package lock、権利記録、各プラットフォームの既知制約が追跡できる。

## Phase 1: 草原MVP

- [x] CC0素材による芝生地面、単一Colliderの丘、Poly Haven Jacaranda一本木、固定照明を作成する。
- [x] 動画プレイヤーとしてYamaPlayer 2.0.0-beta.7を採用する。
- [x] YamaPlayer、QvPen 3.3.15、購入済みUnyStylus v1.3をスポーン背後の設備エリアへ集約する。
- [x] ジャンプ・歩行速度を明示し、スポーンと丘の歩行面を静的検証する。
- [ ] Quest/iOSで基準フレーム時間とメモリを測定する。

完了条件: 星空なしでも安全に滞在でき、全対象プラットフォームで基本動作する。

## Phase 2: 実在星空と月

- [x] 星表のライセンスと変換工程を確定する。
- [x] 全天球Star Mesh BakerとShaderを実装する。
- [x] 東京の現在時刻による天球回転を実装する。
- [ ] 月の位置計算と表示を実装する。

完了条件: 基準日時の期待方位と見た目を許容誤差内で再現し、星1個ごとのランタイム更新がない。

## Phase 3: 流星

- [x] 決定的な毎時イベント、Late Join経過再現、任意デバッグ発火を実装する。
- [ ] 散在流星とペルセウス座流星群を実装する。
- [ ] 残り10群をデータ追加で展開する。

完了条件: 同じ時刻のクライアントでイベントが一致し、途中参加で過去の流星を再生しない。

## Phase 4: 品質・公開

- [ ] PC・Quest(Android)・iOSで機能、負荷、UI、音声を確認する。
- [ ] VRChat SDK警告、権利表示、第三者通知を解消する。
- [ ] 公開候補ビルドの結果を `TEST_PLAN.md` に記録する。

完了条件: `PROJECT_SPEC.md` のMVP項目と公開チェックがすべて完了している。
