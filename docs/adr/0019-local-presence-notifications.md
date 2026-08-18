# ADR 0019: 入退室通知を音と表示に分けたローカル機能として持つ

- 状態: Accepted
- 決定日: 2026-08-16
- 関係: [ADR 0017](0017-local-comfort-settings-board.md) のローカル設定ボードへ機能を追加する

## Context

説明パネルには入退室の履歴一覧があるが、これは能動的に見に行くものである。空を見上げているときに誰かが来たこと・帰ったことに気付けない。一方で、通知音も画面表示も好みが割れる要素であり、星を眺めている最中に視界へ文字が出るのを嫌う人もいる。

## Decision

- `World/SettingsSystem/LocalPresenceNotifier` として、入退室の通知音と画面表示を持つ `WorldPresenceNotifier` を生成する。`BehaviourSyncMode.None` とし、同期変数を持たない。
- 通知音と画面表示は**別々にON/OFF**する。設定ボードの左列下段へ `通知音 ON/OFF` と `入退室表示 ON/OFF` の2ボタンと、現在値を示す1行を置く。初期値は両方ON。
- 音は生成した2音のチャイムとする。入室は上行（660 → 990 Hz）、退室は下行（880 → 587 Hz）。0.40秒、raised-cosine包絡でクリックノイズを避ける。`spatialBlend 0`、`EnableSpatialization` オフのローカル2D音声とし、ラジオやアラームと違ってワールド上の位置を持たない。
- 画面表示は頭部追従の1〜3行トーストとし、5秒で1行ずつ消える。前方1.5m、視線から0.42m下へ置き、空を遮らない。レイヤーは `PlayerLocal` とする。ローカル専用であり、鏡にも他人の写真にも写らない。
- 初回入室時の一斉通知を避けるため、ローカルプレイヤー自身の `OnPlayerJoined` を受け取るまでは通知しない。VRChatは既存プレイヤー分の join を再生してから最後にローカルプレイヤーを通知するため、これが実質的な「ライブ開始」の合図になる。ローカルプレイヤー自身の入退室は通知しない。
- 表示文言は設定ボードの言語切替へ追従する。既に表示中の行は切替時に書き換えない。
- `SAVE` がONのときだけ、`StargazingHill.Settings.NotifySound` と `StargazingHill.Settings.NotifyDisplay` としてPlayerDataへ保存する。

## Consequences

- 空を見上げたままでも人の出入りが分かる。履歴一覧（説明パネル）は「後から確認する」用途として残り、役割が分かれる。
- トーストは `PlayerLocal` レイヤーなので、VRChatのカメラで撮った写真には写らない。通知は一時的なUIであり、写真へ残さないほうが望ましいと判断した。恒久的に見せたい情報は従来どおり説明パネルに置く。
- 音は2D固定なので、ワールドのどこにいても同じ音量で鳴る。人数の多いインスタンスでは連続して鳴り得るため、音だけを切れる必要がある。ON/OFFを音と表示で分けたのはこのためである。
- 頭部追従UIはVRで酔いの原因になり得る。距離と下方向オフセットを控えめにし、表示が無いときはGameObjectごと非アクティブにして描画も止める。実機での快適性は要確認とする。

## Evidence

- `Assets/StargazingHill/Scripts/WorldPresenceNotifier.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsController.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsButton.cs`
- `Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs`
- [VRChat PlayerData](https://creators.vrchat.com/worlds/udon/persistence/player-data/)（確認日 2026-08-16、Worlds SDK 3.10.4）
- [Player Layers](https://creators.vrchat.com/worlds/layers/)（確認日 2026-08-16、Worlds SDK 3.10.4）
