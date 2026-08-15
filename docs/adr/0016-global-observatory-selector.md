# ADR 0016: 観測地点番号だけをGlobal同期する

- 状態: Accepted
- 決定日: 2026-08-14

## Context

説明パネルから複数都市の星空へ切り替えたい。観測地点は全員が同じ空を見るためのWorld状態であり、ユーザーごとのローカル表示にはしない。一方、星、月、流星のTransformを直接同期すると通信量が増え、既存のネットワークUTCによる決定的計算とも重複する。

## Decision

- `WorldObservatorySelector` をManual Syncとし、22地点catalogの `selectedIndex` だけを `[UdonSynced]` にする。
- 操作者はselectorのOwnershipを取得して番号を更新し、即時ローカル適用後に `RequestSerialization()` する。
- 受信側と途中参加者は番号からversioned catalogの緯度・東経を読み、`RealSkyController` と `MeteorController` へ適用する。
- 星空回転と月位置は切替時に再計算し、流星放射点は次のruntime評価から新地点を使う。急な見た目の切替を許容し、補間しない。
- 誰でも変更できるLast-writer-winsとする。Global操作であることは見出しとInteractionへ `(global)` と明記する。
- 見出し、選択中ラベル、22地点タイルの名称は、説明本文と同じローカル言語indexに従い、日本語 / English / 繁體中文 / 简体中文 / 한국어のversioned catalogから表示する。名称と言語indexは同期しない。
- リスト開閉、デバッグパネル表示、言語、履歴スクロールは同期しない。
- catalog順のTokyo=0からSeoul=19は同期プロトコルとして維持する。2026-08-15追加のTottori=20、Matsue (Shimane)=21は末尾へ置く。選択一覧だけを逆順の3列タイルとして上方向へ展開し、視覚順と同期indexを分離する。

## Consequences

- 送信するのは整数1つだけで、全員が同じ地点から見た星、月、流星を共有できる。
- 天体Transformは従来どおり各クライアントがNetwork UTCから計算するため、継続的な同期は不要。
- 同時操作時は表示が後から受理された地点へ急に変わる。これは要求上許容する。
- catalog順序は同期プロトコルになるため、公開後に既存indexの意味を入れ替えない。追加・変更時はversion管理と回帰試験が必要になる。
- 一覧の並べ方を変更しても同期値の意味は変わらない。ボタンは表示位置とは別に元のcatalog indexを保持する。
- 同じGlobal地点を選んでいる利用者同士でも、地点名は各自が選んだ言語で表示できる。翻訳追加や表記修正は同期プロトコルを変えない。

## Evidence

- `Assets/StargazingHill/Scripts/WorldObservatorySelector.cs`
- `Assets/StargazingHill/Editor/WorldInformationPanelInstaller.cs`
- `Tools/Validate-StargazingImplementation.py`
- `docs/TEST_PLAN.md` の「2026-08-14 Global観測地点セレクター」
