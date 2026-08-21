# リスクと未解決事項

確認日: 2026-08-15

| 状態 | 項目 | 解決条件 |
|---|---|---|
| Confirmed | HYG星表と派生Meshの配布条件 | v4.1、原本commit/hash、CC BY-SA 4.0、加工工程と表示を記録済み |
| Provisional | YamaPlayer 2.0.0-beta.7 | 採用・依存固定・距離減衰実装済み。PC/Android/iOS実機検証はOpen |
| Provisional | YamaPlayer Editor自動更新確認 | 2.0.0-beta.7の起動時VPM照会をローカルパッチで停止。YamaPlayer更新時は自動適用せず、上流修正の有無を確認してパッチの更新または撤去を判断する |
| Confirmed | クリーンclone初回import順序 | import完了前のbatchビルダー実行ではYamaPlayer extensionが一時的に二重登録される。初回import後にUnityを再起動する復元手順でC# / Udon / Scene生成Pass |
| Provisional | VRChat Worlds SDK 3.10.4 ClientSim Udon初期化順序 | `VrcSdk3104ClientSimGuard` で既知の2断片だけをguard化。SDK更新時は自動書換えを止めるため、上流修正有無を確認し、不要ならADR 0006とguardを削除する |
| Provisional | QvPen 3.3.15 | 公式VPM依存として導入済み。上流packageに明示ライセンスファイルがないため、package本体は追跡せず公式配布から復元する |
| Confirmed | UnyStylus v1.3の復元手順 | 購入済みVN3素材としてローカル導入。本体は再配布せず、`SETUP_AND_RESTORE.md` に期待Prefabと手順を記録済み。実機動作は別項目 |
| Open | QvPenとUnyStylusの併設負荷 | PC/Android/iOSで描画、同期、UI、メモリを測定し、必要なら片方をプラットフォーム別に無効化する |
| Open | 手持ちデバッグパネルの実機操作 | 約0.47 × 0.41mの文字可読性、片手保持中の別手ボタン操作、ドロップ10秒後のdock復帰、復帰待ち中の再取得キャンセルをPCVR / Questで確認する。iOSは画面操作と負荷を確認する |
| Provisional | 木陰のローカル設定ボード | 約0.62 × 0.57m、正面向き、proximity 0.35mの上端グリップ、2列UIとして実装。歯車は単一Udon Interact Colliderとし、利用者がSceneで手調整した位置・回転・scaleを生成正本へ取り込んだ。小さい見た目に対してColliderはworld約0.30m角を維持する。位置変更後のUnity Physics first-hit、ボードUI、Pickup、10秒復帰、両眼描画をPCVR / Quest / iOSで再確認してConfirmedへ上げる |
| Open | ワールドUIのクリック成立 | VRChatはUIレイヤーを操作対象から外すため全CanvasをDefaultレイヤーへ移し、さらにクリック対象を子ではなくCanvasルートへ置き直した。Sliderが動作しボタンが動作しなかった差から立てた単一仮説で、実機未確認。反応しない場合は `UiBeamTarget` へUdonBehaviourを移し、1つのColliderでUI面とInteract面を兼ねる構成へ切り替える |
| Open | 頭部追従トーストのVR快適性 | 入退室表示は前方1.5m・視線から0.42m下の頭部追従UIで、1行5秒・最大3行、非表示時はGameObjectごと停止する。頭部追従UIは酔いの原因になり得るため、PCVR / Questで距離・位置・滞留時間の快適性と、人数の多いインスタンスでの煩わしさを確認する。通知音と表示は別々にOFFできる |
| Open | ローカルミラーとナイトモード負荷 | ミラーは初期OFF。方向ごとにON/OFFし、画質は全面共通でLQ/HQを切り替える。LQはpixel light無効・AA 1、HQはpixel light有効・AA 4。複数面、とくにHQは高負荷になり得ることをUIに表示した。Quest / iOSで組み合わせ別GPU負荷、透明描画、視界の快適性を確認する |
| Open | アラームとPlayerData復元 | `OnPlayerRestored`後のSAVE ON復元、SAVE OFFの非復元、日跨ぎ再発報、インスタンス再入室を実機確認する |
| Open | YamaPlayerラジオSpeaker | ラジオ本体のUSE Trigger・状態表示を廃止し、設定ボードのローカルON/OFFから追加Speakerを直接切り替える。YamaPlayerマスター音量へ0〜100%のラジオ専用ローカル倍率を掛け、ON/OFFと音量をSAVE対象に含める。Unity / AVPro両経路の再生、音量・Mute追従、実機操作と再入室復元をPC / Android / iOSで確認する |
| Provisional | 手持ちローカル方位磁石 | CC0素材、本体Pickup/ObjectSync、針の非同期ローカル計算、天文上の北=ワールド+Z、owner限定のドロップ10秒後Respawnを保存Sceneと静的検査で確認済み。Opus 5静的レビューでも針計算と同期境界は妥当。ティーポットとの干渉を避けるため初期位置をworld X方向へ0.30m離し、高さは真下の面へのレイキャストで決めて浮きを解消した。PCVR / Questで水平保持、赤針の読みやすさ、落下復帰、物理挙動、星座探索への有用性を確認する |
| Open | iOSで利用可能なShaderと動画経路 | 対象Unity/SDK版でBuild & Test |
| Confirmed | 月位置の許容誤差 | USNO APIの東京5日時を基準に、高度・方位とも0.10°以内（実測最大0.0495°）。静的CIとUnity試験でPass |
| Confirmed | 流星群データの一次出典 | IMO Meteor Shower Calendar 2026 Table 5、確認日2026-08-12。主要11群をcatalog化 |
| Provisional | 流星の光度階級確率 | IMOの光度分布指標 `r` をNormal / Bright / Fireball確率へ変換する式は演出上の近似。PC / Quest / iOS実機で視認性と過剰発光を評価して調整する |
| Confirmed | Quest実機の基準負荷 | 2026-08-15、ユーザー実機スモーク確認で現行構成に目立つ負荷問題なし。大きな描画変更時は再確認する |
| Provisional | iOS性能予算 | 基準シーンの実機確認から確定 |
| Provisional | Jacaranda一本木の実機描画負荷 | 原本約386万三角形を19,507三角形へ削減済み（ADR-0009、樹冠はalpha testフロンドカード）。近距離の自然さのため葉を4,645枚まで増やしており、一本木としては軽量とは言えない。Questでは2026-08-15の実機スモーク確認で目立つ問題なし。密なalpha test樹冠のoverdrawはtile GPUで別コストになるため、大きな描画変更時とiOS実機で再確認し、必要なら枚数を戻す |
| Open | 樹冠直下の空の遮蔽 | 真下から見上げると樹冠がほぼ不透明で星空が見えない。星空ワールドとして木の下を鑑賞位置に含めるなら、カード密度かカード配置の再検討が要る |
| Confirmed | 手作業配置したPicnicクッションの接地 | 2026-08-14、利用者が敷物へ自然に沈むよう確定した柄クッション2点の`Model`子Transformを`PicnicLayout.json`へcapture。硬い小物の8〜18cm地形離隔は維持し、クッション・枕だけは敷物への軽い埋め込みを許す専用範囲で検査する |
| Pending Evidence | GitHub Release ZIP自動化 | workflowとarchive検査は実装・静的確認済み。Actions runnerは稼働確認済み。`main`上の`v*` tagでRelease作成、checksum、clean importを確認する |
| Pending Evidence | Public化直前のremote再監査 | 現在ツリーと全到達履歴の秘密pattern検査はPass。visibility変更直前にGitHub上の全branch/tagと最終差分を再確認する。詳細は`PUBLIC_RELEASE_AUDIT.md` |
| Out of scope | 月相表示 | 初期MVPでは位置のみ。将来の視覚効果としてBacklogへ保持 |
