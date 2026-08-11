# Handoff

## 現在地

- 仕様の正本は `docs/PROJECT_SPEC.md` と `docs/REAL_SKY_SYSTEM.md`。
- HYG v4.1の12,495星、全天球Mesh Baker、Shader、東京時刻天球回転を実装。
- 自作プロシージャルMeshによる草原、小丘、一本木、固定照明のEditorビルダーを実装。
- YamaPlayer 2.0.0-beta.7、VideoInfoDownloader、参照ワールドと同じ8点距離減衰を実装。
- データ再現性、C#/UdonSharpコンパイル、Unityシーン生成、保存後参照検証、Direct3DプレビューはPass。

## 次の安全な一手

1. ClientSimで星空の現在時刻追従とYamaPlayer再生・同期を確認する。
2. PC、Quest(Android)、iOSで星空、YamaPlayer、距離減衰、負荷を確認する。
3. 月、流星群、QvPenの順に残りMVPを実装する。

一時的な作業状況だけをここへ置き、仕様判断は必ず該当する正本またはADRへ反映する。
