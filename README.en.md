# Stargazing Hill / 星見の丘

[日本語](README.md) | [English](README.en.md) | [繁體中文](README.zh-Hant.md) | [简体中文](README.zh-Hans.md) | [한국어](README.ko.md)

A quiet grassland world for VRChat, built around a real star field and Moon aligned with the current time and one of 20 globally shared observation locations.

## Highlights

- Real stars generated from HYG v4.1 and rotated from UTC, latitude, and east longitude selected globally from 20 locations
- Current apparent Moon position
- A meteor event beginning at the top of every hour, plus 11 major showers from the IMO 2026 calendar
- A large grassland, a small hill, one landmark tree, and a CC0 picnic spot
- YamaPlayer, QvPen, and UnyStylus areas
- Japanese, English, Traditional Chinese, Simplified Chinese, and Korean information, observatory, and debug panels
- Current instance population and a local join/leave history
- Designed for Windows, Android/standalone VR, and iOS

The star field uses 12,495 stars baked into one mesh, one renderer, and one additive unlit material. At runtime, the whole celestial sphere rotates instead of updating thousands of stars individually. See the illustrated [Starfield Implementation Guide](docs/STARFIELD_IMPLEMENTATION_GUIDE.md) and the authoritative [Real Sky System specification](docs/REAL_SKY_SYSTEM.md).

## Project status

The environment, real star field, atmospheric extinction, globally shared 20-location selector, Moon calculation, meteor system, YamaPlayer standard playlist workflow, drawing tools, five-language panels, population display, local history, debug controls, and mobile-oriented shaders are implemented. Final ClientSim multiplayer and physical Windows/Android/iOS validation remain open.

## Open a clean clone

Use Unity `2022.3.22f1`. Add the clone to VRChat Creator Companion, restore the locked VPM dependencies, apply the documented YamaPlayer patch, import your legitimately purchased UnyStylus v1.3 package, and apply its compatibility patch. Then open `Assets/StargazingHill/Scenes/StargazingHill.unity` and run:

`Stargazing Hill/Validate Saved Scene`

A full rebuild is not required for normal use. The complete procedure is in [Setup and Restore](docs/SETUP_AND_RESTORE.md).

## Unity menus

- `Stargazing Hill/Content`: select and validate the information panel, apply its saved layout, and maintain the versioned picnic layout
- `Stargazing Hill/Preview & Debug`: preview the sky and meteor systems locally
- `Stargazing Hill/Build & Export`: create the redistributable unitypackage
- `Stargazing Hill/Advanced`: replace generated content, rebuild the scene, or repair SDK guards; intended for maintainers who understand the consequences

`Advanced/Generated Content/Rebuild Complete World (Destructive)...` is only for a deliberate full regeneration. The picnic generator reads `Assets/StargazingHill/Editor/Data/PicnicLayout.json`. After adjusting the picnic in the Scene, use `Content/Picnic/Save Current Scene Layout to Generator...` and commit both the Scene and JSON.

Edit playlists with YamaPlayer's own **Edit Playlist** button in the Inspector or `YamaPlayer/Edit Playlist`. This project does not maintain a separate playlist file or automatic synchronization path. A full rebuild preserves the standard-editor-authored YamaPlayer from the saved scene.

## Redistribution

Create the redistributable package with:

`Stargazing Hill/Build & Export/Redistributable UnityPackage...`

This path exports project-owned assets and redistributable CC0/CC BY-SA content only. It deliberately excludes YamaPlayer, QvPen, and the purchased UnyStylus files. Do not use Unity's generic **Include dependencies** option for redistribution.

## Documentation

- [Documentation index](docs/README.md)
- [Project specification](docs/PROJECT_SPEC.md)
- [Starfield Implementation Guide](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)
- [Real Sky System specification](docs/REAL_SKY_SYSTEM.md)
- [Setup and Restore](docs/SETUP_AND_RESTORE.md)
- [Third-party dependencies](docs/legal/THIRD_PARTY_DEPENDENCIES.md)
- [Third-party assets and licenses](docs/legal/THIRD_PARTY_ASSETS.md)

## GitHub Releases

Pushing a `v*` tag runs GitHub Actions to create and validate the redistributable UnityPackage, then wraps it and its SHA-256 checksum in a ZIP attached to the matching Release. The workflow needs neither Unity Editor nor a Unity license. YamaPlayer, QvPen, and the purchased UnyStylus files are excluded. Actions runner availability is confirmed; the first tag-based Release run remains `Pending Evidence`. See the [public-release audit](docs/PUBLIC_RELEASE_AUDIT.md).
