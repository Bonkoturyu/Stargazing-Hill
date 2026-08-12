using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    public sealed class MeteorShowerDebugWindow : EditorWindow
    {
        private const string CatalogPath =
            "Assets/StargazingHill/Settings/IMO2026MajorShowers.asset";
        private const int PerseidsIndex = 4;

        private int _selectedShowerIndex = PerseidsIndex;

        [MenuItem("Stargazing Hill/Debug/Meteor Shower Preview...", false, 45)]
        public static void OpenWindow()
        {
            MeteorShowerDebugWindow window = GetWindow<MeteorShowerDebugWindow>();
            window.titleContent = new GUIContent("Meteor Preview");
            window.minSize = new Vector2(390f, 250f);
            window.Show();
        }

        [MenuItem("Stargazing Hill/Debug/Force Perseids Preview (20 Meteors)", false, 46)]
        public static void ForcePerseidsPreview()
        {
            TriggerPreview(PerseidsIndex);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("流星群 強制プレビュー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play Mode中に、選んだ流星群を季節と放射点高度に関係なくローカル再生します。" +
                "25秒間に20本（5秒ごとに4本）を生成し、各波の1本目を現在の視線正面へ配置します。",
                MessageType.Info);

            string[] names = GetShowerDisplayNames();
            _selectedShowerIndex = Mathf.Clamp(_selectedShowerIndex, 0, names.Length - 1);
            _selectedShowerIndex = EditorGUILayout.Popup("流星群", _selectedShowerIndex, names);

            MeteorController controller = Object.FindObjectOfType<MeteorController>(true);
            bool ready = Application.isPlaying && controller != null;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("状態", Application.isPlaying ?
                controller == null ? "MeteorController が見つかりません" : "実行可能" :
                "Play Modeに入ってください");

            using (new EditorGUI.DisabledScope(!ready))
            {
                if (GUILayout.Button("選択した流星群を正面へ強制表示", GUILayout.Height(36f)))
                    TriggerPreview(_selectedShowerIndex);
                if (GUILayout.Button("ペルセウス座流星群を強制表示"))
                    TriggerPreview(PerseidsIndex);
                if (GUILayout.Button("停止"))
                    controller.DebugStopHourlyEvent();
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
            controller.DebugTriggerSelectedShower(showerIndex, viewForward);
            SceneView.RepaintAll();
        }

        private static Vector3 GetCurrentViewForward()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null) return mainCamera.transform.forward;

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
}
