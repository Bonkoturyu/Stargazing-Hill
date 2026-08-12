# Backlog

## Ready

- ClientSimと実機でYamaPlayer再生・同期・距離減衰を確認する。
- PC / Quest(Android) / iOSで月、11流星群、描画負荷を確認する。
- 毎時3分の流星イベントをBuild & Testで確認し、弱い群〜ふたご座極大相当まで出現間隔と同時4 Quad上限を実機評価する。
- `StargazingWorldBuilder` の生成時 `eventDurationSeconds` と保存Sceneのserialized値を180秒へ統一する。現状は旧25秒値を `MeteorController.Start()` のmigration guardで180秒へ補正する。
- 流星群ごとの実際のピーク幅をcatalog化し、開始→極大→終了の三角形近似を置き換える。
- 次年のIMO calendar採用時にcatalogと検証fixtureを更新する。

## Blocked by evidence

- QvPen upstream packageの明示ライセンスを確認する。

## Later

- 月相表現を採用するか決定する。
- 複数流星群の同時放射点演出が必要か実機で評価する。
