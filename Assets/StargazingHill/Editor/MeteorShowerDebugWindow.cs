using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace StargazingHill.Editor
{
    public sealed class MeteorShowerDebugWindow : EditorWindow
    {
        private const string CatalogPath =
            "Assets/StargazingHill/Settings/IMO2026MajorShowers.asset";
        private const int PerseidsIndex = 4;

        private int _selectedShowerIndex = PerseidsIndex;

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        [MenuItem("Stargazing Hill/Preview & Debug/Meteor Shower Preview...", false, 50)]
        public static void OpenWindow()
        {
            MeteorShowerDebugWindow window = GetWindow<MeteorShowerDebugWindow>();
            window.titleContent = new GUIContent("Meteor Preview");
            window.minSize = new Vector2(390f, 250f);
            window.Show();
        }

        [MenuItem("Stargazing Hill/Preview & Debug/Force Perseids Preview (20 Meteors)", false, 51)]
        public static void ForcePerseidsPreview()
        {
            TriggerPreview(PerseidsIndex);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("流星群 強制プレビュー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play Mode中に、選んだ流星群を季節と放射点高度に関係なくローカル再生します。" +
                MeteorController.DebugForcedPreviewDurationSeconds + "秒間に20本（5秒ごとに4本）を生成し、" +
                "各波の1本目を現在の視線正面へ配置します。" +
                "開始時の1本だけはFireball表示を確実に検査できる階級へ固定します。",
                MessageType.Info);

            string[] names = GetShowerDisplayNames();
            _selectedShowerIndex = Mathf.Clamp(_selectedShowerIndex, 0, names.Length - 1);
            _selectedShowerIndex = EditorGUILayout.Popup("流星群", _selectedShowerIndex, names);

            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            UdonBehaviour backing = controller == null ? null :
                UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            bool ready = Application.isPlaying && backing != null && backing.IsInitialized && !backing.HasError;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("状態", GetRuntimeStatus(controller, backing));

            using (new EditorGUI.DisabledScope(!ready))
            {
                if (GUILayout.Button("選択した流星群を正面へ強制表示", GUILayout.Height(36f)))
                    TriggerPreview(_selectedShowerIndex);
                if (GUILayout.Button("ペルセウス座流星群を強制表示"))
                    TriggerPreview(PerseidsIndex);
                if (GUILayout.Button("停止"))
                    backing.SendCustomEvent(nameof(MeteorController.DebugStopHourlyEvent));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "このプレビューは自分のEditorだけに表示され、他プレイヤーへ同期しません。" +
                "通常の毎時イベントの日時・活動度・放射点計算には影響しません。",
                MessageType.None);
        }

        private static void TriggerPreview(int showerIndex)
        {
            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            if (!Application.isPlaying || controller == null)
            {
                Debug.LogWarning(
                    "[Stargazing Hill] Open the generated scene and enter Play Mode before forcing a meteor shower.");
                return;
            }

            Vector3 viewForward = GetCurrentViewForward();
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            if (backing == null || !backing.IsInitialized || backing.HasError)
            {
                Debug.LogError("[Stargazing Hill] Meteor preview UdonBehaviour is not ready. " +
                               "Exit Play Mode, clear the Console, and enter Play Mode again.");
                return;
            }

            backing.SetProgramVariable(nameof(MeteorController.debugRequestedShowerIndex), showerIndex);
            backing.SetProgramVariable(nameof(MeteorController.debugRequestedViewForward), viewForward);
            backing.SendCustomEvent(nameof(MeteorController.DebugTriggerSelectedShower));
            SceneView.RepaintAll();
        }

        private static Vector3 GetCurrentViewForward()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
                return localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;

            Camera mainCamera = Camera.main;
            if (mainCamera != null) return mainCamera.transform.forward;

            Camera[] cameras = Camera.allCameras;
            for (int index = 0; index < cameras.Length; index++)
                if (cameras[index] != null && cameras[index].name == "PlayerCamera")
                    return cameras[index].transform.forward;

            GameObject referenceCamera = GameObject.Find("World/WorldSettings/ReferenceCamera");
            if (referenceCamera != null)
            {
                Camera camera = referenceCamera.GetComponent<Camera>();
                if (camera != null) return camera.transform.forward;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
                return sceneView.camera.transform.forward;
            return Vector3.forward;
        }

        private static string GetRuntimeStatus(MeteorController controller, UdonBehaviour backing)
        {
            if (!Application.isPlaying) return "Play Modeに入ってください";
            if (controller == null || backing == null) return "MeteorController / UdonBehaviour が見つかりません";
            if (backing.HasError) return "UdonBehaviour停止（ConsoleのUdonエラーを確認）";
            if (!backing.IsInitialized) return "Udon初期化待ち";

            object activeValue = backing.GetProgramVariable(nameof(MeteorController.debugPreviewActive));
            bool active = activeValue is bool && (bool)activeValue;
            if (!active) return "実行可能（現在は停止中）";

            string showerId = backing.GetProgramVariable(nameof(MeteorController.debugPreviewShowerId)) as string;
            object elapsedValue = backing.GetProgramVariable(nameof(MeteorController.debugPreviewElapsedSeconds));
            object visibleValue = backing.GetProgramVariable(nameof(MeteorController.debugVisibleMeteorCount));
            float elapsed = elapsedValue is float ? (float)elapsedValue : -1f;
            int visible = visibleValue is int ? (int)visibleValue : -1;
            return "再生中: " + showerId + "  " + elapsed.ToString("F1") + " / " +
                   MeteorController.DebugForcedPreviewDurationSeconds.ToString("F1") + "秒  表示中 " + visible + "本";
        }

        private static string[] GetShowerDisplayNames()
        {
            MeteorShowerCatalog catalog = AssetDatabase.LoadAssetAtPath<MeteorShowerCatalog>(CatalogPath);
            if (catalog != null && catalog.namesJa != null && catalog.ids != null &&
                catalog.namesJa.Length == catalog.ids.Length && catalog.namesJa.Length > 0)
            {
                string[] labels = new string[catalog.namesJa.Length];
                for (int index = 0; index < labels.Length; index++)
                    labels[index] = catalog.namesJa[index] + " / " + catalog.ids[index];
                return labels;
            }

            return new[] { "PERSEIDS" };
        }
    }

    [InitializeOnLoad]
    public static class ClientSimMeteorDebugVerifier
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string ActiveKey = "StargazingHill.MeteorDebugVerifier.Active";
        private const string StageKey = "StargazingHill.MeteorDebugVerifier.Stage";
        private const string NetworkingErrorKey = "StargazingHill.MeteorDebugVerifier.NetworkingError";

        private static double _deadline;
        private static double _triggerTime;
        private static bool _attached;

        static ClientSimMeteorDebugVerifier()
        {
            if (SessionState.GetBool(ActiveKey, false)) Attach();
        }

        public static void RunForBatchMode()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("ClientSim meteor verifier must start outside Play Mode.");

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetBool(NetworkingErrorKey, false);
            Attach();
            _deadline = EditorApplication.timeSinceStartup + 45.0;
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            if (_attached) return;
            _attached = true;
            _deadline = EditorApplication.timeSinceStartup + 45.0;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void Detach()
        {
            if (!_attached) return;
            _attached = false;
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Application.logMessageReceived -= OnLogMessage;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetInt(StageKey, 1);
                _deadline = EditorApplication.timeSinceStartup + 45.0;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(StageKey, 0) == 3)
            {
                Finish(0);
            }
        }

        private static void Update()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            int stage = SessionState.GetInt(StageKey, 0);
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                Fail("Timed out waiting for ClientSim meteor preview stage " + stage + ".");
                return;
            }

            if (!Application.isPlaying || stage < 1) return;
            if (stage == 1)
            {
                MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
                UdonBehaviour backing = controller == null ? null :
                    UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
                VRCPlayerApi player = Networking.LocalPlayer;
                if (backing == null || !backing.IsInitialized || backing.HasError || player == null) return;

                Vector3 viewForward = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation *
                                      Vector3.forward;
                backing.SetProgramVariable(nameof(MeteorController.debugRequestedShowerIndex), 4);
                backing.SetProgramVariable(nameof(MeteorController.debugRequestedViewForward), viewForward);
                backing.SendCustomEvent(nameof(MeteorController.DebugTriggerSelectedShower));
                SessionState.SetInt(StageKey, 2);
                _triggerTime = EditorApplication.timeSinceStartup;
                return;
            }

            if (stage != 2 || EditorApplication.timeSinceStartup - _triggerTime < 0.35) return;
            VerifyRuntimeState();
        }

        private static void VerifyRuntimeState()
        {
            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            UdonBehaviour backing = controller == null ? null :
                UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            if (backing == null) { Fail("Meteor backing UdonBehaviour was not found in Play Mode."); return; }

            bool active = (bool)backing.GetProgramVariable(nameof(MeteorController.debugPreviewActive));
            int visible = (int)backing.GetProgramVariable(nameof(MeteorController.debugVisibleMeteorCount));
            string showerId = (string)backing.GetProgramVariable(nameof(MeteorController.debugPreviewShowerId));
            bool rendererVisible = controller.meteorRenderers != null && controller.meteorRenderers.Length > 0 &&
                                   controller.meteorRenderers[0].enabled;
            if (!active || visible < 1 || showerId != "PERSEIDS" || !rendererVisible)
            {
                Fail("Backing Udon meteor trigger failed: active=" + active + ", visible=" + visible +
                     ", shower=" + showerId + ", renderer=" + rendererVisible + ".");
                return;
            }

            if (SessionState.GetBool(NetworkingErrorKey, false))
            {
                Fail("ClientSim raised IsNetworkingSupported after UdonBehaviour initialization.");
                return;
            }

            Debug.Log("[Stargazing Hill] ClientSim meteor debug passed through backing UdonBehaviour: " +
                      showerId + ", visible=" + visible + ".");
            backing.SendCustomEvent(nameof(MeteorController.DebugStopHourlyEvent));
            SessionState.SetInt(StageKey, 3);
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (condition.Contains("IsNetworkingSupported cannot be changed after"))
                SessionState.SetBool(NetworkingErrorKey, true);
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Stargazing Hill] ClientSim meteor debug failed: " + message);
            if (Application.isPlaying)
            {
                SessionState.SetInt(StageKey, 4);
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => Finish(1);
            }
            else
            {
                Finish(1);
            }
        }

        private static void Finish(int exitCode)
        {
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(StageKey);
            SessionState.EraseBool(NetworkingErrorKey);
            Detach();
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }
    }
}
