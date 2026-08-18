using System;
using System.Collections.Generic;
using System.IO;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;
using VRC.Udon;
using Yamadev.YamaStream;

namespace StargazingHill.Editor
{
    /// <summary>Builds the local comfort settings board and its tree/picnic integrations.</summary>
    public static class WorldSettingsSystemInstaller
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string RootPath = "Assets/StargazingHill";
        private const string AlarmPath = RootPath + "/Generated/Audio/SettingsAlarm.wav";
        private const string JoinChimePath = RootPath + "/Generated/Audio/PresenceJoinChime.wav";
        private const string LeaveChimePath = RootPath + "/Generated/Audio/PresenceLeaveChime.wav";
        private const string GearIconMeshPath = RootPath + "/Generated/Meshes/TreeSettingsGearIcon.asset";
        private const float CanvasScale = 0.002f;
        private const float BoardScale = 0.18f;
        // The four upright mirrors stand on the ground just outside the picnic mat edge.
        private const float MirrorEdgeMargin = 0.06f;
        private const float MirrorGroundClearance = 0.02f;
        private const float MirrorHeight = 2.15f;
        private const float MirrorCeilingHeight = 2.55f;
        // Docked beside the tree base and facing the picnic blanket.  Text and controls render
        // toward local -Z, so this yaw intentionally points -Z toward the blanket.
        private static readonly Vector3 BoardEuler = new Vector3(0f, 54f, 0f);
        // Captured from the user's hand-adjusted saved scene.  Keep the complete transform as the
        // generator baseline; the small visible gear gets a separately compensated hit target.
        private const float TreeToggleScale = 0.46967f;
        private static readonly Vector3 TreeTogglePosition = new Vector3(8.053f, 2.331f, 7.59f);
        private static readonly Quaternion TreeToggleRotation = new Quaternion(
            0.627459f, 0.33232313f, -0.35606158f, 0.6075168f);
        // Keep the board at its established dock beside the tree. The toggle transform above is the
        // user-adjusted source of truth and is intentionally independent from this board position.
        private static readonly Vector3 BoardPosition = new Vector3(8.716f, 2.53f, 7.169f);

        [MenuItem("Stargazing Hill/Content/Settings Board/Rebuild...", false, 40)]
        public static void InstallMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Settings Board",
                    "This replaces World/SettingsSystem and the generated local radio speaker.",
                    "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StargazingWorldBuilder.EnsureSettingsSystemProgramAssets();
            UdonSharpCompilerV1.CompileSync();
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void InstallForBatchMode()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StargazingWorldBuilder.EnsureSettingsSystemProgramAssets();
            UdonSharpCompilerV1.CompileSync();
            // The feature set also owns the blanket collision boundary. Rebuild it from the
            // captured layout in this unattended entry point so CI-style validation starts
            // from the same deterministic scene state as the full world builder.
            PicnicSceneInstaller.InstallForBuild(scene);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            PicnicSceneInstaller.ValidateScene();
            ValidateScene(scene);
            WorldInformationPanelInstaller.ValidateScene(scene);
        }

        internal static void InstallForBuild(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            Transform world = GameObject.Find("World")?.transform;
            if (world == null) throw new InvalidOperationException("World root is missing.");

            Transform old = world.Find("SettingsSystem");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            RemoveGeneratedRadioComponents();
            EnsureEventSystem(scene);
            CompassSceneInstaller.InstallForBuild(scene);

            Font font = WorldInformationPanelInstaller.EnsureFont();
            Material textMaterial = WorldInformationPanelInstaller.EnsureTextMaterial();
            Material boardMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/SettingsBoard.mat",
                new Color(0.018f, 0.028f, 0.050f, 1f));
            Material buttonMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/SettingsButton.mat",
                new Color(0.08f, 0.17f, 0.28f, 1f));
            Material accentMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/SettingsAccent.mat",
                new Color(0.58f, 0.90f, 1f, 1f));
            Material mirrorMaterial = EnsureMirrorMaterial();
            Material nightMaterial = EnsureNightMaterial();
            AudioClip alarmClip = EnsureAlarmClip();

            GameObject system = new GameObject("SettingsSystem");
            system.transform.SetParent(world, false);
            WorldSettingsController controller = UdonSharpUndo.AddComponent<WorldSettingsController>(system);

            GameObject board = new GameObject("LocalSettingsBoard");
            board.transform.SetParent(system.transform, false);
            board.transform.position = BoardPosition;
            board.transform.rotation = Quaternion.Euler(BoardEuler);
            board.transform.localScale = Vector3.one * BoardScale;

            GameObject sheet = CreateCube("PanelSheet", board.transform, new Vector3(0f, 0f, 0f),
                new Vector3(2.65f, 2.25f, 0.045f), boardMaterial, false);

            BoxCollider pickupCollider = board.AddComponent<BoxCollider>();
            // Keep pickup handling on a narrow top grip so it does not steal the UI ray
            // from buttons and the slider across the entire face of the board.
            pickupCollider.center = new Vector3(0f, 1.03f, 0.05f);
            pickupCollider.size = new Vector3(2.72f, 0.22f, 0.16f);
            pickupCollider.isTrigger = true;
            Rigidbody body = board.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.drag = 8f;
            body.angularDrag = 8f;
            VRCPickup pickup = board.AddComponent<VRCPickup>();
            pickup.pickupable = true;
            pickup.proximity = 0.35f;
            pickup.InteractionText = "設定ボードを持つ / Grab settings board";
            pickup.UseText = string.Empty;
            pickup.orientation = VRC_Pickup.PickupOrientation.Any;
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.No;
            WorldSettingsBoardPickup pickupReturn = UdonSharpUndo.AddComponent<WorldSettingsBoardPickup>(board);
            pickupReturn.pickupCollider = pickupCollider;
            pickupReturn.pickupRigidbody = body;
            UdonSharpEditorUtility.CopyProxyToUdon(pickupReturn);
            EditorUtility.SetDirty(pickupReturn);

            Text title = CreateText(board.transform, "ローカル設定",
                new Vector3(-0.20f, 0.98f, -0.028f), 0.090f, TextAnchor.UpperCenter,
                font, textMaterial, new Color(0.78f, 0.90f, 1f));
            title.transform.parent.name = "TitleCanvas";
            GameObject languageButton = CreateButton(board.transform, "LanguageToggle", "日→EN",
                new Vector3(1.00f, 0.94f, -0.0125f), new Vector3(0.48f, 0.19f, 0.019f),
                0.050f, buttonMaterial, font, textMaterial, controller,
                WorldSettingsButton.LanguageToggle, 0, "Language / 言語");
            Text languageButtonText = GetButtonLabel(languageButton);
            Text clock = CreateText(board.transform, "0000-00-00  00:00:00  LOCAL",
                new Vector3(-0.20f, 0.76f, -0.028f), 0.064f, TextAnchor.UpperCenter,
                font, textMaterial, Color.white);
            clock.transform.parent.name = "ClockCanvas";

            CreateCube("HeaderDivider", board.transform, new Vector3(0f, 0.64f, -0.026f),
                new Vector3(2.35f, 0.012f, 0.012f), accentMaterial, false);
            CreateCube("ColumnDivider", board.transform, new Vector3(0.06f, -0.17f, -0.026f),
                new Vector3(0.012f, 1.48f, 0.012f), accentMaterial, false);

            Text mirrorState = CreateText(board.transform, "ミラー（HQは高負荷）  ON 0 / 5  LQ",
                new Vector3(-1.18f, 0.54f, -0.028f), 0.046f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            mirrorState.transform.parent.name = "MirrorStateCanvas";
            // One ON/OFF toggle per direction in a two-column grid, then a clear-all button and a
            // single LQ/HQ switch that applies to whichever mirrors are on.
            string[] mirrorDirections = { "上", "下", "左", "右", "天井" };
            string[] mirrorNames = { "Up", "Down", "Left", "Right", "Ceiling" };
            Text[] mirrorButtonTexts = new Text[mirrorDirections.Length + 2];
            for (int mirrorIndex = 0; mirrorIndex < mirrorDirections.Length; mirrorIndex++)
            {
                float x = mirrorIndex % 2 == 0 ? -0.90f : -0.14f;
                float y = 0.36f - (mirrorIndex / 2) * 0.19f;
                GameObject mirrorButton = CreateButton(board.transform,
                    "Mirror_" + mirrorNames[mirrorIndex], mirrorDirections[mirrorIndex] + "  OFF",
                    new Vector3(x, y, -0.0125f), new Vector3(0.70f, 0.16f, 0.019f),
                    0.045f, buttonMaterial, font, textMaterial, controller,
                    WorldSettingsButton.Mirror, mirrorIndex,
                    "Mirror " + mirrorNames[mirrorIndex] + " ON/OFF");
                mirrorButtonTexts[mirrorIndex] = GetButtonLabel(mirrorButton);
            }
            mirrorButtonTexts[5] = GetButtonLabel(CreateButton(board.transform, "MirrorsAllOff", "すべてOFF",
                new Vector3(-0.14f, -0.02f, -0.0125f), new Vector3(0.70f, 0.16f, 0.019f),
                0.045f, buttonMaterial, font, textMaterial, controller,
                WorldSettingsButton.MirrorsOff, 0, "All mirrors OFF"));
            mirrorButtonTexts[6] = GetButtonLabel(CreateButton(board.transform, "MirrorQualityToggle", "画質  LQ",
                new Vector3(-0.52f, -0.21f, -0.0125f), new Vector3(1.46f, 0.16f, 0.019f),
                0.045f, buttonMaterial, font, textMaterial, controller,
                WorldSettingsButton.MirrorQuality, 0, "Mirror quality LQ/HQ"));

            Text nightModeText = CreateText(board.transform, "ナイトモード",
                new Vector3(-1.18f, -0.54f, -0.028f), 0.052f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            Slider slider = CreateNightSlider(board.transform, textMaterial, accentMaterial);
            Text[] actionButtonTexts = new Text[10];

            Text notifyState = CreateText(board.transform, "入退室通知  音 ON / 表示 ON",
                new Vector3(-1.18f, -0.90f, -0.028f), 0.042f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            notifyState.transform.parent.name = "NotifyStateCanvas";
            actionButtonTexts[8] = GetButtonLabel(CreateButton(board.transform, "NotifySoundToggle",
                "通知音 ON/OFF", new Vector3(-0.95f, -1.06f, -0.0125f),
                new Vector3(0.60f, 0.16f, 0.019f), 0.038f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.NotifySoundToggle, 0, "Join/leave sound ON/OFF"));
            actionButtonTexts[9] = GetButtonLabel(CreateButton(board.transform, "NotifyDisplayToggle",
                "入退室表示 ON/OFF", new Vector3(-0.30f, -1.06f, -0.0125f),
                new Vector3(0.60f, 0.16f, 0.019f), 0.034f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.NotifyDisplayToggle, 0, "Join/leave toast ON/OFF"));

            Text alarmState = CreateText(board.transform, "ALARM  22:00  OFF",
                new Vector3(0.15f, 0.52f, -0.028f), 0.060f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            alarmState.transform.parent.name = "AlarmStateCanvas";
            actionButtonTexts[0] = GetButtonLabel(CreateButton(board.transform, "AlarmHourDown", "時−", new Vector3(0.35f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmHour, -1, "Alarm hour -1"));
            actionButtonTexts[1] = GetButtonLabel(CreateButton(board.transform, "AlarmHourUp", "時＋", new Vector3(0.75f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmHour, 1, "Alarm hour +1"));
            actionButtonTexts[2] = GetButtonLabel(CreateButton(board.transform, "AlarmMinuteDown", "分−", new Vector3(0.35f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmMinute, -5, "Alarm minute -5"));
            actionButtonTexts[3] = GetButtonLabel(CreateButton(board.transform, "AlarmMinuteUp", "分＋", new Vector3(0.75f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmMinute, 5, "Alarm minute +5"));
            actionButtonTexts[4] = GetButtonLabel(CreateButton(board.transform, "AlarmToggle", "ON/OFF", new Vector3(1.15f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.044f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmToggle, 0, "Alarm ON/OFF"));
            actionButtonTexts[5] = GetButtonLabel(CreateButton(board.transform, "AlarmStop", "停止", new Vector3(1.15f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.046f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmStop, 0, "Stop alarm"));

            Text radioState = CreateText(board.transform, "ラジオ音声  ON",
                new Vector3(0.15f, -0.24f, -0.028f), 0.046f, TextAnchor.UpperLeft,
                font, textMaterial, Color.white);
            radioState.transform.parent.name = "RadioStateCanvas";
            actionButtonTexts[6] = GetButtonLabel(CreateButton(board.transform, "RadioToggle", "音声 ON/OFF", new Vector3(0.94f, -0.43f, -0.0125f),
                new Vector3(0.72f, 0.20f, 0.019f), 0.041f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.RadioToggle, 0, "Radio speaker ON/OFF"));
            Text radioVolumeText = CreateText(board.transform, "ラジオ音量  85%",
                new Vector3(0.15f, -0.60f, -0.028f), 0.044f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            radioVolumeText.transform.parent.name = "RadioVolumeCanvas";
            Slider radioVolumeSlider = CreateSlider(board.transform, "RadioVolumeSliderCanvas",
                new Vector3(0.70f, -0.73f, -0.032f), new Vector2(180f, 32f),
                textMaterial, accentMaterial, WorldRadioSpeaker.DefaultLocalVolume);
            Text saveState = CreateText(board.transform, "設定保存 / SAVE  OFF",
                new Vector3(0.15f, -0.86f, -0.028f), 0.044f, TextAnchor.UpperLeft,
                font, textMaterial, Color.white);
            saveState.transform.parent.name = "SaveStateCanvas";
            actionButtonTexts[7] = GetButtonLabel(CreateButton(board.transform, "SaveToggle", "保存 ON/OFF", new Vector3(0.94f, -1.02f, -0.0125f),
                new Vector3(0.72f, 0.18f, 0.019f), 0.040f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.SaveToggle, 0, "Save local settings ON/OFF"));

            AudioSource alarmAudio = system.AddComponent<AudioSource>();
            alarmAudio.clip = alarmClip;
            alarmAudio.loop = true;
            alarmAudio.playOnAwake = false;
            alarmAudio.spatialBlend = 0f;
            alarmAudio.volume = 0.22f;
            VRCSpatialAudioSource alarmSpatial = system.AddComponent<VRCSpatialAudioSource>();
            SerializedObject serializedAlarmSpatial = new SerializedObject(alarmSpatial);
            SetBool(serializedAlarmSpatial, "EnableSpatialization", false);
            SetBool(serializedAlarmSpatial, "UseAudioSourceVolumeCurve", false);
            serializedAlarmSpatial.ApplyModifiedPropertiesWithoutUndo();

            GameObject[] mirrorsLow;
            GameObject[] mirrorsHigh;
            CreateMirrors(system.transform, mirrorMaterial, buttonMaterial, out mirrorsLow, out mirrorsHigh);
            GameObject nightOverlay = CreateNightOverlay(system.transform, nightMaterial);
            WorldPresenceNotifier notifier = CreatePresenceNotifier(system.transform, font, textMaterial);
            WorldRadioSpeaker radio = ConfigureRadioSpeaker();
            CreateTreeToggle(system.transform, buttonMaterial, accentMaterial, font, textMaterial, controller);

            controller.settingsBoard = board;
            controller.nightOverlay = nightOverlay;
            controller.nightOverlayMaterial = nightMaterial;
            controller.nightSlider = slider;
            controller.mirrorsLow = mirrorsLow;
            controller.mirrorsHigh = mirrorsHigh;
            controller.clockText = clock;
            controller.alarmText = alarmState;
            controller.mirrorText = mirrorState;
            controller.radioStateText = radioState;
            controller.radioVolumeText = radioVolumeText;
            controller.saveText = saveState;
            controller.titleText = title;
            controller.nightModeText = nightModeText;
            controller.languageButtonText = languageButtonText;
            controller.mirrorButtonTexts = mirrorButtonTexts;
            controller.actionButtonTexts = actionButtonTexts;
            controller.alarmAudio = alarmAudio;
            controller.radioSpeaker = radio;
            controller.radioVolumeSlider = radioVolumeSlider;
            controller.presenceNotifier = notifier;
            controller.notifyText = notifyState;
            UdonSharpEditorUtility.CopyProxyToUdon(controller);
            EditorUtility.SetDirty(controller);

            board.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed five-language local settings board, five per-direction mirror ON/OFF toggles with a shared LQ/HQ switch, night mode, alarm, clock, and the radio speaker with its own local volume.");
        }

        internal static void ValidateScene(Scene scene)
        {
            GameObject system = GameObject.Find("World/SettingsSystem");
            WorldSettingsController controller = system != null ? system.GetComponent<WorldSettingsController>() : null;
            WorldSettingsButton[] buttons = system != null
                ? system.GetComponentsInChildren<WorldSettingsButton>(true)
                : new WorldSettingsButton[0];
            GameObject board = system != null ? system.transform.Find("LocalSettingsBoard")?.gameObject : null;
            Transform treeToggle = system != null ? system.transform.Find("TreeSettingsToggle") : null;
            Transform treeToggleButton = treeToggle != null ? treeToggle.Find("Toggle") : null;
            Transform treeGearIcon = treeToggle != null ? treeToggle.Find("GearIcon") : null;
            BoxCollider treeToggleCollider = treeToggleButton != null
                ? treeToggleButton.GetComponent<BoxCollider>() : null;
            MeshRenderer treeGearRenderer = treeGearIcon != null
                ? treeGearIcon.GetComponent<MeshRenderer>() : null;
            VRCPickup boardPickup = board != null ? board.GetComponent<VRCPickup>() : null;
            if (controller == null || board == null || board.activeSelf ||
                buttons.Length != 19 ||
                controller.nightSlider == null || controller.mirrorsLow == null || controller.mirrorsLow.Length != 5 ||
                controller.mirrorsHigh == null || controller.mirrorsHigh.Length != 5 ||
                controller.radioSpeaker == null || controller.alarmAudio == null ||
                controller.radioVolumeSlider == null || controller.radioVolumeText == null ||
                controller.titleText == null || controller.nightModeText == null ||
                controller.languageButtonText == null || controller.mirrorButtonTexts == null ||
                controller.mirrorButtonTexts.Length != 7 || controller.actionButtonTexts == null ||
                controller.actionButtonTexts.Length != 10 ||
                controller.presenceNotifier == null || controller.notifyText == null ||
                boardPickup == null || board.GetComponent<WorldSettingsBoardPickup>() == null ||
                treeToggle == null || treeToggleButton == null || treeGearIcon == null ||
                treeGearIcon.GetComponent<MeshFilter>() == null ||
                treeGearIcon.GetComponent<MeshFilter>().sharedMesh == null ||
                treeGearRenderer == null || treeGearRenderer.sharedMaterial == null ||
                treeGearRenderer.sharedMaterial.shader == null ||
                treeGearRenderer.sharedMaterial.shader.name != "Unlit/Color" ||
                treeToggleCollider == null ||
                !treeToggleCollider.isTrigger ||
                treeToggleButton.Find("TextCanvas") != null ||
                treeToggleButton.GetComponentInChildren<VRCUiShape>(true) != null ||
                treeToggleCollider.center.x != 0f || treeToggleCollider.center.y != 0f ||
                Mathf.Abs(treeToggleCollider.center.z - -1.15f) > 0.001f ||
                Mathf.Abs(treeToggleCollider.size.x - (0.30f / (0.24f * TreeToggleScale))) > 0.001f ||
                Mathf.Abs(treeToggleCollider.size.y - (0.30f / (0.24f * TreeToggleScale))) > 0.001f ||
                Mathf.Abs(treeToggleCollider.size.z - 2.60f) > 0.001f ||
                (treeToggle.position - TreeTogglePosition).sqrMagnitude > 0.0001f ||
                Quaternion.Angle(treeToggle.rotation, TreeToggleRotation) > 0.01f ||
                Mathf.Abs(treeToggle.localScale.x - TreeToggleScale) > 0.0001f)
                throw new InvalidOperationException("Local settings system validation failed.");
            float colliderWorldWidth = treeToggleButton.TransformVector(
                Vector3.right * treeToggleCollider.size.x).magnitude;
            if (colliderWorldWidth < 0.29f || colliderWorldWidth > 0.31f)
                throw new InvalidOperationException(
                    "Tree settings interaction target must remain a compact square around the visible gear.");
            if (Mathf.Abs(boardPickup.proximity - 0.35f) > 0.001f ||
                !string.IsNullOrEmpty(boardPickup.UseText) || board.GetComponent<VRCObjectSync>() != null)
                throw new InvalidOperationException("Local settings board pickup must remain local and UI-safe.");
            if ((board.transform.position - BoardPosition).sqrMagnitude > 0.0001f ||
                Mathf.Abs(board.transform.localScale.x - BoardScale) > 0.0001f ||
                Quaternion.Angle(board.transform.rotation, Quaternion.Euler(BoardEuler)) > 0.01f)
                throw new InvalidOperationException("Local settings board size or facing validation failed.");
            ValidateTreeSettingsAccess(treeToggle, treeToggleButton, treeToggleCollider, treeGearIcon, board);
            BoxCollider boardGrip = board.GetComponent<BoxCollider>();
            if (boardGrip == null || boardGrip.size.y > 0.30f)
                throw new InvalidOperationException("Local settings board pickup grip overlaps the control area.");
            if (UnityEngine.Object.FindObjectOfType<EventSystem>(true) == null)
                throw new InvalidOperationException(
                    "The scene has no EventSystem, so the board sliders cannot receive drag events.");
            ValidateWorldUiTarget(controller.nightSlider.gameObject);
            ValidateWorldUiTarget(controller.radioVolumeSlider.gameObject);
            VRCUiShape[] shapes = system.GetComponentsInChildren<VRCUiShape>(true);
            for (int index = 0; index < shapes.Length; index++)
                ValidateWorldUiTarget(shapes[index].gameObject);
            for (int index = 0; index < controller.mirrorsLow.Length; index++)
                if (controller.mirrorsLow[index] == null || controller.mirrorsLow[index].activeSelf ||
                    controller.mirrorsLow[index].GetComponent<VRCMirrorReflection>() == null ||
                    controller.mirrorsHigh[index] == null || controller.mirrorsHigh[index].activeSelf ||
                    controller.mirrorsHigh[index].GetComponent<VRCMirrorReflection>() == null)
                    throw new InvalidOperationException("Local mirror quality validation failed at index " + index + ".");
            for (int index = 0; index < buttons.Length; index++)
                if (UdonSharpEditorUtility.GetBackingUdonBehaviour(buttons[index]) == null ||
                    (buttons[index].action != WorldSettingsButton.BoardToggle &&
                     buttons[index].GetComponentInChildren<VRCUiShape>(true) == null))
                    throw new InvalidOperationException(
                        "Local settings button has no backing Udon behaviour or VR UI beam target: " +
                        buttons[index].name);
            Transform blanket = GameObject.Find("World/Environment/PicnicSpot/PicnicBlanketBlue")?.transform;
            if (blanket == null || blanket.GetComponentsInChildren<MeshCollider>(true).Length != 1)
                throw new InvalidOperationException("Picnic blanket collider validation failed.");
            Vector3 matCenter;
            Vector3 matForward;
            Vector3 matRight;
            float matHalfForward;
            float matHalfRight;
            ResolveBlanketFrame(out matCenter, out matForward, out matRight,
                out matHalfForward, out matHalfRight);
            float[] expectedDistances =
            {
                matHalfForward + MirrorEdgeMargin, matHalfForward + MirrorEdgeMargin,
                matHalfRight + MirrorEdgeMargin, matHalfRight + MirrorEdgeMargin
            };
            for (int index = 0; index < 4; index++)
            {
                Transform mirror = controller.mirrorsLow[index].transform;
                Vector3 towardBlanket = Vector3.ProjectOnPlane(
                    matCenter - mirror.position, Vector3.up).normalized;
                Vector3 reflectiveFront = -mirror.forward;
                if (Vector3.Dot(towardBlanket, reflectiveFront) < 0.95f)
                    throw new InvalidOperationException(
                        "Picnic mirror reflective face points away at index " + index + ".");
                // The panels must hug the mat edge and stand on the ground rather than float.
                float offset = Vector3.ProjectOnPlane(mirror.position - matCenter, Vector3.up).magnitude;
                float baseHeight = mirror.position.y - MirrorHeight * 0.5f;
                if (Mathf.Abs(offset - expectedDistances[index]) > 0.01f ||
                    Mathf.Abs(baseHeight - MirrorBaseHeight(mirror.position)) > 0.01f)
                    throw new InvalidOperationException(
                        "Picnic mirror is not aligned to the mat edge at index " + index + ".");
            }
            if (Vector3.Dot(-controller.mirrorsLow[4].transform.forward, Vector3.down) < 0.95f)
                throw new InvalidOperationException("Ceiling mirror reflective face does not point down.");
            Transform radioRoot = GameObject.Find("World/Environment/PicnicSpot/PicnicRadio")?.transform;
            UdonBehaviour radioBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller.radioSpeaker);
            if (radioRoot == null || radioRoot.Find("RadioUseTrigger") != null ||
                controller.radioSpeaker.transform.name != "YamaRadioSpeaker" ||
                controller.radioSpeaker.GetComponent<Collider>() != null ||
                controller.radioSpeaker.speakerSource == null ||
                controller.radioSpeaker.referenceSource == null ||
                controller.radioSpeaker.speakerSource.GetComponent<VRCSpatialAudioSource>() == null ||
                radioBacking == null ||
                !string.IsNullOrEmpty(radioBacking.InteractionText) ||
                radioBacking.proximity > 0.001f ||
                controller.alarmAudio.GetComponent<VRCSpatialAudioSource>() == null)
                throw new InvalidOperationException(
                    "Radio speaker must be board-controlled and expose no radio-side USE interaction.");
            WorldPresenceNotifier notifier = controller.presenceNotifier;
            int localLayer = LayerMask.NameToLayer("PlayerLocal");
            if (notifier.hudRoot == null || notifier.hudText == null || notifier.hudRoot.activeSelf ||
                notifier.joinAudio == null || notifier.leaveAudio == null ||
                notifier.joinAudio.clip == null || notifier.leaveAudio.clip == null ||
                notifier.joinAudio.clip == notifier.leaveAudio.clip ||
                notifier.joinAudio.playOnAwake || notifier.leaveAudio.playOnAwake ||
                notifier.joinAudio.spatialBlend != 0f || notifier.leaveAudio.spatialBlend != 0f ||
                notifier.joinAudio.GetComponent<VRCSpatialAudioSource>() == null ||
                notifier.leaveAudio.GetComponent<VRCSpatialAudioSource>() == null ||
                UdonSharpEditorUtility.GetBackingUdonBehaviour(notifier) == null ||
                (localLayer >= 0 && notifier.hudRoot.layer != localLayer))
                throw new InvalidOperationException(
                    "Local join/leave notifier must stay local, silent on load, and hidden by default.");
            CompassSceneInstaller.ValidateScene();
            Debug.Log("[Stargazing Hill] Local settings system validation passed.");
        }

        /// <summary>
        /// A world UI surface is only usable if VRChat's pointer can reach it: an interactive layer
        /// plus a collider. The UI layer is excluded from the interactive mask while the main menu is
        /// closed, so a canvas parked there is inert during play and invisible to the camera.
        /// </summary>
        internal static void ValidateWorldUiTarget(GameObject canvasObject)
        {
            BoxCollider target = canvasObject.GetComponent<BoxCollider>();
            if (canvasObject.layer != WorldInformationPanelInstaller.WorldUiLayer ||
                target == null || !target.isTrigger)
                throw new InvalidOperationException(
                    "World UI surface is unreachable by the VRChat pointer: " + canvasObject.name +
                    " (layer " + canvasObject.layer + ", collider " + (target != null) + ").");
        }

        private static void ValidateTreeSettingsAccess(Transform treeToggle, Transform treeToggleButton,
            BoxCollider interactionCollider, Transform gearIcon, GameObject board)
        {
            Collider treeCollider = GameObject.Find("World/Environment/LandmarkTreeCollider")
                ?.GetComponent<Collider>();
            if (treeCollider == null)
                throw new InvalidOperationException("Landmark tree collider is missing.");

            Physics.SyncTransforms();
            Vector3 readerPosition = gearIcon.position - treeToggle.forward * 0.80f;
            Vector3 direction = treeToggle.forward;
            RaycastHit[] hits = Physics.RaycastAll(
                readerPosition, direction, 1.20f, ~0, QueryTriggerInteraction.Collide);
            Collider firstCollider = null;
            float firstDistance = float.PositiveInfinity;
            for (int index = 0; index < hits.Length; index++)
            {
                if (hits[index].distance >= firstDistance) continue;
                firstDistance = hits[index].distance;
                firstCollider = hits[index].collider;
            }
            if (firstCollider != interactionCollider)
                throw new InvalidOperationException(
                    "The landmark tree blocks the reader ray before the settings gear interaction collider.");

            // Sample the settings face from the same reader side. The tree must not cross the
            // line of sight to the center or any control-area corner.
            Vector2[] localSamples =
            {
                Vector2.zero,
                new Vector2(-1.20f, 0.90f), new Vector2(1.20f, 0.90f),
                new Vector2(-1.20f, -0.90f), new Vector2(1.20f, -0.90f)
            };
            for (int index = 0; index < localSamples.Length; index++)
            {
                Vector3 target = board.transform.TransformPoint(
                    new Vector3(localSamples[index].x, localSamples[index].y, -0.03f));
                Vector3 origin = target - board.transform.forward * 0.80f;
                Ray ray = new Ray(origin, board.transform.forward);
                RaycastHit treeHit;
                if (treeCollider.Raycast(ray, out treeHit, 0.80f))
                    throw new InvalidOperationException(
                        "The landmark tree blocks the settings board at sample " + index + ".");
            }
        }

        /// <summary>
        /// uGUI only delivers drag events through an EventSystem, and the VRChat default world
        /// scene ships one for exactly that reason.  Without it the board sliders never move.
        /// </summary>
        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem existing = UnityEngine.Object.FindObjectOfType<EventSystem>(true);
            if (existing == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
                existing = eventSystem.AddComponent<EventSystem>();
            }
            if (existing.GetComponent<StandaloneInputModule>() == null)
                existing.gameObject.AddComponent<StandaloneInputModule>();
            EditorUtility.SetDirty(existing);
        }

        private static void RemoveGeneratedRadioComponents()
        {
            Transform radio = GameObject.Find("World/Environment/PicnicSpot/PicnicRadio")?.transform;
            if (radio == null) return;
            Transform oldTrigger = radio.Find("RadioUseTrigger");
            if (oldTrigger != null) UnityEngine.Object.DestroyImmediate(oldTrigger.gameObject);
            Transform oldSpeaker = radio.Find("YamaRadioSpeaker");
            if (oldSpeaker != null) UnityEngine.Object.DestroyImmediate(oldSpeaker.gameObject);
        }

        private static WorldRadioSpeaker ConfigureRadioSpeaker()
        {
            Transform radio = GameObject.Find("World/Environment/PicnicSpot/PicnicRadio")?.transform;
            Yamadev.YamaStream.Controller yama =
                UnityEngine.Object.FindObjectOfType<Yamadev.YamaStream.Controller>(true);
            if (radio == null || yama == null)
                throw new InvalidOperationException("Picnic radio or YamaPlayer controller is missing.");

            GameObject speakerObject = new GameObject("YamaRadioSpeaker");
            speakerObject.transform.SetParent(radio, false);
            speakerObject.transform.localPosition = Vector3.zero;
            AudioSource source = speakerObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.spatialize = true;
            // A 0.35 m inner radius put the picnic party outside full volume and made the radio
            // read as silent.  Cover the mat, then roll off across the hill.
            source.minDistance = 1.5f;
            source.maxDistance = 22f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.enabled = true;
            source.volume = WorldRadioSpeaker.DefaultLocalVolume;
            YamaPlayerSpeaker yamaSpeaker = speakerObject.AddComponent<YamaPlayerSpeaker>();
            yamaSpeaker.controller = yama;
            VRCSpatialAudioSource spatial = speakerObject.GetComponent<VRCSpatialAudioSource>();
            SerializedObject serializedSpatial = new SerializedObject(spatial);
            SerializedProperty near = serializedSpatial.FindProperty("Near");
            if (near != null) near.floatValue = 1.5f;
            SerializedProperty far = serializedSpatial.FindProperty("Far");
            if (far != null) far.floatValue = 22f;
            SetBool(serializedSpatial, "EnableSpatialization", true);
            SetBool(serializedSpatial, "UseAudioSourceVolumeCurve", true);
            serializedSpatial.ApplyModifiedPropertiesWithoutUndo();
            VRCAVProVideoSpeaker avProSpeaker = speakerObject.GetComponent<VRCAVProVideoSpeaker>();
            VRCAVProVideoPlayer avProPlayer = UnityEngine.Object.FindObjectOfType<VRCAVProVideoPlayer>(true);
            if (avProSpeaker != null && avProPlayer != null)
            {
                SerializedObject serializedSpeaker = new SerializedObject(avProSpeaker);
                SerializedProperty videoPlayer = serializedSpeaker.FindProperty("videoPlayer");
                if (videoPlayer != null) videoPlayer.objectReferenceValue = avProPlayer;
                serializedSpeaker.ApplyModifiedPropertiesWithoutUndo();
            }
            AppendAudioSource(yama, source);
            VRCUnityVideoPlayer[] unityPlayers = UnityEngine.Object.FindObjectsOfType<VRCUnityVideoPlayer>(true);
            for (int index = 0; index < unityPlayers.Length; index++)
                AppendSerializedAudioSource(unityPlayers[index], "targetAudioSources", source);

            WorldRadioSpeaker radioBehaviour = UdonSharpUndo.AddComponent<WorldRadioSpeaker>(speakerObject);
            radioBehaviour.speakerSource = source;
            AudioSource[] referenceCandidates = yama.AudioSources ?? new AudioSource[0];
            for (int index = 0; index < referenceCandidates.Length; index++)
            {
                if (referenceCandidates[index] == null || referenceCandidates[index] == source) continue;
                radioBehaviour.referenceSource = referenceCandidates[index];
                break;
            }
            UdonSharpEditorUtility.CopyProxyToUdon(radioBehaviour);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(radioBehaviour);
            if (backing != null)
            {
                backing.InteractionText = string.Empty;
                backing.proximity = 0f;
                EditorUtility.SetDirty(backing);
            }
            EditorUtility.SetDirty(radioBehaviour);
            return radioBehaviour;
        }

        private static void AppendAudioSource(Yamadev.YamaStream.Controller controller, AudioSource source)
        {
            AudioSource[] current = controller.AudioSources ?? new AudioSource[0];
            var result = new List<AudioSource>(current.Length + 1);
            for (int index = 0; index < current.Length; index++)
                if (current[index] != null && current[index] != source) result.Add(current[index]);
            result.Add(source);
            controller.AudioSources = result.ToArray();
            EditorUtility.SetDirty(controller);
        }

        private static void AppendSerializedAudioSource(UnityEngine.Object target, string propertyName, AudioSource source)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(propertyName);
            if (array == null || !array.isArray) return;
            for (int index = 0; index < array.arraySize; index++)
                if (array.GetArrayElementAtIndex(index).objectReferenceValue == source) return;
            int newIndex = array.arraySize;
            array.arraySize++;
            array.GetArrayElementAtIndex(newIndex).objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Measures the picnic blanket in its own horizontal axes from the rendered mesh rather
        /// than from the saved anchor transform.  The mat conforms to the hill and spans more than
        /// a metre of height, so only its ground plan comes from here; each mirror takes its own
        /// base height from the terrain directly beneath it.
        /// </summary>
        private static void ResolveBlanketFrame(out Vector3 center, out Vector3 forward, out Vector3 right,
            out float halfForward, out float halfRight)
        {
            Transform blanket = GameObject.Find("World/Environment/PicnicSpot/PicnicBlanketBlue")?.transform;
            MeshFilter filter = blanket != null ? blanket.GetComponentInChildren<MeshFilter>(true) : null;
            if (blanket == null || filter == null || filter.sharedMesh == null)
                throw new InvalidOperationException("Picnic blanket is missing for mirror layout.");

            forward = Vector3.ProjectOnPlane(blanket.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(blanket.right, Vector3.up).normalized;
            Vector3[] vertices = filter.sharedMesh.vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("Picnic blanket mesh has no readable vertices.");
            float minForward = float.MaxValue;
            float maxForward = float.MinValue;
            float minRight = float.MaxValue;
            float maxRight = float.MinValue;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 world = filter.transform.TransformPoint(vertices[index]);
                float alongForward = Vector3.Dot(world, forward);
                float alongRight = Vector3.Dot(world, right);
                minForward = Mathf.Min(minForward, alongForward);
                maxForward = Mathf.Max(maxForward, alongForward);
                minRight = Mathf.Min(minRight, alongRight);
                maxRight = Mathf.Max(maxRight, alongRight);
            }
            // forward, right and up form an orthonormal basis, so the projections rebuild a point.
            center = forward * ((minForward + maxForward) * 0.5f) +
                right * ((minRight + maxRight) * 0.5f);
            center.y = MirrorBaseHeight(center);
            halfForward = (maxForward - minForward) * 0.5f;
            halfRight = (maxRight - minRight) * 0.5f;
        }

        /// <summary>Ground height a mirror stands on, just clear of the mat it sits beside.</summary>
        private static float MirrorBaseHeight(Vector3 position)
        {
            return StargazingWorldBuilder.EvaluateTerrainHeightForEditor(position.x, position.z) +
                MirrorGroundClearance;
        }

        private static void CreateMirrors(Transform parent, Material material, Material frameMaterial,
            out GameObject[] lowQuality, out GameObject[] highQuality)
        {
            string[] names = { "Up", "Down", "Left", "Right", "Ceiling" };
            Transform blanket = GameObject.Find("World/Environment/PicnicSpot/PicnicBlanketBlue")?.transform;
            Vector3 center;
            Vector3 forward;
            Vector3 right;
            float halfForward;
            float halfRight;
            ResolveBlanketFrame(out center, out forward, out right, out halfForward, out halfRight);
            Vector3[] outward = { forward, -forward, -right, right };
            // Each panel is as wide as the mat edge it stands on and starts at the mat surface.
            float[] distances =
            {
                halfForward + MirrorEdgeMargin, halfForward + MirrorEdgeMargin,
                halfRight + MirrorEdgeMargin, halfRight + MirrorEdgeMargin
            };
            float[] widths = { halfRight * 2f, halfRight * 2f, halfForward * 2f, halfForward * 2f };
            Vector3[] positions = new Vector3[5];
            Quaternion[] rotations = new Quaternion[5];
            Vector3[] scales = new Vector3[5];
            for (int index = 0; index < 4; index++)
            {
                Vector3 footing = center + outward[index] * distances[index];
                footing.y = MirrorBaseHeight(footing) + MirrorHeight * 0.5f;
                positions[index] = footing;
                // Unity's Quad and the VRChat mirror face local -Z.  Point local +Z away
                // from the blanket so every reflective face looks inward.
                rotations[index] = Quaternion.LookRotation(outward[index], Vector3.up);
                scales[index] = new Vector3(widths[index], MirrorHeight, 1f);
            }
            positions[4] = center + Vector3.up * MirrorCeilingHeight;
            rotations[4] = Quaternion.Euler(-90f, blanket.eulerAngles.y, 0f);
            scales[4] = new Vector3(halfRight * 2f, halfForward * 2f, 1f);
            GameObject root = new GameObject("LocalPicnicMirrors");
            root.transform.SetParent(parent, false);
            lowQuality = new GameObject[names.Length];
            highQuality = new GameObject[names.Length];
            int reflectLayers = LayerMask.GetMask("Default", "Environment", "Pickup", "Walkthrough",
                "Player", "PlayerLocal", "MirrorReflection");
            for (int index = 0; index < names.Length; index++)
            {
                lowQuality[index] = CreateMirrorVariant(root.transform, "Mirror_" + names[index] + "_LQ",
                    positions[index], rotations[index], scales[index], material, frameMaterial,
                    reflectLayers, true, 1);
                highQuality[index] = CreateMirrorVariant(root.transform, "Mirror_" + names[index] + "_HQ",
                    positions[index], rotations[index], scales[index], material, frameMaterial,
                    reflectLayers, false, 4);
            }
        }

        private static GameObject CreateMirrorVariant(Transform parent, string name, Vector3 position,
            Quaternion rotation, Vector3 scale, Material material, Material frameMaterial,
            int reflectLayers, bool disablePixelLights, int antialiasing)
        {
            GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mirror.name = name;
            mirror.transform.SetParent(parent, false);
            mirror.transform.position = position;
            mirror.transform.rotation = rotation;
            mirror.transform.localScale = scale;
            mirror.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(mirror.GetComponent<Collider>());
            VRCMirrorReflection reflection = mirror.AddComponent<VRCMirrorReflection>();
            SerializedObject serialized = new SerializedObject(reflection);
            SetBool(serialized, "m_DisablePixelLights", disablePixelLights);
            SetBool(serialized, "TurnOffMirrorOcclusion", false);
            SetInt(serialized, "m_ReflectLayers", reflectLayers);
            SetInt(serialized, "maximumAntialiasing", antialiasing);
            SetInt(serialized, "cameraClearFlags", (int)CameraClearFlags.SolidColor);
            SerializedProperty clear = serialized.FindProperty("customClearColor");
            if (clear != null) clear.colorValue = new Color(0.003f, 0.008f, 0.018f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateMirrorFrame(mirror.transform, frameMaterial);
            mirror.SetActive(false);
            return mirror;
        }

        private static void CreateMirrorFrame(Transform mirror, Material material)
        {
            const float front = -0.012f;
            CreateCube("FrameTop", mirror, new Vector3(0f, 0.505f, front),
                new Vector3(1.035f, 0.025f, 0.018f), material, false);
            CreateCube("FrameBottom", mirror, new Vector3(0f, -0.505f, front),
                new Vector3(1.035f, 0.025f, 0.018f), material, false);
            CreateCube("FrameLeft", mirror, new Vector3(-0.505f, 0f, front),
                new Vector3(0.025f, 1.035f, 0.018f), material, false);
            CreateCube("FrameRight", mirror, new Vector3(0.505f, 0f, front),
                new Vector3(0.025f, 1.035f, 0.018f), material, false);
        }

        /// <summary>
        /// Local join/leave chime and toast. The toast follows the head on the PlayerLocal layer, so
        /// only its owner sees it and it stays out of mirrors and other people's photographs.
        /// </summary>
        private static WorldPresenceNotifier CreatePresenceNotifier(Transform parent, Font font,
            Material textMaterial)
        {
            GameObject root = new GameObject("LocalPresenceNotifier");
            root.transform.SetParent(parent, false);
            WorldPresenceNotifier notifier = UdonSharpUndo.AddComponent<WorldPresenceNotifier>(root);

            GameObject hud = new GameObject("Hud");
            hud.transform.SetParent(root.transform, false);
            int localLayer = LayerMask.NameToLayer("PlayerLocal");
            if (localLayer >= 0) hud.layer = localLayer;
            Text hudText = CreateText(hud.transform, string.Empty, Vector3.zero, 0.062f,
                TextAnchor.UpperCenter, font, textMaterial, new Color(0.85f, 0.94f, 1f));
            if (localLayer >= 0)
            {
                hudText.transform.parent.gameObject.layer = localLayer;
                hudText.gameObject.layer = localLayer;
            }
            hud.SetActive(false);

            AudioSource joinAudio = CreateChimeSource(root.transform, "JoinChime",
                EnsureChimeClip(JoinChimePath, 660f, 990f));
            AudioSource leaveAudio = CreateChimeSource(root.transform, "LeaveChime",
                EnsureChimeClip(LeaveChimePath, 880f, 587f));

            notifier.hudRoot = hud;
            notifier.hudText = hudText;
            notifier.joinAudio = joinAudio;
            notifier.leaveAudio = leaveAudio;
            UdonSharpEditorUtility.CopyProxyToUdon(notifier);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(notifier);
            if (backing != null)
            {
                backing.InteractionText = string.Empty;
                backing.proximity = 0f;
                EditorUtility.SetDirty(backing);
            }
            EditorUtility.SetDirty(notifier);
            return notifier;
        }

        private static AudioSource CreateChimeSource(Transform parent, string name, AudioClip clip)
        {
            GameObject chime = new GameObject(name);
            chime.transform.SetParent(parent, false);
            AudioSource source = chime.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = false;
            source.playOnAwake = false;
            // A notification about the instance is not a thing in the world, so it plays flat.
            source.spatialBlend = 0f;
            source.volume = 0.30f;
            VRCSpatialAudioSource spatial = chime.AddComponent<VRCSpatialAudioSource>();
            SerializedObject serialized = new SerializedObject(spatial);
            SetBool(serialized, "EnableSpatialization", false);
            SetBool(serialized, "UseAudioSourceVolumeCurve", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return source;
        }

        private static GameObject CreateNightOverlay(Transform parent, Material material)
        {
            GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            overlay.name = "LocalNightModeOverlay";
            overlay.transform.SetParent(parent, false);
            overlay.transform.localScale = Vector3.one * 0.42f;
            Renderer renderer = overlay.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            UnityEngine.Object.DestroyImmediate(overlay.GetComponent<Collider>());
            int localLayer = LayerMask.NameToLayer("PlayerLocal");
            if (localLayer >= 0) overlay.layer = localLayer;
            overlay.SetActive(false);
            return overlay;
        }

        private static void CreateTreeToggle(Transform parent, Material buttonMaterial, Material iconMaterial,
            Font font, Material textMaterial, WorldSettingsController controller)
        {
            GameObject dock = new GameObject("TreeSettingsToggle");
            dock.transform.SetParent(parent, false);
            dock.transform.localPosition = TreeTogglePosition;
            dock.transform.localRotation = TreeToggleRotation;
            dock.transform.localScale = Vector3.one * TreeToggleScale;
            // Keep the UI label empty: the CJK font has no stable gear glyph on every target and
            // rendering it over the old 3D parts caused severe shimmering. A single mesh below is
            // the visible icon; a separate transparent Image supplies the VRChat UI ray target.
            GameObject toggle = CreateButton(dock.transform, "Toggle", string.Empty, Vector3.zero,
                new Vector3(0.24f, 0.24f, 0.035f), 0.115f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.BoardToggle, 0, "設定を開く / Open local settings");
            BoxCollider interactionCollider = toggle.GetComponent<BoxCollider>();
            // A compact 0.30m square sits around the visible gear. The whole dock is outside the
            // tree capsule, so the reader ray reaches this trigger before the trunk collider.
            interactionCollider.center = new Vector3(0f, 0f, -1.15f);
            float colliderFaceSize = 0.30f / (0.24f * TreeToggleScale);
            interactionCollider.size = new Vector3(colliderFaceSize, colliderFaceSize, 2.60f);
            Transform interactionCanvas = toggle.transform.Find("TextCanvas");
            if (interactionCanvas != null)
                UnityEngine.Object.DestroyImmediate(interactionCanvas.gameObject);
            CreateGearIcon(dock.transform, iconMaterial);
        }

        private static void CreateGearIcon(Transform parent, Material iconMaterial)
        {
            GameObject icon = new GameObject("GearIcon");
            icon.transform.SetParent(parent, false);
            // Leave a visible gap from the button face so the icon cannot z-fight at grazing angles.
            icon.transform.localPosition = new Vector3(0f, 0f, -0.040f);
            MeshFilter filter = icon.AddComponent<MeshFilter>();
            filter.sharedMesh = EnsureGearIconMesh();
            MeshRenderer renderer = icon.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = iconMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Mesh EnsureGearIconMesh()
        {
            const int segmentCount = 32;
            const float innerRadius = 0.032f;
            const float baseRadius = 0.084f;
            const float toothRadius = 0.108f;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GearIconMeshPath);
            bool createAsset = mesh == null;
            if (mesh == null) mesh = new Mesh();
            mesh.name = "TreeSettingsGearIcon";
            mesh.Clear();
            var vertices = new List<Vector3>(segmentCount * 4);
            var normals = new List<Vector3>(segmentCount * 4);
            var triangles = new List<int>(segmentCount * 6);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float angle0 = segment * Mathf.PI * 2f / segmentCount;
                float angle1 = (segment + 1) * Mathf.PI * 2f / segmentCount;
                float outer0 = segment % 4 < 2 ? toothRadius : baseRadius;
                float outer1 = (segment + 1) % 4 < 2 ? toothRadius : baseRadius;
                int first = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(angle0) * innerRadius, Mathf.Sin(angle0) * innerRadius, 0f));
                vertices.Add(new Vector3(Mathf.Cos(angle0) * outer0, Mathf.Sin(angle0) * outer0, 0f));
                vertices.Add(new Vector3(Mathf.Cos(angle1) * outer1, Mathf.Sin(angle1) * outer1, 0f));
                vertices.Add(new Vector3(Mathf.Cos(angle1) * innerRadius, Mathf.Sin(angle1) * innerRadius, 0f));
                normals.Add(Vector3.back);
                normals.Add(Vector3.back);
                normals.Add(Vector3.back);
                normals.Add(Vector3.back);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
                triangles.Add(first + 3);
                triangles.Add(first + 2);
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (createAsset) AssetDatabase.CreateAsset(mesh, GearIconMeshPath);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Slider CreateNightSlider(Transform parent, Material uiMaterial, Material accentMaterial)
        {
            return CreateSlider(parent, "NightModeSliderCanvas",
                new Vector3(-0.85f, -0.76f, -0.032f), new Vector2(255f, 41f),
                uiMaterial, accentMaterial, 0f);
        }

        private static Slider CreateSlider(Transform parent, string name, Vector3 position,
            Vector2 size, Material uiMaterial, Material accentMaterial, float defaultValue)
        {
            GameObject canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            canvasObject.transform.localScale = Vector3.one * CanvasScale;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = size;
            // Default layer, UI shape, raycaster and a trigger box: VRChat hits the collider first and
            // only then runs the GraphicRaycaster, and it ignores the UI layer during normal play.
            // Missing both is why neither knob could be grabbed.
            WorldInformationPanelInstaller.ConfigureWorldUiCanvas(canvas);

            GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.layer = canvasObject.layer;
            trackObject.transform.SetParent(canvasObject.transform, false);
            RectTransform track = trackObject.GetComponent<RectTransform>();
            track.anchorMin = new Vector2(0f, 0.35f);
            track.anchorMax = new Vector2(1f, 0.65f);
            track.offsetMin = Vector2.zero;
            track.offsetMax = Vector2.zero;
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = new Color(0.08f, 0.17f, 0.28f, 1f);
            trackImage.material = uiMaterial;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = canvasObject.layer;
            fillObject.transform.SetParent(track, false);
            RectTransform fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color(0.58f, 0.90f, 1f, 1f);
            fillImage.material = uiMaterial;

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObject.layer = canvasObject.layer;
            handleObject.transform.SetParent(canvasObject.transform, false);
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            handle.sizeDelta = new Vector2(Mathf.Min(56f, size.y * 0.70f), size.y * 0.90f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = accentMaterial.color;
            handleImage.material = uiMaterial;

            Slider slider = canvasObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(defaultValue);
            slider.wholeNumbers = false;
            return slider;
        }

        private static GameObject CreateButton(Transform parent, string name, string label,
            Vector3 position, Vector3 scale, float textHeight, Material material, Font font,
            Material textMaterial, WorldSettingsController controller, int action, int value,
            string interactionText)
        {
            GameObject button = CreateCube(name, parent, position, scale, material, true);
            BoxCollider collider = button.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            CreateText(button.transform, label, new Vector3(0f, 0f, -0.53f), textHeight,
                TextAnchor.MiddleCenter, font, textMaterial, Color.white, true);
            WorldSettingsButton behaviour = UdonSharpUndo.AddComponent<WorldSettingsButton>(button);
            behaviour.controller = controller;
            behaviour.action = action;
            behaviour.value = value;
            UdonSharpEditorUtility.CopyProxyToUdon(behaviour);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
            if (backing != null)
            {
                backing.InteractionText = interactionText;
                backing.proximity = 2.5f;
                EditorUtility.SetDirty(backing);
                WorldInformationPanelInstaller.EnableUiBeamForInteraction(button, backing);
            }
            EditorUtility.SetDirty(behaviour);
            return button;
        }

        private static Text GetButtonLabel(GameObject button)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label == null) throw new InvalidOperationException("Generated settings button label is missing.");
            return label;
        }

        private static Text CreateText(Transform parent, string value, Vector3 position, float height,
            TextAnchor anchor, Font font, Material material, Color color, bool compensate = false)
        {
            return WorldInformationPanelInstaller.CreateText(
                parent, value, position, height, anchor, font, material, color, compensate);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        internal static Material EnsurePublicColorMaterial(string path, Color color)
        {
            return EnsureColorMaterial(path, color);
        }

        private static Material EnsureColorMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("Unlit/Color shader is missing.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMirrorMaterial()
        {
            string path = RootPath + "/Generated/Materials/LocalMirror.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("FX/MirrorReflection");
            if (shader == null) throw new InvalidOperationException("VRChat mirror shader is missing.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureNightMaterial()
        {
            string path = RootPath + "/Generated/Materials/NightModeOverlay.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("StargazingHill/NightModeOverlay");
            if (shader == null) throw new InvalidOperationException("NightModeOverlay shader is missing.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetFloat("_Darkness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AudioClip EnsureAlarmClip()
        {
            string directory = Path.GetDirectoryName(AlarmPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(AlarmPath)) File.WriteAllBytes(AlarmPath, BuildAlarmWave());
            AssetDatabase.ImportAsset(AlarmPath, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(AlarmPath) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.PCM,
                    quality = 1f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
                };
                importer.SaveAndReimport();
            }
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AlarmPath);
            if (clip == null) throw new InvalidOperationException("Generated alarm clip could not be imported.");
            return clip;
        }

        /// <summary>Two-tone chime; rising for arrivals, falling for departures.</summary>
        private static AudioClip EnsureChimeClip(string path, float firstTone, float secondTone)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, BuildChimeWave(firstTone, secondTone));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.PCM,
                    quality = 1f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
                };
                importer.SaveAndReimport();
            }
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException("Generated chime could not be imported: " + path);
            return clip;
        }

        private static byte[] BuildChimeWave(float firstTone, float secondTone)
        {
            const int sampleRate = 22050;
            const float toneSeconds = 0.16f;
            const float totalSeconds = 0.40f;
            int sampleCount = Mathf.RoundToInt(sampleRate * totalSeconds);
            byte[] data = new byte[44 + sampleCount * 2];
            WriteWaveHeader(data, sampleRate, sampleCount);
            for (int index = 0; index < sampleCount; index++)
            {
                float t = index / (float)sampleRate;
                bool second = t >= toneSeconds;
                float local = second ? t - toneSeconds : t;
                float frequency = second ? secondTone : firstTone;
                // Short raised-cosine envelope so neither tone clicks on or off.
                float envelope = local >= toneSeconds
                    ? 0f
                    : 0.5f * (1f - Mathf.Cos(Mathf.PI * 2f * local / toneSeconds));
                float sample = Mathf.Sin(t * Mathf.PI * 2f * frequency) * 0.42f * envelope;
                WriteShort(data, 44 + index * 2, (short)Mathf.RoundToInt(sample * 32767f));
            }
            return data;
        }

        private static void WriteWaveHeader(byte[] data, int sampleRate, int sampleCount)
        {
            WriteAscii(data, 0, "RIFF");
            WriteInt(data, 4, 36 + sampleCount * 2);
            WriteAscii(data, 8, "WAVEfmt ");
            WriteInt(data, 16, 16);
            WriteShort(data, 20, 1);
            WriteShort(data, 22, 1);
            WriteInt(data, 24, sampleRate);
            WriteInt(data, 28, sampleRate * 2);
            WriteShort(data, 32, 2);
            WriteShort(data, 34, 16);
            WriteAscii(data, 36, "data");
            WriteInt(data, 40, sampleCount * 2);
        }

        private static byte[] BuildAlarmWave()
        {
            const int sampleRate = 22050;
            const int seconds = 2;
            int sampleCount = sampleRate * seconds;
            byte[] data = new byte[44 + sampleCount * 2];
            WriteAscii(data, 0, "RIFF");
            WriteInt(data, 4, 36 + sampleCount * 2);
            WriteAscii(data, 8, "WAVEfmt ");
            WriteInt(data, 16, 16);
            WriteShort(data, 20, 1);
            WriteShort(data, 22, 1);
            WriteInt(data, 24, sampleRate);
            WriteInt(data, 28, sampleRate * 2);
            WriteShort(data, 32, 2);
            WriteShort(data, 34, 16);
            WriteAscii(data, 36, "data");
            WriteInt(data, 40, sampleCount * 2);
            for (int index = 0; index < sampleCount; index++)
            {
                float t = index / (float)sampleRate;
                bool sounding = (t < 0.34f) || (t > 0.50f && t < 0.84f);
                float envelope = sounding ? Mathf.Min(1f, Mathf.Min((t % 0.50f) * 35f,
                    (0.34f - (t % 0.50f)) * 20f)) : 0f;
                float sample = Mathf.Sin(t * Mathf.PI * 2f * 880f) * 0.34f * Mathf.Clamp01(envelope);
                short pcm = (short)Mathf.RoundToInt(sample * 32767f);
                WriteShort(data, 44 + index * 2, pcm);
            }
            return data;
        }

        private static void WriteAscii(byte[] data, int offset, string value)
        {
            for (int index = 0; index < value.Length; index++) data[offset + index] = (byte)value[index];
        }

        private static void WriteInt(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteShort(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.boolValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.intValue = value;
        }
    }
}
