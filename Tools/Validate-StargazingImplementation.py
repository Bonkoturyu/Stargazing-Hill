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
METEOR_SHADER = ROOT / "Assets/StargazingHill/Shaders/Meteor.shader"
PLAYER_SETTINGS = ROOT / "Assets/StargazingHill/Scripts/WorldPlayerSettings.cs"
VPM_MANIFEST = ROOT / "Packages/vpm-manifest.json"
GRASS_DIFFUSE = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_diff_1k.jpg"
GRASS_NORMAL = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_nor_gl_1k.jpg"
JACARANDA_ROOT = ROOT / "Assets/StargazingHill/ThirdParty/PolyHaven/JacarandaTree"
TREE_MESH = JACARANDA_ROOT / "Jacaranda_LOD0.asset"
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
    TREE_MESH: "8e361f258c85727d3df4676ee6ca8411ec51a286fc20394b9247171237c4f227",
    TREE_TEXTURES[0]: "5a4fe735f0c346cec83b6b444a0169fc14d2b31bbb7b26e26b1b6c726b6e06f6",
    TREE_TEXTURES[1]: "2d67393ba76a0f49cf965fa0c99d8c16268e71e87b1203d35539bfdb971a514e",
    TREE_TEXTURES[2]: "e5582fba664a9255252d1ee8088f75ad557f07360aa7f4513f2b2f1577e90be9",
    TREE_TEXTURES[3]: "b2ad4e5daf8ec3fb87ba6c5f38e6a6924c9a6adebec20389f503e42da5d04587",
    TREE_TEXTURES[4]: "6fe80c1f514ef690cccefb90b7e559fbd1aa9c8935fdc02523cc8ab83c22f7c2",
    TREE_TEXTURES[5]: "61520be2529ffe8e3d93d4361892134833bb4657117e121749089769067914c1",
    TREE_TEXTURES[6]: "e040c86c4bd9ce703244e33a6b9cf25d2ac65c618be88178b8be3297f5d41960",
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


def validate_environment_and_drawing() -> None:
    for path, expected_hash in EXPECTED_CC0_HASHES.items():
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert digest == expected_hash, f"third-party asset SHA-256 changed: {path}: {digest}"

    manifest = json.loads(VPM_MANIFEST.read_text(encoding="utf-8"))
    assert manifest["dependencies"]["net.ureishi.qvpen"]["version"] == "3.3.15"
    assert manifest["locked"]["net.ureishi.qvpen"]["dependencies"]["com.vrchat.worlds"] == "^3.5.0"

    builder = BUILDER.read_text(encoding="utf-8")
    assert "const int tuftCount = 9000;" in builder
    assert "private const float HillHeight = 2.3f;" in builder
    assert "private const float HillRadius = 10f;" in builder
    assert 'QvPenPrefabPath = "Packages/net.ureishi.qvpen/QvPen(grad).prefab"' in builder
    assert 'UnyStylusPrefabPath = "Assets/Rasta/UnyStylus/UnyStylus.prefab"' in builder
    assert 'TreeMeshPath =\n            Root + "/ThirdParty/PolyHaven/JacarandaTree/Jacaranda_LOD0.asset"' in builder
    assert 'CreateChild(parent, "LandmarkTree")' in builder
    assert 'CreateChild(tree.transform, "Model")' in builder
    assert "treeModel.TransformDirection(Vector3.up)" in builder
    assert "treeTriangles < 450000L || treeTriangles > 480000L" in builder
    assert "descriptorObject.AddComponent<PipelineManager>();" in builder
    assert "private static readonly Vector3 SpawnGroundPosition = new Vector3(-2.78f, 0f, -20.80f);" in builder
    assert "private static readonly Vector3 YamaPlayerPosition = new Vector3(-4f, 1.813f, -24f);" in builder
    assert "private static readonly Vector3 QvPenPosition = new Vector3(-7.6f, 0.848461f, -22.454f);" in builder
    assert "private static readonly Vector3 UnyStylusPosition = new Vector3(-8.668f, 0.858f, -20.672f);" in builder
    assert "ValidateAmenityPlacement" in builder
    assert 'material.SetTexture("_AlphaMap", alpha);' in builder

    player_settings = PLAYER_SETTINGS.read_text(encoding="utf-8")
    for expected_call in (
        "SetWalkSpeed(walkSpeed)",
        "SetRunSpeed(runSpeed)",
        "SetStrafeSpeed(strafeSpeed)",
        "SetJumpImpulse(jumpImpulse)",
        "SetGravityStrength(gravityStrength)",
    ):
        assert expected_call in player_settings


def validate_sky_reference() -> None:
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
    assert "celestialSphere.position = _localPlayer.GetPosition();" in controller
    assert "CalculateSkyRotation(" in controller
    assert "DebugAdvanceOneHour()" in controller
    assert "DebugResetTimeOffset()" in controller

    meteor = METEOR_CONTROLLER.read_text(encoding="utf-8")
    assert "GetHourlyEventId(" in meteor
    assert "DebugTriggerHourlyEvent()" in meteor
    assert "DebugPreviewEventAtSecond(" in meteor
    assert "const float waveLength = 5f;" in meteor
    assert "eventDurationSeconds = 25f" in meteor
    assert "Networking.GetNetworkDateTime()" in meteor

    meteor_shader = METEOR_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Meteor"' in meteor_shader
    assert "Blend One One" in meteor_shader

    builder = BUILDER.read_text(encoding="utf-8")
    assert 'MenuItem("Stargazing Hill/Debug/Trigger Hourly Meteor Shower"' in builder
    assert 'MenuItem("Stargazing Hill/Debug/Advance Sky +1 Hour"' in builder
    assert "TestSkyAndMeteorForBatchMode" in builder


def main() -> None:
    validate_catalog()
    validate_yama_dependency_and_rolloff()
    validate_environment_and_drawing()
    validate_sky_reference()
    print(
        f"OK: {EXPECTED_STAR_COUNT} HYG stars, Tokyo sky, YamaPlayer, CC0 environment, "
        "locomotion, QvPen, and UnyStylus references validated"
    )


if __name__ == "__main__":
    main()
