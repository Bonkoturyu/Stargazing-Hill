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
VPM_MANIFEST = ROOT / "Packages/vpm-manifest.json"
EXPECTED_CATALOG_SHA256 = "976abeb38d0d6f7b12fb069943140b59c31a299a72dc04350014e9ca1a4a0e4a"
EXPECTED_STAR_COUNT = 12_495
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
    digest = hashlib.sha256(CATALOG.read_bytes()).hexdigest()
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


def main() -> None:
    validate_catalog()
    validate_yama_dependency_and_rolloff()
    validate_sky_reference()
    print(f"OK: {EXPECTED_STAR_COUNT} HYG stars, Tokyo sky basis, and YamaPlayer rolloff validated")


if __name__ == "__main__":
    main()
