# 第三者アセット・データセット

第三者素材は証拠が `Confirmed` になるまでリポジトリへ取り込まない。詳細な帰属とhashは各素材に隣接するNOTICEを正本とする。

| アセット | 用途 | 必要な証拠 | 状態 |
|---|---|---|---|
| HYG Stellar Database v4.1 | 星Mesh生成 | David Nash / Astronexus、GitHub archive commit `c7f7f883fe678cc7680169a50ccd7dcc49b060ce`、CC BY-SA 4.0、取得日・原本/派生hash・加工手順は `Assets/StargazingHill/Editor/Data/NOTICE.md` | Confirmed |
| 流星群データ | 活動期間・放射点 | 一次出典URL、確認日、対象年、利用条件、加工手順 | Pending Evidence |
| Poly Haven `Leafy Grass` | 地表diffuse / normal | Charlotte Baglioni、CC0 1.0、取得元・取得日・2ファイルのSHA-256は隣接 `NOTICE.md` | Confirmed |
| Quaternius `Textured LowPoly Trees` / `Tree_3` | 一本木FBX・樹皮・葉 | Quaternius、CC0 1.0、配布元・archive SHA-256・採用ファイルは隣接 `NOTICE.md` | Confirmed |
| 立体草・地形Mesh | 環境表現 | リポジトリ内Editorコードで単一Meshへ生成。第三者Meshなし | Confirmed |
| YamaPlayer関連素材 | 機能UI | package側の権利記録は `THIRD_PARTY_DEPENDENCIES.md` | Confirmed |
| QvPen関連素材 | 機能UI | 公式VPM packageとして復元し、package本体は追跡しない | Provisional |
| UnyStylus関連素材 | 機能UI | 購入済みv1.3をローカル導入。本体は `.gitignore` 対象で再配布しない | Confirmed |

新規素材には [アセット受入テンプレート](../templates/ASSET_INTAKE_TEMPLATE.md) を使用する。
