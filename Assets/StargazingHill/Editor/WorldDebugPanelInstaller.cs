using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        // Temporary placement near YamaPlayer / QvPen / UnyStylus. Keep these as the only placement knobs.
        private static readonly Vector3 PanelPosition = new Vector3(-9.65f, 1.42f, -22.05f);
        private static readonly Vector3 PanelEuler = new Vector3(0f, 230f, 0f);
        private static readonly Vector3 TogglePosition = new Vector3(-8.75f, 1.03f, -21.80f);
        private static readonly Vector3 ToggleEuler = new Vector3(0f, 230f, 0f);

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
            SceneManager.MoveGameObjectToScene(panel, scene);

            CreateThinBoard(panel.transform, boardMaterial);
            CreateText(panel.transform, "METEOR DEBUG", new Vector3(0f, 0.67f, -0.022f), 0.085f, TextAnchor.MiddleCenter);
            CreateText(panel.transform, "FORCED 25s / REPLAY CURRENT 3min / LOCAL ONLY",
                new Vector3(0f, 0.54f, -0.022f), 0.036f, TextAnchor.MiddleCenter);

            string[] labels =
            {
                "QUADRANTIDS", "LYRIDS", "ETA AQUARIIDS", "S DELTA AQUARIIDS",
                "PERSEIDS", "DRACONIDS", "ORIONIDS", "S TAURIDS",
                "N TAURIDS", "LEONIDS", "GEMINIDS"
            };

            const float startY = 0.37f;
            const float rowStep = 0.205f;
            for (int index = 0; index < labels.Length; index++)
            {
                int column = index % 2;
                int row = index / 2;
                float x = column == 0 ? -0.57f : 0.57f;
                float y = startY - row * rowStep;
                CreateActionButton(panel.transform, labels[index], new Vector3(x, y, -0.012f),
                    new Vector3(1.00f, 0.145f, 0.006f), buttonMaterial,
                    WorldDebugPanelButton.ActionForcedShower, index, panel, meteor, sky);
            }

            CreateActionButton(panel.transform, "REPLAY CURRENT 3 MIN", new Vector3(0.57f, -0.655f, -0.012f),
                new Vector3(1.00f, 0.145f, 0.006f), buttonMaterial,
                WorldDebugPanelButton.ActionNaturalEvent, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "STOP", new Vector3(-0.57f, -0.655f, -0.012f),
                new Vector3(1.00f, 0.145f, 0.006f), dangerMaterial,
                WorldDebugPanelButton.ActionStopMeteor, 0, panel, meteor, sky);

            CreateActionButton(panel.transform, "SKY -1H", new Vector3(-0.76f, -0.86f, -0.012f),
                new Vector3(0.64f, 0.145f, 0.006f), buttonMaterial,
                WorldDebugPanelButton.ActionSkyMinusHour, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "SKY +1H", new Vector3(0f, -0.86f, -0.012f),
                new Vector3(0.64f, 0.145f, 0.006f), buttonMaterial,
                WorldDebugPanelButton.ActionSkyPlusHour, 0, panel, meteor, sky);
            CreateActionButton(panel.transform, "SKY RESET", new Vector3(0.76f, -0.86f, -0.012f),
                new Vector3(0.64f, 0.145f, 0.006f), buttonMaterial,
                WorldDebugPanelButton.ActionSkyReset, 0, panel, meteor, sky);

            // Toggle stays outside panelRoot so it remains usable while the panel is hidden.
            GameObject toggle = CreatePrimitive(ToggleObjectName, null, buttonMaterial,
                TogglePosition, Quaternion.Euler(ToggleEuler), new Vector3(0.78f, 0.22f, 0.018f));
            SceneManager.MoveGameObjectToScene(toggle, scene);
            ConfigureButton(toggle, WorldDebugPanelButton.ActionTogglePanel, 0, panel, meteor, sky,
                "Toggle meteor debug panel");
            CreateText(toggle.transform, "DEBUG ON / OFF", new Vector3(0f, 0f, -0.53f), 0.055f,
                TextAnchor.MiddleCenter, true);

            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed flat local VR debug panel near the amenity cluster (default OFF, color feedback enabled).");
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
                label.Length > 14 ? 0.032f : 0.043f, TextAnchor.MiddleCenter, true);
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

        private static TextMesh CreateText(
            Transform parent, string text, Vector3 localPosition, float characterSize,
            TextAnchor anchor, bool compensateParentScale = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
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
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
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
