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
        private const string AlarmEnabledKey = "StargazingHill.Settings.AlarmEnabled";
        private const string AlarmHourKey = "StargazingHill.Settings.AlarmHour";
        private const string AlarmMinuteKey = "StargazingHill.Settings.AlarmMinute";
        private const string RadioUseKey = "StargazingHill.Settings.RadioUse";

        public GameObject settingsBoard;
        public GameObject nightOverlay;
        public Material nightOverlayMaterial;
        public Slider nightSlider;
        public GameObject[] mirrors;
        public Text clockText;
        public Text alarmText;
        public Text mirrorText;
        public Text radioUseText;
        public Text saveText;
        public AudioSource alarmAudio;
        public WorldRadioSpeaker radioSpeaker;

        private bool _playerDataReady;
        private bool _saveEnabled;
        private bool _alarmEnabled;
        private bool _alarmRinging;
        private bool _radioUseAllowed;
        private int _alarmHour = 22;
        private int _alarmMinute;
        private int _mirrorIndex = -1;
        private int _lastAlarmDate = -1;
        private float _nightAmount;
        private float _nextClockUpdate;
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
                bool restoredAlarmEnabled;
                int restoredAlarmHour;
                int restoredAlarmMinute;
                bool restoredRadioUse;
                if (PlayerData.TryGetFloat(player, NightKey, out restoredNight))
                    _nightAmount = Mathf.Clamp01(restoredNight);
                if (PlayerData.TryGetInt(player, MirrorKey, out restoredMirror))
                    _mirrorIndex = Mathf.Clamp(restoredMirror, -1, 4);
                if (PlayerData.TryGetBool(player, AlarmEnabledKey, out restoredAlarmEnabled))
                    _alarmEnabled = restoredAlarmEnabled;
                if (PlayerData.TryGetInt(player, AlarmHourKey, out restoredAlarmHour))
                    _alarmHour = Mathf.Clamp(restoredAlarmHour, 0, 23);
                if (PlayerData.TryGetInt(player, AlarmMinuteKey, out restoredAlarmMinute))
                    _alarmMinute = Mathf.Clamp(restoredAlarmMinute, 0, 59);
                if (PlayerData.TryGetBool(player, RadioUseKey, out restoredRadioUse))
                    _radioUseAllowed = restoredRadioUse;
            }
            ApplyAll();
        }

        public void ToggleBoard()
        {
            if (settingsBoard != null) settingsBoard.SetActive(!settingsBoard.activeSelf);
        }

        public void SetMirror(int index)
        {
            index = Mathf.Clamp(index, 0, 4);
            _mirrorIndex = _mirrorIndex == index ? -1 : index;
            ApplyMirrors();
            SaveIfEnabled();
        }

        public void AllMirrorsOff()
        {
            _mirrorIndex = -1;
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

        public void ToggleRadioUse()
        {
            _radioUseAllowed = !_radioUseAllowed;
            ApplyRadioUse();
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
            ApplyNightAmount();
            ApplyMirrors();
            ApplyRadioUse();
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
            if (mirrors != null)
                for (int index = 0; index < mirrors.Length; index++)
                    if (mirrors[index] != null) mirrors[index].SetActive(index == _mirrorIndex);
            UpdateLabels();
        }

        private void ApplyRadioUse()
        {
            if (radioSpeaker != null) radioSpeaker.SetUseAllowed(_radioUseAllowed);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (alarmText != null)
                alarmText.text = "ALARM  " + _alarmHour.ToString("00") + ":" +
                    _alarmMinute.ToString("00") + "  " +
                    (_alarmRinging ? "RINGING" : (_alarmEnabled ? "ON" : "OFF"));
            if (mirrorText != null)
            {
                string[] names = { "上", "下", "左", "右", "天井" };
                mirrorText.text = _mirrorIndex < 0 ? "MIRROR / ミラー  OFF" :
                    "MIRROR / ミラー  " + names[_mirrorIndex];
            }
            if (radioUseText != null)
                radioUseText.text = "RADIO USE / ラジオ操作  " + (_radioUseAllowed ? "ON" : "OFF");
            if (saveText != null)
                saveText.text = "SAVE / 保存  " + (_saveEnabled ? "ON" : "OFF");
        }

        private void SaveIfEnabled()
        {
            if (_saveEnabled && _playerDataReady) SaveAll();
        }

        private void SaveAll()
        {
            PlayerData.SetBool(SaveKey, true);
            PlayerData.SetFloat(NightKey, _nightAmount);
            PlayerData.SetInt(MirrorKey, _mirrorIndex);
            PlayerData.SetBool(AlarmEnabledKey, _alarmEnabled);
            PlayerData.SetInt(AlarmHourKey, _alarmHour);
            PlayerData.SetInt(AlarmMinuteKey, _alarmMinute);
            PlayerData.SetBool(RadioUseKey, _radioUseAllowed);
        }
    }
}
