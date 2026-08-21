# Compass PBR (Unity) CC0

- Status: Confirmed
- Creator / rights holder: Lucian Pavel
- Use in this project: handheld compass prop near the picnic spot
- Distribution page: https://opengameart.org/content/compass-pbrunity-cc0
- Direct archive: https://opengameart.org/sites/default/files/Compass.zip
- Published: 2017-03-04
- Retrieved and verified: 2026-08-15
- Upstream version: no numbered version; distribution page publication is the identification point
- License: Creative Commons Zero v1.0 Universal (CC0-1.0)
- License evidence: the OpenGameArt distribution page explicitly lists `CC0` and states that attribution is not required
- Canonical license: https://creativecommons.org/publicdomain/zero/1.0/
- Attribution: not required; this notice retains creator and source information for provenance
- Modification / redistribution: permitted by CC0-1.0
- Original archive SHA-256: `1998ae2e7d16f28e0b0a5a183599ad0906cd2b484ef521b21a869a90d3f4ed9a`

## Selected upstream files

| File | Original SHA-256 | Repository use |
|---|---|---|
| `Compass.fbx` | `2198a54c7b87dc97790413a5ad97a1c2184eb953e5329b7b8ba89e068367434a` | Mesh source |
| `Compass.png` | `69f3b93eee69ea21d3f53efaa505041d83a441f62f7bb2d539750f774e328afd` | Albedo source |
| `Compass NRM.png` | `49cdce093d8c4c8920b78f9ed74d7a0af5f3649acdc52c5baa32127beef7c9e0` | Normal-map source |

The upstream 2048 px albedo and normal map are resized to 512 px PNG files for the
repository. Unity import also caps both at 512 px, enables mipmaps and compression,
and disables animation, cameras, lights, embedded materials, and generated colliders.
The metallic and AO maps are intentionally omitted because the cross-platform
`StargazingHill/Environment` material uses albedo and normal only.

## Included derived files

| File | SHA-256 |
|---|---|
| `Compass_Albedo.png` | `b0b870da48d1e45b7ce12d5d26cce75ae2598d9fb20a0c82b971e740df1f97f3` |
| `Compass_Normal.png` | `2bd60ae53a5f72a4f26b3d169268a672e68846460096fb25dc6f80222339200a` |
