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
MOON_SHADER = ROOT / "Assets/StargazingHill/Shaders/Moon.shader"
OBSERVATORY_PROFILE = ROOT / "Assets/StargazingHill/Settings/TokyoObservatory.asset"
SHOWER_CATALOG = ROOT / "Assets/StargazingHill/Settings/IMO2026MajorShowers.asset"
PLAYER_SETTINGS = ROOT / "Assets/StargazingHill/Scripts/WorldPlayerSettings.cs"
VPM_MANIFEST = ROOT / "Packages/vpm-manifest.json"
TREE_SELECTION_TEMP = ROOT / "Assets/TreeSelectionTemp"
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
EXPECTED_SHOWERS = {
    "ids": ["QUADRANTIDS", "LYRIDS", "ETA_AQUARIIDS", "SOUTH_DELTA_AQUARIIDS", "PERSEIDS", "DRACONIDS", "ORIONIDS", "SOUTH_TAURIDS", "NORTH_TAURIDS", "LEONIDS", "GEMINIDS"],
    "activeStartMonthDay": [1228, 414, 419, 712, 717, 1006, 1002, 920, 1020, 1106, 1204],
    "activeEndMonthDay": [112, 430, 528, 823, 824, 1010, 1107, 1120, 1210, 1130, 1220],
    "peakMonthDay": [103, 422, 506, 731, 813, 1009, 1021, 1105, 1112, 1117, 1214],
    "radiantRightAscensionDegrees": [230, 271, 338, 340, 48, 262, 95, 52, 58, 152, 112],
    "radiantDeclinationDegrees": [49, 34, -1, -16, 58, 54, 16, 15, 22, 22, 33],
    "zenithalHourlyRates": [80, 18, 50, 25, 100, 5, 20, 7, 5, 15, 150],
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
    assert "forcedPreview && slot == 0" in meteor
    assert "const float waveLength = 5f;" in meteor
    assert "eventDurationSeconds = 25f" in meteor
    assert "Networking.GetNetworkDateTime()" in meteor

    meteor_shader = METEOR_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Meteor"' in meteor_shader
    assert "Blend One One" in meteor_shader

    moon_shader = MOON_SHADER.read_text(encoding="utf-8")
    assert 'Shader "StargazingHill/Moon"' in moon_shader

    catalog = SHOWER_CATALOG.read_text(encoding="utf-8")
    assert "sourceUrl: https://imo.net/files/meteor-shower/cal2026.pdf" in catalog
    assert "verifiedDate: 2026-08-12" in catalog
    for field, expected in EXPECTED_SHOWERS.items():
        match = re.search(rf"^  {field}: \[(.*?)\]$", catalog, re.MULTILINE)
        assert match, f"missing meteor shower field: {field}"
        values = [value.strip() for value in match.group(1).split(",")]
        actual = values if field == "ids" else [float(value) for value in values]
        assert actual == expected, f"unexpected IMO 2026 {field}: {actual}"

    builder = BUILDER.read_text(encoding="utf-8")
    assert 'MenuItem("Stargazing Hill/Debug/Trigger Hourly Meteor Shower"' in builder
    assert 'MenuItem("Stargazing Hill/Debug/Advance Sky +1 Hour"' in builder
    assert "TestSkyAndMeteorForBatchMode" in builder

    debug_window = (ROOT / "Assets/StargazingHill/Editor/MeteorShowerDebugWindow.cs").read_text(encoding="utf-8")
    assert 'MenuItem("Stargazing Hill/Debug/Meteor Shower Preview..."' in debug_window
    assert 'MenuItem("Stargazing Hill/Debug/Force Perseids Preview (20 Meteors)"' in debug_window
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
    validate_sky_reference()
    print(
        f"OK: {EXPECTED_STAR_COUNT} HYG stars, parameterized observatory, five USNO Moon "
        "references, 11 IMO showers, YamaPlayer, CC0 environment, locomotion, QvPen, "
        "and UnyStylus references validated"
    )


if __name__ == "__main__":
    main()
