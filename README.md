# 星見の丘 / Stargazing Hill

VRChat向けの静かな星空・草原ワールド。

短い芝生の草原、小さな丘と一本木、淡い固定照明の中で、現実の東京の現在時刻に連動した星空と月を眺めることを主題とする。

## コア要素

- Tokyo profileの現在時刻に連動し、緯度・東経の差替えに対応する実在星空
- 現在時刻に連動する月の位置
- 毎時00分の流星演出
- IMO 2026 Calendarの主要11群をデータとして扱う流星群システム
- 短い芝生の草原
- 小さな丘と一本木
- 淡い固定照明 / 月明かり風の固定照明
- 動画プレイヤー
- QvPen
- PC / Quest(Android) / iOS対応を前提とした軽量設計
- 日英切替のワールド説明、現在人数、ローカル入退室履歴パネル

## ドキュメント

- [docs/README.md](docs/README.md) — ドキュメント全体の案内
- [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) — ワールド全体仕様
- [docs/REAL_SKY_SYSTEM.md](docs/REAL_SKY_SYSTEM.md) — 星空・月・流星群の技術仕様

## 方針

星空は、既存プロジェクト `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` で実績のある「HYG星表 → 極小Quad → 1 Mesh → Additive Unlit Shader」の考え方を再利用し、ランタイムでは星を個別更新せず天球全体を回転させる。

照明そのものは天体位置に追従させない。星と月の見た目上の位置だけを現実時刻に連動させる。

## ステータス

CC0テクスチャの±250m草原、単一Colliderの小丘、Poly Haven `Jacaranda Tree` のCC0一本木、明示的なジャンプ設定、HYG v4.1の全天球星空、地平線の空気遠近、差替可能な観測地、topocentric月位置、IMO 2026主要11流星群、状態表示付き毎時流星イベント、runtime Playlistを保存するYamaPlayer 2.0.0-beta.7、QvPen 3.3.15、購入済みUnyStylus v1.3、日英説明・人数・入退室履歴まで実装済み。月位置は東京のUSNO基準5日時で高度・方位とも0.10°以内。ClientSimとPC / Quest / iOS実機の最終検証は未完了。

Unityライセンスが有効なEditorでプロジェクトを開き、メニュー `Stargazing Hill/Build Complete World` を実行すると、Mesh・Materialと `Assets/StargazingHill/Scenes/StargazingHill.unity` を生成する。生成後はConsoleとUdon変換結果を確認する。

配布用unitypackageは `Stargazing Hill/Export/Redistributable UnityPackage...` から生成する。この経路はYamaPlayer、QvPen、購入品UnyStylusを同梱しない。

クリーン環境からの依存・購入品復元は [docs/SETUP_AND_RESTORE.md](docs/SETUP_AND_RESTORE.md) を参照する。
