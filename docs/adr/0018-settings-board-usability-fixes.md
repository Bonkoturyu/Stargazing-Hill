# ADR 0018: 設定ボードの操作性と設置基準を実測へ寄せる

- 状態: Accepted
- 決定日: 2026-08-16
- 関係: [ADR 0017](0017-local-comfort-settings-board.md) の一部決定を置き換える

## Context

[ADR 0017](0017-local-comfort-settings-board.md) で導入したローカル設定ボードと方位磁石を実機で触ったところ、次の4点が「仕様どおりだが使えない」状態だった。

- ラジオから音が出ない。ラジオ音量をYamaPlayerマスターへの倍率と定義したが、YamaPlayer 2.0.0-beta.7のマスター既定値は `0.1` である。既定の倍率0.65と掛けると実効0.065になり、空間減衰後はほぼ無音になる。加えてSpeakerの初期値がOFFのため、何もしなければ音源が存在しないのと同じだった。
- ナイトモードとラジオ音量のつまみを掴めない。原因は2つあった。(1) VRChatのワールドUIはまずColliderへ当ててからGraphicRaycasterへ渡すが、Slider CanvasにColliderが無かった。SceneにEventSystemも無く、uGUIのdragイベント経路も欠けていた。(2) **より本質的に、UIレイヤー(5)に置いていた。** つまみ自体も板面に対して大きすぎた。
- 説明ボード・設定パネル・デバッグパネルに選択ビームが出ない。ビームが出るのはYamaPlayerのControlBarだけだった。
- ミラーが方向ごとに `OFF / LQ / HQ` の3択・計15ボタンで、ON/OFFしたいだけの操作に対して重い。
- ミラーと方位磁石の位置が実際の地形・敷物と合っていない。どちらも手入力した固定値で、方位磁石は約2.5cm浮き、ミラーは敷物の保存Transform（実マット中心からずれる）と固定オフセットを基準にしていた。

## Decision

- ラジオ音量は、追加ローカルSpeakerだけに掛かる **絶対** ローカル音量とする。YamaPlayerマスターへの倍率にはしない。YamaPlayer本体と他ユーザーの音量は従来どおり変更せず、MuteだけがYamaPlayerへ追従する。初期値はON / 85%とし、Speakerの空間音響はNear 1.5m / Far 22mへ広げて敷物の上を全音量域に収める。
- **ワールドUIのCanvasをUIレイヤーへ置くのをやめ、Defaultレイヤー(0)へ統一する。** VRChat公式SDKの `VRC.SDK3.ClientSim.ClientSimInteractiveLayerProvider` は、メニューを閉じている間の操作対象レイヤーを `~(1 << UI_LAYER) & ~(1 << UI_MENU_LAYER) & ~(1 << PLAYER_LOCAL_LAYER) & ~(1 << MIRROR_REFLECTION_LAYER)` として構築する。つまりUIレイヤーは通常プレイ中は操作対象から外れる。VRChatのカメラも同レイヤーを写さないため、写真にUIが写らない問題も同じ原因だった。このワールドで唯一ビームが出ていたYamaPlayer ControlBarのCanvasがレイヤー0であることが裏付けになる。
- ワールドUIのCanvasには、Canvasと同寸法のtrigger `BoxCollider` を必ず持たせる。奥行はCanvasごとにスケールが2桁違うため、`lossyScale` から逆算して world 4mm に揃える。SceneにEventSystemが無ければ `EventSystem` + `StandaloneInputModule` を1つ生成する。VRChatのdefault world sceneと同じ構成である。
- ラベルがuGUI Textのボタンはそのラベル用Canvasをビーム面として再利用し、ラベルがTextMeshのデバッグパネルには不可視の `UiBeamTarget` Canvasを別途生成する。これで3パネルすべてにビームが出る。
- つまみは従来の半分（ナイトモード255 × 41、ラジオ音量180 × 32 canvas units）にする。
- ミラーUIは、方向ごとのON/OFFボタン5個、全OFFボタン1個、ON中の全面へ一括適用する `画質 LQ / HQ` ボタン1個の計7個に置き換える。初期値は全OFF・LQのまま、LQ/HQの描画設定も変えない。
- ミラー4面は、敷物の**レンダリング済みメッシュ**から測った辺に沿って立てる。幅は辺の長さ、位置は辺から0.06m外側、基準面は各面の真下の地形へ接地させる。天井面は敷物中心の2.55m上へ敷物の平面寸法で置く。
- 方位磁石は接地平面 `X 7.52 / Z 7.48` だけを正本とし、高さは生成時に真下へレイキャストして得た面（敷物があれば敷物、なければ地形）から決める。

## Consequences

- 「触れない」「ビームが出ない」「写真に写らない」の3つが1つの原因に収束し、レイヤーの統一で同時に解決する。ワールドUIをUIレイヤーへ置くという一般的な直感は、VRChatでは逆効果である。
- ラジオ音量スライダーの表示値と体感音量が一致する。反面、YamaPlayerのマスター音量を上げてもラジオ音量は変わらない。ラジオはワールド全体のBGMではなく、敷物まわりのローカル音源という位置づけになる。
- 敷物は丘の斜面に沿って1.2m以上の高低差を持つため、4面のミラーは互いに異なる高さで立つ。これは平面を仮定した固定値より実際の地面に忠実であり、敷物のレイアウトを動かせばミラーも追従する。
- ミラーの方向別HQ/LQ混在ができなくなる。方向ごとに負荷を作り分ける用途は失われるが、ボタン数が15から7へ減り、想定していた「見たい面だけ点ける」操作に一致する。
- 保存済みPlayerDataは読み替えで引き継ぐ。方向ごとの品質値は「0でなければON」として扱い、いずれかがHQなら共通画質をHQとして復元する。
- 生成物の検証も実測基準へ変わる。ミラーは辺からの距離と接地高さ、方位磁石は真下の面との差、Sliderはtrigger Colliderの有無をScene検証で確認する。

## Evidence

- `Assets/StargazingHill/Scripts/WorldRadioSpeaker.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsController.cs`
- `Assets/StargazingHill/Scripts/WorldSettingsButton.cs`
- `Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs`
- `Assets/StargazingHill/Editor/CompassSceneInstaller.cs`
- `Packages/net.kwxxw.yama-stream/Prefabs/Components/Controller.prefab`（`_volume: 0.1`、確認日 2026-08-16、YamaPlayer 2.0.0-beta.7）
- `Packages/net.kwxxw.yama-stream/Prefabs/ControlBar.prefab`（動作するワールドUI Canvasのレイヤー0 + `BoxCollider` 構成、確認日 2026-08-16）
- `Packages/com.vrchat.worlds/Integrations/ClientSim/Runtime/System/ClientSimInteractiveLayerProvider.cs`（操作対象レイヤーマスクの構築、確認日 2026-08-16、Worlds SDK 3.10.4）
- `Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/VRCDefaultWorldScene.unity`（EventSystem + StandaloneInputModule、確認日 2026-08-16）
- [VRC UI Shape](https://creators.vrchat.com/worlds/components/vrc_uishape/)（確認日 2026-08-16、Worlds SDK 3.10.4）
- [VRC Mirror Reflection](https://creators.vrchat.com/worlds/components/vrc_mirrorreflection/)（確認日 2026-08-16、Worlds SDK 3.10.4）
