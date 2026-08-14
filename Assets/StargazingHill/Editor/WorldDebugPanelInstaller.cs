using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

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
        // Kept only so old generated scenes can be cleaned up. The information panel now owns the
        // only debug ON/OFF control, so this standalone object must never be regenerated.
        private const string LegacyToggleObjectName = "VRDebugPanelToggle";
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

        // Dock placement immediately to the information panel's right. These values are derived from the
        // hand-adjusted information-panel pose plus a 0.30m edge gap, and are the only placement knobs.
        internal static readonly Vector3 PanelPosition = new Vector3(-0.842f, 1.45f, -25.342f);
        internal static readonly Vector3 PanelEuler = new Vector3(0f, 202.2865f, 0f);

        private static bool _installing;

        static WorldDebugPanelInstaller()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorApplication.delayCall += InstallIfTargetSceneIsAlreadyOpen;
        }

        [MenuItem("Stargazing Hill/Advanced/Generated Content/Rebuild Debug Panel...", false, 83)]
        public static void InstallOrRefreshMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Debug Panel",
                    "This replaces the generated VRDebugPanel. Manual changes inside it will be lost.",
                    "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Install(scene, true);
            EditorSceneManager.SaveScene(scene);
        }

        public static void InstallOrRefreshForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Install(scene, true);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
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
            if (scene.path == ScenePath &&
                (FindRoot(scene, PanelObjectName) == null || FindRoot(scene, LegacyToggleObjectName) != null))
            {
                Install(scene, false);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (_installing || Application.isPlaying || scene.path != ScenePath) return;
            if (FindRoot(scene, PanelObjectName) != null && FindRoot(scene, LegacyToggleObjectName) == null) return;

            Install(scene, false);
            _installing = true;
            try { EditorSceneManager.SaveScene(scene); }
            finally { _installing = false; }
        }

        private static void Install(Scene scene, bool refreshExisting)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");

            GameObject oldPanel = FindRoot(scene, PanelObjectName);
            GameObject oldToggle = FindRoot(scene, LegacyToggleObjectName);
            if (!refreshExisting && oldPanel != null)
            {
                if (oldToggle != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldToggle);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                return;
            }
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
            Font dynamicFont = WorldInformationPanelInstaller.EnsureFont();
            Material dynamicTextMaterial = WorldInformationPanelInstaller.EnsureTextMaterial();

            GameObject panel = new GameObject(PanelObjectName);
            panel.transform.position = PanelPosition;
            panel.transform.rotation = Quaternion.Euler(PanelEuler);
            panel.transform.localScale = Vector3.one * PanelScale;
            SceneManager.MoveGameObjectToScene(panel, scene);

            CreateThinBoard(panel.transform, boardMaterial);
            ConfigurePickup(panel);
            GameObject japaneseLabels = new GameObject("JapaneseLabels");
            japaneseLabels.transform.SetParent(panel.transform, false);
            GameObject englishLabels = new GameObject("EnglishLabels");
            englishLabels.transform.SetParent(panel.transform, false);
            englishLabels.SetActive(false);
            GameObject traditionalChineseLabels = new GameObject("TraditionalChineseLabels");
            traditionalChineseLabels.transform.SetParent(panel.transform, false);
            traditionalChineseLabels.SetActive(false);
            GameObject simplifiedChineseLabels = new GameObject("SimplifiedChineseLabels");
            simplifiedChineseLabels.transform.SetParent(panel.transform, false);
            simplifiedChineseLabels.SetActive(false);
            GameObject koreanLabels = new GameObject("KoreanLabels");
            koreanLabels.transform.SetParent(panel.transform, false);
            koreanLabels.SetActive(false);

            CreateUiText(japaneseLabels.transform, "流星デバッグ", new Vector3(-0.12f, 0.885f, -0.022f),
                0.100f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(englishLabels.transform, "METEOR DEBUG", new Vector3(-0.12f, 0.885f, -0.022f),
                0.100f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(traditionalChineseLabels.transform, "流星偵錯", new Vector3(-0.12f, 0.885f, -0.022f),
                0.100f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(simplifiedChineseLabels.transform, "流星调试", new Vector3(-0.12f, 0.885f, -0.022f),
                0.100f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(koreanLabels.transform, "유성 디버그", new Vector3(-0.12f, 0.885f, -0.022f),
                0.100f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(japaneseLabels.transform,
                "強制 " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "秒 / 現在を再生 " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "秒 / ローカルのみ",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            CreateUiText(englishLabels.transform,
                "FORCED " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "s / REPLAY CURRENT " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "s / LOCAL ONLY",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            CreateUiText(traditionalChineseLabels.transform,
                "強制 " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "秒 / 播放目前 " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "秒 / 僅限本機",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            CreateUiText(simplifiedChineseLabels.transform,
                "强制 " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "秒 / 播放当前 " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "秒 / 仅限本地",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            CreateUiText(koreanLabels.transform,
                "강제 " + MeteorController.DebugForcedPreviewDurationSeconds.ToString("0") +
                "초 / 현재 재생 " + MeteorController.NaturalEventDurationSeconds.ToString("0") + "초 / 로컬 전용",
                new Vector3(0f, 0.765f, -0.022f), 0.048f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            Text japaneseStatusText = CreateUiText(japaneseLabels.transform,
                "状態: 待機   流星群: -\n表示中: 0",
                new Vector3(0f, 0.655f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial, new Color(0.66f, 0.90f, 1f));
            Text englishStatusText = CreateUiText(englishLabels.transform,
                "EVENT: IDLE   SHOWER: -\nVISIBLE: 0",
                new Vector3(0f, 0.655f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial, new Color(0.66f, 0.90f, 1f));
            Text traditionalChineseStatusText = CreateUiText(traditionalChineseLabels.transform,
                "狀態: 待機   流星雨: -\n顯示中: 0",
                new Vector3(0f, 0.655f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial, new Color(0.66f, 0.90f, 1f));
            Text simplifiedChineseStatusText = CreateUiText(simplifiedChineseLabels.transform,
                "状态: 待机   流星雨: -\n显示中: 0",
                new Vector3(0f, 0.655f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial, new Color(0.66f, 0.90f, 1f));
            Text koreanStatusText = CreateUiText(koreanLabels.transform,
                "상태: 대기   유성우: -\n표시 중: 0",
                new Vector3(0f, 0.655f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial, new Color(0.66f, 0.90f, 1f));

            string[] labels =
            {
                "QUADRANTIDS", "LYRIDS", "ETA AQUARIIDS", "S DELTA AQUARIIDS",
                "PERSEIDS", "DRACONIDS", "ORIONIDS", "S TAURIDS",
                "N TAURIDS", "LEONIDS", "GEMINIDS"
            };
            string[] japaneseShowerLabels =
            {
                "しぶんぎ座", "こと座", "みずがめ座η", "みずがめ座δ南",
                "ペルセウス座", "りゅう座", "オリオン座", "おうし座南",
                "おうし座北", "しし座", "ふたご座"
            };
            string[] traditionalChineseShowerLabels =
            {
                "象限儀座", "天琴座", "寶瓶座η", "南寶瓶座δ",
                "英仙座", "天龍座", "獵戶座", "南金牛座",
                "北金牛座", "獅子座", "雙子座"
            };
            string[] simplifiedChineseShowerLabels =
            {
                "象限仪座", "天琴座", "宝瓶座η", "南宝瓶座δ",
                "英仙座", "天龙座", "猎户座", "南金牛座",
                "北金牛座", "狮子座", "双子座"
            };
            string[] koreanShowerLabels =
            {
                "사분의자리", "거문고자리", "물병자리 η", "남쪽 물병자리 δ",
                "페르세우스자리", "용자리", "오리온자리", "남쪽 황소자리",
                "북쪽 황소자리", "사자자리", "쌍둥이자리"
            };

            // Eleven showers over six two-column rows. The last row is half empty on purpose: it separates
            // the shower grid from the control rows below, which must not share a slot with a shower.
            const float startY = 0.505f;
            const float rowStep = 0.19f;
            Vector3 wideButton = new Vector3(ButtonWidth, ButtonHeight, 0.006f);
            for (int index = 0; index < labels.Length; index++)
            {
                float x = index % 2 == 0 ? -ColumnOffset : ColumnOffset;
                float y = startY - (index / 2) * rowStep;
                GameObject showerButton = CreateActionButton(panel.transform, labels[index], new Vector3(x, y, -0.012f),
                    wideButton, buttonMaterial,
                    WorldDebugPanelButton.ActionForcedShower, index, panel, meteor, sky);
                RemoveStaticLabel(showerButton);
                float labelHeight = FitLabelHeight(labels[index], wideButton.x * 0.88f, wideButton.y * 0.52f);
                CreateUiText(englishLabels.transform, labels[index], new Vector3(x, y, -0.022f),
                    labelHeight, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
                CreateUiText(japaneseLabels.transform, japaneseShowerLabels[index], new Vector3(x, y, -0.022f),
                    labelHeight, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
                CreateUiText(traditionalChineseLabels.transform, traditionalChineseShowerLabels[index],
                    new Vector3(x, y, -0.022f), labelHeight, TextAnchor.MiddleCenter,
                    dynamicFont, dynamicTextMaterial);
                CreateUiText(simplifiedChineseLabels.transform, simplifiedChineseShowerLabels[index],
                    new Vector3(x, y, -0.022f), labelHeight, TextAnchor.MiddleCenter,
                    dynamicFont, dynamicTextMaterial);
                CreateUiText(koreanLabels.transform, koreanShowerLabels[index], new Vector3(x, y, -0.022f),
                    labelHeight, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            }

            const float controlY = -0.675f;
            GameObject playStop = CreateActionButton(panel.transform, "PLAY CURRENT",
                new Vector3(0f, controlY, -0.012f), new Vector3(2.11f, ButtonHeight, 0.006f), dangerMaterial,
                WorldDebugPanelButton.ActionNaturalEvent, 0, panel, meteor, sky);
            RemoveStaticLabel(playStop);
            Text japanesePlayStopLabel = CreateUiText(japaneseLabels.transform,
                "現在を再生", new Vector3(0f, controlY, -0.022f), 0.070f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            Text englishPlayStopLabel = CreateUiText(englishLabels.transform,
                "PLAY CURRENT", new Vector3(0f, controlY, -0.022f), 0.070f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            Text traditionalChinesePlayStopLabel = CreateUiText(traditionalChineseLabels.transform,
                "播放目前", new Vector3(0f, controlY, -0.022f), 0.070f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            Text simplifiedChinesePlayStopLabel = CreateUiText(simplifiedChineseLabels.transform,
                "播放当前", new Vector3(0f, controlY, -0.022f), 0.070f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            Text koreanPlayStopLabel = CreateUiText(koreanLabels.transform,
                "현재 재생", new Vector3(0f, controlY, -0.022f), 0.070f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);

            WorldDebugPanelStatus status = UdonSharpUndo.AddComponent<WorldDebugPanelStatus>(panel);
            status.meteorController = meteor;
            status.japaneseStatusText = japaneseStatusText;
            status.englishStatusText = englishStatusText;
            status.traditionalChineseStatusText = traditionalChineseStatusText;
            status.simplifiedChineseStatusText = simplifiedChineseStatusText;
            status.koreanStatusText = koreanStatusText;
            status.japanesePlayStopLabel = japanesePlayStopLabel;
            status.englishPlayStopLabel = englishPlayStopLabel;
            status.traditionalChinesePlayStopLabel = traditionalChinesePlayStopLabel;
            status.simplifiedChinesePlayStopLabel = simplifiedChinesePlayStopLabel;
            status.koreanPlayStopLabel = koreanPlayStopLabel;

            const float skyY = -0.865f;
            Vector3 skyButton = new Vector3(0.66f, ButtonHeight, 0.006f);
            GameObject skyMinus = CreateActionButton(panel.transform, "SKY -1H", new Vector3(-0.72f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyMinusHour, 0, panel, meteor, sky);
            GameObject skyPlus = CreateActionButton(panel.transform, "SKY +1H", new Vector3(0f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyPlusHour, 0, panel, meteor, sky);
            GameObject skyReset = CreateActionButton(panel.transform, "SKY RESET", new Vector3(0.72f, skyY, -0.012f),
                skyButton, buttonMaterial,
                WorldDebugPanelButton.ActionSkyReset, 0, panel, meteor, sky);
            RemoveStaticLabel(skyMinus);
            RemoveStaticLabel(skyPlus);
            RemoveStaticLabel(skyReset);
            CreateUiText(englishLabels.transform, "SKY -1H", new Vector3(-0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(englishLabels.transform, "SKY +1H", new Vector3(0f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(englishLabels.transform, "SKY RESET", new Vector3(0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(japaneseLabels.transform, "空 -1時間", new Vector3(-0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(japaneseLabels.transform, "空 +1時間", new Vector3(0f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(japaneseLabels.transform, "空 リセット", new Vector3(0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(traditionalChineseLabels.transform, "天空 -1小時", new Vector3(-0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(traditionalChineseLabels.transform, "天空 +1小時", new Vector3(0f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(traditionalChineseLabels.transform, "天空 重設", new Vector3(0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(simplifiedChineseLabels.transform, "天空 -1小时", new Vector3(-0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(simplifiedChineseLabels.transform, "天空 +1小时", new Vector3(0f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(simplifiedChineseLabels.transform, "天空 重置", new Vector3(0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(koreanLabels.transform, "하늘 -1시간", new Vector3(-0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(koreanLabels.transform, "하늘 +1시간", new Vector3(0f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);
            CreateUiText(koreanLabels.transform, "하늘 초기화", new Vector3(0.72f, skyY, -0.022f),
                0.060f, TextAnchor.MiddleCenter, dynamicFont, dynamicTextMaterial);

            GameObject languageButton = CreatePrimitive("LanguageToggle", panel.transform, buttonMaterial,
                new Vector3(0.94f, 0.89f, -0.012f), Quaternion.identity,
                new Vector3(0.32f, 0.12f, 0.006f));
            BoxCollider languageCollider = languageButton.GetComponent<BoxCollider>();
            if (languageCollider != null) languageCollider.isTrigger = true;
            Text languageLabel = CreateUiText(panel.transform, "ENGLISH",
                new Vector3(0.94f, 0.89f, -0.022f), 0.050f, TextAnchor.MiddleCenter,
                dynamicFont, dynamicTextMaterial);
            WorldInfoLanguageToggle languageToggle = UdonSharpUndo.AddComponent<WorldInfoLanguageToggle>(languageButton);
            languageToggle.japaneseText = japaneseLabels;
            languageToggle.englishText = englishLabels;
            languageToggle.traditionalChineseText = traditionalChineseLabels;
            languageToggle.simplifiedChineseText = simplifiedChineseLabels;
            languageToggle.koreanText = koreanLabels;
            languageToggle.buttonLabel = languageLabel;
            UdonSharpEditorUtility.CopyProxyToUdon(languageToggle);
            UdonBehaviour languageBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(languageToggle);
            if (languageBacking != null)
            {
                languageBacking.InteractionText = "Language / 言語";
                languageBacking.proximity = 2.5f;
                EditorUtility.SetDirty(languageBacking);
            }
            EditorUtility.SetDirty(languageToggle);

            UdonSharpEditorUtility.CopyProxyToUdon(status);
            EditorUtility.SetDirty(status);

            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed handheld local VR debug panel beside the information panel " +
                      "(default OFF, information-panel toggle, pickup enabled, 10 second return).");
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

        private static GameObject CreateActionButton(
            Transform parent, string label, Vector3 localPosition, Vector3 localScale, Material material,
            int action, int showerIndex, GameObject panelRoot, MeteorController meteor, RealSkyController sky)
        {
            GameObject button = CreatePrimitive(label.Replace(" ", "_"), parent, material,
                localPosition, Quaternion.identity, localScale);
            ConfigureButton(button, action, showerIndex, panelRoot, meteor, sky, label);
            CreateText(button.transform, label, new Vector3(0f, 0f, -0.53f),
                FitLabelHeight(label, localScale.x * 0.88f, localScale.y * 0.52f),
                TextAnchor.MiddleCenter, true);
            return button;
        }

        private static void RemoveStaticLabel(GameObject button)
        {
            Transform label = button.transform.Find("Label");
            if (label != null) UnityEngine.Object.DestroyImmediate(label.gameObject);
        }

        private static Text CreateUiText(Transform parent, string value, Vector3 position, float height,
            TextAnchor anchor, Font font, Material material)
        {
            return CreateUiText(parent, value, position, height, anchor, font, material, Color.white);
        }

        private static Text CreateUiText(Transform parent, string value, Vector3 position, float height,
            TextAnchor anchor, Font font, Material material, Color color)
        {
            Text text = WorldInformationPanelInstaller.CreateText(parent, value, position, height,
                anchor, font, material, color);
            text.raycastTarget = false;
            return text;
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
