using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldInfoLanguageToggle : UdonSharpBehaviour
    {
        public GameObject japaneseText;
        public GameObject englishText;
        public GameObject traditionalChineseText;
        public GameObject simplifiedChineseText;
        public GameObject koreanText;
        public Text buttonLabel;
        public WorldObservatorySelector observatorySelector;

        private int _languageIndex;

        private void Start()
        {
            ApplyLanguage();
        }

        public override void Interact()
        {
            _languageIndex++;
            if (_languageIndex > 4) _languageIndex = 0;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (japaneseText != null) japaneseText.SetActive(_languageIndex == 0);
            if (englishText != null) englishText.SetActive(_languageIndex == 1);
            if (traditionalChineseText != null) traditionalChineseText.SetActive(_languageIndex == 2);
            if (simplifiedChineseText != null) simplifiedChineseText.SetActive(_languageIndex == 3);
            if (koreanText != null) koreanText.SetActive(_languageIndex == 4);
            if (observatorySelector != null) observatorySelector.SetDisplayLanguage(_languageIndex);

            if (buttonLabel == null) return;
            // Show both the current and next language. The arrow is deliberately not a
            // media-style triangle because this board already uses triangles for location navigation.
            if (_languageIndex == 0) buttonLabel.text = "日→EN";
            else if (_languageIndex == 1) buttonLabel.text = "EN→繁";
            else if (_languageIndex == 2) buttonLabel.text = "繁→简";
            else if (_languageIndex == 3) buttonLabel.text = "简→한";
            else buttonLabel.text = "한→日";
        }
    }
}
