# ADR-0003: CC0環境素材、単一地形Collider、2種類のペン

- 状態: Accepted
- 決定日: 2026-08-11

## Context

初期のプロシージャル草地と木は草原としての質感が不足し、丘の地面Colliderと見た目用Colliderが重なっていたため、プレイヤーが埋まる・登れない問題が発生した。また、QvPenに加えて購入済みUnyStylus v1.3の併設が要求された。

## Options

- 既存の単色プロシージャル環境を調整する。
- 権利が確認できるCC0素材へ置き換え、地形Colliderを一本化する。
- QvPenまたはUnyStylusの一方だけを配置する。
- 両方を配置し、負荷と操作競合を実機検証する。

## Decision

- 地表はPoly Haven `Leafy Grass`、木はQuaternius `Textured LowPoly Trees` のCC0素材を採用する。
- 丘の形状を草原Meshへ統合し、歩行面のMeshColliderを1つにする。丘オーバーレイは描画専用とする。
- ジャンプと移動速度をUdonSharpで明示設定する。
- QvPen 3.3.15とUnyStylus v1.3を主景観外へ併設する。
- YamaPlayer、QvPen、UnyStylusをスポーン背後の1つの設備エリアへ集約する。
- 木のFBXルートが持つZ-upからY-upへのimport変換は保持し、外側Anchorだけで配置する。
- VRCSceneDescriptorと同じGameObjectにPipelineManagerを明示してSDK Builderから認識可能にする。
- QvPenは公式VPMから復元し、購入品のUnyStylus本体はGitへ含めない。

## Consequences

草原とランドマークの品質、丘の歩行安全性、ペン選択肢が改善する。一方で、2種類のペンによる描画・同期・UI負荷は増えるため、PC / Android / iOSの実機測定を残す。UnyStylusを持たないclone環境では購入済みv1.3をインポートするまでPrefab参照が欠落する。
