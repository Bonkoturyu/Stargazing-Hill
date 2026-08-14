# 星見の丘 / Stargazing Hill — ワールド仕様書

作成日: 2026-08-11

## 1. コンセプト

短い芝生に覆われた静かな草原で、淡い光と月明かりの中、現実の時刻と全員で共有する観測地点に連動した星空を眺めるVRChatワールド。

ワールド内は基本的に常夜とし、昼夜サイクルそのものは再現しない。

現実時刻に連動する対象は以下。

- 星座の位置
- 月の位置
- 流星群の放射点

主な用途:

- V睡
- 雑談
- 星空観賞
- 動画鑑賞
- QvPenによる交流
- 写真撮影

## 2. 対応プラットフォーム

- PC
- Quest / Android
- iOS

PC専用の高負荷表現に依存せず、モバイル版でもワールドの主題が成立することを必須とする。

## 3. 空間構成

主景観は建物ではなく、空と草原そのものとする。

### 3.1 草原

- 短く整った芝生寄りの草原
- 草丈の目安: 約3〜10cm
- 寝転んだときに視界を大きく遮らない
- ゴルフ場のように均一すぎず、少し自然なムラを残す

地面は不透明の芝生マテリアルを基本とし、近距離だけ低ポリゴンの立体草を追加する。

草1本ごとのGameObject配置や、大量の透明カードは避ける。

実装はPoly Haven `Leafy Grass` のCC0 1K diffuse / normalを地表に使用し、草丈3.5〜9.5cmの立体草15,000株を単一Meshへベイクする。中央±40mは1m格子の起伏を維持し、その外側を±250mまで粗い連続地形で延長する。遠端は夜色のlinear fogへ溶かし、スポーンから容易に地面端へ到達・視認できない構成とする。

### 3.2 小丘

草原の一部だけを緩やかに盛り上げる。

初期目安:

- 高さ: 約1.5〜3m
- 直径: 約12〜20m
- 通常歩行で登れる緩い傾斜
- 頂上付近は座る・寝転がる用途を考慮して少し平らにする

丘は地面Meshへ直接成形し、歩行用Colliderは草原と共通の1枚だけにする。見た目用の丘オーバーレイにはColliderを付けず、重複Colliderによる埋まり・引っ掛かりを防ぐ。

### 3.3 一本木

小丘の頂上または頂上付近に一本だけ配置する。

目的は夜空に対するランドマークと、実在感のある広い樹冠のシルエット形成。

外観方針:

- 中型〜やや大きめ
- 幹は比較的太め
- 枝ぶりが分かりやすい
- Poly Haven `Jacaranda Tree` の幅広く密度のある自然な樹冠
- 葉の隙間から星空も部分的に見える
- 枯れ木ではない
- CC0の1K diffuse / normal / alphaと、連結部品単位で軽量化したMeshを用いる

描画方針:

- 幹・枝: 不透明Mesh
- 葉: alpha clip付きMesh
- マテリアル数: 枝・幹・葉の3
- Realtime Shadow: 原則なし
- Baked Shadowまたは簡易固定影
- Collider: 幹周辺のみ
- Wind Physics: 使用しない

Poly Haven配布FBXの軸・単位変換は派生Meshへベイクし、Scene内の `Model` はidentity/Y-upとする。外側の `LandmarkTree` Anchorだけでyaw・0.40倍スケール・接地を行う。生成後の目標寸法は高さ7.5〜8.5m、幅8.5〜10.5mとし、上方向・接地・丘中央・三角形数45〜48万を検証して横倒しや原本の過密Mesh混入を許容しない。

### 3.4 木陰のピクニックスポット

一本木の下に、Tiny Treats `Pleasant Picnic 1.0` のCC0素材から青い敷物、ラジオ、ティーポット、マグ、青系クッション2点をまとめて配置する。休憩地点の視覚的な目印とし、プレイヤー移動を妨げないよう全品Colliderなしとする。

- runtime Scene: `World/Environment/PicnicSpot`
- 生成配置正本: `Assets/StargazingHill/Editor/Data/PicnicLayout.json`。8 Anchorのworld Transformと各`Model`子のlocal Transformを保持し、Scene上の手修正をEditorメニューからcaptureする
- 構成: 上流6モデルから8 Scene item（敷物、ラジオ、ティーポット、マグ、柄クッション2、無地枕2）、2テクスチャ、2マテリアル
- 配置: シート上方向を丘の頂上側とし、左上のラジオは坂下向き、ティーセットはラジオ付近、柄クッション2つは右側、無地枕2つは反対側。クッションと枕は立てず、90度寝かせたうえで各品のyaw・pitch・rollを少しずつ変える
- 接地: 約4m角へ拡大した敷物Meshの各頂点を丘の高さへ追従させ、小物も各配置地点の地表法線へ個別に合わせる。ラジオ・ティーセットは地面からの離隔を厳密に検査し、柔らかいクッション・枕は浮いて見えないよう敷物へわずかに沈めた配置を許容する
- 木との離隔: 敷物中心を木から約3m離し、敷物外周と木中心の水平距離を最低0.85m確保する
- 描画: `StargazingHill/Environment`、Realtime Shadow・Light Probe・Reflection Probeなし
- モバイル設定: texture最大512 px、mipmap・圧縮あり、Mesh Compression Medium
- 形状予算: 上流6種OBJ合計2,360 triangles、8 Scene itemと地形追従Meshを含む最終合計3,028 triangles
- 対象: PC / Android / iOSで同じ構成を使用

ポリゴン削減は行わない。削減による形状劣化や保守用の派生データを増やすほどの負荷ではないため、上流FBXを追跡可能なまま用いる。全Scene再生成と局所feature updateは同じ配置正本を読み、手作業確定後のSceneと生成結果が乖離しないことを検証する。

## 4. 照明

照明は天体の位置に追従させない。

- Directional Light: 固定
- 月明かり風の照明: 固定
- 草地への主要照明: 原則Bake
- 淡い装飾光: 必要最低限

星と月の「見た目上の位置」だけを現実時刻に連動させる。

## 5. 星空

詳細は [REAL_SKY_SYSTEM.md](REAL_SKY_SYSTEM.md) を正本とする。

基本方針:

- HYG星表を使用
- 実在恒星の赤経・赤緯を利用
- 星は1個ずつGameObjectにしない
- Editorで全天球用の1 Meshへベイク
- Additive Unlit Shaderで描画
- ランタイムでは天球全体のTransformのみ更新
- 観測地点は `ObservatoryProfile` と説明パネル内の20地点catalogで一元管理し、初期値を東京とする。実行中の選択は全員共通のGlobal状態とする
- 星の背面には地平線の淡い空気遠近を表す三色gradient skyboxを置く。天頂の暗さを維持し、地平線のみ低彩度の青を加え、地面側は黒に近づける
- AmbientはFlat、遠景は同系色のlinear fogとし、星・月・流星のAdditive描画を阻害しない

## 6. 月

- 星とは別GameObject
- 現在日時と観測地profileからtopocentric高度・方位を算出
- 見た目上の位置のみ移動
- 地平線より下なら非表示
- 照明方向には連動させない

月齢表現は初期MVPでは必須としない。

位置計算は主要月摂動と扁平地球上の観測者視差を含める。東京の年内5基準日時で、USNO Celestial Navigation APIの高度・方位との差を各0.10°以内とする。

## 7. 流星・流星群

毎時00分を時報相当の演出タイミングとする。

- 1イベント: 180秒（`MeteorController.NaturalEventDurationSeconds` を実装上の唯一の調整値とする）
- 活動中の流星群があれば、その放射点に基づいて生成
- 複数群が活動中なら、活動強度・放射点高度・ZHRから当該hourの代表群を決定する
- 流星群がない時期は散在流星を1〜2本程度出す
- IMO Meteor Shower Calendar 2026 Table 5の活動期間、極大日、放射点、ZHR、対地速度、光度分布指標を11群共通catalogへ格納する
- 群ごとの個別分岐は作らず、同一の活動カーブと放射点計算で処理する
- 対地速度を表示時間・移動距離・尾の長さへ反映し、光度分布指標を演出上のNormal / Bright / Fireball比率へ変換する
- 通常流星は細い暖白色を主体とし、明るい流星と緑白色の火球を少数混ぜる。長時間露光の連続線や同時多発本数は直接再現しない
- 詳細は [REAL_SKY_SYSTEM.md](REAL_SKY_SYSTEM.md)
- Play ModeのEditorメニューから、現在条件の毎時イベントを再生できる
- デバッグ専用プレビューでは主要11群から任意の1群を選び、活動期・放射点高度に関係なく25秒間20本をローカル再生できる。時間は `MeteorController.DebugForcedPreviewDurationSeconds` で自然イベントと別に調整し、各5秒waveの最低1本を開始時の視線正面へ配置する
- 星は+1時間のローカルoffsetとresetで移動を目視比較でき、自動試験でも回転差を検証する
- ワールド内デバッグパネルは約0.47 × 0.41mのローカル専用Pickupとし、説明パネル右隣を初期位置とする。ドロップ後 `WorldDebugPanelPickup.ReturnDelaySeconds`（初期値10秒）で初期位置へ戻し、復帰待ち中の再取得は古い復帰要求を取り消す。表示切替は説明パネル内のボタンだけに集約し、独立した旧 `VRDebugPanelToggle` は置かない
- デバッグパネルは現在のイベント状態、群ID、表示中の流星数を常時表示する。既定表示は日本語とし、右上のツライチボタンで各ユーザーがローカルに日本語 / English / 繁體中文 / 简体中文 / 한국어を順番に切り替える。現在条件ボタンは停止中に各言語の「現在を再生」相当、再生中に「イベント停止」相当へ表示を切り替える。自然イベントを停止した場合は同じhour Event IDをその時刻内で再開しない

## 8. 動画プレイヤー

ワールド内に1系統の動画プレイヤーを設置する。

用途:

- 雑談しながらの動画視聴
- V睡前の動画・音楽再生
- 交流用途

配置は主景観の正面を避け、QvPen・UnyStylusと同じスポーン背後の交流用スペースへ集約する。

プレイヤー実装はYamaPlayer 2.0.0-beta.7を採用し、VPM依存として管理する。動画情報取得用のVideoInfoDownloader moduleを含める。

PlaylistはYamaPlayer標準のInspectorまたは `YamaPlayer/Edit Playlist` で編集し、保存Scene内の `PlaylistItem` を編集上の正本とする。独自のPlaylist設定ファイル、独自Editor、自動同期hookは設けない。同じ4 Playlist / 15 Trackのruntime `Playlist` UdonはControllerの子階層に保持し、YamaPlayerのbuild後処理がControllerをルートへ切り離しても一緒に移動する構造とする。AutoPlayのController参照も保存Sceneへ焼き込み、ClientSimとアップロード済みPC / Android / iOS buildで同じ初期リストと自動再生設定を使う。全Scene再生成では保存Sceneの標準エディター編集済みYamaPlayerを複製する。

音声は3D Spatial Audioとし、参照ワールドと同じ距離減衰を適用する。基準点は `0m:1.0`、`7m:0.8`、`14m:0.6`、`19m:0.5`、`21m:0.35`、`28m:0.24`、`29.5m:0.20`、`45m:0.05`。草原は開放空間のため、屋内外の遮音切替は設けない。

PC / Android / iOSでの実再生、同期、UI、負荷は実機検証を完了条件とする。

## 9. 描画ペン

QvPen 3.3.15と購入済みUnyStylus v1.3を導入する。

用途:

- 落書き
- 会話補助
- 星空の説明
- 簡単なメモ

主景観を遮らないスポーン後方の交流用スペースへ、YamaPlayerとまとめて配置する。

QvPenは公式VPM依存として復元する。UnyStylus本体は購入者向け素材のためGitへ再配布せず、利用者が正規購入したv1.3をローカルへインポートしてシーン参照を解決する。

### 9.1 ワールド説明・在室情報

動画プレイヤーの隣に同程度の大きさの説明パネルを置く。パネル本体にはColliderを付けず、面と同一平面に見える言語切替とDebug ON/OFFだけをInteract対象とする。本文は日本語を初期表示とし、日本語 / English / 繁體中文 / 简体中文 / 한국어を各ユーザーがローカルに切り替える。本文は選択中のGlobal観測地点に依存するため、特定都市を現在地として固定記述しない。右下には言語切替を左、Debug ON/OFFを右へ同じ高さで並べ、押し間違いを避ける間隔を設ける。右側の在室人数・端末内訳・入退室履歴は見た目と表示件数を変えず一体で上へ寄せ、下端の操作列と重ならない配置にする。

同じパネルへ現在人数 `ONLINE n / 80`、推奨人数 `RECOMMENDED 40` と、ローカルクライアントが観測した直近40件の入退室履歴を表示する。履歴は古いものを上、最新を最下段とし、一度に約20件が見える選択可能な縦ScrollRectとスクロールバーで操作する。新しい入退室を受信したときは最下段へ戻す。現在人数はUdonの `VRCPlayerApi.GetPlayerCount()` から取得する。最大・推奨人数はVRChat SDKのupload設定からruntime Udonへ公開されないため、`WorldPresenceBoard.DefaultMaximumCapacity` / `DefaultRecommendedCapacity` を表示用の正本としてSceneへ焼き込む。履歴は個人名を外部保存・永続化・ネットワーク同期しない。

PCとMobile（Android / iOS）の人数内訳は、各クライアントがビルド対象のUnity platform defineから自身をPC / Mobileの2分類で判定し、`PlayerData` の整数値として自動同期する。受信済みの値を全player分集計し、Laptop / Smartphoneの図形アイコンとともに表示する。未受信者は `WAITING` として合計人数との差を明示する。Android VRもAndroid buildであるためMobileへ数え、`IsUserInVR()` を端末OSの判定には用いない（根拠: [VRChat PlayerData](https://creators.vrchat.com/worlds/udon/persistence/player-data/)、[VRChat Player API](https://creators.vrchat.com/worlds/udon/players/)、確認日 2026-08-13、VRChat Worlds SDK 3.10.4）。

説明パネル下端には観測地点のGlobal切替を追加する。重複見出しは置かず、操作列の中央上に「星空の基準地点 (global)」を中央揃えで1つだけ表示する。`◀` / `▶` は20地点を横送りし、現在地点の文字を押すと同じ20地点を3列タイルで上方向へ展開して直接選べる。一覧の視覚順はcatalogと逆順のSeoulからTokyoとし、Tokyoを一覧下端へ置く。ただし同期プロトコルであるcatalog indexは従来のTokyo=0からSeoul=19を維持する。誰でも操作でき、最後に選択された地点へ星、月、流星放射点を即時切り替える。選択地点番号だけをManual Syncし、緯度・東経は全クライアント共通のversioned catalogから適用する。途中参加者にも同じ地点を復元する。Global性は見出しの `(global)` で示し、見出し、選択中ラベル、20地点タイルの名称は本文と同じローカル言語設定に従って日本語 / English / 繁體中文 / 简体中文 / 한국어を表示する。表示言語は同期せず、異なる言語の利用者同士でも同じ地点indexを共有する。初期地点はTokyo、初期表示は日本語の「東京（日本）」。対象はTokyo、Sapporo、Osaka、Takamatsu (Kagawa)、Oita、Miyazaki、Naha (Okinawa)、Rome、Paris、Moscow、Washington D.C.、San Francisco、Los Angeles、Las Vegas、New York、Ottawa、Canberra、Jakarta、Beijing、Seoulの20地点とする。

観測地点の右側には、言語切替とデバッグパネルON/OFFを面一で横並びに置く。この表示切替、観測地点リストの開閉、本文の言語切替、履歴スクロールはローカル状態とし、観測地点だけをGlobal状態とする。パネルの生成位置は手作業確定値 `Position (1.471, 1.999, -26.29)` / `Y Rotation 202.2865°` を正本とする。Laptop / Smartphoneアイコンは手作業確定値 `x=0.79`、`y=0.584 / 0.456` を生成コードとScene検証で固定する。Debugパネルの初期位置は説明パネル右隣の `(-0.842, 1.45, -25.342)` とし、以前の位置から下げる。

### 9.2 プレイヤー移動

ワールド開始時にローカルプレイヤーへ歩行2m/s、走行4m/s、横移動2m/s、ジャンプ力3.2、重力1.0を明示設定する。スポーンは地表から0.4m上に置き、地面へ埋め込まない。

## 10. パフォーマンス方針

優先順位:

1. Questで安定して動く
2. 星・月の見た目を維持
3. 草原と一本木の雰囲気を維持
4. PC版のみ必要に応じて強化

避けるもの:

- 大量の透明草
- 草1本ごとのGameObject
- 草の物理シミュレーション
- Realtime Shadow大量使用
- 星1個ごとのGameObject
- 星1個ごとのUdon計算
- 流星の大量Particle
- 不要なネットワーク同期

## 11. 初期システム構成

```text
World
├─ Environment
│  ├─ GrassGround
│  ├─ GrassClusters
│  ├─ Hill
│  ├─ LandmarkTree
│  ├─ BakedLighting
│  └─ PaleLights
│
├─ RealSkySystem
│  ├─ Starfield_Celestial
│  ├─ Moon
│  └─ RealSkyController
│
├─ MeteorShowerSystem
│  ├─ MeteorController
│  ├─ MeteorRenderer
│  └─ ShowerDatabase
│
├─ VideoSystem
│  └─ YamaPlayer
│
└─ DrawingSystem
   └─ QvPen
```

## 12. MVP

最初に作る範囲:

1. 草原
2. 小丘
3. 一本木
4. 固定の淡い照明
5. HYG全天球Mesh
6. 現在時刻とGlobal観測地点による星空回転
7. 月位置
8. 散在流星
9. IMO 2026主要11流星群
10. 毎時00分イベント
11. 動画プレイヤー
12. QvPen

追加群・別年版は同じcatalog形式へデータ追加して展開する。

## 13. 未確定事項

以下は採用済み扱いにしない。

- 月齢表現の有無
- PC版のみの草揺れ・追加演出

第三者素材を採用する場合は、配布元URL、作者、ライセンス、取得日、使用箇所、ライセンス証拠を記録してから採用確定とする。
