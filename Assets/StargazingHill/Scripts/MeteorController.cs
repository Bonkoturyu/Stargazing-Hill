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

        [Header("Observatory (same profile as RealSkyController)")]
        public string observatoryProfileId = "tokyo";
        [Range(-90f, 90f)] public float latitudeDegrees = 35.68f;
        [Range(-180f, 180f)] public float longitudeDegreesEast = 139.76f;

        [Header("IMO 2026 Shower Database")]
        public string[] showerIds;
        public string[] showerNamesJa;
        public int[] activeStartMonthDay;
        public int[] activeEndMonthDay;
        public int[] peakMonthDay;
        public float[] radiantRightAscensionDegrees;
        public float[] radiantDeclinationDegrees;
        public int[] zenithalHourlyRates;

        private VRCPlayerApi _localPlayer;
        private bool _debugEventActive;
        private float _debugStartTime;
        private int _debugEventId;
        private DateTime _debugUtc;
        private int _debugForcedShowerIndex = -1;
        private Vector3 _debugViewForward = Vector3.forward;

        private const int DebugForcedMeteorCount = 20;
        private const float DebugImmediatePreviewElapsed = 0.75f;

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
            DateTime utc = Networking.GetNetworkDateTime();
            if (_debugEventActive)
            {
                elapsed = Time.time - _debugStartTime;
                eventId = _debugEventId;
                utc = _debugUtc;
                if (elapsed >= eventDurationSeconds) { _debugEventActive = false; elapsed = -1f; }
            }
            else
            {
                float secondsIntoHour = utc.Minute * 60f + utc.Second + utc.Millisecond / 1000f;
                if (secondsIntoHour < eventDurationSeconds)
                {
                    elapsed = secondsIntoHour;
                    eventId = GetHourlyEventId(utc.Year, utc.Month, utc.Day, utc.Hour);
                }
            }

            UpdateMeteorVisuals(eventId, elapsed, utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
                utc.Second + utc.Millisecond / 1000.0, _debugEventActive ? _debugForcedShowerIndex : -1,
                _debugViewForward);
        }

        public void DebugTriggerHourlyEvent()
        {
            _debugUtc = Networking.GetNetworkDateTime();
            _debugEventId = GetHourlyEventId(_debugUtc.Year, _debugUtc.Month, _debugUtc.Day, _debugUtc.Hour);
            _debugStartTime = Time.time;
            _debugForcedShowerIndex = -1;
            _debugEventActive = true;
            UpdateMeteorVisuals(_debugEventId, 0f, _debugUtc.Year, _debugUtc.Month, _debugUtc.Day,
                _debugUtc.Hour, _debugUtc.Minute, _debugUtc.Second + _debugUtc.Millisecond / 1000.0,
                -1, _debugViewForward);
        }

        public void DebugTriggerSelectedShower(int showerIndex, Vector3 viewForward)
        {
            if (!CatalogLengthsMatch() || showerIndex < 0 || showerIndex >= showerIds.Length)
            {
                Debug.LogWarning("[Stargazing Hill] Meteor preview rejected: invalid shower index " + showerIndex + ".");
                return;
            }

            _debugUtc = Networking.GetNetworkDateTime();
            _debugEventId = GetHourlyEventId(_debugUtc.Year, _debugUtc.Month, _debugUtc.Day, _debugUtc.Hour) +
                            showerIndex * 104729;
            _debugViewForward = NormalizeViewForward(viewForward);
            _debugForcedShowerIndex = showerIndex;
            _debugStartTime = Time.time - DebugImmediatePreviewElapsed;
            _debugEventActive = true;
            UpdateMeteorVisuals(_debugEventId, DebugImmediatePreviewElapsed,
                _debugUtc.Year, _debugUtc.Month, _debugUtc.Day, _debugUtc.Hour, _debugUtc.Minute,
                _debugUtc.Second + _debugUtc.Millisecond / 1000.0, showerIndex, _debugViewForward);
            Debug.Log("[Stargazing Hill] Forced meteor shower preview: " + showerNamesJa[showerIndex] +
                      " / " + showerIds[showerIndex] + " (20 meteors over 25 seconds, local-only).");
        }

        public void DebugStopHourlyEvent()
        {
            _debugEventActive = false;
            _debugForcedShowerIndex = -1;
            SetAllVisible(false);
        }

        public void DebugPreviewEventAtSecond(float elapsed)
        {
            int eventId = GetHourlyEventId(2026, 8, 13, 0);
            UpdateMeteorVisuals(eventId, Mathf.Clamp(elapsed, 0f, eventDurationSeconds - 0.001f),
                2026, 8, 13, 0, 0, 0.0, -1, Vector3.forward);
        }

        public void DebugPreviewSelectedShowerAtSecond(int showerIndex, float elapsed, Vector3 viewForward)
        {
            if (!CatalogLengthsMatch() || showerIndex < 0 || showerIndex >= showerIds.Length)
            {
                SetAllVisible(false);
                return;
            }

            int eventId = GetHourlyEventId(2026, peakMonthDay[showerIndex] / 100,
                peakMonthDay[showerIndex] % 100, 0) + showerIndex * 104729;
            UpdateMeteorVisuals(eventId, Mathf.Clamp(elapsed, 0f, eventDurationSeconds - 0.001f),
                2026, peakMonthDay[showerIndex] / 100, peakMonthDay[showerIndex] % 100,
                0, 0, 0.0, showerIndex, NormalizeViewForward(viewForward));
        }

        public int DebugGetForcedMeteorCount()
        {
            return DebugForcedMeteorCount;
        }

        public int DebugGetForcedShowerIndex()
        {
            return _debugForcedShowerIndex;
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

        public static float CalculateDateActivity(
            int year, int month, int day, int startMonthDay, int peakMonthDayValue, int endMonthDay)
        {
            int daysInYear = IsLeapYear(year) ? 366 : 365;
            int dateOrdinal = DayOfYear(year, month, day) - 1;
            int startOrdinal = DayOfYear(year, startMonthDay / 100, startMonthDay % 100) - 1;
            int peakOrdinal = DayOfYear(year, peakMonthDayValue / 100, peakMonthDayValue % 100) - 1;
            int endOrdinal = DayOfYear(year, endMonthDay / 100, endMonthDay % 100) - 1;
            int span = PositiveModulo(endOrdinal - startOrdinal, daysInYear);
            int position = PositiveModulo(dateOrdinal - startOrdinal, daysInYear);
            if (position > span) return 0f;
            int peakPosition = PositiveModulo(peakOrdinal - startOrdinal, daysInYear);
            if (peakPosition > span) peakPosition = span / 2;
            if (position <= peakPosition)
                return peakPosition == 0 ? 1f : Mathf.Clamp01((float)position / peakPosition);
            int fallingSpan = span - peakPosition;
            return fallingSpan == 0 ? 1f : Mathf.Clamp01((float)(span - position) / fallingSpan);
        }

        public int DebugGetStrongestShowerIndex(
            int year, int month, int day, int hour, int minute, double second)
        {
            return GetStrongestShowerIndex(year, month, day, hour, minute, second);
        }

        private void UpdateMeteorVisuals(
            int eventId, float elapsed, int year, int month, int day, int hour, int minute, double second,
            int forcedShowerIndex, Vector3 forcedViewForward)
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
            bool forcedPreview = forcedShowerIndex >= 0 && forcedShowerIndex < showerIds.Length;
            int showerIndex = forcedPreview ? forcedShowerIndex :
                GetStrongestShowerIndex(year, month, day, hour, minute, second);
            int targetCount = forcedPreview ? DebugForcedMeteorCount : showerIndex < 0 ? 2 : CalculateCompressedCount(
                CalculateDateActivity(year, month, day, activeStartMonthDay[showerIndex],
                    peakMonthDay[showerIndex], activeEndMonthDay[showerIndex]),
                zenithalHourlyRates[showerIndex]);
            Vector3 radiant = showerIndex < 0 ? Vector3.zero :
                RealSkyController.EquatorialDirectionToHorizontal(
                    radiantRightAscensionDegrees[showerIndex], radiantDeclinationDegrees[showerIndex],
                    year, month, day, hour, minute, second, latitudeDegrees, longitudeDegreesEast);
            Vector3 localViewForward = transform.InverseTransformDirection(
                EnsureSkywardViewForward(forcedViewForward)).normalized;
            if (forcedPreview)
            {
                Vector3 localUp = transform.InverseTransformDirection(Vector3.up).normalized;
                radiant = (localViewForward + localUp * 0.48f).normalized;
            }

            for (int slot = 0; slot < count; slot++)
            {
                int eventSlot = wave * count + slot;
                float onset = 0.35f + slot * 0.88f + DebugSampleValue(eventId, wave, slot, 0) * 0.28f;
                float duration = 0.9f + DebugSampleValue(eventId, wave, slot, 1) * 0.45f;
                float progress = (waveTime - onset) / duration;
                bool visible = eventSlot < targetCount && progress >= 0f && progress <= 1f &&
                               elapsed < eventDurationSeconds;
                meteorRenderers[slot].enabled = visible;
                if (visible) ConfigureMeteor(eventId, wave, slot, Mathf.Clamp01(progress), radiant,
                    showerIndex >= 0, forcedPreview, localViewForward);
            }
        }

        private int GetStrongestShowerIndex(
            int year, int month, int day, int hour, int minute, double second)
        {
            if (!CatalogLengthsMatch()) return -1;
            int bestIndex = -1;
            float bestScore = 0f;
            for (int index = 0; index < showerIds.Length; index++)
            {
                float activity = CalculateDateActivity(year, month, day, activeStartMonthDay[index],
                    peakMonthDay[index], activeEndMonthDay[index]);
                if (activity <= 0f) continue;
                Vector3 radiant = RealSkyController.EquatorialDirectionToHorizontal(
                    radiantRightAscensionDegrees[index], radiantDeclinationDegrees[index],
                    year, month, day, hour, minute, second, latitudeDegrees, longitudeDegreesEast);
                float score = activity * Mathf.Max(0f, radiant.y) * zenithalHourlyRates[index];
                if (score > bestScore) { bestScore = score; bestIndex = index; }
            }
            return bestIndex;
        }

        private static int CalculateCompressedCount(float activity, int zhr)
        {
            float strength = Mathf.Clamp01(activity * zhr / 100f);
            return Mathf.Clamp(Mathf.RoundToInt(2f + 18f * Mathf.Sqrt(strength)), 2, 20);
        }

        private void ConfigureMeteor(
            int eventId, int wave, int slot, float progress, Vector3 radiant, bool showerActive,
            bool forcedPreview, Vector3 localViewForward)
        {
            Transform meteor = meteorTransforms[slot];
            Vector3 radial;
            Vector3 motion;
            if (showerActive)
            {
                if (forcedPreview && slot == 0)
                {
                    radial = localViewForward;
                    motion = (radial * Vector3.Dot(radial, radiant) - radiant).normalized;
                }
                else
                {
                    Vector3 basis = Vector3.Cross(radiant,
                        Mathf.Abs(radiant.y) > 0.92f ? Vector3.right : Vector3.up).normalized;
                    Vector3 secondBasis = Vector3.Cross(radiant, basis).normalized;
                    float around = DebugSampleValue(eventId, wave, slot, 2) * Mathf.PI * 2f;
                    Vector3 away = (basis * Mathf.Cos(around) + secondBasis * Mathf.Sin(around)).normalized;
                    float minimumSeparation = forcedPreview ? 12f : 24f;
                    float maximumSeparation = forcedPreview ? 42f : 64f;
                    float separation = Mathf.Lerp(minimumSeparation, maximumSeparation,
                        DebugSampleValue(eventId, wave, slot, 3)) * Mathf.Deg2Rad;
                    radial = (radiant * Mathf.Cos(separation) + away * Mathf.Sin(separation)).normalized;
                    motion = (radial * Mathf.Cos(separation) - radiant).normalized;
                }
            }
            else
            {
                float azimuth = DebugSampleValue(eventId, wave, slot, 2) * Mathf.PI * 2f;
                float elevation = Mathf.Lerp(28f, 72f, DebugSampleValue(eventId, wave, slot, 3)) * Mathf.Deg2Rad;
                radial = new Vector3(Mathf.Sin(azimuth) * Mathf.Cos(elevation), Mathf.Sin(elevation),
                    Mathf.Cos(azimuth) * Mathf.Cos(elevation)).normalized;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized;
                Vector3 bitangent = Vector3.Cross(radial, tangent).normalized;
                float trackAngle = DebugSampleValue(eventId, wave, slot, 4) * Mathf.PI * 2f;
                motion = (tangent * Mathf.Cos(trackAngle) + bitangent * Mathf.Sin(trackAngle)).normalized;
            }

            float travel = Mathf.Lerp(8f, -8f, progress);
            meteor.localPosition = radial * skyRadius + motion * travel;
            meteor.localRotation = Quaternion.LookRotation(-radial, motion);
            float length = Mathf.Lerp(7f, 11f, DebugSampleValue(eventId, wave, slot, 5));
            float fade = Mathf.Sin(progress * Mathf.PI);
            float debugWidthScale = forcedPreview ? 2.4f : 1f;
            float debugLengthScale = forcedPreview ? 1.6f : 1f;
            meteor.localScale = new Vector3(
                0.22f * fade * debugWidthScale, length * fade * debugLengthScale, 1f);
        }

        private bool CatalogLengthsMatch()
        {
            int count = showerIds == null ? 0 : showerIds.Length;
            return count == 11 && showerNamesJa != null && showerNamesJa.Length == count &&
                   activeStartMonthDay != null && activeStartMonthDay.Length == count &&
                   activeEndMonthDay != null && activeEndMonthDay.Length == count &&
                   peakMonthDay != null && peakMonthDay.Length == count &&
                   radiantRightAscensionDegrees != null && radiantRightAscensionDegrees.Length == count &&
                   radiantDeclinationDegrees != null && radiantDeclinationDegrees.Length == count &&
                   zenithalHourlyRates != null && zenithalHourlyRates.Length == count;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static Vector3 NormalizeViewForward(Vector3 viewForward)
        {
            return viewForward.sqrMagnitude < 0.0001f ? Vector3.forward : viewForward.normalized;
        }

        private static Vector3 EnsureSkywardViewForward(Vector3 viewForward)
        {
            Vector3 normalized = NormalizeViewForward(viewForward);
            const float minimumVertical = 0.22f;
            if (normalized.y >= minimumVertical) return normalized;

            Vector3 horizontal = new Vector3(normalized.x, 0f, normalized.z);
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.forward;
            float horizontalScale = Mathf.Sqrt(1f - minimumVertical * minimumVertical);
            return horizontal.normalized * horizontalScale + Vector3.up * minimumVertical;
        }

        private static bool IsLeapYear(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        private static int DayOfYear(int year, int month, int day)
        {
            int result = day;
            if (month > 1) result += 31;
            if (month > 2) result += IsLeapYear(year) ? 29 : 28;
            if (month > 3) result += 31;
            if (month > 4) result += 30;
            if (month > 5) result += 31;
            if (month > 6) result += 30;
            if (month > 7) result += 31;
            if (month > 8) result += 31;
            if (month > 9) result += 30;
            if (month > 10) result += 31;
            if (month > 11) result += 30;
            return result;
        }

        private void SetAllVisible(bool visible)
        {
            if (meteorRenderers == null) return;
            for (int index = 0; index < meteorRenderers.Length; index++)
                if (meteorRenderers[index] != null) meteorRenderers[index].enabled = visible;
        }
    }
}
