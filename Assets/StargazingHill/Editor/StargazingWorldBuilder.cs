using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using Yamadev.YamaStream;
using Object = UnityEngine.Object;

namespace StargazingHill.Editor
{
    public static class StargazingWorldBuilder
    {
        private const string Root = "Assets/StargazingHill";
        private const string GeneratedRoot = Root + "/Generated";
        private const string MeshRoot = GeneratedRoot + "/Meshes";
        private const string MaterialRoot = GeneratedRoot + "/Materials";
        private const string SceneRoot = Root + "/Scenes";
        private const string ScenePath = SceneRoot + "/StargazingHill.unity";
        private const string CatalogPath = Root + "/Editor/Data/hyg_bright_v41.csv";
        private const string SkyControllerScriptPath = Root + "/Scripts/RealSkyController.cs";
        private const string SkyControllerProgramPath = Root + "/Scripts/RealSkyController.asset";
        private const string YamaPrefabPath = "Packages/net.kwxxw.yama-stream/YamaPlayer.prefab";
        private const string YamaModulePath =
            "Packages/net.kwxxw.yama-stream/Modules/VideoInfoDownloader/VideoInfoDownloader.prefab";

        private const float StarRadius = 220f;
        private const float StarMagnitudeLimit = 6.8f;
        private const float HillHeight = 2.3f;
        private static readonly Vector3 HillPosition = new Vector3(9f, 0f, 8f);

        private static readonly Vector3[] BranchEnds =
        {
            new Vector3( 2.4f, 6.2f,  0.7f),
            new Vector3(-2.0f, 6.6f,  0.9f),
            new Vector3( 1.7f, 7.2f, -1.6f),
            new Vector3(-1.5f, 7.5f, -1.5f),
            new Vector3( 0.8f, 8.2f,  0.5f),
            new Vector3(-0.4f, 8.5f,  0.2f),
        };

        [MenuItem("Stargazing Hill/Build Complete World", false, 10)]
        public static void BuildCompleteWorld()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureSkyControllerProgramAsset();

            Material groundMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/GrassGround.mat", "StargazingHill/Environment",
                new Color(0.20f, 0.38f, 0.17f), 0.58f);
            Material hillMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/HillGrass.mat", "StargazingHill/Environment",
                new Color(0.18f, 0.34f, 0.14f), 0.58f);
            Material bladeMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/GrassBlades.mat", "StargazingHill/Environment",
                new Color(0.23f, 0.44f, 0.17f), 0.66f);
            Material barkMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/TreeBark.mat", "StargazingHill/Environment",
                new Color(0.28f, 0.16f, 0.09f), 0.48f);
            Material leafMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/TreeLeaves.mat", "StargazingHill/Environment",
                new Color(0.16f, 0.30f, 0.12f), 0.55f);
            Material starMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/Starfield.mat", "StargazingHill/Starfield", Color.white, 0f);
            starMaterial.SetFloat("_Intensity", 1.35f);
            starMaterial.SetFloat("_HorizonStart", 0f);
            starMaterial.SetFloat("_HorizonFull", Mathf.Sin(15f * Mathf.Deg2Rad));
            EditorUtility.SetDirty(starMaterial);

            Mesh groundMesh = SaveMesh(MeshRoot + "/GrassGround.asset", BuildGroundMesh());
            Mesh hillMesh = SaveMesh(MeshRoot + "/Hill.asset", BuildHillMesh());
            Mesh grassMesh = SaveMesh(MeshRoot + "/GrassClusters.asset", BuildGrassMesh());
            Mesh trunkMesh = SaveMesh(MeshRoot + "/LandmarkTreeTrunk.asset", BuildTrunkMesh());
            Mesh leafMesh = SaveMesh(MeshRoot + "/LandmarkTreeLeaves.asset", BuildLeafMesh());
            Mesh starMesh = SaveMesh(MeshRoot + "/Starfield_Celestial.asset", BuildStarMesh());

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureRenderSettings();

            GameObject world = new GameObject("World");
            GameObject environment = CreateChild(world.transform, "Environment");
            CreateEnvironment(environment.transform, groundMesh, hillMesh, grassMesh, trunkMesh, leafMesh,
                groundMaterial, hillMaterial, bladeMaterial, barkMaterial, leafMaterial);
            CreateLighting(environment.transform);
            CreateWorldSettings(world.transform);
            CreateRealSky(world.transform, starMesh, starMaterial);
            CreateYamaPlayer(world.transform);

            ValidateScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Stargazing Hill] Build complete: 1 star mesh, low-poly grass field/hill/tree, " +
                      "Tokyo real-time sky rotation, and YamaPlayer with the reference distance rolloff.");
        }

        public static void BuildForBatchMode()
        {
            BuildCompleteWorld();
        }

        public static void ValidateForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("[Stargazing Hill] Saved scene validation passed.");
        }

        public static void RenderPreviewForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            GameObject cameraObject = GameObject.Find("World/WorldSettings/ReferenceCamera");
            Camera camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            image.Apply();
            string previewPath = Path.Combine(Path.GetTempPath(), "stargazing-hill-preview.png");
            File.WriteAllBytes(previewPath, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Debug.Log("[Stargazing Hill] Preview rendered: " + previewPath);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "StargazingHill");
            EnsureFolder(Root, "Generated");
            EnsureFolder(GeneratedRoot, "Meshes");
            EnsureFolder(GeneratedRoot, "Materials");
            EnsureFolder(Root, "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void EnsureSkyControllerProgramAsset()
        {
            UdonSharpProgramAsset programAsset =
                UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(RealSkyController));
            if (programAsset == null)
                programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(SkyControllerProgramPath);
            if (programAsset == null)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(SkyControllerScriptPath);
                if (script == null)
                    throw new InvalidOperationException("RealSkyController MonoScript is missing.");

                programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                programAsset.sourceCsScript = script;
                AssetDatabase.CreateAsset(programAsset, SkyControllerProgramPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Stargazing Hill] Created U# program asset for RealSkyController.");
            }

            if (programAsset.ScriptVersion < UdonSharpProgramVersion.CurrentVersion)
            {
                // This is a newly hand-authored script, so no legacy source rewrite is required.
                programAsset.ScriptVersion = UdonSharpProgramVersion.CurrentVersion;
                EditorUtility.SetDirty(programAsset);
            }

            if (programAsset.CompiledVersion < UdonSharpProgramVersion.CurrentVersion)
            {
                UdonSharpCompilerV1.CompileSync();
                AssetDatabase.SaveAssets();
            }

            programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset(typeof(RealSkyController));
            if (programAsset == null ||
                programAsset.ScriptVersion < UdonSharpProgramVersion.CurrentVersion ||
                programAsset.CompiledVersion < UdonSharpProgramVersion.CurrentVersion)
                throw new InvalidOperationException("RealSkyController U# program asset did not compile.");
        }

        private static Material CreateOrUpdateMaterial(
            string path, string shaderName, Color color, float ambient)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Shader not found: " + shaderName);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Ambient")) material.SetFloat("_Ambient", ambient);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(string path, Mesh generated)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                generated.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            EditorUtility.CopySerialized(generated, mesh);
            mesh.name = Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh BuildGroundMesh()
        {
            const int cells = 40;
            const float size = 80f;
            int side = cells + 1;
            var vertices = new Vector3[side * side];
            var colors = new Color[vertices.Length];
            var triangles = new int[cells * cells * 6];

            for (int z = 0; z < side; z++)
            {
                for (int x = 0; x < side; x++)
                {
                    float px = (x / (float)cells - 0.5f) * size;
                    float pz = (z / (float)cells - 0.5f) * size;
                    float y = Mathf.Sin(px * 0.21f) * Mathf.Cos(pz * 0.17f) * 0.055f;
                    int index = z * side + x;
                    vertices[index] = new Vector3(px, y, pz);
                    float variation = 0.86f + Mathf.PerlinNoise(px * 0.08f + 31f, pz * 0.08f + 17f) * 0.22f;
                    colors[index] = new Color(variation, variation, variation, 1f);
                }
            }

            int triangle = 0;
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int i = z * side + x;
                    triangles[triangle++] = i;
                    triangles[triangle++] = i + side;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + 1;
                    triangles[triangle++] = i + side;
                    triangles[triangle++] = i + side + 1;
                }
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildHillMesh()
        {
            const int rings = 12;
            const int segments = 40;
            const float radius = 10f;
            var vertices = new List<Vector3>(1 + rings * segments);
            var colors = new List<Color>(1 + rings * segments);
            var triangles = new List<int>(segments * (1 + (rings - 1) * 2) * 3);
            vertices.Add(new Vector3(0f, HillHeight, 0f));
            colors.Add(Color.white);

            for (int ring = 1; ring <= rings; ring++)
            {
                float t = ring / (float)rings;
                float ringRadius = radius * t;
                float height = HillHeight * Mathf.Pow(1f - t * t, 1.35f) + 0.02f;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * ringRadius, height, Mathf.Sin(angle) * ringRadius));
                    float shade = 0.90f + 0.10f * Mathf.Sin(angle * 3f + ring);
                    colors.Add(new Color(shade, shade, shade, 1f));
                }
            }

            for (int segment = 0; segment < segments; segment++)
            {
                triangles.Add(0);
                triangles.Add(1 + segment);
                triangles.Add(1 + (segment + 1) % segments);
            }

            for (int ring = 1; ring < rings; ring++)
            {
                int inner = 1 + (ring - 1) * segments;
                int outer = 1 + ring * segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    triangles.Add(inner + segment);
                    triangles.Add(outer + segment);
                    triangles.Add(inner + next);
                    triangles.Add(inner + next);
                    triangles.Add(outer + segment);
                    triangles.Add(outer + next);
                }
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildGrassMesh()
        {
            const int tuftCount = 520;
            var random = new System.Random(20260811);
            var vertices = new List<Vector3>(tuftCount * 9);
            var colors = new List<Color>(tuftCount * 9);
            var triangles = new List<int>(tuftCount * 9);

            for (int tuft = 0; tuft < tuftCount; tuft++)
            {
                float x = (float)(random.NextDouble() * 70.0 - 35.0);
                float z = (float)(random.NextDouble() * 70.0 - 35.0);
                if (new Vector2(x, z + 14f).sqrMagnitude < 20f ||
                    new Vector2(x - HillPosition.x, z - HillPosition.z).sqrMagnitude < 82f ||
                    new Vector2(x + 15f, z - 5f).sqrMagnitude < 36f)
                {
                    tuft--;
                    continue;
                }

                float baseY = Mathf.Sin(x * 0.21f) * Mathf.Cos(z * 0.17f) * 0.055f + 0.015f;
                float height = 0.04f + (float)random.NextDouble() * 0.06f;
                float width = 0.010f + (float)random.NextDouble() * 0.008f;
                float phase = (float)random.NextDouble() * Mathf.PI * 2f;
                float shade = 0.75f + (float)random.NextDouble() * 0.28f;
                Color color = new Color(shade, shade, shade, 1f);

                for (int blade = 0; blade < 3; blade++)
                {
                    float angle = phase + blade * Mathf.PI / 3f;
                    Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * width;
                    int first = vertices.Count;
                    vertices.Add(new Vector3(x, baseY, z) - side);
                    vertices.Add(new Vector3(x, baseY, z) + side);
                    vertices.Add(new Vector3(x, baseY + height, z));
                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color * 1.08f);
                    triangles.Add(first);
                    triangles.Add(first + 1);
                    triangles.Add(first + 2);
                }
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildTrunkMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            AddCylinder(vertices, triangles, colors, new Vector3(0f, 0f, 0f), new Vector3(0f, 6.6f, 0f),
                0.52f, 0.27f, 9);

            for (int i = 0; i < BranchEnds.Length; i++)
            {
                float startY = 3.7f + i * 0.42f;
                Vector3 start = new Vector3(0f, startY, 0f);
                AddCylinder(vertices, triangles, colors, start, BranchEnds[i], 0.23f, 0.08f, 7);
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCylinder(List<Vector3> vertices, List<int> triangles, List<Color> colors,
            Vector3 start, Vector3 end, float startRadius, float endRadius, int sides)
        {
            Vector3 axis = (end - start).normalized;
            Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;
            int first = vertices.Count;
            for (int ring = 0; ring < 2; ring++)
            {
                Vector3 center = ring == 0 ? start : end;
                float radius = ring == 0 ? startRadius : endRadius;
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    vertices.Add(center + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius);
                    float shade = 0.82f + 0.16f * (side / (float)sides);
                    colors.Add(new Color(shade, shade, shade, 1f));
                }
            }

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                triangles.Add(first + side);
                triangles.Add(first + sides + side);
                triangles.Add(first + next);
                triangles.Add(first + next);
                triangles.Add(first + sides + side);
                triangles.Add(first + sides + next);
            }
        }

        private static Mesh BuildLeafMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            for (int i = 0; i < BranchEnds.Length; i++)
            {
                Vector3 end = BranchEnds[i];
                AddLeafCluster(vertices, triangles, colors, end, new Vector3(1.45f, 0.95f, 1.20f), i);
                AddLeafCluster(vertices, triangles, colors,
                    Vector3.Lerp(new Vector3(0f, 4.3f + i * 0.25f, 0f), end, 0.70f),
                    new Vector3(1.05f, 0.72f, 0.92f), i + 7);
            }
            AddLeafCluster(vertices, triangles, colors, new Vector3(0f, 8.55f, 0f),
                new Vector3(1.30f, 1.05f, 1.15f), 20);

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddLeafCluster(List<Vector3> vertices, List<int> triangles, List<Color> colors,
            Vector3 center, Vector3 size, int seed)
        {
            int first = vertices.Count;
            vertices.Add(center + Vector3.up * size.y);
            vertices.Add(center - Vector3.up * size.y);
            vertices.Add(center + Vector3.right * size.x);
            vertices.Add(center - Vector3.right * size.x);
            vertices.Add(center + Vector3.forward * size.z);
            vertices.Add(center - Vector3.forward * size.z);
            float shade = 0.82f + (seed % 4) * 0.045f;
            for (int i = 0; i < 6; i++) colors.Add(new Color(shade, shade, shade, 1f));

            int[] local =
            {
                0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2,
                1, 4, 2, 1, 3, 4, 1, 5, 3, 1, 2, 5
            };
            for (int i = 0; i < local.Length; i++) triangles.Add(first + local[i]);
        }

        private static Mesh BuildStarMesh()
        {
            string absolutePath = Path.GetFullPath(CatalogPath);
            if (!File.Exists(absolutePath)) throw new FileNotFoundException("HYG bright-star catalog missing", absolutePath);

            string[] lines = File.ReadAllLines(absolutePath);
            var vertices = new List<Vector3>((lines.Length - 1) * 4);
            var colors = new List<Color>((lines.Length - 1) * 4);
            var uvs = new List<Vector2>((lines.Length - 1) * 4);
            var triangles = new List<int>((lines.Length - 1) * 6);

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string[] fields = lines[lineIndex].Split(',');
                if (fields.Length < 4) continue;
                if (!double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double ra) ||
                    !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double dec) ||
                    !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float magnitude) ||
                    magnitude > StarMagnitudeLimit)
                {
                    continue;
                }

                float colorIndex = 0.65f;
                if (!string.IsNullOrWhiteSpace(fields[3]))
                {
                    float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out colorIndex);
                }

                float cosDec = Mathf.Cos((float)dec);
                Vector3 direction = new Vector3(
                    cosDec * Mathf.Cos((float)ra),
                    Mathf.Sin((float)dec),
                    cosDec * Mathf.Sin((float)ra)).normalized;
                Vector3 tangent = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.96f ? Vector3.right : Vector3.up).normalized;
                Vector3 bitangent = Vector3.Cross(direction, tangent).normalized;
                float visibility = Mathf.Clamp01((StarMagnitudeLimit - magnitude) / 8.3f);
                float halfSize = Mathf.Lerp(0.055f, 0.24f, Mathf.Pow(visibility, 0.55f));
                float brightness = Mathf.Lerp(0.22f, 1f, Mathf.Pow(visibility, 0.45f));
                Color starColor = StarColor(colorIndex);
                starColor.a = brightness;
                Vector3 center = direction * StarRadius;
                int first = vertices.Count;
                vertices.Add(center - tangent * halfSize - bitangent * halfSize);
                vertices.Add(center + tangent * halfSize - bitangent * halfSize);
                vertices.Add(center + tangent * halfSize + bitangent * halfSize);
                vertices.Add(center - tangent * halfSize + bitangent * halfSize);
                colors.Add(starColor);
                colors.Add(starColor);
                colors.Add(starColor);
                colors.Add(starColor);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (StarRadius + 2f) * 2f);
            Debug.Log("[Stargazing Hill] Baked " + (vertices.Count / 4) + " HYG v4.1 stars into one mesh.");
            return mesh;
        }

        private static Color StarColor(float colorIndex)
        {
            float t = Mathf.InverseLerp(-0.4f, 2.0f, Mathf.Clamp(colorIndex, -0.4f, 2.0f));
            Color blue = new Color(0.66f, 0.78f, 1.00f, 1f);
            Color white = new Color(1.00f, 0.96f, 0.90f, 1f);
            Color orange = new Color(1.00f, 0.58f, 0.34f, 1f);
            return t < 0.5f ? Color.Lerp(blue, white, t * 2f) : Color.Lerp(white, orange, (t - 0.5f) * 2f);
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.012f, 0.017f, 0.030f);
            RenderSettings.fogDensity = 0.0022f;
        }

        private static void CreateEnvironment(Transform parent, Mesh groundMesh, Mesh hillMesh, Mesh grassMesh,
            Mesh trunkMesh, Mesh leafMesh, Material groundMaterial, Material hillMaterial, Material bladeMaterial,
            Material barkMaterial, Material leafMaterial)
        {
            GameObject ground = CreateMeshObject(parent, "GrassGround", groundMesh, groundMaterial, Vector3.zero);
            var groundCollider = ground.AddComponent<MeshCollider>();
            groundCollider.sharedMesh = groundMesh;

            GameObject hill = CreateMeshObject(parent, "Hill", hillMesh, hillMaterial, HillPosition);
            var hillCollider = hill.AddComponent<MeshCollider>();
            hillCollider.sharedMesh = hillMesh;

            CreateMeshObject(parent, "GrassClusters", grassMesh, bladeMaterial, Vector3.zero);

            GameObject tree = CreateChild(parent, "LandmarkTree");
            tree.transform.position = HillPosition + Vector3.up * HillHeight;
            CreateMeshObject(tree.transform, "TrunkAndBranches", trunkMesh, barkMaterial, Vector3.zero);
            CreateMeshObject(tree.transform, "SparseLeaves", leafMesh, leafMaterial, Vector3.zero);
            var trunkCollider = tree.AddComponent<CapsuleCollider>();
            trunkCollider.center = new Vector3(0f, 2.9f, 0f);
            trunkCollider.height = 5.8f;
            trunkCollider.radius = 0.56f;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = CreateChild(parent, "PaleFixedLight");
            lightObject.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.53f, 0.64f, 0.88f);
            light.intensity = 0.52f;
            light.shadows = LightShadows.None;
        }

        private static void CreateWorldSettings(Transform parent)
        {
            GameObject settings = CreateChild(parent, "WorldSettings");
            GameObject spawn = CreateChild(settings.transform, "Spawn");
            spawn.transform.position = new Vector3(0f, 0.25f, -14f);
            spawn.transform.rotation = Quaternion.LookRotation((HillPosition - spawn.transform.position).normalized, Vector3.up);

            GameObject cameraObject = CreateChild(settings.transform, "ReferenceCamera");
            cameraObject.transform.position = new Vector3(0f, 1.6f, -14f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                (HillPosition + Vector3.up * 2.5f - cameraObject.transform.position).normalized,
                Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.009f, 0.020f);
            camera.farClipPlane = 500f;
            camera.nearClipPlane = 0.03f;
            camera.enabled = false;

            GameObject descriptorObject = CreateChild(settings.transform, "VRCWorld");
            VRCSceneDescriptor descriptor = descriptorObject.AddComponent<VRCSceneDescriptor>();
            descriptor.spawns = new[] { spawn.transform };
            descriptor.ReferenceCamera = cameraObject;
            descriptor.RespawnHeightY = -15f;
        }

        private static void CreateRealSky(Transform world, Mesh starMesh, Material starMaterial)
        {
            GameObject system = CreateChild(world, "RealSkySystem");
            GameObject celestial = CreateMeshObject(system.transform, "Starfield_Celestial", starMesh, starMaterial, Vector3.zero);
            MeshRenderer renderer = celestial.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            GameObject controllerObject = CreateChild(system.transform, "RealSkyController");
            RealSkyController controller = UdonSharpUndo.AddComponent<RealSkyController>(controllerObject);
            controller.celestialSphere = celestial.transform;
            controller.latitudeDegrees = 35.68f;
            controller.longitudeDegreesEast = 139.76f;
            controller.updateIntervalSeconds = 15f;
            UdonSharpEditorUtility.CopyProxyToUdon(controller);
            EditorUtility.SetDirty(controller);
        }

        private static void CreateYamaPlayer(Transform world)
        {
            GameObject videoSystem = CreateChild(world, "VideoSystem");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(YamaPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "YamaPlayer 2.0.0-beta.7 is not resolved. Add https://vpm.kwxxw.net/index.json in VCC.");
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "YamaPlayer";
            player.transform.SetParent(videoSystem.transform, false);
            player.transform.localPosition = new Vector3(-15f, 2.7f, 5f);
            player.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            player.transform.localScale = Vector3.one * 2.20f;

            EnsureVideoInfoDownloader(player);
            ApplyYamaDistanceRolloff(player);
        }

        private static void EnsureVideoInfoDownloader(GameObject player)
        {
            ModuleManager[] managers = player.GetComponentsInChildren<ModuleManager>(true);
            if (managers.Length != 1)
            {
                throw new InvalidOperationException("Expected one YamaPlayer ModuleManager, found " + managers.Length + ".");
            }

            YamaPlayerModuleDefinition[] definitions =
                managers[0].GetComponentsInChildren<YamaPlayerModuleDefinition>(true);
            int downloaderCount = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].gameObject.name == "VideoInfoDownloader") downloaderCount++;
            }
            if (downloaderCount > 1) throw new InvalidOperationException("Multiple VideoInfoDownloader modules found.");
            if (downloaderCount == 1) return;

            GameObject modulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(YamaModulePath);
            if (modulePrefab == null) throw new InvalidOperationException("VideoInfoDownloader prefab is missing.");
            GameObject module = (GameObject)PrefabUtility.InstantiatePrefab(modulePrefab, managers[0].transform);
            module.name = "VideoInfoDownloader";
        }

        private static void ApplyYamaDistanceRolloff(GameObject player)
        {
            AudioSource[] sources = player.GetComponentsInChildren<AudioSource>(true);
            if (sources.Length == 0) throw new InvalidOperationException("YamaPlayer has no AudioSource.");
            AnimationCurve curve = CreateYamaRolloffCurve();
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                source.spatialBlend = 1f;
                source.spatialize = true;
                source.minDistance = 0f;
                source.maxDistance = 45f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

                VRCSpatialAudioSource spatial = source.GetComponent<VRCSpatialAudioSource>();
                if (spatial == null) spatial = source.gameObject.AddComponent<VRCSpatialAudioSource>();
                SetSerializedFloat(spatial, "Near", 0f);
                SetSerializedFloat(spatial, "Far", 45f);
                SetSerializedBool(spatial, "EnableSpatialization", true);
                SetSerializedBool(spatial, "UseAudioSourceVolumeCurve", true);
                EditorUtility.SetDirty(source);
            }
        }

        private static AnimationCurve CreateYamaRolloffCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f, -0.028571f, -0.028571f),
                new Keyframe(7f, 0.8f, -0.028571f, -0.028571f),
                new Keyframe(14f, 0.6f, -0.023301f, -0.023301f),
                new Keyframe(19f, 0.5f, -0.034426f, -0.034426f),
                new Keyframe(21f, 0.35f, -0.029562f, -0.029562f),
                new Keyframe(28f, 0.24f, -0.020943f, -0.020943f),
                new Keyframe(29.5f, 0.2f, -0.016292f, -0.016292f),
                new Keyframe(45f, 0.05f, 0f, 0f));
        }

        private static void SetSerializedFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.name + " lacks " + propertyName + ".");
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedBool(Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.name + " lacks " + propertyName + ".");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float GetSerializedFloat(Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.name + " lacks " + propertyName + ".");
            return property.floatValue;
        }

        private static bool GetSerializedBool(Object target, string propertyName)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException(target.name + " lacks " + propertyName + ".");
            return property.boolValue;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateMeshObject(
            Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition)
        {
            GameObject go = CreateChild(parent, name);
            go.transform.localPosition = localPosition;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        private static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("Generated scene is invalid.");
            RealSkyController[] skyControllers = Object.FindObjectsOfType<RealSkyController>(true);
            VRCSceneDescriptor[] descriptors = Object.FindObjectsOfType<VRCSceneDescriptor>(true);
            ModuleManager[] yamaManagers = Object.FindObjectsOfType<ModuleManager>(true);
            if (skyControllers.Length != 1 || skyControllers[0].celestialSphere == null)
                throw new InvalidOperationException("RealSkyController validation failed.");
            if (descriptors.Length != 1 || descriptors[0].spawns == null || descriptors[0].spawns.Length != 1)
                throw new InvalidOperationException("VRCSceneDescriptor validation failed.");
            if (yamaManagers.Length != 1)
                throw new InvalidOperationException("YamaPlayer validation failed.");
            if (GameObject.Find("World/Environment/LandmarkTree") == null)
                throw new InvalidOperationException("Landmark tree validation failed.");

            GameObject starfield = GameObject.Find("World/RealSkySystem/Starfield_Celestial");
            MeshFilter starFilter = starfield == null ? null : starfield.GetComponent<MeshFilter>();
            if (starFilter == null || starFilter.sharedMesh == null || starFilter.sharedMesh.vertexCount / 4 != 12495)
                throw new InvalidOperationException("HYG star mesh validation failed.");

            GameObject yamaPlayer = GameObject.Find("World/VideoSystem/YamaPlayer");
            AudioSource[] sources = yamaPlayer == null
                ? Array.Empty<AudioSource>()
                : yamaPlayer.GetComponentsInChildren<AudioSource>(true);
            YamaPlayerModuleDefinition[] definitions = yamaPlayer == null
                ? Array.Empty<YamaPlayerModuleDefinition>()
                : yamaPlayer.GetComponentsInChildren<YamaPlayerModuleDefinition>(true);
            int downloaderCount = 0;
            for (int definitionIndex = 0; definitionIndex < definitions.Length; definitionIndex++)
            {
                if (definitions[definitionIndex].gameObject.name == "VideoInfoDownloader") downloaderCount++;
            }
            if (yamaPlayer == null || yamaPlayer.transform.localPosition != new Vector3(-15f, 2.7f, 5f) ||
                yamaPlayer.transform.localScale != Vector3.one * 2.20f || downloaderCount != 1)
            {
                throw new InvalidOperationException("YamaPlayer placement/module validation failed.");
            }

            Keyframe[] expectedKeys = CreateYamaRolloffCurve().keys;
            if (sources.Length == 0) throw new InvalidOperationException("YamaPlayer audio validation failed.");
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                AudioSource source = sources[sourceIndex];
                VRCSpatialAudioSource spatial = source.GetComponent<VRCSpatialAudioSource>();
                Keyframe[] actualKeys = source.GetCustomCurve(AudioSourceCurveType.CustomRolloff).keys;
                if (!source.spatialize || !Mathf.Approximately(source.spatialBlend, 1f) ||
                    !Mathf.Approximately(source.minDistance, 0f) || !Mathf.Approximately(source.maxDistance, 45f) ||
                    source.rolloffMode != AudioRolloffMode.Custom || actualKeys.Length != expectedKeys.Length ||
                    spatial == null || !Mathf.Approximately(GetSerializedFloat(spatial, "Near"), 0f) ||
                    !Mathf.Approximately(GetSerializedFloat(spatial, "Far"), 45f) ||
                    !GetSerializedBool(spatial, "EnableSpatialization") ||
                    !GetSerializedBool(spatial, "UseAudioSourceVolumeCurve"))
                {
                    throw new InvalidOperationException("YamaPlayer AudioSource rolloff validation failed.");
                }

                for (int keyIndex = 0; keyIndex < expectedKeys.Length; keyIndex++)
                {
                    float normalizedTime = expectedKeys[keyIndex].time / 45f;
                    bool timeMatches = Mathf.Approximately(actualKeys[keyIndex].time, expectedKeys[keyIndex].time) ||
                                       Mathf.Approximately(actualKeys[keyIndex].time, normalizedTime);
                    bool monotonic = keyIndex == 0 ||
                                     (actualKeys[keyIndex].time > actualKeys[keyIndex - 1].time &&
                                      actualKeys[keyIndex].value <= actualKeys[keyIndex - 1].value);
                    if (!timeMatches || !Mathf.Approximately(actualKeys[keyIndex].value, expectedKeys[keyIndex].value) ||
                        !monotonic)
                    {
                        throw new InvalidOperationException("YamaPlayer rolloff key validation failed.");
                    }
                }
            }
        }
    }
}
