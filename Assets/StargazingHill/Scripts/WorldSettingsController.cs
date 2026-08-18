using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace StargazingHill
{
    /// <summary>Local comfort settings, clock, alarm, mirror selection, and opt-in persistence.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldSettingsController : UdonSharpBehaviour
    {
        private const string SaveKey = "StargazingHill.Settings.Save";
        private const string NightKey = "StargazingHill.Settings.Night";
        private const string MirrorKey = "StargazingHill.Settings.Mirror";
        private const string MirrorQuality0Key = "StargazingHill.Settings.MirrorQuality0";
        private const string MirrorQuality1Key = "StargazingHill.Settings.MirrorQuality1";
        private const string MirrorQuality2Key = "StargazingHill.Settings.MirrorQuality2";
        private const string MirrorQuality3Key = "StargazingHill.Settings.MirrorQuality3";
        private const string MirrorQuality4Key = "StargazingHill.Settings.MirrorQuality4";
        private const string MirrorHighQualityKey = "StargazingHill.Settings.MirrorHighQuality";
        private const string NotifySoundKey = "StargazingHill.Settings.NotifySound";
        private const string NotifyDisplayKey = "StargazingHill.Settings.NotifyDisplay";
        private const string AlarmEnabledKey = "StargazingHill.Settings.AlarmEnabled";
        private const string AlarmHourKey = "StargazingHill.Settings.AlarmHour";
        private const string AlarmMinuteKey = "StargazingHill.Settings.AlarmMinute";
        private const string RadioEnabledKey = "StargazingHill.Settings.RadioEnabled";
        private const string LegacyRadioUseKey = "StargazingHill.Settings.RadioUse";
        private const string RadioVolumeKey = "StargazingHill.Settings.RadioVolume";

        public GameObject settingsBoard;
        public GameObject nightOverlay;
        public Material nightOverlayMaterial;
        public Slider nightSlider;
        public GameObject[] mirrorsLow;
        public GameObject[] mirrorsHigh;
        public Text clockText;
        public Text alarmText;
        public Text mirrorText;
        public Text radioStateText;
        public Text radioVolumeText;
        public Text saveText;
        public Text titleText;
        public Text nightModeText;
        public Text languageButtonText;
        public Text[] mirrorButtonTexts;
        public Text[] actionButtonTexts;
        public AudioSource alarmAudio;
        public WorldRadioSpeaker radioSpeaker;
        public Slider radioVolumeSlider;
        public WorldPresenceNotifier presenceNotifier;
        public Text notifyText;

        private bool _playerDataReady;
        private bool _saveEnabled;
        private bool _alarmEnabled;
        private bool _alarmRinging;
        // The picnic radio plays for everyone on join; the board switches it off locally.
        private bool _radioEnabled = true;
        private bool _mirrorHighQuality;
        // Join/leave feedback is on by default; each half switches off independently.
        private bool _notifySound = true;
        private bool _notifyDisplay = true;
        private int _alarmHour = 22;
        private int _alarmMinute;
        private int[] _mirrorQuality = new int[5];
        private int _lastAlarmDate = -1;
        private float _nightAmount;
        private float _radioVolume = WorldRadioSpeaker.DefaultLocalVolume;
        private float _nextClockUpdate;
        private int _languageIndex;
        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            if (settingsBoard != null) settingsBoard.SetActive(false);
            ApplyAll();
            UpdateClockAndAlarm();
        }

        private void Update()
        {
            if (nightSlider != null)
            {
                float requested = Mathf.Clamp01(nightSlider.value);
                if (Mathf.Abs(requested - _nightAmount) > 0.002f)
                {
                    _nightAmount = requested;
                    ApplyNightAmount();
                    SaveIfEnabled();
                }
            }

            if (radioVolumeSlider != null)
            {
                float requested = Mathf.Clamp01(radioVolumeSlider.value);
                if (Mathf.Abs(requested - _radioVolume) > 0.002f)
                {
                    _radioVolume = requested;
                    ApplyRadioVolume();
                    UpdateLabels();
                    SaveIfEnabled();
                }
            }

            if (nightOverlay != null && nightOverlay.activeSelf && Utilities.IsValid(_localPlayer))
            {
                VRCPlayerApi.TrackingData head =
                    _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                nightOverlay.transform.position = head.position;
                nightOverlay.transform.rotation = head.rotation;
            }

            if (Time.time >= _nextClockUpdate) UpdateClockAndAlarm();
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal) return;
            _playerDataReady = true;

            bool restoredSave;
            _saveEnabled = PlayerData.TryGetBool(player, SaveKey, out restoredSave) && restoredSave;
            if (_saveEnabled)
            {
                float restoredNight;
                int restoredMirror;
                int restoredMirrorQuality;
                bool restoredAlarmEnabled;
                int restoredAlarmHour;
                int restoredAlarmMinute;
                bool restoredRadioEnabled;
                float restoredRadioVolume;
                if (PlayerData.TryGetFloat(player, NightKey, out restoredNight))
                    _nightAmount = Mathf.Clamp01(restoredNight);
                bool restoredNewMirrors = false;
                if (PlayerData.TryGetInt(player, MirrorQuality0Key, out restoredMirrorQuality))
                {
                    _mirrorQuality[0] = Mathf.Clamp(restoredMirrorQuality, 0, 2);
                    restoredNewMirrors = true;
                }
                if (PlayerData.TryGetInt(player, MirrorQuality1Key, out restoredMirrorQuality))
                {
                    _mirrorQuality[1] = Mathf.Clamp(restoredMirrorQuality, 0, 2);
                    restoredNewMirrors = true;
                }
                if (PlayerData.TryGetInt(player, MirrorQuality2Key, out restoredMirrorQuality))
                {
                    _mirrorQuality[2] = Mathf.Clamp(restoredMirrorQuality, 0, 2);
                    restoredNewMirrors = true;
                }
                if (PlayerData.TryGetInt(player, MirrorQuality3Key, out restoredMirrorQuality))
                {
                    _mirrorQuality[3] = Mathf.Clamp(restoredMirrorQuality, 0, 2);
                    restoredNewMirrors = true;
                }
                if (PlayerData.TryGetInt(player, MirrorQuality4Key, out restoredMirrorQuality))
                {
                    _mirrorQuality[4] = Mathf.Clamp(restoredMirrorQuality, 0, 2);
                    restoredNewMirrors = true;
                }
                // One-time compatibility with the previous single-mirror setting.
                if (!restoredNewMirrors && PlayerData.TryGetInt(player, MirrorKey, out restoredMirror) &&
                    restoredMirror >= 0 && restoredMirror < _mirrorQuality.Length)
                    _mirrorQuality[restoredMirror] = 1;
                bool restoredHighQuality;
                if (PlayerData.TryGetBool(player, MirrorHighQualityKey, out restoredHighQuality))
                    _mirrorHighQuality = restoredHighQuality;
                else
                    // Saves written before the per-mirror ON/OFF board only recorded quality per
                    // direction.  Any HQ mirror in that save selects HQ for the single toggle.
                    for (int index = 0; index < _mirrorQuality.Length; index++)
                        if (_mirrorQuality[index] == 2) _mirrorHighQuality = true;
                for (int index = 0; index < _mirrorQuality.Length; index++)
                    if (_mirrorQuality[index] != 0)
                        _mirrorQuality[index] = _mirrorHighQuality ? 2 : 1;
                if (PlayerData.TryGetBool(player, AlarmEnabledKey, out restoredAlarmEnabled))
                    _alarmEnabled = restoredAlarmEnabled;
                if (PlayerData.TryGetInt(player, AlarmHourKey, out restoredAlarmHour))
                    _alarmHour = Mathf.Clamp(restoredAlarmHour, 0, 23);
                if (PlayerData.TryGetInt(player, AlarmMinuteKey, out restoredAlarmMinute))
                    _alarmMinute = Mathf.Clamp(restoredAlarmMinute, 0, 59);
                if (PlayerData.TryGetBool(player, RadioEnabledKey, out restoredRadioEnabled))
                    _radioEnabled = restoredRadioEnabled;
                // One-time compatibility with the previous two-step radio USE-area setting.
                else if (PlayerData.TryGetBool(player, LegacyRadioUseKey, out restoredRadioEnabled))
                    _radioEnabled = restoredRadioEnabled;
                if (PlayerData.TryGetFloat(player, RadioVolumeKey, out restoredRadioVolume))
                    _radioVolume = Mathf.Clamp01(restoredRadioVolume);
                bool restoredNotify;
                if (PlayerData.TryGetBool(player, NotifySoundKey, out restoredNotify))
                    _notifySound = restoredNotify;
                if (PlayerData.TryGetBool(player, NotifyDisplayKey, out restoredNotify))
                    _notifyDisplay = restoredNotify;
            }
            ApplyAll();
        }

        public void ToggleBoard()
        {
            if (settingsBoard != null) settingsBoard.SetActive(!settingsBoard.activeSelf);
        }

        public void ToggleMirror(int mirrorIndex)
        {
            int index = Mathf.Clamp(mirrorIndex, 0, 4);
            _mirrorQuality[index] = _mirrorQuality[index] != 0 ? 0 : (_mirrorHighQuality ? 2 : 1);
            ApplyMirrors();
            SaveIfEnabled();
        }

        public void AllMirrorsOff()
        {
            for (int index = 0; index < _mirrorQuality.Length; index++)
                _mirrorQuality[index] = 0;
            ApplyMirrors();
            SaveIfEnabled();
        }

        public void ToggleMirrorQuality()
        {
            _mirrorHighQuality = !_mirrorHighQuality;
            for (int index = 0; index < _mirrorQuality.Length; index++)
                if (_mirrorQuality[index] != 0)
                    _mirrorQuality[index] = _mirrorHighQuality ? 2 : 1;
            ApplyMirrors();
            SaveIfEnabled();
        }

        public void AdjustAlarmHour(int delta)
        {
            _alarmHour = (_alarmHour + delta + 24) % 24;
            StopAlarmSound();
            UpdateLabels();
            SaveIfEnabled();
        }

        public void AdjustAlarmMinute(int delta)
        {
            _alarmMinute = (_alarmMinute + delta + 60) % 60;
            StopAlarmSound();
            UpdateLabels();
            SaveIfEnabled();
        }

        public void ToggleAlarm()
        {
            _alarmEnabled = !_alarmEnabled;
            if (!_alarmEnabled) StopAlarmSound();
            UpdateLabels();
            SaveIfEnabled();
        }

        public void StopAlarmSound()
        {
            _alarmRinging = false;
            if (alarmAudio != null) alarmAudio.Stop();
            UpdateLabels();
        }

        public void ToggleRadio()
        {
            _radioEnabled = !_radioEnabled;
            ApplyRadioState();
            SaveIfEnabled();
        }

        public void ToggleNotifySound()
        {
            _notifySound = !_notifySound;
            ApplyNotifier();
            SaveIfEnabled();
        }

        public void ToggleNotifyDisplay()
        {
            _notifyDisplay = !_notifyDisplay;
            ApplyNotifier();
            SaveIfEnabled();
        }

        public void ToggleSave()
        {
            _saveEnabled = !_saveEnabled;
            if (_playerDataReady)
            {
                PlayerData.SetBool(SaveKey, _saveEnabled);
                if (_saveEnabled) SaveAll();
            }
            UpdateLabels();
        }

        public void ToggleLanguage()
        {
            _languageIndex = (_languageIndex + 1) % 5;
            if (presenceNotifier != null) presenceNotifier.SetLanguage(_languageIndex);
            UpdateLabels();
        }

        private void UpdateClockAndAlarm()
        {
            DateTime now = DateTime.Now;
            _nextClockUpdate = Time.time + Mathf.Clamp(1f - now.Millisecond * 0.001f, 0.05f, 1f);
            if (clockText != null)
                clockText.text = now.Year.ToString("0000") + "-" + now.Month.ToString("00") + "-" +
                    now.Day.ToString("00") + "  " + now.Hour.ToString("00") + ":" +
                    now.Minute.ToString("00") + ":" + now.Second.ToString("00") + "  LOCAL";

            int dateCode = now.Year * 10000 + now.Month * 100 + now.Day;
            if (_alarmEnabled && !_alarmRinging && dateCode != _lastAlarmDate &&
                now.Hour == _alarmHour && now.Minute == _alarmMinute)
            {
                _lastAlarmDate = dateCode;
                _alarmRinging = true;
                if (alarmAudio != null) alarmAudio.Play();
            }
            UpdateLabels();
        }

        private void ApplyAll()
        {
            if (nightSlider != null) nightSlider.value = _nightAmount;
            if (radioVolumeSlider != null) radioVolumeSlider.value = _radioVolume;
            ApplyNightAmount();
            ApplyMirrors();
            ApplyRadioState();
            ApplyRadioVolume();
            ApplyNotifier();
            UpdateLabels();
        }

        private void ApplyNightAmount()
        {
            bool active = _nightAmount > 0.002f;
            if (nightOverlay != null) nightOverlay.SetActive(active);
            if (nightOverlayMaterial != null)
                nightOverlayMaterial.SetFloat("_Darkness", Mathf.Lerp(0f, 0.90f, _nightAmount));
        }

        private void ApplyMirrors()
        {
            for (int index = 0; index < _mirrorQuality.Length; index++)
            {
                if (mirrorsLow != null && index < mirrorsLow.Length && mirrorsLow[index] != null)
                    mirrorsLow[index].SetActive(_mirrorQuality[index] == 1);
                if (mirrorsHigh != null && index < mirrorsHigh.Length && mirrorsHigh[index] != null)
                    mirrorsHigh[index].SetActive(_mirrorQuality[index] == 2);
            }
            UpdateLabels();
        }

        private void ApplyRadioState()
        {
            if (radioSpeaker != null) radioSpeaker.SetSpeakerEnabled(_radioEnabled);
            UpdateLabels();
        }

        private void ApplyRadioVolume()
        {
            if (radioSpeaker != null) radioSpeaker.SetLocalVolume(_radioVolume);
        }

        private void ApplyNotifier()
        {
            if (presenceNotifier == null) return;
            presenceNotifier.SetSoundEnabled(_notifySound);
            presenceNotifier.SetDisplayEnabled(_notifyDisplay);
            presenceNotifier.SetLanguage(_languageIndex);
        }

        private void UpdateLabels()
        {
            UpdateStaticLabels();
            string alarmOff = "OFF";
            string alarmOn = "ON";
            string alarmRinging = "RINGING";
            if (_languageIndex == 2) alarmRinging = "響鈴中";
            else if (_languageIndex == 3) alarmRinging = "响铃中";
            else if (_languageIndex == 4) alarmRinging = "울리는 중";
            if (alarmText != null)
                alarmText.text = "ALARM  " + _alarmHour.ToString("00") + ":" +
                    _alarmMinute.ToString("00") + "  " +
                    (_alarmRinging ? alarmRinging : (_alarmEnabled ? alarmOn : alarmOff));
            if (mirrorText != null)
            {
                int onCount = 0;
                for (int index = 0; index < _mirrorQuality.Length; index++)
                    if (_mirrorQuality[index] != 0) onCount++;
                string heading = "ミラー（HQは高負荷）";
                if (_languageIndex == 1) heading = "MIRRORS (HQ: HIGH LOAD)";
                else if (_languageIndex == 2) heading = "鏡面（HQ負載較高）";
                else if (_languageIndex == 3) heading = "镜面（HQ负载较高）";
                else if (_languageIndex == 4) heading = "거울 (HQ: 고부하)";
                mirrorText.text = heading + "  ON " + onCount + " / 5  " +
                    (_mirrorHighQuality ? "HQ" : "LQ");
            }
            if (radioStateText != null)
            {
                string label = "ラジオ音声";
                if (_languageIndex == 1) label = "RADIO SPEAKER";
                else if (_languageIndex == 2) label = "收音機喇叭";
                else if (_languageIndex == 3) label = "收音机扬声器";
                else if (_languageIndex == 4) label = "라디오 스피커";
                radioStateText.text = label + "  " + (_radioEnabled ? "ON" : "OFF");
            }
            if (radioVolumeText != null)
            {
                string label = "ラジオ音量";
                if (_languageIndex == 1) label = "RADIO VOLUME";
                else if (_languageIndex == 2) label = "收音機音量";
                else if (_languageIndex == 3) label = "收音机音量";
                else if (_languageIndex == 4) label = "라디오 음량";
                radioVolumeText.text = label + "  " + Mathf.RoundToInt(_radioVolume * 100f) + "%";
            }
            if (notifyText != null)
            {
                string label = "入退室通知";
                string sound = "音";
                string display = "表示";
                if (_languageIndex == 1) { label = "JOIN / LEAVE"; sound = "SOUND"; display = "TOAST"; }
                else if (_languageIndex == 2) { label = "進出通知"; sound = "音效"; display = "顯示"; }
                else if (_languageIndex == 3) { label = "进出通知"; sound = "音效"; display = "显示"; }
                else if (_languageIndex == 4) { label = "입퇴장 알림"; sound = "소리"; display = "표시"; }
                notifyText.text = label + "  " + sound + " " + (_notifySound ? "ON" : "OFF") +
                    " / " + display + " " + (_notifyDisplay ? "ON" : "OFF");
            }
            if (saveText != null)
            {
                string label = "設定保存";
                if (_languageIndex == 1) label = "SAVE SETTINGS";
                else if (_languageIndex == 2) label = "儲存設定";
                else if (_languageIndex == 3) label = "保存设置";
                else if (_languageIndex == 4) label = "설정 저장";
                saveText.text = label + "  " + (_saveEnabled ? "ON" : "OFF");
            }
        }

        private void UpdateStaticLabels()
        {
            string title = "ローカル設定";
            string night = "ナイトモード";
            string[] mirrorDirections = { "上", "下", "左", "右", "天井" };
            string[] actionButtons = { "時−", "時＋", "分−", "分＋", "ON/OFF", "停止", "音声 ON/OFF", "保存 ON/OFF", "通知音 ON/OFF", "入退室表示 ON/OFF" };
            string language = "日→EN";
            string mirrorAllOff = "すべてOFF";
            string mirrorQuality = "画質";
            if (_languageIndex == 1)
            {
                title = "LOCAL SETTINGS";
                night = "NIGHT MODE";
                mirrorDirections = new[] { "TOP", "BOTTOM", "LEFT", "RIGHT", "CEILING" };
                actionButtons = new[] { "HOUR−", "HOUR＋", "MIN−", "MIN＋", "ON/OFF", "STOP", "SPEAKER ON/OFF", "SAVE ON/OFF", "SOUND ON/OFF", "TOAST ON/OFF" };
                language = "EN→繁";
                mirrorAllOff = "ALL OFF";
                mirrorQuality = "QUALITY";
            }
            else if (_languageIndex == 2)
            {
                title = "本機設定";
                night = "夜間模式";
                mirrorDirections = new[] { "上", "下", "左", "右", "天花板" };
                actionButtons = new[] { "時−", "時＋", "分−", "分＋", "ON/OFF", "停止", "喇叭 ON/OFF", "儲存 ON/OFF", "音效 ON/OFF", "進出顯示 ON/OFF" };
                language = "繁→简";
                mirrorAllOff = "全部關閉";
                mirrorQuality = "畫質";
            }
            else if (_languageIndex == 3)
            {
                title = "本地设置";
                night = "夜间模式";
                mirrorDirections = new[] { "上", "下", "左", "右", "天花板" };
                actionButtons = new[] { "时−", "时＋", "分−", "分＋", "ON/OFF", "停止", "扬声器 ON/OFF", "保存 ON/OFF", "音效 ON/OFF", "进出显示 ON/OFF" };
                language = "简→한";
                mirrorAllOff = "全部关闭";
                mirrorQuality = "画质";
            }
            else if (_languageIndex == 4)
            {
                title = "로컬 설정";
                night = "나이트 모드";
                mirrorDirections = new[] { "위", "아래", "왼쪽", "오른쪽", "천장" };
                actionButtons = new[] { "시−", "시＋", "분−", "분＋", "ON/OFF", "정지", "스피커 ON/OFF", "저장 ON/OFF", "알림음 ON/OFF", "입퇴장 표시 ON/OFF" };
                language = "한→日";
                mirrorAllOff = "모두 OFF";
                mirrorQuality = "화질";
            }

            if (titleText != null) titleText.text = title;
            if (nightModeText != null) nightModeText.text = night;
            if (languageButtonText != null) languageButtonText.text = language;
            // 0-4 are the per-direction ON/OFF toggles, 5 clears them all, and 6 switches the
            // quality every enabled mirror runs at.
            if (mirrorButtonTexts != null)
                for (int index = 0; index < mirrorButtonTexts.Length; index++)
                {
                    if (mirrorButtonTexts[index] == null) continue;
                    if (index < mirrorDirections.Length)
                    {
                        bool on = _mirrorQuality[index] != 0;
                        mirrorButtonTexts[index].text =
                            (on ? "● " : "") + mirrorDirections[index] + "  " + (on ? "ON" : "OFF");
                    }
                    else if (index == mirrorDirections.Length) mirrorButtonTexts[index].text = mirrorAllOff;
                    else if (index == mirrorDirections.Length + 1)
                        mirrorButtonTexts[index].text =
                            mirrorQuality + "  " + (_mirrorHighQuality ? "HQ" : "LQ");
                }
            if (actionButtonTexts != null)
                for (int index = 0; index < actionButtonTexts.Length && index < actionButtons.Length; index++)
                    if (actionButtonTexts[index] != null) actionButtonTexts[index].text = actionButtons[index];
        }

        private void SaveIfEnabled()
        {
            if (_saveEnabled && _playerDataReady) SaveAll();
        }

        private void SaveAll()
        {
            PlayerData.SetBool(SaveKey, true);
            PlayerData.SetFloat(NightKey, _nightAmount);
            PlayerData.SetInt(MirrorKey, -1);
            PlayerData.SetInt(MirrorQuality0Key, _mirrorQuality[0]);
            PlayerData.SetInt(MirrorQuality1Key, _mirrorQuality[1]);
            PlayerData.SetInt(MirrorQuality2Key, _mirrorQuality[2]);
            PlayerData.SetInt(MirrorQuality3Key, _mirrorQuality[3]);
            PlayerData.SetInt(MirrorQuality4Key, _mirrorQuality[4]);
            PlayerData.SetBool(MirrorHighQualityKey, _mirrorHighQuality);
            PlayerData.SetBool(AlarmEnabledKey, _alarmEnabled);
            PlayerData.SetInt(AlarmHourKey, _alarmHour);
            PlayerData.SetInt(AlarmMinuteKey, _alarmMinute);
            PlayerData.SetBool(RadioEnabledKey, _radioEnabled);
            PlayerData.SetFloat(RadioVolumeKey, _radioVolume);
            PlayerData.SetBool(NotifySoundKey, _notifySound);
            PlayerData.SetBool(NotifyDisplayKey, _notifyDisplay);
        }
    }
}
