# 星見の丘 / Stargazing Hill

[日本語](README.md) | [English](README.en.md) | [繁體中文](README.zh-Hant.md) | [简体中文](README.zh-Hans.md) | [한국어](README.ko.md)

VRChat向けの静かな星空・草原ワールド。

短い芝生の草原、小さな丘と一本木、淡い固定照明の中で、全員で共有する22か所の観測地点と現実の時刻に連動した星空と月を眺めることを主題とする。

## コア要素

- 22か所からGlobal選択した観測地点の緯度・東経と現実の時刻に連動する実在星空
- 現在時刻に連動する月の位置
- 毎時00分の流星演出
- IMO 2026 Calendarの主要11群をデータとして扱う流星群システム
- 短い芝生の草原
- 小さな丘と一本木
- CC0素材による木陰のピクニックスポット
- 淡い固定照明 / 月明かり風の固定照明
- 動画プレイヤー
- QvPen
- PC / Quest(Android) / iOS対応を前提とした軽量設計
- 日本語・英語・繁体字・簡体字・韓国語切替のワールド説明、現在人数、ローカル入退室履歴パネル
- 木のそばの手持ちローカル設定ボード（5方向ミラーの個別ON/OFFと共通LQ/HQ、ナイトモード、日時・アラーム、ラジオ音声ON/OFF・音量、入退室の通知音と表示、任意保存）
- 星空の北をローカルに指す、持ち運び可能なCC0方位磁石

## ドキュメント

- [docs/README.md](docs/README.md) — ドキュメント全体の案内
- [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) — ワールド全体仕様
- [docs/REAL_SKY_SYSTEM.md](docs/REAL_SKY_SYSTEM.md) — 星空・月・流星群の技術仕様
- [docs/STARFIELD_IMPLEMENTATION_GUIDE.md](docs/STARFIELD_IMPLEMENTATION_GUIDE.md) — 図解付き「星空の作り方」（簡易説明→詳細説明）

## 方針

星空は、非公開の既存VRChatプロジェクトで実績のある「HYG星表 → 極小Quad → 1 Mesh → Additive Unlit Shader」の考え方を再利用し、ランタイムでは星を個別更新せず天球全体を回転させる。

照明そのものは天体位置に追従させない。星と月の見た目上の位置だけを現実時刻に連動させる。

## ステータス

CC0テクスチャの±250m草原、単一Colliderの小丘、CC0の一本木・ピクニックスポット・手持ち方位磁石、HYG v4.1の全天球星空、地平線の空気遠近と大気消散、Global共有の22観測地点、topocentric月位置、IMO 2026主要11流星群、状態表示付き毎時流星イベント、YamaPlayer 2.0.0-beta.7、QvPen 3.3.15、購入済みUnyStylus v1.3、5言語UI、ローカル設定ボードまで実装済み。方位磁石の針は各クライアントで星空の北を指す。月位置は東京のUSNO基準5日時で高度・方位とも0.10°以内。新しい設定ボードと方位磁石のUnityコンパイル・保存Scene構造検証は完了し、PCVR / Android / iOS実機検証は未完了。

clone後はVCC/VPM依存と購入済みUnyStylusを復元してから、保存済みの `Assets/StargazingHill/Scenes/StargazingHill.unity` を開く。通常利用ではSceneの全再生成は不要。詳しい順序は [docs/SETUP_AND_RESTORE.md](docs/SETUP_AND_RESTORE.md) を参照する。

初回importが完了したら、`Stargazing Hill/Validate Saved Scene`で保存Sceneを検証する。

Unityメニューは用途別に整理している。

- `Stargazing Hill/Content`: 説明パネルの選択・検証・配置調整とピクニック配置の保守
- `Stargazing Hill/Preview & Debug`: 星空・流星のローカル確認
- `Stargazing Hill/Build & Export`: 第三者依存を除いた配布用unitypackage
- `Stargazing Hill/Advanced`: 生成物の置換、全Scene再生成、SDK診断。内容を理解した保守者向け

全Sceneを作り直す場合だけ `Stargazing Hill/Advanced/Generated Content/Rebuild Complete World (Destructive)...` を使う。ピクニックの生成配置は `Assets/StargazingHill/Editor/Data/PicnicLayout.json` が正本で、Scene上の手修正は `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...` から正本へ保存できる。

YamaPlayerのPlaylistはYamaPlayer標準のInspectorにある「プレイリストを編集する」、または `YamaPlayer/Edit Playlist` から編集する。独自のPlaylist設定ファイルや自動同期は使用しない。全Scene再生成時も、保存Sceneにある標準エディター編集済みYamaPlayerを引き継ぐ。

配布用unitypackageは `Stargazing Hill/Build & Export/Redistributable UnityPackage...` から生成する。この経路はYamaPlayer、QvPen、購入品UnyStylusを同梱しない。

クリーン環境からの依存・購入品復元は [docs/SETUP_AND_RESTORE.md](docs/SETUP_AND_RESTORE.md) を参照する。

## GitHub Release

`v*` tagをpushするとGitHub Actionsが再配布用unitypackageを生成・検査し、SHA-256一覧とともにZIP化してReleaseへ添付する。Unity EditorやUnity licenseは不要。YamaPlayer、QvPen、購入品UnyStylusは含まれない。Actions runnerの稼働は確認済みで、tagによる最初のRelease実行は `Pending Evidence`。公開監査は [docs/PUBLIC_RELEASE_AUDIT.md](docs/PUBLIC_RELEASE_AUDIT.md) を参照する。
