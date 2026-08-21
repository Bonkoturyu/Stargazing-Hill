# Public公開監査

状態: `Confirmed`

確認日: 2026-08-15

この文書は、GitHub repositoryをPrivateからPublicへ変更する前の確認結果、2026-08-14の公開実施結果、公開後も維持する境界を記録する。コード・仕様の正本ではなく、公開判断のチェックリストである。

## 現在の確認結果

| 対象 | 結果 | 状態 |
|---|---|---|
| repository visibility | `Bonkoturyu/Stargazing-Hill` は2026-08-14に `PUBLIC` へ変更済み | Confirmed |
| 現在ツリーの秘密情報 | 秘密鍵、GitHub/GitLab/AWS token、Discord webhook、一般的なpassword/secret代入、Windows/macOS/Linuxの個人ホーム絶対pathを検出せず | Confirmed |
| Git履歴の秘密情報 | 全到達commitに同じpattern検査を実施し、該当なし | Confirmed |
| commit作者情報 | 作者mailはGitHubの `users.noreply.github.com` のみ | Confirmed |
| VRChat user/group/avatar ID | `usr_`、`grp_`、`avtr_` を検出せず | Confirmed |
| Blueprint ID | 保存Sceneにproduction worldの `wrld_...` が1件ある。秘密情報ではないが、clone利用者向け注意が必要 | Confirmed |
| QvPen / YamaPlayer / UnyStylus | package本体は追跡されていない。VPM manifestのpackage名と固定version、局所patch、復元手順だけを公開する | Confirmed |
| 第三者asset | HYG、Poly Haven、Tiny Treats、OpenGameArt方位磁石、Noto Sans CJKは権利記録と隣接license/NOTICEを保持する | Confirmed |
| VRChat VPM resolver | `Packages/com.vrchat.core.vpm-resolver` はVRChat Distro License付きで追跡される。root MITの例外であり、無償公開とする | Confirmed |
| 大容量履歴 | 最大の到達blobは旧Jacaranda mesh約37.1 MiB。GitHubの単一file 100 MiB制限未満だが、clone容量には残る | Confirmed |

自動pattern検査は、未知の形式の秘密や画像内の情報まで完全に証明するものではない。Public化直前にGitHub上の全branch/tagを再確認し、remote branchは `main` のみ、tagは0件だった。

## 公開実施結果

| 対象 | 結果 | 状態 |
|---|---|---|
| visibility | GitHub APIで `visibility: public`、`private: false` を確認 | Confirmed |
| default branch | `main` | Confirmed |
| branch ruleset | `main` を対象にPR経由、branch削除禁止、non-fast-forward禁止、squash mergeの既存rulesetを `active` 化 | Confirmed |
| Actions履歴 | 過去にBudget/Billingでjob開始前に失敗した `Static validation` run 74件を削除。2026-08-15にrunner起動を確認し、Actionをrepository方針どおりfull commit SHAへ固定。PR #28のpush / pull request runがともにPass | Confirmed |
| Actions allowlist | 利用者が2026-08-15に `natsuneko-laboratory/create-unitypackage@*` 相当を許可リストへ追加。workflow本体は `0e02a37fdb702e893dc2a96603768ca7e7dd9b54` へ固定済み。次の `v*` tag実行でremote設定を実証する | Provisional |
| Dependabot | security updatesを有効化し、open alert 0件を確認 | Confirmed |
| Secret scanning | Secret scanningとPush protectionを有効化 | Confirmed |
| CodeQL | Public化後に利用可能。設定と初回実行は未実施 | Pending Evidence |

## Blueprint IDの扱い

VRChat SDKの `VRCPipelineManager` はBlueprint IDをworldの一意なIDとして保存する。SDK 3.5.1以降は、別userが所有するIDまたは無効なIDを検出した場合に、そのIDをclearする挙動が公式release noteに記録されている。このため、repositoryをcloneした別accountがproduction worldを上書きできる認証情報にはならない。

- ownerが既存worldを更新するsourceとして使うため、repository側ではproduction Blueprint IDを維持する。
- clone利用者はSDKがIDをclearしたことを確認するか、`VRCPipelineManager` の **Detach** を実行して自分の新しいBlueprintへuploadする。
- Windows / Android / iOSを同じworldとして配信する場合は、自分が所有する同一Blueprint IDへ各platform buildをuploadする。

根拠（確認日2026-08-14、対象SDK 3.10.4）:

- https://creators.vrchat.com/sdk/vrcpipelinemanager/
- https://creators.vrchat.com/platforms/android/cross-platform-setup/
- https://creators.vrchat.com/releases/release-3-5-1/

## 公開後に残る作業

- `Confirmed`: BOOTH初回正式版 `1.0.0` のunitypackageと顧客向けZIPを2026-08-20に生成し、179 pathnameの所有境界、外部依存除外、5file構成、SHA-256一致を確認した。顧客向け文書はrepository非所持を前提とする5言語の `README.txt` / `NOTICE.txt` とし、Markdown文書は同梱しない。
- `Confirmed`: 利用者が2026-08-21にBOOTHへ初回正式版 `1.0.0` を出品した。価格、サポート範囲、更新方針、返金条件の現在値はBOOTH商品ページ側で管理する。
- `Pending Evidence`: tagによるRelease workflowを1回実行し、ZIP内のunitypackageをclean projectへimportする。
- `Pending Evidence`: QvPen upstream packageには明示license fileがないため、本体は今後も追跡・同梱しない。
- `Pending Evidence`: BOOTH向け初回artifactをclean Unity 2022.3.22f1 projectへimportし、依存復元後の保存Scene validationを行う。

## Release artifactの境界

`.github/workflows/release-unitypackage.yml` は `v*` tagまたは既存tagを指定した手動実行で、`Assets/StargazingHill` だけからunitypackageを生成する。archive pathname検査後、unitypackageとSHA-256一覧をZIPにまとめ、同じtagのGitHub Releaseへ添付する。Unity EditorとUnity licenseは不要で、YamaPlayer、QvPen、UnyStylusのpackage本体は含まれない。

Actionは第三者Actionをtag名ではなくcommit SHAで固定する。GitHub Release作成にはjob単位の `contents: write` だけを付与し、長期tokenは保存しない。

## 推奨World tag

VRChat SDK 3.10.4のauthor tag上限5件に合わせ、初期候補は次の5件とする。

1. `stargazing`
2. `astronomy`
3. `chill`
4. `sleep`
5. `hangout`

状態は `Confirmed`。VRChat SDKで `stargazing`、`astronomy`、`chill`、`sleep`、`hangout` の5件を採用した。現在の内容ではContent Warningに該当する要素は確認していないが、公開時点のworld内容を基準にcreator自身が最終判断する。
