# Real Sky System 技術仕様

作成日: 2026-08-11

## 実装状況（2026-08-12）

- HYG Stellar Database v4.1から `mag <= 6.8` の12,495星を抽出し、赤経・赤緯・等級・色指数だけを追跡する。
- `StargazingWorldBuilder` が全天球の星を4頂点Quadへ変換し、1 Mesh / 1 Renderer / 1 Materialへ統合する。
- `Starfield.shader` がAdditive Unlit描画と地平線フェードを担当する。
- `ObservatoryProfile` が観測地ID、表示名、緯度、東経を一元管理する。初期assetはTokyo 35.68°N / 139.76°E。
- `RealSkyController` がVRChatのネットワークUTC、Julian Date、恒星時、profileの緯度経度から天球回転を15秒ごとに更新し、全天球の中心をローカルプレイヤーへ追従させる。
- 月は主要摂動と扁平地球上のtopocentric parallaxを含む低コスト計算で位置を求める。東京のUSNO基準5日時で高度・方位とも0.10°以内。
- `MeteorShowerCatalog` がIMO Meteor Shower Calendar 2026 Table 5から主要11群の活動期間、極大日、放射点、ZHRを保持する。
- `MeteorController` が毎時00分から25秒間、共通UTCのhour Event IDから決定的に最大4本の再利用Quadを描画する。活動日、放射点高度、ZHRから当該hourの群を選び、活動群がなければ散在流星へfallbackする。5秒waveを5回使い、1イベント最大20本とする。
- Play Mode中の `Stargazing Hill/Debug/Meteor Shower Preview...` で主要11群を選択し、活動期と放射点高度に関係なく20本をローカル強制再生できる。`Force Perseids Preview (20 Meteors)` はペルセウス座流星群の短縮入口。`Advance Sky +1 Hour` と `Reset Sky Time Offset` で天球移動を目視比較できる。
- 原本、ライセンス、SHA-256、加工工程は `Assets/StargazingHill/Editor/Data/NOTICE.md` を正本とする。
- データ再生成、範囲検査、C#/UdonSharpコンパイル、Unityシーン生成、保存後参照検証、月のUSNO基準、5観測地parameterization、11群catalogはPass。ClientSimと実機確認はOpen。

## 1. 目的

`星見の丘 / Stargazing Hill` の星空・月・流星群を、現実の東京の現在日時と連動させる。

対象:

- 実在恒星の位置
- 月の見かけ上の位置
- 流星群の放射点
- 毎時00分の時報代替流星演出

照明そのものは天体位置へ追従させない。

## 2. 既存実装からの再利用方針

`VRChat-World_Luxury_Cruise_Ship_PRETTY_MUCH` の夜空実装で採用した以下の考え方を再利用する。

- HYG星表
- 等級ベースの明るさ
- B-V値ベースの星色
- 星ごとの極小Quad
- 全星を1 Meshへ結合
- Additive Unlit Shader
- Shadowなし
- Light Probe / Reflection Probeなし

既存客船ワールドでは `mag <= 6.8` を実機比較で採用し、5,843星 / 23,372 vertices / 約1.97MBの星メッシュとして運用している。

本ワールドでは地平線上の星だけをベイクする方式ではなく、全天球を保持する方式へ変更する。

## 3. 天球メッシュ

### 3.1 Editorベイク

HYG星表から各恒星の赤経・赤緯を読み、観測時刻・観測地点を掛ける前の天球座標として1 Meshへベイクする。

```text
HYG
 ↓
赤経・赤緯
 ↓
Celestial Sphere座標
 ↓
Starfield_Celestial.mesh
```

星1個ごとのGameObjectは作らない。

全天球を保持するため、星数は既存の地平線上のみの実装より増える見込みだが、1 Mesh / 1 Rendererを維持する。

### 3.2 見た目

初期値:

- Magnitude Limit: 6.8
- 極小Quad
- Additive Unlit
- B-V値に応じた色
- 等級に応じたサイズ・輝度

実機で星が過密に見える場合は等級上限を再調整する。

## 4. 観測地点

観測地点は `Assets/StargazingHill/Settings/TokyoObservatory.asset` を正本とする。初期値:

- Latitude: 35.68 deg N前後
- Longitude: 139.76 deg E前後

緯度は北を正、経度は東を正とする。星、月、流星放射点へ同じ値を渡すため、別の都市・緯度経度へ差し替えても個別コード変更は不要。San Francisco、Rome、Moscow、Torontoを含む5地点で、天の北極高度が観測緯度と一致することを試験する。

## 5. 時刻

VRChat側のネットワーク日時を基準にする。

各クライアントが同じ日時から独立して天球角度を計算し、星空回転自体はネット同期しない。

目的:

- Ownership不要
- Synced Variable不要
- Network Event不要
- Late Join特別同期不要

## 6. 星空回転

ランタイムで全恒星を再計算しない。

処理:

```text
現在UTC日時
 ↓
Julian Date
 ↓
Greenwich Sidereal Time
 ↓
+ Tokyo Longitude
 ↓
Local Sidereal Time
 ↓
Tokyo Latitude
 ↓
Starfield_Celestial.rotation
```

更新頻度の初期値:

- 10〜30秒に1回

補間が必要な場合のみTransform回転を滑らかに補間する。

## 7. 地平線と大気減光

全天球を保持するため、地平線下の恒星はShader側で非表示にする。

初期仕様:

- 地平線以下: 0%
- 高度3°付近: 約20%
- 高度8°付近: 約70%
- 高度15°以上: 100%

`smoothstep` 相当の単純処理を優先する。

既存客船ワールドで使用した詳細な大気消散式をそのままランタイムへ移すことは必須としない。

## 8. 月

月は恒星天球と別GameObjectにする。

```text
Moon
└─ MoonQuad / MoonMesh
```

現在日時から月の地心赤道座標を求め、観測地から見たtopocentric高度・方位へ変換する。

```text
現在日時
 ↓
- 主要月摂動を含む地心月位置
 ↓
- 扁平地球上の観測者視差補正
 ↓
- profile地点のAlt / Az
 ↓
Moon Transform
```

初期仕様:

- 更新: 10〜30秒に1回
- 地平線以下: 非表示
- Light方向は変更しない
- Realtime Shadowには使用しない
- 月齢表現はMVP外

精度基準はUSNO Celestial Navigation APIの `hc - pa` を月中心のtopocentric高度、`zn` を方位として使用する。2025-01-15、03-14、06-10、08-12、11-05の各12:00 UTC、Tokyo 35.68 / 139.76で高度・方位の絶対誤差を各0.10°以内とする。実測最大誤差は高度0.0394°、方位0.0495°。この保証は当該5fixtureに対するものであり、全時刻・全地点の高精度暦を意味しない。参照URLと判定は `Tools/Validate-StargazingImplementation.py` および [ADR 0005](adr/0005-data-driven-celestial-observatory.md) を正本とする。

## 9. 毎時流星イベント

毎時00分を時報相当のイベントとする。

初期仕様:

- 開始: 毎時00分
- 長さ: 約20〜30秒
- 同時表示: 最大2〜4本程度
- 流星群なし: 散在流星1〜2本

演出はネットワークイベントで同期せず、時刻から決定論的に生成する。

### 9.1 Event ID

例:

```text
YYYYMMDDHH
2026081114
```

これをRandom Seedの基礎にする。

各クライアントが同じSeedから以下を決める。

- 発生時刻
- 長さ
- 速度
- 放射点からの離角
- 軌跡方向
- 明るさ

### 9.2 Late Join

イベント中にJoinした場合は、現在時刻からイベント開始後の経過秒数を求める。

例:

```text
14:00:17 Join
 ↓
14時イベント開始から17秒経過
 ↓
残り区間のみ再現
```

過去に終了した流星を再生し直さない。

### 9.3 デバッグ発火

生成SceneをPlay Modeで開き、`Stargazing Hill/Debug/Meteor Shower Preview...` を開く。主要11群から選択して `選択した流星群を正面へ強制表示` を押すと、選択群を25秒間・20本（5秒ごとに4本）でローカル再生する。活動期間、実際の放射点高度、ZHRはこの強制プレビューに限り無視する。各waveの1本目はボタン押下時のGame camera正面へ配置し、地形に隠れないよう高度が約13°未満の視線だけ画角内で上方補正するため、最初の流星は即時確認できる。他3本は選択群の収束する軌跡を保ったまま視界周辺へ配置する。

`Stargazing Hill/Debug/Force Perseids Preview (20 Meteors)` はペルセウス座流星群を直接開始する。`Stargazing Hill/Debug/Trigger Hourly Meteor Shower` は現在hourの自然条件を経過0秒から再生する旧経路であり、活動群がない日時には散在流星だけになる。

いずれもネットワークイベントは送らず、他プレイヤーの状態を変更しない。`MeteorController.DebugTriggerSelectedShower(int, Vector3)` と `DebugTriggerHourlyEvent()` を公開する。強制プレビューは観測方向へ演出を回し、見落とし防止のため軌跡の幅を2.4倍、長さを1.6倍にする視認性試験であり、自然発生側のUTC、活動度、実放射点計算と表示寸法は変更しない。

天球は `Stargazing Hill/Debug/Advance Sky +1 Hour` でローカル時刻offsetを1時間進め、`Reset Sky Time Offset` で現在UTCへ戻す。自動試験では同一時刻の回転一致、+1時間で約15.04°、+24時間で約0.985°の恒星日差を確認する。

## 10. 流星描画

大量Particleは使用しない。

初期案:

- 細長いQuadまたは少数の再利用Mesh
- Unlit
- Additive
- Shadowなし
- Colliderなし
- Lightなし
- 共通Material

Shader側で尾の減衰、フェード、移動を行える構造を優先する。

流星Transformを大量にUdonで毎フレーム更新しない。

## 11. 対応する主な流星群

IMO `Meteor Shower Calendar 2026` Table 5を基準に、以下11群をデータとして持つ。日付と放射点は2026版、ZHRは同表のrecent observed returnsに基づく値であり、年次更新時はcatalogと検証値を同時更新する。

| ID | 流星群 | 活動期間の目安 | 極大の目安 | 初期演出強度 |
|---|---|---|---|---|
| QUADRANTIDS | しぶんぎ座流星群 | 12/28〜1/12 | 1/3 | Strong |
| LYRIDS | 4月こと座流星群 | 4月中旬〜下旬 | 4/22頃 | Medium |
| ETA_AQUARIIDS | みずがめ座η流星群 | 4月下旬〜5月下旬 | 5/6頃 | Medium |
| SOUTH_DELTA_AQUARIIDS | みずがめ座δ南流星群 | 7月中旬〜8月下旬 | 7/31頃 | Medium |
| PERSEIDS | ペルセウス座流星群 | 7月中旬〜8月下旬 | 8/13頃 | Strong |
| DRACONIDS | 10月りゅう座流星群 | 10月上旬 | 10/9頃 | Weak |
| ORIONIDS | オリオン座流星群 | 10月上旬〜11月上旬 | 10/21頃 | Medium |
| SOUTH_TAURIDS | おうし座南流星群 | 9月下旬〜11月下旬 | 11/5頃 | Weak |
| NORTH_TAURIDS | おうし座北流星群 | 10月下旬〜12月上旬 | 11/12頃 | Weak |
| LEONIDS | しし座流星群 | 11/6〜11/30 | 11/17 | Weak-Medium |
| GEMINIDS | ふたご座流星群 | 12月上旬〜下旬 | 12/14頃 | Strong |

三大流星群:

- しぶんぎ座流星群
- ペルセウス座流星群
- ふたご座流星群

## 12. ShowerDatabase

流星群ごとに個別コードを書かない。

各流星群をデータとして登録する。

概念フィールド:

```text
MeteorShowerData
- id
- displayNameJa
- displayNameEn
- activeStart
- activeEnd
- peakDate
- radiantRA
- radiantDec
- strengthClass
- activityCurve
- visualProfile
```

必要に応じて年別補正を追加可能な構造にする。

## 13. 活動強度

活動期間中ずっと同じ本数にはしない。

概念:

```text
activityStrength
 = dateActivityCurve
 × radiantAltitudeFactor
```

- 活動開始直後: 少ない
- 極大へ近づく: 増える
- 極大: 最大
- 極大後: 減る
- 放射点が地平線以下: 原則その流星群は演出しない

MVPでは簡易カーブで実装し、必要なら将来ZHR等を用いた年別補正へ拡張する。

## 14. 放射点

各流星群の放射点は赤経・赤緯で保持する。

星空と同じ天球変換を適用し、現在の東京の空での高度・方位を得る。

流星は放射点そのものから開始するのではなく、画面上で放射点から離れた場所に生成し、軌跡を逆延長すると放射点へ収束するようにする。

複数群が活動中の場合は、日付活動強度 × 放射点高度factor × ZHRが最大の群を当該hourの代表群とする。これは20〜30秒の圧縮演出で複数の放射点を混在させないMVP上の選択であり、catalog構造は将来の同時描画拡張を妨げない。

## 15. 演出本数の初期目安

毎時イベント1回あたり:

- Weak: 2〜5本
- Medium: 5〜10本
- Strong: 10〜20本
- Sporadic only: 1〜2本

これは現実の1時間あたり観測数をそのまま再現する値ではなく、20〜30秒の「時報演出」として圧縮した視覚表現である。

## 16. パフォーマンス目標

星空:

- 1 Mesh
- 1 Renderer
- 1 Materialを基本
- 個別星GameObjectなし
- 個別星Udon更新なし

流星:

- 少数オブジェクトの再利用
- 毎時イベント中のみ描画
- 透明面積を必要最小限にする

月:

- 1 Renderer
- 低頻度Transform更新

ネットワーク:

- 星空同期なし
- 月同期なし
- 流星同期なし
- 共通ネットワーク日時を基準に決定論的再現

## 17. MVP実装順

1. [x] 全天球Star Mesh Baker
2. [x] Star Shaderの地平線処理
3. [x] RealSkyController
4. [x] ObservatoryProfileと東京初期値
5. [x] topocentric Moon計算と表示
6. [x] MeteorRenderer
7. [x] 決定論的毎時イベント
8. [x] 散在流星
9. [x] MeteorShowerCatalog化
10. [x] IMO 2026主要11群
11. [ ] Quest / iOS実機負荷確認

## 18. 実装前に再確認するもの

天文データは正確性を優先し、実装着手時に以下を公式・一次情報で再確認する。

- 次年版の流星群ごとの活動期間
- 次年版の極大時期
- 次年版の放射点座標
- 必要なら年別の極大予測
- VRChat SDK / Udonの現在利用可能な時刻API

流星群データはコードへ直接散在させず、出典を追跡できる形で一元管理する。
