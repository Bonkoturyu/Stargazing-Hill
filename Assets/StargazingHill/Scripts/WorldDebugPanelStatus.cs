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
        public Text traditionalChineseStatusText;
        public Text simplifiedChineseStatusText;
        public Text koreanStatusText;
        public Text japanesePlayStopLabel;
        public Text englishPlayStopLabel;
        public Text traditionalChinesePlayStopLabel;
        public Text simplifiedChinesePlayStopLabel;
        public Text koreanPlayStopLabel;

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
            string remainingTraditional = playing ? "   剩餘: " + Mathf.CeilToInt(remaining) + "秒" : "";
            string remainingSimplified = playing ? "   剩余: " + Mathf.CeilToInt(remaining) + "秒" : "";
            string remainingKorean = playing ? "   남음: " + Mathf.CeilToInt(remaining) + "초" : "";
            if (englishStatusText != null)
                englishStatusText.text = "EVENT: " + meteorController.debugEventMode +
                    "   SHOWER: " + shower + "\nVISIBLE: " +
                    meteorController.debugVisibleMeteorCount + remainingEnglish;
            if (japaneseStatusText != null)
                japaneseStatusText.text = "状態: " + TranslateMode(meteorController.debugEventMode) +
                    "   流星群: " + shower + "\n表示中: " +
                    meteorController.debugVisibleMeteorCount + remainingJapanese;
            if (traditionalChineseStatusText != null)
                traditionalChineseStatusText.text = "狀態: " + TranslateModeTraditional(meteorController.debugEventMode) +
                    "   流星雨: " + shower + "\n顯示中: " +
                    meteorController.debugVisibleMeteorCount + remainingTraditional;
            if (simplifiedChineseStatusText != null)
                simplifiedChineseStatusText.text = "状态: " + TranslateModeSimplified(meteorController.debugEventMode) +
                    "   流星雨: " + shower + "\n显示中: " +
                    meteorController.debugVisibleMeteorCount + remainingSimplified;
            if (koreanStatusText != null)
                koreanStatusText.text = "상태: " + TranslateModeKorean(meteorController.debugEventMode) +
                    "   유성우: " + shower + "\n표시 중: " +
                    meteorController.debugVisibleMeteorCount + remainingKorean;
            if (englishPlayStopLabel != null)
                englishPlayStopLabel.text = playing ? "STOP EVENT" : "PLAY CURRENT";
            if (japanesePlayStopLabel != null)
                japanesePlayStopLabel.text = playing ? "イベント停止" : "現在を再生";
            if (traditionalChinesePlayStopLabel != null)
                traditionalChinesePlayStopLabel.text = playing ? "停止事件" : "播放目前";
            if (simplifiedChinesePlayStopLabel != null)
                simplifiedChinesePlayStopLabel.text = playing ? "停止事件" : "播放当前";
            if (koreanPlayStopLabel != null)
                koreanPlayStopLabel.text = playing ? "이벤트 중지" : "현재 재생";
        }

        private string TranslateMode(string mode)
        {
            if (mode == "FORCED") return "強制";
            if (mode == "NATURAL") return "自然";
            return "待機";
        }

        private string TranslateModeTraditional(string mode)
        {
            if (mode == "FORCED") return "強制";
            if (mode == "NATURAL") return "自然";
            return "待機";
        }

        private string TranslateModeSimplified(string mode)
        {
            if (mode == "FORCED") return "强制";
            if (mode == "NATURAL") return "自然";
            return "待机";
        }

        private string TranslateModeKorean(string mode)
        {
            if (mode == "FORCED") return "강제";
            if (mode == "NATURAL") return "자연";
            return "대기";
        }
    }
}
