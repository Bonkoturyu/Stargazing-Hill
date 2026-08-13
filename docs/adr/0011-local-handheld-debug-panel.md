# ADR-0011: ローカル手持ちデバッグパネルと遅延復帰

- 状態: Accepted
- 決定日: 2026-08-13

## Context

既存の流星デバッグパネルは据置き前提の約2.35 × 2.05mで、VR内で手に持って流星を見ながら操作できなかった。ユーザーは、参考ワールド `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` と同様のリモコンUI、手持ち操作、放置後約10秒で元位置へ戻る挙動を要求した。

参考実装はcommit `1d8ccafca7b0c0c11dbadef5aa8a029f6c7ef8ae` の `LocalPickupReturn.cs` とデッキ上リモコン生成処理で確認した。同リポジトリの実機記録ではローカルPickupと復帰処理がPCVR / QuestでPassしている。

## Decision

- パネルのauthoring layoutは維持し、root scaleを `0.20` にして実寸を約0.47 × 0.41mへ縮小する。
- rootへ `VRCPickup`、trigger `BoxCollider`、重力なし `Rigidbody`、`WorldDebugPanelPickup` を付与する。
- Pickupはローカル専用とし、`ObjectSync`や同期変数を追加しない。デバッグ操作自体も従来どおり各利用者のローカル状態だけを変更する。
- ドロップから `WorldDebugPanelPickup.ReturnDelaySeconds`（初期値10秒）が経過したら、初期local position / rotation / scaleへ戻す。
- 復帰待ち中に再取得した場合は古い復帰要求を無効化し、次のドロップから再計時する。
- 保持中はrootの取得用Colliderを無効化し、子ボタンの操作Colliderを使える状態にする。
- 参考実装の両手拡縮やネットワーク同期は目的外のため採用しない。

## Consequences

流星を見ながら片手で持ち運び、もう一方の手で操作できる小型リモコンになる。放置されたパネルは10秒後に設備エリアのdockへ戻る。Unityで構造・寸法・定数を自動検証する一方、文字の可読性、ボタンの押しやすさ、保持姿勢、復帰挙動は本ワールドのPCVR / Quest実機で確認するまで `Open` とする。
