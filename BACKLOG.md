# Backlog

## Ready

- ClientSimとQuest実機で、保存済みruntime Playlistの4リスト15曲表示、AutoPlay開始、同期、距離減衰を再確認する。旧uploadではリスト空・自動再生なしを確認済みのため、新buildでのPass証拠が必要。
- PC / Quest(Android) / iOSで月、11流星群、描画負荷を確認する。
- Quest / iOSでNightSkyGradientの黒階調・banding、±250m地面端、説明パネルの可読性、入退室履歴、デバッグPickupと10秒復帰を確認する。
- 通常Unity Editorで追加設定システムを生成し、UdonSharp compileと保存Scene validationを通す。
- PCVR / Quest / iOSで設定ボードPickup、10秒復帰、5方向ミラー、ナイトモード、アラーム、ラジオSpeaker、SAVE ON/OFFの再入室復元を確認する。
- 毎時3分の流星イベントをBuild & Testで確認し、弱い群〜ふたご座極大相当まで出現間隔と同時4 Quad上限を実機評価する。
- 流星群ごとの実際のピーク幅をcatalog化し、開始→極大→終了の三角形近似を置き換える。
- 次年のIMO calendar採用時にcatalogと検証fixtureを更新する。

## Blocked by evidence

- QvPen upstream packageの明示ライセンスを確認する。

## Later

- プロジェクトのScript / Editorコードをasmdefへ分割し、Unity Test Runnerで動くEditModeテストへ移行するか決定する。現状は `Assembly-CSharp` / `Assembly-CSharp-Editor` に直置きで、テスト用asmdefから定義済みアセンブリを参照できないため、Test Runnerでは検査できない。代替として `Tools/Run-LocalChecks.ps1` が `-executeMethod` 経由で同等の検査を実行している。UdonSharpのコンパイル経路へ影響するため、移行時は流星・天球・Udon同期の再検証が要る。
- 月相表現を採用するか決定する。
- 複数流星群の同時放射点演出が必要か実機で評価する。
