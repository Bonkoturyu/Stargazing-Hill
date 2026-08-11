# 星見の丘 / Stargazing Hill

VRChat向けの静かな星空・草原ワールド。

短い芝生の草原、小さな丘と一本木、淡い固定照明の中で、現実の東京の現在時刻に連動した星空と月を眺めることを主題とする。

## コア要素

- 東京の現在時刻に連動する実在星空
- 現在時刻に連動する月の位置
- 毎時00分の流星演出
- 国立天文台の「主な流星群」11群をデータとして扱う流星群システム
- 短い芝生の草原
- 小さな丘と一本木
- 淡い固定照明 / 月明かり風の固定照明
- 動画プレイヤー
- QvPen
- PC / Quest(Android) / iOS対応を前提とした軽量設計

## ドキュメント

- [docs/README.md](docs/README.md) — ドキュメント全体の案内
- [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) — ワールド全体仕様
- [docs/REAL_SKY_SYSTEM.md](docs/REAL_SKY_SYSTEM.md) — 星空・月・流星群の技術仕様

## 方針

星空は、既存プロジェクト `VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` で実績のある「HYG星表 → 極小Quad → 1 Mesh → Additive Unlit Shader」の考え方を再利用し、ランタイムでは星を個別更新せず天球全体を回転させる。

照明そのものは天体位置に追従させない。星と月の見た目上の位置だけを現実時刻に連動させる。

## ステータス

CC0テクスチャの草原、単一Colliderの小丘、CC0テクスチャ付き一本木、明示的なジャンプ設定、HYG v4.1の全天球星空、東京の現在時刻による天球回転、YamaPlayer 2.0.0-beta.7と距離減衰、QvPen 3.3.15、購入済みUnyStylus v1.3まで実装済み。月、流星群、各対象プラットフォームでの実機検証は未実装・未実施。

Unityライセンスが有効なEditorでプロジェクトを開き、メニュー `Stargazing Hill/Build Complete World` を実行すると、Mesh・Materialと `Assets/StargazingHill/Scenes/StargazingHill.unity` を生成する。生成後はConsoleとUdon変換結果を確認する。
