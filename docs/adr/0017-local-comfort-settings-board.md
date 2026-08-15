# ADR 0017: 木陰の快適設定をローカル手持ちボードへ集約する

- 状態: Accepted
- 決定日: 2026-08-15

## Context

チル用途として、ミラー、画面を暗くするナイトモード、日時、アラーム、ピクニックラジオを一か所から操作したい。これらは利用者ごとの快適性設定であり、Global同期すると他人の視界・音・UIを予期せず変更する。設定の永続化も明示的に選べる必要がある。

## Decision

- `World/SettingsSystem` を生成し、木のボタンからローカル表示する `LocalSettingsBoard` へ機能を集約する。
- ボードは `VRCPickup` とし、ドロップ10秒後に生成位置へ戻す。初期状態は非表示、操作ボタンは板面と面一にする。
- ミラーは上、下、左、右、天井の5候補を用意し、常に最大1面だけをローカル表示する。初期値は全OFF、反射LayerとAAをモバイル向けに制限する。
- ナイトモードはPost ProcessingやWorld Lightingを変更せず、ローカルプレイヤー頭部へ追従する内向き半透明sphereをWorld Space UI Sliderで調整する。
- 日時はクライアントの `DateTime.Now` を表示する。アラームはローカル2D音源で鳴らし、時・分・ON/OFF・STOPを持つ。
- ラジオはYamaPlayerの追加Speakerとして構成し、YamaPlayerの音量・Muteへ追従する。設定ボードでUSE可否を有効にした場合だけラジオ側SpeakerをUSEでON/OFFできる。
- `SAVE` がONのときだけVRChat PlayerDataへ保存し、`OnPlayerRestored` 後に復元する。保存対象はナイトモード、選択ミラー、アラーム、ラジオUSE可否とする。
- 設定ボード全体は `BehaviourSyncMode.None` とし、Global同期変数を持たない。

## Consequences

- 利用者は他人へ影響せず、自分の明るさ、鏡、通知、ラジオを調整できる。
- ナイトモードは見た目を暗くするだけで、Bloomや露出制御を持つ本格的なPost Processingとは異なる。
- ミラーと透明オーバーレイは描画負荷を増やすため初期OFFとし、Quest / iOS実機確認を完了条件に残す。
- PlayerDataは同じWorld内のUdonBehaviourで共有されるため、keyを `StargazingHill.Settings.*` へ名前空間化する。

## Evidence

- `Assets/StargazingHill/Scripts/WorldSettingsController.cs`
- `Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs`
- `Assets/StargazingHill/Shaders/NightModeOverlay.shader`
- [VRChat PlayerData](https://creators.vrchat.com/worlds/udon/persistence/player-data/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC Mirror Reflection](https://creators.vrchat.com/worlds/components/vrc_mirrorreflection/)（確認日 2026-08-15、Worlds SDK 3.10.4）
- [VRC UI Shape](https://creators.vrchat.com/worlds/components/vrc_uishape/)（確認日 2026-08-15、Worlds SDK 3.10.4）
