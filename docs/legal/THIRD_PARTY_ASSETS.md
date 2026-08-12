# 第三者アセット・データセット

第三者素材は証拠が `Confirmed` になるまでリポジトリへ取り込まない。詳細な帰属とhashは各素材に隣接するNOTICEを正本とする。

| アセット | 用途 | 必要な証拠 | 状態 |
|---|---|---|---|
| HYG Stellar Database v4.1 | 星Mesh生成 | David Nash / Astronexus、GitHub archive commit `c7f7f883fe678cc7680169a50ccd7dcc49b060ce`、CC BY-SA 4.0、取得日・原本/派生hash・加工手順は `Assets/StargazingHill/Editor/Data/NOTICE.md` | Confirmed |
| IMO Meteor Shower Calendar 2026 | 主要11群の活動期間、極大日、放射点、ZHR | International Meteor Organization、https://imo.net/files/meteor-shower/cal2026.pdf、確認日2026-08-12、Table 5の事実値のみを転記。PDF本文・図表は再配布しない。`IMO2026MajorShowers.asset` と静的fixtureを同時更新 | Confirmed |
| Poly Haven `Leafy Grass` | 地表diffuse / normal | Charlotte Baglioni、CC0 1.0、取得元・取得日・2ファイルのSHA-256は隣接 `NOTICE.md` | Confirmed |
| Poly Haven `Jacaranda Tree` | 一本木の派生Mesh、枝・幹・葉の1K diffuse / normal / alpha | Rico Cilliers、Rob Tuytel、CC0 1.0。原本FBX/API hash、連結部品単位の軽量化工程、派生Meshと7テクスチャのSHA-256は隣接 `NOTICE.md` | Confirmed |
| 立体草・地形Mesh | 環境表現 | リポジトリ内Editorコードで単一Meshへ生成。第三者Meshなし | Confirmed |
| YamaPlayer関連素材 | 機能UI | package側の権利記録は `THIRD_PARTY_DEPENDENCIES.md` | Confirmed |
| QvPen関連素材 | 機能UI | 公式VPM packageとして復元し、package本体は追跡しない | Provisional |
| UnyStylus関連素材 | 機能UI | 購入済みv1.3をローカル導入。本体は `.gitignore` 対象で再配布しない | Confirmed |

新規素材には [アセット受入テンプレート](../templates/ASSET_INTAKE_TEMPLATE.md) を使用する。
