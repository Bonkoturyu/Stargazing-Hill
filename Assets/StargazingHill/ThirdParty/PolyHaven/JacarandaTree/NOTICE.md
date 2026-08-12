# Jacaranda Tree

- Asset: `Jacaranda Tree`
- Authors: Rico Cilliers (`All`), Rob Tuytel (`Guidance`)
- Publisher: Poly Haven
- Asset page: https://polyhaven.com/a/jacaranda_tree
- Official files API: https://api.polyhaven.com/files/jacaranda_tree
- License: CC0 1.0 Universal
- License page: https://polyhaven.com/license
- Retrieved: 2026-08-11

Poly Haven marks this model and its textures as CC0. Attribution is not required, but the
source, authors, selected files, and transformations are retained here for provenance.

## Source and bake record

- Source FBX: `jacaranda_tree_1k.fbx`
- Source FBX MD5: `98f9827599dd42b18c1e9dfab3062d2f` (matches the official API)
- Source FBX SHA-256: `392c74d85100efc8ee045fed820ec4fbe79d62e154e4c54156ef1e0ec0465a5a`
- Source FBX size: `132437628` bytes; retained only as an ignored local bake input
- Imported source: 3,863,832 triangles (`branches 1,231,286`, `trunk 230,112`,
  `leaves 2,402,434`), three material submeshes
- Final bake: complete connected components were selected deterministically so no leaf,
  twig, or trunk component is cut mid-surface: branches 7%, trunk 100%, leaves 6%
- Intermediate mesh: `Jacaranda_LOD0.asset` (ignored local bake input), 288,899 vertices and 465,580 triangles
  (`branches 88,864`, `trunk 230,112`, `leaves 146,604`)
- The imported FBX root axis/unit transform was baked into the mesh. The scene Model root is
  therefore identity/Y-up, preventing the previous sideways-tree failure.
- Intermediate mesh SHA-256: `8e361f258c85727d3df4676ee6ca8411ec51a286fc20394b9247171237c4f227`

## Quest bake record

`Jacaranda_LOD0.asset` is the input to a second bake, not the shipped mesh, and is kept only as an
ignored local input under `SourceDownloads/JacarandaTree/` alongside the original FBX. See ADR-0009.

- Shipped mesh: `Jacaranda_Quest.asset`, 23,802 vertices and 19,507 triangles
  (`branches 3,345`, `trunk 6,872`, `leaves 9,290`), three material submeshes in the same order
- Baked by `StargazingHill.Editor.JacarandaQuestLodBaker`, deterministically from the intermediate
- Branches and trunk: grid vertex clustering, cell size bisected against a triangle target, with each
  cluster placed at the mean of its vertices rather than at the cell centre. The trunk keeps the larger
  share because it is the surface players stand next to, and hard clustering folds bark into flat shards
- Leaves: the leaf geometry is discarded. The canopy is rebuilt as 4,645 alpha-tested cards whose UVs
  address the three complete compound fronds inside `jacaranda_tree_leaves_diff_1k.jpg`, each card
  mapped so its petiole edge is the edge that meets the wood
- Each card takes its position from the branches and its direction from the leaves. The target is a
  voxel-binned centroid of the discarded leaf triangles, the stem sits on the nearest shipped branch
  surface point, and the frond runs from stem to target. Mean stem-to-target distance is 0.54 and the
  maximum is 2.79 model units, so foliage stays on wood while keeping the scan's crown shape.
- Card length is 1.25 model units, roughly 0.50m at the scene's 0.40 scale, against a real compound
  frond of 30-45cm. Cards above life size turn the crown into stacked slabs with straight edges.
- Shipped mesh SHA-256: `0cebaa16c70c9c08a6d7ce83133a2a1ce0edb8d44cb6c6e8628d3c79f97318b2`
- Shipped mesh size: `2522857` bytes

## Selected 1K textures

| File | SHA-256 |
|---|---|
| `jacaranda_tree_branches_diff_1k.jpg` | `5a4fe735f0c346cec83b6b444a0169fc14d2b31bbb7b26e26b1b6c726b6e06f6` |
| `jacaranda_tree_branches_nor_gl_1k.jpg` | `2d67393ba76a0f49cf965fa0c99d8c16268e71e87b1203d35539bfdb971a514e` |
| `jacaranda_tree_trunk_diff_1k.jpg` | `e5582fba664a9255252d1ee8088f75ad557f07360aa7f4513f2b2f1577e90be9` |
| `jacaranda_tree_trunk_nor_gl_1k.jpg` | `b2ad4e5daf8ec3fb87ba6c5f38e6a6924c9a6adebec20389f503e42da5d04587` |
| `jacaranda_tree_leaves_diff_1k.jpg` | `6fe80c1f514ef690cccefb90b7e559fbd1aa9c8935fdc02523cc8ab83c22f7c2` |
| `jacaranda_tree_leaves_nor_gl_1k.jpg` | `61520be2529ffe8e3d93d4361892134833bb4657117e121749089769067914c1` |
| `jacaranda_tree_leaves_alpha_1k.jpg` | `e040c86c4bd9ce703244e33a6b9cf25d2ac65c618be88178b8be3297f5d41960` |
