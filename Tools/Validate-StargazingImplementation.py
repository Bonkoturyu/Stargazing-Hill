#!/usr/bin/env python3
"""Fast, dependency-free checks for the generated-world source inputs."""

import csv
import hashlib
import json
import math
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Assets/StargazingHill/Editor/Data/hyg_bright_v41.csv"
BUILDER = ROOT / "Assets/StargazingHill/Editor/StargazingWorldBuilder.cs"
SKY_CONTROLLER = ROOT / "Assets/StargazingHill/Scripts/RealSkyController.cs"
METEOR_CONTROLLER = ROOT / "Assets/StargazingHill/Scripts/MeteorController.cs"
CLIENTSIM_GUARD = ROOT / "Assets/StargazingHill/Editor/VrcSdk3104ClientSimGuard.cs"
METEOR_SHADER = ROOT / "Assets/StargazingHill/Shaders/Meteor.shader"
STARFIELD_SHADER = ROOT / "Assets/StargazingHill/Shaders/Starfield.shader"
ATMOSPHERE_INCLUDE = ROOT / "Assets/StargazingHill/Shaders/StargazingAtmosphere.cginc"
MOON_SHADER = ROOT / "Assets/StargazingHill/Shaders/Moon.shader"
OBSERVATORY_PROFILE = ROOT / "Assets/StargazingHill/Settings/TokyoObservatory.asset"
SHOWER_CATALOG = ROOT / "Assets/StargazingHill/Settings/IMO2026MajorShowers.asset"
PLAYER_SETTINGS = ROOT / "Assets/StargazingHill/Scripts/WorldPlayerSettings.cs"
DEBUG_PANEL_PICKUP = ROOT / "Assets/StargazingHill/Scripts/WorldDebugPanelPickup.cs"
PACKAGE_EXPORTER = ROOT / "Assets/StargazingHill/Editor/StargazingUnityPackageExporter.cs"
PACKAGE_README = ROOT / "Assets/StargazingHill/README_UNITYPACKAGE.md"
RELEASE_WORKFLOW = ROOT / ".github/workflows/release-unitypackage.yml"
PUBLIC_RELEASE_AUDIT = ROOT / "docs/PUBLIC_RELEASE_AUDIT.md"
PROJECT_NOTICE = ROOT / "NOTICE.md"
YAMA_PATCH_SCRIPT = ROOT / "Tools/Apply-YamaPlayerPatches.ps1"
YAMA_PATCH = ROOT / "Tools/YamaPlayerPatches/2.0.0-beta.7-disable-editor-auto-update.patch"
UNYSTYLUS_PATCH_SCRIPT = ROOT / "Tools/Apply-UnyStylusPatches.ps1"
UNYSTYLUS_PATCH_README = ROOT / "Tools/UnyStylusPatches/README.md"
NIGHT_SKY_SHADER = ROOT / "Assets/StargazingHill/Shaders/NightSkyGradient.shader"
DEBUG_PANEL_STATUS = ROOT / "Assets/StargazingHill/Scripts/WorldDebugPanelStatus.cs"
INFO_LANGUAGE_TOGGLE = ROOT / "Assets/StargazingHill/Scripts/WorldInfoLanguageToggle.cs"
PRESENCE_BOARD = ROOT / "Assets/StargazingHill/Scripts/WorldPresenceBoard.cs"
PRESENCE_SCROLL = ROOT / "Assets/StargazingHill/Scripts/WorldPresenceHistoryScrollButton.cs"
OBSERVATORY_SELECTOR = ROOT / "Assets/StargazingHill/Scripts/WorldObservatorySelector.cs"
OBSERVATORY_BUTTON = ROOT / "Assets/StargazingHill/Scripts/WorldObservatoryButton.cs"
INFO_PANEL_INSTALLER = ROOT / "Assets/StargazingHill/Editor/WorldInformationPanelInstaller.cs"
PANEL_FONT = ROOT / "Assets/StargazingHill/ThirdParty/Fonts/NotoSansCJKkr-Regular.otf"
QUALITY_SETTINGS = ROOT / "ProjectSettings/QualitySettings.asset"
VPM_MANIFEST = ROOT / "Packages/vpm-manifest.json"
TREE_SELECTION_TEMP = ROOT / "Assets/TreeSelectionTemp"
GRASS_DIFFUSE = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_diff_1k.jpg"
GRASS_NORMAL = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_nor_gl_1k.jpg"
JACARANDA_ROOT = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/JacarandaTree"
TREE_MESH = JACARANDA_ROOT / "Jacaranda_Quest.asset"
PICNIC_ROOT = ROOT / "Assets/StargazingHill/ThirdParty/TinyTreats/PleasantPicnic"
PICNIC_INSTALLER = ROOT / "Assets/StargazingHill/Editor/PicnicSceneInstaller.cs"
PICNIC_LAYOUT = ROOT / "Assets/StargazingHill/Editor/Data/PicnicLayout.json"
SETTINGS_INSTALLER = ROOT / "Assets/StargazingHill/Editor/WorldSettingsSystemInstaller.cs"
SETTINGS_CONTROLLER = ROOT / "Assets/StargazingHill/Scripts/WorldSettingsController.cs"
SETTINGS_BUTTON = ROOT / "Assets/StargazingHill/Scripts/WorldSettingsButton.cs"
SETTINGS_PICKUP = ROOT / "Assets/StargazingHill/Scripts/WorldSettingsBoardPickup.cs"
RADIO_SPEAKER = ROOT / "Assets/StargazingHill/Scripts/WorldRadioSpeaker.cs"
PRESENCE_NOTIFIER = ROOT / "Assets/StargazingHill/Scripts/WorldPresenceNotifier.cs"
COMPASS_ROOT = ROOT / "Assets/StargazingHill/ThirdParty/OpenGameArt/Compass"
COMPASS_INSTALLER = ROOT / "Assets/StargazingHill/Editor/CompassSceneInstaller.cs"
COMPASS_NEEDLE = ROOT / "Assets/StargazingHill/Scripts/LocalCompassNeedle.cs"
NIGHT_MODE_SHADER = ROOT / "Assets/StargazingHill/Shaders/NightModeOverlay.shader"
WORLD_SCENE = ROOT / "Assets/StargazingHill/Scenes/StargazingHill.unity"
LOCALIZED_READMES = [
    ROOT / "README.md",
    ROOT / "README.en.md",
    ROOT / "README.zh-Hant.md",
    ROOT / "README.zh-Hans.md",
    ROOT / "README.ko.md",
]
STARFIELD_GUIDE = ROOT / "docs/STARFIELD_IMPLEMENTATION_GUIDE.md"
TREE_TEXTURES = [
    JACARANDA_ROOT / "jacaranda_tree_branches_diff_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_branches_nor_gl_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_trunk_diff_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_trunk_nor_gl_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_leaves_diff_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_leaves_nor_gl_1k.jpg",
    JACARANDA_ROOT / "jacaranda_tree_leaves_alpha_1k.jpg",
]
EXPECTED_CATALOG_SHA256 = "976abeb38d0d6f7b12fb069943140b59c31a299a72dc04350014e9ca1a4a0e4a"
EXPECTED_STAR_COUNT = 12_495
EXPECTED_CC0_HASHES = {
    GRASS_DIFFUSE: "cfa40bc9d9417d1852db8753a8d5917f110c40101179f63543c382e39bc05e4a",
    GRASS_NORMAL: "832328216adc0a7e1f70a31d5ee48c9ab7f2152d83816122736cf42ac4b2ebd6",
    TREE_MESH: "0cebaa16c70c9c08a6d7ce83133a2a1ce0edb8d44cb6c6e8628d3c79f97318b2",
    TREE_TEXTURES[0]: "5a4fe735f0c346cec83b6b444a0169fc14d2b31bbb7b26e26b1b6c726b6e06f6",
    TREE_TEXTURES[1]: "2d67393ba76a0f49cf965fa0c99d8c16268e71e87b1203d35539bfdb971a514e",
    TREE_TEXTURES[2]: "e5582fba664a9255252d1ee8088f75ad557f07360aa7f4513f2b2f1577e90be9",
    TREE_TEXTURES[3]: "b2ad4e5daf8ec3fb87ba6c5f38e6a6924c9a6adebec20389f503e42da5d04587",
    TREE_TEXTURES[4]: "6fe80c1f514ef690cccefb90b7e559fbd1aa9c8935fdc02523cc8ab83c22f7c2",
    TREE_TEXTURES[5]: "61520be2529ffe8e3d93d4361892134833bb4657117e121749089769067914c1",
    TREE_TEXTURES[6]: "e040c86c4bd9ce703244e33a6b9cf25d2ac65c618be88178b8be3297f5d41960",
}
EXPECTED_PICNIC_HASHES = {
    PICNIC_ROOT / "picnic_blanket_blue.fbx": "552a74e55e224a24051aaf7a2e6c63973a71e771bb5fae9289c71be0fbe0e2db",
    PICNIC_ROOT / "radio.fbx": "9d6af6d768427eecfabffa3920caa9690473979cbee9df6349b76789589eaf06",
    PICNIC_ROOT / "teapot.fbx": "0ab6edbe361f19b24d3ea6395434300f7ffd69ad1007b88a426365f29f17997a",
    PICNIC_ROOT / "mug.fbx": "d3a3df63297f2f2a5b7a87fff5dd1803054616c69ce4fff7aed82ec1e50b6d9e",
    PICNIC_ROOT / "pillow_small_blue.fbx": "1eabd1867988fd0609a8cb110e360bba5a4779285d9fea1d57900a87e4e7a912",
    PICNIC_ROOT / "pillow_large_blue.fbx": "86c834155243e7315aebf734caeab56a69ce818b1db8d325180eaabf443e670d",
    PICNIC_ROOT / "tiny_treats_texture_1.png": "31e5c7c81bfba644a59797b1907519592c7728d4af98a705449975dd929482e5",
    PICNIC_ROOT / "tiny_treats_plaid_pattern_blue.png": "cd967007ad742e3c964be4854c971ed0d9aa98d2b6710ad05352833a68e8d6f9",
}
EXPECTED_COMPASS_HASHES = {
    COMPASS_ROOT / "Compass.fbx": "2198a54c7b87dc97790413a5ad97a1c2184eb953e5329b7b8ba89e068367434a",
    COMPASS_ROOT / "Compass_Albedo.png": "b0b870da48d1e45b7ce12d5d26cce75ae2598d9fb20a0c82b971e740df1f97f3",
    COMPASS_ROOT / "Compass_Normal.png": "2bd60ae53a5f72a4f26b3d169268a672e68846460096fb25dc6f80222339200a",
}
EXPECTED_ROLLOFF = [
    (0.0, 1.0),
    (7.0, 0.8),
    (14.0, 0.6),
    (19.0, 0.5),
    (21.0, 0.35),
    (28.0, 0.24),
    (29.5, 0.2),
    (45.0, 0.05),
]
EXPECTED_SHOWERS = {
    "ids": ["QUADRANTIDS", "LYRIDS", "ETA_AQUARIIDS", "SOUTH_DELTA_AQUARIIDS", "PERSEIDS", "DRACONIDS", "ORIONIDS", "SOUTH_TAURIDS", "NORTH_TAURIDS", "LEONIDS", "GEMINIDS"],
    "activeStartMonthDay": [1228, 414, 419, 712, 717, 1006, 1002, 920, 1020, 1106, 1204],
    "activeEndMonthDay": [112, 430, 528, 823, 824, 1010, 1107, 1120, 1210, 1130, 1220],
    "peakMonthDay": [103, 422, 506, 731, 813, 1009, 1021, 1105, 1112, 1117, 1214],
    "radiantRightAscensionDegrees": [230, 271, 338, 340, 48, 262, 95, 52, 58, 152, 112],
    "radiantDeclinationDegrees": [49, 34, -1, -16, 58, 54, 16, 15, 22, 22, 33],
    "zenithalHourlyRates": [80, 18, 50, 25, 100, 5, 20, 7, 5, 15, 150],
    "geocentricVelocityKilometersPerSecond": [41, 49, 66, 41, 59, 20, 66, 27, 29, 71, 35],
    "populationIndices": [2.1, 2.1, 2.4, 2.5, 2.2, 2.6, 2.5, 2.3, 2.3, 2.5, 2.6],
}
USNO_MOON_REFERENCES = [
    (2025, 1, 15, 12, 0, 31.961191, 88.018947),
    (2025, 3, 14, 12, 0, 35.566529, 119.239320),
    (2025, 6, 10, 12, 0, 20.737319, 151.921682),
    (2025, 8, 12, 12, 0, 8.438289, 95.119726),
    (2025, 11, 5, 12, 0, 55.521352, 109.156911),
]


def validate_repository_hygiene() -> None:
    assert not TREE_SELECTION_TEMP.exists(), (
        "Assets/TreeSelectionTemp is a local comparison workspace and must not exist: "
        "Unity compiles ignored C# files under Assets. Use Temp/TreeSelectionTemp instead."
    )


def validate_catalog() -> None:
    # Git may check text files out as CRLF on Windows. Hash the canonical LF form
    # recorded by the source notice so validation is independent of checkout policy.
    catalog_bytes = CATALOG.read_bytes().replace(b"\r\n", b"\n")
    digest = hashlib.sha256(catalog_bytes).hexdigest()
    assert digest == EXPECTED_CATALOG_SHA256, f"catalog SHA-256 changed: {digest}"

    count = 0
    with CATALOG.open("r", encoding="utf-8", newline="") as source:
        reader = csv.DictReader(source)
        assert reader.fieldnames == ["rarad", "decrad", "mag", "ci"]
        for row in reader:
            ra = float(row["rarad"])
            dec = float(row["decrad"])
            magnitude = float(row["mag"])
            assert 0.0 <= ra <= math.tau
            assert -math.pi / 2.0 <= dec <= math.pi / 2.0
            assert magnitude <= 6.8
            if row["ci"]:
                float(row["ci"])
            count += 1
    assert count == EXPECTED_STAR_COUNT, f"expected {EXPECTED_STAR_COUNT} stars, found {count}"


def validate_yama_dependency_and_rolloff() -> None:
    manifest = json.loads(VPM_MANIFEST.read_text(encoding="utf-8"))
    assert manifest["dependencies"]["net.kwxxw.yama-stream"]["version"] == "2.0.0-beta.7"
    assert manifest["locked"]["net.kwxxw.yama-stream"]["dependencies"]["com.vrchat.worlds"] == ">=3.8.1"

    builder = BUILDER.read_text(encoding="utf-8")
    curve_body = re.search(
        r"private static AnimationCurve CreateYamaRolloffCurve\(\).*?return new AnimationCurve\((.*?)\);",
        builder,
        re.DOTALL,
    )
    assert curve_body, "YamaPlayer rolloff curve was not found"
    points = [
        (float(distance), float(volume))
        for distance, volume in re.findall(
            r"new Keyframe\(([0-9.]+)f,\s*([0-9.]+)f,", curve_body.group(1)
        )
    ]
    assert points == EXPECTED_ROLLOFF, f"unexpected YamaPlayer rolloff: {points}"
    assert "source.maxDistance = 45f;" in builder
    assert 'SetSerializedFloat(spatial, "Far", 45f);' in builder

    assert not (ROOT / "Assets/StargazingHill/Editor/YamaPlayerPlaylistSync.cs").exists()
    assert not (ROOT / "Assets/StargazingHill/Editor/YamaPlayerPlaylistConfigPostprocessor.cs").exists()
    assert not (ROOT / "Assets/StargazingHill/Config/music_list.txt").exists()
    assert "YamaPlayerPlaylistSync" not in builder
    assert "standard editor-authored instance" in builder
    assert 'FindGameObjectInScene(sourceScene, "World/VideoSystem/YamaPlayer")' in builder
    assert "Object.Instantiate(standardEditorAuthoredSource)" in builder
    assert "ValidateYamaPlayerPreservationForBatchMode" in builder
    assert "autoPlayBacking.publicVariables.TryGetVariableValue(" in builder
    assert "if (!hasSerializedController || autoPlayController as UdonBehaviour != controllerBacking)" in builder
    assert "runtimePlaylists.Length != playlistItems.Length" in builder
    assert "playlistTrackCount != authoringPlaylistTrackCount" in builder

    patch_script = YAMA_PATCH_SCRIPT.read_text(encoding="utf-8")
    patch = YAMA_PATCH.read_text(encoding="utf-8")
    assert "'2.0.0-beta.7' = " in patch_script
    assert "package.version" in patch_script
    assert "git apply --reverse --check" not in patch_script
    assert "$transforms = @(" in patch_script
    assert "Original = @'" in patch_script
    assert "Patched = @'" in patch_script
    assert "source matches neither or both verified fragments" in patch_script
    assert "Packages/net.kwxxw.yama-stream/Editor/Package/PackageManager.cs" in patch
    assert "-      EditorApplication.delayCall += () =>" in patch
    assert "+      // Stargazing Hill: package updates are managed through VCC." in patch
    assert "var udonPlaylist = item.GetComponent<Playlist>();" in patch
    assert "if (udonPlaylist == null) udonPlaylist = item.gameObject.AddUdonSharpComponent<Playlist>();" in patch
    assert "UdonSharpEditorUtility.CopyProxyToUdon(udonPlaylist);" in patch
    assert "new PlaylistBuildProcess().Process();" in patch


def validate_environment_and_drawing() -> None:
    for path, expected_hash in EXPECTED_CC0_HASHES.items():
        asset_bytes = path.read_bytes()
        if path == TREE_MESH:
            # The baked mesh is Unity YAML. Keep its recorded hash portable when
            # a Windows checkout predating the eol=lf rule still contains CRLF.
            asset_bytes = asset_bytes.replace(b"\r\n", b"\n")
        digest = hashlib.sha256(asset_bytes).hexdigest()
        assert digest == expected_hash, f"third-party asset SHA-256 changed: {path}: {digest}"

    manifest = json.loads(VPM_MANIFEST.read_text(encoding="utf-8"))
    assert manifest["dependencies"]["net.ureishi.qvpen"]["version"] == "3.3.15"
    assert manifest["locked"]["net.ureishi.qvpen"]["dependencies"]["com.vrchat.worlds"] == "^3.5.0"

    builder = BUILDER.read_text(encoding="utf-8")
    assert "const int tuftCount = 15000;" in builder
    assert "const float outer = 250f;" in builder
    assert "private const float HillHeight = 2.3f;" in builder
    assert "private const float HillRadius = 10f;" in builder
    assert 'QvPenPrefabPath = "Packages/net.ureishi.qvpen/QvPen(grad).prefab"' in builder
    assert 'UnyStylusPrefabPath = "Assets/Rasta/UnyStylus/UnyStylus.prefab"' in builder
    assert 'TreeMeshPath =\n            Root + "/ThirdParty/PolyHaven/JacarandaTree/Jacaranda_Quest.asset"' in builder
    assert 'CreateChild(parent, "LandmarkTree")' in builder
    assert 'CreateChild(tree.transform, "Model")' in builder
    assert "Pre-Quest landmark tree must not be in the scene." in builder
    assert "treeModel.TransformDirection(Vector3.up)" in builder
    assert "treeTriangles < 4000L || treeTriangles > 26000L" in builder
    assert "descriptorObject.AddComponent<PipelineManager>();" in builder
    assert "private static readonly Vector3 SpawnGroundPosition = new Vector3(-2.78f, 0f, -20.80f);" in builder
    assert "private static readonly Vector3 YamaPlayerPosition = new Vector3(-4f, 1.813f, -24f);" in builder
    assert "private static readonly Vector3 QvPenPosition = new Vector3(-7.6f, 0.848461f, -22.454f);" in builder
    assert "private static readonly Vector3 UnyStylusPosition = new Vector3(-8.668f, 0.858f, -20.672f);" in builder
    assert "ValidateAmenityPlacement" in builder
    assert 'material.SetTexture("_AlphaMap", alpha);' in builder

    unystylus_patch = UNYSTYLUS_PATCH_SCRIPT.read_text(encoding="utf-8")
    unystylus_readme = UNYSTYLUS_PATCH_README.read_text(encoding="utf-8")
    assert "rounded_trail_for_uny_stylus.shader" in unystylus_patch
    assert "rounded_trail_for_uny_stylus_selected.shader" in unystylus_patch
    assert "{ UNITY_TRANSFER_FOG(o, o.vertex); }" in unystylus_patch
    assert "GLES3" in unystylus_readme

    quality_settings = QUALITY_SETTINGS.read_text(encoding="utf-8")
    assert "Android: 3" in quality_settings
    assert "iPhone: 3" in quality_settings

    player_settings = PLAYER_SETTINGS.read_text(encoding="utf-8")
    for expected_call in (
        "SetWalkSpeed(walkSpeed)",
        "SetRunSpeed(runSpeed)",
        "SetStrafeSpeed(strafeSpeed)",
        "SetJumpImpulse(jumpImpulse)",
        "SetGravityStrength(gravityStrength)",
    ):
        assert expected_call in player_settings


def validate_picnic_spot() -> None:
    for path, expected_hash in EXPECTED_PICNIC_HASHES.items():
        assert path.is_file(), f"missing Tiny Treats asset: {path}"
        assert path.with_name(path.name + ".meta").is_file(), f"missing Unity meta: {path}.meta"
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert digest == expected_hash, f"Tiny Treats asset SHA-256 changed: {path}: {digest}"

    notice = (PICNIC_ROOT / "NOTICE.md").read_text(encoding="utf-8")
    license_text = (PICNIC_ROOT / "LICENSE.txt").read_text(encoding="utf-8")
    assert "da50c97a056fe1513413343787f2526ea7f25174" in notice
    assert "Creative Commons Zero v1.0 Universal" in notice
    assert "2,360 triangles" in notice
    assert "CC0" in license_text

    installer = PICNIC_INSTALLER.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")
    for expected in (
        'new GameObject("PicnicSpot")',
        "ModelImporterMeshCompression.Medium",
        'Shader.Find("StargazingHill/Environment")',
        "ConformBlanketToTerrain(",
        'LayoutPath =',
        '"Assets/StargazingHill/Editor/Data/PicnicLayout.json"',
        "CaptureCurrentLayoutForBatchMode()",
        "PlaceItemFromLayout(",
        "modelLocalPosition = model.localPosition",
        "ValidateLayoutMatchesSaved(root);",
        '"PicnicCushionBlueLeft"',
        '"PicnicCushionBlueRight"',
        '"PicnicPillowBlueLeft"',
        '"PicnicPillowBlueRight"',
        "ValidateTerrainContact(root);",
        "AllowBlanketEmbedding",
        "SoftFurnishingMinimumTerrainClearance",
        "SoftFurnishingMaximumTerrainClearance",
        "ValidateScene()",
        "triangles < 2800L || triangles > 3600L",
    ):
        assert expected in installer, f"missing picnic installer invariant: {expected}"

    assert PICNIC_LAYOUT.is_file(), "missing versioned picnic generator layout"
    assert PICNIC_LAYOUT.with_name(PICNIC_LAYOUT.name + ".meta").is_file()
    layout = json.loads(PICNIC_LAYOUT.read_text(encoding="utf-8"))
    assert layout["schemaVersion"] == 1
    expected_names = [
        "PicnicBlanketBlue",
        "PicnicRadio",
        "PicnicTeapot",
        "PicnicMug",
        "PicnicCushionBlueLeft",
        "PicnicCushionBlueRight",
        "PicnicPillowBlueLeft",
        "PicnicPillowBlueRight",
    ]
    assert [item["name"] for item in layout["items"]] == expected_names
    for item in layout["items"]:
        for field in ("position", "rotation", "scale", "modelLocalPosition", "modelLocalRotation", "modelLocalScale"):
            assert all(math.isfinite(float(value)) for value in item[field].values())
        for field in ("rotation", "modelLocalRotation"):
            quaternion = item[field]
            magnitude = math.sqrt(sum(float(quaternion[key]) ** 2 for key in ("x", "y", "z", "w")))
            assert abs(magnitude - 1.0) < 0.001, f"non-normalized picnic rotation: {item['name']}/{field}"

    # The committed scene is the visual result users open after cloning. Verify that the versioned
    # generator input was captured from that exact result, including prefab-root overrides made by hand.
    scene = WORLD_SCENE.read_text(encoding="utf-8")

    def document_at(start: int) -> str:
        end = scene.find("\n--- !u!", start + 1)
        return scene[start:] if end < 0 else scene[start:end]

    def game_object_transform(name: str) -> tuple[str, str]:
        marker = f"  m_Name: {name}\n"
        marker_index = scene.find(marker)
        assert marker_index >= 0, f"missing scene picnic object: {name}"
        start = scene.rfind("--- !u!1 &", 0, marker_index)
        game_object = document_at(start)
        transform_id = re.search(r"- component: \{fileID: (\d+)\}", game_object)
        assert transform_id, f"missing scene transform: {name}"
        transform_start = scene.find(f"--- !u!4 &{transform_id.group(1)}\n")
        assert transform_start >= 0, f"missing transform document: {name}"
        return document_at(transform_start), transform_id.group(1)

    def inline_vector(block: str, field: str, keys: tuple[str, ...]) -> dict[str, float]:
        match = re.search(rf"  {re.escape(field)}: \{{([^}}]+)\}}", block)
        assert match, f"missing {field}"
        values = {}
        for key in keys:
            value = re.search(rf"(?:^|, )\s*{key}: ([^,}}]+)", match.group(1))
            assert value, f"missing {field}.{key}"
            values[key] = float(value.group(1))
        return values

    def assert_vector_close(actual: dict[str, float], expected: dict[str, float], label: str) -> None:
        for key, expected_value in expected.items():
            assert abs(actual[key] - float(expected_value)) <= 1e-6, (
                f"scene/layout mismatch: {label}.{key}: {actual[key]} != {expected_value}"
            )

    for item in layout["items"]:
        anchor, _ = game_object_transform(item["name"])
        assert_vector_close(inline_vector(anchor, "m_LocalPosition", ("x", "y", "z")), item["position"], item["name"])
        assert_vector_close(inline_vector(anchor, "m_LocalRotation", ("x", "y", "z", "w")), item["rotation"], item["name"])
        assert_vector_close(inline_vector(anchor, "m_LocalScale", ("x", "y", "z")), item["scale"], item["name"])

        child = re.search(r"  m_Children:\s*\n  - \{fileID: (\d+)\}", anchor)
        assert child, f"missing model child: {item['name']}"
        child_start = scene.find(f"--- !u!4 &{child.group(1)} stripped\n")
        assert child_start >= 0, f"missing stripped model transform: {item['name']}"
        child_block = document_at(child_start)
        prefab_instance = re.search(r"m_PrefabInstance: \{fileID: (\d+)\}", child_block)
        assert prefab_instance, f"missing model prefab instance: {item['name']}"
        instance_start = scene.find(f"--- !u!1001 &{prefab_instance.group(1)}\n")
        instance = document_at(instance_start)

        def override_vector(prefix: str, keys: tuple[str, ...], default: float) -> dict[str, float]:
            result = {}
            for key in keys:
                value = re.search(
                    rf"propertyPath: {re.escape(prefix)}\.{key}\s*\n\s+value: ([^\r\n]+)", instance
                )
                result[key] = float(value.group(1)) if value else default
            return result

        assert_vector_close(
            override_vector("m_LocalPosition", ("x", "y", "z"), 0.0),
            item["modelLocalPosition"], f"{item['name']}/Model"
        )
        assert_vector_close(
            override_vector("m_LocalRotation", ("x", "y", "z", "w"), 0.0) | {"w": override_vector("m_LocalRotation", ("w",), 1.0)["w"]},
            item["modelLocalRotation"], f"{item['name']}/Model"
        )
        assert_vector_close(
            override_vector("m_LocalScale", ("x", "y", "z"), 1.0),
            item["modelLocalScale"], f"{item['name']}/Model"
        )
    assert "PicnicSceneInstaller.InstallForBuild(scene);" in builder
    assert "PicnicSceneInstaller.ValidateScene();" in builder
    assert "MeshCollider blanketCollider" in installer
    assert "collider.sharedMesh = asset" in installer


def validate_redistributable_package_and_debug_pickup() -> None:
    exporter = PACKAGE_EXPORTER.read_text(encoding="utf-8")
    assert 'OwnedAssetRoot = "Assets/StargazingHill"' in exporter
    assert 'BatchOutputPath = "Build/StargazingHill-redistributable.unitypackage"' in exporter
    assert 'OwnedAssetRoot + "/SourceDownloads/"' in exporter
    assert "ExportForBatchMode()" in exporter
    assert "ExportPackageOptions.Default" in exporter
    assert 'OwnedAssetRoot + "/Editor/Data/PicnicLayout.json"' in exporter
    assert "IncludeDependencies" not in exporter.replace(
        "Deliberately omits IncludeDependencies", ""
    )
    for forbidden_path in (
        '"Assets/Rasta/"',
        '"Packages/net.kwxxw.yama-stream/"',
        '"Packages/net.ureishi.qvpen/"',
    ):
        assert forbidden_path in exporter

    restore_guide = PACKAGE_README.read_text(encoding="utf-8")
    for dependency in ("YamaPlayer", "QvPen", "UnyStylus"):
        assert dependency in restore_guide

    release_workflow = RELEASE_WORKFLOW.read_text(encoding="utf-8")
    for required in (
        'tags:\n      - "v*"',
        "permissions:\n  contents: write",
        "Assets/StargazingHill.meta",
        "Build/unitypackage-meta.txt",
        "create-unitypackage@0e02a37fdb702e893dc2a96603768ca7e7dd9b54",
        "Tools/Validate-UnityPackage.py",
        "StargazingHill-redistributable.unitypackage",
        "SHA256SUMS.txt",
        "gh release create",
        "gh release upload",
    ):
        assert required in release_workflow, f"release workflow is missing: {required}"
    assert "IncludeDependencies" not in release_workflow

    pickup = DEBUG_PANEL_PICKUP.read_text(encoding="utf-8")
    return_delay = re.findall(
        r"public const float ReturnDelaySeconds = ([0-9]+(?:\.[0-9]+)?)f;", pickup
    )
    assert return_delay == ["10"]
    assert "UdonBehaviourSyncMode(BehaviourSyncMode.None)" in pickup
    assert "public override void OnPickup()" in pickup
    assert "public override void OnDrop()" in pickup
    assert "public void ReturnIfReady()" in pickup
    assert "_returnPending = false;" in pickup

    builder = BUILDER.read_text(encoding="utf-8")
    installer = (ROOT / "Assets/StargazingHill/Editor/WorldDebugPanelInstaller.cs").read_text(
        encoding="utf-8"
    )
    assert "EnsureProgramAsset(typeof(WorldDebugPanelPickup)" in builder
    assert "EnsureSettingsSystemProgramAssets();" in builder
    assert "WorldSettingsSystemInstaller.InstallForBuild(scene);" in builder
    assert "WorldSettingsSystemInstaller.ValidateScene(scene);" in builder
    assert "internal const float PanelScale = 0.20f;" in installer
    assert "PanelPosition = new Vector3(-0.842f, 1.45f, -25.342f)" in installer
    assert 'LegacyToggleObjectName = "VRDebugPanelToggle"' in installer
    assert "CreatePrimitive(ToggleObjectName" not in installer
    assert "panel.AddComponent<VRCPickup>()" in installer
    assert "WorldDebugPanelPickup.ReturnDelaySeconds" in builder

    debug_status = DEBUG_PANEL_STATUS.read_text(encoding="utf-8")
    info_toggle = INFO_LANGUAGE_TOGGLE.read_text(encoding="utf-8")
    presence_board = PRESENCE_BOARD.read_text(encoding="utf-8")
    presence_scroll = PRESENCE_SCROLL.read_text(encoding="utf-8")
    info_installer = INFO_PANEL_INSTALLER.read_text(encoding="utf-8")
    assert "debugEventPlaying" in debug_status
    assert "PLAY CURRENT" in debug_status and "STOP EVENT" in debug_status
    assert "現在を再生" in debug_status and "イベント停止" in debug_status
    assert "japaneseStatusText" in debug_status and "englishStatusText" in debug_status
    assert "traditionalChineseStatusText" in debug_status
    assert "simplifiedChineseStatusText" in debug_status
    assert "koreanStatusText" in debug_status
    assert 'new GameObject("JapaneseLabels")' in installer
    assert 'new GameObject("EnglishLabels")' in installer
    assert 'new GameObject("TraditionalChineseLabels")' in installer
    assert 'new GameObject("SimplifiedChineseLabels")' in installer
    assert 'new GameObject("KoreanLabels")' in installer
    assert 'UdonSharpUndo.AddComponent<WorldInfoLanguageToggle>(languageButton)' in installer
    assert 'CreateUiText(panel.transform, "ENGLISH"' in installer
    assert "private void ApplyLanguage()" in info_toggle
    assert "private int _languageIndex;" in info_toggle
    assert "if (_languageIndex > 4) _languageIndex = 0;" in info_toggle
    assert "traditionalChineseText" in info_toggle
    assert "simplifiedChineseText" in info_toggle
    assert "koreanText" in info_toggle
    assert "observatorySelector.SetDisplayLanguage(_languageIndex)" in info_toggle
    for label in ("日→EN", "EN→繁", "繁→简", "简→한", "한→日"):
        assert label in info_toggle
    assert "VRCPlayerApi.GetPlayerCount()" in presence_board
    assert "DefaultMaximumCapacity = 80" in presence_board
    assert "DefaultRecommendedCapacity = 40" in presence_board
    assert "DefaultHistoryCapacity = 40" in presence_board
    assert "DefaultVisibleHistoryCount = 20" in presence_board
    assert 'PlatformDataKey = "StargazingHill.Platform.v1"' in presence_board
    assert "PlayerData.SetInt(PlatformDataKey, LocalPlatform)" in presence_board
    assert "PlayerData.TryGetInt(player, PlatformDataKey" in presence_board
    assert "#if UNITY_ANDROID || UNITY_IOS" in presence_board
    assert "#elif UNITY_STANDALONE_WIN" in presence_board
    assert "public void ScrollHistoryNewer()" in presence_board
    assert "public void ScrollHistoryOlder()" in presence_board
    assert "presenceBoard.ScrollHistoryOlder()" in presence_scroll
    assert '"\\nRECOMMENDED  " + recommendedCapacity' in presence_board
    assert "public override void OnPlayerJoined" in presence_board
    assert "public override void OnPlayerLeft" in presence_board
    assert 'PanelName = "WorldInformationPanel"' in info_installer
    assert "DestroyImmediate(sheet.GetComponent<Collider>())" in info_installer
    assert "Position = new Vector3(1.471f, 1.999f, -26.29f)" in info_installer
    assert "LanguageTogglePosition = new Vector3(1.03f, ObservatoryControlY, -0.0125f)" in info_installer
    assert "DebugTogglePosition = new Vector3(1.72f, ObservatoryControlY, -0.0125f)" in info_installer
    assert "LaptopIconTop = 0.584f" in info_installer
    assert "MobileIconTop = 0.456f" in info_installer
    assert "ApplyGeneratedLayoutMigrationIfNeeded" not in info_installer
    assert "[InitializeOnLoad]" not in info_installer
    assert 'new GameObject("Visual")' not in info_installer
    assert 'CreateGroup(panel.transform, "Visual")' in info_installer
    assert 'CreateGroup(visualGroup, "Descriptions")' in info_installer
    assert 'CreateGroup(visualGroup, "Presence")' in info_installer
    assert 'CreateGroup(panel.transform, "Controls")' in info_installer
    assert 'CreateGroup(controlsGroup, "Observatory")' in info_installer
    assert "Manual changes inside InformationSystem will be lost." in info_installer
    assert "PresenceTop = 0.88f" in info_installer
    assert "HistoryTop = 0.31f" in info_installer
    assert "HistoryHeightPixels = 610f" in info_installer
    assert "presence.historyCapacity = WorldPresenceBoard.DefaultHistoryCapacity" in info_installer
    assert "presence.visibleHistoryCount = WorldPresenceBoard.DefaultVisibleHistoryCount" in info_installer
    assert 'new GameObject("PlatformLaptopIcon")' in info_installer
    assert 'new GameObject("PlatformMobileIcon")' in info_installer
    assert PANEL_FONT.is_file()
    assert hashlib.sha256(PANEL_FONT.read_bytes()).hexdigest() == (
        "6bcb2a0703aa137e874fc2dffa85f6c21ba9a67fa329e81b8c801663af7e992a"
    )
    assert "ThirdParty/Fonts/NotoSansCJKkr-Regular.otf" in info_installer
    assert "東京の現在時刻" not in info_installer
    assert "current time in Tokyo" not in info_installer
    assert "EnableUiBeamForInteraction(button, backing);" in info_installer
    assert "VRCUiShape" in info_installer
    assert "1, 0, 20, 21, 2, 3, 4, 5, 6" in info_installer
    assert "int catalogIndex = ObservatoryVisualOrder[visualIndex];" in info_installer
    assert 'observatoryList.Find("Location_01")' in builder
    assert 'observatoryList.Find("Location_06")' in builder
    assert 'observatoryList.Find("Location_19")' in builder
    assert 'observatoryList.Find("Location_21")' not in builder

    observatory_selector = OBSERVATORY_SELECTOR.read_text(encoding="utf-8")
    observatory_button = OBSERVATORY_BUTTON.read_text(encoding="utf-8")
    assert "UdonBehaviourSyncMode(BehaviourSyncMode.Manual)" in observatory_selector
    assert "[UdonSynced] public int selectedIndex" in observatory_selector
    assert "Networking.SetOwner(localPlayer, gameObject)" in observatory_selector
    assert "RequestSerialization()" in observatory_selector
    assert "public override void OnDeserialization()" in observatory_selector
    assert "skyController.ApplyCurrentSkyRotation()" in observatory_selector
    assert "meteorController.latitudeDegrees = latitude" in observatory_selector
    assert "public void SetDisplayLanguage(int languageIndex)" in observatory_selector
    assert "observatoryHeadingLabel.text = localizedHeadingLabels[_displayLanguageIndex]" in observatory_selector
    assert "selectedLocationLabel.text = GetLocalizedDisplayName(selectedIndex);" in observatory_selector
    assert "locationListLabels[index].text = GetLocalizedDisplayName(index);" in observatory_selector
    assert "displayNamesJapanese" in observatory_selector
    assert "displayNamesTraditionalChinese" in observatory_selector
    assert "displayNamesSimplifiedChinese" in observatory_selector
    assert "displayNamesKorean" in observatory_selector
    assert 'displayNames[selectedIndex] + "  (global)"' not in observatory_selector
    assert "debugPanelRoot.SetActive(!debugPanelRoot.activeSelf)" in observatory_selector
    assert "ExpectedLocationCount = 22" in observatory_selector
    assert "ActionSelectLocation = 3" in observatory_button
    assert "ActionToggleDebugPanel = 4" in observatory_button
    assert "selector.SelectPrevious()" in observatory_button
    assert "selector.SelectNext()" in observatory_button
    assert "selector.SelectLocation(locationIndex)" in observatory_button
    assert "public int[] selectionOrder;" in observatory_selector
    assert "CommitGlobalSelection(selectionOrder[nextOrderIndex]);" in observatory_selector
    assert "selector.selectionOrder = ObservatoryVisualOrder;" in info_installer

    expected_profiles = (
        "tokyo", "sapporo", "osaka", "takamatsu-kagawa", "oita", "miyazaki",
        "naha-okinawa", "rome", "paris", "moscow", "washington-dc", "san-francisco",
        "los-angeles", "las-vegas", "new-york", "ottawa", "canberra", "jakarta",
        "beijing", "seoul", "tottori", "matsue-shimane",
    )
    for profile in expected_profiles:
        assert f'"{profile}"' in info_installer, f"missing observatory profile: {profile}"
    profile_catalog = re.search(
        r"private static readonly string\[\] ObservatoryProfileIds\s*=\s*\{(.*?)\};",
        info_installer,
        re.DOTALL,
    )
    assert profile_catalog, "missing observatory profile catalog"
    actual_profiles = tuple(
        re.findall(r'"([^"\\]*(?:\\.[^"\\]*)*)"', profile_catalog.group(1))
    )
    assert actual_profiles == expected_profiles, (
        "observatory profile indices are persistent network state and must remain append-only"
    )
    localized_catalogs = (
        "ObservatoryDisplayNames",
        "ObservatoryDisplayNamesJapanese",
        "ObservatoryDisplayNamesTraditionalChinese",
        "ObservatoryDisplayNamesSimplifiedChinese",
        "ObservatoryDisplayNamesKorean",
    )
    for catalog_name in localized_catalogs:
        match = re.search(
            rf"private static readonly string\[\] {catalog_name}\s*=\s*\{{(.*?)\}};",
            info_installer,
            re.DOTALL,
        )
        assert match, f"missing localized observatory catalog: {catalog_name}"
        assert len(re.findall(r'"(?:[^"\\]|\\.)*"', match.group(1))) == 22, (
            f"localized observatory catalog must contain 22 names: {catalog_name}"
        )
    for translated_name in (
        "東京（日本）", "鳥取（日本）", "松江・島根（日本）", "ワシントンD.C.（アメリカ）", "ソウル（韓国）",
        "東京，日本", "鳥取，日本", "松江（島根），日本", "華盛頓特區，美國", "首爾，韓國",
        "东京，日本", "鸟取，日本", "松江（岛根），日本", "华盛顿特区，美国", "首尔，韩国",
        "도쿄, 일본", "돗토리, 일본", "마쓰에(시마네), 일본", "워싱턴 D.C., 미국", "서울, 한국",
    ):
        assert f'"{translated_name}"' in info_installer, f"missing observatory translation: {translated_name}"
    assert "toggle.observatorySelector = selector;" in info_installer
    assert "selector.locationListLabels = locationListLabels;" in info_installer
    assert "private const float PanelTop = 1.25f" in info_installer
    assert "private const float PanelBottom = -1.85f" in info_installer
    assert '"星空の基準地点  (global)"' in info_installer
    assert '"SKY REFERENCE LOCATION  (global)"' in info_installer
    assert '"星空基準地點  (global)"' in info_installer
    assert '"星空基准地点  (global)"' in info_installer
    assert '"별하늘 기준 위치  (global)"' in info_installer
    assert 'parent.name = "ObservatoryHeading"' in info_installer
    assert '"OBSERVATORY  (global)"' not in info_installer
    assert '"星空の基準地点 / SKY VIEWPOINT"' not in info_installer
    assert '"Tokyo, Japan  (global)"' not in info_installer
    assert 'new GameObject("ObservatoryLocationList")' in info_installer
    assert "int catalogIndex = ObservatoryVisualOrder[visualIndex];" in info_installer
    assert "int row = visualIndex / 3;" in info_installer
    assert "int column = visualIndex % 3;" in info_installer
    assert "float x = -1.38f + column * 1.38f;" in info_installer
    assert "private const float ObservatoryListBottomY = -1.10f;" in info_installer
    assert "private const float ObservatoryListRowSpacing = 0.20f;" in info_installer
    assert "private const float ObservatoryListBackdropY = -0.40f;" in info_installer
    assert "float y = ObservatoryListBottomY + row * ObservatoryListRowSpacing;" in info_installer
    assert '"ObservatoryPrevious", "◀"' in info_installer
    assert '"ObservatoryNext", "▶"' in info_installer
    assert '"DebugPanelToggle", "DEBUG: OFF"' in info_installer
    assert "WorldObservatorySelector.Action" not in info_installer
    assert "WorldObservatoryButton.ActionPrevious" in info_installer
    assert "WorldObservatoryButton.ActionNext" in info_installer
    assert "WorldObservatoryButton.ActionToggleList" in info_installer
    assert "WorldObservatoryButton.ActionSelectLocation" in info_installer
    assert "WorldObservatoryButton.ActionToggleDebugPanel" in info_installer
    assert "InstallOrRefreshForBatchMode()" in info_installer
    assert "public static void ValidateForBatchMode()" in info_installer
    assert 'Debug.Log("World information panel validation passed.")' in info_installer

    settings_installer = SETTINGS_INSTALLER.read_text(encoding="utf-8")
    settings_controller = SETTINGS_CONTROLLER.read_text(encoding="utf-8")
    settings_button = SETTINGS_BUTTON.read_text(encoding="utf-8")
    settings_pickup = SETTINGS_PICKUP.read_text(encoding="utf-8")
    radio_speaker = RADIO_SPEAKER.read_text(encoding="utf-8")
    compass_installer = COMPASS_INSTALLER.read_text(encoding="utf-8")
    compass_needle = COMPASS_NEEDLE.read_text(encoding="utf-8")
    night_shader = NIGHT_MODE_SHADER.read_text(encoding="utf-8")
    for path, expected_hash in EXPECTED_COMPASS_HASHES.items():
        assert path.exists(), f"Missing recorded CC0 compass asset: {path}"
        assert hashlib.sha256(path.read_bytes()).hexdigest() == expected_hash
    assert 'new GameObject("SettingsSystem")' in settings_installer
    assert 'new GameObject("LocalSettingsBoard")' in settings_installer
    assert "board.SetActive(false);" in settings_installer
    assert "private const float BoardScale = 0.22f;" in settings_installer
    assert "pickupCollider.center = new Vector3(0f, 1.27f, 0.05f);" in settings_installer
    assert "pickupCollider.size = new Vector3(2.86f, 0.14f, 0.16f);" in settings_installer
    assert "pickup.proximity = 0.35f;" in settings_installer
    assert "pickup.UseText = string.Empty;" in settings_installer
    assert "WorldInformationPanelInstaller.EnableUiBeamForInteraction(button, backing);" in settings_installer
    assert "VRCMirrorReflection" in settings_installer
    assert "mirrorsLow.Length != 5" in settings_installer
    assert "mirrorsHigh.Length != 5" in settings_installer
    assert '"maximumAntialiasing", antialiasing' in settings_installer
    assert '"m_DisablePixelLights", disablePixelLights' in settings_installer
    assert "CreateNightSlider" in settings_installer and "VRCUiShape" in settings_installer
    assert '"RadioVolumeSliderCanvas"' in settings_installer
    # VRChat resolves world-space UI through a collider before the GraphicRaycaster, and uGUI only
    # emits drag events when an EventSystem exists. Both knobs were inert without these.
    assert "WorldInformationPanelInstaller.ConfigureWorldUiCanvas(canvas);" in settings_installer
    assert "ValidateWorldUiTarget(controller.nightSlider.gameObject);" in settings_installer
    assert "ValidateWorldUiTarget(controller.radioVolumeSlider.gameObject);" in settings_installer
    # VRChat drops the UI layer from its interactive mask while the menu is closed, and its camera
    # does not photograph that layer, so world UI stays on Default with a collider to hit.
    assert "internal const int WorldUiLayer = 0;" in info_installer
    assert "internal static BoxCollider ConfigureWorldUiCanvas(Canvas canvas)" in info_installer
    assert "internal static void ValidateWorldUiTargets(GameObject root)" in info_installer
    assert "ValidateWorldUiTargets(panel);" in info_installer
    assert 'LayerMask.NameToLayer("UI")' not in info_installer
    assert 'LayerMask.NameToLayer("UI")' not in settings_installer
    assert 'private const string BeamTargetName = "UiBeamTarget";' in info_installer
    assert "WorldInformationPanelInstaller.EnableUiBeamForInteraction(button, backing);" in installer
    assert "WorldInformationPanelInstaller.EnableUiBeamForInteraction(languageButton, languageBacking);" in installer
    assert "EnsureEventSystem(scene);" in settings_installer
    assert "existing.gameObject.AddComponent<StandaloneInputModule>();" in settings_installer
    assert "FindObjectOfType<EventSystem>(true) == null" in settings_installer
    assert "new Vector2(380f, 44f)" in settings_installer
    assert "new Vector2(320f, 40f)" in settings_installer
    # One ON/OFF toggle per direction, one clear-all, one shared LQ/HQ switch.
    assert "Text[] mirrorButtonTexts = new Text[mirrorDirections.Length + 2];" in settings_installer
    assert "controller.mirrorButtonTexts.Length != 7" in settings_installer
    assert "controller.actionButtonTexts.Length != 10" in settings_installer
    assert "buttons.Length != 23" in settings_installer
    # Sliders cannot be dragged on desktop, so each has step buttons alongside.
    assert 'CreateButton(board.transform, "NightDown"' in settings_installer
    assert 'CreateButton(board.transform, "RadioVolumeUp"' in settings_installer
    assert "public void AdjustNight(int deltaPercent)" in settings_controller
    assert "public void AdjustRadioVolume(int deltaPercent)" in settings_controller
    assert "NightAdjust = 13" in settings_button
    # UdonSharp compiles Interact() to the "_interact" entry point, so the click must call
    # UdonBehaviour.Interact directly; SendCustomEvent("Interact") reaches nothing.
    assert "AddVoidPersistentListener(uiButton.onClick, backing.Interact)" in info_installer
    assert 'AddStringPersistentListener(uiButton.onClick, backing.SendCustomEvent, "Interact")' not in info_installer
    # Local join/leave chime and head-following toast, each switchable, neither synced.
    assert "CreatePresenceNotifier(system.transform, font, textMaterial)" in settings_installer
    assert 'CreateButton(board.transform, "NotifySoundToggle"' in settings_installer
    assert 'CreateButton(board.transform, "NotifyDisplayToggle"' in settings_installer
    assert "EnsureChimeClip(JoinChimePath, 660f, 990f)" in settings_installer
    assert "EnsureChimeClip(LeaveChimePath, 880f, 587f)" in settings_installer
    assert "Local join/leave notifier must stay local" in settings_installer
    assert "NotifySoundToggle = 11" in settings_button
    assert "NotifyDisplayToggle = 12" in settings_button
    assert "public void ToggleNotifySound()" in settings_controller
    assert "public void ToggleNotifyDisplay()" in settings_controller
    assert 'NotifySoundKey = "StargazingHill.Settings.NotifySound"' in settings_controller
    assert 'NotifyDisplayKey = "StargazingHill.Settings.NotifyDisplay"' in settings_controller
    presence_notifier = PRESENCE_NOTIFIER.read_text(encoding="utf-8")
    assert "UdonBehaviourSyncMode(BehaviourSyncMode.None)" in presence_notifier
    assert "public override void OnPlayerJoined(VRCPlayerApi player)" in presence_notifier
    assert "public override void OnPlayerLeft(VRCPlayerApi player)" in presence_notifier
    # The initial join replay must not fire a burst of chimes in a busy instance.
    assert "if (Utilities.IsValid(player) && player.isLocal)" in presence_notifier
    assert "_live = true;" in presence_notifier
    assert "public void SetSoundEnabled(bool enabled)" in presence_notifier
    assert "public void SetDisplayEnabled(bool enabled)" in presence_notifier
    # Arrivals and departures collect into a short window: one chime and one line per group.
    assert "public const float WindowSeconds = 3f;" in presence_notifier
    assert "private string ComposeLine(bool joined, string displayName, int others)" in presence_notifier
    assert "さんほか" in presence_notifier
    assert "verticalOffset = -0.48f" in presence_notifier
    # The toast dims out rather than blinking away.
    assert "public const float FadeSeconds = 1.2f;" in presence_notifier
    assert "private void ApplyFade()" in presence_notifier
    # Pointer targets need real depth so a shallow ray still crosses them.
    assert "internal const float UiTargetWorldDepth = 0.012f;" in info_installer
    assert "source.volume = 0.22f;" in settings_installer
    # Night mode multiplies the scene toward black; a tinted overlay read as pale haze.
    assert "fixed4(0, 0, 0, _Darkness)" in night_shader
    assert "_Darkness (\"Darkness\", Range(0, 1))" in night_shader
    assert 'nightOverlayMaterial.SetFloat("_Darkness", _nightAmount)' in settings_controller
    # Compass cardinals are rasterised far above the default canvas density.
    assert "CardinalTextCanvasScale = 0.0001f" in compass_installer
    # The beam surface is a canvas-root graphic, matching the sliders that already worked.
    assert "Image surface = canvas.GetComponent<Image>();" in info_installer
    assert "private static Canvas EnsureBeamCanvas(GameObject button)" in info_installer
    assert "DisableLabelRaycast(button);" in info_installer
    assert "EnsureProgramAsset(typeof(WorldPresenceNotifier)" in builder
    assert 'CreateButton(board.transform, "MirrorsAllOff"' in settings_installer
    assert 'CreateButton(board.transform, "MirrorQualityToggle"' in settings_installer
    assert "WorldSettingsButton.MirrorQuality" in settings_installer
    assert "MirrorQuality = 10" in settings_button
    assert "public void ToggleMirror(int mirrorIndex)" in settings_controller
    assert "public void ToggleMirrorQuality()" in settings_controller
    assert 'MirrorHighQualityKey = "StargazingHill.Settings.MirrorHighQuality"' in settings_controller
    # The mirrors are measured from the rendered mat instead of the off-centre saved anchor.
    assert "ResolveBlanketFrame(out center, out forward, out right, out halfForward, out halfRight);" in settings_installer
    assert "private const float MirrorEdgeMargin = 0.06f;" in settings_installer
    assert "private const float MirrorGroundClearance = 0.02f;" in settings_installer
    assert "footing.y = MirrorBaseHeight(footing) + MirrorHeight * 0.5f;" in settings_installer
    assert "private const float MirrorHeight = 2.15f;" in settings_installer
    assert "private const float MirrorCeilingHeight = 2.55f;" in settings_installer
    assert "Picnic mirror is not aligned to the mat edge at index" in settings_installer
    assert "ConfigureRadioSpeaker" in settings_installer and "YamaPlayerSpeaker" in settings_installer
    assert "PlayerData.TryGet" in settings_controller and "OnPlayerRestored" in settings_controller
    assert "PlayerData.SetBool(SaveKey, true)" in settings_controller
    assert "DateTime.Now" in settings_controller
    assert "ReturnDelaySeconds = 10f" in settings_pickup
    assert "BoardToggle = 8" in settings_button
    assert "TreeTogglePosition = new Vector3(8.261f, 2.331f, 7.755f)" in settings_installer
    assert "private const float TreeToggleScale = 0.46967f;" in settings_installer
    assert "0.627459f, 0.33232313f, -0.35606158f, 0.6075168f" in settings_installer
    assert 'CreateButton(dock.transform, "Toggle", string.Empty' in settings_installer
    assert "BoardPosition = new Vector3(8.515f, 2.778f, 7.217f)" in settings_installer
    assert "interactionCollider.center = new Vector3(0f, 0f, -1.15f);" in settings_installer
    assert "float colliderFaceSize = 0.30f / (0.24f * TreeToggleScale);" in settings_installer
    assert "interactionCollider.size = new Vector3(colliderFaceSize, colliderFaceSize, 2.60f);" in settings_installer
    assert "CreateGearIcon(dock.transform, iconMaterial);" in settings_installer
    assert 'new GameObject("GearIcon")' in settings_installer
    assert "EnsureGearIconMesh()" in settings_installer
    assert 'shader.name != "Unlit/Color"' in settings_installer
    assert "DestroyImmediate(interactionCanvas.gameObject)" in settings_installer
    assert "ValidateTreeSettingsAccess(" in settings_installer
    assert 'new Vector3(0f, 0f, -0.040f)' in settings_installer
    assert "RenderTreeSettingsTogglePreviewForBatchMode" in builder
    assert "RenderOpenSettingsLayoutPreviewForBatchMode" in builder
    assert 'CreateCube("GearSpoke"' not in settings_installer
    assert "speakerSource.enabled = true" in radio_speaker
    # The YamaPlayer master volume defaults to 0.1, so scaling by it left the radio inaudible.
    # The board volume is an absolute local gain; only mute still follows the main speaker.
    assert "speakerSource.volume = _speakerEnabled ? _localVolume : 0f;" in radio_speaker
    assert "referenceSource.volume * _localVolume" not in radio_speaker
    assert "public const float DefaultLocalVolume = 0.85f;" in radio_speaker
    assert "private bool _speakerEnabled = true;" in radio_speaker
    assert "private bool _radioEnabled = true;" in settings_controller
    assert "source.minDistance = 1.5f;" in settings_installer
    assert 'near.floatValue = 1.5f;' in settings_installer
    assert "SetLocalVolume(float volume)" in radio_speaker
    assert "SetSpeakerEnabled(bool enabled)" in radio_speaker
    assert "public override void Interact()" not in radio_speaker
    assert 'new GameObject("RadioUseTrigger")' not in settings_installer
    assert "backing.InteractionText = string.Empty;" in settings_installer
    assert "backing.proximity = 0f;" in settings_installer
    assert "RadioToggle = 6" in settings_button
    assert "ToggleRadio()" in settings_controller
    assert 'RadioEnabledKey = "StargazingHill.Settings.RadioEnabled"' in settings_controller
    assert 'LegacyRadioUseKey = "StargazingHill.Settings.RadioUse"' in settings_controller
    assert 'RadioVolumeKey = "StargazingHill.Settings.RadioVolume"' in settings_controller
    assert "PlayerData.SetFloat(RadioVolumeKey, _radioVolume)" in settings_controller
    assert 'MirrorQuality4Key = "StargazingHill.Settings.MirrorQuality4"' in settings_controller
    assert "CompassSceneInstaller.InstallForBuild(scene);" in settings_installer
    assert 'new GameObject("LocalNorthCompass")' in compass_installer
    # Only the ground plan is authored; the resting height is measured from the surface below,
    # which is what stopped the compass from hovering above the picnic mat.
    assert "CompassGroundPosition = new Vector2(7.52f, 7.48f)" in compass_installer
    assert "private const float CompassRestClearance = 0.002f;" in compass_installer
    assert "ResolveCompassPosition(null)" in compass_installer
    assert "Physics.RaycastAll(" in compass_installer
    assert "pickup.proximity = 0.4f;" in compass_installer
    assert "compass.AddComponent<VRCPickup>()" in compass_installer
    assert "compass.AddComponent<VRCObjectSync>()" in compass_installer
    assert "UdonSharpUndo.AddComponent<LocalCompassNeedle>(compass)" in compass_installer
    assert "UdonBehaviourSyncMode(BehaviourSyncMode.None)" in compass_needle
    assert "transform.InverseTransformDirection(Vector3.forward)" in compass_needle
    assert "Quaternion.LookRotation(localNorth.normalized, Vector3.up)" in compass_needle
    assert "ReturnDelaySeconds = 10f" in compass_needle
    assert "Networking.IsOwner(gameObject)" in compass_needle
    assert "objectSync.Respawn()" in compass_needle
    assert 'Shader "StargazingHill/NightModeOverlay"' in night_shader


def validate_sky_reference() -> None:
    builder = BUILDER.read_text(encoding="utf-8")
    # J2000.0 is JD 2451545.0; the controller's GMST polynomial starts at
    # 280.46061837 degrees there. Tokyo longitude gives the expected LST below.
    j2000_jd = 2451545.0
    gmst_degrees = 280.46061837 + 360.98564736629 * (j2000_jd - 2451545.0)
    tokyo_lst = (gmst_degrees + 139.76) % 360.0
    assert math.isclose(tokyo_lst, 60.22061837, abs_tol=1e-9)

    latitude = math.radians(35.68)
    sidereal = math.radians(tokyo_lst)
    north_pole = (0.0, math.sin(latitude), math.cos(latitude))
    ra_six_hours = (
        math.cos(sidereal),
        math.cos(latitude) * math.sin(sidereal),
        -math.sin(latitude) * math.sin(sidereal),
    )
    dot = sum(left * right for left, right in zip(north_pole, ra_six_hours))
    assert math.isclose(sum(value * value for value in north_pole), 1.0, abs_tol=1e-12)
    assert math.isclose(sum(value * value for value in ra_six_hours), 1.0, abs_tol=1e-12)
    assert math.isclose(dot, 0.0, abs_tol=1e-12)

    controller = SKY_CONTROLLER.read_text(encoding="utf-8")
    assert "Networking.GetNetworkDateTime()" in controller
    assert "latitudeDegrees = 35.68f" in controller
    assert "longitudeDegreesEast = 139.76f" in controller
    assert "Vector3 observer = _localPlayer.GetPosition();" in controller
    assert "celestialSphere.position = observer;" in controller
    assert "CalculateSkyRotation(" in controller
    assert "DebugAdvanceOneHour()" in controller
    assert "DebugResetTimeOffset()" in controller
    assert "CalculateMoonDirection(" in controller
    assert "EquatorialDirectionToHorizontal(" in controller
    assert "topocentric parallax on an oblate Earth" in controller

    profile = OBSERVATORY_PROFILE.read_text(encoding="utf-8")
    assert "profileId: tokyo" in profile
    assert "displayName: Tokyo" in profile
    assert "latitudeDegrees: 35.68" in profile
    assert "longitudeDegreesEast: 139.76" in profile

    meteor = METEOR_CONTROLLER.read_text(encoding="utf-8")
    assert "GetHourlyEventId(" in meteor
    assert "DebugTriggerHourlyEvent()" in meteor
    assert "DebugPreviewEventAtSecond(" in meteor
    assert "public void DebugTriggerSelectedShower()" in meteor
    assert "DebugPreviewSelectedShowerAtSecond(" in meteor
    assert "DebugForcedMeteorCount = 20" in meteor
    natural_duration = re.findall(
        r"public const float NaturalEventDurationSeconds = ([0-9]+(?:\.[0-9]+)?)f;", meteor
    )
    debug_duration = re.findall(
        r"public const float DebugForcedPreviewDurationSeconds = ([0-9]+(?:\.[0-9]+)?)f;", meteor
    )
    assert len(natural_duration) == 1 and float(natural_duration[0]) > 0.0
    assert len(debug_duration) == 1 and float(debug_duration[0]) > 0.0
    assert "forcedPreview && slot == 0" in meteor
    assert "const float waveLength = 5f;" in meteor
    assert "eventDurationSeconds" not in meteor
    assert "Networking.GetNetworkDateTime()" in meteor
    assert "CalculateVisualTier(" in meteor
    assert "CalculateMeteorDuration(" in meteor
    assert "debugWidthScale" not in meteor
    assert "debugLengthScale" not in meteor
    assert "DebugToggleHourlyEvent()" in meteor
    assert "_suppressedNaturalEventId" in meteor
    assert "debugCurrentShowerId" in meteor

    meteor_shader = METEOR_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Meteor"' in meteor_shader
    assert "Blend One One" in meteor_shader
    assert "_TailColor" in meteor_shader
    assert "_CoreColor" in meteor_shader
    assert "_HeadColor" in meteor_shader
    assert "_Afterglow" in meteor_shader
    assert '#include "StargazingAtmosphere.cginc"' in meteor_shader
    assert "StargazingAtmosphericTransmission(" in meteor_shader
    assert "brightness * input.atmosphere" in meteor_shader

    starfield_shader = STARFIELD_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Starfield"' in starfield_shader
    assert '#include "StargazingAtmosphere.cginc"' in starfield_shader
    assert "StargazingAtmosphericTransmission(" in starfield_shader
    assert "i.atmosphericTransmission * _Intensity" in starfield_shader

    atmosphere = ATMOSPHERE_INCLUDE.read_text(encoding="utf-8")
    assert "extinctionCoefficient * (airmass - 1.0)" in atmosphere
    assert "exp2(-1.32877124 * extinctionMagnitudes)" in atmosphere
    assert 'starMaterial.SetFloat("_ExtinctionCoefficient", 0.23f);' in builder
    assert 'starMaterial.SetFloat("_MinimumSinAltitude", 0.05f);' in builder
    assert 'material.SetFloat("_HorizonFull", Mathf.Sin(12f * Mathf.Deg2Rad));' in builder

    # Match the reference implementation: k=0.23 mag/airmass and
    # X=1/max(sin(altitude), 0.05). Zenith must remain unchanged while
    # low-altitude light is attenuated monotonically.
    def atmospheric_transmission(altitude_degrees: float) -> float:
        sin_altitude = max(math.sin(math.radians(altitude_degrees)), 0.05)
        extinction_magnitudes = 0.23 * (1.0 / sin_altitude - 1.0)
        return 10.0 ** (-0.4 * extinction_magnitudes)

    transmissions = [atmospheric_transmission(value) for value in (90.0, 30.0, 10.0, 0.0)]
    assert math.isclose(transmissions[0], 1.0, abs_tol=1e-9)
    assert transmissions[0] > transmissions[1] > transmissions[2] > transmissions[3]
    assert math.isclose(transmissions[1], 0.8093, abs_tol=0.001)
    assert math.isclose(transmissions[2], 0.3649, abs_tol=0.001)

    moon_shader = MOON_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Moon"' in moon_shader

    night_sky_shader = NIGHT_SKY_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/NightSkyGradient"' in night_sky_shader
    assert "_HorizonColor" in night_sky_shader
    assert "_ZenithColor" in night_sky_shader
    assert "_GroundColor" in night_sky_shader
    assert "RenderSettings.ambientMode = AmbientMode.Flat;" in builder
    assert "RenderSettings.fog = true;" in builder

    catalog = SHOWER_CATALOG.read_text(encoding="utf-8")
    assert "sourceUrl: https://imo.net/files/meteor-shower/cal2026.pdf" in catalog
    assert "verifiedDate: 2026-08-12" in catalog
    for field, expected in EXPECTED_SHOWERS.items():
        match = re.search(rf"^  {field}: \[(.*?)\]$", catalog, re.MULTILINE)
        assert match, f"missing meteor shower field: {field}"
        values = [value.strip() for value in match.group(1).split(",")]
        actual = values if field == "ids" else [float(value) for value in values]
        assert actual == expected, f"unexpected IMO 2026 {field}: {actual}"

    assert 'MenuItem("Stargazing Hill/Preview & Debug/Trigger Hourly Meteor Shower"' in builder
    assert 'MenuItem("Stargazing Hill/Preview & Debug/Advance Sky +1 Hour"' in builder
    assert "TestSkyAndMeteorForBatchMode" in builder
    assert "CreateOrUpdateMeteorMaterials" in builder
    assert "UpgradeMeteorVisualsForBatchMode" in builder

    debug_window = (ROOT / "Assets/StargazingHill/Editor/MeteorShowerDebugWindow.cs").read_text(encoding="utf-8")
    assert 'MenuItem("Stargazing Hill/Preview & Debug/Meteor Shower Preview..."' in debug_window
    assert 'MenuItem("Stargazing Hill/Preview & Debug/Force Perseids Preview (20 Meteors)"' in debug_window
    assert "backing.SetProgramVariable(nameof(MeteorController.debugRequestedShowerIndex), showerIndex);" in debug_window
    assert "backing.SendCustomEvent(nameof(MeteorController.DebugTriggerSelectedShower));" in debug_window
    assert "Networking.LocalPlayer" in debug_window
    assert "controller.DebugTriggerSelectedShower(showerIndex, viewForward);" not in debug_window

    clientsim_guard = CLIENTSIM_GUARD.read_text(encoding="utf-8")
    assert 'private const string PackageVersion = "3.10.4";' in clientsim_guard
    assert "if(!udonBehaviour.IsInitialized)" in clientsim_guard
    assert "udonBehaviour.IsNetworkingSupported = true;" in clientsim_guard
    assert "did not match the verified source" in clientsim_guard
    assert "five USNO lunar references <=0.10 degrees" in builder
    assert "five observatories" in builder
    assert "11 IMO showers" in builder

    for year, month, day, hour, minute, expected_altitude, expected_azimuth in USNO_MOON_REFERENCES:
        altitude, azimuth = calculate_moon_horizontal(
            year, month, day, hour, minute, 0.0, 35.68, 139.76
        )
        azimuth_error = abs((azimuth - expected_azimuth + 180.0) % 360.0 - 180.0)
        assert abs(altitude - expected_altitude) <= 0.10, (
            f"USNO Moon altitude error at {year}-{month:02}-{day:02}: {altitude}"
        )
        assert azimuth_error <= 0.10, (
            f"USNO Moon azimuth error at {year}-{month:02}-{day:02}: {azimuth}"
        )

    for latitude, longitude in ((35.68, 139.76), (37.7749, -122.4194),
                                (41.9028, 12.4964), (55.7558, 37.6173),
                                (43.6532, -79.3832)):
        pole_altitude = math.degrees(math.asin(math.sin(math.radians(latitude))))
        assert math.isclose(pole_altitude, latitude, abs_tol=1e-9), (latitude, longitude)


def validate_public_documentation() -> None:
    builder = BUILDER.read_text(encoding="utf-8")
    info_installer = INFO_PANEL_INSTALLER.read_text(encoding="utf-8")
    picnic_installer = PICNIC_INSTALLER.read_text(encoding="utf-8")
    package_exporter = PACKAGE_EXPORTER.read_text(encoding="utf-8")
    assert 'MenuItem("Stargazing Hill/Validate Saved Scene"' in builder
    assert 'MenuItem("Stargazing Hill/Content/Information Panel/Select in Hierarchy"' in info_installer
    assert 'MenuItem("Stargazing Hill/Content/Information Panel/Validate"' in info_installer
    assert 'MenuItem("Stargazing Hill/Advanced/Generated Content/Rebuild InformationSystem' in info_installer
    assert 'MenuItem("Stargazing Hill/Content/Picnic/' in picnic_installer
    assert 'MenuItem("Stargazing Hill/Build & Export/Redistributable UnityPackage...' in package_exporter
    assert 'MenuItem("Stargazing Hill/Advanced/Generated Content/Rebuild Complete World' in builder
    assert "Stargazing Hill/Integrations" not in builder

    public_markdown_files = list(ROOT.glob("*.md")) + list((ROOT / "docs").rglob("*.md"))
    forbidden_private_reference_patterns = (
        r"VRChat-World_[A-Za-z0-9_-]+",
        r"Assets/[A-Za-z0-9_-]*World/",
    )
    for markdown_file in public_markdown_files:
        content = markdown_file.read_text(encoding="utf-8")
        for private_reference_pattern in forbidden_private_reference_patterns:
            assert re.search(private_reference_pattern, content) is None, (
                f"private reference remains in public documentation: "
                f"{markdown_file.relative_to(ROOT)}"
            )

    language_links = (
        "README.md",
        "README.en.md",
        "README.zh-Hant.md",
        "README.zh-Hans.md",
        "README.ko.md",
    )
    for readme in LOCALIZED_READMES:
        assert readme.is_file(), f"missing localized README: {readme.name}"
        content = readme.read_text(encoding="utf-8")
        for link in language_links:
            assert f"]({link})" in content, f"missing language link {link} in {readme.name}"
        assert "Stargazing Hill/Validate Saved Scene" in content
        assert "Stargazing Hill/Build & Export/Redistributable UnityPackage..." in content
        assert "docs/PUBLIC_RELEASE_AUDIT.md" in content

    audit = PUBLIC_RELEASE_AUDIT.read_text(encoding="utf-8")
    notice = PROJECT_NOTICE.read_text(encoding="utf-8")
    for required in (
        "Blueprint ID",
        "QvPen / YamaPlayer / UnyStylus",
        "GitHub Release",
        "stargazing",
        "astronomy",
        "hangout",
    ):
        assert required in audit, f"public release audit is missing: {required}"
    for required in (
        "Third-party material",
        "HYG-derived stellar data",
        "com.vrchat.core.vpm-resolver",
        "YamaPlayer, QvPen, and the purchased UnyStylus",
    ):
        assert required in notice, f"project notice is missing: {required}"

    guide = STARFIELD_GUIDE.read_text(encoding="utf-8")
    assert "\\operatorname" not in guide, "GitHub does not render operatorname in this guide"
    for required in (
        "## 簡易説明",
        "## 詳細説明",
        "```mermaid",
        "BuildStarMesh()",
        "Networking.GetNetworkDateTime()",
        "Julian Date",
        "Greenwich恒星時と地方恒星時",
        "Quaternion.LookRotation(R6h, NCP)",
        "Blend One One",
        "12,495",
        "星空の「空気感」を作る4層",
        "StargazingAtmosphere.cginc",
        "airmass",
        "6.8等級",
        "中高生",
        "REAL_SKY_SYSTEM.md",
    ):
        assert required in guide, f"starfield guide is missing: {required}"


def normalize_degrees(value: float) -> float:
    return value % 360.0


def sin_degrees(value: float) -> float:
    return math.sin(math.radians(value))


def cos_degrees(value: float) -> float:
    return math.cos(math.radians(value))


def julian_date(year: int, month: int, day: float) -> float:
    if month <= 2:
        year -= 1
        month += 12
    century = year // 100
    correction = 2 - century + century // 4
    return (math.floor(365.25 * (year + 4716)) +
            math.floor(30.6001 * (month + 1)) + day + correction - 1524.5)


def local_sidereal_degrees(year: int, month: int, day: int, hour: int,
                           minute: int, second: float, longitude_east: float) -> float:
    day_with_time = day + (hour + (minute + second / 60.0) / 60.0) / 24.0
    jd = julian_date(year, month, day_with_time)
    centuries = (jd - 2451545.0) / 36525.0
    gmst = (280.46061837 + 360.98564736629 * (jd - 2451545.0) +
            0.000387933 * centuries * centuries - centuries ** 3 / 38710000.0)
    return normalize_degrees(gmst + longitude_east)


def calculate_moon_horizontal(year: int, month: int, day: int, hour: int,
                              minute: int, second: float, latitude: float,
                              longitude_east: float) -> tuple[float, float]:
    """Independent port of the Udon lunar approximation for CI reference checks."""
    day_with_time = day + (hour + (minute + second / 60.0) / 60.0) / 24.0
    days = julian_date(year, month, day_with_time) - 2451543.5
    node = normalize_degrees(125.1228 - 0.0529538083 * days)
    inclination = 5.1454
    periapsis = normalize_degrees(318.0634 + 0.1643573223 * days)
    eccentricity = 0.0549
    mean_anomaly = normalize_degrees(115.3654 + 13.0649929509 * days)
    eccentric_anomaly = (mean_anomaly + eccentricity * 180.0 / math.pi *
                         sin_degrees(mean_anomaly) *
                         (1.0 + eccentricity * cos_degrees(mean_anomaly)))
    x_orbit = 60.2666 * (cos_degrees(eccentric_anomaly) - eccentricity)
    y_orbit = (60.2666 * math.sqrt(1.0 - eccentricity * eccentricity) *
               sin_degrees(eccentric_anomaly))
    true_anomaly = math.degrees(math.atan2(y_orbit, x_orbit))
    distance = math.hypot(x_orbit, y_orbit)
    argument = true_anomaly + periapsis
    ecliptic_x = distance * (cos_degrees(node) * cos_degrees(argument) -
                             sin_degrees(node) * sin_degrees(argument) * cos_degrees(inclination))
    ecliptic_y = distance * (sin_degrees(node) * cos_degrees(argument) +
                             cos_degrees(node) * sin_degrees(argument) * cos_degrees(inclination))
    ecliptic_z = distance * sin_degrees(argument) * sin_degrees(inclination)
    ecliptic_longitude = math.degrees(math.atan2(ecliptic_y, ecliptic_x))
    ecliptic_latitude = math.degrees(math.atan2(
        ecliptic_z, math.hypot(ecliptic_x, ecliptic_y)))

    sun_periapsis = normalize_degrees(282.9404 + 0.0000470935 * days)
    sun_mean_anomaly = normalize_degrees(356.0470 + 0.9856002585 * days)
    sun_mean_longitude = normalize_degrees(sun_periapsis + sun_mean_anomaly)
    moon_mean_longitude = normalize_degrees(node + periapsis + mean_anomaly)
    elongation = normalize_degrees(moon_mean_longitude - sun_mean_longitude)
    argument_latitude = normalize_degrees(moon_mean_longitude - node)
    ecliptic_longitude += (
        -1.274 * sin_degrees(mean_anomaly - 2.0 * elongation) +
        0.658 * sin_degrees(2.0 * elongation) -
        0.186 * sin_degrees(sun_mean_anomaly) -
        0.059 * sin_degrees(2.0 * mean_anomaly - 2.0 * elongation) -
        0.057 * sin_degrees(mean_anomaly - 2.0 * elongation + sun_mean_anomaly) +
        0.053 * sin_degrees(mean_anomaly + 2.0 * elongation) +
        0.046 * sin_degrees(2.0 * elongation - sun_mean_anomaly) +
        0.041 * sin_degrees(mean_anomaly - sun_mean_anomaly) -
        0.035 * sin_degrees(elongation) -
        0.031 * sin_degrees(mean_anomaly + sun_mean_anomaly) -
        0.015 * sin_degrees(2.0 * argument_latitude - 2.0 * elongation) +
        0.011 * sin_degrees(mean_anomaly - 4.0 * elongation))
    ecliptic_latitude += (
        -0.173 * sin_degrees(argument_latitude - 2.0 * elongation) -
        0.055 * sin_degrees(mean_anomaly - argument_latitude - 2.0 * elongation) -
        0.046 * sin_degrees(mean_anomaly + argument_latitude - 2.0 * elongation) +
        0.033 * sin_degrees(argument_latitude + 2.0 * elongation) +
        0.017 * sin_degrees(2.0 * mean_anomaly + argument_latitude))

    x = distance * cos_degrees(ecliptic_longitude) * cos_degrees(ecliptic_latitude)
    y = distance * sin_degrees(ecliptic_longitude) * cos_degrees(ecliptic_latitude)
    z = distance * sin_degrees(ecliptic_latitude)
    obliquity = 23.4393 - 0.0000003563 * days
    equatorial_x = x
    equatorial_y = y * cos_degrees(obliquity) - z * sin_degrees(obliquity)
    equatorial_z = y * sin_degrees(obliquity) + z * cos_degrees(obliquity)

    sidereal = local_sidereal_degrees(
        year, month, day, hour, minute, second, longitude_east)
    geocentric_latitude = math.atan(0.99664719 * math.tan(math.radians(latitude)))
    equatorial_x -= math.cos(geocentric_latitude) * cos_degrees(sidereal)
    equatorial_y -= math.cos(geocentric_latitude) * sin_degrees(sidereal)
    equatorial_z -= 0.99664719 * math.sin(geocentric_latitude)

    magnitude = math.sqrt(equatorial_x ** 2 + equatorial_y ** 2 + equatorial_z ** 2)
    equatorial_x /= magnitude
    equatorial_y /= magnitude
    equatorial_z /= magnitude
    latitude_radians = math.radians(latitude)
    sidereal_radians = math.radians(sidereal)
    east = -math.sin(sidereal_radians) * equatorial_x + math.cos(sidereal_radians) * equatorial_y
    north = (-math.sin(latitude_radians) * math.cos(sidereal_radians) * equatorial_x -
             math.sin(latitude_radians) * math.sin(sidereal_radians) * equatorial_y +
             math.cos(latitude_radians) * equatorial_z)
    up = (math.cos(latitude_radians) * math.cos(sidereal_radians) * equatorial_x +
          math.cos(latitude_radians) * math.sin(sidereal_radians) * equatorial_y +
          math.sin(latitude_radians) * equatorial_z)
    return math.degrees(math.asin(up)), normalize_degrees(math.degrees(math.atan2(east, north)))


def main() -> None:
    validate_repository_hygiene()
    validate_catalog()
    validate_yama_dependency_and_rolloff()
    validate_environment_and_drawing()
    validate_picnic_spot()
    validate_redistributable_package_and_debug_pickup()
    validate_sky_reference()
    validate_public_documentation()
    print(
        f"OK: {EXPECTED_STAR_COUNT} HYG stars, parameterized observatory, five USNO Moon "
        "references, 11 IMO showers, YamaPlayer, CC0 environment, locomotion, QvPen, "
        "UnyStylus references, Tiny Treats picnic spot, YamaPlayer patch workflow, "
        "redistributable package boundary, localized READMEs, starfield guide, global observatory "
        "selector, debug pickup, local settings, and a CC0 local-north handheld compass validated"
    )


if __name__ == "__main__":
    main()
