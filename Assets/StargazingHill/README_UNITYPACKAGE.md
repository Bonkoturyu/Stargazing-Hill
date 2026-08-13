# Stargazing Hill UnityPackage

This unitypackage contains only the assets owned and redistributed by Stargazing Hill.

It intentionally does **not** bundle these external dependencies:

- YamaPlayer `net.kwxxw.yama-stream` 2.0.0-beta.7
- QvPen `net.ureishi.qvpen` 3.3.15
- purchased UnyStylus v1.3 files under `Assets/Rasta/UnyStylus`

Before importing or opening the generated scene, restore YamaPlayer and QvPen with VCC/VPM and import your own legitimately purchased UnyStylus v1.3 package. The expected UnyStylus prefab path is `Assets/Rasta/UnyStylus/UnyStylus.prefab`.

The repository's complete restore procedure is `docs/SETUP_AND_RESTORE.md`. From the source project, create this package with Unity menu `Stargazing Hill/Export/Redistributable UnityPackage...`. Do not use Unity's generic **Include dependencies** option for redistribution.
