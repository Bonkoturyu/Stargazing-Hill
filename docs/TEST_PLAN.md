# テスト計画

状態: `Provisional`

## 自動・静的確認

- JSON / YAML / TOML / UdonSharpの形式確認
- Unityアセットと `.meta` の対応、GUIDの意図しない変更
- 禁止生成物と大容量ファイルの混入
- 星表変換の件数、座標範囲、等級・色の境界値
- 天文計算の既知日時に対する基準値
- 毎時イベントSeedとLate Join経過時間の決定性

## Editor / ClientSim

- シーン参照切れ、Udon compile error、Console error
- Station、動画、QvPen、World-space UI、2D/3D音声
- Owner/非Owner/途中参加での状態適用
- 時刻境界、日付境界、流星群活動期間境界

## 実機

| 対象 | 必須確認 |
|---|---|
| PC | VR操作、星/月の位置、動画、音声、描画負荷 |
| Quest / Android | フレーム時間、メモリ、Shader、透明描画、UI可読性 |
| iOS | 対応Shader、入力、動画/音声、メモリ、パッケージ制約 |

## 結果記録

テストごとに日付、commit、Unity/SDK版、端末、手順、期待値、結果、証拠、残課題を記録する。未実施は成功とせず `Open` とする。
