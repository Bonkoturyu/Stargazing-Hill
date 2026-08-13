using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.Udon;

namespace StargazingHill.Editor
{
    public static class WorldInformationPanelInstaller
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string RootPath = "Assets/StargazingHill";
        private const string FontPath = "Packages/net.kwxxw.yama-stream/Assets/Fonts/ZenMaruGothic-Regular.ttf";
        private const string PanelName = "WorldInformationPanel";
        private const float CanvasScale = 0.002f;
        private static readonly Vector3 Position = new Vector3(0.70f, 1.78f, -25.92f);
        private static readonly Vector3 Euler = new Vector3(0f, 202.2865f, 0f);

        [MenuItem("Stargazing Hill/Install or Refresh World Information Panel", false, 31)]
        public static void InstallOrRefreshMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void InstallForBuild(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            GameObject world = GameObject.Find("World");
            if (world == null) throw new InvalidOperationException("World root is missing.");
            Transform old = world.transform.Find("InformationSystem");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

            StargazingWorldBuilder.EnsureInformationPanelProgramAssets();
            Font font = EnsureFont();
            Material textMaterial = EnsureTextMaterial();
            Material boardMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/WorldInfoPanel.mat",
                new Color(0.018f, 0.028f, 0.050f, 1f));
            Material buttonMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/WorldInfoButton.mat",
                new Color(0.08f, 0.17f, 0.28f, 1f));

            GameObject system = new GameObject("InformationSystem");
            system.transform.SetParent(world.transform, false);
            GameObject panel = new GameObject(PanelName);
            panel.transform.SetParent(system.transform, false);
            panel.transform.position = Position;
            panel.transform.rotation = Quaternion.Euler(Euler);

            GameObject sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "PanelSheet";
            sheet.transform.SetParent(panel.transform, false);
            sheet.transform.localScale = new Vector3(4.4f, 2.5f, 0.04f);
            sheet.GetComponent<Renderer>().sharedMaterial = boardMaterial;
            UnityEngine.Object.DestroyImmediate(sheet.GetComponent<Collider>());

            CreateText(panel.transform, "STARGAZING HILL / 星見の丘", new Vector3(-2.0f, 1.02f, -0.026f),
                0.15f, TextAnchor.UpperLeft, font, textMaterial, new Color(0.78f, 0.90f, 1f));
            Text japanese = CreateText(panel.transform,
                "東京の現在時刻に連動した\n星空と月を眺める、静かな草原です。\n\n毎時00分から流星イベントが始まります。\n季節の流星群がない時間は\n散在流星が空を横切ります。\n\n動画・ペン・V睡・写真撮影にどうぞ。\nPC / Android / iOS 対応",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.102f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            japanese.gameObject.transform.parent.name = "JapaneseDescription";
            Text english = CreateText(panel.transform,
                "A quiet grassland beneath a real sky and Moon,\naligned with the current time in Tokyo.\n\nA meteor event begins at the top of every hour.\nSeasonal showers appear when active;\notherwise sporadic meteors cross the sky.\n\nRelax, chat, sleep in VR, draw, or take photos.\nPC / Android / iOS",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.096f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            english.gameObject.transform.parent.name = "EnglishDescription";
            english.gameObject.transform.parent.gameObject.SetActive(false);

            Text count = CreateText(panel.transform, "ONLINE  0 / 32", new Vector3(0.73f, 0.50f, -0.027f),
                0.115f, TextAnchor.UpperLeft, font, textMaterial, new Color(0.58f, 0.90f, 1f));
            Text history = CreateText(panel.transform, "JOIN / LEAVE HISTORY", new Vector3(0.73f, 0.18f, -0.027f),
                0.085f, TextAnchor.UpperLeft, font, textMaterial, new Color(0.76f, 0.86f, 0.96f));
            WorldPresenceBoard presence = UdonSharpUndo.AddComponent<WorldPresenceBoard>(panel);
            presence.playerCountText = count;
            presence.historyText = history;
            presence.maximumCapacity = 32;
            presence.historyCapacity = 7;
            UdonSharpEditorUtility.CopyProxyToUdon(presence);
            EditorUtility.SetDirty(presence);

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "LanguageToggle";
            button.transform.SetParent(panel.transform, false);
            // The visible face is flush with the board's -Z face; only this intentional button has a collider.
            button.transform.localPosition = new Vector3(1.72f, 0.98f, -0.0105f);
            button.transform.localScale = new Vector3(0.72f, 0.22f, 0.019f);
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            button.GetComponent<BoxCollider>().isTrigger = true;
            Text buttonLabel = CreateText(button.transform, "ENGLISH", new Vector3(0f, 0f, -0.53f),
                0.10f, TextAnchor.MiddleCenter, font, textMaterial, Color.white, true);
            WorldInfoLanguageToggle toggle = UdonSharpUndo.AddComponent<WorldInfoLanguageToggle>(button);
            toggle.japaneseText = japanese.transform.parent.gameObject;
            toggle.englishText = english.transform.parent.gameObject;
            toggle.buttonLabel = buttonLabel;
            UdonSharpEditorUtility.CopyProxyToUdon(toggle);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(toggle);
            if (backing != null)
            {
                backing.InteractionText = "日本語 / English";
                backing.proximity = 2.5f;
                EditorUtility.SetDirty(backing);
            }
            EditorUtility.SetDirty(toggle);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        internal static Font EnsureFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) throw new InvalidOperationException("YamaPlayer Japanese font is missing: " + FontPath);
            return font;
        }

        internal static Material EnsureTextMaterial()
        {
            string path = RootPath + "/Generated/Materials/WorldText.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("VRChat/Mobile/Worlds/Supersampled UI");
            if (shader == null) throw new InvalidOperationException("VRChat mobile supersampled UI shader is missing.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Text CreateText(Transform parent, string value, Vector3 position, float height,
            TextAnchor anchor, Font font, Material material, Color color, bool compensateParentScale = false)
        {
            GameObject canvasObject = new GameObject("TextCanvas");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            // Unity UI faces the panel's -Z reader side at identity rotation. Rotating this
            // canvas 180 degrees makes every label readable only from behind and mirrored.
            canvasObject.transform.localRotation = Quaternion.identity;
            Vector3 scale = Vector3.one * CanvasScale;
            if (compensateParentScale)
                scale = new Vector3(CanvasScale / parent.localScale.x, CanvasScale / parent.localScale.y, CanvasScale);
            canvasObject.transform.localScale = scale;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(canvasObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.Max(1, Mathf.RoundToInt(height / CanvasScale));
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            text.color = color;
            text.material = material;
            RectTransform rect = text.rectTransform;
            rect.sizeDelta = new Vector2(2000f, 1100f);
            if (anchor == TextAnchor.UpperLeft) rect.pivot = new Vector2(0f, 1f);
            else rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return text;
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
    }
}
