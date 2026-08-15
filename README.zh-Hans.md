# 星见之丘 / Stargazing Hill

[日本語](README.md) | [English](README.en.md) | [繁體中文](README.zh-Hant.md) | [简体中文](README.zh-Hans.md) | [한국어](README.ko.md)

这是一个为 VRChat 制作的宁静草原世界。星空和月亮会根据当前时间与全体共享的22个观测地点之一显示。

## 主要特色

- 使用 HYG v4.1 的真实恒星数据，并根据 UTC 与全局选择的22个地点之纬度、东经旋转天球
- 显示月亮当前的视位置
- 每个整点开始的流星事件，以及 IMO 2026 年历中的 11 个主要流星群
- 广阔草原、小山丘、地标树和 CC0 野餐区
- YamaPlayer、QvPen 和 UnyStylus 区域
- 可切换日语、英语、繁体中文、简体中文和韩语的世界说明、观测地点与调试面板
- 显示当前实例人数和本地进出记录
- 树旁可手持的本地设置板：五方向镜面、夜间模式滑杆、日期时间、闹钟、收音机操作和自选保存
- 面向 Windows、Android／一体式 VR 和 iOS

星空会把 12,495 颗星预先烘焙为一个 Mesh、一个 Renderer 和一个 Additive Unlit Material。运行时只旋转整个天球，不逐颗更新恒星。请参阅带图解的[星空制作指南（日语）](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)和规范正本 [Real Sky System（日语）](docs/REAL_SKY_SYSTEM.md)。

## 项目状态

环境、真实星空、大气消散、全局共享的22地点选择器、月亮计算、流星系统、YamaPlayer 标准播放列表流程、绘图工具、五语面板、人数和进出记录、调试控制及本地舒适度设置均已实现。新设置板已通过 Unity 编译和保存场景结构验证；Windows／Android／iOS 真机最终验证仍未完成。

## 使用全新 Clone

请使用 Unity `2022.3.22f1`。把 Clone 添加到 VRChat Creator Companion，恢复锁定的 VPM 依赖，应用文档指定的 YamaPlayer patch，导入合法购买的 UnyStylus v1.3，再应用兼容性 patch。然后打开 `Assets/StargazingHill/Scenes/StargazingHill.unity` 并执行：

`Stargazing Hill/Validate Saved Scene`

正常使用不需要重建整个 Scene。完整步骤请参阅[设置与恢复（日语）](docs/SETUP_AND_RESTORE.md)。

## Unity 菜单

- `Stargazing Hill/Content`：选择并验证说明面板、应用已保存布局，并维护受版本管理的野餐布局
- `Stargazing Hill/Preview & Debug`：在本地预览星空和流星
- `Stargazing Hill/Build & Export`：创建可再分发的 unitypackage
- `Stargazing Hill/Advanced`：替换生成内容、完整重建 Scene 或修复 SDK；仅供了解影响的维护者使用

仅在有意完整再生成时使用 `Advanced/Generated Content/Rebuild Complete World (Destructive)...`。野餐生成器读取 `Assets/StargazingHill/Editor/Data/PicnicLayout.json`。在 Scene 中手动调整野餐物件后，请执行 `Content/Picnic/Save Current Scene Layout to Generator...`，并一起提交 Scene 和 JSON。

播放列表请使用 YamaPlayer Inspector 中的编辑按钮或 `YamaPlayer/Edit Playlist` 编辑。本项目不再维护独立的播放列表配置文件或自动同步流程；完整重建会沿用已保存 Scene 中由标准编辑器配置的 YamaPlayer。

## 再分发

请通过 `Stargazing Hill/Build & Export/Redistributable UnityPackage...` 创建分发包。该流程只输出项目自有资源和允许再分发的 CC0／CC BY-SA 内容，并明确排除 YamaPlayer、QvPen 和付费 UnyStylus 文件。请勿使用 Unity 通用的 **Include dependencies** 选项制作再分发包。

## 文档

- [文档索引（日语）](docs/README.md)
- [项目规范（日语）](docs/PROJECT_SPEC.md)
- [星空制作指南（日语）](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)
- [Real Sky System 技术规范（日语）](docs/REAL_SKY_SYSTEM.md)
- [设置与恢复（日语）](docs/SETUP_AND_RESTORE.md)
- [第三方依赖（日语）](docs/legal/THIRD_PARTY_DEPENDENCIES.md)
- [第三方素材与许可证（日语）](docs/legal/THIRD_PARTY_ASSETS.md)

## GitHub Release

推送 `v*` 标签后，GitHub Actions 会创建并检查可再分发的 UnityPackage，再将其与 SHA-256 校验文件一起打包为 ZIP，并附加到对应的 Release。该流程不需要 Unity Editor 或 Unity 许可证，也不会包含 YamaPlayer、QvPen 和付费的 UnyStylus 文件。Actions runner已确认可用；首次基于标签创建Release仍为 `Pending Evidence`。请参阅[公开审计（日语）](docs/PUBLIC_RELEASE_AUDIT.md)。
