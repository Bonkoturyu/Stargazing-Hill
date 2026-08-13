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
| 任意発火入口 | Pass | Play Modeメニューと `DebugTriggerHourlyEvent()` を実装。UdonSharp 92 scripts変換、Scene生成・再読込検証Pass |
| 流星描画 | Pass | 4 Quad pool、25秒、5秒waveを実装。強制経路の0.75秒地点をDirect3D描画し、加算発光軌跡を目視確認 |
| VRChat Client | Open | 毎時00分、途中参加、任意発火、星+1時間/resetをBuild & Testで確認する |

## 2026-08-12 流星群強制プレビュー試験

| 項目 | 状態 | 証拠 / 判定 |
|---|---|---|
| 主要11群の選択 | Confirmed | `MeteorShowerDebugWindow` が正本catalogの日本語名・IDを列挙し、backing `UdonBehaviour` の入力変数へ選択indexを渡してCustomEventを送る |
| 活動期・高度の無視 | Confirmed | 強制経路は選択indexを直接採用し、自然発生の活動日・放射点高度選択を通らない |
| 20本固定 | Pass | 4本pool × 5wave、`DebugForcedMeteorCount = 20`。静的検査とUnity batch testで確認 |
| 視線正面の保証 | Pass | 各waveのslot 0を開始時のcamera forwardへ配置し、地形回避の最低高度を約13°に設定。寸法は自然発生と同じで、開始時の1本だけFireball階級に固定。Unity batch testで先頭Renderer有効かつ方向dot >= 0.98を確認 |
| Direct3D描画 | Pass | 1280×720画像を生成し、丘と木の上に強制流星の発光軌跡が出ることを目視確認 |
| Udon VM発火経路 | Pass | proxy直接呼出しでは次のUdon Updateに表示を消される不具合を再現。backing `UdonBehaviour.SetProgramVariable` + `SendCustomEvent` へ修正し、ClientSim Play ModeでPERSEIDS、debug active、表示Renderer 1本以上を自動確認 |
| ペルセウス座短縮入口 | Pass | `Stargazing Hill/Debug/Force Perseids Preview (20 Meteors)` がcatalog index 4を起動 |
| 自然発生への非干渉 | Confirmed | 強制indexはローカルdebug状態だけに保持し、通常経路は従来のUTC・活動度・実放射点を使用 |
| ClientSim単一クライアント | Pass | `ClientSimMeteorDebugVerifier` が実Udon VMへ強制イベントを送信し、PERSEIDS、25秒preview active、表示Rendererを確認。SDK 3.10.4のnetworking初期化例外0件 |
| Play Mode目視 | Pending Evidence | 利用者が修正版Game viewでメニュー操作し、ウィンドウの `再生中: PERSEIDS`、5秒ごとのwave、25秒終了を確認する |

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

## 2026-08-12 流星ビジュアルprofile試験

- 対象ブランチ: `agent/improve-meteor-visuals`
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、Direct3D 11
- 外部基準: IMO Meteor Shower Calendar 2026 Table 5と [ADR 0007](adr/0007-data-driven-meteor-visual-profiles.md) 記載の4映像（確認日2026-08-12）

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| IMO速度・光度分布 | Pass | 主要11群の `V∞=[41,49,66,41,59,20,66,27,29,71,35]`、`r=[2.1,2.1,2.4,2.5,2.2,2.6,2.5,2.3,2.3,2.5,2.6]` をcatalog、静的fixture、Sceneへ反映 |
| 共通速度式 | Pass | 20 km/sの表示時間が71 km/sより長くなること、同じEvent IDで結果が決定的になることをUnity試験で確認 |
| 光度階級 | Pass | Normal / Bright / Fireballの境界sampleと3 Material参照を静的検査・Unity試験で確認。確率対応は実機調整前の `Provisional` |
| 強制表示 | Pass | 0.75秒地点で先頭Renderer有効、視線方向dot >= 0.98、先頭だけFireball階級、デバッグ専用寸法倍率なし |
| Direct3D描画 | Pass | 1280×720を描画し、青い均一線ではなく斜めの暖白色軌跡、明るい先頭、先細りの尾を目視確認 |
| UdonSharp | Pass | 92 scripts compile、Material配列とruntime `sharedMaterial` 切替を含めerror 0 |
| ClientSim単一クライアント | Pass | backing Udon VM経由でPERSEIDSを強制し、preview active、表示Renderer 1本、networking初期化例外0件 |
| 保存シーン非破壊更新 | Pass | `UpgradeMeteorVisualsForBatchMode` でMeteorShowerSystemのcatalog / Material参照だけを更新。Respawn、YamaPlayer、QvPen、UnyStylus配置差分なし |
| PC / Quest / iOS実機 | Open | 明暗、尾の連続性、3階級比率、GPU時間を実機Build & Testで確認する |

## 2026-08-12 Jacaranda樹冠のQuest向けカード化試験

- 要求: Codexが導入したJacaranda一本木を、Questで見えるレベルまで簡易化する。葉の見た目と夜景としての雰囲気も改善する
- 対象: [ADR 0009](adr/0009-quest-frond-card-canopy.md)
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、Direct3D 11

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 葉と枝の接続 | Pass | 茎から葉目標点までの距離が平均0.54、最大2.79 model unit。近接3視点の描画で葉が枝に接していることを確認 |
| 葉の向きと分布 | Pass | 向きは茎から葉目標点へのベクトル。分布は破棄した葉三角形の重心をボクセル集約した4,645点。見上げ・樹冠端・幹接合部で、カーテン状の垂れ下がりも塊への偏りも解消 |
| 幹の近接品質 | Pass | 2,162 trianglesでは樹皮が平たい破片とねじれたリボンに折り畳まれた。6,872へ引き上げ、幹接合部の描画で解消を確認 |
| 樹冠の見た目 | Pass | 連結部品6%選択の葉ジオメトリは向こう側が透ける斑点状だった。4,645枚のフロンドカードで連続した樹冠になったことを遠景・近景・見上げで確認 |
| 三角形数 | Pass | 465,580から19,507へ削減（枝3,345 / 幹6,872 / 葉9,290）。Scene検証で3 submeshと4,000〜26,000 trianglesを機械確認 |
| アセットサイズ | Pass | `Jacaranda_Quest.asset` は2,522,857 bytes。従来の派生Meshは約39MB |
| フロンド寸法 | Pass | 実物の複合葉30〜45cmに対し初回1.43m相当。0.76m相当では個々のカードが平たいシートとして読め、樹冠下端が直線的な棚になり、真横のカードが長い筋として視界を横切った。0.50m相当へ縮め枚数を4,645へ増やして解消 |
| 軸・接地 | Pass | Model Y-up、高さ8.02m、Mesh実幅10.12m、丘中央接地をScene生成時と再読込後に検証。カード樹冠の広がりに合わせ実幅上限を10.5mから12.0mへ更新 |
| 夜景の明度 | Pass | 全面被覆化により白tint・ambient 0.48では昼間的な淡色の塊になった。深緑tintとambient 0.32へ変更し、草地と整合することを描画で確認 |
| ベイクの決定性 | Pass | カード位置・姿勢・frond選択を決定的hashで生成。`0cebaa16…` を静的検証のSHA-256対象に追加 |
| 旧木の削除 | Pass | 利用者判断により旧Meshを削除。`LandmarkTreeLegacy` がSceneに存在しないことをScene検証で機械確認し、38MBの中間Meshを追跡対象から外した |
| 近接描画の常設化 | Pass | 遠景2方向では葉と枝の接続不良、垂れ下がり、幹の破綻をいずれも検出できなかった。`RenderTreeCloseupsForBatchMode` で樹冠端・見上げ・幹接合部の3視点を追加 |
| 樹冠直下の遮蔽 | Open | 真下から見上げると樹冠がほぼ不透明で空が見えない。木の下を鑑賞位置にするなら密度を再検討する。現行のspawn導線では未影響 |
| alpha test overdraw | Open | 19,507 trianglesは幾何としては軽いが、密なalpha test樹冠のoverdrawはtile GPUで別コスト。樹冠を見上げる状態のGPU時間をPC / Quest / iOS実機で測定する |

## 2026-08-13 ローカル検査ランナーの導入

- 要求: 静的CIで検出できない欠陥を、push前にローカルで捕捉する
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4

静的CI（`.github/workflows/static-validation.yml`）はPython検証のみを実行する。この検証はソース文字列とアセットhashを読むだけでUnityを起動しないため、コンパイル不能なスクリプト、U# program assetの欠落、生成Sceneの不備をいずれも検出できない。実際にPR #16 の4件の欠陥は、CIが正常動作していても全て素通りしていた。

`Tools/Run-LocalChecks.ps1` が安く落ちる順に検査を実行する。

| 段階 | 検査 | 検出対象 |
|---|---|---|
| 1 | Python静的検証 | hash・定数の退行 |
| 2 | `StargazingChecks.CheckUdonSharpProgramAssetsForBatchMode` | U# program assetの欠落・未コンパイル |
| 3 | `BuildForBatchMode` | コンパイルエラー、Scene生成の退行 |
| 4 | `ValidateForBatchMode` / `TestSkyAndMeteorForBatchMode` | Scene不変条件、天球・流星の数値退行 |

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 全検査の通過 | Pass | 5検査すべてPass。`-SkipBuild` で段階3を省略可 |
| program asset検査の範囲 | Pass | `Assembly-CSharp` かつ名前空間 `StargazingHill` の5 behaviour（MeteorController / RealSkyController / WorldDebugPanelButton / WorldDebugPanelPickup / WorldPlayerSettings）を対象。YamaPlayer同梱のTAC UI 7 behaviourは自プロジェクト外として除外 |
| 強制プレビューの退行検出 | Pass | 導入直後に `TestSkyAndMeteorForBatchMode` の失敗を検出（`visible=False`）。PR #16 がonset式を `0.35+slot*0.88` から `(slot+0.5)*slotSpacing+jitter` へ変更した結果、slot 0のonsetが0.625±0.28秒となり、強制プレビュー開始点0.75秒より後になる場合に流星が1本も出なかった。強制プレビューのみ旧onset式へ戻し、自然イベントのPR #16 スケジューリングは維持 |
| Test Runner統合 | Open | テスト用asmdefから `Assembly-CSharp-Editor` を参照できないため、EditModeテスト化にはプロジェクトのasmdef分割が要る。BACKLOG `Later` へ記録 |
| CI側でのUnity実行 | Open | ライセンスと実行時間の都合で未導入。実行枠が使えるときは現行のPython検証がCIで走る |

## 2026-08-13 VRデバッグパネルのレイアウト修正と幾何検査

- 要求: 実機でパネルを表示したところ、ラベルが板からはみ出し互いに重なっていた
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4

`TextMesh` の実寸は `characterSize × fontSize ÷ 10` メートルである。`WorldDebugPanelInstaller` は `fontSize = 64` のまま `characterSize` にメートル値を直接渡していたため、全ラベルが6.4倍で描画されていた。既存の検査はいずれもこれを見られない — Python検証はソース文字列を読むだけ、`StargazingChecks` はprogram assetの有無だけ、`ValidateScene` はボタン数と backing Udon behaviour だけを見ていた。

| 欠陥 | 内容 | 発見手段 |
|---|---|---|
| ラベル6.4倍 | `characterSize` にメートル値を直接代入。`METEOR DEBUG` が幅約4.4m（板幅2.35m） | ユーザーのスクリーンショット |
| ボタン重複 | `GEMINIDS`（index 10 → row 5 / column 0）と `STOP` が共に `(-0.57, -0.655)` | 座標の再計算 |
| ラベル鏡文字 | ラベルの `localRotation` が `Euler(0,180,0)`。`TextMesh` は自身の -Z から読めるため、可読面が板の裏側になっていた | バッチレンダリング |
| トグルの遮蔽 | `VRDebugPanelToggle` がパネル座標系 `(-0.39, -0.39, -0.85)`、板の手前かつシルエット内側で `STOP` を完全に隠していた | バッチレンダリング |

`StargazingWorldBuilder.ValidateDebugPanelLayout()` を `ValidateScene` へ追加した。

| 検査 | 方式 |
|---|---|
| ボタン同士の重なり・板からのはみ出し | パネルローカルの矩形演算 |
| ラベルの向き | `dot(label.forward, panel.forward) ≥ 0.99` |
| ラベルが板・ボタン面に収まるか | `Renderer.localBounds` の4隅をパネル空間／ボタン空間へ変換 |
| トグルが板の可読面を塞いでいないか | トグル中心をパネル空間へ射影し、`z < 0` かつ板矩形内なら失敗 |
| 板下端の地面クリアランス | `EvaluateTerrainHeight` と比較し 0.05m 以上 |

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 全検査の通過 | Pass | ローカル5検査すべてPass |
| ラベル実寸の測定 | Pass | ビルド経路では生成メッシュを実測。最も余裕がないのは `REPLAY CURRENT 3 MIN` でボタン面の78% |
| 検証のみ経路のフォールバック | Pass | `TextMesh` は初回描画時にしかメッシュを生成せず、ディスクから読んだSceneを `-nographics` で検証する経路では実測できない。serialized値から `characterSize × fontSize ÷ 10`、字送り0.68で保守的に算出（同条件で `S DELTA AQUARIIDS` 88%）。実測できたかはログに明示する |
| 目視確認 | Pass | `RenderDebugPanelPreviewForBatchMode`（`-nographics` を付けずに実行）で正面・グリッド近接・広角の3枚を出力し、数値だけで判断しない |
| 板下端の地面クリアランス | Pass | 0.35m。広角で下部に重なって見えるのはQvPenパレットの手前遮蔽であり、正面からは干渉しない |
| 実機でのVR可読性・押しやすさ | Open | 描画では確認済みだが、Quest実機での文字可読性とコライダーの押しやすさは未評価 |

## 2026-08-13 流星イベント時間の正本統合

- 要求: 自然イベントの180秒と強制デバッグプレビューの25秒を別々に調整可能にし、コード上の変更箇所を一つへ集約する
- 実装: `MeteorController.NaturalEventDurationSeconds` / `DebugForcedPreviewDurationSeconds` を唯一の調整値とし、BuilderとSceneの旧 `eventDurationSeconds` 複製、migration guard、デバッグUIの時間ハードコードを除去

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 静的検査 | Pass | `python Tools/Validate-StargazingImplementation.py`。時間定数が各1定義で、旧 `eventDurationSeconds` がControllerにないことを確認 |
| Runtime / Editor C# | Pass | Unity 2022.3.22f1の既存Bee response fileと同梱Roslynで `Assembly-CSharp` / `Assembly-CSharp-Editor` をコンパイル |
| Unity Scene生成・UdonSharp変換 | Pass | 通常環境で `Tools/Run-LocalChecks.ps1` を再実行し、program asset確認、Scene生成、Scene検証、天球・流星数値試験の全段階がPass |

## 2026-08-13 再配布用unitypackageと手持ちデバッグパネル

- 要求: YamaPlayer、QvPen、UnyStylusをunitypackageへ同梱せず、デバッグパネルを手持ちサイズ・Pickup対応・ドロップ約10秒後の初期位置復帰にする
- 参考: `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae` のローカルPickup復帰パターン
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、Direct3D 11

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 静的検査 | Pass | 所有root、依存を含めないExport option、外部3パスとベイク原本の除外、10秒定数、ローカルUdon、再取得キャンセル経路を検査 |
| Runtime / Editor C#・UdonSharp | Pass | `WorldDebugPanelPickup.asset` を生成し、5 behaviourのprogram asset検査がPass |
| Scene生成・構造検査 | Pass | Pickup layer、trigger BoxCollider、重力なしRigidbody、VRCPickup、参照、root scale 0.20、約0.47 × 0.41mの寸法を保存Sceneで検査 |
| Scene・天球・流星の退行 | Pass | `Tools/Run-LocalChecks.ps1 -SkipBuild` の全段階がPass。直前のScene再生成もPass |
| パネル描画 | Pass | 正面・近接・周辺配置の3枚を1280 × 720で描画。ラベル欠け・重なり・鏡文字なし、小型化後もボタンを識別可能 |
| unitypackage実物 | Pass | `Build/StargazingHill-redistributable.unitypackage`、11,353,339 bytes、80 pathname。全て `Assets/StargazingHill` 配下で、YamaPlayer / QvPen / UnyStylus / `SourceDownloads` の混入なし |
| 実機Pickup・10秒復帰 | Open | 本ワールドのPCVR / Questで片手保持、別手操作、ドロップ10秒後復帰、待機中再取得によるキャンセルを確認する |
