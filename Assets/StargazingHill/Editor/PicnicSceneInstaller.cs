using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StargazingHill.Editor
{
    public static class PicnicSceneInstaller
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string AssetRoot = "Assets/StargazingHill/ThirdParty/TinyTreats/PleasantPicnic";
        private const string MaterialRoot = "Assets/StargazingHill/Generated/Materials";
        private const string AtlasTexturePath = AssetRoot + "/tiny_treats_texture_1.png";
        private const string PlaidTexturePath = AssetRoot + "/tiny_treats_plaid_pattern_blue.png";
        private const string AtlasMaterialPath = MaterialRoot + "/PicnicAtlas.mat";
        private const string PlaidMaterialPath = MaterialRoot + "/PicnicPlaidBlue.mat";
        private const string BlanketMeshPath =
            "Assets/StargazingHill/Generated/Meshes/PicnicBlanketTerrain.asset";
        private const string LayoutPath =
            "Assets/StargazingHill/Editor/Data/PicnicLayout.json";
        private const float BlanketGroundClearance = 0.018f;
        private const float RigidPropMinimumTerrainClearance = 0.08f;
        private const float RigidPropMaximumTerrainClearance = 0.18f;
        private const float SoftFurnishingMinimumTerrainClearance = -0.08f;
        private const float SoftFurnishingMaximumTerrainClearance = 0.12f;

        private static readonly string[] ModelNames =
        {
            "picnic_blanket_blue",
            "radio",
            "teapot",
            "mug",
            "pillow_small_blue",
            "pillow_large_blue"
        };

        private static readonly PicnicItemDefinition[] ItemDefinitions =
        {
            new PicnicItemDefinition("PicnicBlanketBlue", "picnic_blanket_blue", true, true, false),
            new PicnicItemDefinition("PicnicRadio", "radio", false, false, false),
            new PicnicItemDefinition("PicnicTeapot", "teapot", false, false, false),
            new PicnicItemDefinition("PicnicMug", "mug", false, false, false),
            new PicnicItemDefinition("PicnicCushionBlueLeft", "pillow_large_blue", true, false, true),
            new PicnicItemDefinition("PicnicCushionBlueRight", "pillow_large_blue", true, false, true),
            new PicnicItemDefinition("PicnicPillowBlueLeft", "pillow_small_blue", false, false, true),
            new PicnicItemDefinition("PicnicPillowBlueRight", "pillow_small_blue", false, false, true)
        };

        [Serializable]
        private sealed class PicnicLayout
        {
            public int schemaVersion = 1;
            public PicnicItemTransform[] items;
        }

        [Serializable]
        private sealed class PicnicItemTransform
        {
            public string name;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Vector3 modelLocalPosition;
            public Quaternion modelLocalRotation;
            public Vector3 modelLocalScale;
        }

        private sealed class PicnicItemDefinition
        {
            public readonly string SceneName;
            public readonly string AssetName;
            public readonly bool UsePlaidMaterial;
            public readonly bool ConformToTerrain;
            public readonly bool AllowBlanketEmbedding;

            public PicnicItemDefinition(string sceneName, string assetName, bool usePlaidMaterial,
                bool conformToTerrain, bool allowBlanketEmbedding)
            {
                SceneName = sceneName;
                AssetName = assetName;
                UsePlaidMaterial = usePlaidMaterial;
                ConformToTerrain = conformToTerrain;
                AllowBlanketEmbedding = allowBlanketEmbedding;
            }
        }

        [MenuItem("Stargazing Hill/Content/Picnic/Rebuild from Saved Layout...", false, 31)]
        public static void InstallOrRefreshMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Picnic Spot",
                    "This replaces World/Environment/PicnicSpot with the versioned saved layout. " +
                    "Save the current layout first if it contains manual adjustments.",
                    "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Stargazing Hill/Content/Picnic/Save Current Scene Layout to Generator...", false, 30)]
        public static void CaptureCurrentLayoutMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Save Picnic Layout",
                    "Use the current PicnicSpot transforms as the generator source? " +
                    "This overwrites the versioned PicnicLayout.json file.",
                    "Save Layout", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CaptureCurrentLayout(scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Stargazing Hill",
                "Saved the current picnic transforms to:\n" + LayoutPath,
                "OK");
        }

        public static void CaptureCurrentLayoutForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CaptureCurrentLayout(scene);
            AssetDatabase.SaveAssets();
        }

        public static void InstallForBatchMode()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ValidateScene();
        }

        internal static void InstallForBuild(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            Transform environment = GameObject.Find("World/Environment")?.transform;
            if (environment == null) throw new InvalidOperationException("World environment is missing.");

            ConfigureImportSettings();
            Texture2D atlas = LoadRequiredAsset<Texture2D>(AtlasTexturePath);
            Texture2D plaid = LoadRequiredAsset<Texture2D>(PlaidTexturePath);
            Material atlasMaterial = CreateOrUpdateMaterial(AtlasMaterialPath, atlas);
            Material plaidMaterial = CreateOrUpdateMaterial(PlaidMaterialPath, plaid);
            PicnicLayout layout = LoadLayout();

            Transform old = environment.Find("PicnicSpot");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            GameObject root = new GameObject("PicnicSpot");
            root.transform.SetParent(environment, false);
            for (int index = 0; index < ItemDefinitions.Length; index++)
            {
                PicnicItemDefinition definition = ItemDefinitions[index];
                PicnicItemTransform item = FindLayoutItem(layout, definition.SceneName);
                Material material = definition.UsePlaidMaterial ? plaidMaterial : atlasMaterial;
                GameObject instance = PlaceItemFromLayout(root.transform, definition, item, material);
                if (definition.ConformToTerrain) ConformBlanketToTerrain(instance);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed terrain-conforming Tiny Treats picnic spot: " +
                "8 scene items, one blanket MeshCollider, no prop colliders or realtime shadows.");
        }

        internal static void ValidateScene()
        {
            GameObject root = GameObject.Find("World/Environment/PicnicSpot");
            if (root == null || root.transform.childCount != ItemDefinitions.Length)
                throw new InvalidOperationException("Picnic spot hierarchy validation failed.");
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            Transform blanket = root.transform.Find("PicnicBlanketBlue");
            MeshFilter blanketFilter = blanket != null ? blanket.GetComponentInChildren<MeshFilter>(true) : null;
            MeshCollider blanketCollider = blanket != null ? blanket.GetComponentInChildren<MeshCollider>(true) : null;
            int physicalColliderCount = 0;
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider collider = colliders[colliderIndex];
                if (!collider.isTrigger)
                {
                    physicalColliderCount++;
                    continue;
                }
                throw new InvalidOperationException(
                    "Picnic spot contains an unexpected interaction trigger: " + collider.name);
            }
            if (physicalColliderCount != 1 || blanketFilter == null || blanketCollider == null ||
                blanketCollider.sharedMesh != blanketFilter.sharedMesh || blanketCollider.convex ||
                blanketCollider.isTrigger)
                throw new InvalidOperationException(
                    "Picnic spot must contain exactly one physical collider: the static blanket MeshCollider.");

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            long triangles = 0;
            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                Mesh mesh = filters[filterIndex].sharedMesh;
                if (mesh == null) throw new InvalidOperationException("Picnic spot contains a missing mesh.");
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
            }
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null ||
                    renderer.sharedMaterial.shader.name != "StargazingHill/Environment" ||
                    renderer.shadowCastingMode != ShadowCastingMode.Off || renderer.receiveShadows)
                    throw new InvalidOperationException("Picnic spot renderer validation failed.");
            }

            Vector3 hill = StargazingWorldBuilder.HillPositionForEditor;
            Bounds bounds = CalculateRendererBounds(root);
            float horizontalDistance = Vector2.Distance(
                new Vector2(bounds.center.x, bounds.center.z), new Vector2(hill.x, hill.z));
            ValidateTerrainContact(root);
            ValidateLayoutMatchesSaved(root);
            if (triangles < 2800L || triangles > 3600L || renderers.Length == 0 ||
                horizontalDistance < 1.5f || horizontalDistance > 5.5f)
                throw new InvalidOperationException(
                    "Picnic spot placement/budget validation failed: triangles=" + triangles +
                    ", bounds=" + bounds);

            Debug.Log("[Stargazing Hill] Picnic spot validated: " + triangles + " triangles, bounds=" + bounds + ".");
        }

        private static GameObject PlaceItemFromLayout(Transform parent, PicnicItemDefinition definition,
            PicnicItemTransform item, Material material)
        {
            GameObject source = LoadRequiredAsset<GameObject>(AssetRoot + "/" + definition.AssetName + ".fbx");
            GameObject anchor = new GameObject(definition.SceneName);
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = item.position;
            anchor.transform.rotation = item.rotation;
            anchor.transform.localScale = item.scale;

            GameObject model = PrefabUtility.InstantiatePrefab(source, anchor.transform) as GameObject;
            if (model == null)
                throw new InvalidOperationException("Could not instantiate picnic model: " + definition.AssetName);
            model.name = "Model";
            model.transform.localPosition = item.modelLocalPosition;
            model.transform.localRotation = item.modelLocalRotation;
            model.transform.localScale = item.modelLocalScale;

            Collider[] colliders = anchor.GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                UnityEngine.Object.DestroyImmediate(colliders[colliderIndex]);

            Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(definition.AssetName + " has no renderer.");
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    materials[materialIndex] = material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            SetStaticRecursively(anchor);
            return anchor;
        }

        private static void ConformBlanketToTerrain(GameObject blanket)
        {
            MeshFilter[] filters = blanket.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || filters[0].sharedMesh == null)
                throw new InvalidOperationException("Picnic blanket must contain exactly one mesh.");

            MeshFilter filter = filters[0];
            Mesh generated = UnityEngine.Object.Instantiate(filter.sharedMesh);
            generated.name = "PicnicBlanketTerrain";
            Vector3[] vertices = generated.vertices;
            Bounds initialBounds = CalculateRendererBounds(blanket);
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[index]);
                float heightAboveBottom = world.y - initialBounds.min.y;
                world.y = StargazingWorldBuilder.EvaluateTerrainHeightForEditor(world.x, world.z) +
                    BlanketGroundClearance + heightAboveBottom;
                vertices[index] = filter.transform.InverseTransformPoint(world);
            }
            generated.vertices = vertices;
            generated.RecalculateBounds();
            generated.RecalculateNormals();
            generated.RecalculateTangents();
            MeshUtility.SetMeshCompression(generated, ModelImporterMeshCompression.Medium);

            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(BlanketMeshPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(generated, BlanketMeshPath);
                asset = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, asset);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(asset);
            }
            filter.sharedMesh = asset;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = asset;
            collider.convex = false;
            collider.isTrigger = false;
        }

        private static void ValidateTerrainContact(GameObject root)
        {
            Transform blanket = root.transform.Find("PicnicBlanketBlue");
            MeshFilter filter = blanket != null ? blanket.GetComponentInChildren<MeshFilter>(true) : null;
            if (filter == null || filter.sharedMesh == null)
                throw new InvalidOperationException("Terrain-conforming picnic blanket is missing.");

            Vector3[] vertices = filter.sharedMesh.vertices;
            float minimumClearance = float.MaxValue;
            float maximumClearance = float.MinValue;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[index]);
                float clearance = world.y -
                    StargazingWorldBuilder.EvaluateTerrainHeightForEditor(world.x, world.z);
                minimumClearance = Mathf.Min(minimumClearance, clearance);
                maximumClearance = Mathf.Max(maximumClearance, clearance);
            }
            if (minimumClearance < 0.012f || maximumClearance > 0.14f)
                throw new InvalidOperationException(
                    "Picnic blanket terrain contact failed: clearance=" + minimumClearance + ".." +
                    maximumClearance);

            for (int itemIndex = 1; itemIndex < ItemDefinitions.Length; itemIndex++)
            {
                PicnicItemDefinition definition = ItemDefinitions[itemIndex];
                Transform itemTransform = root.transform.Find(definition.SceneName);
                if (itemTransform == null)
                    throw new InvalidOperationException(definition.SceneName + " is missing.");
                GameObject item = itemTransform.gameObject;
                Bounds itemBounds = CalculateRendererBounds(item);
                float terrain = StargazingWorldBuilder.EvaluateTerrainHeightForEditor(
                    itemBounds.center.x, itemBounds.center.z);
                float clearance = itemBounds.min.y - terrain;
                float itemMinimumClearance = definition.AllowBlanketEmbedding
                    ? SoftFurnishingMinimumTerrainClearance
                    : RigidPropMinimumTerrainClearance;
                float itemMaximumClearance = definition.AllowBlanketEmbedding
                    ? SoftFurnishingMaximumTerrainClearance
                    : RigidPropMaximumTerrainClearance;
                if (clearance < itemMinimumClearance || clearance > itemMaximumClearance)
                    throw new InvalidOperationException(
                        item.name + " terrain contact failed: clearance=" + clearance);
            }
        }

        private static void CaptureCurrentLayout(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Open and save the StargazingHill scene before capturing layout.");
            Transform root = GameObject.Find("World/Environment/PicnicSpot")?.transform;
            if (root == null)
                throw new InvalidOperationException("World/Environment/PicnicSpot is missing.");
            if (root.childCount != ItemDefinitions.Length)
                throw new InvalidOperationException(
                    "PicnicSpot must contain exactly " + ItemDefinitions.Length +
                    " versioned items before its layout can be captured.");

            var layout = new PicnicLayout
            {
                schemaVersion = 1,
                items = new PicnicItemTransform[ItemDefinitions.Length]
            };
            for (int index = 0; index < ItemDefinitions.Length; index++)
            {
                PicnicItemDefinition definition = ItemDefinitions[index];
                Transform anchor = root.Find(definition.SceneName);
                if (anchor == null)
                    throw new InvalidOperationException("Picnic layout item is missing: " + definition.SceneName);
                Transform model = anchor.Find("Model");
                if (model == null)
                    throw new InvalidOperationException(definition.SceneName + "/Model is missing.");
                layout.items[index] = new PicnicItemTransform
                {
                    name = definition.SceneName,
                    position = anchor.position,
                    rotation = anchor.rotation,
                    scale = anchor.localScale,
                    modelLocalPosition = model.localPosition,
                    modelLocalRotation = model.localRotation,
                    modelLocalScale = model.localScale
                };
            }

            string directory = Path.GetDirectoryName(LayoutPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(LayoutPath, JsonUtility.ToJson(layout, true));
            AssetDatabase.ImportAsset(LayoutPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Stargazing Hill] Saved the current picnic layout as generator data: " + LayoutPath);
        }

        private static PicnicLayout LoadLayout()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (asset == null)
                throw new InvalidOperationException(
                    "Picnic layout data is missing. Capture the current scene layout first: " + LayoutPath);
            PicnicLayout layout = JsonUtility.FromJson<PicnicLayout>(asset.text);
            if (layout == null || layout.schemaVersion != 1 || layout.items == null ||
                layout.items.Length != ItemDefinitions.Length)
                throw new InvalidOperationException("Picnic layout data is invalid or unsupported: " + LayoutPath);
            for (int index = 0; index < ItemDefinitions.Length; index++)
                FindLayoutItem(layout, ItemDefinitions[index].SceneName);
            return layout;
        }

        private static PicnicItemTransform FindLayoutItem(PicnicLayout layout, string name)
        {
            for (int index = 0; index < layout.items.Length; index++)
                if (layout.items[index] != null && layout.items[index].name == name)
                    return layout.items[index];
            throw new InvalidOperationException("Picnic layout item is missing from generator data: " + name);
        }

        private static void ValidateLayoutMatchesSaved(GameObject root)
        {
            PicnicLayout layout = LoadLayout();
            for (int index = 0; index < ItemDefinitions.Length; index++)
            {
                PicnicItemDefinition definition = ItemDefinitions[index];
                PicnicItemTransform expected = FindLayoutItem(layout, definition.SceneName);
                Transform actual = root.transform.Find(definition.SceneName);
                Transform model = actual != null ? actual.Find("Model") : null;
                if (actual == null || model == null ||
                    Vector3.Distance(actual.position, expected.position) > 0.0005f ||
                    Quaternion.Angle(actual.rotation, expected.rotation) > 0.02f ||
                    Vector3.Distance(actual.localScale, expected.scale) > 0.0005f ||
                    Vector3.Distance(model.localPosition, expected.modelLocalPosition) > 0.0005f ||
                    Quaternion.Angle(model.localRotation, expected.modelLocalRotation) > 0.02f ||
                    Vector3.Distance(model.localScale, expected.modelLocalScale) > 0.0005f)
                    throw new InvalidOperationException(
                        definition.SceneName + " does not match the saved generator layout.");
            }
        }

        private static void SetStaticRecursively(GameObject root)
        {
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            for (int child = 0; child < root.transform.childCount; child++)
                SetStaticRecursively(root.transform.GetChild(child).gameObject);
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void ConfigureImportSettings()
        {
            for (int index = 0; index < ModelNames.Length; index++)
            {
                string path = AssetRoot + "/" + ModelNames[index] + ".fbx";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Picnic FBX importer is missing: " + path);
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = false;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.addCollider = false;
                importer.isReadable = ModelNames[index] == "picnic_blanket_blue";
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                importer.SaveAndReimport();
            }

            ConfigureTextureImporter(AtlasTexturePath);
            ConfigureTextureImporter(PlaidTexturePath);
        }

        private static void ConfigureTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Picnic texture importer is missing: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateMaterial(string path, Texture2D texture)
        {
            Shader shader = Shader.Find("StargazingHill/Environment");
            if (shader == null) throw new InvalidOperationException("StargazingHill/Environment shader is missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Ambient", 0.55f);
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetFloat("_UseTexture", 1f);
            material.SetFloat("_UseNormal", 0f);
            material.SetFloat("_UseAlphaMap", 0f);
            material.SetFloat("_UseVertexColor", 0f);
            material.SetFloat("_Cutoff", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required picnic asset is missing: " + path);
            return asset;
        }
    }
}
