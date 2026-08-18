# VRChat実装ガイド

この文書は参考リポジトリで得た実装上の注意をまとめる。公式仕様として確認できていない項目は `Provisional` とする。

## Unityアセット

- `Confirmed`: アセットと `.meta` は対で扱い、GUIDを維持する。
- `Confirmed`: UdonSharpのコンパイル生成物、SDKサンプル、Editorキャッシュはソース管理しない。
- `Confirmed`: シーン/Prefabの同時編集を避け、Unity Editorの所有者を一人にする。

## 星空

- `Confirmed`: カタログ座標から全天球の星を極小Quad化し、1 Mesh / 1 Renderer / Additive Unlitを基本とする。
- `Confirmed`: 星ごとのGameObjectや毎フレーム更新は行わず、天球Transformを低頻度で更新する。
- `Confirmed`: 既存の観測地固定ベーカーは直接移植せず、カタログ空間のMesh生成と、現在時刻・Global観測地点による天球回転を分離する。
- `Confirmed`: HYG v4.1の出典、版、ライセンス、加工工程、原本と派生データのSHA-256を `Assets/StargazingHill/Editor/Data/NOTICE.md` と [第三者素材台帳](legal/THIRD_PARTY_ASSETS.md) に記録する。

## Udon / Networking

- `Provisional`: サーバー時刻の差は直接減算せず、現行SDKの安全な差分計算APIを使う。
- `Provisional`: 大きな整数を単純に `int` へキャストしない。Udonの型制約とオーバーフローを確認する。
- `Provisional`: `OnDeserialization` は受信側の適用経路として設計し、Owner側は状態変更時に明示適用する。適用処理は冪等にする。
- `Confirmed`: 日時から決定的に再構成できる星空・月・毎時イベントは、ネットワーク同期を持たず同じ時刻源から算出する。
- `Confirmed`: Play Mode中のEditorツールからUdonSharpを操作するときはproxyのC#メソッドを直接呼ばない。`UdonSharpEditorUtility.GetBackingUdonBehaviour` でbacking Udonを取得し、入力を `SetProgramVariable` して引数なしCustomEventを送る。proxy直接呼出しの状態はUdon VM heapへ反映されず、次のUdon Updateで上書きされる。
- `Provisional`: VRChat Worlds SDK 3.10.4のClientSimでは、Scene load中にUdonを先行初期化した後で `IsNetworkingSupported` を再設定して例外になる場合がある。`VrcSdk3104ClientSimGuard` は上流3.10.4の該当2断片が完全一致するときだけ、初回登録前のnetworking有効化と初期化済みsetterのskipをVPM管理packageへ適用する。VPM復元後も再適用し、SDK版または上流コードが変わった場合は書換えず警告する。詳細は [ADR 0006](adr/0006-vrcsdk-3104-clientsim-networking-guard.md) を正本とする。

## コンポーネントとUI

- `Provisional`: 2D音声でもアップロード時のVRChat Audio Source挙動を確認し、空間化を明示設定する。
- `Provisional`: Stationは着席設定、`Interact()` からの入場、VRでの退出操作をClientSimと実機で確認する。
- `Provisional`: World-space Canvasは背景面から十分離し、Z-fightingを実機で確認する。初期目安は0.05m以上。
- `Provisional`: TMPのフォールバック、モバイル対応Shader、透明描画コストをQuest/iOSで確認する。
- `Confirmed`: ローカル専用デバッグリモコンは `VRCPickup` と重力なし `Rigidbody` を使い、同期コンポーネントを付けない。ドロップからの復帰待ち中に再取得された場合、古い遅延eventを状態検査で無効化する。詳細は [ADR 0011](adr/0011-local-handheld-debug-panel.md)。
- `Confirmed`: ローカル設定ボードも `VRCPickup` だけを使い、Transform同期を行う `VRCObjectSync` は付けない。操作面とPickup判定を重ねず、上端の細いグリップだけをPickup対象にする。ボード本体、5言語切替、各設定、10秒復帰は利用者ごとのローカル状態とする。
- `Confirmed`: 5方向ミラーはピクニック敷物のレンダリング済みメッシュの実寸を基準に生成し、VRChat mirrorの反射面であるQuadのlocal `-Z`を敷物側へ向ける。4面は各辺に沿って辺長と同じ幅で立て、辺から0.06m外側、真下の地形へ接地させる。敷物の保存Transformは実マット中心からずれるため基準にしない。UIは方向ごとのON/OFFと共通の `画質 LQ/HQ` とし初期OFF、複数HQの実機負荷は未検証としてUIと試験計画に残す。
- `Confirmed`: YamaPlayerのラジオSpeakerは専用 `AudioSource` と `VRCSpatialAudioSource` を明示して追加する。ラジオ本体にはUSE Triggerや状態表示を作らず、設定ボードのローカルON/OFFからSpeaker gainを直接切り替える。音声経路は無効化せず、OFF時はvolumeを0にする。専用音量はYamaPlayerマスター音量へ掛けるローカル倍率とし、元プレイヤーや他ユーザーの音量を変更しない。
- `Confirmed`: 共有する方位磁石は `VRCPickup` と `VRCObjectSync` を付ける一方、針は同期せず各クライアントでワールド北へ向ける。復帰時は現在のownerだけが `Respawn()` を呼ぶ。初期位置はティーポットからworld X方向へ0.30m離した接地平面で、高さは真下へのレイキャストで決める。用途別の同期境界は [ADR 0017](adr/0017-local-comfort-settings-board.md)、操作性と設置基準の更新は [ADR 0018](adr/0018-settings-board-usability-fixes.md) を正本とする。
- `Confirmed`: **ワールドUIをUIレイヤー(5)へ置いてはいけない。** `VRC.SDK3.ClientSim.ClientSimInteractiveLayerProvider` はメニューを閉じている間の操作対象を `~(1 << UI_LAYER) & ~(1 << UI_MENU_LAYER) & ~(1 << PLAYER_LOCAL_LAYER) & ~(1 << MIRROR_REFLECTION_LAYER)` で組み立てる。UIレイヤーは通常プレイ中の操作対象から外れ、ワールドカメラの写真にも写らない。Defaultレイヤー(0)へ置く。
- `Confirmed`: ワールドUIのCanvasは、Canvasと同寸のtrigger `BoxCollider` とSceneのEventSystemが揃って初めて操作できる。VRChatはColliderへ当ててからGraphicRaycasterへ渡すため、`VRCUiShape` とGraphicRaycasterだけでは表示のみで反応しない。動作実績は `net.kwxxw.yama-stream` のControlBar Canvas（レイヤー0 + BoxCollider）と、VRChat default world sceneのEventSystemで確認した。
- `Confirmed`: ワールドUIの `Button.onClick` からUdonを呼ぶときは `UdonBehaviour.Interact()` を指定する。UdonSharpの `public override void Interact()` はUdonのエントリポイント `_interact` へコンパイルされるため、`SendCustomEvent("Interact")` は存在しないイベントを指し、クリックが黙って捨てられる。VRChatの `UnityEventFilter` は両方を許可するので誤りに気付きにくい。
- `Confirmed`: Desktopではマウス移動がカメラ操作なので、ワールド空間のSliderはドラッグできない。Sliderを置く場合は同じ値を動かすボタンを併設する。
- `Confirmed`: 3Dキューブのボタンへビームを出すには、ボタン面にUI Canvasを重ねる。ラベルがuGUI Textならそのラベル用Canvasを流用し、ラベルがTextMeshなら不可視の `UiBeamTarget` Canvasを別途生成して `Button.onClick` からUdon `Interact` を送る。Udon Interact用Colliderだけではビームは出ない。
- `Confirmed`: 頭部追従のローカルUIは `PlayerLocal` レイヤーへ置く。本人だけに見え、鏡にも他人の写真にも写らない。逆に、写真へ残したい常設UIをこのレイヤーへ置いてはいけない。
- `Confirmed`: YamaPlayerの追加Speakerへローカル音量を掛けるときは、YamaPlayerマスター（既定 `0.1`）への倍率にしない。倍率にすると実効音量が桁で下がる。追加Speakerだけに掛かる絶対音量とし、Muteのみ追従する。

## 配布

- `Confirmed`: 再配布用unitypackageは所有アセットrootを明示列挙し、外部依存を再帰的に含めない。YamaPlayer、QvPen、購入品UnyStylusは配布先で正規経路から復元する。詳細は [ADR 0010](adr/0010-redistributable-unitypackage-boundary.md)。

## 性能

固定の予算値を他プロジェクトからコピーしない。Profiler、Build Size、VRChat SDKの警告、対象端末の実測から予算を決め、[TEST_PLAN.md](TEST_PLAN.md)へ記録する。
