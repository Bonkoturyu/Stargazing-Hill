# ADR-0014: 手作業確定配置をversioned layoutとして生成処理へ戻す

- 状態: Accepted
- 決定日: 2026-08-14

## Context

`StargazingWorldBuilder`と局所InstallerはSceneを再生成できる一方、Unity Editor上で自然さを見ながら行ったTransform調整が生成コードの固定値へ戻されないと、再生成やclone先で配置が巻き戻る。Scene YAMLだけを生成入力にすると、再生成対象自身を入力にする循環が生じ、Prefab子へ行った調整も追跡しづらい。

## Options

1. C#の固定座標を手作業ごとに転記する。
2. 保存Sceneだけを正本として、全再生成機能を廃止する。
3. Sceneから明示的にcaptureするversioned layoutを生成正本にする。

## Decision

3を採用する。`Assets/StargazingHill/Editor/Data/PicnicLayout.json`に、8 Scene itemのAnchor world Transformと各`Model`子のlocal Transformを保存する。`Save Current Scene Layout to Generator...`は現在の保存Sceneをcaptureし、`Rebuild from Saved Layout...`、局所feature update、全Scene再生成は同じJSONを読む。

通常のclone利用者は依存復元後に保存Sceneを開けばよく、全再生成を必須にしない。破壊的な全再生成と局所置換には確認dialogを出す。配布用unitypackageにはScene、復元ガイド、layout JSONを含め、YamaPlayer、QvPen、UnyStylus本体は含めない。

## Consequences

- 手作業の見た目と生成結果を同じversioned入力から再現できる。
- 今後の手修正ではScene保存後にcaptureメニューを実行し、SceneとJSONを一緒にcommitする必要がある。
- 新しい配置対象やschema変更時は`schemaVersion`、Installer、静的検査、復元手順を同時更新する。
- Unity batch検証ではlayoutと再生成後Transformの一致を確認する。Editor licenseが利用できない環境では静的検査までをPassとし、Unity再生成は`Pending Evidence`として残す。
