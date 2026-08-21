# ADR 0019: 入退室通知を音と表示に分けたローカル機能として持つ

- 状態: Accepted
- 決定日: 2026-08-16
- 関係: [ADR 0017](0017-local-comfort-settings-board.md) のローカル設定ボードへ機能を追加する

## Context

説明パネルには入退室の履歴一覧があるが、これは能動的に見に行くものである。空を見上げているときに誰かが来たこと・帰ったことに気付けない。一方で、通知音も画面表示も好みが割れる要素であり、星を眺めている最中に視界へ文字が出るのを嫌う人もいる。

## Decision

- `World/SettingsSystem/LocalPresenceNotifier` として、入退室の通知音と画面表示を持つ `WorldPresenceNotifier` を生成する。`BehaviourSyncMode.None` とし、同期変数を持たない。
- 通知音と画面表示は**別々にON/OFF**する。設定ボードの左列下段へ `通知音 ON/OFF` と `入退室表示 ON/OFF` の2ボタンと、現在値を示す1行を置く。初期値は両方ON。
- 乗降と同じく、入室と退室を**別々の約3秒の受付窓**へ集約する。窓の最初の1人は名前をそのまま表示し、窓の内で増えた分は `○○ さんほか3名が入室しました` のように件数だけ更新する。**通知音は各窓の最初の1回だけ**鳴らし、窓の内の追加では鳴らさない。説明パネルの履歴は従来どおり一人ずつ記録する。この集約仕様は、同じ作者の非公開の姉妹ワールドで先に決めた乗下船通知の設計から取り込んだ。
- 音は生成した2音のチャイムとする。入室は上行（660 → 990 Hz）、退室は下行（880 → 587 Hz）。0.40秒、raised-cosine包絡でクリックノイズを避ける。`spatialBlend 0`、`EnableSpatialization` オフのローカル2D音声とし、ラジオやアラームと違ってワールド上の位置を持たない。
- 表示は消える直前の1.2秒でフェードアウトさせる。透明度は最も新しい行の残り時間で決めるので、フェード中に次の入退室があればブロック全体が全不透明へ戻る。
- 画面表示は頭部追従の1〜3行トーストとし、5秒で1行ずつ消える。前方1.5m、視線から0.48m下へ置き、空を遮らない。レイヤーは `PlayerLocal` とする。ローカル専用であり、鏡にも他人の写真にも写らない。
- 初回入室時の一斉通知を避けるため、ローカルプレイヤー自身の `OnPlayerJoined` を受け取るまでは通知しない。VRChatは既存プレイヤー分の join を再生してから最後にローカルプレイヤーを通知するため、これが実質的な「ライブ開始」の合図になる。ローカルプレイヤー自身の入退室は通知しない。
- 表示文言は設定ボードの言語切替へ追従する。既に表示中の行は切替時に書き換えない。
- `SAVE` がONのときだけ、`StargazingHill.Settings.NotifySound` と `StargazingHill.Settings.NotifyDisplay` としてPlayerDataへ保存する。姉妹ワールドは `bkw.kogastats.notify.*` へ無条件保存だが、本ワールドは全設定を1つの `SAVE` トグルで束ねる既存方針（[ADR 0017](0017-local-comfort-settings-board.md)）を優先する。
- 初期値は**通知音・表示とも ON** とする。姉妹ワールドは音を初期OFFとしているが、本ワールドは最大32人・推奨16人の静かなインスタンスで、受付窓により1グループ1回へ集約され、音量も0.22と控えめである。ラジオを初期ONとした判断（[ADR 0018](0018-settings-board-usability-fixes.md)）とも揃える。うるさければボタン1つで切れる。

## Consequences

- 空を見上げたままでも人の出入りが分かる。履歴一覧（説明パネル）は「後から確認する」用途として残り、役割が分かれる。
- トーストは `PlayerLocal` レイヤーなので、VRChatのカメラで撮った写真には写らない。通知は一時的なUIであり、写真へ残さないほうが望ましいと判断した。恒久的に見せたい情報は従来どおり説明パネルに置く。
- 音は2D固定なので、ワールドのどこにいても同じ音量で鳴る。受付窓で1グループ1回へ集約されるが、断続的な出入りが続けば繰り返し鳴る。音だけを切れる必要があるのはこのためで、ON/OFFを音と表示で分けている。
- 受付窓は表示の即時性と引き換えに、2人目以降の名前を落とす。名前を全員分たどりたい場合は説明パネルの履歴を見る、という役割分担にする。
- 頭部追従UIはVRで酔いの原因になり得る。距離と下方向オフセットを控えめにし、表示が無いときはGameObjectごと非アクティブにして描画も止める。実機での快適性は要確認とする。

## Evidence

- `Assets/StargazingHill/Scripts/WorldPresenceNotifier.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsController.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsButton.cs`
- `Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs`
- [VRChat PlayerData](https://creators.vrchat.com/worlds/udon/persistence/player-data/)（確認日 2026-08-16、Worlds SDK 3.10.4）
- [Player Layers](https://creators.vrchat.com/worlds/layers/)（確認日 2026-08-16、Worlds SDK 3.10.4）
- 非公開の姉妹ワールドリポジトリの構想メモとBACKLOG（乗下船通知の受付窓・集約・音の扱い、確認日 2026-08-18）
