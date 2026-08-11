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
using VRC.Core;
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
        private const string PlayerSettingsScriptPath = Root + "/Scripts/WorldPlayerSettings.cs";
        private const string PlayerSettingsProgramPath = Root + "/Scripts/WorldPlayerSettings.asset";
        private const string MeteorControllerScriptPath = Root + "/Scripts/MeteorController.cs";
        private const string MeteorControllerProgramPath = Root + "/Scripts/MeteorController.asset";
        private const string GrassDiffusePath =
            Root + "/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_diff_1k.jpg";
        private const string GrassNormalPath =
            Root + "/ThirdParty/PolyHaven/LeafyGrass/leafy_grass_nor_gl_1k.jpg";
        private const string TreeMeshPath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/Jacaranda_LOD0.asset";
        private const string TreeBranchesDiffusePath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_branches_diff_1k.jpg";
        private const string TreeBranchesNormalPath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_branches_nor_gl_1k.jpg";
        private const string TreeTrunkDiffusePath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_trunk_diff_1k.jpg";
        private const string TreeTrunkNormalPath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_trunk_nor_gl_1k.jpg";
        private const string TreeLeavesDiffusePath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_leaves_diff_1k.jpg";
        private const string TreeLeavesNormalPath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_leaves_nor_gl_1k.jpg";
        private const string TreeLeavesAlphaPath =
            Root + "/ThirdParty/PolyHaven/JacarandaTree/jacaranda_tree_leaves_alpha_1k.jpg";
        private const string YamaPrefabPath = "Packages/net.kwxxw.yama-stream/YamaPlayer.prefab";
        private const string YamaModulePath =
            "Packages/net.kwxxw.yama-stream/Modules/VideoInfoDownloader/VideoInfoDownloader.prefab";
        private const string QvPenPrefabPath = "Packages/net.ureishi.qvpen/QvPen(grad).prefab";
        private const string UnyStylusPrefabPath = "Assets/Rasta/UnyStylus/UnyStylus.prefab";

        private const float StarRadius = 220f;
        private const float StarMagnitudeLimit = 6.8f;
        private const float HillHeight = 2.3f;
        private const float HillRadius = 10f;
        private const float HillTopRadius = 2.25f;
        private static readonly Vector3 HillPosition = new Vector3(9f, 0f, 8f);
        private static readonly Vector3 SpawnGroundPosition = new Vector3(-2.78f, 0f, -20.80f);
        private static readonly Vector3 SpawnEuler = new Vector3(0.96409f, 22.24902f, 0f);
        private static readonly Vector3 YamaPlayerPosition = new Vector3(-4f, 1.813f, -24f);
        private static readonly Vector3 YamaPlayerEuler = new Vector3(0f, 202.2865f, 0f);
        private static readonly Vector3 QvPenPosition = new Vector3(-7.6f, 0.848461f, -22.454f);
        private static readonly Vector3 QvPenEuler = new Vector3(0f, 238.2345f, 0f);
        private static readonly Vector3 UnyStylusPosition = new Vector3(-8.668f, 0.858f, -20.672f);
        private static readonly Vector3 UnyStylusEuler = new Vector3(0f, 239.9454f, 0f);
        private static readonly Vector3 AmenityCenter = new Vector3(-6.756f, 0f, -22.375f);

        [MenuItem("Stargazing Hill/Build Complete World", false, 10)]
        public static void BuildCompleteWorld()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureThirdPartyImportSettings();
            EnsureProgramAsset(typeof(RealSkyController), SkyControllerScriptPath, SkyControllerProgramPath);
            EnsureProgramAsset(typeof(WorldPlayerSettings), PlayerSettingsScriptPath, PlayerSettingsProgramPath);
            EnsureProgramAsset(typeof(MeteorController), MeteorControllerScriptPath, MeteorControllerProgramPath);

            Texture2D grassDiffuse = LoadRequiredAsset<Texture2D>(GrassDiffusePath);
            Texture2D grassNormal = LoadRequiredAsset<Texture2D>(GrassNormalPath);
            Mesh treeMesh = LoadRequiredAsset<Mesh>(TreeMeshPath);
            Texture2D treeBranchesDiffuse = LoadRequiredAsset<Texture2D>(TreeBranchesDiffusePath);
            Texture2D treeBranchesNormal = LoadRequiredAsset<Texture2D>(TreeBranchesNormalPath);
            Texture2D treeTrunkDiffuse = LoadRequiredAsset<Texture2D>(TreeTrunkDiffusePath);
            Texture2D treeTrunkNormal = LoadRequiredAsset<Texture2D>(TreeTrunkNormalPath);
            Texture2D treeLeavesDiffuse = LoadRequiredAsset<Texture2D>(TreeLeavesDiffusePath);
            Texture2D treeLeavesNormal = LoadRequiredAsset<Texture2D>(TreeLeavesNormalPath);
            Texture2D treeLeavesAlpha = LoadRequiredAsset<Texture2D>(TreeLeavesAlphaPath);

            Material groundMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/GrassGround.mat", "StargazingHill/Environment",
                new Color(0.20f, 0.38f, 0.17f), 0.58f);
            Material hillMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/HillGrass.mat", "StargazingHill/Environment",
                new Color(0.18f, 0.34f, 0.14f), 0.58f);
            Material bladeMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/GrassBlades.mat", "StargazingHill/Environment",
                new Color(0.23f, 0.44f, 0.17f), 0.66f);
            Material branchMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/JacarandaBranches.mat", "StargazingHill/Environment",
                Color.white, 0.42f);
            Material trunkMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/JacarandaTrunk.mat", "StargazingHill/Environment",
                Color.white, 0.42f);
            Material leafMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/JacarandaLeaves.mat", "StargazingHill/Environment",
                Color.white, 0.48f);
            ConfigureTexturedMaterial(groundMaterial, grassDiffuse, grassNormal, null,
                new Vector2(40f, 40f), false, true);
            ConfigureTexturedMaterial(hillMaterial, grassDiffuse, grassNormal, null,
                new Vector2(10f, 10f), false, true);
            ConfigureTexturedMaterial(branchMaterial, treeBranchesDiffuse, treeBranchesNormal, null,
                Vector2.one, false, false);
            ConfigureTexturedMaterial(trunkMaterial, treeTrunkDiffuse, treeTrunkNormal, null,
                Vector2.one, false, false);
            ConfigureTexturedMaterial(leafMaterial, treeLeavesDiffuse, treeLeavesNormal, treeLeavesAlpha,
                Vector2.one, true, false);
            Material starMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/Starfield.mat", "StargazingHill/Starfield", Color.white, 0f);
            starMaterial.SetFloat("_Intensity", 1.35f);
            starMaterial.SetFloat("_HorizonStart", 0f);
            starMaterial.SetFloat("_HorizonFull", Mathf.Sin(15f * Mathf.Deg2Rad));
            EditorUtility.SetDirty(starMaterial);
            Material meteorMaterial = CreateOrUpdateMaterial(
                MaterialRoot + "/Meteor.mat", "StargazingHill/Meteor",
                new Color(0.65f, 0.82f, 1f), 0f);
            meteorMaterial.SetFloat("_Intensity", 6.0f);
            EditorUtility.SetDirty(meteorMaterial);

            Mesh groundMesh = SaveMesh(MeshRoot + "/GrassGround.asset", BuildGroundMesh());
            Mesh hillMesh = SaveMesh(MeshRoot + "/Hill.asset", BuildHillMesh());
            Mesh grassMesh = SaveMesh(MeshRoot + "/GrassClusters.asset", BuildGrassMesh());
            Mesh starMesh = SaveMesh(MeshRoot + "/Starfield_Celestial.asset", BuildStarMesh());
            Mesh meteorMesh = SaveMesh(MeshRoot + "/MeteorQuad.asset", BuildMeteorMesh());

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureRenderSettings();

            GameObject world = new GameObject("World");
            GameObject environment = CreateChild(world.transform, "Environment");
            CreateEnvironment(environment.transform, groundMesh, hillMesh, grassMesh, treeMesh,
                groundMaterial, hillMaterial, bladeMaterial, branchMaterial, trunkMaterial, leafMaterial);
            CreateLighting(environment.transform);
            CreateWorldSettings(world.transform);
            CreateRealSky(world.transform, starMesh, starMaterial);
            CreateMeteorSystem(world.transform, meteorMesh, meteorMaterial);
            CreateYamaPlayer(world.transform);
            CreateDrawingSystems(world.transform);

            ValidateScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Stargazing Hill] Build complete: textured walkable grassland and hill, CC0 landmark tree, " +
                      "Tokyo real-time sky, YamaPlayer, QvPen, and locally licensed UnyStylus.");
        }

        [MenuItem("Stargazing Hill/Debug/Trigger Hourly Meteor Shower", false, 50)]
        public static void DebugTriggerHourlyMeteorShower()
        {
            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            if (!Application.isPlaying || controller == null)
            {
                Debug.LogWarning("[Stargazing Hill] Enter Play Mode with the generated scene open before triggering meteors.");
                return;
            }
            controller.DebugTriggerHourlyEvent();
            Debug.Log("[Stargazing Hill] Forced the local hourly meteor event from t=0.");
        }

        [MenuItem("Stargazing Hill/Debug/Advance Sky +1 Hour", false, 51)]
        public static void DebugAdvanceSkyOneHour()
        {
            RealSkyController controller = Object.FindObjectOfType<RealSkyController>(true);
            if (!Application.isPlaying || controller == null)
            {
                Debug.LogWarning("[Stargazing Hill] Enter Play Mode with the generated scene open before advancing the sky.");
                return;
            }
            controller.DebugAdvanceOneHour();
            Debug.Log("[Stargazing Hill] Advanced the local sky debug offset by one hour.");
        }

        [MenuItem("Stargazing Hill/Debug/Reset Sky Time Offset", false, 52)]
        public static void DebugResetSkyTimeOffset()
        {
            RealSkyController controller = Object.FindObjectOfType<RealSkyController>(true);
            if (!Application.isPlaying || controller == null)
            {
                Debug.LogWarning("[Stargazing Hill] Enter Play Mode with the generated scene open before resetting the sky.");
                return;
            }
            controller.DebugResetTimeOffset();
            Debug.Log("[Stargazing Hill] Reset the local sky debug offset.");
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

        public static void TestSkyAndMeteorForBatchMode()
        {
            Quaternion reference = RealSkyController.CalculateSkyRotation(
                2026, 8, 11, 12, 0, 0.0, 35.68f, 139.76f);
            Quaternion sameReference = RealSkyController.CalculateSkyRotation(
                2026, 8, 11, 12, 0, 0.0, 35.68f, 139.76f);
            Quaternion oneHourLater = RealSkyController.CalculateSkyRotation(
                2026, 8, 11, 13, 0, 0.0, 35.68f, 139.76f);
            Quaternion oneDayLater = RealSkyController.CalculateSkyRotation(
                2026, 8, 12, 12, 0, 0.0, 35.68f, 139.76f);
            float oneHourMotion = Quaternion.Angle(reference, oneHourLater);
            float oneDayResidual = Quaternion.Angle(reference, oneDayLater);
            if (Quaternion.Angle(reference, sameReference) > 0.0001f ||
                oneHourMotion < 14.9f || oneHourMotion > 15.2f ||
                oneDayResidual < 0.9f || oneDayResidual > 1.1f)
                throw new InvalidOperationException(
                    "Sky motion test failed: hour=" + oneHourMotion + ", dayResidual=" + oneDayResidual);

            int eventId = MeteorController.GetHourlyEventId(2026, 8, 11, 12);
            int nextEventId = MeteorController.GetHourlyEventId(2026, 8, 11, 13);
            float sample = MeteorController.DebugSampleValue(eventId, 2, 1, 4);
            float sameSample = MeteorController.DebugSampleValue(eventId, 2, 1, 4);
            float nextSample = MeteorController.DebugSampleValue(nextEventId, 2, 1, 4);
            if (eventId == nextEventId || !Mathf.Approximately(sample, sameSample) ||
                Mathf.Approximately(sample, nextSample))
                throw new InvalidOperationException("Hourly meteor determinism test failed.");

            Debug.Log("[Stargazing Hill] Sky/meteor test passed: +1h=" + oneHourMotion.ToString("F4") +
                      " degrees, +24h residual=" + oneDayResidual.ToString("F4") +
                      " degrees, deterministic hourly event IDs=" + eventId + "/" + nextEventId + ".");
        }

        public static void RenderPreviewForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            GameObject cameraObject = GameObject.Find("World/WorldSettings/ReferenceCamera");
            Camera camera = cameraObject.GetComponent<Camera>();
            RenderCameraToPng(camera, "stargazing-hill-preview.png");
        }

        public static void RenderInspectionPreviewsForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            Camera camera = GameObject.Find("World/WorldSettings/ReferenceCamera").GetComponent<Camera>();
            float treeBaseY = EvaluateTerrainHeight(HillPosition.x, HillPosition.z);

            camera.transform.position = HillPosition + new Vector3(-13f, 5.3f, -13f);
            camera.transform.LookAt(HillPosition + Vector3.up * (treeBaseY + 4.2f));
            RenderCameraToPng(camera, "stargazing-hill-tree-front.png");

            camera.transform.position = HillPosition + new Vector3(13f, 5.3f, -13f);
            camera.transform.LookAt(HillPosition + Vector3.up * (treeBaseY + 4.2f));
            RenderCameraToPng(camera, "stargazing-hill-tree-side.png");

            float spawnSurfaceY = EvaluateTerrainHeight(SpawnGroundPosition.x, SpawnGroundPosition.z);
            camera.transform.position = new Vector3(
                SpawnGroundPosition.x, spawnSurfaceY + 1.65f, SpawnGroundPosition.z);
            camera.transform.LookAt(new Vector3(
                AmenityCenter.x, EvaluateTerrainHeight(AmenityCenter.x, AmenityCenter.z) + 2.4f,
                AmenityCenter.z));
            RenderCameraToPng(camera, "stargazing-hill-amenities.png");
        }

        public static void RenderMeteorDebugPreviewForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            Camera camera = GameObject.Find("World/WorldSettings/ReferenceCamera").GetComponent<Camera>();
            Vector3 observer = new Vector3(
                SpawnGroundPosition.x,
                EvaluateTerrainHeight(SpawnGroundPosition.x, SpawnGroundPosition.z) + 1.65f,
                SpawnGroundPosition.z);
            controller.transform.position = observer;
            controller.DebugPreviewEventAtSecond(2.4f);

            Renderer visibleMeteor = null;
            for (int index = 0; index < controller.meteorRenderers.Length; index++)
            {
                if (controller.meteorRenderers[index].enabled)
                {
                    visibleMeteor = controller.meteorRenderers[index];
                    break;
                }
            }
            if (visibleMeteor == null)
                throw new InvalidOperationException("Meteor debug preview did not enable a pooled visual.");

            camera.transform.position = observer;
            camera.transform.LookAt(visibleMeteor.transform.position);
            RenderCameraToPng(camera, "stargazing-hill-meteor-debug.png");
            controller.DebugStopHourlyEvent();
        }

        public static void ReportUserLayoutForBatchMode()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string[] paths =
            {
                "World/WorldSettings/Spawn",
                "World/VideoSystem/YamaPlayer",
                "World/DrawingSystem/QvPen",
                "World/DrawingSystem/UnyStylus"
            };
            for (int index = 0; index < paths.Length; index++)
            {
                GameObject target = GameObject.Find(paths[index]);
                if (target == null)
                {
                    Debug.LogError("[UserLayout] Missing " + paths[index]);
                    continue;
                }
                Transform transform = target.transform;
                Debug.Log("[UserLayout] " + paths[index] +
                          " localPosition=" + transform.localPosition.ToString("F6") +
                          " localEuler=" + transform.localEulerAngles.ToString("F6") +
                          " localScale=" + transform.localScale.ToString("F6"));
            }
        }

        public static void ReportJacarandaAssetForBatchMode()
        {
            Mesh mesh = LoadRequiredAsset<Mesh>(TreeMeshPath);
            long triangles = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
            Debug.Log("[Jacaranda] mesh=" + mesh.name + " vertices=" + mesh.vertexCount +
                      " triangles=" + triangles + " submeshes=" + mesh.subMeshCount +
                      " bounds=" + mesh.bounds);
        }

        private static void RenderCameraToPng(Camera camera, string fileName)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            image.Apply();
            string previewPath = Path.Combine(Path.GetTempPath(), fileName);
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

        private static void ConfigureThirdPartyImportSettings()
        {
            ConfigureTextureImporter(GrassDiffusePath, false, false);
            ConfigureTextureImporter(GrassNormalPath, true, false);
            ConfigureTextureImporter(TreeBranchesDiffusePath, false, false);
            ConfigureTextureImporter(TreeBranchesNormalPath, true, false);
            ConfigureTextureImporter(TreeTrunkDiffusePath, false, false);
            ConfigureTextureImporter(TreeTrunkNormalPath, true, false);
            ConfigureTextureImporter(TreeLeavesDiffusePath, false, false);
            ConfigureTextureImporter(TreeLeavesNormalPath, true, false);
            ConfigureTextureImporter(TreeLeavesAlphaPath, false, false);
        }

        private static void ConfigureTextureImporter(string path, bool normalMap, bool alphaTransparency)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            TextureImporterType type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool dirty = importer.textureType != type || importer.maxTextureSize != 1024 ||
                         importer.alphaIsTransparency != alphaTransparency || !importer.mipmapEnabled;
            if (!dirty) return;
            importer.textureType = type;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.alphaIsTransparency = alphaTransparency;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static void EnsureProgramAsset(Type behaviourType, string scriptPath, string programPath)
        {
            UdonSharpProgramAsset programAsset =
                UdonSharpEditorUtility.GetUdonSharpProgramAsset(behaviourType);
            if (programAsset == null)
                programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(programPath);
            if (programAsset == null)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script == null)
                    throw new InvalidOperationException(behaviourType.Name + " MonoScript is missing.");

                programAsset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                programAsset.sourceCsScript = script;
                AssetDatabase.CreateAsset(programAsset, programPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Stargazing Hill] Created U# program asset for " + behaviourType.Name + ".");
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

            programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset(behaviourType);
            if (programAsset == null ||
                programAsset.ScriptVersion < UdonSharpProgramVersion.CurrentVersion ||
                programAsset.CompiledVersion < UdonSharpProgramVersion.CurrentVersion)
                throw new InvalidOperationException(behaviourType.Name + " U# program asset did not compile.");
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset is missing: " + path);
            return asset;
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
            if (material.HasProperty("_UseVertexColor")) material.SetFloat("_UseVertexColor", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTexturedMaterial(
            Material material, Texture2D albedo, Texture2D normal, Texture2D alpha,
            Vector2 tiling, bool alphaClip, bool useVertexColor)
        {
            material.SetTexture("_MainTex", albedo);
            material.SetTextureScale("_MainTex", tiling);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_AlphaMap", alpha);
            material.SetFloat("_UseTexture", albedo == null ? 0f : 1f);
            material.SetFloat("_UseNormal", normal == null ? 0f : 1f);
            material.SetFloat("_UseAlphaMap", alpha == null ? 0f : 1f);
            material.SetFloat("_UseVertexColor", useVertexColor ? 1f : 0f);
            material.SetFloat("_Cutoff", alphaClip ? 0.36f : 0f);
            if (alphaClip) material.EnableKeyword("_ALPHATEST_ON");
            else material.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
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

        private static float EvaluateBaseHeight(float x, float z)
        {
            return Mathf.Sin(x * 0.21f) * Mathf.Cos(z * 0.17f) * 0.055f;
        }

        private static float EvaluateHillHeight(float x, float z)
        {
            float distance = Vector2.Distance(new Vector2(x, z), new Vector2(HillPosition.x, HillPosition.z));
            if (distance >= HillRadius) return 0f;
            if (distance <= HillTopRadius)
            {
                float topT = distance / HillTopRadius;
                return HillHeight - 0.06f * topT * topT;
            }

            float t = Mathf.InverseLerp(HillTopRadius, HillRadius, distance);
            float smooth = t * t * (3f - 2f * t);
            return Mathf.Lerp(HillHeight - 0.06f, 0f, smooth);
        }

        private static float EvaluateTerrainHeight(float x, float z)
        {
            return EvaluateBaseHeight(x, z) + EvaluateHillHeight(x, z);
        }

        private static Mesh BuildGroundMesh()
        {
            const int cells = 80;
            const float size = 80f;
            int side = cells + 1;
            var vertices = new Vector3[side * side];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[cells * cells * 6];

            for (int z = 0; z < side; z++)
            {
                for (int x = 0; x < side; x++)
                {
                    float px = (x / (float)cells - 0.5f) * size;
                    float pz = (z / (float)cells - 0.5f) * size;
                    float y = EvaluateTerrainHeight(px, pz);
                    int index = z * side + x;
                    vertices[index] = new Vector3(px, y, pz);
                    uvs[index] = new Vector2(x / (float)cells, z / (float)cells);
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
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildHillMesh()
        {
            const int rings = 16;
            const int segments = 64;
            var vertices = new List<Vector3>(1 + rings * segments);
            var colors = new List<Color>(1 + rings * segments);
            var uvs = new List<Vector2>(1 + rings * segments);
            var triangles = new List<int>(segments * (1 + (rings - 1) * 2) * 3);
            vertices.Add(new Vector3(0f, EvaluateTerrainHeight(HillPosition.x, HillPosition.z) + 0.018f, 0f));
            colors.Add(Color.white);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int ring = 1; ring <= rings; ring++)
            {
                float t = ring / (float)rings;
                float ringRadius = HillRadius * t;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    float localX = Mathf.Cos(angle) * ringRadius;
                    float localZ = Mathf.Sin(angle) * ringRadius;
                    float worldX = HillPosition.x + localX;
                    float worldZ = HillPosition.z + localZ;
                    float height = EvaluateTerrainHeight(worldX, worldZ) + 0.018f;
                    vertices.Add(new Vector3(localX, height, localZ));
                    float shade = 0.90f + 0.10f * Mathf.Sin(angle * 3f + ring);
                    colors.Add(new Color(shade, shade, shade, 1f));
                    uvs.Add(new Vector2(0.5f + localX / (HillRadius * 2f), 0.5f + localZ / (HillRadius * 2f)));
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
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildGrassMesh()
        {
            const int tuftCount = 9000;
            var random = new System.Random(20260811);
            var vertices = new List<Vector3>(tuftCount * 12);
            var colors = new List<Color>(tuftCount * 12);
            var triangles = new List<int>(tuftCount * 12);

            for (int tuft = 0; tuft < tuftCount; tuft++)
            {
                float x = (float)(random.NextDouble() * 76.0 - 38.0);
                float z = (float)(random.NextDouble() * 76.0 - 38.0);
                if (IsGrassExclusion(x, z))
                {
                    tuft--;
                    continue;
                }

                float baseY = EvaluateTerrainHeight(x, z) + 0.012f;
                float height = 0.035f + (float)random.NextDouble() * 0.060f;
                float width = 0.010f + (float)random.NextDouble() * 0.010f;
                float phase = (float)random.NextDouble() * Mathf.PI * 2f;
                float shade = 0.78f + (float)random.NextDouble() * 0.25f;
                Color color = new Color(shade, shade, shade, 1f);

                for (int blade = 0; blade < 4; blade++)
                {
                    float angle = phase + blade * Mathf.PI * 0.5f;
                    Vector3 side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * width;
                    Vector3 bend = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)) * height * 0.13f;
                    int first = vertices.Count;
                    vertices.Add(new Vector3(x, baseY, z) - side);
                    vertices.Add(new Vector3(x, baseY, z) + side);
                    vertices.Add(new Vector3(x, baseY + height, z) + bend);
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

        private static bool IsGrassExclusion(float x, float z)
        {
            return new Vector2(x, z + 14f).sqrMagnitude < 5f ||
                   new Vector2(x + 15f, z - 5f).sqrMagnitude < 28f ||
                   new Vector2(x - 17f, z + 12f).sqrMagnitude < 42f ||
                   new Vector2(x - HillPosition.x, z - HillPosition.z).sqrMagnitude < 1.6f;
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

        private static Mesh BuildMeteorMesh()
        {
            var mesh = new Mesh { name = "MeteorQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
            Mesh treeMesh, Material groundMaterial, Material hillMaterial, Material bladeMaterial,
            Material branchMaterial, Material trunkMaterial, Material leafMaterial)
        {
            GameObject ground = CreateMeshObject(parent, "GrassGround", groundMesh, groundMaterial, Vector3.zero);
            var groundCollider = ground.AddComponent<MeshCollider>();
            groundCollider.sharedMesh = groundMesh;

            // The hill is a visual overlay only. Its shape is already part of the single ground collider,
            // preventing the VRChat player capsule from becoming wedged between overlapping mesh colliders.
            CreateMeshObject(parent, "Hill", hillMesh, hillMaterial, HillPosition);

            CreateMeshObject(parent, "GrassClusters", grassMesh, bladeMaterial, Vector3.zero);

            GameObject tree = CreateChild(parent, "LandmarkTree");
            tree.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            tree.transform.localScale = Vector3.one * 0.40f;
            GameObject treeModel = CreateChild(tree.transform, "Model");
            MeshFilter treeFilter = treeModel.AddComponent<MeshFilter>();
            treeFilter.sharedMesh = treeMesh;
            MeshRenderer treeRenderer = treeModel.AddComponent<MeshRenderer>();
            treeRenderer.sharedMaterials = new[] { branchMaterial, trunkMaterial, leafMaterial };
            treeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            treeRenderer.receiveShadows = false;
            treeRenderer.lightProbeUsage = LightProbeUsage.Off;
            treeRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // The baked mesh already contains Poly Haven's FBX axis/unit conversion. The outer anchor only
            // applies world scale/yaw, and bounds-based placement keeps the multi-trunk base on the hill.
            Bounds importedBounds = CalculateRendererBounds(tree);
            float treeSurfaceY = EvaluateTerrainHeight(HillPosition.x, HillPosition.z) + 0.02f;
            tree.transform.position += new Vector3(
                HillPosition.x - importedBounds.center.x,
                treeSurfaceY - importedBounds.min.y,
                HillPosition.z - importedBounds.center.z);

            GameObject colliderObject = CreateChild(parent, "LandmarkTreeCollider");
            colliderObject.transform.position = new Vector3(HillPosition.x, treeSurfaceY + 2.40f, HillPosition.z);
            var trunkCollider = colliderObject.AddComponent<CapsuleCollider>();
            trunkCollider.height = 4.80f;
            trunkCollider.radius = 0.78f;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                bounds.Encapsulate(renderers[rendererIndex].bounds);
            return bounds;
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
            float spawnSurfaceY = EvaluateTerrainHeight(SpawnGroundPosition.x, SpawnGroundPosition.z);
            spawn.transform.position = new Vector3(
                SpawnGroundPosition.x, spawnSurfaceY + 0.40f, SpawnGroundPosition.z);
            spawn.transform.rotation = Quaternion.Euler(SpawnEuler);

            GameObject cameraObject = CreateChild(settings.transform, "ReferenceCamera");
            cameraObject.transform.position = new Vector3(
                SpawnGroundPosition.x, spawnSurfaceY + 1.65f, SpawnGroundPosition.z);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                (HillPosition + Vector3.up * 4.2f - cameraObject.transform.position).normalized,
                Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.009f, 0.020f);
            camera.farClipPlane = 500f;
            camera.nearClipPlane = 0.03f;
            camera.enabled = false;

            GameObject descriptorObject = CreateChild(settings.transform, "VRCWorld");
            VRCSceneDescriptor descriptor = descriptorObject.AddComponent<VRCSceneDescriptor>();
            descriptorObject.AddComponent<PipelineManager>();
            descriptor.spawns = new[] { spawn.transform };
            descriptor.ReferenceCamera = cameraObject;
            descriptor.RespawnHeightY = -15f;

            GameObject playerSettingsObject = CreateChild(settings.transform, "PlayerMovementSettings");
            WorldPlayerSettings playerSettings = UdonSharpUndo.AddComponent<WorldPlayerSettings>(playerSettingsObject);
            playerSettings.walkSpeed = 2f;
            playerSettings.runSpeed = 4f;
            playerSettings.strafeSpeed = 2f;
            playerSettings.jumpImpulse = 3.2f;
            playerSettings.gravityStrength = 1f;
            UdonSharpEditorUtility.CopyProxyToUdon(playerSettings);
            EditorUtility.SetDirty(playerSettings);
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

        private static void CreateMeteorSystem(Transform world, Mesh meteorMesh, Material meteorMaterial)
        {
            GameObject system = CreateChild(world, "MeteorShowerSystem");
            MeteorController controller = UdonSharpUndo.AddComponent<MeteorController>(system);
            controller.eventDurationSeconds = 25f;
            controller.skyRadius = 65f;

            const int poolSize = 4;
            controller.meteorTransforms = new Transform[poolSize];
            controller.meteorRenderers = new Renderer[poolSize];
            GameObject visuals = CreateChild(system.transform, "MeteorVisuals");
            for (int index = 0; index < poolSize; index++)
            {
                GameObject meteor = CreateMeshObject(
                    visuals.transform, "Meteor_" + (index + 1), meteorMesh, meteorMaterial, Vector3.zero);
                MeshRenderer renderer = meteor.GetComponent<MeshRenderer>();
                renderer.enabled = false;
                controller.meteorTransforms[index] = meteor.transform;
                controller.meteorRenderers[index] = renderer;
            }

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
            player.transform.localPosition = YamaPlayerPosition;
            player.transform.localRotation = Quaternion.Euler(YamaPlayerEuler);
            player.transform.localScale = Vector3.one * 2.20f;

            EnsureVideoInfoDownloader(player);
            RemoveEmptyPlaylistManagers(player);
            ApplyYamaDistanceRolloff(player);
        }

        private static void RemoveEmptyPlaylistManagers(GameObject player)
        {
            PlaylistManager[] managers = player.GetComponentsInChildren<PlaylistManager>(true);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i].GetPlaylists().Count == 0) Object.DestroyImmediate(managers[i]);
            }
        }

        private static void CreateDrawingSystems(Transform world)
        {
            GameObject drawingSystem = CreateChild(world, "DrawingSystem");
            InstantiateDrawingPrefab(drawingSystem.transform, QvPenPrefabPath, "QvPen",
                QvPenPosition, Quaternion.Euler(QvPenEuler));
            InstantiateDrawingPrefab(drawingSystem.transform, UnyStylusPrefabPath, "UnyStylus",
                UnyStylusPosition, Quaternion.Euler(UnyStylusEuler));
        }

        private static void InstantiateDrawingPrefab(
            Transform parent, string path, string name, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new InvalidOperationException(name + " is not installed at " + path + ".");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one;
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
            MeteorController[] meteorControllers = Object.FindObjectsOfType<MeteorController>(true);
            WorldPlayerSettings[] playerSettings = Object.FindObjectsOfType<WorldPlayerSettings>(true);
            VRCSceneDescriptor[] descriptors = Object.FindObjectsOfType<VRCSceneDescriptor>(true);
            PipelineManager[] pipelineManagers = Object.FindObjectsOfType<PipelineManager>(true);
            ModuleManager[] yamaManagers = Object.FindObjectsOfType<ModuleManager>(true);
            if (skyControllers.Length != 1 || skyControllers[0].celestialSphere == null)
                throw new InvalidOperationException("RealSkyController validation failed.");
            if (meteorControllers.Length != 1 || meteorControllers[0].meteorTransforms == null ||
                meteorControllers[0].meteorRenderers == null ||
                meteorControllers[0].meteorTransforms.Length != 4 ||
                meteorControllers[0].meteorRenderers.Length != 4 ||
                !Mathf.Approximately(meteorControllers[0].eventDurationSeconds, 25f) ||
                meteorControllers[0].GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Hourly meteor/debug system validation failed.");
            for (int meteorIndex = 0; meteorIndex < meteorControllers[0].meteorRenderers.Length; meteorIndex++)
            {
                Renderer meteorRenderer = meteorControllers[0].meteorRenderers[meteorIndex];
                if (meteorRenderer == null || meteorRenderer.enabled || meteorRenderer.sharedMaterial == null ||
                    meteorRenderer.sharedMaterial.shader.name != "StargazingHill/Meteor")
                    throw new InvalidOperationException("Meteor visual pool validation failed.");
            }
            if (descriptors.Length != 1 || descriptors[0].spawns == null || descriptors[0].spawns.Length != 1)
                throw new InvalidOperationException("VRCSceneDescriptor validation failed.");
            if (pipelineManagers.Length != 1 || pipelineManagers[0].gameObject != descriptors[0].gameObject)
                throw new InvalidOperationException("VRC PipelineManager validation failed.");
            if (playerSettings.Length != 1 || !Mathf.Approximately(playerSettings[0].jumpImpulse, 3.2f) ||
                !Mathf.Approximately(playerSettings[0].walkSpeed, 2f) ||
                !Mathf.Approximately(playerSettings[0].runSpeed, 4f))
                throw new InvalidOperationException("Player locomotion validation failed.");
            if (yamaManagers.Length != 1)
                throw new InvalidOperationException("YamaPlayer validation failed.");
            GameObject tree = GameObject.Find("World/Environment/LandmarkTree");
            GameObject treeColliderObject = GameObject.Find("World/Environment/LandmarkTreeCollider");
            if (tree == null || tree.GetComponentsInChildren<MeshRenderer>(true).Length == 0 ||
                treeColliderObject == null || treeColliderObject.GetComponent<CapsuleCollider>() == null)
                throw new InvalidOperationException("Landmark tree validation failed.");
            Bounds treeBounds = CalculateRendererBounds(tree);
            Transform treeModel = tree.transform.Find("Model");
            Mesh treeAsset = treeModel == null ? null : treeModel.GetComponent<MeshFilter>()?.sharedMesh;
            long treeTriangles = 0;
            if (treeAsset != null)
            {
                for (int subMesh = 0; subMesh < treeAsset.subMeshCount; subMesh++)
                    treeTriangles += (long)treeAsset.GetIndexCount(subMesh) / 3L;
            }
            float expectedTreeBase = EvaluateTerrainHeight(HillPosition.x, HillPosition.z) + 0.02f;
            float treeMeshWidth = treeAsset == null ? 0f : treeAsset.bounds.size.x * tree.transform.lossyScale.x;
            if (treeModel == null || treeAsset == null || treeAsset.subMeshCount != 3 ||
                treeTriangles < 450000L || treeTriangles > 480000L ||
                Vector3.Angle(treeModel.TransformDirection(Vector3.up), Vector3.up) > 1f ||
                treeBounds.size.y < 7.5f || treeBounds.size.y > 8.5f ||
                treeMeshWidth < 8.5f || treeMeshWidth > 10.5f ||
                Mathf.Abs(treeBounds.min.y - expectedTreeBase) > 0.05f ||
                Vector2.Distance(new Vector2(treeBounds.center.x, treeBounds.center.z),
                    new Vector2(HillPosition.x, HillPosition.z)) > 0.05f)
                throw new InvalidOperationException("Landmark tree import scale validation failed: " + treeBounds);
            GameObject qvPen = GameObject.Find("World/DrawingSystem/QvPen");
            GameObject unyStylus = GameObject.Find("World/DrawingSystem/UnyStylus");
            if (qvPen == null || unyStylus == null)
                throw new InvalidOperationException("Drawing-system validation failed.");

            GameObject ground = GameObject.Find("World/Environment/GrassGround");
            GameObject hill = GameObject.Find("World/Environment/Hill");
            MeshCollider groundCollider = ground == null ? null : ground.GetComponent<MeshCollider>();
            if (groundCollider == null || hill == null || hill.GetComponent<Collider>() != null)
                throw new InvalidOperationException("Walkable terrain must use one non-overlapping collider.");
            MeshFilter grassFilter = GameObject.Find("World/Environment/GrassClusters")?.GetComponent<MeshFilter>();
            if (grassFilter == null || grassFilter.sharedMesh == null || grassFilter.sharedMesh.vertexCount < 100000)
                throw new InvalidOperationException("Volumetric grass density validation failed.");
            ValidateWalkableSurface(descriptors[0].spawns[0]);

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
            if (yamaPlayer == null || yamaPlayer.transform.localScale != Vector3.one * 2.20f ||
                downloaderCount != 1)
            {
                throw new InvalidOperationException("YamaPlayer placement/module validation failed.");
            }
            ValidateAmenityPlacement(descriptors[0].spawns[0], yamaPlayer, qvPen, unyStylus);

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

        private static void ValidateWalkableSurface(Transform spawn)
        {
            Physics.SyncTransforms();
            Vector3 topSample = new Vector3(HillPosition.x + 1.5f, 8f, HillPosition.z);
            Vector3 slopeSample = new Vector3(HillPosition.x + 6f, 8f, HillPosition.z);
            if (!Physics.Raycast(topSample, Vector3.down, out RaycastHit topHit, 12f) ||
                !Physics.Raycast(slopeSample, Vector3.down, out RaycastHit slopeHit, 12f))
                throw new InvalidOperationException("Hill collider raycast validation failed.");
            if (topHit.point.y < 2.15f || Vector3.Angle(slopeHit.normal, Vector3.up) > 28f)
                throw new InvalidOperationException("Hill height/slope is not normally walkable.");

            float expectedSpawnSurface = EvaluateTerrainHeight(spawn.position.x, spawn.position.z);
            if (spawn.position.y < expectedSpawnSurface + 0.30f ||
                !Approximately(spawn.position,
                    new Vector3(SpawnGroundPosition.x, expectedSpawnSurface + 0.40f, SpawnGroundPosition.z), 0.001f) ||
                Quaternion.Angle(spawn.rotation, Quaternion.Euler(SpawnEuler)) > 0.01f)
                throw new InvalidOperationException("Spawn is embedded in the terrain.");
        }

        private static void ValidateAmenityPlacement(
            Transform spawn, GameObject yamaPlayer, GameObject qvPen, GameObject unyStylus)
        {
            if (!Approximately(yamaPlayer.transform.localPosition, YamaPlayerPosition, 0.001f) ||
                Quaternion.Angle(yamaPlayer.transform.localRotation, Quaternion.Euler(YamaPlayerEuler)) > 0.01f ||
                !Approximately(qvPen.transform.localPosition, QvPenPosition, 0.001f) ||
                Quaternion.Angle(qvPen.transform.localRotation, Quaternion.Euler(QvPenEuler)) > 0.01f ||
                !Approximately(unyStylus.transform.localPosition, UnyStylusPosition, 0.001f) ||
                Quaternion.Angle(unyStylus.transform.localRotation, Quaternion.Euler(UnyStylusEuler)) > 0.01f)
                throw new InvalidOperationException("User-confirmed spawn amenity layout changed.");
        }

        private static bool Approximately(Vector3 left, Vector3 right, float tolerance)
        {
            return (left - right).sqrMagnitude <= tolerance * tolerance;
        }
    }
}
