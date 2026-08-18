# ADR 0017: 木陰の快適設定をローカル手持ちボードへ集約する

- 状態: Amended
- 決定日: 2026-08-15
- 更新: ミラーUI、ラジオ音量の定義、ミラーと方位磁石の設置基準は [ADR 0018](0018-settings-board-usability-fixes.md) が置き換える

## Context

チル用途として、ミラー、画面を暗くするナイトモード、日時、アラーム、ピクニックラジオを一か所から操作したい。これらは利用者ごとの快適性設定であり、Global同期すると他人の視界・音・UIを予期せず変更する。設定の永続化も明示的に選べる必要がある。

## Decision

- `World/SettingsSystem` を生成し、木の小型歯車ボタンからローカル表示する `LocalSettingsBoard` へ機能を集約する。歯車はフォントglyphや重複Cubeではなく、穴のある単一Meshと `Unlit/Color` 材質で生成してちらつきを避ける。歯車のTransformは利用者がSceneで確定した位置 `(8.053, 2.331, 7.59)`、回転quaternion `(0.627459, 0.33232313, -0.35606158, 0.6075168)`、scale `0.46967`を生成正本へ取り込む。見た目は小さいまま、操作面だけworld約0.30m角となるようlocal Colliderを逆補正し、Udon `Interact`へ直接接続する。木の歯車には透明`Image` / `Button` / `VRCUiShape`面を重ねない。
- ボードは約0.62 × 0.57mの `VRCPickup` とし、ドロップ10秒後に生成位置へ戻す。初期状態は非表示、操作ボタンは板面と面一にする。Pickup Colliderは上端の細いグリップに限定し、proximityは0.35mとする。`VRCObjectSync`を付けず、持ち運びと復帰を各クライアントだけで処理することで、板面のUI操作とローカル設定を他人へ干渉させない。
- ミラーは上、下、左、右、天井の5方向をローカル選択する。初期値は全OFF。LQはpixel light無効・AA 1、HQはpixel light有効・AA 4とし、UIへHQ高負荷の注意と有効数を表示する。操作系は [ADR 0018](0018-settings-board-usability-fixes.md) で方向ごとのON/OFF＋共通画質切替へ置き換えた。
- ナイトモードはPost ProcessingやWorld Lightingを変更せず、ローカルプレイヤー頭部へ追従する内向き半透明sphereをWorld Space UI Sliderで調整する。
- 日時はクライアントの `DateTime.Now` を表示する。アラームはローカル2D音源で鳴らし、時・分・ON/OFF・STOPを持つ。
- ラジオはYamaPlayerの追加Speakerとして構成し、設定ボードの0〜100%ローカル音量を適用してMuteへ追従する。当初はYamaPlayerマスターへの倍率としたが、マスター既定値0.1では実質無音になるため [ADR 0018](0018-settings-board-usability-fixes.md) で絶対音量へ変更した。ラジオ本体にはUSE Trigger、状態表示、Colliderを作らず、設定ボードのローカルON/OFFが追加Speakerを直接切り替える。
- 木陰の方位磁石は本体だけをPickup/ObjectSyncする。針は `BehaviourSyncMode.None` で、天文系と同じワールド+Zを北として各クライアントがローカル計算する。導出可能な針角度を同期データにしない。ドロップ10秒後は現在のObject ownerだけが `VRCObjectSync.Respawn()` を呼び、初期位置へ戻す。初期配置はティーポットからworld X方向へ0.30m離し、Pickup時の干渉を避ける。高さの決め方は [ADR 0018](0018-settings-board-usability-fixes.md) で実測へ変更した。
- `SAVE` がONのときだけVRChat PlayerDataへ保存し、`OnPlayerRestored` 後に復元する。保存対象はナイトモード、5方向のミラー状態と共通画質、アラーム、ラジオ音声ON/OFF、ラジオ音量とする。旧 `RadioUse` keyは読み取り互換だけ残す。
- 設定ボード全体は `BehaviourSyncMode.None` とし、Global同期変数を持たない。

## Consequences

- 利用者は他人へ影響せず、自分の明るさ、鏡、通知、ラジオを調整できる。
- ナイトモードは見た目を暗くするだけで、Bloomや露出制御を持つ本格的なPost Processingとは異なる。
- ミラーと透明オーバーレイは描画負荷を増やすため初期OFFとし、Quest / iOS実機確認を完了条件に残す。
- PlayerDataは同じWorld内のUdonBehaviourで共有されるため、keyを `StargazingHill.Settings.*` へ名前空間化する。
- 方位磁石は誰が持っても各自の描画で同じワールド北を示す。本体姿勢の同期遅延はあり得るが、針状態の同期競合は起きない。復帰処理をownerへ限定するため、複数クライアントが同時にRespawnを送らない。

## Evidence

- `Assets/StargazingHill/Scripts/WorldSettingsController.cs`
- `Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs`
- `Assets/StargazingHill/Shaders/NightModeOverlay.shader`
- `Assets/StargazingHill/Scripts/LocalCompassNeedle.cs`
- `Assets/StargazingHill/Editor/CompassSceneInstaller.cs`
- `Assets/StargazingHill/ThirdParty/OpenGameArt/Compass/NOTICE.md`
- [VRChat PlayerData](https://creators.vrchat.com/worlds/udon/persistence/player-data/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC Mirror Reflection](https://creators.vrchat.com/worlds/components/vrc_mirrorreflection/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC UI Shape](https://creators.vrchat.com/worlds/components/vrc_uishape/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC Pickup](https://creators.vrchat.com/worlds/components/vrc_pickup/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC Object Sync](https://creators.vrchat.com/worlds/components/vrc_objectsync/)（確認日 2026-08-15、Worlds SDK 3.10.4）
