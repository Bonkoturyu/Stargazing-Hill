using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MeteorController : UdonSharpBehaviour
    {
        [Header("Visual Pool")]
        public Transform[] meteorTransforms;
        public Renderer[] meteorRenderers;

        [Header("Hourly Event")]
        [Range(20f, 30f)] public float eventDurationSeconds = 25f;
        public float skyRadius = 65f;

        private VRCPlayerApi _localPlayer;
        private bool _debugEventActive;
        private float _debugStartTime;
        private int _debugEventId;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            SetAllVisible(false);
        }

        private void Update()
        {
            if (_localPlayer != null) transform.position = _localPlayer.GetPosition();

            float elapsed = -1f;
            int eventId = 0;
            if (_debugEventActive)
            {
                elapsed = Time.time - _debugStartTime;
                eventId = _debugEventId;
                if (elapsed >= eventDurationSeconds)
                {
                    _debugEventActive = false;
                    elapsed = -1f;
                }
            }
            else
            {
                DateTime utc = Networking.GetNetworkDateTime();
                float secondsIntoHour = utc.Minute * 60f + utc.Second + utc.Millisecond / 1000f;
                if (secondsIntoHour < eventDurationSeconds)
                {
                    elapsed = secondsIntoHour;
                    eventId = GetHourlyEventId(utc.Year, utc.Month, utc.Day, utc.Hour);
                }
            }

            UpdateMeteorVisuals(eventId, elapsed);
        }

        public void DebugTriggerHourlyEvent()
        {
            DateTime utc = Networking.GetNetworkDateTime();
            _debugEventId = GetHourlyEventId(utc.Year, utc.Month, utc.Day, utc.Hour);
            _debugStartTime = Time.time;
            _debugEventActive = true;
            UpdateMeteorVisuals(_debugEventId, 0f);
        }

        public void DebugStopHourlyEvent()
        {
            _debugEventActive = false;
            SetAllVisible(false);
        }

        public void DebugPreviewEventAtSecond(float elapsed)
        {
            int eventId = GetHourlyEventId(2026, 8, 12, 0);
            UpdateMeteorVisuals(eventId, Mathf.Clamp(elapsed, 0f, eventDurationSeconds - 0.001f));
        }

        public static int GetHourlyEventId(int year, int month, int day, int hour)
        {
            return (((year * 12 + month) * 31 + day) * 24) + hour;
        }

        public static float DebugSampleValue(int eventId, int wave, int slot, int channel)
        {
            int value = eventId;
            value = value * 397 ^ wave * 7919;
            value = value * 397 ^ slot * 104729;
            value = value * 397 ^ channel * 15485863;
            value ^= value >> 16;
            value = value * -2048144789;
            value ^= value >> 13;
            return (value & 0x7fffffff) / 2147483647f;
        }

        private void UpdateMeteorVisuals(int eventId, float elapsed)
        {
            if (meteorTransforms == null || meteorRenderers == null || elapsed < 0f)
            {
                SetAllVisible(false);
                return;
            }

            const float waveLength = 5f;
            int wave = Mathf.FloorToInt(elapsed / waveLength);
            float waveTime = elapsed - wave * waveLength;
            int count = Mathf.Min(meteorTransforms.Length, meteorRenderers.Length);
            for (int slot = 0; slot < count; slot++)
            {
                float onset = 0.35f + slot * 0.88f + DebugSampleValue(eventId, wave, slot, 0) * 0.28f;
                float duration = 0.9f + DebugSampleValue(eventId, wave, slot, 1) * 0.45f;
                float progress = (waveTime - onset) / duration;
                bool visible = progress >= 0f && progress <= 1f && elapsed < eventDurationSeconds;
                meteorRenderers[slot].enabled = visible;
                if (visible) ConfigureMeteor(eventId, wave, slot, Mathf.Clamp01(progress));
            }
        }

        private void ConfigureMeteor(int eventId, int wave, int slot, float progress)
        {
            Transform meteor = meteorTransforms[slot];
            float azimuth = DebugSampleValue(eventId, wave, slot, 2) * Mathf.PI * 2f;
            float elevation = Mathf.Lerp(28f, 72f, DebugSampleValue(eventId, wave, slot, 3)) * Mathf.Deg2Rad;
            Vector3 radial = new Vector3(
                Mathf.Sin(azimuth) * Mathf.Cos(elevation),
                Mathf.Sin(elevation),
                Mathf.Cos(azimuth) * Mathf.Cos(elevation)).normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized;
            Vector3 bitangent = Vector3.Cross(radial, tangent).normalized;
            float trackAngle = DebugSampleValue(eventId, wave, slot, 4) * Mathf.PI * 2f;
            Vector3 motion = (tangent * Mathf.Cos(trackAngle) + bitangent * Mathf.Sin(trackAngle)).normalized;
            float travel = Mathf.Lerp(8f, -8f, progress);

            meteor.localPosition = radial * skyRadius + motion * travel;
            meteor.localRotation = Quaternion.LookRotation(-radial, motion);
            float length = Mathf.Lerp(7f, 11f, DebugSampleValue(eventId, wave, slot, 5));
            float fade = Mathf.Sin(progress * Mathf.PI);
            meteor.localScale = new Vector3(0.22f * fade, length * fade, 1f);
        }

        private void SetAllVisible(bool visible)
        {
            if (meteorRenderers == null) return;
            for (int index = 0; index < meteorRenderers.Length; index++)
            {
                if (meteorRenderers[index] != null) meteorRenderers[index].enabled = visible;
            }
        }
    }
}
