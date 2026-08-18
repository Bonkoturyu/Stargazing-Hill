using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace StargazingHill.Editor
{
    /// <summary>Installs the CC0 handheld compass used to orient viewers beneath the real sky.</summary>
    internal static class CompassSceneInstaller
    {
        private const string RootPath = "Assets/StargazingHill";
        private const string ModelPath = RootPath + "/ThirdParty/OpenGameArt/Compass/Compass.fbx";
        private const string AlbedoPath = RootPath + "/ThirdParty/OpenGameArt/Compass/Compass_Albedo.png";
        private const string NormalPath = RootPath + "/ThirdParty/OpenGameArt/Compass/Compass_Normal.png";
        private const string MaterialPath = RootPath + "/Generated/Materials/HandheldCompass.mat";
        private const string NorthMaterialPath = RootPath + "/Generated/Materials/CompassNeedleNorth.mat";
        private const string SouthMaterialPath = RootPath + "/Generated/Materials/CompassNeedleSouth.mat";
        private const string DialMaterialPath = RootPath + "/Generated/Materials/CompassDial.mat";
        private const string RimMaterialPath = RootPath + "/Generated/Materials/CompassRim.mat";
        private const float TargetModelDiameter = 0.18f;

        // Beside the radio/tea set, offset far enough from the teapot for a clean pickup silhouette.
        // Only the ground plan is authored: the model is normalized so its base sits at the root
        // origin, so the resting height is measured from whatever surface is actually underneath.
        private static readonly Vector2 CompassGroundPosition = new Vector2(7.52f, 7.48f);
        private const float CompassRestClearance = 0.002f;

        private static float ResolveRestingHeight(Transform ignoreRoot)
        {
            float terrain = StargazingWorldBuilder.EvaluateTerrainHeightForEditor(
                CompassGroundPosition.x, CompassGroundPosition.y);
            float surface = terrain;
            Physics.SyncTransforms();
            // The picnic mat sits above the terrain here, so take the highest solid hit rather
            // than the first one, and fall back to the analytic terrain when nothing is loaded.
            RaycastHit[] hits = Physics.RaycastAll(
                new Vector3(CompassGroundPosition.x, terrain + 3f, CompassGroundPosition.y),
                Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider hit = hits[index].collider;
                if (ignoreRoot != null && hit.transform.IsChildOf(ignoreRoot)) continue;
                if (hits[index].point.y > surface) surface = hits[index].point.y;
            }
            return surface + CompassRestClearance;
        }

        private static Vector3 ResolveCompassPosition(Transform ignoreRoot)
        {
            return new Vector3(CompassGroundPosition.x, ResolveRestingHeight(ignoreRoot),
                CompassGroundPosition.y);
        }

        internal static void InstallForBuild(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            Transform environment = GameObject.Find("World/Environment")?.transform;
            if (environment == null) throw new InvalidOperationException("World/Environment is missing.");

            Transform old = environment.Find("LocalNorthCompass");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            ConfigureImports();
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null) throw new InvalidOperationException("CC0 compass model is missing: " + ModelPath);

            Material bodyMaterial = EnsureEnvironmentMaterial();
            Material northMaterial = WorldSettingsSystemInstaller.EnsurePublicColorMaterial(
                NorthMaterialPath, new Color(0.95f, 0.10f, 0.12f, 1f));
            Material southMaterial = WorldSettingsSystemInstaller.EnsurePublicColorMaterial(
                SouthMaterialPath, new Color(0.88f, 0.92f, 1f, 1f));
            Material dialMaterial = WorldSettingsSystemInstaller.EnsurePublicColorMaterial(
                DialMaterialPath, new Color(0.018f, 0.035f, 0.060f, 1f));
            Material rimMaterial = WorldSettingsSystemInstaller.EnsurePublicColorMaterial(
                RimMaterialPath, new Color(0.72f, 0.86f, 0.91f, 1f));

            GameObject compass = new GameObject("LocalNorthCompass");
            compass.transform.SetParent(environment, false);
            compass.transform.position = ResolveCompassPosition(null);
            compass.transform.rotation = Quaternion.identity;

            GameObject model = PrefabUtility.InstantiatePrefab(source, compass.transform) as GameObject;
            if (model == null) throw new InvalidOperationException("CC0 compass model could not be instantiated.");
            model.name = "CC0CompassModel";
            NormalizeModel(model, bodyMaterial);
            CreateCompassDial(compass.transform, dialMaterial, rimMaterial, northMaterial);

            GameObject needlePivot = new GameObject("LocalNeedlePivot");
            needlePivot.transform.SetParent(compass.transform, false);
            needlePivot.transform.localPosition = new Vector3(0f, 0.083f, 0f);
            CreateNeedleHalf("NorthRed", needlePivot.transform, 0.032f, northMaterial);
            CreateNeedleHalf("SouthWhite", needlePivot.transform, -0.032f, southMaterial);
            CreateCylinder("NeedlePin", needlePivot.transform, new Vector3(0f, 0.004f, 0f),
                new Vector3(0.016f, 0.004f, 0.016f), rimMaterial);

            BoxCollider collider = compass.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.055f, 0f);
            collider.size = new Vector3(0.22f, 0.12f, 0.22f);
            Rigidbody body = compass.AddComponent<Rigidbody>();
            body.mass = 0.12f;
            body.drag = 1.5f;
            body.angularDrag = 2.5f;
            VRCPickup pickup = compass.AddComponent<VRCPickup>();
            pickup.pickupable = true;
            pickup.proximity = 0.4f;
            pickup.InteractionText = "方位磁石を持つ / Pick up compass";
            pickup.UseText = string.Empty;
            pickup.orientation = VRC_Pickup.PickupOrientation.Any;
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;
            VRCObjectSync objectSync = compass.AddComponent<VRCObjectSync>();

            LocalCompassNeedle needle = UdonSharpUndo.AddComponent<LocalCompassNeedle>(compass);
            needle.needlePivot = needlePivot.transform;
            needle.objectSync = objectSync;
            needle.pickupRigidbody = body;
            UdonSharpEditorUtility.CopyProxyToUdon(needle);
            EditorUtility.SetDirty(needle);

            EditorUtility.SetDirty(compass);
            Debug.Log("[Stargazing Hill] Installed local-north handheld compass from the recorded CC0 source.");
        }

        internal static void ValidateScene()
        {
            GameObject compass = GameObject.Find("World/Environment/LocalNorthCompass");
            LocalCompassNeedle needle = compass != null ? compass.GetComponent<LocalCompassNeedle>() : null;
            VRCPickup pickup = compass != null ? compass.GetComponent<VRCPickup>() : null;
            VRCObjectSync objectSync = compass != null ? compass.GetComponent<VRCObjectSync>() : null;
            Rigidbody body = compass != null ? compass.GetComponent<Rigidbody>() : null;
            if (compass == null || needle == null || needle.needlePivot == null ||
                pickup == null || objectSync == null ||
                body == null || compass.GetComponent<BoxCollider>() == null ||
                needle.objectSync != objectSync || needle.pickupRigidbody != body ||
                UdonSharpEditorUtility.GetBackingUdonBehaviour(needle) == null)
                throw new InvalidOperationException("Local handheld compass validation failed.");
            if (Mathf.Abs(pickup.proximity - 0.4f) > 0.001f || !string.IsNullOrEmpty(pickup.UseText))
                throw new InvalidOperationException("Local handheld compass pickup settings drifted.");
            if ((compass.transform.position - ResolveCompassPosition(compass.transform)).sqrMagnitude > 0.0001f)
                throw new InvalidOperationException(
                    "Local handheld compass dock drifted, or it no longer rests on the surface below it.");
            if (compass.transform.Find("CompassDial") == null ||
                compass.transform.Find("CompassDial/CardinalMarks") == null)
                throw new InvalidOperationException("Compass dial or cardinal marks are missing.");
        }

        private static void ConfigureImports()
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (modelImporter != null)
            {
                bool dirty = modelImporter.importAnimation || modelImporter.importCameras ||
                    modelImporter.importLights || modelImporter.addCollider ||
                    modelImporter.materialImportMode != ModelImporterMaterialImportMode.None;
                modelImporter.importAnimation = false;
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.addCollider = false;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                if (dirty) modelImporter.SaveAndReimport();
            }
            ConfigureTexture(AlbedoPath, false);
            ConfigureTexture(NormalPath, true);
        }

        private static void ConfigureTexture(string path, bool normalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material EnsureEnvironmentMaterial()
        {
            Shader shader = Shader.Find("StargazingHill/Environment");
            if (shader == null) throw new InvalidOperationException("Environment shader is missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.color = Color.white;
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.SetFloat("_UseTexture", 1f);
            material.SetFloat("_UseNormal", 1f);
            material.SetFloat("_UseVertexColor", 0f);
            material.SetFloat("_Ambient", 0.34f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void NormalizeModel(GameObject model, Material material)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("CC0 compass has no renderers.");
            Bounds worldBounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) worldBounds.Encapsulate(renderers[index].bounds);
            float sourceDiameter = Mathf.Max(worldBounds.size.x, Mathf.Max(worldBounds.size.y, worldBounds.size.z));
            if (sourceDiameter <= 0.0001f) throw new InvalidOperationException("CC0 compass bounds are invalid.");
            model.transform.localScale = Vector3.one * (TargetModelDiameter / sourceDiameter);

            renderers = model.GetComponentsInChildren<Renderer>(true);
            worldBounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) worldBounds.Encapsulate(renderers[index].bounds);
            Vector3 localCenter = model.transform.parent.InverseTransformPoint(worldBounds.center);
            float localBottom = model.transform.parent.InverseTransformPoint(worldBounds.min).y;
            model.transform.localPosition += new Vector3(-localCenter.x, -localBottom, -localCenter.z);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].sharedMaterial = material;
                renderers[index].shadowCastingMode = ShadowCastingMode.Off;
                renderers[index].receiveShadows = false;
            }
        }

        private static void CreateNeedleHalf(string name, Transform parent, float centerZ, Material material)
        {
            GameObject half = GameObject.CreatePrimitive(PrimitiveType.Cube);
            half.name = name;
            half.transform.SetParent(parent, false);
            half.transform.localPosition = new Vector3(0f, 0f, centerZ);
            half.transform.localScale = new Vector3(0.010f, 0.004f, 0.064f);
            half.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(half.GetComponent<Collider>());
        }

        private static void CreateCompassDial(Transform parent, Material dialMaterial,
            Material rimMaterial, Material northMaterial)
        {
            GameObject dial = new GameObject("CompassDial");
            dial.transform.SetParent(parent, false);
            CreateCylinder("OuterRim", dial.transform, new Vector3(0f, 0.058f, 0f),
                new Vector3(0.205f, 0.006f, 0.205f), rimMaterial);
            CreateCylinder("DialFace", dial.transform, new Vector3(0f, 0.066f, 0f),
                new Vector3(0.184f, 0.004f, 0.184f), dialMaterial);

            GameObject ticks = new GameObject("TickMarks");
            ticks.transform.SetParent(dial.transform, false);
            for (int index = 0; index < 24; index++)
            {
                float angle = index * 15f;
                float radians = angle * Mathf.Deg2Rad;
                bool cardinal = index % 6 == 0;
                GameObject tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tick.name = "Tick_" + index.ToString("00");
                tick.transform.SetParent(ticks.transform, false);
                tick.transform.localPosition = new Vector3(Mathf.Sin(radians) * 0.076f, 0.073f,
                    Mathf.Cos(radians) * 0.076f);
                tick.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                tick.transform.localScale = new Vector3(cardinal ? 0.006f : 0.003f, 0.003f,
                    cardinal ? 0.020f : 0.012f);
                tick.GetComponent<Renderer>().sharedMaterial = cardinal && index == 0
                    ? northMaterial
                    : rimMaterial;
                UnityEngine.Object.DestroyImmediate(tick.GetComponent<Collider>());
            }

            GameObject marks = new GameObject("CardinalMarks");
            marks.transform.SetParent(dial.transform, false);
            Font font = WorldInformationPanelInstaller.EnsureFont();
            Material textMaterial = WorldInformationPanelInstaller.EnsureTextMaterial();
            CreateCardinalText(marks.transform, "N", new Vector3(0f, 0.079f, 0.052f),
                font, textMaterial, new Color(1f, 0.24f, 0.24f));
            CreateCardinalText(marks.transform, "E", new Vector3(0.052f, 0.079f, 0f),
                font, textMaterial, Color.white);
            CreateCardinalText(marks.transform, "S", new Vector3(0f, 0.079f, -0.052f),
                font, textMaterial, Color.white);
            CreateCardinalText(marks.transform, "W", new Vector3(-0.052f, 0.079f, 0f),
                font, textMaterial, Color.white);
        }

        private static void CreateCardinalText(Transform parent, string value, Vector3 position,
            Font font, Material material, Color color)
        {
            UnityEngine.UI.Text text = WorldInformationPanelInstaller.CreateText(parent, value, position,
                0.020f, UnityEngine.TextAnchor.MiddleCenter, font, material, color, false);
            text.transform.parent.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void CreateCylinder(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        }
    }
}
