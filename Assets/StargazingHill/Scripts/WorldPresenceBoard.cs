using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;

#if VRC_ENABLE_PLAYER_PERSISTENCE
using VRC.SDK3.Persistence;
#endif

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldPresenceBoard : UdonSharpBehaviour
    {
        // VRChat exposes the live player count to Udon, but not the maximum/recommended
        // capacities configured in the SDK upload panel. Keep the baked values together here.
        public const int DefaultMaximumCapacity = 80;
        public const int DefaultRecommendedCapacity = 40;
        public const int DefaultHistoryCapacity = 40;
        public const int DefaultVisibleHistoryCount = 20;
        public const string PlatformDataKey = "StargazingHill.Platform.v1";

        private const int PlatformUnknown = 0;
        private const int PlatformPc = 1;
        private const int PlatformMobile = 2;

#if UNITY_ANDROID || UNITY_IOS
        private const int LocalPlatform = PlatformMobile;
#elif UNITY_STANDALONE_WIN
        private const int LocalPlatform = PlatformPc;
#else
        private const int LocalPlatform = PlatformUnknown;
#endif

        public Text playerCountText;
        public Text historyText;
        public int maximumCapacity = DefaultMaximumCapacity;
        public int recommendedCapacity = DefaultRecommendedCapacity;
        public int historyCapacity = DefaultHistoryCapacity;
        public int visibleHistoryCount = DefaultVisibleHistoryCount;

        private string[] _history;
        private int _historyCount;
        private int _historyOffset;
        private VRCPlayerApi[] _players;

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
            _historyOffset = 0;
            RefreshHistory();
        }

#if VRC_ENABLE_PLAYER_PERSISTENCE
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
                PlayerData.SetInt(PlatformDataKey, LocalPlatform);
            RefreshCount();
        }

        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
        {
            RefreshCount();
        }
#endif

        public void ScrollHistoryNewer()
        {
            _historyOffset = Mathf.Max(0, _historyOffset - 1);
            RefreshHistory();
        }

        public void ScrollHistoryOlder()
        {
            int pageSize = Mathf.Max(1, visibleHistoryCount);
            int maximumOffset = Mathf.Max(0, _historyCount - pageSize);
            _historyOffset = Mathf.Min(maximumOffset, _historyOffset + 1);
            RefreshHistory();
        }

        private void RefreshCount()
        {
            if (playerCountText == null) return;

            int total = VRCPlayerApi.GetPlayerCount();
            int pcCount = 0;
            int mobileCount = 0;
            int unknownCount = total;

#if VRC_ENABLE_PLAYER_PERSISTENCE
            if (_players == null || _players.Length < total)
                _players = new VRCPlayerApi[Mathf.Max(maximumCapacity, total)];
            VRCPlayerApi.GetPlayers(_players);
            unknownCount = 0;
            for (int index = 0; index < total; index++)
            {
                VRCPlayerApi player = _players[index];
                if (!Utilities.IsValid(player) ||
                    !PlayerData.TryGetInt(player, PlatformDataKey, out int platform))
                {
                    unknownCount++;
                    continue;
                }
                if (platform == PlatformPc) pcCount++;
                else if (platform == PlatformMobile) mobileCount++;
                else unknownCount++;
            }
#endif

            string value = "ONLINE  " + total + " / " + maximumCapacity +
                "\nRECOMMENDED  " + recommendedCapacity +
                "\n       PC  " + pcCount +
                "\n       MOBILE  " + mobileCount;
            if (unknownCount > 0) value += "\n       WAITING  " + unknownCount;
            playerCountText.text = value;
        }

        private void RefreshHistory()
        {
            if (historyText == null) return;
            int pageSize = Mathf.Max(1, visibleHistoryCount);
            int visibleCount = Mathf.Min(pageSize, Mathf.Max(0, _historyCount - _historyOffset));
            int first = visibleCount == 0 ? 0 : _historyOffset + 1;
            int last = _historyOffset + visibleCount;
            string value = "JOIN / LEAVE  " + first + "-" + last + " / " + _historyCount;
            for (int index = 0; index < visibleCount; index++)
                value += "\n" + _history[_historyOffset + index];
            historyText.text = value;
        }
    }
}
