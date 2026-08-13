using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldPresenceBoard : UdonSharpBehaviour
    {
        public Text playerCountText;
        public Text historyText;
        public int maximumCapacity = 32;
        public int historyCapacity = 7;

        private string[] _history;
        private int _historyCount;

        private void Start()
        {
            _history = new string[Mathf.Max(1, historyCapacity)];
            RefreshCount();
            RefreshHistory();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            AddHistory("+  ", player);
            RefreshCount();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            AddHistory("-  ", player);
            RefreshCount();
        }

        private void AddHistory(string prefix, VRCPlayerApi player)
        {
            if (_history == null) _history = new string[Mathf.Max(1, historyCapacity)];
            for (int index = _history.Length - 1; index > 0; index--)
                _history[index] = _history[index - 1];
            string displayName = Utilities.IsValid(player) ? player.displayName : "Unknown";
            _history[0] = prefix + displayName;
            _historyCount = Mathf.Min(_historyCount + 1, _history.Length);
            RefreshHistory();
        }

        private void RefreshCount()
        {
            if (playerCountText != null)
                playerCountText.text = "ONLINE  " + VRCPlayerApi.GetPlayerCount() + " / " + maximumCapacity;
        }

        private void RefreshHistory()
        {
            if (historyText == null) return;
            string value = "JOIN / LEAVE HISTORY";
            for (int index = 0; index < _historyCount; index++) value += "\n" + _history[index];
            historyText.text = value;
        }
    }
}
