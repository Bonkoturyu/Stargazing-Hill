using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;

namespace StargazingHill.Editor
{
    public static class WorldInformationPanelInstaller
    {
        private const string ScenePath = "Assets/StargazingHill/Scenes/StargazingHill.unity";
        private const string RootPath = "Assets/StargazingHill";
        private const string FontPath = "Assets/StargazingHill/ThirdParty/Fonts/NotoSansCJKkr-Regular.otf";
        private const string PanelName = "WorldInformationPanel";
        private const string VisualGroupPath = "Visual";
        private const string DescriptionsGroupPath = "Visual/Descriptions";
        private const string PresenceGroupPath = "Visual/Presence";
        private const string ControlsGroupPath = "Controls";
        private const string ObservatoryGroupPath = "Controls/Observatory";
        private const float CanvasScale = 0.002f;
        // World-space UI stays on the Default layer; see ConfigureWorldUiCanvas for why.
        internal const int WorldUiLayer = 0;
        internal const float UiTargetWorldDepth = 0.004f;
        private const string BeamTargetName = "UiBeamTarget";
        private const float PresenceTop = 0.88f;
        private const float HistoryTop = 0.31f;
        private const float HistoryHeightPixels = 610f;
        private const float LaptopIconTop = 0.584f;
        private const float MobileIconTop = 0.456f;
        private const float PanelTop = 1.25f;
        private const float PanelBottom = -1.85f;
        private const float ObservatoryControlY = -1.52f;
        // Leave a clear strip above the selector row so the lowest tile and its hover text do not
        // cover the previous/selected/next controls when the three-column list is open.
        private const float ObservatoryListBottomY = -1.10f;
        private const float ObservatoryListRowSpacing = 0.20f;
        private const float ObservatoryListBackdropY = -0.40f;
        private static readonly string[] ObservatoryProfileIds =
        {
            "tokyo", "sapporo", "osaka", "takamatsu-kagawa", "oita", "miyazaki", "naha-okinawa",
            "rome", "paris", "moscow", "washington-dc", "san-francisco", "los-angeles", "las-vegas",
            "new-york", "ottawa", "canberra", "jakarta", "beijing", "seoul", "tottori", "matsue-shimane"
        };
        private static readonly string[] ObservatoryDisplayNames =
        {
            "Tokyo, Japan", "Sapporo, Japan", "Osaka, Japan", "Takamatsu, Kagawa, Japan",
            "Oita, Japan", "Miyazaki, Japan", "Naha, Okinawa, Japan", "Rome, Italy", "Paris, France",
            "Moscow, Russia", "Washington D.C., America", "San Francisco, America",
            "Los Angeles, America", "Las Vegas, America", "New York, America", "Ottawa, Canada",
            "Canberra, Australia", "Jakarta, Indonesia", "Beijing, China", "Seoul, Korea",
            "Tottori, Japan", "Matsue, Shimane, Japan"
        };
        private static readonly string[] ObservatoryDisplayNamesJapanese =
        {
            "東京（日本）", "札幌（日本）", "大阪（日本）", "高松・香川（日本）", "大分（日本）",
            "宮崎（日本）", "那覇・沖縄（日本）", "ローマ（イタリア）", "パリ（フランス）",
            "モスクワ（ロシア）", "ワシントンD.C.（アメリカ）", "サンフランシスコ（アメリカ）",
            "ロサンゼルス（アメリカ）", "ラスベガス（アメリカ）", "ニューヨーク（アメリカ）",
            "オタワ（カナダ）", "キャンベラ（オーストラリア）", "ジャカルタ（インドネシア）",
            "北京（中国）", "ソウル（韓国）", "鳥取（日本）", "松江・島根（日本）"
        };
        private static readonly string[] ObservatoryDisplayNamesTraditionalChinese =
        {
            "東京，日本", "札幌，日本", "大阪，日本", "高松（香川），日本", "大分，日本",
            "宮崎，日本", "那霸（沖繩），日本", "羅馬，義大利", "巴黎，法國", "莫斯科，俄羅斯",
            "華盛頓特區，美國", "舊金山，美國", "洛杉磯，美國", "拉斯維加斯，美國", "紐約，美國",
            "渥太華，加拿大", "坎培拉，澳洲", "雅加達，印尼", "北京，中國", "首爾，韓國",
            "鳥取，日本", "松江（島根），日本"
        };
        private static readonly string[] ObservatoryDisplayNamesSimplifiedChinese =
        {
            "东京，日本", "札幌，日本", "大阪，日本", "高松（香川），日本", "大分，日本",
            "宫崎，日本", "那霸（冲绳），日本", "罗马，意大利", "巴黎，法国", "莫斯科，俄罗斯",
            "华盛顿特区，美国", "旧金山，美国", "洛杉矶，美国", "拉斯维加斯，美国", "纽约，美国",
            "渥太华，加拿大", "堪培拉，澳大利亚", "雅加达，印度尼西亚", "北京，中国", "首尔，韩国",
            "鸟取，日本", "松江（岛根），日本"
        };
        private static readonly string[] ObservatoryDisplayNamesKorean =
        {
            "도쿄, 일본", "삿포로, 일본", "오사카, 일본", "다카마쓰(가가와), 일본", "오이타, 일본",
            "미야자키, 일본", "나하(오키나와), 일본", "로마, 이탈리아", "파리, 프랑스",
            "모스크바, 러시아", "워싱턴 D.C., 미국", "샌프란시스코, 미국", "로스앤젤레스, 미국",
            "라스베이거스, 미국", "뉴욕, 미국", "오타와, 캐나다", "캔버라, 호주",
            "자카르타, 인도네시아", "베이징, 중국", "서울, 한국", "돗토리, 일본", "마쓰에(시마네), 일본"
        };
        private static readonly string[] ObservatoryHeadingLabels =
        {
            "星空の基準地点  (global)",
            "SKY REFERENCE LOCATION  (global)",
            "星空基準地點  (global)",
            "星空基准地点  (global)",
            "별하늘 기준 위치  (global)"
        };
        private static readonly float[] ObservatoryLatitudes =
        {
            35.68f, 43.0618f, 34.6937f, 34.3428f, 33.2396f, 31.9077f, 26.2124f,
            41.9028f, 48.8566f, 55.7558f, 38.9072f, 37.7749f, 34.0522f, 36.1699f,
            40.7128f, 45.4215f, -35.2809f, -6.2088f, 39.9042f, 37.5665f, 35.5011f, 35.4681f
        };
        private static readonly float[] ObservatoryLongitudesEast =
        {
            139.76f, 141.3545f, 135.5023f, 134.0466f, 131.6093f, 131.4202f, 127.6809f,
            12.4964f, 2.3522f, 37.6173f, -77.0369f, -122.4194f, -118.2437f, -115.1398f,
            -74.0060f, -75.6972f, 149.1300f, 106.8456f, 116.4074f, 126.9780f, 134.2351f, 133.0484f
        };
        // UI order is independent from the stable synchronized catalog index. Keep Tokyo at index 0
        // as the default while grouping Japan first from north to south in the expanded tile list.
        private static readonly int[] ObservatoryVisualOrder =
        {
            1, 0, 20, 21, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
        };
        // Kept in sync with the hand-adjusted scene placement so a future full refresh
        // does not put the information board back at its older generated position.
        private static readonly Vector3 Position = new Vector3(1.471f, 1.999f, -26.29f);
        private static readonly Vector3 Euler = new Vector3(0f, 202.2865f, 0f);
        private static readonly Vector3 LanguageTogglePosition = new Vector3(1.03f, ObservatoryControlY, -0.0125f);
        private static readonly Vector3 LanguageToggleScale = new Vector3(0.56f, 0.18f, 0.019f);
        private static readonly Vector3 DebugTogglePosition = new Vector3(1.72f, ObservatoryControlY, -0.0125f);
        private static readonly Vector3 DebugToggleScale = new Vector3(0.72f, 0.18f, 0.019f);

        [MenuItem("Stargazing Hill/Content/Information Panel/Select in Hierarchy", false, 20)]
        public static void SelectInformationPanelMenu()
        {
            GameObject panel = GameObject.Find("World/InformationSystem/" + PanelName);
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Stargazing Hill", "WorldInformationPanel is not present in the open scene.", "OK");
                return;
            }
            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);
        }

        [MenuItem("Stargazing Hill/Content/Information Panel/Validate", false, 21)]
        public static void ValidateMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog("Stargazing Hill", "InformationSystem validation passed.", "OK");
        }

        [MenuItem("Stargazing Hill/Advanced/Generated Content/Rebuild InformationSystem (Replaces Children)...", false, 80)]
        public static void InstallOrRefreshMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild InformationSystem",
                    "This replaces World/InformationSystem from the versioned generator. " +
                    "Manual changes inside InformationSystem will be lost.",
                    "Rebuild", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldDebugPanelInstaller.InstallForBuild(scene);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Stargazing Hill/Content/Information Panel/Apply Saved Readability Layout", false, 22)]
        public static void ApplyReadabilityLayoutMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply Information Panel Layout",
                    "Apply the versioned panel, presence, icon, and control positions without rebuilding children?",
                    "Apply Layout", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyReadabilityLayout(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyReadabilityLayoutForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyReadabilityLayout(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void InstallOrRefreshForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldDebugPanelInstaller.InstallForBuild(scene);
            InstallForBuild(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ValidateForBatchMode()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("World information panel validation passed.");
        }

        internal static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            GameObject panel = GameObject.Find("World/InformationSystem/" + PanelName);
            if (panel == null || panel.GetComponent<Collider>() != null)
                throw new InvalidOperationException("World information panel root is missing or has a collider.");

            WorldInfoLanguageToggle languageToggle = panel.GetComponentInChildren<WorldInfoLanguageToggle>(true);
            WorldPresenceBoard presence = panel.GetComponent<WorldPresenceBoard>();
            WorldObservatorySelector selector = panel.GetComponent<WorldObservatorySelector>();
            WorldObservatoryButton[] buttons = panel.GetComponentsInChildren<WorldObservatoryButton>(true);
            Transform visualGroup = panel.transform.Find(VisualGroupPath);
            Transform descriptionsGroup = panel.transform.Find(DescriptionsGroupPath);
            Transform presenceGroup = panel.transform.Find(PresenceGroupPath);
            Transform controlsGroup = panel.transform.Find(ControlsGroupPath);
            Transform observatoryGroup = panel.transform.Find(ObservatoryGroupPath);
            Transform sheet = panel.transform.Find(VisualGroupPath + "/PanelSheet");
            Transform languageButton = panel.transform.Find(ControlsGroupPath + "/LanguageToggle");
            Transform laptopIcon = panel.transform.Find(PresenceGroupPath + "/PlatformLaptopIcon");
            Transform mobileIcon = panel.transform.Find(PresenceGroupPath + "/PlatformMobileIcon");
            Transform list = panel.transform.Find(ObservatoryGroupPath + "/ObservatoryLocationList");
            Transform heading = panel.transform.Find(ObservatoryGroupPath + "/ObservatoryHeading");
            Transform debugButton = panel.transform.Find(ControlsGroupPath + "/DebugPanelToggle");
            Transform firstVisualLocation = list != null ? list.Find("Location_01") : null;
            Transform lastJapaneseLocation = list != null ? list.Find("Location_06") : null;
            Transform lastVisualLocation = list != null ? list.Find("Location_19") : null;

            if (languageToggle == null || languageButton == null || presence == null ||
                languageButton.GetComponentInChildren<VRCUiShape>(true) == null ||
                presence.playerCountText == null || presence.historyText == null ||
                presence.historyScrollRect == null || selector == null || buttons.Length != 26 ||
                UdonSharpEditorUtility.GetBackingUdonBehaviour(selector) == null ||
                selector.profileIds == null || selector.profileIds.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.displayNames == null || selector.displayNames.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.displayNamesJapanese == null ||
                selector.displayNamesJapanese.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.displayNamesTraditionalChinese == null ||
                selector.displayNamesTraditionalChinese.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.displayNamesSimplifiedChinese == null ||
                selector.displayNamesSimplifiedChinese.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.displayNamesKorean == null ||
                selector.displayNamesKorean.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.latitudeDegrees == null || selector.latitudeDegrees.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.longitudeDegreesEast == null ||
                selector.longitudeDegreesEast.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.selectionOrder == null ||
                selector.selectionOrder.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.observatoryHeadingLabel == null ||
                selector.localizedHeadingLabels == null || selector.localizedHeadingLabels.Length != 5 ||
                selector.selectedLocationLabel == null ||
                selector.selectedLocationLabel.text.Contains("(global)") ||
                selector.locationListLabels == null ||
                selector.locationListLabels.Length != WorldObservatorySelector.ExpectedLocationCount ||
                selector.locationListRoot != (list != null ? list.gameObject : null) ||
                selector.skyController == null || selector.meteorController == null ||
                selector.debugPanelRoot == null || selector.debugToggleLabel == null ||
                visualGroup == null || descriptionsGroup == null || presenceGroup == null ||
                controlsGroup == null || observatoryGroup == null ||
                descriptionsGroup.Find("TitleCanvas") == null ||
                descriptionsGroup.Find("JapaneseDescription") == null ||
                descriptionsGroup.Find("EnglishDescription") == null ||
                descriptionsGroup.Find("TraditionalChineseDescription") == null ||
                descriptionsGroup.Find("SimplifiedChineseDescription") == null ||
                descriptionsGroup.Find("KoreanDescription") == null ||
                sheet == null || sheet.localScale.y < 3.09f ||
                panel.transform.Find(ObservatoryGroupPath + "/ObservatoryPrevious") == null ||
                panel.transform.Find(ObservatoryGroupPath + "/ObservatorySelected") == null ||
                panel.transform.Find(ObservatoryGroupPath + "/ObservatoryNext") == null ||
                heading == null || debugButton == null ||
                languageToggle.traditionalChineseText == null ||
                languageToggle.simplifiedChineseText == null || languageToggle.koreanText == null ||
                languageToggle.observatorySelector != selector ||
                firstVisualLocation == null || lastJapaneseLocation == null || lastVisualLocation == null ||
                !Mathf.Approximately(firstVisualLocation.localPosition.x, -1.38f) ||
                !Mathf.Approximately(firstVisualLocation.localPosition.y, ObservatoryListBottomY) ||
                !Mathf.Approximately(lastJapaneseLocation.localPosition.x, 1.38f) ||
                !Mathf.Approximately(lastJapaneseLocation.localPosition.y,
                    ObservatoryListBottomY + 2f * ObservatoryListRowSpacing) ||
                !Mathf.Approximately(lastVisualLocation.localPosition.x, -1.38f) ||
                !Mathf.Approximately(lastVisualLocation.localPosition.y,
                    ObservatoryListBottomY + 7f * ObservatoryListRowSpacing))
                throw new InvalidOperationException("World information panel global observatory controls are incomplete.");

            RectTransform countCanvas = presence.playerCountText.transform.parent as RectTransform;
            RectTransform historyCanvas = presence.historyScrollRect.GetComponent<RectTransform>();
            if (countCanvas == null ||
                !Mathf.Approximately(countCanvas.anchoredPosition.x, 0.73f) ||
                !Mathf.Approximately(countCanvas.anchoredPosition.y, PresenceTop) ||
                historyCanvas == null ||
                !Mathf.Approximately(historyCanvas.anchoredPosition.x, 0.73f) ||
                !Mathf.Approximately(historyCanvas.anchoredPosition.y, HistoryTop) ||
                !Mathf.Approximately(historyCanvas.sizeDelta.y, HistoryHeightPixels) ||
                !Mathf.Approximately(languageButton.localPosition.x, LanguageTogglePosition.x) ||
                !Mathf.Approximately(languageButton.localPosition.y, LanguageTogglePosition.y) ||
                !Mathf.Approximately(languageButton.localScale.x, LanguageToggleScale.x) ||
                !Mathf.Approximately(debugButton.localPosition.x, DebugTogglePosition.x) ||
                !Mathf.Approximately(debugButton.localPosition.y, DebugTogglePosition.y) ||
                !Mathf.Approximately(debugButton.localScale.x, DebugToggleScale.x) ||
                laptopIcon == null || !Mathf.Approximately(laptopIcon.localPosition.x, 0.79f) ||
                !Mathf.Approximately(laptopIcon.localPosition.y, LaptopIconTop) ||
                mobileIcon == null || !Mathf.Approximately(mobileIcon.localPosition.x, 0.79f) ||
                !Mathf.Approximately(mobileIcon.localPosition.y, MobileIconTop))
                throw new InvalidOperationException("Existing information panel UI coordinates changed unexpectedly.");

            for (int index = 0; index < buttons.Length; index++)
                if (UdonSharpEditorUtility.GetBackingUdonBehaviour(buttons[index]) == null ||
                    buttons[index].GetComponentInChildren<VRCUiShape>(true) == null)
                    throw new InvalidOperationException(
                        "Observatory selector button has no backing Udon behaviour or VR UI beam target: " +
                        buttons[index].name);
            ValidateWorldUiTargets(panel);

            // The debug panel is a scene root and is inactive by default, so it is looked up rather
            // than found through the hierarchy above.
            GameObject debugPanel = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                if (roots[index].name == "VRDebugPanel") debugPanel = roots[index];
            if (debugPanel != null) ValidateWorldUiTargets(debugPanel);
        }

        /// <summary>
        /// Every VRChat UI surface under a panel must sit on an interactive layer and carry a collider.
        /// The UI layer is removed from the interactive mask whenever the main menu is closed, so a
        /// canvas left there is dead to the pointer and absent from in-game photographs.
        /// </summary>
        internal static void ValidateWorldUiTargets(GameObject root)
        {
            VRCUiShape[] shapes = root.GetComponentsInChildren<VRCUiShape>(true);
            if (shapes.Length == 0)
                throw new InvalidOperationException("Panel has no VRChat UI surfaces: " + root.name);
            for (int index = 0; index < shapes.Length; index++)
            {
                GameObject canvasObject = shapes[index].gameObject;
                BoxCollider target = canvasObject.GetComponent<BoxCollider>();
                if (canvasObject.layer != WorldUiLayer || target == null || !target.isTrigger)
                    throw new InvalidOperationException(
                        "World UI surface is unreachable by the VRChat pointer: " +
                        root.name + "/" + canvasObject.name + " (layer " + canvasObject.layer +
                        ", collider " + (target != null) + ").");
            }
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
            Material accentMaterial = EnsureColorMaterial(RootPath + "/Generated/Materials/WorldInfoAccent.mat",
                new Color(0.58f, 0.90f, 1f, 1f));

            GameObject system = new GameObject("InformationSystem");
            system.transform.SetParent(world.transform, false);
            GameObject panel = new GameObject(PanelName);
            panel.transform.SetParent(system.transform, false);
            panel.transform.position = Position;
            panel.transform.rotation = Quaternion.Euler(Euler);

            Transform visualGroup = CreateGroup(panel.transform, "Visual");
            Transform descriptionsGroup = CreateGroup(visualGroup, "Descriptions");
            Transform presenceGroup = CreateGroup(visualGroup, "Presence");
            Transform controlsGroup = CreateGroup(panel.transform, "Controls");
            Transform observatoryGroup = CreateGroup(controlsGroup, "Observatory");

            GameObject sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "PanelSheet";
            sheet.transform.SetParent(visualGroup, false);
            sheet.transform.localPosition = new Vector3(0f, (PanelTop + PanelBottom) * 0.5f, 0f);
            sheet.transform.localScale = new Vector3(4.4f, PanelTop - PanelBottom, 0.04f);
            sheet.GetComponent<Renderer>().sharedMaterial = boardMaterial;
            UnityEngine.Object.DestroyImmediate(sheet.GetComponent<Collider>());

            Text title = CreateText(descriptionsGroup, "STARGAZING HILL / 星見の丘", new Vector3(-2.0f, 1.02f, -0.026f),
                0.15f, TextAnchor.UpperLeft, font, textMaterial, new Color(0.78f, 0.90f, 1f));
            title.transform.parent.name = "TitleCanvas";
            Text japanese = CreateText(descriptionsGroup,
                "現実の時刻と、全員で共有する観測地点に連動した\n星空と月を眺める、静かな草原です。\n\n毎時00分から流星イベントが始まります。\n季節の流星群が活動中でない時は\n散在流星が空を横切ります。\n\nV睡・雑談・動画鑑賞・お絵描き・写真撮影にどうぞ。\nPC / Android / iOS 対応",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.102f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            japanese.gameObject.transform.parent.name = "JapaneseDescription";
            Text english = CreateText(descriptionsGroup,
                "A quiet grassland beneath a real-time sky and Moon,\naligned to a globally shared observation location.\n\nA meteor event begins at the top of every hour.\nSeasonal showers appear when active;\notherwise sporadic meteors cross the sky.\n\nRelax, chat, sleep in VR, watch videos, draw, or take photos.\nPC / Android / iOS",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.096f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            english.gameObject.transform.parent.name = "EnglishDescription";
            english.gameObject.transform.parent.gameObject.SetActive(false);
            Text traditionalChinese = CreateText(descriptionsGroup,
                "在寧靜草原中仰望星空與月亮，\n景象會依照目前時間與全體共享的觀測地點呈現。\n\n每逢整點會開始流星事件。\n有活躍的季節性流星雨時會顯示流星雨；\n其他時間則會有零星流星劃過天空。\n\n歡迎放鬆、聊天、VR睡眠、看影片、繪圖或拍照。\n支援 PC / Android / iOS",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.092f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            traditionalChinese.gameObject.transform.parent.name = "TraditionalChineseDescription";
            traditionalChinese.gameObject.transform.parent.gameObject.SetActive(false);
            Text simplifiedChinese = CreateText(descriptionsGroup,
                "在宁静草原上仰望星空和月亮，\n景象会根据当前时间和全体共享的观测地点显示。\n\n每逢整点会开始流星事件。\n有活跃的季节性流星雨时会显示流星雨；\n其他时间则会有零星流星划过天空。\n\n欢迎放松、聊天、VR睡眠、观看视频、绘图或拍照。\n支持 PC / Android / iOS",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.092f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            simplifiedChinese.gameObject.transform.parent.name = "SimplifiedChineseDescription";
            simplifiedChinese.gameObject.transform.parent.gameObject.SetActive(false);
            Text korean = CreateText(descriptionsGroup,
                "현재 시각과 모두가 공유하는 관측 지점에 맞춘\n별하늘과 달을 바라보는 조용한 초원입니다.\n\n매 정각에 유성 이벤트가 시작됩니다.\n활동 중인 계절 유성우가 있으면 유성우가 나타나며,\n그 외에는 산발 유성이 하늘을 가로지릅니다.\n\n휴식, 대화, VR 수면, 영상 감상, 그림, 사진 촬영을 즐겨 주세요.\nPC / Android / iOS 지원",
                new Vector3(-2.0f, 0.69f, -0.026f), 0.088f, TextAnchor.UpperLeft,
                font, textMaterial, new Color(0.86f, 0.92f, 1f));
            korean.gameObject.transform.parent.name = "KoreanDescription";
            korean.gameObject.transform.parent.gameObject.SetActive(false);

            Text count = CreateText(presenceGroup,
                "ONLINE  0 / 80\nRECOMMENDED  40\n       PC  0\n       MOBILE  0",
                new Vector3(0.73f, PresenceTop, -0.027f),
                0.082f, TextAnchor.UpperLeft, font, textMaterial, new Color(0.58f, 0.90f, 1f));
            count.transform.parent.name = "PlayerCountCanvas";
            CreatePlatformIcons(presenceGroup, accentMaterial);
            ScrollRect historyScrollRect;
            Text history = CreateHistoryScrollView(presenceGroup, font, textMaterial,
                out historyScrollRect);
            WorldPresenceBoard presence = UdonSharpUndo.AddComponent<WorldPresenceBoard>(panel);
            presence.playerCountText = count;
            presence.historyText = history;
            presence.historyScrollRect = historyScrollRect;
            presence.maximumCapacity = WorldPresenceBoard.DefaultMaximumCapacity;
            presence.recommendedCapacity = WorldPresenceBoard.DefaultRecommendedCapacity;
            presence.historyCapacity = WorldPresenceBoard.DefaultHistoryCapacity;
            presence.visibleHistoryCount = WorldPresenceBoard.DefaultVisibleHistoryCount;
            UdonSharpEditorUtility.CopyProxyToUdon(presence);
            EditorUtility.SetDirty(presence);

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "LanguageToggle";
            button.transform.SetParent(controlsGroup, false);
            // The visible face is flush with the board's -Z face; only this intentional button has a collider.
            // Leave the button visually flush, but move its readable face 2 mm toward the user.
            // At exactly -0.020 m it was coplanar with the panel face and flickered from z-fighting.
            button.transform.localPosition = LanguageTogglePosition;
            button.transform.localScale = LanguageToggleScale;
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            button.GetComponent<BoxCollider>().isTrigger = true;
            Text buttonLabel = CreateText(button.transform, "日→EN", new Vector3(0f, 0f, -0.53f),
                0.075f, TextAnchor.MiddleCenter, font, textMaterial, Color.white, true);
            WorldInfoLanguageToggle toggle = UdonSharpUndo.AddComponent<WorldInfoLanguageToggle>(button);
            toggle.japaneseText = japanese.transform.parent.gameObject;
            toggle.englishText = english.transform.parent.gameObject;
            toggle.traditionalChineseText = traditionalChinese.transform.parent.gameObject;
            toggle.simplifiedChineseText = simplifiedChinese.transform.parent.gameObject;
            toggle.koreanText = korean.transform.parent.gameObject;
            toggle.buttonLabel = buttonLabel;
            UdonSharpEditorUtility.CopyProxyToUdon(toggle);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(toggle);
            if (backing != null)
            {
                backing.InteractionText = "Language / 言語";
                backing.proximity = 2.5f;
                EditorUtility.SetDirty(backing);
                EnableUiBeamForInteraction(button, backing);
            }
            EditorUtility.SetDirty(toggle);

            RealSkyController sky = UnityEngine.Object.FindObjectOfType<RealSkyController>(true);
            MeteorController meteor = UnityEngine.Object.FindObjectOfType<MeteorController>(true);
            WorldDebugPanelPickup debugPickup = UnityEngine.Object.FindObjectOfType<WorldDebugPanelPickup>(true);
            if (sky == null || meteor == null || debugPickup == null)
                throw new InvalidOperationException(
                    "Information panel observatory controls require RealSkyController, MeteorController, and VRDebugPanel.");
            WorldObservatorySelector selector = CreateObservatoryControls(
                panel.transform, controlsGroup, observatoryGroup, font, textMaterial, boardMaterial, buttonMaterial,
                sky, meteor, debugPickup.gameObject);
            toggle.observatorySelector = selector;
            UdonSharpEditorUtility.CopyProxyToUdon(toggle);
            EditorUtility.SetDirty(toggle);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static WorldObservatorySelector CreateObservatoryControls(
            Transform panel, Transform controls, Transform observatory, Font font,
            Material textMaterial, Material boardMaterial,
            Material buttonMaterial, RealSkyController sky,
            MeteorController meteor, GameObject debugPanel)
        {
            Text heading = CreateText(observatory, ObservatoryHeadingLabels[0],
                new Vector3(-0.77f, -1.31f, -0.026f), 0.060f, TextAnchor.UpperCenter,
                font, textMaterial, new Color(0.58f, 0.90f, 1f));
            heading.transform.parent.name = "ObservatoryHeading";

            WorldObservatorySelector selector = UdonSharpUndo.AddComponent<WorldObservatorySelector>(panel.gameObject);
            selector.profileIds = ObservatoryProfileIds;
            selector.displayNames = ObservatoryDisplayNames;
            selector.displayNamesJapanese = ObservatoryDisplayNamesJapanese;
            selector.displayNamesTraditionalChinese = ObservatoryDisplayNamesTraditionalChinese;
            selector.displayNamesSimplifiedChinese = ObservatoryDisplayNamesSimplifiedChinese;
            selector.displayNamesKorean = ObservatoryDisplayNamesKorean;
            selector.latitudeDegrees = ObservatoryLatitudes;
            selector.longitudeDegreesEast = ObservatoryLongitudesEast;
            selector.selectionOrder = ObservatoryVisualOrder;
            selector.observatoryHeadingLabel = heading;
            selector.localizedHeadingLabels = ObservatoryHeadingLabels;
            selector.skyController = sky;
            selector.meteorController = meteor;
            selector.debugPanelRoot = debugPanel;
            selector.selectedIndex = 0;

            CreateObservatoryButton(observatory, "ObservatoryPrevious", "◀",
                new Vector3(-1.98f, ObservatoryControlY, -0.0125f), new Vector3(0.30f, 0.18f, 0.019f),
                0.095f, buttonMaterial, font, textMaterial, selector,
                WorldObservatoryButton.ActionPrevious, 0, "Previous observatory (global)");

            GameObject selectedButton = CreateObservatoryButton(observatory, "ObservatorySelected", "東京（日本）",
                new Vector3(-0.77f, ObservatoryControlY, -0.0125f), new Vector3(1.98f, 0.18f, 0.019f),
                0.072f, buttonMaterial, font, textMaterial, selector,
                WorldObservatoryButton.ActionToggleList, 0, "Open observatory list (global)");
            selector.selectedLocationLabel = selectedButton.GetComponentInChildren<Text>(true);

            CreateObservatoryButton(observatory, "ObservatoryNext", "▶",
                new Vector3(0.44f, ObservatoryControlY, -0.0125f), new Vector3(0.30f, 0.18f, 0.019f),
                0.095f, buttonMaterial, font, textMaterial, selector,
                WorldObservatoryButton.ActionNext, 0, "Next observatory (global)");

            GameObject debugButton = CreateObservatoryButton(controls, "DebugPanelToggle", "DEBUG: OFF",
                DebugTogglePosition, DebugToggleScale, 0.068f, buttonMaterial, font, textMaterial, selector,
                WorldObservatoryButton.ActionToggleDebugPanel, 0, "Toggle local debug panel");
            selector.debugToggleLabel = debugButton.GetComponentInChildren<Text>(true);

            GameObject listRoot = new GameObject("ObservatoryLocationList");
            listRoot.transform.SetParent(observatory, false);
            GameObject listBackdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            listBackdrop.name = "ListBackdrop";
            listBackdrop.transform.SetParent(listRoot.transform, false);
            listBackdrop.transform.localPosition = new Vector3(0f, ObservatoryListBackdropY, -0.042f);
            listBackdrop.transform.localScale = new Vector3(4.18f, 1.75f, 0.022f);
            listBackdrop.GetComponent<Renderer>().sharedMaterial = boardMaterial;
            UnityEngine.Object.DestroyImmediate(listBackdrop.GetComponent<Collider>());

            Text[] locationListLabels = new Text[ObservatoryDisplayNames.Length];
            for (int visualIndex = 0; visualIndex < ObservatoryDisplayNames.Length; visualIndex++)
            {
                int catalogIndex = ObservatoryVisualOrder[visualIndex];
                int row = visualIndex / 3;
                int column = visualIndex % 3;
                float x = -1.38f + column * 1.38f;
                // The list opens upward from the selector row.  Keep the first item at the
                // bottom-left, then read left-to-right and bottom-to-top.
                float y = ObservatoryListBottomY + row * ObservatoryListRowSpacing;
                GameObject locationButton = CreateObservatoryButton(
                    listRoot.transform, "Location_" + catalogIndex.ToString("00"),
                    ObservatoryDisplayNamesJapanese[catalogIndex], new Vector3(x, y, -0.058f),
                    new Vector3(1.28f, 0.17f, 0.018f), 0.042f,
                    buttonMaterial, font, textMaterial, selector,
                    WorldObservatoryButton.ActionSelectLocation, catalogIndex,
                    "Select " + ObservatoryDisplayNames[catalogIndex] + " (global)");
                locationListLabels[catalogIndex] = locationButton.GetComponentInChildren<Text>(true);
            }

            selector.locationListLabels = locationListLabels;
            selector.locationListRoot = listRoot;
            listRoot.SetActive(false);
            UdonSharpEditorUtility.CopyProxyToUdon(selector);
            EditorUtility.SetDirty(selector);
            return selector;
        }

        private static GameObject CreateObservatoryButton(
            Transform parent, string name, string label, Vector3 position, Vector3 scale,
            float textHeight, Material buttonMaterial, Font font, Material textMaterial,
            WorldObservatorySelector selector, int action, int locationIndex, string interactionText)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = position;
            button.transform.localScale = scale;
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            button.GetComponent<BoxCollider>().isTrigger = true;
            CreateText(button.transform, label, new Vector3(0f, 0f, -0.53f), textHeight,
                TextAnchor.MiddleCenter, font, textMaterial, Color.white, true);

            WorldObservatoryButton behaviour = UdonSharpUndo.AddComponent<WorldObservatoryButton>(button);
            behaviour.selector = selector;
            behaviour.action = action;
            behaviour.locationIndex = locationIndex;
            UdonSharpEditorUtility.CopyProxyToUdon(behaviour);
            UdonBehaviour backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
            if (backing != null)
            {
                backing.InteractionText = interactionText;
                backing.proximity = 2.5f;
                EditorUtility.SetDirty(backing);
                EnableUiBeamForInteraction(button, backing);
            }
            EditorUtility.SetDirty(behaviour);
            return button;
        }

        /// <summary>
        /// Makes a world-space Canvas an actual VRChat UI surface: ordinary layer, the raycaster and
        /// shape components, and a trigger box the pointer can hit.
        /// </summary>
        /// <remarks>
        /// The layer is the load-bearing part. `VRC.SDK3.ClientSim.ClientSimInteractiveLayerProvider`
        /// builds the interactive mask as `~(1 &lt;&lt; UI_LAYER) &amp; ~(1 &lt;&lt; UI_MENU_LAYER) &amp; ...`
        /// whenever the main menu is closed, so anything parked on the UI layer is unreachable during
        /// normal play, and the in-game camera does not photograph that layer either. Keeping world UI
        /// on the Default layer matches the YamaPlayer control bar canvas, the one surface in this scene
        /// whose pointer beam already works.
        /// </remarks>
        internal static BoxCollider ConfigureWorldUiCanvas(Canvas canvas)
        {
            if (canvas == null) throw new InvalidOperationException("World UI canvas is missing.");
            GameObject canvasObject = canvas.gameObject;
            SetLayerRecursively(canvasObject.transform, WorldUiLayer);
            if (canvasObject.GetComponent<CanvasScaler>() == null) canvasObject.AddComponent<CanvasScaler>();
            if (canvasObject.GetComponent<GraphicRaycaster>() == null) canvasObject.AddComponent<GraphicRaycaster>();
            if (canvasObject.GetComponent<VRCUiShape>() == null) canvasObject.AddComponent<VRCUiShape>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            BoxCollider target = canvasObject.GetComponent<BoxCollider>();
            if (target == null) target = canvasObject.AddComponent<BoxCollider>();
            target.isTrigger = true;
            // Canvas pivots are not always centred, so anchor the box on the rect, not the origin.
            target.center = new Vector3(
                (0.5f - canvasRect.pivot.x) * canvasRect.sizeDelta.x,
                (0.5f - canvasRect.pivot.y) * canvasRect.sizeDelta.y, 0f);
            // Canvas scales differ by two orders of magnitude across these panels, so the depth is
            // derived from the world scale to stay a consistent few millimetres everywhere.
            float scaleZ = Mathf.Abs(canvasObject.transform.lossyScale.z);
            float depth = scaleZ > 1e-9f ? UiTargetWorldDepth / scaleZ : 1f;
            target.size = new Vector3(canvasRect.sizeDelta.x, canvasRect.sizeDelta.y, depth);
            EditorUtility.SetDirty(canvasObject);
            return target;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int index = 0; index < root.childCount; index++)
                SetLayerRecursively(root.GetChild(index), layer);
        }

        /// <summary>
        /// Adds the same VR laser-pointer interaction path used by VRChat world-space UI while retaining
        /// the ordinary Udon Interact collider for desktop and direct-use input.
        /// </summary>
        /// <remarks>
        /// The clickable graphic goes on the Canvas root, not on a child. The board sliders, whose
        /// Slider component sits on the canvas root, worked as soon as they were on an interactive
        /// layer, while buttons whose Button sat on the child label object did not respond at all.
        /// The YamaPlayer control bar canvas — the one surface in this scene that always worked —
        /// likewise carries its own CanvasRenderer on the canvas root.
        /// </remarks>
        internal static void EnableUiBeamForInteraction(GameObject button, UdonBehaviour backing)
        {
            if (button == null || backing == null) return;
            DisableLabelRaycast(button);
            Canvas canvas = EnsureBeamCanvas(button);
            if (canvas == null) return;
            ConfigureWorldUiCanvas(canvas);

            Image surface = canvas.GetComponent<Image>();
            Button uiButton = canvas.GetComponent<Button>();
            if (uiButton == null) uiButton = canvas.gameObject.AddComponent<Button>();
            uiButton.targetGraphic = surface;
            // The 3D button already draws the visible face, so no colour tint on top of it.
            uiButton.transition = Selectable.Transition.None;
            uiButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddStringPersistentListener(uiButton.onClick, backing.SendCustomEvent, "Interact");
            EditorUtility.SetDirty(uiButton);
        }

        /// <summary>
        /// One dedicated canvas per button, covering its face, carrying an almost invisible Image as
        /// the raycast target. Existing label canvases are left alone so they only render text.
        /// </summary>
        private static Canvas EnsureBeamCanvas(GameObject button)
        {
            Transform existing = button.transform.Find(BeamTargetName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            Vector3 face = button.transform.localScale;
            if (face.x <= 0f || face.y <= 0f) return null;
            GameObject canvasObject = new GameObject(BeamTargetName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image));
            canvasObject.transform.SetParent(button.transform, false);
            // Just off the reader-side face, in front of the label so nothing occludes the ray.
            canvasObject.transform.localPosition = new Vector3(0f, 0f, -0.56f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = new Vector3(
                CanvasScale / face.x, CanvasScale / face.y, CanvasScale);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta =
                new Vector2(face.x / CanvasScale, face.y / CanvasScale);

            Image image = canvasObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.004f);
            image.raycastTarget = true;
            return canvas;
        }

        /// <summary>Label canvases only render; the beam surface owns the raycast.</summary>
        private static void DisableLabelRaycast(GameObject button)
        {
            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].raycastTarget = false;
                EditorUtility.SetDirty(labels[index]);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void ApplyReadabilityLayout(Scene scene)
        {
            if (!scene.IsValid()) throw new InvalidOperationException("StargazingHill scene is not loaded.");
            GameObject panel = GameObject.Find("World/InformationSystem/" + PanelName);
            if (panel == null) throw new InvalidOperationException("World information panel is missing.");

            Transform languageToggle = panel.transform.Find(ControlsGroupPath + "/LanguageToggle");
            if (languageToggle == null) throw new InvalidOperationException("Information panel language toggle is missing.");
            languageToggle.localPosition = LanguageTogglePosition;
            languageToggle.localScale = LanguageToggleScale;
            Transform debugToggle = panel.transform.Find(ControlsGroupPath + "/DebugPanelToggle");
            if (debugToggle == null) throw new InvalidOperationException("Information panel debug toggle is missing.");
            debugToggle.localPosition = DebugTogglePosition;
            debugToggle.localScale = DebugToggleScale;

            panel.transform.position = Position;
            panel.transform.rotation = Quaternion.Euler(Euler);

            WorldPresenceBoard presence = panel.GetComponent<WorldPresenceBoard>();
            if (presence == null || presence.playerCountText == null || presence.historyScrollRect == null)
                throw new InvalidOperationException("Information panel presence display is incomplete.");

            RectTransform countCanvas = presence.playerCountText.transform.parent as RectTransform;
            if (countCanvas == null) throw new InvalidOperationException("Presence count canvas is missing.");
            countCanvas.anchoredPosition = new Vector2(0.73f, PresenceTop);
            presence.playerCountText.fontStyle = FontStyle.Normal;

            RectTransform historyCanvas = presence.historyScrollRect.GetComponent<RectTransform>();
            historyCanvas.anchoredPosition = new Vector2(0.73f, HistoryTop);
            historyCanvas.sizeDelta = new Vector2(historyCanvas.sizeDelta.x, HistoryHeightPixels);

            SetLocalPosition(panel.transform.Find(PresenceGroupPath + "/PlatformLaptopIcon"),
                new Vector3(0.79f, LaptopIconTop, -0.023f));
            SetLocalPosition(panel.transform.Find(PresenceGroupPath + "/PlatformMobileIcon"),
                new Vector3(0.79f, MobileIconTop, -0.023f));

            EditorUtility.SetDirty(languageToggle);
            EditorUtility.SetDirty(debugToggle);
            EditorUtility.SetDirty(panel.transform);
            EditorUtility.SetDirty(countCanvas);
            EditorUtility.SetDirty(presence.playerCountText);
            EditorUtility.SetDirty(historyCanvas);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void SetLocalPosition(Transform target, Vector3 position)
        {
            if (target == null) throw new InvalidOperationException("Information panel platform icon is missing.");
            target.localPosition = position;
            EditorUtility.SetDirty(target);
        }

        private static Text CreateHistoryScrollView(Transform parent, Font font,
            Material textMaterial, out ScrollRect scrollRect)
        {
            GameObject canvasObject = new GameObject("HistoryScrollCanvas");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = new Vector3(0.73f, HistoryTop, -0.027f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * CanvasScale;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(650f, HistoryHeightPixels);
            canvasRect.pivot = new Vector2(0f, 1f);
            ConfigureWorldUiCanvas(canvas);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.layer = canvasObject.layer;
            viewportObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-28f, 0f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.material = textMaterial;
            viewportImage.raycastTarget = true;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));
            contentObject.layer = canvasObject.layer;
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            Text history = contentObject.GetComponent<Text>();
            history.font = font;
            history.fontSize = 25;
            history.alignment = TextAnchor.UpperLeft;
            history.horizontalOverflow = HorizontalWrapMode.Wrap;
            history.verticalOverflow = VerticalWrapMode.Overflow;
            history.text = "JOIN / LEAVE  0-0 / 0";
            history.color = new Color(0.76f, 0.86f, 0.96f);
            history.material = textMaterial;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            scrollbarObject.layer = canvasObject.layer;
            scrollbarObject.transform.SetParent(canvasObject.transform, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-20f, 0f);
            scrollbarRect.offsetMax = Vector2.zero;
            Image scrollbarImage = scrollbarObject.GetComponent<Image>();
            scrollbarImage.color = new Color(0.08f, 0.17f, 0.28f, 0.75f);
            // UI Image/ScrollRect reads the material's _MainTex during LateUpdate.
            // Unlit/Color has no texture property and logs an error every frame, so all
            // graphics in this Canvas use the VRChat supersampled UI material instead.
            scrollbarImage.material = textMaterial;

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            handleObject.layer = canvasObject.layer;
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(3f, 3f);
            handleRect.offsetMax = new Vector2(-3f, -3f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.58f, 0.90f, 1f, 1f);
            handleImage.material = textMaterial;

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect = canvasObject.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 36f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalNormalizedPosition = 0f;
            return history;
        }

        private static void CreatePlatformIcons(Transform parent, Material material)
        {
            GameObject laptop = new GameObject("PlatformLaptopIcon");
            laptop.transform.SetParent(parent, false);
            laptop.transform.localPosition = new Vector3(0.79f, LaptopIconTop, -0.023f);
            CreateIconOutline(laptop.transform, 0.11f, 0.065f, 0.010f, material);
            CreateIconPart(laptop.transform, "Base", new Vector3(0f, -0.044f, 0f),
                new Vector3(0.145f, 0.012f, 0.004f), material);

            GameObject mobile = new GameObject("PlatformMobileIcon");
            mobile.transform.SetParent(parent, false);
            mobile.transform.localPosition = new Vector3(0.79f, MobileIconTop, -0.023f);
            CreateIconOutline(mobile.transform, 0.055f, 0.090f, 0.009f, material);
        }

        private static void CreateIconOutline(Transform parent, float width, float height,
            float thickness, Material material)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            CreateIconPart(parent, "Top", new Vector3(0f, halfHeight, 0f),
                new Vector3(width, thickness, 0.004f), material);
            CreateIconPart(parent, "Bottom", new Vector3(0f, -halfHeight, 0f),
                new Vector3(width, thickness, 0.004f), material);
            CreateIconPart(parent, "Left", new Vector3(-halfWidth, 0f, 0f),
                new Vector3(thickness, height, 0.004f), material);
            CreateIconPart(parent, "Right", new Vector3(halfWidth, 0f, 0f),
                new Vector3(thickness, height, 0.004f), material);
        }

        private static void CreateIconPart(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        internal static Font EnsureFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) throw new InvalidOperationException("Bundled Noto CJK font is missing: " + FontPath);
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
            return CreateText(parent, value, position, height, anchor, font, material, color,
                compensateParentScale, CanvasScale);
        }

        /// <summary>
        /// <paramref name="canvasScale"/> trades world size against glyph resolution: the world height
        /// stays put while the font is rasterised at <c>height / canvasScale</c> pixels. Small labels
        /// read as a smear at the default scale, so they pass a smaller value here.
        /// </summary>
        internal static Text CreateText(Transform parent, string value, Vector3 position, float height,
            TextAnchor anchor, Font font, Material material, Color color, bool compensateParentScale,
            float canvasScale)
        {
            GameObject canvasObject = new GameObject("TextCanvas");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            // Unity UI faces the panel's -Z reader side at identity rotation. Rotating this
            // canvas 180 degrees makes every label readable only from behind and mirrored.
            canvasObject.transform.localRotation = Quaternion.identity;
            Vector3 scale = Vector3.one * canvasScale;
            if (compensateParentScale)
                scale = new Vector3(canvasScale / parent.localScale.x, canvasScale / parent.localScale.y, canvasScale);
            canvasObject.transform.localScale = scale;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(canvasObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.Max(1, Mathf.RoundToInt(height / canvasScale));
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            text.color = color;
            text.material = material;
            RectTransform rect = text.rectTransform;
            rect.sizeDelta = new Vector2(2000f, 1100f);
            if (anchor == TextAnchor.UpperLeft) rect.pivot = new Vector2(0f, 1f);
            else if (anchor == TextAnchor.UpperCenter) rect.pivot = new Vector2(0.5f, 1f);
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
