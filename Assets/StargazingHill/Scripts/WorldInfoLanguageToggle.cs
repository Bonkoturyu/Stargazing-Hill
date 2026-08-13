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
        public Text buttonLabel;

        private bool _english;

        private void Start()
        {
            ApplyLanguage();
        }

        public override void Interact()
        {
            _english = !_english;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (japaneseText != null) japaneseText.SetActive(!_english);
            if (englishText != null) englishText.SetActive(_english);
            if (buttonLabel != null) buttonLabel.text = _english ? "日本語" : "ENGLISH";
        }
    }
}
