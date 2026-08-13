#!/usr/bin/env python3
"""Verify that a generated unitypackage contains only Stargazing Hill-owned asset paths."""

import argparse
import tarfile
from pathlib import Path


OWNED_ROOT = "Assets/StargazingHill"
REQUIRED_PATHS = {
    f"{OWNED_ROOT}/Scenes/StargazingHill.unity",
    f"{OWNED_ROOT}/README_UNITYPACKAGE.md",
}
FORBIDDEN_PREFIXES = (
    "Assets/Rasta/",
    f"{OWNED_ROOT}/SourceDownloads/",
    "Packages/net.kwxxw.yama-stream/",
    "Packages/net.ureishi.qvpen/",
)


def read_package_paths(package_path: Path) -> list[str]:
    paths: list[str] = []
    with tarfile.open(package_path, mode="r:gz") as archive:
        for member in archive.getmembers():
            if not member.isfile() or not member.name.endswith("/pathname"):
                continue
            stream = archive.extractfile(member)
            if stream is None:
                raise AssertionError(f"could not read {member.name}")
            paths.append(stream.read().decode("utf-8").strip().replace("\\", "/"))
    return paths


def validate(package_path: Path) -> None:
    if not package_path.is_file() or package_path.stat().st_size == 0:
        raise AssertionError(f"missing or empty unitypackage: {package_path}")

    paths = read_package_paths(package_path)
    if not paths:
        raise AssertionError("unitypackage contains no pathname records")

    escaped = [path for path in paths if path != OWNED_ROOT and not path.startswith(f"{OWNED_ROOT}/")]
    if escaped:
        raise AssertionError(f"unitypackage escaped owned root: {escaped[:10]}")

    forbidden = [
        path
        for path in paths
        if any(path == prefix.rstrip("/") or path.startswith(prefix) for prefix in FORBIDDEN_PREFIXES)
    ]
    if forbidden:
        raise AssertionError(f"unitypackage bundled forbidden dependencies: {forbidden[:10]}")

    missing = sorted(REQUIRED_PATHS.difference(paths))
    if missing:
        raise AssertionError(f"unitypackage lacks required assets: {missing}")

    print(
        f"OK: {len(paths)} package paths under {OWNED_ROOT}; "
        "external player/pen assets and local bake inputs omitted"
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("package", type=Path)
    args = parser.parse_args()
    validate(args.package.resolve())


if __name__ == "__main__":
    main()
