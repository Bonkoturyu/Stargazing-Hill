# 第三者アセット・データセット

第三者素材は証拠が `Confirmed` になるまでリポジトリへ取り込まない。詳細な帰属とhashは各素材に隣接するNOTICEを正本とする。

| アセット | 用途 | 必要な証拠 | 状態 |
|---|---|---|---|
| HYG Stellar Database v4.1 | 星Mesh生成 | David Nash / Astronexus、GitHub archive commit `c7f7f883fe678cc7680169a50ccd7dcc49b060ce`、CC BY-SA 4.0、取得日・原本/派生hash・加工手順は `Assets/StargazingHill/Editor/Data/NOTICE.md` | Confirmed |
| IMO Meteor Shower Calendar 2026 | 主要11群の活動期間、極大日、放射点、ZHR、`V∞`、`r` | International Meteor Organization、https://imo.net/files/meteor-shower/cal2026.pdf、確認日2026-08-12、Table 5の事実値のみを転記。PDF本文・図表は再配布しない。`IMO2026MajorShowers.asset` と静的fixtureを同時更新 | Confirmed |
| Poly Haven `Leafy Grass` | 地表diffuse / normal | Charlotte Baglioni、CC0 1.0、取得元・取得日・2ファイルのSHA-256は隣接 `NOTICE.md` | Confirmed |
| Poly Haven `Jacaranda Tree` | 一本木の派生Mesh、枝・幹・葉の1K diffuse / normal / alpha | Rico Cilliers、Rob Tuytel、CC0 1.0。原本FBX/API hash、連結部品単位の軽量化工程、樹冠をフロンドカード化するQuest向け再ベイク工程、出荷Mesh `Jacaranda_Quest.asset` と7テクスチャのSHA-256は隣接 `NOTICE.md`。中間Meshは原本FBXと同じく追跡しないローカル入力 | Confirmed |
| Tiny Treats `Pleasant Picnic 1.0` | 一本木の下の青い敷物、ラジオ、ティーポット、マグ、青系クッション2点 | Isa Lousberg / Tiny Treats、CC0-1.0。Godot Asset Library掲載、上流commit、同梱9ファイルのSHA-256、選定内容、軽量化判断は `Assets/StargazingHill/ThirdParty/TinyTreats/PleasantPicnic/NOTICE.md` | Confirmed |
| OpenGameArt `Compass PBR (Unity) CC0` | 木陰の持ち運べる方位磁石 | Lucian Pavel、CC0-1.0。配布URL、確認日2026-08-15、原本ZIP/FBX/texture hash、512 px派生手順は `Assets/StargazingHill/ThirdParty/OpenGameArt/Compass/NOTICE.md` | Confirmed |
| Noto Sans CJK KR Regular | 説明パネル・Debugパネルの日本語、英語、繁体字、簡体字、韓国語共通フォント | notofonts / Google、SIL Open Font License 1.1。公式 [`notofonts/noto-cjk`](https://github.com/notofonts/noto-cjk) の [`Sans/OTF/Korean/NotoSansCJKkr-Regular.otf`](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf) と[ライセンス](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)を2026-08-14確認・取得。SHA-256 `6bcb2a0703aa137e874fc2dffa85f6c21ba9a67fa329e81b8c801663af7e992a`。ライセンス本文は隣接 `LICENSE-NotoSansCJK.txt` | Confirmed |
| 立体草・地形Mesh | 環境表現 | リポジトリ内Editorコードで単一Meshへ生成。第三者Meshなし | Confirmed |
| YamaPlayer関連素材 | 機能UI | package側の権利記録は `THIRD_PARTY_DEPENDENCIES.md` | Confirmed |
| QvPen関連素材 | 機能UI | 公式VPM packageとして復元し、package本体は追跡しない | Provisional |
| UnyStylus関連素材 | 機能UI | 購入済みv1.3をローカル導入。本体は `.gitignore` 対象で再配布しない | Confirmed |

新規素材には [アセット受入テンプレート](../templates/ASSET_INTAKE_TEMPLATE.md) を使用する。
