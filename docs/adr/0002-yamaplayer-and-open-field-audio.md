# ADR-0002: YamaPlayerと開放草原の距離減衰

- 状態: Accepted
- 決定日: 2026-08-11

## Context

動画プレイヤーの具体実装が未確定だった。参照ワールドはYamaPlayerを使用し、3D音声に8点のカスタム距離減衰を設定している。本ワールドでも同じ減衰が要求される一方、空間は壁のない草原である。

## Options

- YamaPlayer 2.0.0-beta.7と参照距離減衰をそのまま採用する。
- 別の動画プレイヤーを比較・採用する。
- 音声を2Dにして距離減衰を設けない。

評価軸は参照実装との一貫性、VPMによる再現性、VRChat SDK互換性、利用条件、景観と音声漏れである。

## Decision

- `net.kwxxw.yama-stream` 2.0.0-beta.7をVPM依存として固定する。
- VideoInfoDownloader moduleを追加する。
- 全AudioSourceを3D化し、0/7/14/19/21/28/29.5/45mの8点で参照ワールドと同じ減衰を適用する。
- `VRCSpatialAudioSource` はNear 0m、Far 45m、Spatialization有効、AudioSourceのVolume Curve使用とする。
- 開放草原であるため、参照ワールドの客室向け屋内外遮音ロジックは採用しない。

## Consequences

参照ワールドと同じ聴取距離を再現でき、package本体をGit管理せずVPMで復元できる。YamaPlayerがbeta版であること、PC / Android / iOSの実再生・同期・負荷が未検証であることを残余リスクとし、対象プラットフォームで問題が出た場合は版またはプレイヤー選定を見直す。
