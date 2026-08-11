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

## 2026-08-11 実装時結果

- 対象ブランチ: `agent/build-stargazing-world`（未コミット作業ツリー）
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、YamaPlayer 2.0.0-beta.7

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| HYG派生データ再生成 | Pass | 原本から12,495件を再生成し、checkoutのLF/CRLF差を正規化した追跡CSVのSHA-256が一致 |
| 星表の件数・赤経・赤緯・等級・色指数 | Pass | `python Tools/Validate-StargazingImplementation.py` |
| J2000東京恒星時と天球回転基底 | Pass | 同上。東京LST 60.22061837°、正規直交基底を確認 |
| YamaPlayer依存版と8点距離減衰 | Pass | 同上。0m〜45mの基準点とSpatial Audio設定を静的確認 |
| UdonSharpランタイムC# | Pass | Unity Bee生成の `Assembly-CSharp.rsp` でRoslynコンパイル |
| EditorワールドビルダーC# | Pass | Unity Bee生成の `Assembly-CSharp-Editor.rsp` でRoslynコンパイル |
| Unityシーン生成・Udon変換 | Pass | `StargazingWorldBuilder.BuildForBatchMode`、UdonSharp 50 scripts compile、終了コード0 |
| 保存済みSceneの参照・YamaPlayer設定 | Pass | Scene再読込後、星Mesh、Udon、VRCSceneDescriptor、VideoInfoDownloader、全AudioSource/VRCSpatialAudioSourceを検証 |
| Direct3Dカメラ描画 | Pass | 1280x720 previewを生成し、星空、草原、小丘、一本木、固定照明を目視確認 |
| ClientSim / PC / Quest / iOS | Open | シーン生成後に実施 |

残課題としてClientSimおよび実機でYamaPlayer再生・同期と、距離0/7/14/19/21/28/29.5/45mの聴感を確認する。

## 2026-08-11 草原・移動・描画機能の修正結果

- 対象ブランチ: `agent/fix-world-playability`
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、QvPen 3.3.15、UnyStylus 1.3
- 発端: VRChat Build & Testで単色地面、簡素な木、ジャンプ不可、丘での埋まり・登坂不可、空Playlist警告が報告された

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| CC0素材の同一性 | Pass | Poly Haven grass 2点とQuaternius Tree_3のSHA-256を `Tools/Validate-StargazingImplementation.py` で確認 |
| 草原表現 | Pass | 1K diffuse / normal地表と、3.5〜9.5cm・9,000株・108,000 verticesの単一立体草Meshを生成 |
| 丘の歩行面 | Pass | 草原と丘を1つのMeshColliderへ統合。頂上高2.15m以上、斜面28°以下、重複ColliderなしをRaycast検証 |
| スポーンと移動設定 | Pass | 地表+0.4mのスポーン、歩行2、走行4、横移動2、ジャンプ3.2、重力1.0をScene再読込後に検証 |
| CC0一本木 | Pass | FBX単位・上下軸・接地を補正し、高さ7〜9m、丘中央、幹専用CapsuleColliderを検証 |
| QvPen / UnyStylus | Pass | 両PrefabのScene参照を検証。購入品UnyStylus本体はGit追跡外 |
| 空Playlist警告 | Pass | 空のYamaPlayer PlaylistManagerを生成時に除去し、Unity build logに `No playlists found` なし |
| Unityシーン生成・Udon変換 | Pass | `StargazingWorldBuilder.BuildForBatchMode` 終了コード0、C# error/warningなし |
| Direct3Dカメラ描画 | Pass | 1280x720 previewでCC0草地、立体草、緩斜面、接地した樹冠・幹、星空を目視確認 |
| VRChat ClientSim / PC / Quest / iOS | Open | 実クライアントでジャンプ、斜面登坂、両ペンの描画・同期・負荷を再確認する |

静的検証とEditor描画は合格したが、VRChatアバターControllerでの最終合格は実クライアント再試験後に更新する。
