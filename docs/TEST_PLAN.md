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
| CC0素材の同一性 | Pass | Poly Haven grass 2点、Jacaranda派生Mesh、Jacaranda texture 7点のSHA-256を `Tools/Validate-StargazingImplementation.py` で確認。Unity YAML MeshはLF/CRLFを正規化してWindows checkoutでも一致 |
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

## 2026-08-11 木の軸・設備配置の再修正結果

- 発端: Unity Sceneで一本木が横倒しになり、YamaPlayer・QvPen・UnyStylusが分散している実画面が報告された
- 原因: FBXのlocal Z-upをUnity Y-upへ変換するimport済みルート回転を、Scene生成時に上書きしていた

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 報告時の木 | Fail | 幹が水平方向を向いているUnity実画面を確認 |
| FBX座標系 | Pass | Mesh local Z-up、import root X=270° / scale=100をUnityログで確認 |
| 木の直立 | Pass | import TransformをModel子に保持し、外側Anchorで接地。Modelのlocal Zがworld Yと1°以内で一致 |
| 木の正面・側面描画 | Pass | Direct3D 1280x720を2方向から生成し、幹の根元が丘へ接地し樹冠が上にあることを目視確認 |
| 設備エリア | Pass | スポーンから主景観と逆方向3m以上、中心から7m以内にYamaPlayer・QvPen・UnyStylusがあることを検証。振り返りpreviewで3設備を目視確認 |
| PipelineManager | Pass | VRCSceneDescriptorと同一GameObjectに1個存在することをScene再読込後に検証 |
| VRChat Client実画面 | Open | 修正Sceneを開き直し、木と3設備の最終配置を再確認する |

## 2026-08-11 Poly Haven Jacaranda置換

- 要求: 一本木をCC0のPoly Haven `Jacaranda Tree`へ置き換え、利用者が調整したRespawn・YamaPlayer・QvPen・UnyStylus配置を維持する
- 権利証拠: 公式asset page / license / files API、authors、取得日、原本FBX MD5・SHA-256、派生工程を隣接 `NOTICE.md`へ記録

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 原本FBX | Pass | API記載MD5一致。132,437,628 bytes、Unity展開後3,863,832 trianglesのため追跡せず `.gitignore` 対象 |
| 軽量化境界 | Pass | 三角形単位ではなく連結部品単位で枝7%・幹100%・葉6%を決定的に選択し、葉や枝の途中切断なし |
| 派生Mesh | Pass | 288,899 vertices、465,580 triangles（枝88,864 / 幹230,112 / 葉146,604）、3 submesh、約39MB |
| テクスチャ | Pass | 枝・幹・葉の1K diffuse / normalと葉alpha、計7ファイルのSHA-256を検証対象化 |
| 軸・接地 | Pass | FBX root軸・単位をMeshへベイク。Model Y-up、高さ7.74m、Mesh実幅9.67m、丘中央接地をScene生成時と再読込後に検証 |
| 確定配置 | Pass | Respawn `(-2.78, 0.40, -20.80)`、YamaPlayer `(-4, 1.813, -24)`、QvPen `(-7.6, 0.848461, -22.454)`、UnyStylus `(-8.668, 0.858, -20.672)` と各回転を0.001m / 0.01°以内で検証 |
| 正面・側面描画 | Pass | Direct3D 1280x720を2方向から生成し、複数幹・樹冠・alpha・丘への接地を目視確認 |
| 実機性能 | Open | PC / Quest / iOSでGPU時間・メモリを測定し、必要なら遠距離LODを追加する |

## 2026-08-12 星空移動・毎時流星デバッグ試験

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 同一時刻の天球 | Pass | 同じUTC入力のQuaternion差0.0001°以下 |
| +1時間の天球移動 | Pass | 2026-08-11 12:00→13:00 UTCで15.0411°回転 |
| +24時間の恒星日差 | Pass | 同UTC時刻の翌日との差0.9852°。太陽日24時間で完全一致しないことを確認 |
| 毎時Event ID | Pass | `year/month/day/hour`から連続hourで異なるID `18094356` / `18094357` を生成 |
| 決定的パラメータ | Pass | 同一Event ID / wave / slot / channelのsample一致、次hourで不一致 |
| 任意発火入口 | Pass | Play Modeメニューと `DebugTriggerHourlyEvent()` を実装。UdonSharp 91 scripts変換、Scene生成・再読込検証Pass |
| 流星描画 | Pass | 4 Quad pool、25秒、5秒waveを実装。任意発火と同じ経路の2.4秒地点をDirect3D描画し、複数の加算発光軌跡を目視確認 |
| VRChat Client | Open | 毎時00分、途中参加、任意発火、星+1時間/resetをBuild & Testで確認する |

## 2026-08-12 TreeSelectionTempビルド阻害の回帰試験

- 報告: VRChat Build & Testで `TreeCandidateRenderer.cs` の `AssetDatabase` CS0103が9件発生し、AssetBundle・World build・UdonSharp scene upgradeが連鎖失敗
- 原因: 木の比較用EditorスクリプトをGitでは無視していたが、Unityがコンパイルする `Assets/TreeSelectionTemp` に残していた

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 一時比較物の除去 | Pass | `Assets/TreeSelectionTemp` と対応 `.meta` を削除。完成Scene・Prefabからの参照なし |
| 再発防止 | Pass | 同パスを `.gitignore` 対象から外し、静的検証で存在をFailにする。今後はUnity非管理の `Temp/TreeSelectionTemp` を使用 |
| C# / UdonSharp / Scene生成 | Pass | キャッシュ更新後のUnity clean runで `TreeCandidateRenderer=0`、`error CS=0`、compiler error=0、exception=0。World生成と保存Scene再読込検証も終了コード0 |
| VRChat SDK AssetBundle / Build & Test | Pass | SDK公開APIのWorld build経路でWindows `.vrcw` を2回生成。2回目は `TreeCandidateRenderer=0`、`error CS=0`、compiler error=0、Udon `ArgumentNullException=0`、build failure=0。初回のUdon Prefab参照再生成時だけ例外が発生し、再実行では解消 |

## 2026-08-12 観測地・月・IMO主要11流星群

- 対象ブランチ: `agent/complete-sky-mvp-before-device-tests`
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4
- 外部基準: USNO Celestial Navigation API、IMO Meteor Shower Calendar 2026 Table 5（いずれも確認日2026-08-12）

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| ObservatoryProfile | Pass | Tokyo 35.68 / 139.76をasset化し、星・月・流星へ同一profileを複製。San Francisco、Rome、Moscow、Torontoを含む5地点で天の北極高度=観測緯度を確認 |
| 月のtopocentric位置 | Pass | 主要月摂動と扁平地球上の観測者視差を実装。2025年の東京5基準日時でUSNOとの差は高度最大0.0394°、方位最大0.0495°、合格閾値0.10°。UnityとPython CIの両方で検証 |
| 主要11流星群catalog | Pass | IMO 2026 Table 5のID、活動開始・終了、極大日、代表放射点、ZHRを全件比較。年跨ぎのしぶんぎ座流星群も境界試験 |
| 放射点と活動群選択 | Pass | 共通赤道座標変換、日付活動カーブ × 放射点高度 × ZHR、ペルセウス座極大時の選択をUnity試験で確認 |
| 静的回帰検証 | Pass | `python Tools/Validate-StargazingImplementation.py`。12,495星、月5基準、11群、観測地asset、依存とCC0 hashを検証 |
| Unity生成・UdonSharp | Pass | `BuildForBatchMode` 終了コード0、C# error 0、Udon error 0、exception 0。明示的Udon compile後に新規fieldをSceneへ保存 |
| Unity天文試験 | Pass | +1h=15.0411°、+24h residual=0.9852°、USNO月5件、5観測地、11 IMO群、hour Event IDの決定性を確認 |
| VRChat SDK Windows bundle | Pass | SDK 3.10.4公開World Builder APIで `.vrcw` を生成。C# error 0、exception 0、build failure 0 |
| Libraryなしcloneからの復元 | Pass | ローカルclone、固定VPM package、購入済みUnyStylusのみから新規Libraryを構築。初回import完了後にUnityを再起動し、Scene生成、C#、Udon、参照検証がPass。初回importと同時のbatch実行ではYamaPlayer extensionの一時的な二重登録が出たため復元手順へ再起動を明記 |
| ClientSim / PC / Quest / iOS | Open | 利用者方針により後続。途中参加・複数人同期はClientSimで確認する |
