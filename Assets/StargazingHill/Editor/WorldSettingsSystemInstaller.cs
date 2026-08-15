using System;
using System.Collections.Generic;
using System.IO;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        private const float CanvasScale = 0.002f;
        private const float BoardScale = 0.18f;
        private static readonly Vector3 BoardPosition = new Vector3(10.55f, 2.95f, 9.35f);
        private static readonly Vector3 BoardEuler = new Vector3(0f, 22.2865f, 0f);

        [MenuItem("Stargazing Hill/Content/Settings Board/Rebuild...", false, 40)]
        public static void InstallMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Settings Board",
                    "This replaces World/SettingsSystem and the generated radio interaction.",
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

            Text title = CreateText(board.transform, "ローカル設定 / LOCAL SETTINGS",
                new Vector3(0f, 0.98f, -0.028f), 0.090f, TextAnchor.UpperCenter,
                font, textMaterial, new Color(0.78f, 0.90f, 1f));
            title.transform.parent.name = "TitleCanvas";
            Text clock = CreateText(board.transform, "0000-00-00  00:00:00  LOCAL",
                new Vector3(0f, 0.76f, -0.028f), 0.064f, TextAnchor.UpperCenter,
                font, textMaterial, Color.white);
            clock.transform.parent.name = "ClockCanvas";

            CreateCube("HeaderDivider", board.transform, new Vector3(0f, 0.64f, -0.026f),
                new Vector3(2.35f, 0.012f, 0.012f), accentMaterial, false);
            CreateCube("ColumnDivider", board.transform, new Vector3(0.06f, -0.17f, -0.026f),
                new Vector3(0.012f, 1.48f, 0.012f), accentMaterial, false);

            Text mirrorState = CreateText(board.transform, "ミラー / MIRROR  OFF",
                new Vector3(-1.18f, 0.52f, -0.028f), 0.060f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            mirrorState.transform.parent.name = "MirrorStateCanvas";
            string[] mirrorLabels = { "上", "下", "左", "右", "天井", "全OFF" };
            for (int index = 0; index < mirrorLabels.Length; index++)
            {
                int column = index % 3;
                int row = index / 3;
                float x = -1.04f + column * 0.39f;
                float y = 0.30f - row * 0.25f;
                CreateButton(board.transform, "Mirror_" + index, mirrorLabels[index],
                    new Vector3(x, y, -0.0125f), new Vector3(0.34f, 0.20f, 0.019f),
                    0.055f, buttonMaterial, font, textMaterial, controller,
                    index < 5 ? WorldSettingsButton.Mirror : WorldSettingsButton.MirrorsOff,
                    index, "Mirror / ミラー " + mirrorLabels[index]);
            }

            CreateText(board.transform, "ナイトモード / NIGHT MODE",
                new Vector3(-1.18f, -0.26f, -0.028f), 0.058f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            Slider slider = CreateNightSlider(board.transform, textMaterial, accentMaterial);

            Text alarmState = CreateText(board.transform, "ALARM  22:00  OFF",
                new Vector3(0.15f, 0.52f, -0.028f), 0.060f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            alarmState.transform.parent.name = "AlarmStateCanvas";
            CreateButton(board.transform, "AlarmHourDown", "時−", new Vector3(0.35f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmHour, -1, "Alarm hour -1");
            CreateButton(board.transform, "AlarmHourUp", "時＋", new Vector3(0.75f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmHour, 1, "Alarm hour +1");
            CreateButton(board.transform, "AlarmMinuteDown", "分−", new Vector3(0.35f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmMinute, -5, "Alarm minute -5");
            CreateButton(board.transform, "AlarmMinuteUp", "分＋", new Vector3(0.75f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.052f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmMinute, 5, "Alarm minute +5");
            CreateButton(board.transform, "AlarmToggle", "ON/OFF", new Vector3(1.15f, 0.30f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.044f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmToggle, 0, "Alarm ON/OFF");
            CreateButton(board.transform, "AlarmStop", "STOP", new Vector3(1.15f, 0.05f, -0.0125f),
                new Vector3(0.34f, 0.20f, 0.019f), 0.046f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.AlarmStop, 0, "Stop alarm");

            Text radioState = CreateText(board.transform, "ラジオのUSE範囲 / RADIO USE AREA  OFF",
                new Vector3(0.15f, -0.28f, -0.028f), 0.050f, TextAnchor.UpperLeft,
                font, textMaterial, Color.white);
            radioState.transform.parent.name = "RadioStateCanvas";
            CreateButton(board.transform, "RadioUseToggle", "USE範囲 ON/OFF", new Vector3(0.94f, -0.50f, -0.0125f),
                new Vector3(0.72f, 0.23f, 0.019f), 0.044f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.RadioUseToggle, 0, "Show or hide radio USE area");
            Text saveState = CreateText(board.transform, "設定保存 / SAVE  OFF",
                new Vector3(0.15f, -0.74f, -0.028f), 0.050f, TextAnchor.UpperLeft,
                font, textMaterial, Color.white);
            saveState.transform.parent.name = "SaveStateCanvas";
            CreateButton(board.transform, "SaveToggle", "SAVE ON/OFF", new Vector3(0.94f, -0.96f, -0.0125f),
                new Vector3(0.72f, 0.23f, 0.019f), 0.044f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.SaveToggle, 0, "Save local settings ON/OFF");

            AudioSource alarmAudio = system.AddComponent<AudioSource>();
            alarmAudio.clip = alarmClip;
            alarmAudio.loop = true;
            alarmAudio.playOnAwake = false;
            alarmAudio.spatialBlend = 0f;
            alarmAudio.volume = 0.22f;

            GameObject[] mirrors = CreateMirrors(system.transform, mirrorMaterial);
            GameObject nightOverlay = CreateNightOverlay(system.transform, nightMaterial);
            WorldRadioSpeaker radio = ConfigureRadioSpeaker(controller, font, textMaterial);
            CreateTreeToggle(system.transform, buttonMaterial, font, textMaterial, controller);

            controller.settingsBoard = board;
            controller.nightOverlay = nightOverlay;
            controller.nightOverlayMaterial = nightMaterial;
            controller.nightSlider = slider;
            controller.mirrors = mirrors;
            controller.clockText = clock;
            controller.alarmText = alarmState;
            controller.mirrorText = mirrorState;
            controller.radioUseText = radioState;
            controller.saveText = saveState;
            controller.alarmAudio = alarmAudio;
            controller.radioSpeaker = radio;
            UdonSharpEditorUtility.CopyProxyToUdon(controller);
            EditorUtility.SetDirty(controller);

            board.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Stargazing Hill] Installed local settings board, five-position mirror, night mode, alarm, clock, and radio speaker.");
        }

        internal static void ValidateScene(Scene scene)
        {
            GameObject system = GameObject.Find("World/SettingsSystem");
            WorldSettingsController controller = system != null ? system.GetComponent<WorldSettingsController>() : null;
            WorldSettingsButton[] buttons = system != null
                ? system.GetComponentsInChildren<WorldSettingsButton>(true)
                : new WorldSettingsButton[0];
            GameObject board = system != null ? system.transform.Find("LocalSettingsBoard")?.gameObject : null;
            VRCPickup boardPickup = board != null ? board.GetComponent<VRCPickup>() : null;
            if (controller == null || board == null || board.activeSelf ||
                buttons.Length != 15 ||
                controller.nightSlider == null || controller.mirrors == null || controller.mirrors.Length != 5 ||
                controller.radioSpeaker == null || controller.alarmAudio == null ||
                boardPickup == null || board.GetComponent<WorldSettingsBoardPickup>() == null)
                throw new InvalidOperationException("Local settings system validation failed.");
            if (Mathf.Abs(boardPickup.proximity - 0.35f) > 0.001f ||
                !string.IsNullOrEmpty(boardPickup.UseText) || board.GetComponent<VRCObjectSync>() != null)
                throw new InvalidOperationException("Local settings board pickup must remain local and UI-safe.");
            if (Mathf.Abs(board.transform.localScale.x - BoardScale) > 0.0001f ||
                Quaternion.Angle(board.transform.rotation, Quaternion.Euler(BoardEuler)) > 0.01f)
                throw new InvalidOperationException("Local settings board size or facing validation failed.");
            BoxCollider boardGrip = board.GetComponent<BoxCollider>();
            if (boardGrip == null || boardGrip.size.y > 0.30f)
                throw new InvalidOperationException("Local settings board pickup grip overlaps the control area.");
            for (int index = 0; index < controller.mirrors.Length; index++)
                if (controller.mirrors[index] == null || controller.mirrors[index].activeSelf ||
                    controller.mirrors[index].GetComponent<VRCMirrorReflection>() == null)
                    throw new InvalidOperationException("Local mirror validation failed at index " + index + ".");
            for (int index = 0; index < buttons.Length; index++)
                if (UdonSharpEditorUtility.GetBackingUdonBehaviour(buttons[index]) == null ||
                    buttons[index].GetComponentInChildren<VRCUiShape>(true) == null)
                    throw new InvalidOperationException(
                        "Local settings button has no backing Udon behaviour or VR UI beam target: " +
                        buttons[index].name);
            Transform blanket = GameObject.Find("World/Environment/PicnicSpot/PicnicBlanketBlue")?.transform;
            if (blanket == null || blanket.GetComponentsInChildren<MeshCollider>(true).Length != 1)
                throw new InvalidOperationException("Picnic blanket collider validation failed.");
            if (controller.radioSpeaker.interactionCollider == null ||
                controller.radioSpeaker.interactionCollider.enabled ||
                controller.radioSpeaker.stateText == null || controller.radioSpeaker.stateText.gameObject.activeSelf)
                throw new InvalidOperationException("Radio USE area must be hidden by default.");
            CompassSceneInstaller.ValidateScene();
            Debug.Log("[Stargazing Hill] Local settings system validation passed.");
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

        private static WorldRadioSpeaker ConfigureRadioSpeaker(WorldSettingsController controller,
            Font font, Material textMaterial)
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
            source.minDistance = 0.35f;
            source.maxDistance = 18f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.enabled = false;
            YamaPlayerSpeaker yamaSpeaker = speakerObject.AddComponent<YamaPlayerSpeaker>();
            yamaSpeaker.controller = yama;
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

            GameObject trigger = new GameObject("RadioUseTrigger");
            trigger.transform.SetParent(radio, false);
            Bounds radioBounds = CalculateLocalRendererBounds(radio);
            trigger.transform.localPosition = radioBounds.center;
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                Mathf.Max(0.15f, radioBounds.size.x * 1.08f),
                Mathf.Max(0.12f, radioBounds.size.y * 1.08f),
                Mathf.Max(0.12f, radioBounds.size.z * 1.08f));
            collider.isTrigger = true;
            collider.enabled = false;
            WorldRadioSpeaker radioBehaviour = UdonSharpUndo.AddComponent<WorldRadioSpeaker>(trigger);
            radioBehaviour.speakerSource = source;
            radioBehaviour.interactionCollider = collider;
            Text state = CreateText(trigger.transform, "RADIO LOCKED", new Vector3(0f, radioBounds.extents.y + 0.12f, 0f),
                0.15f, TextAnchor.MiddleCenter, font, textMaterial, new Color(0.58f, 0.90f, 1f));
            state.gameObject.SetActive(false);
            radioBehaviour.stateText = state;
            UdonSharpEditorUtility.CopyProxyToUdon(radioBehaviour);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(radioBehaviour);
            if (backing != null)
            {
                backing.InteractionText = "ラジオ音声 ON/OFF / Radio speaker ON/OFF";
                backing.proximity = 0.6f;
                EditorUtility.SetDirty(backing);
            }
            EditorUtility.SetDirty(radioBehaviour);
            return radioBehaviour;
        }

        private static Bounds CalculateLocalRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Bounds bounds = renderers[rendererIndex].bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 world = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    Vector3 local = root.InverseTransformPoint(world);
                    if (!initialized)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else localBounds.Encapsulate(local);
                }
            }
            if (!initialized) throw new InvalidOperationException("Radio renderer bounds are missing.");
            return localBounds;
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

        private static GameObject[] CreateMirrors(Transform parent, Material material)
        {
            string[] names = { "Up", "Down", "Left", "Right", "Ceiling" };
            Vector3[] positions =
            {
                new Vector3(6.50f, 3.45f, 8.45f), new Vector3(6.50f, 3.05f, 4.35f),
                new Vector3(4.35f, 3.25f, 6.40f), new Vector3(8.65f, 3.25f, 6.40f),
                new Vector3(6.50f, 4.55f, 6.40f)
            };
            Vector3[] rotations =
            {
                new Vector3(0f, 180f, 0f), new Vector3(0f, 0f, 0f),
                new Vector3(0f, 90f, 0f), new Vector3(0f, -90f, 0f),
                new Vector3(90f, 0f, 0f)
            };
            Vector3[] scales =
            {
                new Vector3(3.8f, 2.2f, 1f), new Vector3(3.8f, 2.2f, 1f),
                new Vector3(3.8f, 2.2f, 1f), new Vector3(3.8f, 2.2f, 1f),
                new Vector3(3.8f, 3.8f, 1f)
            };
            GameObject root = new GameObject("LocalPicnicMirrors");
            root.transform.SetParent(parent, false);
            GameObject[] result = new GameObject[names.Length];
            int reflectLayers = LayerMask.GetMask("Player", "PlayerLocal", "MirrorReflection");
            for (int index = 0; index < names.Length; index++)
            {
                GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mirror.name = "Mirror_" + names[index];
                mirror.transform.SetParent(root.transform, false);
                mirror.transform.position = positions[index];
                mirror.transform.rotation = Quaternion.Euler(rotations[index]);
                mirror.transform.localScale = scales[index];
                mirror.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(mirror.GetComponent<Collider>());
                VRCMirrorReflection reflection = mirror.AddComponent<VRCMirrorReflection>();
                SerializedObject serialized = new SerializedObject(reflection);
                SetBool(serialized, "m_DisablePixelLights", true);
                SetBool(serialized, "TurnOffMirrorOcclusion", false);
                SetInt(serialized, "m_ReflectLayers", reflectLayers);
                SetInt(serialized, "maximumAntialiasing", 1);
                SetInt(serialized, "cameraClearFlags", (int)CameraClearFlags.SolidColor);
                SerializedProperty clear = serialized.FindProperty("customClearColor");
                if (clear != null) clear.colorValue = new Color(0.003f, 0.008f, 0.018f, 1f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                mirror.SetActive(false);
                result[index] = mirror;
            }
            return result;
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

        private static void CreateTreeToggle(Transform parent, Material buttonMaterial, Font font,
            Material textMaterial, WorldSettingsController controller)
        {
            GameObject dock = new GameObject("TreeSettingsToggle");
            dock.transform.SetParent(parent, false);
            dock.transform.position = new Vector3(9.10f, 2.68f, 7.55f);
            dock.transform.rotation = Quaternion.Euler(0f, 22.2865f, 0f);
            CreateButton(dock.transform, "Toggle", "SETTINGS\n設定", Vector3.zero,
                new Vector3(0.56f, 0.34f, 0.05f), 0.068f, buttonMaterial, font, textMaterial,
                controller, WorldSettingsButton.BoardToggle, 0, "Local settings board ON/OFF");
        }

        private static Slider CreateNightSlider(Transform parent, Material uiMaterial, Material accentMaterial)
        {
            GameObject canvasObject = new GameObject("NightModeSliderCanvas");
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = new Vector3(-0.59f, -0.50f, -0.032f);
            canvasObject.transform.localScale = Vector3.one * CanvasScale;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(510f, 90f);
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<VRCUiShape>();

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
            handle.sizeDelta = new Vector2(56f, 82f);
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
            slider.value = 0f;
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
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("Unlit/Texture shader is missing.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = Color.white;
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
