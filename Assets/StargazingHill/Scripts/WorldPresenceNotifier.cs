using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace StargazingHill
{
    /// <summary>
    /// Local join/leave chime and head-following toast. Both halves switch independently from the
    /// settings board and hold no synced state, so one viewer's choice never reaches anyone else.
    /// Arrivals and departures are each collected into a short acceptance window, so a group moving
    /// together produces one chime and one line instead of a burst.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldPresenceNotifier : UdonSharpBehaviour
    {
        public const int LineCapacity = 3;
        public const float LineSeconds = 5f;
        public const float WindowSeconds = 3f;

        public GameObject hudRoot;
        public Text hudText;
        public AudioSource joinAudio;
        public AudioSource leaveAudio;
        // Placed below the eyeline so a busy arrival never covers the sky.
        public float forwardDistance = 1.5f;
        public float verticalOffset = -0.66f;

        private bool _soundEnabled = true;
        private bool _displayEnabled = true;
        private bool _live;
        private int _languageIndex;
        private int _lineCount;
        private string[] _lines = new string[LineCapacity];
        private float[] _expiry = new float[LineCapacity];
        private int[] _lineSequence = new int[LineCapacity];
        private int _nextSequence = 1;
        // Per-direction acceptance windows. Sequence -1 means no window is open.
        private int _joinSequence = -1;
        private int _leaveSequence = -1;
        private int _joinCount;
        private int _leaveCount;
        private float _joinWindowEnd;
        private float _leaveWindowEnd;
        private string _joinFirstName = string.Empty;
        private string _leaveFirstName = string.Empty;
        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            if (hudRoot != null) hudRoot.SetActive(false);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            // VRChat replays a join for everyone already in the instance and finishes with the local
            // player. Treating that last one as the start of live notifications is what stops a
            // burst of chimes when someone walks into a busy instance.
            if (Utilities.IsValid(player) && player.isLocal)
            {
                _live = true;
                return;
            }
            if (!_live) return;
            Announce(player, true);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!_live || (Utilities.IsValid(player) && player.isLocal)) return;
            Announce(player, false);
        }

        private void Update()
        {
            if (_lineCount > 0)
            {
                bool expired = false;
                while (_lineCount > 0 && Time.time >= _expiry[0])
                {
                    for (int index = 1; index < _lineCount; index++)
                    {
                        _lines[index - 1] = _lines[index];
                        _expiry[index - 1] = _expiry[index];
                        _lineSequence[index - 1] = _lineSequence[index];
                    }
                    _lineCount--;
                    expired = true;
                }
                if (expired) RefreshHud();
            }

            if (hudRoot != null && hudRoot.activeSelf) FollowHead();
        }

        private void FollowHead()
        {
            if (hudRoot == null || !Utilities.IsValid(_localPlayer)) return;
            VRCPlayerApi.TrackingData head =
                _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            hudRoot.transform.position = head.position +
                head.rotation * new Vector3(0f, verticalOffset, forwardDistance);
            hudRoot.transform.rotation = head.rotation;
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
        }

        public void SetDisplayEnabled(bool enabled)
        {
            _displayEnabled = enabled;
            if (!enabled) _lineCount = 0;
            RefreshHud();
        }

        public void SetLanguage(int languageIndex)
        {
            _languageIndex = languageIndex;
        }

        private void Announce(VRCPlayerApi player, bool joined)
        {
            string displayName = Utilities.IsValid(player) ? player.displayName : "???";
            int openSequence = joined ? _joinSequence : _leaveSequence;
            float windowEnd = joined ? _joinWindowEnd : _leaveWindowEnd;
            int lineIndex = openSequence < 0 ? -1 : FindLine(openSequence);

            if (Time.time < windowEnd && lineIndex >= 0)
            {
                // Inside an open window: fold this person into the existing line and stay silent.
                if (joined) _joinCount++;
                else _leaveCount++;
                _lines[lineIndex] = ComposeLine(joined,
                    joined ? _joinFirstName : _leaveFirstName,
                    (joined ? _joinCount : _leaveCount) - 1);
                _expiry[lineIndex] = Time.time + LineSeconds;
                RefreshHud();
                return;
            }

            // A new window: the first arrival or departure is announced immediately.
            if (_soundEnabled)
            {
                AudioSource source = joined ? joinAudio : leaveAudio;
                if (source != null) source.Play();
            }
            int sequence = _nextSequence++;
            if (joined)
            {
                _joinSequence = sequence;
                _joinCount = 1;
                _joinFirstName = displayName;
                _joinWindowEnd = Time.time + WindowSeconds;
            }
            else
            {
                _leaveSequence = sequence;
                _leaveCount = 1;
                _leaveFirstName = displayName;
                _leaveWindowEnd = Time.time + WindowSeconds;
            }
            if (!_displayEnabled) return;
            PushLine(ComposeLine(joined, displayName, 0), sequence);
        }

        private int FindLine(int sequence)
        {
            for (int index = 0; index < _lineCount; index++)
                if (_lineSequence[index] == sequence) return index;
            return -1;
        }

        /// <summary><paramref name="others"/> is the number of people beyond the named one.</summary>
        private string ComposeLine(bool joined, string displayName, int others)
        {
            string marker = joined ? "＋  " : "－  ";
            if (others <= 0)
            {
                string single = joined ? " さんが入室しました" : " さんが退室しました";
                if (_languageIndex == 1) single = joined ? " joined" : " left";
                else if (_languageIndex == 2) single = joined ? " 已加入" : " 已離開";
                else if (_languageIndex == 3) single = joined ? " 已加入" : " 已离开";
                else if (_languageIndex == 4) single = joined ? " 님이 입장했습니다" : " 님이 퇴장했습니다";
                return marker + displayName + single;
            }

            string count = others.ToString();
            string grouped = joined
                ? " さんほか" + count + "名が入室しました"
                : " さんほか" + count + "名が退室しました";
            if (_languageIndex == 1)
                grouped = joined ? " and " + count + " others joined" : " and " + count + " others left";
            else if (_languageIndex == 2)
                grouped = joined ? " 等" + count + "人已加入" : " 等" + count + "人已離開";
            else if (_languageIndex == 3)
                grouped = joined ? " 等" + count + "人已加入" : " 等" + count + "人已离开";
            else if (_languageIndex == 4)
                grouped = joined
                    ? " 님 외 " + count + "명이 입장했습니다"
                    : " 님 외 " + count + "명이 퇴장했습니다";
            return marker + displayName + grouped;
        }

        private void PushLine(string value, int sequence)
        {
            if (_lineCount < LineCapacity)
            {
                _lines[_lineCount] = value;
                _expiry[_lineCount] = Time.time + LineSeconds;
                _lineSequence[_lineCount] = sequence;
                _lineCount++;
            }
            else
            {
                for (int index = 1; index < LineCapacity; index++)
                {
                    _lines[index - 1] = _lines[index];
                    _expiry[index - 1] = _expiry[index];
                    _lineSequence[index - 1] = _lineSequence[index];
                }
                _lines[LineCapacity - 1] = value;
                _expiry[LineCapacity - 1] = Time.time + LineSeconds;
                _lineSequence[LineCapacity - 1] = sequence;
            }
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (hudText != null)
            {
                string value = string.Empty;
                for (int index = 0; index < _lineCount; index++)
                    value += (index == 0 ? string.Empty : "\n") + _lines[index];
                hudText.text = value;
            }
            if (hudRoot == null) return;
            bool visible = _displayEnabled && _lineCount > 0;
            // Place it before the first frame it is shown, otherwise the toast flashes once at
            // wherever the head happened to be when it was last hidden.
            if (visible && !hudRoot.activeSelf)
            {
                hudRoot.SetActive(true);
                FollowHead();
                return;
            }
            hudRoot.SetActive(visible);
        }
    }
}
