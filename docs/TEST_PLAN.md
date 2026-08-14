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
| ペルセウス座短縮入口 | Pass | `Stargazing Hill/Preview & Debug/Force Perseids Preview (20 Meteors)` がcatalog index 4を起動 |
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
| program asset検査の範囲 | Pass | `Assembly-CSharp` かつ名前空間 `StargazingHill` の8 behaviour（MeteorController / RealSkyController / WorldDebugPanelButton / WorldDebugPanelPickup / WorldDebugPanelStatus / WorldInfoLanguageToggle / WorldPlayerSettings / WorldPresenceBoard）を対象。YamaPlayer同梱のTAC UI 7 behaviourは自プロジェクト外として除外 |
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

## 2026-08-13 YamaPlayer AutoPlayのClientSim参照修正

- 発端: ClientSim起動時に `[YamaStream] Controller is not set in module AutoPlay` が1件発生し、自動再生が開始されなかった
- 原因: YamaPlayerのSDK build hookはbuild時にmoduleへControllerを注入するが、保存Sceneから直接起動するClientSimより後段である。独自Playlist同期は再生モード・遅延・曲番号だけを保存し、基底moduleの`_controller`を保存していなかった
- 修正: Playlist同期時にAutoPlayのserialized `_controller`へ親Controllerを設定し、UdonSharp backingへコピーする。World Builderは最初のScene保存後のhookへ委ねず、保存前にPlaylist同期・proxy/backing参照検査を行う

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 静的検査 | Pass | Controller serialized property、proxy-to-Udon copy、Builder内の保存前同期、proxy/backing参照検査が存在することを確認 |
| C# / UdonSharp / Scene生成 | Pass | Unity 2022.3.22f1で`BuildForBatchMode`を実行。AutoPlay Controller検査とScene生成がPass |
| 保存Scene再読込 | Pass | `Tools/Run-LocalChecks.ps1 -SkipBuild`のprogram asset、Scene、天球・流星検査が全てPass。AutoPlay proxyの`_controller`とbacking Udonのserialized public variableが同じControllerを参照 |
| ClientSim再実行 | Pass | ユーザー提供画面でClientSimがInitializedまで到達し、`Controller is not set in module AutoPlay`の再発なしを確認。別途、YamaPlayer Editor更新確認の`Thread was being aborted`が発生したが、VCC `settings.json`は読取・JSON解析とも正常でありワールド実行とは別件 |

## 2026-08-13 YamaPlayer Editor更新確認の再適用可能パッチ

- 発端: ClientSim開始時、YamaPlayer 2.0.0-beta.7がEditor起動時に開始したVPM更新確認とPlay Mode移行が競合し、正常なVCC `settings.json`に対して`Thread was being aborted`を赤エラーとして出した
- 修正: `PackageManager`の起動時`CheckUpdate().Forget()`登録だけを除去する版限定patchを保存し、適用・確認・復元を`Tools/Apply-YamaPlayerPatches.ps1`へ集約した

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 対象版・上流確認 | Pass | 公式VPMとGitHub releaseの公開最新版は2.0.0-beta.7。developのrelease後3 commitに対象Editorファイル変更なし |
| パッチ適用器 | Pass | 2.0.0-beta.7で適用済み確認、復元、未適用検出、再適用、二重適用の各経路を実行。未対応版・ソース不一致は書換え前に停止する実装を静的確認 |
| 追跡対象の静的検査 | Pass | `python Tools/Validate-StargazingImplementation.py`と`git diff --check`がPass。版対応、対象ファイル、削除断片、停止条件を検査 |
| Unity batch回帰 | Pass | Unity 2022.3.22f1で局所feature upgrade、UdonSharp 97 scripts compile、保存Scene検証、説明パネル・デバッグパネル描画が終了コード0 |
| ClientSim再確認 | Open | Unity再compile後にClientSimへ入り、VCC settings / `CheckUpdate failed`の赤エラーが再発しないことを確認する |

## 2026-08-13 Quest Playlist欠落・ワールド品質更新

- 発端: QuestへuploadしたWorldでもYamaPlayerのリストが空で、AutoPlayが開始されなかった。併せて流星状態、STOPボタン、地面端、入退室表示、日英説明、mobile shader error、夜空の空気感を改善する
- 参考: `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae`。夜空gradientはClaude Opus 5の2026-08-13設計レビューも使用
- 環境: Unity 2022.3.22f1、VRChat SDK 3.10.4、YamaPlayer 2.0.0-beta.7、UnyStylus v1.3、Direct3D 11

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| runtime Playlist保存 | Pass | 保存Sceneに4 `PlaylistItem`、4 runtime `Playlist`、合計15 trackを保持。`_playlistName` / `_videoPlayerTypes` / `_titles` / `_urls`をUdon backingへcopyし、空のtree spacerをPlaylist名として扱わない |
| YamaPlayer build hook互換 | Pass | `PlaylistBuildProcess`が既存runtime `Playlist`を再利用する版限定patchを追加。2対象ファイルでRestore→Apply→Checkを実行し、版・元断片・修正断片の停止条件を確認 |
| AutoPlay保存参照 | Pass | Controller proxy / backing参照とFromPlaylist設定を保存Sceneで検証。実際のQuest自動再生は新upload待ち |
| 流星debug状態 | Pass | EVENT状態、群ID、visible数を0.2秒ごとに表示。`PLAY CURRENT` / `STOP EVENT`を動的切替し、自然イベント停止hourの再発火を抑止 |
| 説明・在室パネル | Pass | 日本語初期表示、英語ローカル切替、現在人数、直近7件のローカル入退室履歴を実装。パネル本体Colliderなし、言語ボタンのみtrigger。1280×720正面描画で左右反転を検出・修正後、全項目を目視確認 |
| 草原範囲 | Pass | 中央±40mの詳細格子を維持し、粗い連続地形を±250mへ延長。立体草9,000→15,000株。Scene validationで地面boundsと草vertex数を確認 |
| 夜空の空気遠近 | Pass | 三色gradient skybox、Flat ambient、linear fogを実装。Direct3D 11のpanel背景描画で暗い天頂、淡い青の地平線、星contrastを目視確認 |
| UnyStylus GLES3 patch | Pass | 2 shaderの各`UNITY_TRANSFER_FOG`をblock scope化。v1.3のoriginal / patched SHA-256を照合し、Restore→Apply→Checkを実行。Restore時のUTF-8 BOM復元も原本hashで確認。Unity再import logに`unityFogFactor` shader errorなし |
| mobile Quality | Pass | Android / iPhoneの既定QualityをVRC Mobileへ変更し、pixel lightを1へ制限。AudioLink / AVPro / QvPenの未使用field warningは第三者package由来で機能errorではない |
| C# / UdonSharp / Scene | Pass | feature upgradeでUdonSharp 97 scripts compile、保存Scene validation、終了コード0。Python静的検証と2種patch checkもPass |
| Debug panel描画 | Pass | 正面・近接・広角の3枚を1280×720描画。状態表示、11群、単一再生／停止ボタン、sky操作に欠け・重なり・鏡文字なし |
| Quest / Android再upload | Open | 新しいbuildで4リスト15曲、AutoPlay、UnyStylus線、説明UI、地面端、夜空階調を実機確認する |
| iOS再upload | Open | GLES3/Metal shader、説明UI、動画/音声、夜空階調を実機確認する |

## 2026-08-13 説明パネルのちらつき・定員表示修正

- 発端: VRChat実機で言語切替ボタンがちらつき、人数表示が旧値32のままだった
- 原因: ボタン表面とパネル表面が同じ深度 `-0.020m` でz-fightingしていた。最大人数はSDK upload panelのremote metadataであり、Udon runtime APIは現在人数だけを公開する

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| z-fighting修正 | Pass | 言語ボタン表面をパネルより2mm手前へ移動し、Unity正面描画で欠け・重なりなし。VRChat実機のちらつき再確認はOpen |
| 現在人数 | Confirmed | `VRCPlayerApi.GetPlayerCount()`でinstance内の実人数を更新 |
| 最大・推奨人数 | Confirmed | 表示用正本を最大80・推奨40へ更新。Udonからupload metadataを直接取得できないためbuild時にserialized値として保存 |

## 2026-08-13 PCVR Playlist・在室履歴UI再修正

- 発端: PCVR実機でYamaPlayerのPlaylistが0件のままAutoPlayも開始せず、説明板のPC/Mobileアイコンが重なり、履歴の並びと上下ボタン操作が要件に合わなかった
- 原因: PlaylistManagerがYamaPlayer root直下、Controllerがその兄弟だったため、Controllerの子探索に含まれていなかった。YamaPlayer build後処理はControllerをrootへ切り離すので実機でも関係が回復しなかった

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| Playlist階層 | Confirmed | 4 Playlist / 15 TrackをController子階層へ移動し、build後のController切離しへ追従する構造へ変更。Scene validationでも全runtime PlaylistがController子であることを要求 |
| AutoPlay参照 | Confirmed | AutoPlay proxy / backingのController参照、FromPlaylist設定、Controller子のruntime Playlistを保存Sceneへ焼き込み |
| 履歴順序・容量 | Confirmed | 最大40件を古い順に保持し、最新を最下段へ追加。新規イベント受信後は1 frame後に最下段へ追従 |
| 履歴操作 | Confirmed | 旧上下ボタンをSceneから除去し、`VRCUiShape`付きWorld Space ScrollRectと選択可能な縦スクロールバーへ変更。表示高は約20件分 |
| PC/Mobileアイコン | Confirmed | LaptopとSmartphoneの中心間隔を0.10mから0.14mへ広げ、両アイコンも縮小 |
| Unity compile / 保存Scene | Confirmed | Unity 2022.3.22f1 batchmodeでUdonSharp program asset再生成と対象Scene保存が終了コード0 |
| 情報板描画 | Confirmed | 1280×720正面描画でアイコンの非重複、旧上下ボタンの撤去、縦スクロールバー表示を目視確認 |
| ScrollRect UI material | Confirmed | 実機Editorで`WorldInfoButton`の`Unlit/Color`に`_MainTex`がないエラーを確認。ScrollbarのImageを`VRChat/Mobile/Worlds/Supersampled UI`材へ変更し、UI Imageが要求するtexture propertyを保持 |
| ClientSim操作 | Pending Evidence | スクロールバーの選択・ドラッグ、最新下端追従、4 Playlist表示、AutoPlayを確認する |
| PCVR upload実機 | Pending Evidence | 新しいWindows buildで4 Playlist / 15 Track表示とAutoPlay開始を確認する |
| 入退室履歴 | Pending Evidence | ローカル保持40件、表示約20件へ拡張。選択可能な縦ScrollRectとスクロールバーで操作する。40件充足時の可読性とスクロール操作をVRChat実機で確認する |
| PC / Mobile内訳 | Pending Evidence | 各local clientがUnity platform defineで自己判定し、PlayerDataで同期。Laptop / Smartphoneアイコン付きでPC / Mobile / WAITINGを集計。PC・Android・iOS混在実機で再確認する |
| ローカル自動検証 | Pass | `Tools/Run-LocalChecks.ps1 -SkipBuild`: static validation、UdonSharp program assets、Scene validation、sky/meteor testsの全項目Pass |
| デバッグパネル日英切替 | Pass | 既定日本語。タイトル、説明、動的状態、11群、再生/停止、sky操作を右上ボタンでローカル英語切替。日本語3視点と英語正面の1280×720描画で欠け・重なりなし。VRChat実機操作はOpen |

## 2026-08-13 説明パネル右側レイアウト調整

- 発端: 手作業で説明パネル全体を調整した後、右上の言語切替ボタンが届きにくく、人数・履歴欄が他の本文と区別しづらかった
- 保存方針: 手作業済みのパネルroot位置・回転・本文調整は保持し、右側UIのlocal座標だけを変更する

| 対象 | 状態 | 結果 / 残確認 |
|---|---|---|
| 言語切替 | Confirmed | ボタンを右下へ移動し、高さを少し抑えてパネル面内へ収めた |
| 人数・履歴 | Confirmed | 表示内容・ScrollRect高610 px（約20件）を維持したまま、人数・PC/Mobileアイコン・履歴を0.33 m上へ移動した |
| Scene静的確認 | Pass | 手作業済みpanel rootを変更せず、対象Transform / RectTransformのみ更新。生成コードにも同じ座標を反映 |
| Unity正面描画 | Pending Evidence | batchmode用Editor licenseがこの実行環境で有効化されておらず未実施。Unity Editorで正面表示とInteract到達性を確認する |

## 2026-08-13 木陰のピクニックスポット追加

- 要求: Tiny Treats `Pleasant Picnic 1.0` の指定された青い敷物、ラジオ、ティーセット、青系クッションを一本木の下へ配置する
- 環境: Unity 2022.3.22f1、Direct3D 11

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 権利・出所 | Confirmed | Godot Asset Libraryと上流repository commit `da50c97a056fe1513413343787f2526ea7f25174`を確認。上流`LICENSE.txt`、9ファイルのSHA-256、取得日を隣接`NOTICE.md`へ記録 |
| ポリゴン予算 | Pass | 上流6種OBJ合計2,360 triangles。複製したクッション・枕と地形追従敷物を含むScene最終合計3,028 trianglesで、追加削減なし |
| Unity import | Pass | 6 FBXをanimation / camera / light / collider / embedded materialなし、Medium mesh compressionで取込。2 textureはmipmap・圧縮あり、最大512 px |
| Scene構造 | Pass | `World/Environment/PicnicSpot`直下に8品を配置。Colliderなし、mobile-compatible shader、realtime shadow / light probe / reflection probeなしをUnity検証 |
| 地形接地 | Pass | 約4m角の敷物を頂点単位で丘の高さへ追従させ、全頂点の地表クリアランスと7小物の個別接地範囲をUnityで数値検証 |
| 指定配置 | Pass | 頂上側左上に坂下向きラジオ、その付近にティーセット、右側に柄クッション2つ、反対側に無地枕2つを配置。正面・反対側・低い横視点の3 previewで接地と並びを目視確認 |
| unitypackage境界 | Pass | 自然配置修正版を含む12,600,585 bytes、111 pathnameで再生成。Tiny Treatsの選定10 pathname（license / noticeを含む）と地形追従Meshを収録し、YamaPlayer / QvPen / UnyStylus pathnameは0件 |
| PC / Android / iOS実機 | Pending Evidence | 共通Scene・shader・materialを各platform buildで確認する |

## 2026-08-14 ピクニック配置の自然さ・木との干渉修正

- 発端: 約4m角へ拡大した敷物の上端が木へ入り、クッション・枕が立ったまま不自然に見えた
- Opus 5設計レビュー: `claude-opus-5`で成功。木からの半径3m配置、赤・緑・橙の各指定領域、非対称offset、寝かせ回転の合成順を採用

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 木との離隔 | Pass | 敷物中心を木から約2.02mから約3.00mへ移動。敷物外周と木中心の水平距離0.85m以上をUnity validationで要求 |
| ラジオ・ティーセット | Pass | ラジオを以前と逆向きにし、頂上側左上の赤指定領域へティーポット・マグと集約。ラジオ正面と坂下方向のdot product 0.9以上を検証 |
| 柄クッション | Pass | 右上の緑指定領域へ2つ配置。90度寝かせ、異なるyawと±5度以内のsettle pitch / rollで非対称化 |
| 無地枕 | Pass | 下側の橙指定領域へ2つ配置。90度寝かせ、異なるyawと±5度以内のsettle pitch / rollで非対称化 |
| 接地・干渉 | Pass | 敷物の地形追従、全小物の地表clearance、4つの寝具の上方向を数値検証。3方向のUnity previewで木との非交差、重なりなし、平置きを目視確認 |
| Opus 5最終画像レビュー | Pending Evidence | 修正版3画像を再投入したが、21ターン処理後にsession limit。2026-08-14 05:30 JST reset。別モデルへ切替・再試行はせず、最終自然さの判定のみ保留 |

## 2026-08-14 公開用生成配置・メニュー・clone手順の整備

- 発端: Unity上で手作業確定したピクニック配置を生成コードへ戻し、GitHubからcloneした利用者にも保存Scene・再生成・unitypackage出力を利用可能にする
- 正本: `Assets/StargazingHill/Editor/Data/PicnicLayout.json`、設計判断は[ADR 0014](adr/0014-versioned-generated-scene-layout.md)

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 手作業配置のcapture | Confirmed | 保存Scene YAMLから8 Anchorのworld Transformと各Prefab `Model`子のlocal Transformをversioned JSONへ反映。今後はUnityメニューから同じschemaへ保存可能 |
| 生成処理 | Confirmed | 全Scene build、generated feature upgrade、Picnic単体rebuildが同じJSONを読み、再生成後Transformとの一致をUnity validationで要求 |
| メニュー整理 | Confirmed | World / Layout / Integrations / Preview & Debug / Export / Diagnosticsへ分類。全Scene buildとPicnic置換には確認dialogを追加 |
| clone手順 | Confirmed | 通常利用は依存復元後に保存Sceneを開き、全再生成を必須にしない。生成・capture・検証・配布package手順をREADMEとSETUP_AND_RESTOREへ記録 |
| unitypackage境界 | Confirmed | Export前に保存Scene検証を実行し、Scene・復元ガイドに加えてlayout JSONの収録を必須化。外部3依存は引き続き除外 |
| 静的回帰 | Pass | `python Tools/Validate-StargazingImplementation.py`でlayout schema、8品、有限値、Quaternion正規化、Installer・Exporter接続を検証 |
| Unity compile / 再生成比較 | Pending Evidence | batchmode起動時にUnity Licensing Clientの署名検証Code 10で停止。Sceneは事前退避し、変更なし。ライセンス復旧後にcompile、単体rebuild、保存Scene validationを実行する |

## 2026-08-14 公開README多言語化・星空実装ガイド

- 要求: GitHubの入口を日本語・英語・繁体字・簡体字・韓国語へ対応し、星空の作り方を図解・仕組み・計算方式込みの別文書へ分離する

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 多言語README | Confirmed | `README.md`、`README.en.md`、`README.zh-Hant.md`、`README.zh-Hans.md`、`README.ko.md`を作成。全ファイル先頭から5言語へ相互移動可能 |
| 公開利用手順 | Confirmed | 各言語へ概要、実装状態、clone後の依存復元、保存Scene検証、用途別Unityメニュー、再配布境界を記載 |
| 星空の簡易説明 | Confirmed | HYG→Editor bake→1 Mesh→UTC回転→Shader描画をMermaid flowchartで説明 |
| 星空の詳細説明 | Confirmed | 赤経・赤緯の単位方向、接線Quad、等級curve、B−V色、Julian Date、GMST/LST、天球Quaternion、Shader強度式を実装値と照合して記載 |
| runtime図解 | Confirmed | Editor/runtime責任分離図とNetwork UTCからGPU描画までのsequence diagramを記載 |
| 文書静的検証 | Pass | 5 READMEの存在・相互言語link・検証／Export menu、ガイド必須章・図・式・実装語、全local Markdown linkを検査 |
| GitHub描画確認 | Pending Evidence | PR作成後にMermaid 2図、数式、CJK文字、言語リンクのWeb描画を確認する |

## 2026-08-14 恒星・流星の大気消散

- 発端: 星Meshは肉眼限界`mag <= 6.8`を採用済みだが、現行Shaderは地平線fadeだけで、空気を通る距離による輝度低下を計算していなかった
- 参照: `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` commit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae` の`NightStarMeshBaker.cs`（確認日2026-08-14）

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 参照値 | Confirmed | 肉眼限界6.8等級、消散係数0.23 mag/airmass、`1 / sin(altitude)`、最低正弦0.05を確認 |
| 回転天球への適用 | Confirmed | 固定高度へ焼かず、現在のworld高度からvertex shaderで毎描画時に評価。15秒ごとの天球回転へ追従 |
| 恒星 | Confirmed | 既存の6.8等級選別と15°地平線fadeを維持し、大気透過率を追加 |
| 流星 | Confirmed | Normal / Bright / Fireballの尾・核・先頭フレア・残光へ同じ透過率を追加。12°地平線fade、天頂の既存profileは維持 |
| 共通式 | Pass | `StargazingAtmosphere.cginc`を両Shaderがincludeし、静的検証で定数・式・Material設定を確認 |
| 文書 | Pass | 中高生向け簡易説明、空気感の4層、airmass・減光量・透過率、流星への適用をガイドと正本へ記載 |
| Unity compile / Scene描画 | Pending Evidence | batchmode環境のUnity Licensing Client署名検証Code 10が未解消。Unity EditorでShader compile後、高度90°/30°/10°の星と流星を比較する |
| PC / Android / iOS実機 | Pending Evidence | 低空の暗星密度、流星の視認性、地平線fade、GPU時間を各platformで確認する |

## 2026-08-14 Global観測地点セレクター

- 要求: 説明パネルの観測地点だけを全員共通にし、急な切替を許容して `(global)` と明示する
- 正本: [ADR 0016](adr/0016-global-observatory-selector.md)、地点一覧は[REAL_SKY_SYSTEM.md](REAL_SKY_SYSTEM.md)

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| Global設計 | Confirmed | `WorldObservatorySelector`をManual Syncとし、整数`selectedIndex`だけを同期。Ownership取得、即時適用、serialization、deserialization適用を実装 |
| 地点catalog | Confirmed | 初期Tokyoを含む指定20地点をID・表示名・緯度・東経の同長配列で一元化。星・月と流星へ同じ値を渡す |
| 操作UI | Confirmed | `◀` / 選択文字 / `▶`、押下時に20件を縦展開する直接選択リスト、面一のローカルDebug ON/OFFを生成コードへ追加 |
| Global表記 | Pass | 見出しとInteractionに`(global)`を含め、選択名には重複表示しない静的検査を追加 |
| 既存UI不変条件 | Pass | ONLINEと履歴を維持し、Laptop / Smartphone iconは最新の手調整座標をUnity Scene validationの厳密値として固定。言語切替は下端操作列へ移動 |
| 静的回帰 | Pass | `python Tools/Validate-StargazingImplementation.py`でManual Sync、20地点、Global表記、操作経路、既存UI座標、生成・検証入口を確認 |
| Unity compile / Scene再生成 | Pass | Unity 2022.3.22f1でUdonSharp 100 scriptsをcompileし、selector / button program assetと説明パネルを保存Sceneへ生成。説明パネル専用Scene validationがPass |
| 正面描画 | Pass | 通常状態と20件展開状態を1280×720で描画。下端のGlobal地点切替・Debugボタン、既存在室欄、言語ボタンに重なりがなく、全20行を確認 |
| 全Scene validation | Pending Evidence | 今回と無関係な手作業配置 `PicnicCushionBlueLeft` が生成用接地閾値を3.11cm超え、説明パネル検証より前に停止。手作業配置は変更せず、説明パネル専用検証で今回範囲を分離確認 |
| ClientSim複数人 | Pending Evidence | 2クライアントで前後・直接選択、同時操作のLast-writer-wins、途中参加復元、星・月・流星の同地点化を確認する |
| PC / Android / iOS実機 | Pending Evidence | 20件リストの到達性、文字サイズ、Quest / mobile描画負荷、Global同期を確認する |

## 2026-08-14 説明・Debugパネルの手作業配置取り込み

- 要求: 保存Sceneで手調整した説明パネルと端末アイコンの座標を生成値へ戻し、観測地点説明、ボタン列、Debugパネル初期位置を整理する
- 状態: 生成コード、保存Scene、静的検査、正面描画までConfirmed

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 手調整値のcapture | Confirmed | 説明パネル`(1.471, 1.999, -26.29)` / Y `202.2865°`、Laptop / Smartphone iconの`x=0.79`、`y=0.584 / 0.456`を保存Sceneから取得し生成定数へ反映 |
| Global表記 | Pass | 見出しとInteractionだけへ`(global)`を残し、選択名は`Tokyo, Japan`のように地点名だけを表示。見出し横へ「星空の基準地点 / SKY VIEWPOINT」を追加 |
| 下端操作列 | Pass | 言語切替を左`x=1.03`、Debugを右`x=1.72`へ配置。幅`0.56 / 0.72m`、間隔`0.05m`を生成値とScene validationで固定 |
| 旧Debugトグル | Pass | 独立root `VRDebugPanelToggle`の新規生成を廃止。既存Sceneでは移行時に削除し、説明パネル内Debugボタンへ一本化 |
| Debugパネル初期位置 | Confirmed | 説明パネル右隣の`(-0.842, 1.849, -25.342)` / Y `202.2865°`へ移動。Pickupと10秒復帰は維持 |
| clone / 旧Scene移行 | Confirmed | 当初は`[InitializeOnLoad]`で旧Sceneだけを再生成したが、公開後の手編集を暗黙に上書きし得るため、後続の公開準備で明示メニューへ変更 |
| 静的回帰 | Pass | `python Tools/Validate-StargazingImplementation.py`、`git diff --check` |
| Unity compile / Scene再生成 | Pass | batchmode専用認証は失敗したため通常Editorを非表示起動。UdonSharp 100 scripts compile、Debug→Informationの順で再生成、Scene保存が成功 |
| Scene検証・正面描画 | Pass | 説明パネル専用validation後、通常状態と20地点展開状態を1280×720で描画。端末アイコン、説明、地点名、言語・Debugボタンの分離を確認 |
| PCVR / Quest操作 | Pending Evidence | 2ボタンの押し分け、Debug表示位置、Pickup後10秒復帰、観測地点Global同期を実機確認する |

## 2026-08-14 観測地点UI再構成と5言語化

- 要求: 観測地点見出しの重複をなくし、一覧を逆順3列タイルへ変更する。説明・Debugを日本語、英語、繁体字、簡体字、韓国語へ対応し、Debugパネルを低くする

| 確認項目 | 状態 | 証拠 |
|---|---|---|
| 同期互換性 | Confirmed | catalog配列のTokyo=0からSeoul=19は不変。UI生成だけ `catalogIndex = count - 1 - visualIndex` とし、各ボタンへ元indexを設定 |
| 観測地点見出し | Confirmed | `OBSERVATORY` / `SKY VIEW POINT`を廃止し、中央揃えの「星空の基準地点 (global)」へ一本化 |
| 一覧レイアウト | Confirmed | 20地点を3列7行、SeoulからTokyoの視覚順で上方向へ展開。Scene validationはLocation_19の左上とLocation_00の最下段位置を検査 |
| 説明本文 | Confirmed | 日本語から「東京の」、英語から`in Tokyo`を除去。日本語初期表示で5言語をローカル循環切替 |
| Debug表示 | Confirmed | 5言語のタイトル、説明、動的状態、11群名、再生/停止、sky操作を生成し、初期Yを`1.45`へ変更 |
| 多言語フォント | Confirmed | 公式Noto Sans CJK KR RegularをOFL-1.1で同梱。SHA-256を静的検査と第三者素材台帳で固定 |
| 静的検査 | Pass | `Validate-StargazingImplementation.py`で地点catalog、逆順index割当、3列式、5言語参照、Noto SHA-256を確認。`git diff --check`もPass |
| Unity compile / Scene再生成 | Pass | Unity 2022.3.22f1でUdonSharp 100 scripts compile後、Debug→Informationを再生成。Noto fontと`.meta`をimportしScene保存成功 |
| 説明パネル描画 | Pass | 通常・3列一覧・英語・繁体字・簡体字・韓国語を1280×720描画。中央見出しのUpperCenter pivotずれを検出・修正し、文字欠け・一覧重なりなしを目視確認 |
| Debugパネル描画 | Pass | Debug固有検証で15ボタン、板下端の地上高1.24m、手持ちサイズ0.47×0.41mを確認。日本語・英語・繁体字・簡体字・韓国語を描画し文字欠け・重なりなし |
| 全Scene validation | Pass | 手作業配置を正本へcaptureし、柔らかい寝具用の接地検査へ分離した後に再実行。Picnic 3,028 trianglesを含む保存Scene全体のvalidationがPass |
| PCVR / Quest / iOS操作 | Pending Evidence | 5言語切替、3列一覧、Global同期、Debug表示位置を実機で確認する |

## 2026-08-14 Picnicクッション手調整の生成取り込み

- 発端: 利用者がUnity Scene上で柄クッション2点を敷物へ少し沈め、より自然に見える最終配置へ調整した
- 正本: `Assets/StargazingHill/Editor/Data/PicnicLayout.json`

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| Sceneから配置正本へのcapture | Confirmed | 既存Editor capture経路で8 Anchorと各`Model`子Transformを保存。柄クッション左はlocal position `(0.030, -0.031, -0.187)`、右は`(-0.599, -0.999, -0.423)` |
| 接地検査の意図分離 | Confirmed | ラジオ・ティーセットの硬い小物は従来の8〜18cmを維持。クッション・枕は敷物へ軽く埋める意図を`AllowBlanketEmbedding`で明示し、回転済みモデルの外接Boundsを地形基準-8〜12cmで検査 |
| JSONからの再生成 | Pass | Unity 2022.3.22f1で`InstallForBatchMode`を実行。8品を再生成し、Anchor / `Model`子Transform一致、Colliderなし、3,028 triangles、接地範囲を確認 |
| 描画確認 | Pass | 再生成後の正面・反対側・低位置プレビューを通常Editor相当のD3D11で描画。柄クッション2点が手調整時と同じ沈み方を保ち、木・地形への抜けや再浮上がないことを目視確認 |

## 2026-08-14 観測地点名の5言語化

- 要求: 選択中の星空基準地点と展開一覧の地点名を、説明パネルの表示言語へ合わせて翻訳する
- 非対象: `selectedIndex`、20地点の順序・緯度・経度、Global同期方式、星・月・流星の計算

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 翻訳catalog | Confirmed | 日本語・英語・繁体字・簡体字・韓国語を各20件用意。静的検査で全配列長とTokyo / Washington D.C. / Seoulの代表表記を確認 |
| Global / Local分離 | Confirmed | Global同期は従来どおり整数`selectedIndex`だけ。`WorldInfoLanguageToggle`のローカル言語indexをselectorへ渡し、選択中ラベルと20タイルのTextだけを更新 |
| UdonSharp / Scene生成 | Pass | Unity 2022.3.22f1で100 scriptsをcompileし、説明パネルを再生成。5言語配列、20 Text参照、言語トグル参照を含む専用validationがPass |
| 5言語描画 | Pass | 通常Editor相当のD3D11で各言語の通常状態と20地点展開状態を計10枚描画。全地点が切り替わり、3列内の文字欠け・重なりなしを目視確認 |
| 全Scene validation | Pass | Picnic、Debugパネルを含む保存Scene全体のvalidationがPass |
| ClientSim / PCVR / Quest / iOS操作 | Pending Evidence | 実際の言語ボタン操作、一覧を開いたままの言語変更、異言語クライアント間で同じGlobal地点が維持されることを確認する |

## 2026-08-14 InformationSystem公開準備とメニュー安全化

- 要求: 「星空の基準地点 (global)」見出しの5言語化、InformationSystemの編集しやすい階層化、第三者が誤操作しにくいUnityメニュー構成
- 状態: 実装・静的検査を実施。Unity再生成・UdonSharp compile・描画はこの節の実行結果へ追記する

| 確認 | 結果 | 証拠・残課題 |
|---|---|---|
| 見出し翻訳 | Confirmed | 日本語 / English / 繁體中文 / 简体中文 / 한국어の5文言をversioned配列化し、本文と同じローカル言語indexで更新。Global同期値には影響しない |
| InformationSystem階層 | Confirmed | `Visual/{Descriptions,Presence}` と `Controls/Observatory` に目的別整理。各group Transformはidentityで既存表示座標を維持 |
| 暗黙の上書き防止 | Confirmed | `WorldInformationPanelInstaller`の`[InitializeOnLoad]`自動再生成を廃止。全置換は`Advanced/Generated Content/Rebuild InformationSystem (Replaces Children)...`の確認dialog経由だけに限定 |
| 通常操作導線 | Confirmed | rootへ保存Scene検証、`Content`へ選択・対象検証・保存済み配置適用、`Build & Export`へ配布package、`Advanced`へ生成物置換と診断を分離 |
| 編集手順 | Confirmed | `SETUP_AND_RESTORE.md`へ階層図、編集境界、自動置換しない方針、再生成時の注意を記録 |
| 静的検査 | Pass | `python Tools/Validate-StargazingImplementation.py`と`git diff --check`がPass。旧メニュー名と旧フラット階層参照が0件 |
| Unity compile / Scene再生成 / 検証 | Pass | 通常Editor相当経路でUdonSharp 100 scripts compile、InformationSystem単体再生成・保存、専用validation、保存Scene全体validationがすべて終了コード0 |
| 5言語描画 | Pass | D3D11で日本語・英語・繁体字・簡体字・韓国語の通常状態を描画し、見出しが各言語へ切り替わり既存の中央位置に収まることを目視確認 |

## 2026-08-14 YamaPlayer標準Playlist Editorへの一本化

- 要求: YamaPlayer本体に標準Playlist Editorがあるため、Stargazing Hill独自のPlaylist編集・同期経路を廃止する
- 正本: 保存Scene内のYamaPlayer標準`PlaylistItem`

| 確認項目 | 状態 | 証拠・残課題 |
|---|---|---|
| 独自経路の撤去 | Confirmed | `music_list.txt`、独自sync class、AssetPostprocessor、scene save hook、`Stargazing Hill/Integrations/YamaPlayer/Sync Playlist Config`を削除 |
| 標準編集経路 | Confirmed | Inspectorの「プレイリストを編集する」または`YamaPlayer/Edit Playlist`だけを公開手順へ記載 |
| ClientSim反映 | Confirmed | 2.0.0-beta.7版限定patchで標準Editor保存後に`PlaylistBuildProcess`を実行し、runtime PlaylistをUdon backingへcopy |
| 全Scene再生成 | Confirmed | 保存Sceneの標準エディター編集済みYamaPlayerを新Sceneへ複製し、独自データ形式から再構築しない |
| patch往復 | Pass | 2.0.0-beta.7 packageへRestore→Apply→Checkを実行し、3対象ファイルの版限定変換が往復可能なことを確認 |
| 静的・Unity検証 | Pass | Python検証と`git diff --check`がPass。UnityでC#再compile後、保存Scene全体validationがPass |
| 全Scene再生成時の保持 | Pass | 保存を伴わないadditive Scene検証で、標準Editor編集済みYamaPlayerを複製後も4 Playlist / 15 TrackとAutoPlayのController参照を保持 |
| ClientSim / 実機再生 | Pending Evidence | 標準EditorでPlaylist変更後、ClientSimとPC / Android / iOS buildでリスト・AutoPlayを確認する |

## 2026-08-14 公開説明・5言語README・現行文書同期

- 要求: 更新済みのVRChat SDK Descriptionとワールド内説明パネルの意味を揃え、GitHubの公開入口を5言語化し、現行仕様を示す文書を最新実装へ同期する
- 境界: ADRと過去の試験節は当時の判断・証拠として保持し、現在仕様を示す正本、README、引き継ぎ文書だけを更新する

| 確認項目 | 状態 | 証拠・残課題 |
|---|---|---|
| 説明パネル本文 | Confirmed | 日本語、英語、繁体字、簡体字、韓国語を「現実の時刻」「全員で共有する観測地点」「毎時00分の流星」「主な利用方法」「PC / Android / iOS」の同じ意味へ統一。生成コードと保存Sceneの両方を更新 |
| GitHub公開入口 | Confirmed | ルートREADME 5言語を20地点Global選択、大気消散、ピクニック、標準YamaPlayer Playlist経路、5言語UI、未完了の実機検証まで同じ現行状態へ同期 |
| 配布package案内 | Confirmed | `Assets/StargazingHill/README_UNITYPACKAGE.md`を5言語化し、外部3依存の除外、復元、標準Playlist編集、再生成不要、配置captureを各言語で案内 |
| 正本文書 | Confirmed | `PROJECT_SPEC`、`REAL_SKY_SYSTEM`、`VRCHAT_IMPLEMENTATION_GUIDE`、`IMPLEMENTATION_PLAN`から現行仕様として残っていた東京固定・HYG未確認記述を更新。`WORLD_DESCRIPTION`を文書索引へ追加 |
| 引き継ぎ | Confirmed | `HANDOFF.md`を星空、大気、20地点Global同期、月、流星、5言語UI、ピクニック、依存境界、残検証の現在地へ全面更新 |
| 静的検査 | Pass | `python Tools/Validate-StargazingImplementation.py`が12,495星、5言語README、Global観測地点、配布境界を含む全項目でPass。52 Markdownファイルの相対link確認と`git diff --check`もPass |
| Unity compile / 描画 | Pending Evidence | 利用者のUnity Editorが開いているため別Editorは起動しない。script再compile後、5言語のパネル表示と改行を通常Editorまたは実機で確認する |

## 2026-08-14 Public公開前監査とRelease ZIP自動化

- 要求: Public化前に秘密情報・第三者依存・Blueprint IDを監査し、再配布用unitypackageをZIP化してGitHub Releaseへ出す仕組みを用意する
- 当初制約: GitHub ActionsはBudget/Billing制限中だった。2026-08-15にremote runner起動を確認し、この制約は解消済み

| 確認項目 | 状態 | 証拠・残課題 |
|---|---|---|
| 現在ツリー秘密情報 | Pass | 秘密鍵、主要token、webhook、credential代入、個人home絶対pathを検出せず |
| 到達Git履歴 | Pass | 同じ秘密pattern、過去path、author mailを監査。author mailはGitHub noreplyのみ。YamaPlayer / QvPen / UnyStylus package本体の履歴混入なし |
| Blueprint ID | Confirmed | 保存Sceneにworld ID 1件。VRChat公式資料上、別owner/無効IDはSDKがclearする。clone利用者はDetachまたはclear確認が必要 |
| 権利境界 | Confirmed | root MITの例外を`NOTICE.md`へ明示し、VRChat VPM resolverをDistro License対象として依存記録へ追加。外部3packageはRelease対象外 |
| workflow静的検査 | Pass | `python Tools/Validate-StargazingImplementation.py`、Python compile、YAML load、`git diff --check`がPass。Actionはcommit SHA固定 |
| Release workflow実行 | Pending Evidence | `main`上の`v*` tagで実行し、Release ZIPとSHA-256を確認する |
| clean import | Pending Evidence | Release ZIP内のunitypackageをclean Unity 2022.3.22f1 projectへimportし、依存復元後に保存Scene validationを実行する |

## 2026-08-14 Public化とBOOTH配布準備

- 要求: 最終監査に問題がなければrepositoryをPublic化し、BOOTH販売用unitypackageの準備を開始する
- 制約: remote Release workflowとCodeQLの初回実行証拠は後日取得する。Actions runner自体は2026-08-15に稼働確認済み

| 確認項目 | 状態 | 証拠・残課題 |
|---|---|---|
| GitHub visibility | Pass | GitHub APIで `public` / `private: false` を確認 |
| remote範囲 | Pass | branchは `main` のみ、tag 0件、Secrets/Variables 0件、失敗Actions run 0件 |
| main保護 | Pass | PR、branch削除禁止、non-fast-forward禁止、squash merge rulesetを `active` 化 |
| GitHub security | Pass | Dependabot security updates、Secret scanning、Push protectionを有効化。open Dependabot alert 0件 |
| packageライセンス | Confirmed | `Assets/StargazingHill` 内へMIT本文と自己完結したNOTICEを追加し、Exporterとvalidatorで収録必須化 |
| BOOTH ZIP組立 | Confirmed | `Tools/Prepare-BoothRelease.ps1`で検査済みunitypackage、5言語README、LICENSE、NOTICE、SHA-256をZIP化する手順を実装 |
| 最新unitypackage生成 | Pass | Unity 2022.3.22f1 batch exportで26,135,648 bytes、118 pathnameを生成。`Validate-UnityPackage.py`がowned root、必須license/NOTICE、外部依存除外を確認 |
| BOOTH draft ZIP | Pass | `StargazingHill-0.1.0-draft-BOOTH.zip`、25,443,648 bytes。unitypackage、5言語README、LICENSE、NOTICE、SHA256SUMSの5fileを収録。package SHA-256 `0e5ec9b9ea58c24fd21f9de572e1d01f1ddd195464666f59d6c3ddad0880afed` を再計算して一致 |
| BOOTH clean import | Pending Evidence | 顧客向けZIP内のunitypackageをclean projectへimportし、VPM依存と購入済みUnyStylus復元後に確認する |
| Static validation Action | Pass | repositoryのfull-SHA必須方針に合わせ、`actions/checkout`と`actions/setup-python`を公式v6 tagが指すcommit SHAへ固定。PR #28のpush run `31823686804` とpull request run `31823690056` がともにPass |
