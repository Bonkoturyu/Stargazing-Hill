# UnyStylus v1.3 ローカルパッチ

状態: `Confirmed`

購入アセット本体はGitへ格納せず、Android / iOSのGLES3で出る
`redefinition of 'unityFogFactor'` を再適用可能なスクリプトで修正する。

Unity 2022.3の `UNITY_TRANSFER_FOG` はGLES3でローカル変数を宣言するため、geometry shader内で
同じマクロを複数回呼ぶと再定義になる。各呼出しをブロックスコープで囲み、描画式は変えない。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Apply-UnyStylusPatches.ps1
powershell -ExecutionPolicy Bypass -File Tools/Apply-UnyStylusPatches.ps1 -Mode Check
powershell -ExecutionPolicy Bypass -File Tools/Apply-UnyStylusPatches.ps1 -Mode Restore
```

スクリプトは対象2 shaderの未修正版（購入packageのUTF-8 BOM付きと、旧スクリプトが作り得たBOMなしの両方）／修正版SHA-256を照合し、v1.3と異なる内容へは書き込まない。`Restore`では元のBOMまで復元し、購入package内のSHA-256へ完全に戻す。
