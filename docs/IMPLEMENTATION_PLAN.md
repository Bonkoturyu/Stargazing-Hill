# 実装計画

状態: `In Progress`

## Phase 0: 基盤

- [x] Unity / VRChat SDK / UdonSharpの版を固定する（Unity 2022.3.22f1、VRChat SDK 3.10.4）。
- [x] HYG、Poly Haven、Tiny Treats、YamaPlayer、QvPen、UnyStylusの依存・権利記録を整える。
- [ ] 空の基準シーンでPC・Android・iOS向けBuild & Test経路を確認する。

完了条件: package lock、権利記録、各プラットフォームの既知制約が追跡できる。

## Phase 1: 草原MVP

- [x] CC0素材による芝生地面、単一Colliderの丘、Poly Haven Jacaranda一本木、固定照明を作成する。
- [x] Tiny TreatsのCC0ピクニック用品6点を一本木の下へ配置し、敷物だけに静的MeshCollider、小物には物理Colliderなしのモバイル共通構成にする。
- [x] 動画プレイヤーとしてYamaPlayer 2.0.0-beta.7を採用する。
- [x] YamaPlayer、QvPen 3.3.15、購入済みUnyStylus v1.3をスポーン背後の設備エリアへ集約する。
- [x] ジャンプ・歩行速度を明示し、スポーンと丘の歩行面を静的検証する。
- [x] Quest実機で現行構成に目立つ負荷問題がないことをスモーク確認する（2026-08-15、ユーザー報告）。
- [ ] iOS実機で基準負荷を確認する。

完了条件: 星空なしでも安全に滞在でき、全対象プラットフォームで基本動作する。

## Phase 2: 実在星空と月

- [x] 星表のライセンスと変換工程を確定する。
- [x] 全天球Star Mesh BakerとShaderを実装する。
- [x] 肉眼限界`6.8等級`と実行時の大気消散を星へ適用し、低空ほど暗くする。
- [x] 現在時刻とGlobal同期された22観測地点による天球回転を実装する。
- [x] 主要摂動とtopocentric parallaxを含む月位置計算と表示を実装する。
- [x] ObservatoryProfileで星・月・流星の観測地を一元化し、任意の緯度・東経へ差替可能にする。
- [x] 説明パネルから22観測地点を `◀` / `▶` と逆順3列タイルで選び、地点番号だけをGlobal同期して星・月・流星へ即時適用する。既存Tokyo=0〜Seoul=19を維持し、鳥取・松江を末尾追加する。

完了条件: 基準日時の期待方位と見た目を許容誤差内で再現し、星1個ごとのランタイム更新がない。

## Phase 3: 流星

- [x] 決定的な毎時イベント、Late Join経過再現、任意デバッグ発火を実装する。
- [x] 散在流星を実装する。
- [x] IMO 2026の主要11群を単一catalogから展開する。
- [x] 活動日、放射点高度、ZHRによる群選択と、放射点へ収束する軌跡を実装する。
- [x] 星と共通の大気消散を流星へ適用し、低空の尾・核・フレア・残光を減光する。

完了条件: 同じ時刻のクライアントでイベントが一致し、途中参加で過去の流星を再生しない。

## Phase 4: 品質・公開

- [x] 手作業のピクニック配置をversioned layoutへcaptureし、clone先の全再生成で復元できるようにする。
- [x] UnityメニューをValidate / Content / Preview & Debug / Build & Export / Advancedへ整理し、生成物置換と診断をAdvancedへ隔離して確認を付ける。YamaPlayerのPlaylist編集は上流標準メニューを使う。
- [x] clone後の通常利用、再生成、配置保守、unitypackage出力手順を文書化する。
- [x] GitHub READMEを日本語・英語・繁体字・簡体字・韓国語で用意し、相互切替できるようにする。
- [x] 恒星Mesh bake、時刻回転、空気感、大気消散、座標・恒星時計算、Shader描画を中高生向け簡易説明から詳細説明へ進む図解ガイドとして分離する。
- [x] 説明パネル下端へ `(global)` 観測地点切替とローカルのデバッグパネルON/OFFを追加し、既存在室・履歴・言語UI座標を固定する。
- [x] 説明パネルとDebugパネルを日本語初期表示の日本語 / 英語 / 繁体字 / 簡体字 / 韓国語切替へ拡張する。
- [x] 説明パネルの操作を直接USEとVRレーザーの両方へ対応し、言語ボタンを現在言語→次言語表記にする。
- [x] 木陰へローカル設定ボードを生成し、5方向ミラー、ナイトモードSlider、日時・アラーム、YamaPlayerラジオSpeaker、任意PlayerData保存を実装する。
- [x] 設定ボードをPickup化し、初期OFF、木のローカルトグル、ドロップ10秒復帰を実装する。
- [x] 新しい設定ボード生成コードをUnity 2022.3.22f1でcompileし、保存Sceneを再生成する。
- [ ] 設定ボードのPCVR / Quest / iOS操作、ミラー負荷、ナイトモード視認性、PlayerData再入室復元を確認する。
- [ ] PC・Quest(Android)・iOSで機能、負荷、UI、音声を確認する。
- [ ] VRChat SDK警告、権利表示、第三者通知を解消する。
- [ ] 公開候補ビルドの結果を `TEST_PLAN.md` に記録する。

完了条件: `PROJECT_SPEC.md` のMVP項目と公開チェックがすべて完了している。
