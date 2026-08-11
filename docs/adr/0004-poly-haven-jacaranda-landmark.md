# ADR-0004: Poly Haven Jacaranda一本木と連結部品単位の軽量化

- 状態: Accepted
- 決定日: 2026-08-11

## Context

既存のQuaternius低ポリゴン木は形状が簡素で、以前はFBX軸変換を上書きしたため横倒しにもなった。利用者から、一本木をPoly Haven `Jacaranda Tree`へ置き換え、より現実感のある見た目にする要求が確定した。

公式ページは312,356 polygonsと表示するが、公式FBXをUnityへ展開すると枝1,231,286、幹230,112、葉2,402,434、合計3,863,832 trianglesとなる。132,437,628 bytesの原本FBXはGitHubの単一ファイル上限も超え、VRChatへそのまま投入できない。

## Options

- 原本FBXをそのまま使う。
- 汎用QEMでMesh全体を簡略化する。
- 枝・葉を連結部品単位で均等に選択し、幹を完全に保持した派生Meshを作る。
- 旧Quaternius木を維持する。

## Decision

- Poly Haven `Jacaranda Tree` のCC0 1Kテクスチャを採用する。
- 原本FBXは `.gitignore` 対象のローカル入力とし、リポジトリへ含めない。
- 枝は連結部品の7%、幹は100%、葉は6%を決定的hashで均等選択する。三角形を途中で切らず、完全な葉・枝・幹部品だけを残す。
- 派生Meshは288,899 vertices / 465,580 triangles、枝・幹・葉の3 submeshとする。
- FBX rootの軸・単位変換を派生Meshへベイクし、SceneのModel rootはidentity/Y-upとする。
- Sceneでは0.40倍とし、Boundsから丘中央へ接地する。高さ7.5〜8.5m、幅8.5〜10.5mを自動検証する。
- 1K diffuse / normalと葉alpha mapを専用Shaderへ渡し、枝・幹・葉の3 materialを使う。

## Consequences

旧木より自然な幹・枝・葉のシルエットとテクスチャ品質を得られ、横倒し原因だったimport root依存を除去できる。原本比で三角形を約88%削減し、派生MeshはGitHub上限内の約39MBになる。

一方、46.6万三角形はQuest/iOSで実測が必要な負荷である。最終的なGPU時間とメモリを実機測定し、必要なら遠距離LODまたはbillboardを追加する。原本・派生・テクスチャのhashと加工手順は隣接 `NOTICE.md` を正本とする。
