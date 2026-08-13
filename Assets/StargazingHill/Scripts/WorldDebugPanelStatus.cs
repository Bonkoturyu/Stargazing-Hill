using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldDebugPanelStatus : UdonSharpBehaviour
    {
        public MeteorController meteorController;
        public Text japaneseStatusText;
        public Text englishStatusText;
        public Text japanesePlayStopLabel;
        public Text englishPlayStopLabel;

        private float _nextRefresh;

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + 0.2f;
            Refresh();
        }

        public void Refresh()
        {
            if (meteorController == null) return;
            bool playing = meteorController.debugEventPlaying;
            string shower = playing ? meteorController.debugCurrentShowerId : "-";
            float remaining = playing
                ? Mathf.Max(0f, meteorController.debugEventDurationSeconds -
                    meteorController.debugEventElapsedSeconds)
                : 0f;
            string remainingEnglish = playing ? "   REMAIN: " + Mathf.CeilToInt(remaining) + "s" : "";
            string remainingJapanese = playing ? "   残り: " + Mathf.CeilToInt(remaining) + "秒" : "";
            if (englishStatusText != null)
                englishStatusText.text = "EVENT: " + meteorController.debugEventMode +
                    "   SHOWER: " + shower + "\nVISIBLE: " +
                    meteorController.debugVisibleMeteorCount + remainingEnglish;
            if (japaneseStatusText != null)
                japaneseStatusText.text = "状態: " + TranslateMode(meteorController.debugEventMode) +
                    "   流星群: " + shower + "\n表示中: " +
                    meteorController.debugVisibleMeteorCount + remainingJapanese;
            if (englishPlayStopLabel != null)
                englishPlayStopLabel.text = playing ? "STOP EVENT" : "PLAY CURRENT";
            if (japanesePlayStopLabel != null)
                japanesePlayStopLabel.text = playing ? "イベント停止" : "現在を再生";
        }

        private string TranslateMode(string mode)
        {
            if (mode == "FORCED") return "強制";
            if (mode == "NATURAL") return "自然";
            return "待機";
        }
    }
}
