using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Installs the local-only VR debug panel into the normal StargazingHill scene.
    /// It deliberately hooks sceneSaved so StargazingWorldBuilder can keep rebuilding the scene
    /// without needing a second debug-only scene or a direct dependency on this installer.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldDebugPanelInstaller
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string PanelObjectName = "VRDebugPanel";
        private const string ToggleObjectName = "VRDebugPanelToggle";
        internal const float PanelScale = 0.20f;

        // TextMesh renders a line at characterSize * fontSize / 10 world units, so a metre-based layout has
        // to convert rather than assign metres straight to characterSize. The first version did not, and
        // every label came out 6.4x too large and spilled off the board.
        private const int LabelFontSize = 64;
        private const float MetresToCharacterSize = 10f / LabelFontSize;
        // Upper bound on Arial uppercase advance as a fraction of the nominal line height. Used to size a
        // label before its mesh exists; StargazingWorldBuilder re-checks the generated mesh for real.
        private const float AdvancePerCharacter = 0.68f;

        // Layout stays in authoring units and the pickup root scales it to a 0.47 x 0.41m handheld tablet.
        // Panel face is 2.35 x 2.05, so usable half-extents are 1.175 / 1.025 minus a small margin.
        private const float ButtonWidth = 1.02f;
        private const float ButtonHeight = 0.15f;
        private const float ColumnOffset = 0.545f;

        // Dock placement near YamaPlayer / QvPen / UnyStylus. Keep these as the only placement knobs.
        private static readonly Vector3 PanelPosition = new Vector3(-9.65f, 1.42f, -22.05f);
        private static readonly Vector3 PanelEuler = new Vector3(0f, 230f, 0f);
        private static readonly Vector3 ToggleEuler = PanelEuler;
        private static readonly Vector3 TogglePosition = PanelPosition +
            Quaternion.Euler(PanelEuler) * new Vector3(-0.42f, -0.16f, 0f);

        private static bool _installing;

        static WorldDebugPanelInstaller()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorApplication.delayCall += InstallIfTargetSceneIsAlreadyOpen;
        }

        [MenuItem("Stargazing Hill/Debug/Install or Refresh VR Debug Panel", false, 47)]
        public static void InstallOrRefreshMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Install(scene, true);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Installs into a scene the world builder is still assembling. The sceneSaved hook cannot serve the
        /// build: the builder validates before it saves, so a save-triggered install would always arrive too
        /// late for validation and the panel would only appear on a later save.
        /// </summary>
        internal static void InstallForBuild(Scene scene)
        {
            Install(scene, true);
        }

        private static void InstallIfTargetSceneIsAlreadyOpen()
        {
            if (Application.isPlaying) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath && FindRoot(scene, PanelObjectName) == null)
            {
                Install(scene, false);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (_installing || Application.isPlaying || scene.path != ScenePath) return;
            if (FindRoot(scene, PanelObjectName) != null && FindRoot(scene, ToggleObjectName) != null) return;

            Install(scene, false);
            _installing = true;
            try { EditorSceneManager.SaveScene(scene); }
            finally { _installing = false; }
        }

        private static void Install(Scene scene, bool refreshExisting)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");

            GameObject oldPanel = FindRoot(scene, PanelObjectName);
            GameObject oldToggle = FindRoot(scene, ToggleObjectName);
            if (!refreshExisting && oldPanel != null && oldToggle != null) return;
            if (oldPanel != null) UnityEngine.Object.DestroyImmediate(oldPanel);
            if (oldToggle != null) UnityEngine.Object.DestroyImmediate(oldToggle);

            MeteorController meteor = UnityEngine.Object.FindObjectOfType<MeteorController>(true);
            RealSkyController sky = UnityEngine.Object.FindObjectOfType<RealSkyController>(true);
            if (meteor == null || sky == null)
            {
                Debug.LogWarning("[Stargazing Hill] VR debug panel install skipped: MeteorController / RealSkyController not found.");
                return;
            }

            // UdonSharpUndo.AddComponent throws a NullReferenceException when the behaviour has no compiled
            // U# program asset, and it throws after the panel root already exists, which strands a partial
            // panel in the open scene. Ensure the asset before anything is created.
            StargazingWorldBuilder.EnsureDebugPanelProgramAssets();

            Material boardMaterial = EnsureColorMaterial(
                "Assets/StargazingHill/Generated/Materials/VRDebugPanel.mat",
                new Color(0.025f, 0.032f, 0.052f, 1f));
            Material buttonMaterial = EnsureColorMaterial(
                "Assets/StargazingHill/Generated/Materials/VRDebugButton.mat",
                new Color(0.10f, 0.17f, 0.25f, 1f));
            Material dangerMaterial = EnsureColorMaterial(
                "Assets/StargazingHill/Generated/Materials/VRDebugStop.mat",
                new Color(0.28f, 0.08f, 0.08f, 1f));

            GameObject panel = new GameObject(PanelObjectName);
            panel.transform.position = PanelPosition;
            panel.transform.rotation = Quaternion.Euler(PanelEuler);
            panel.transform.localScale = Vector3.one * PanelScale;
            SceneManager.MoveGameObjectToScene(panel, scene);

            CreateThinBoard(panel.transform, boardMaterial);
            ConfigurePickup(panel);
            CreateText(panel.transform, "METEOR DEBUG", new Vector3(0f, 0.885f, -0.022f), 0.100f, TextAnchor.MiddleCenter);
            CreateText(panel.transform, "FORCED " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "s / REPLAY CURRENT " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "s / LOCAL ONLY",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter);

            string[] labels =
            {
                "QUADRANTIDS", "LYRIDS", "ETA AQUARIIDS", "S DELTA AQUARIIDS",
                "PERSEIDS", "DRACONIDS", "ORIONIDS", "S TAURIDS",
                "N TAURIDS", "LEONIDS", "GEMINIDS"
            };

            // Eleven showers over six two-column rows. The last row is half empty on purpose: it separates
            // the shower grid from the control rows below, which must not share a slot with a shower.
            const float startY = 0.605f;
            const float rowStep = 0.19f;
            Vector3 wideButton = new Vector3(ButtonWidth, ButtonHeight, 0.006f);
            for (int index = 0; index < labels.Length; index++)
            {
                float x = index % 2 == 0 ? -ColumnOffset : ColumnOffset;
                float y = startY - (index / 2) * rowStep;
                CreateActionButton(panel.transform, labels[index], new Vector3(x, y, -0.012f),
                    wideButton, buttonMaterial,
                    WorldDebugPanelButton.ActionForcedShower, index, panel, meteor, sky);
            }

            const float controlY = -0.585f;
            CreateActionButton(panel.transform, FormatNaturalReplayLabel(), new Vector3(ColumnOffset, controlY, -0.012f),
                wideButton, buttonMaterial,
                WorldDebugPanelButton.ActionNaturalEvent, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "STOP", new Vector3(-ColumnOffset, controlY, -0.012f),
                wideButton, dangerMaterial,
                WorldDebugPanelButton.ActionStopMeteor, 0, panel, meteor, sky);

            const float skyY = -0.795f;
            Vector3 skyButton = new Vector3(0.66f, ButtonHeight, 0.006f);
            CreateActionButton(panel.transform, "SKY -1H", new Vector3(-0.72f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyMinusHour, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "SKY +1H", new Vector3(0f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyPlusHour, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "SKY RESET", new Vector3(0.72f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyReset, 0, panel, meteor, sky);

            // Toggle stays outside panelRoot so it remains usable while the panel is hidden.
            GameObject toggle = CreatePrimitive(ToggleObjectName, null, buttonMaterial,
                TogglePosition, Quaternion.Euler(ToggleEuler), new Vector3(0.30f, 0.10f, 0.018f));
            SceneManager.MoveGameObjectToScene(toggle, scene);
            ConfigureButton(toggle, WorldDebugPanelButton.ActionTogglePanel, 0, panel, meteor, sky,
                "Toggle meteor debug panel");
            CreateText(toggle.transform, "DEBUG ON / OFF", new Vector3(0f, 0f, -0.53f),
                FitLabelHeight("DEBUG ON / OFF", 0.30f * 0.88f, 0.10f * 0.52f),
                TextAnchor.MiddleCenter, true);

            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed handheld local VR debug panel near the amenity cluster " +
                      "(default OFF, pickup enabled, 10 second return).");
        }

        private static string FormatNaturalReplayLabel()
        {
            float seconds = MeteorController.NaturalEventDurationSeconds;
            if (Mathf.Approximately(seconds % 60f, 0f))
                return "REPLAY CURRENT " + (seconds / 60f).ToString("0") + " MIN";
            return "REPLAY CURRENT " + seconds.ToString("0") + " SEC";
        }

        private static void ConfigurePickup(GameObject panel)
        {
            int pickupLayer = LayerMask.NameToLayer("Pickup");
            if (pickupLayer < 0) throw new InvalidOperationException("VRChat Pickup layer is missing.");
            panel.layer = pickupLayer;

            BoxCollider collider = panel.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.35f, 2.05f, 0.12f);
            collider.center = new Vector3(0f, 0f, 0.08f);
            collider.isTrigger = true;

            Rigidbody body = panel.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.drag = 8f;
            body.angularDrag = 8f;

            VRCPickup pickup = panel.AddComponent<VRCPickup>();
            pickup.pickupable = true;
            pickup.proximity = 0.75f;
            pickup.InteractionText = "Grab Meteor Debug Panel";
            pickup.UseText = "Use Debug Controls";
            pickup.orientation = VRC_Pickup.PickupOrientation.Any;
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.No;

            WorldDebugPanelPickup pickupReturn = UdonSharpUndo.AddComponent<WorldDebugPanelPickup>(panel);
            pickupReturn.pickupCollider = collider;
            pickupReturn.pickupRigidbody = body;
            UdonSharpEditorUtility.CopyProxyToUdon(pickupReturn);
            EditorUtility.SetDirty(pickupReturn);
        }

        private static void CreateThinBoard(Transform parent, Material material)
        {
            GameObject board = CreatePrimitive("PanelSheet", parent, material, Vector3.zero, Quaternion.identity,
                new Vector3(2.35f, 2.05f, 0.018f));
            Collider collider = board.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void CreateActionButton(
            Transform parent, string label, Vector3 localPosition, Vector3 localScale, Material material,
            int action, int showerIndex, GameObject panelRoot, MeteorController meteor, RealSkyController sky)
        {
            GameObject button = CreatePrimitive(label.Replace(" ", "_"), parent, material,
                localPosition, Quaternion.identity, localScale);
            ConfigureButton(button, action, showerIndex, panelRoot, meteor, sky, label);
            CreateText(button.transform, label, new Vector3(0f, 0f, -0.53f),
                FitLabelHeight(label, localScale.x * 0.88f, localScale.y * 0.52f),
                TextAnchor.MiddleCenter, true);
        }

        /// <summary>
        /// Largest metre height at which the label still fits inside the button face. Long shower names such
        /// as "S DELTA AQUARIIDS" are what drive this down; short ones just take the height limit.
        /// </summary>
        private static float FitLabelHeight(string label, float widthLimit, float heightLimit)
        {
            if (string.IsNullOrEmpty(label)) return heightLimit;
            return Mathf.Min(heightLimit, widthLimit / (label.Length * AdvancePerCharacter));
        }

        private static void ConfigureButton(
            GameObject button, int action, int showerIndex, GameObject panelRoot,
            MeteorController meteor, RealSkyController sky, string interactionText)
        {
            BoxCollider collider = button.GetComponent<BoxCollider>();
            if (collider != null) collider.isTrigger = true;

            WorldDebugPanelButton behaviour = UdonSharpUndo.AddComponent<WorldDebugPanelButton>(button);
            behaviour.action = action;
            behaviour.showerIndex = showerIndex;
            behaviour.panelRoot = panelRoot;
            behaviour.meteorController = meteor;
            behaviour.skyController = sky;
            behaviour.visualRenderer = button.GetComponent<Renderer>();
            UdonSharpEditorUtility.CopyProxyToUdon(behaviour);

            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
            if (backing != null)
            {
                backing.InteractionText = interactionText;
                // VRC_Interactable.Proximity is read only in SDK 3.10.4; the serialized field behind it is
                // UdonBehaviour.proximity, which is what ClientSim and the client both read.
                backing.proximity = 2.5f;
                EditorUtility.SetDirty(backing);
            }
            EditorUtility.SetDirty(behaviour);
        }

        private static GameObject CreatePrimitive(
            string name, Transform parent, Material material, Vector3 position,
            Quaternion rotation, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
                go.transform.localRotation = rotation;
                go.transform.localScale = scale;
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
                go.transform.localScale = scale;
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }

        /// <param name="heightMetres">Nominal line height in world metres, converted to characterSize here.</param>
        private static TextMesh CreateText(
            Transform parent, string text, Vector3 localPosition, float heightMetres,
            TextAnchor anchor, bool compensateParentScale = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // TextMesh reads from its own -Z, and every label sits just in front of the board on the panel's
            // -Z face, so the label must keep the panel's orientation. A 180 degree flip here put the
            // readable side behind the board and rendered every label mirrored.
            go.transform.localRotation = Quaternion.identity;
            if (compensateParentScale)
            {
                Vector3 s = parent.localScale;
                go.transform.localScale = new Vector3(
                    Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x,
                    Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y,
                    1f);
            }

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = heightMetres * MetresToCharacterSize;
            mesh.fontSize = LabelFontSize;
            mesh.color = new Color(0.88f, 0.94f, 1f, 1f);
            return mesh;
        }

        private static Material EnsureColorMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                if (roots[index].name == name) return roots[index];
            return null;
        }
    }
}
