# ドキュメント案内

## 正本

- [PROJECT_SPEC.md](PROJECT_SPEC.md): ワールド全体の要件とMVP
- [REAL_SKY_SYSTEM.md](REAL_SKY_SYSTEM.md): 星空、月、流星群の技術仕様
- [STARFIELD_IMPLEMENTATION_GUIDE.md](STARFIELD_IMPLEMENTATION_GUIDE.md): 図解、仕組み、座標変換、恒星時、Shader式を簡易説明から詳細説明の順で解説
- [VRCHAT_IMPLEMENTATION_GUIDE.md](VRCHAT_IMPLEMENTATION_GUIDE.md): Unity / VRChat実装の注意事項
- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md): 実装順序と完了条件
- [TEST_PLAN.md](TEST_PLAN.md): 検証方針とテスト記録
- [SETUP_AND_RESTORE.md](SETUP_AND_RESTORE.md): クリーンclone、VPM依存、購入品UnyStylusの復元手順
- [WORLD_DESCRIPTION.md](WORLD_DESCRIPTION.md): VRChat SDKへ貼り付ける日英Descriptionの正本
- [PUBLIC_RELEASE_AUDIT.md](PUBLIC_RELEASE_AUDIT.md): Public化前の秘密情報・権利・Blueprint・Release artifact監査
- [BOOTH_RELEASE_GUIDE.md](BOOTH_RELEASE_GUIDE.md): BOOTH向け配布範囲、依存明示、顧客向けZIP作成、販売前確認
- [RISKS_AND_OPEN_QUESTIONS.md](RISKS_AND_OPEN_QUESTIONS.md): 未解決事項とリスク

## 運用と証拠

- [AI_COLLABORATION.md](AI_COLLABORATION.md): AI協業とSubAgent運用
- [CROSS_REPOSITORY_KNOWLEDGE_AUDIT.md](CROSS_REPOSITORY_KNOWLEDGE_AUDIT.md): 参考リポジトリからの採否
- [adr/README.md](adr/README.md): Architecture Decision Records
- [legal/THIRD_PARTY_DEPENDENCIES.md](legal/THIRD_PARTY_DEPENDENCIES.md): package / SDK依存
- [legal/THIRD_PARTY_ASSETS.md](legal/THIRD_PARTY_ASSETS.md): 素材とデータセットの権利記録
- [templates/](templates/): ADRとアセット受入記録のテンプレート

## GitHubの入口

リポジトリ直下のREADMEは、日本語、英語、繁体字中国語、簡体字中国語、韓国語の5言語を同じ実装状況へ同期する。技術仕様、試験証拠、ADR、権利記録は翻訳ごとに分岐させず、この `docs/` 配下の日本語正本へ集約する。

- [日本語](../README.md)
- [English](../README.en.md)
- [繁體中文](../README.zh-Hant.md)
- [简体中文](../README.zh-Hans.md)
- [한국어](../README.ko.md)

状態ラベルは `Confirmed`、`Provisional`、`Open`、`Out of scope`、`Pending Evidence` を使用する。
