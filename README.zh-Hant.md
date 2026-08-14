# 星見之丘 / Stargazing Hill

[日本語](README.md) | [English](README.en.md) | [繁體中文](README.zh-Hant.md) | [简体中文](README.zh-Hans.md) | [한국어](README.ko.md)

這是一個為 VRChat 製作的寧靜草原世界。星空與月亮會依照目前時間與全體共享的20個觀測地點之一呈現。

## 主要特色

- 使用 HYG v4.1 的真實恆星資料，依 UTC 與全域選擇的20個地點之緯度、東經旋轉天球
- 顯示目前月亮的視位置
- 每個整點開始的流星事件，以及 IMO 2026 年曆中的 11 個主要流星群
- 廣闊草原、小山丘、地標樹與 CC0 野餐區
- YamaPlayer、QvPen 與 UnyStylus 區域
- 可切換日文、英文、繁體中文、簡體中文與韓文的世界說明、觀測地點與除錯面板
- 顯示目前實例人數與本機進出紀錄
- 以 Windows、Android／一體式 VR、iOS 為目標平台

星空會將 12,495 顆星預先烘焙成一個 Mesh、一個 Renderer 與一個 Additive Unlit Material。執行時只旋轉整個天球，不逐顆更新恆星。請參閱附圖解的[星空製作指南（日文）](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)與規格正本 [Real Sky System（日文）](docs/REAL_SKY_SYSTEM.md)。

## 專案狀態

環境、真實星空、大氣消散、全域共享的20地點選擇器、月亮計算、流星系統、YamaPlayer 標準播放清單流程、繪圖工具、五語面板、人數與進出紀錄、除錯控制及行動平台用 Shader 均已實作。ClientSim 多人測試與 Windows／Android／iOS 實機最終驗證尚未完成。

## 使用全新 Clone

請使用 Unity `2022.3.22f1`。將 Clone 加入 VRChat Creator Companion，復原鎖定的 VPM 相依套件，套用文件指定的 YamaPlayer patch，匯入合法購買的 UnyStylus v1.3，再套用相容性 patch。之後開啟 `Assets/StargazingHill/Scenes/StargazingHill.unity`，並執行：

`Stargazing Hill/Validate Saved Scene`

一般使用不需要重建整個 Scene。完整步驟請見[設定與復原（日文）](docs/SETUP_AND_RESTORE.md)。

## Unity 選單

- `Stargazing Hill/Content`：選取與驗證說明面板、套用已儲存配置，並維護有版本管理的野餐配置
- `Stargazing Hill/Preview & Debug`：在本機預覽星空與流星
- `Stargazing Hill/Build & Export`：建立可再散布的 unitypackage
- `Stargazing Hill/Advanced`：置換生成內容、完整重建 Scene 或修復 SDK；僅供了解影響的維護者使用

只有刻意進行完整再生成時才使用 `Advanced/Generated Content/Rebuild Complete World (Destructive)...`。野餐生成器會讀取 `Assets/StargazingHill/Editor/Data/PicnicLayout.json`。在 Scene 手動調整野餐物件後，請執行 `Content/Picnic/Save Current Scene Layout to Generator...`，並一起提交 Scene 與 JSON。

播放清單請使用 YamaPlayer Inspector 的編輯按鈕或 `YamaPlayer/Edit Playlist` 編輯。本專案不再維護獨立的播放清單設定檔或自動同步流程；完整重建會沿用已儲存 Scene 中由標準編輯器設定的 YamaPlayer。

## 再散布

請使用 `Stargazing Hill/Build & Export/Redistributable UnityPackage...` 建立配布套件。此流程只輸出專案自有資產及允許再散布的 CC0／CC BY-SA 內容，刻意排除 YamaPlayer、QvPen 與付費 UnyStylus 檔案。請勿以 Unity 一般的 **Include dependencies** 選項製作再散布套件。

## 文件

- [文件索引（日文）](docs/README.md)
- [專案規格（日文）](docs/PROJECT_SPEC.md)
- [星空製作指南（日文）](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)
- [Real Sky System 技術規格（日文）](docs/REAL_SKY_SYSTEM.md)
- [設定與復原（日文）](docs/SETUP_AND_RESTORE.md)
- [第三方相依套件（日文）](docs/legal/THIRD_PARTY_DEPENDENCIES.md)
- [第三方素材與授權（日文）](docs/legal/THIRD_PARTY_ASSETS.md)

## GitHub Release

推送 `v*` 標籤後，GitHub Actions 會建立並檢查可再散布的 UnityPackage，再將它與 SHA-256 校驗檔一起封裝成 ZIP 並附加到對應的 Release。此流程不需要 Unity Editor 或 Unity 授權，也不包含 YamaPlayer、QvPen 與付費的 UnyStylus 檔案。目前 Actions 受 Budget/Billing 限制，首次實際執行將在限制解除後確認。請參閱[公開前稽核（日文）](docs/PUBLIC_RELEASE_AUDIT.md)。
