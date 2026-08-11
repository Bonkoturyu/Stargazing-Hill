# Stargazing Hill エージェントガイド

このファイルはリポジトリ全体に適用する。詳細は `docs/` の正本へ委譲し、ここには作業規約だけを置く。

## 最初に読むもの

1. `README.md`
2. `docs/README.md`
3. `docs/PROJECT_SPEC.md`
4. 対象機能の仕様書とADR
5. `docs/AI_COLLABORATION.md`

## 記録と根拠

- 状態は `Confirmed`、`Provisional`、`Open`、`Out of scope`、`Pending Evidence` のいずれかで明示する。
- 同じ事実を複数の文書で定義しない。正本を一つ決め、他からリンクする。
- 外部仕様にはURL、確認日、対象バージョンを添える。第三者アセットは権利記録が `Confirmed` になるまで導入しない。
- 大きな技術判断は `docs/adr/`、未解決事項は `docs/RISKS_AND_OPEN_QUESTIONS.md`、作業候補は `BACKLOG.md` に記録する。
- 記録更新では `.agents/skills/maintain-world-records/` を使う。

## Unity / VRChat

- Unityアセットと対応する `.meta` は常に対で扱う。GUIDを維持し、既存 `.meta` を不用意に再生成しない。
- `Library/`、`Temp/`、`Logs/`、`Obj/`、ビルド出力、UdonSharp生成物、SDKサンプルはコミットしない。
- `.unity`、`.prefab`、Project Settingsは差分の意味を説明できる場合だけ変更する。
- 複数作業者が同じ `.unity` または `.prefab` を同時編集しない。Unity Editorを開く作業者も一人に限定する。
- UdonSharpで利用できるAPIと型の制約を先に確認する。ネットワーク時刻から決定的に再現できる状態は同期変数にしない。
- PCだけで完了扱いにしない。Quest(Android)とiOSの制約を設計段階から扱う。

## 作業と検証

- 変更前に現在のブランチ、差分、対象ファイルを確認する。利用者の既存変更を混ぜたり破棄したりしない。
- 複雑・高リスク・複数領域の作業では `.agents/skills/hard-task-protocol/` を使う。
- 検証は小さい順に、静的確認、対象テスト、Unity/ClientSim、実機ビルドを選ぶ。実行できない検証は未実施と明記する。
- GitHubへのpush、PR、mergeなど外部状態の変更は、利用者が明示的に依頼した範囲だけで行う。

## SubAgent

- SubAgentは利用者が明示的に依頼または許可した場合だけ使う。最大1体、深さ1までとする。
- 親は要件、設計、編集範囲、統合、最終検証、採否判断を保持する。
- 委譲時は目的、出力、読取/編集範囲、制約、検証、報告形式を明記する。
- SubAgentにcommit、push、PR、mergeをさせない。
- シーン/Prefab編集は明示された単一対象に限定し、同時編集がないことを確認する。
