# 第三者依存パッケージ

状態: `In Progress`

`Packages/manifest.json` と `Packages/vpm-manifest.json` を技術上の正本とし、この文書には採用理由、配布元、ライセンス、対応版の証拠を記録する。Unity標準モジュールは個別列挙せず、追加VPM/packageを中心に管理する。

| 依存 | 版 | 用途 | 配布元・ライセンス | 状態 |
|---|---|---|---|---|
| VRChat SDK Base / Worlds | manifest参照 | VRChatワールド | 公式配布元を実装着手時に記録 | Pending Evidence |
| UdonSharp | manifest参照 | ワールドロジック | 公式配布元を実装着手時に記録 | Pending Evidence |
| VRCWorldToolkit | 3.4.1 | Editor検証支援 | 配布元、ライセンス、採用理由を記録 | Pending Evidence |
| AudioLink | 3.1.2 | 音声連動表現 | VPM manifestで固定。権利証拠の追記はOpen | Provisional |
| QvPen | 未確定 | 描画 | 配布元、版、ライセンスを比較 | Open |
| YamaPlayer (`net.kwxxw.yama-stream`) | 2.0.0-beta.7 | 動画・音声再生 | 公式VPM `https://vpm.kwxxw.net/index.json` / 公式GitHub `koorimizuw/YamaPlayer`。READMEの利用条件に従い、VRChatワールドへの改変・組込み可。取得日 2026-08-11、package ZIP SHA-256 `fcf94198526e63319ebde966a479f4be7179c58d22f7e54493d9e75034ae2e9d`。VPM依存として解決しpackage本体は追跡しない | Confirmed |
