using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace StargazingHill
{
    /// <summary>
    /// Global observatory selector for the information board. Only the catalog index is synchronized;
    /// every client applies the same versioned coordinates to its local sky and meteor controllers.
    /// The dropdown-open state and debug-panel visibility remain local UI state.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WorldObservatorySelector : UdonSharpBehaviour
    {
        public const int ExpectedLocationCount = 22;

        [Header("Global Observatory Catalog")]
        public string[] profileIds;
        // Kept as the English catalog name for serialized-scene compatibility.
        public string[] displayNames;
        public string[] displayNamesJapanese;
        public string[] displayNamesTraditionalChinese;
        public string[] displayNamesSimplifiedChinese;
        public string[] displayNamesKorean;
        public float[] latitudeDegrees;
        public float[] longitudeDegreesEast;
        // Catalog indices in the same order as the expanded tile list. The synchronized
        // selectedIndex remains a stable catalog ID; only Previous/Next traverse this array.
        public int[] selectionOrder;

        [Header("Scene References")]
        public RealSkyController skyController;
        public MeteorController meteorController;
        public Text observatoryHeadingLabel;
        public string[] localizedHeadingLabels;
        public Text selectedLocationLabel;
        public Text[] locationListLabels;
        public GameObject locationListRoot;
        public GameObject debugPanelRoot;
        public Text debugToggleLabel;

        [UdonSynced] public int selectedIndex;

        private int _displayLanguageIndex;

        private void Start()
        {
            if (locationListRoot != null) locationListRoot.SetActive(false);
            ApplySelection();
            UpdateDebugLabel();
        }

        public void SelectPrevious()
        {
            SelectRelative(-1);
        }

        public void SelectNext()
        {
            SelectRelative(1);
        }

        private void SelectRelative(int direction)
        {
            if (!CatalogIsValid()) return;
            int orderIndex = 0;
            for (int index = 0; index < selectionOrder.Length; index++)
            {
                if (selectionOrder[index] != selectedIndex) continue;
                orderIndex = index;
                break;
            }
            int nextOrderIndex = (orderIndex + direction + selectionOrder.Length) % selectionOrder.Length;
            CommitGlobalSelection(selectionOrder[nextOrderIndex]);
        }

        public void SelectLocation(int index)
        {
            if (!CatalogIsValid() || index < 0 || index >= profileIds.Length) return;
            CommitGlobalSelection(index);
            if (locationListRoot != null) locationListRoot.SetActive(false);
        }

        public void ToggleLocationList()
        {
            if (locationListRoot != null) locationListRoot.SetActive(!locationListRoot.activeSelf);
        }

        public void ToggleDebugPanel()
        {
            if (debugPanelRoot != null) debugPanelRoot.SetActive(!debugPanelRoot.activeSelf);
            UpdateDebugLabel();
        }

        public void SetDisplayLanguage(int languageIndex)
        {
            _displayLanguageIndex = Mathf.Clamp(languageIndex, 0, 4);
            RefreshLocalizedLabels();
        }

        public override void OnDeserialization()
        {
            ApplySelection();
        }

        private void CommitGlobalSelection(int index)
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null && !Networking.IsOwner(gameObject))
                Networking.SetOwner(localPlayer, gameObject);

            selectedIndex = index;
            ApplySelection();
            if (localPlayer != null && Networking.IsOwner(gameObject)) RequestSerialization();
        }

        private void ApplySelection()
        {
            if (!CatalogIsValid())
            {
                if (selectedLocationLabel != null) selectedLocationLabel.text = "OBSERVATORY DATA ERROR";
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, profileIds.Length - 1);
            string profileId = profileIds[selectedIndex];
            float latitude = latitudeDegrees[selectedIndex];
            float longitude = longitudeDegreesEast[selectedIndex];

            if (skyController != null)
            {
                skyController.observatoryProfileId = profileId;
                skyController.latitudeDegrees = latitude;
                skyController.longitudeDegreesEast = longitude;
                skyController.ApplyCurrentSkyRotation();
            }

            if (meteorController != null)
            {
                meteorController.observatoryProfileId = profileId;
                meteorController.latitudeDegrees = latitude;
                meteorController.longitudeDegreesEast = longitude;
            }

            RefreshLocalizedLabels();
        }

        private void RefreshLocalizedLabels()
        {
            if (!CatalogIsValid()) return;
            if (observatoryHeadingLabel != null && localizedHeadingLabels != null &&
                localizedHeadingLabels.Length == 5)
                observatoryHeadingLabel.text = localizedHeadingLabels[_displayLanguageIndex];
            if (selectedLocationLabel != null)
                selectedLocationLabel.text = GetLocalizedDisplayName(selectedIndex);
            if (locationListLabels == null || locationListLabels.Length != profileIds.Length) return;
            for (int index = 0; index < locationListLabels.Length; index++)
                if (locationListLabels[index] != null)
                    locationListLabels[index].text = GetLocalizedDisplayName(index);
        }

        private string GetLocalizedDisplayName(int index)
        {
            if (_displayLanguageIndex == 0) return displayNamesJapanese[index];
            if (_displayLanguageIndex == 2) return displayNamesTraditionalChinese[index];
            if (_displayLanguageIndex == 3) return displayNamesSimplifiedChinese[index];
            if (_displayLanguageIndex == 4) return displayNamesKorean[index];
            return displayNames[index];
        }

        private bool CatalogIsValid()
        {
            int count = profileIds == null ? 0 : profileIds.Length;
            return count == ExpectedLocationCount && displayNames != null && displayNames.Length == count &&
                   displayNamesJapanese != null && displayNamesJapanese.Length == count &&
                   displayNamesTraditionalChinese != null && displayNamesTraditionalChinese.Length == count &&
                   displayNamesSimplifiedChinese != null && displayNamesSimplifiedChinese.Length == count &&
                   displayNamesKorean != null && displayNamesKorean.Length == count &&
                   latitudeDegrees != null && latitudeDegrees.Length == count &&
                   longitudeDegreesEast != null && longitudeDegreesEast.Length == count &&
                   selectionOrder != null && selectionOrder.Length == count;
        }

        private void UpdateDebugLabel()
        {
            if (debugToggleLabel == null) return;
            debugToggleLabel.text = debugPanelRoot != null && debugPanelRoot.activeSelf
                ? "DEBUG: ON" : "DEBUG: OFF";
        }
    }
}
