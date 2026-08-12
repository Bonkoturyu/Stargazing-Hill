# 第三者依存パッケージ

状態: `In Progress`

`Packages/manifest.json` と `Packages/vpm-manifest.json` を技術上の正本とし、この文書には採用理由、配布元、ライセンス、対応版の証拠を記録する。Unity標準モジュールは個別列挙せず、追加VPM/packageを中心に管理する。

| 依存 | 版 | 用途 | 配布元・ライセンス | 状態 |
|---|---|---|---|---|
| VRChat SDK Base / Worlds | 3.10.4 | VRChatワールド | VRChat公式VPM package。各packageの `license.txt` は https://hello.vrchat.com/legal/sdk を参照。取得済みpackage、manifest、公式release `https://github.com/vrchat/packages/releases/tag/3.10.4` を2026-08-12に照合。ClientSimのUdon networking初期化順序に対する局所guardは [ADR 0006](../adr/0006-vrcsdk-3104-clientsim-networking-guard.md) に記録し、package本体は追跡しない | Confirmed |
| UdonSharp | VRChat Worlds 3.10.4内蔵 | ワールドロジック | `com.vrchat.worlds/Integrations/UdonSharp` として復元。上流 https://github.com/vrchat-community/UdonSharp 、MIT License、確認日2026-08-12 | Confirmed |
| VRCWorldToolkit | 3.4.1 | Editor検証支援 | https://github.com/oneVR/VRWorldToolkit 、MIT License。package内 `LICENSE` と `licensesUrl` を2026-08-12に照合 | Confirmed |
| AudioLink | 3.1.2 | 音声連動表現 | https://github.com/llealloo/audiolink 、MIT License。package内 `LICENSE` を2026-08-12に照合。package本体はVPMで復元 | Confirmed |
| QvPen (`net.ureishi.qvpen`) | 3.3.15 | 描画 | 公式GitHub `ureishi/QvPen` / 公式VPM `https://vpm.ureishi.net/repos.json`。Unity 2022.3、`com.vrchat.worlds ^3.5.0`。取得ZIP SHA-256 `220df71a5fb5540ac7c9d75a7f40b57d69aa6aa432db3ddbc7563e0d60606537`。上流packageに明示ライセンスファイルがないため本体は追跡せず公式VPMから復元 | Provisional |
| UnyStylus | 1.3 | 描画 | 購入済みUnityPackage。VN3利用条件に従いワールドへ組込み、packageと展開本体はGitへ再配布しない。シーン参照の復元には購入済みv1.3のローカルimportが必要 | Confirmed |
| YamaPlayer (`net.kwxxw.yama-stream`) | 2.0.0-beta.7 | 動画・音声再生 | 公式VPM `https://vpm.kwxxw.net/index.json` / 公式GitHub `koorimizuw/YamaPlayer`。READMEの利用条件に従い、VRChatワールドへの改変・組込み可。取得日 2026-08-11、package ZIP SHA-256 `fcf94198526e63319ebde966a479f4be7179c58d22f7e54493d9e75034ae2e9d`。VPM依存として解決しpackage本体は追跡しない | Confirmed |
