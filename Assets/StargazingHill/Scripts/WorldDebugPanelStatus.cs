using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldDebugPanelStatus : UdonSharpBehaviour
    {
        public MeteorController meteorController;
        public Text statusText;
        public Text playStopLabel;

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
            if (statusText != null)
                statusText.text = "EVENT: " + meteorController.debugEventMode +
                    "   SHOWER: " + shower + "\nVISIBLE: " +
                    meteorController.debugVisibleMeteorCount +
                    (playing ? "   REMAIN: " + Mathf.CeilToInt(remaining) + "s" : "");
            if (playStopLabel != null)
                playStopLabel.text = playing ? "STOP EVENT" : "PLAY CURRENT";
        }
    }
}
