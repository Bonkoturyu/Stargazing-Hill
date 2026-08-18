# Stargazing Hill UnityPackage

[日本語](#日本語) | [English](#english) | [繁體中文](#繁體中文) | [简体中文](#简体中文) | [한국어](#한국어)

## 日本語

このunitypackageには、Stargazing Hill固有のアセットと、再配布根拠・ライセンスをリポジトリに記録した第三者ファイルだけが含まれます。

Stargazing Hill固有部分は同梱の `LICENSE.md` に記載したMIT Licenseです。第三者素材の適用ライセンスと収録場所は `NOTICE.md` を確認してください。

同梱する第三者ファイルは、HYG派生星表（CC BY-SA 4.0）、Poly HavenとTiny TreatsとOpenGameArt方位磁石（CC0-1.0）、Noto Sans CJK（SIL OFL 1.1）です。出典、固定revision、hash、原文ライセンスは各 `NOTICE` と `LICENSE` に記録しています。

次の外部依存は意図的に同梱していません。

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- `Assets/Rasta/UnyStylus` 配下の購入品UnyStylus v1.3

生成済みSceneを開く前に、YamaPlayerとQvPenをVCC/VPMから復元し、正規購入したUnyStylus v1.3を自分でimportしてください。想定するUnyStylus prefab pathは `Assets/Rasta/UnyStylus/UnyStylus.prefab` です。

PlaylistはYamaPlayer標準Inspectorのボタン、または `YamaPlayer/Edit Playlist` で編集します。Stargazing Hill独自のPlaylist設定ファイルや同期メニューはありません。

完全な復元手順は `docs/SETUP_AND_RESTORE.md` です。source projectからはUnityメニュー `Stargazing Hill/Build & Export/Redistributable UnityPackage...` でこのpackageを作成します。再配布時にUnity標準の **Include dependencies** は使用しないでください。

外部依存の復元後は保存済みSceneをそのまま開け、通常利用で全Scene再生成は不要です。source projectでピクニックを手調整した場合は `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...` を使い、clone先と全再生成でも同じ配置を復元できるようにしてください。

## English

This unitypackage contains only Stargazing Hill project assets and third-party files whose redistribution evidence and licenses are recorded in the repository.

Stargazing Hill's original portions are licensed under the MIT License in `LICENSE.md`. See `NOTICE.md` for the applicable third-party licenses and their bundled locations.

Bundled third-party files are the HYG-derived star catalog (CC BY-SA 4.0), Poly Haven, Tiny Treats, and the OpenGameArt compass (CC0-1.0), plus Noto Sans CJK (SIL OFL 1.1). Sources, fixed revisions, hashes, and original license texts are recorded in the adjacent `NOTICE` and `LICENSE` files.

It intentionally does **not** bundle these external dependencies:

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- purchased UnyStylus v1.3 files under `Assets/Rasta/UnyStylus`

Before importing or opening the generated scene, restore YamaPlayer and QvPen with VCC/VPM and import your own legitimately purchased UnyStylus v1.3 package. The expected UnyStylus prefab path is `Assets/Rasta/UnyStylus/UnyStylus.prefab`.

Edit playlists with YamaPlayer's standard Inspector button or `YamaPlayer/Edit Playlist`. Stargazing Hill does not add a separate playlist configuration file or synchronization menu.

The repository's complete restore procedure is `docs/SETUP_AND_RESTORE.md`. From the source project, create this package with Unity menu `Stargazing Hill/Build & Export/Redistributable UnityPackage...`. Do not use Unity's generic **Include dependencies** option for redistribution.

The saved scene is ready to open after the dependencies above are restored; a full scene rebuild is not required for normal use. If you maintain the source project and manually adjust the picnic objects, use `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...` so clones and full rebuilds reproduce the same transforms.

## 繁體中文

此unitypackage只包含Stargazing Hill自身資產，以及已在repository記錄再散布依據與授權的第三方檔案。

Stargazing Hill原創部分採用 `LICENSE.md` 所載的MIT License。第三方素材的適用授權與收錄位置請參閱 `NOTICE.md`。

內含第三方檔案包括HYG衍生星表（CC BY-SA 4.0）、Poly Haven、Tiny Treats與OpenGameArt方位磁石（CC0-1.0），以及Noto Sans CJK（SIL OFL 1.1）。來源、固定revision、hash與原始授權條款記錄於各自的 `NOTICE` 與 `LICENSE`。

下列外部相依項目不會包含在package內：

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- `Assets/Rasta/UnyStylus` 下的付費UnyStylus v1.3檔案

開啟已生成Scene前，請透過VCC/VPM還原YamaPlayer與QvPen，並自行匯入合法購買的UnyStylus v1.3。預期的prefab路徑是 `Assets/Rasta/UnyStylus/UnyStylus.prefab`。

Playlist請使用YamaPlayer Inspector內建按鈕或 `YamaPlayer/Edit Playlist` 編輯。本專案不提供另外的Playlist設定檔或同步選單。

完整還原流程請參閱 `docs/SETUP_AND_RESTORE.md`。從source project製作package時，請使用 `Stargazing Hill/Build & Export/Redistributable UnityPackage...`，再散布時不要使用Unity一般的 **Include dependencies** 選項。

還原外部相依項目後即可直接開啟已儲存Scene，通常不必重建整個Scene。若在source project手動調整野餐物件，請使用 `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...`，讓clone與完整重建能重現相同配置。

## 简体中文

此unitypackage只包含Stargazing Hill自身资源，以及已在repository记录再分发依据和许可的第三方文件。

Stargazing Hill原创部分采用 `LICENSE.md` 中的MIT License。第三方素材的适用许可和收录位置请参阅 `NOTICE.md`。

内含第三方文件包括HYG衍生星表（CC BY-SA 4.0）、Poly Haven、Tiny Treats和OpenGameArt方位磁石（CC0-1.0），以及Noto Sans CJK（SIL OFL 1.1）。来源、固定revision、hash和原始许可记录在各自的 `NOTICE` 与 `LICENSE` 中。

以下外部依赖不会包含在package中：

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- `Assets/Rasta/UnyStylus` 下的付费UnyStylus v1.3文件

打开已生成Scene前，请通过VCC/VPM恢复YamaPlayer与QvPen，并自行导入合法购买的UnyStylus v1.3。预期的prefab路径是 `Assets/Rasta/UnyStylus/UnyStylus.prefab`。

Playlist请使用YamaPlayer Inspector内置按钮或 `YamaPlayer/Edit Playlist` 编辑。本项目不提供另外的Playlist配置文件或同步菜单。

完整恢复流程请参阅 `docs/SETUP_AND_RESTORE.md`。从source project制作package时，请使用 `Stargazing Hill/Build & Export/Redistributable UnityPackage...`，再分发时不要使用Unity通用的 **Include dependencies** 选项。

恢复外部依赖后即可直接打开已保存Scene，通常不需要重建整个Scene。若在source project手动调整野餐物件，请使用 `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...`，确保clone和完整重建能够复现相同配置。

## 한국어

이 unitypackage에는 Stargazing Hill 자체 에셋과, 재배포 근거 및 라이선스가 repository에 기록된 제3자 파일만 포함됩니다.

Stargazing Hill의 자체 제작 부분은 `LICENSE.md`에 포함된 MIT License를 따릅니다. 제3자 소재의 적용 라이선스와 포함 위치는 `NOTICE.md`를 확인하세요.

포함된 제3자 파일은 HYG 파생 별 목록(CC BY-SA 4.0), Poly Haven·Tiny Treats·OpenGameArt 나침반(CC0-1.0), Noto Sans CJK(SIL OFL 1.1)입니다. 출처, 고정 revision, hash, 원문 라이선스는 각 `NOTICE`와 `LICENSE`에 기록되어 있습니다.

다음 외부 의존성은 의도적으로 포함하지 않습니다.

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- `Assets/Rasta/UnyStylus` 아래의 유료 UnyStylus v1.3 파일

생성된 Scene을 열기 전에 YamaPlayer와 QvPen을 VCC/VPM으로 복원하고, 정식 구매한 UnyStylus v1.3을 직접 import하세요. 예상 prefab 경로는 `Assets/Rasta/UnyStylus/UnyStylus.prefab`입니다.

Playlist는 YamaPlayer Inspector의 기본 버튼 또는 `YamaPlayer/Edit Playlist`에서 편집합니다. 이 프로젝트는 별도의 Playlist 설정 파일이나 동기화 메뉴를 제공하지 않습니다.

전체 복원 절차는 `docs/SETUP_AND_RESTORE.md`를 참고하세요. source project에서 package를 만들 때는 `Stargazing Hill/Build & Export/Redistributable UnityPackage...`를 사용하고, 재배포 시 Unity의 일반 **Include dependencies** 옵션은 사용하지 마세요.

외부 의존성을 복원한 뒤에는 저장된 Scene을 바로 열 수 있으며 일반 사용에서 전체 Scene 재생성은 필요하지 않습니다. source project에서 피크닉 오브젝트를 수동 조정했다면 `Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...`를 사용해 clone과 전체 재생성에서도 같은 배치를 재현하세요.
