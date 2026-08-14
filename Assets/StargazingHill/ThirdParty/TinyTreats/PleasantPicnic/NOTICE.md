# Tiny Treats - Pleasant Picnic 1.0

- Status: Confirmed
- Creator / distributor: Isa Lousberg (`Tiny Treats`)
- Use in this project: blue picnic blanket, radio, teapot and mug, blue plaid cushion, light-blue cushion beneath the landmark tree
- Godot Asset Library: https://godotengine.org/asset-library/asset/3433
- Upstream repository: https://github.com/TinyTreats-Game-Assets/Tiny-Treats-Pleasant-Picnic-1.0
- Upstream commit: `da50c97a056fe1513413343787f2526ea7f25174`
- Version / release date: 1.0 / 2024-10-22 (Godot Asset Library)
- Retrieved and verified: 2026-08-13
- License: Creative Commons Zero v1.0 Universal (CC0-1.0)
- License evidence: the upstream `LICENSE.txt`, the repository README, and the Godot Asset Library entry all identify the set as CC0; the unmodified upstream license is stored beside this notice
- Attribution: not required; this notice retains creator and source information for provenance
- Redistribution / modification: permitted by CC0-1.0

## Included upstream files

| File | SHA-256 | Triangles measured from corresponding upstream OBJ |
|---|---|---:|
| `picnic_blanket_blue.fbx` | `552A74E55E224A24051AAF7A2E6C63973A71E771BB5FAE9289C71BE0FBE0E2DB` | 240 |
| `radio.fbx` | `9D6AF6D768427EECFABFFA3920CAA9690473979CBEE9DF6349B76789589EAF06` | 642 |
| `teapot.fbx` | `0AB6EDBE361F19B24D3EA6395434300F7FFD69AD1007B88A426365F29F17997A` | 698 |
| `mug.fbx` | `D3A3DF63297F2F2A5B7A87FFF5DD1803054616C69CE4FFF7AED82EC1E50B6D9E` | 300 |
| `pillow_small_blue.fbx` | `1EABD1867988FD0609A8CB110E360BBA5A4779285D9FEA1D57900A87E4E7A912` | 240 |
| `pillow_large_blue.fbx` | `86C834155243E7315AEBF734CAEAB56A69CE818B1DB8D325180EAABF443E670D` | 240 |
| `tiny_treats_texture_1.png` | `31E5C7C81BFBA644A59797B1907519592C7728D4AF98A705449975DD929482E5` | - |
| `tiny_treats_plaid_pattern_blue.png` | `CD967007AD742E3C964BE4854C971ED0D9AA98D2B6710AD05352833A68E8D6F9` | - |
| `LICENSE.txt` | `4FEB9B1C670542F0A4227447EC5941616AD9B73CC4881FAD823DEEEA0B51FEA8` | - |

The six selected upstream OBJ meshes total 2,360 triangles. The final scene reuses the two cushion meshes to create four seats and contains 3,028 triangles, including the terrain-conforming blanket mesh. This remains a small environment detail, so no geometry reduction is applied. Unity import disables animation, cameras, lights, generated colliders, and embedded materials, and applies medium mesh compression. Both textures are imported with mipmaps and a 512 px maximum size. Scene instances use the mobile-compatible `StargazingHill/Environment` shader, no colliders, no light/reflection probes, and no realtime shadows.
