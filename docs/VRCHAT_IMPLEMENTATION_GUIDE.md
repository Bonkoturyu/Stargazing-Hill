# VRChat実装ガイド

この文書は参考リポジトリで得た実装上の注意をまとめる。公式仕様として確認できていない項目は `Provisional` とする。

## Unityアセット

- `Confirmed`: アセットと `.meta` は対で扱い、GUIDを維持する。
- `Confirmed`: UdonSharpのコンパイル生成物、SDKサンプル、Editorキャッシュはソース管理しない。
- `Confirmed`: シーン/Prefabの同時編集を避け、Unity Editorの所有者を一人にする。

## 星空

- `Confirmed`: カタログ座標から全天球の星を極小Quad化し、1 Mesh / 1 Renderer / Additive Unlitを基本とする。
- `Confirmed`: 星ごとのGameObjectや毎フレーム更新は行わず、天球Transformを低頻度で更新する。
- `Confirmed`: 既存の観測地固定ベーカーは直接移植せず、カタログ空間のMesh生成と東京・現在時刻の回転を分離する。
- `Pending Evidence`: HYGデータまたは派生物をリポジトリへ追加する前に、出典、版、ライセンス、表示義務、派生物の配布条件を記録する。

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

## 配布

- `Confirmed`: 再配布用unitypackageは所有アセットrootを明示列挙し、外部依存を再帰的に含めない。YamaPlayer、QvPen、購入品UnyStylusは配布先で正規経路から復元する。詳細は [ADR 0010](adr/0010-redistributable-unitypackage-boundary.md)。

## 性能

固定の予算値を他プロジェクトからコピーしない。Profiler、Build Size、VRChat SDKの警告、対象端末の実測から予算を決め、[TEST_PLAN.md](TEST_PLAN.md)へ記録する。
