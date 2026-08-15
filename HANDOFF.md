# Handoff

更新日: 2026-08-15

## 現在地

- 仕様の正本は `docs/PROJECT_SPEC.md` と `docs/REAL_SKY_SYSTEM.md`。公開入口は5言語のルートREADME、VRChat SDK用Descriptionは `docs/WORLD_DESCRIPTION.md` を正本とする。
- HYG v4.1の12,495星を1 Meshへベイクし、肉眼限界6.8等級、地平線の空気遠近、`0.23 mag/airmass`の大気消散を実装済み。
- VRChatの共通時刻とGlobal同期された22観測地点から、星空、topocentric月位置、流星群放射点を各クライアントで決定的に再現する。Tokyo=0〜Seoul=19を維持し、鳥取=20・松江/島根=21を末尾追加した。
- IMO 2026主要11群と散在流星、毎時180秒イベント、25秒・20本のローカルDebugプレビューを実装済み。
- ±250mの草原、単一Colliderの小丘、Poly HavenのCC0一本木、versioned layoutで再生成できるTiny TreatsのCC0ピクニックスポットを実装済み。敷物だけに静的MeshColliderを追加する生成コードへ更新した。
- 説明パネルは日本語初期表示で5言語対応。Global観測地点、現在人数、PC/Mobile内訳、ローカル入退室履歴、Debug表示切替を備え、直接USEとVRレーザーを併用する。Debugパネルも5言語、Pickup、ドロップ10秒後復帰に対応する。
- 木陰のローカル設定ボード、5方向ミラー、ナイトモード、日時・アラーム、YamaPlayerラジオSpeaker、任意PlayerData保存を生成コードへ実装した。Unity 2022.3.22f1通常Editor相当経路で保存Scene再生成、C# / UdonSharp compile、構造validationまでPass。
- YamaPlayer 2.0.0-beta.7は標準Playlist Editorを正本とし、QvPen 3.3.15と購入済みUnyStylus v1.3を含む外部依存は配布用unitypackageへ同梱しない。
- 既存機能と2026-08-15追加機能のUdonSharp/C# compile、保存Scene生成・構造検証はPass。新しい設定UIのClientSim / PCVR / Quest / iOS操作と負荷はPending Evidence。詳細は `docs/TEST_PLAN.md` に集約する。

## 次の安全な一手

1. ClientSim複数人でGlobal観測地点の同期・途中参加、人数内訳、履歴スクロール、5言語切替、設定ボードのローカル性を確認する。
2. YamaPlayer標準Editorで保存した4 Playlist / 15 TrackとAutoPlay、ラジオSpeakerをClientSim、Windows、Android、iOSの新buildで再確認する。
3. PCVR、Android/standalone VR、iOSで星・月・流星、UI、動画・音声、描画ペン、両Pickup、ミラー、ナイトモード、アラーム、PlayerData復元、性能を確認し、結果を `docs/TEST_PLAN.md` へ追記する。
4. VRChat SDKの残存警告を第三者依存・対応可能・実機確認対象に分類して公開候補判定を行う。

一時的な作業状況だけをここへ置き、仕様判断は必ず該当する正本またはADRへ反映する。
