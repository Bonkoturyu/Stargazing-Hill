# Handoff

更新日: 2026-08-20

## 現在地

- 仕様の正本は `docs/PROJECT_SPEC.md` と `docs/REAL_SKY_SYSTEM.md`。公開入口は5言語のルートREADME、VRChat SDK用Descriptionは `docs/WORLD_DESCRIPTION.md` を正本とする。
- HYG v4.1の12,495星を1 Meshへベイクし、肉眼限界6.8等級、地平線の空気遠近、`0.23 mag/airmass`の大気消散を実装済み。
- VRChatの共通時刻とGlobal同期された22観測地点から、星空、topocentric月位置、流星群放射点を各クライアントで決定的に再現する。Tokyo=0〜Seoul=19を維持し、鳥取=20・松江/島根=21を末尾追加した。タイル表示だけは日本の地点を札幌から沖縄まで北→南へまとめ、Tokyo=0の既定選択は維持する。
- IMO 2026主要11群と散在流星、毎時180秒イベント、25秒・20本のローカルDebugプレビューを実装済み。
- ±250mの草原、単一Colliderの小丘、Poly HavenのCC0一本木、versioned layoutで再生成できるTiny TreatsのCC0ピクニックスポットを実装済み。敷物だけに静的MeshColliderを追加する生成コードへ更新した。
- 説明パネルは日本語初期表示で5言語対応。Global観測地点、現在人数、PC/Mobile内訳、ローカル入退室履歴、Debug表示切替を備え、直接USEとVRレーザーを併用する。Debugパネルも5言語、Pickup、ドロップ10秒後復帰に対応する。
- 木陰のローカル設定ボード、5方向ミラー、ナイトモード、日時・アラーム、YamaPlayerラジオSpeaker・専用音量、任意PlayerData保存を生成コードへ実装した。ボードは約0.62 × 0.57m、正面向き、proximity 0.35mの上端グリップ、ObjectSyncなしのローカルPickup、2列UI。ラジオ本体のUSE Trigger・状態表示は廃止し、設定ボードのローカルON/OFFが追加Speakerを直接切り替える。木の歯車は手調整後Transformを生成正本とし、小さい見た目のままworld約0.30m角の操作面を維持する。
- 2026-08-16（続き）、実機で「触れない・ビームが出ない・写真に写らない」の3症状が単一原因だと判明した。VRChat公式SDKの `ClientSimInteractiveLayerProvider` は、メニューを閉じている間の操作対象レイヤーを `~(1 << UI_LAYER) & ...` で組み立てる。**UIレイヤー(5)は通常プレイ中の操作対象から外れ、ワールドカメラも写さない。** ワールドUIのCanvasをすべてDefaultレイヤー(0)へ移し、Canvas同寸のtrigger Colliderを必ず持たせる `ConfigureWorldUiCanvas` へ集約した。ラベルがTextMeshのデバッグパネルには不可視の `UiBeamTarget` Canvasを生成し、3パネルすべてにビームが出るようにした。再生成後の保存Sceneで `VRCUiShape` を持つCanvas 65個すべてが適合、非適合0件。詳細は [ADR 0018](docs/adr/0018-settings-board-usability-fixes.md)。
- 入退室の通知音と頭部追従トーストを `WorldPresenceNotifier` として追加した（[ADR 0019](docs/adr/0019-local-presence-notifications.md)）。音は生成2音チャイムのローカル2D、表示は前方1.5m・視線下0.42m・1行5秒・最大3行の `PlayerLocal` レイヤー。音と表示は設定ボードから別々にON/OFFでき、PlayerDataへ任意保存する。自分の入室時に既存プレイヤー分が一斉に鳴らないよう、ローカルプレイヤー自身の `OnPlayerJoined` を受け取るまで通知しない。
- 2026-08-16、実機で使えなかった4点を [ADR 0018](docs/adr/0018-settings-board-usability-fixes.md) として修正した。(1) ラジオ音量はYamaPlayerマスター（既定0.1）への倍率をやめ、追加Speakerだけに掛かる絶対ローカル音量にした。初期ON / 85%、Near 1.5m / Far 22m。MuteだけYamaPlayerへ追従し、他ユーザーの音量は変えない。(2) 両SliderのCanvasへCanvas同寸のtrigger BoxColliderを、SceneへEventSystem + StandaloneInputModuleを生成した。VRChatはColliderへ当ててからGraphicRaycasterへ渡すため、これが無いと表示だけで触れない。つまみは半分の寸法にした。(3) ミラーUIを方向ごとのON/OFF 5個＋全OFF＋共通の画質LQ/HQの計7個へ置き換え、ボード全体のボタンは25→17。(4) ミラー4面は敷物のレンダリング済みメッシュから測った辺に沿って立て、真下の地形へ接地させ、方位磁石の高さも真下へのレイキャストで決めるようにした。
- OpenGameArtのCC0方位磁石を木陰へ追加した。本体はPickup/ObjectSync、針は各クライアントで天文上の北（ワールド+Z）を指すローカル計算とし、owner限定のドロップ10秒後Respawnを追加。初期位置はティーポットからworld X方向へ0.30m離した接地平面 `X 7.52 / Z 7.48` を正本とし、高さは生成時に真下の面（敷物または地形）から決める。再生成後は `y=2.4047` で敷物に接地する。
- YamaPlayer 2.0.0-beta.7は標準Playlist Editorを正本とし、QvPen 3.3.15と購入済みUnyStylus v1.3を含む外部依存は配布用unitypackageへ同梱しない。
- 今回差分はUnity 2022.3.22f1で `CheckUdonSharpProgramAssetsForBatchMode`、`BuildForBatchMode`、`ValidateForBatchMode`、`TestSkyAndMeteorForBatchMode` をすべて終了コード0で通し、保存Sceneを再生成済みである（`Logs/Claude-*.log`）。静的検査 `python Tools/Validate-StargazingImplementation.py` もPassし、絶対音量・Slider Collider・EventSystem・ミラー7ボタン・実測接地を不変条件として追加した。結果の正本は `docs/TEST_PLAN.md` の最新節とする。エージェントからUnityを起動するときはサンドボックス内のheadless実行ではlicense machine bindingが一致しないため、ホスト環境で通常Editor相当の `-quit -projectPath -executeMethod -logFile` を使う。ClientSim / PCVR / Quest / iOS操作はPending Evidence。
- 2026-08-20、側面2本の取っ手、3パネル共通の角丸ボタン、入退室トースト位置・文字サイズ、アラームアイコン位置まで生成正本と保存Sceneへ反映済み。詳細と検証結果は [ADR 0018](docs/adr/0018-settings-board-usability-fixes.md) 追記7および `docs/TEST_PLAN.md` の最新節を正本とする。
- 引き継ぎ時点のHEADは `0caab9c`。`Provisional`: 引き継ぎ前からある未コミット差分は `Assets/StargazingHill/Scenes/StargazingHill.unity` のみで、内容はVRChat SDKのBuild/Upload前処理が保存する `NetworkIDs`、Dynamic Materials、pipeline metadata、Udon同期方式などと一致する。由来を断定せず、手調整のScene差分と同様に破棄・再生成しない。
- 2026-08-20の引き継ぎ確認で、現在のSceneを再生成しない `Tools/Run-LocalChecks.ps1 -SkipBuild` 相当をWindows PowerShellから実行し、YamaPlayer / UnyStylusパッチ、静的検査、UdonSharp program assets、保存Scene検証、星空・流星数値試験がすべてPassした。ClientSim / Desktop / PCVR / Quest / iOSの操作・見た目は引き続きPending Evidence。
- 2026-08-20実機確認で3パネルの操作と入退室トースト位置はPass。左右2 Colliderの取っ手は右を狙うと左へ吸われたため、左右の棒を盤面奥の単一対称Colliderで覆う生成へ修正した。修正後の左右Grabとパネル操作もVRChat実機でPass。正本は [ADR 0018](docs/adr/0018-settings-board-usability-fixes.md) 追記8と `docs/TEST_PLAN.md` 最新節。
- World uploadは利用者が実施済み。
- BOOTH初回正式版を生成済み。`Build/BOOTH/StargazingHill-1.0.0-BOOTH.zip`（27,010,877 bytes、SHA-256 `a4a253835de475477654edfb4177fead9409c9bdecfdccf923cb6fd8eb67fd94`）に、検査済み `StargazingHill-1.0.0.unitypackage`（27,707,195 bytes、179 pathname、SHA-256 `5f3a8e0eaa1da2184e820e555e13a681084c950f3cca7bd6c8e18f3e56cd3729`）、repository非所持を前提とする5言語 `README.txt` / `NOTICE.txt`、`LICENSE.txt`、checksumを収録。日本語NOTICEは「現状のまま」と表記し、Markdown文書は0件。clean importはPending Evidence。

## 次の安全な一手

1. ClientSimまたはVRChat実機で、ラジオの実音声、つまみの実操作、3パネルの選択ビーム、VRChatカメラの写真へのUI写り込み、ミラーの新ON/OFFと敷物沿いの配置、入退室通知の実挙動を確認する。ここが今回修正の唯一の未証明部分である。
2. ClientSim複数人でGlobal観測地点の同期・途中参加、人数内訳、履歴スクロール、5言語切替、設定ボードのローカル性を確認する。
3. YamaPlayer標準Editorで保存した4 Playlist / 15 TrackとAutoPlay、ラジオSpeakerをClientSim、Windows、Android、iOSの新buildで再確認する。
4. PCVR、Android/standalone VR、iOSで星・月・流星、UI、動画・音声、描画ペン、各Pickup、方位磁石の北表示、ミラー、ナイトモード、アラーム、PlayerData復元、性能を確認し、結果を `docs/TEST_PLAN.md` へ追記する。
5. VRChat SDKの残存警告を第三者依存・対応可能・実機確認対象に分類して公開候補判定を行う。

一時的な作業状況だけをここへ置き、仕様判断は必ず該当する正本またはADRへ反映する。
