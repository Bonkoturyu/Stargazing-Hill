using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MeteorController : UdonSharpBehaviour
    {
        // Single source of truth for meteor event timing. Change these two values when tuning durations.
        public const float NaturalEventDurationSeconds = 180f;
        public const float DebugForcedPreviewDurationSeconds = 25f;

        [Header("Visual Pool")]
        public Transform[] meteorTransforms;
        public Renderer[] meteorRenderers;
        public Material[] meteorMaterials;

        [Header("Hourly Event")]
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
        public float[] geocentricVelocityKilometersPerSecond;
        public float[] populationIndices;

        [HideInInspector] public int debugRequestedShowerIndex = 4;
        [HideInInspector] public Vector3 debugRequestedViewForward = Vector3.forward;
        [HideInInspector] public bool debugPreviewActive;
        [HideInInspector] public float debugPreviewElapsedSeconds = -1f;
        [HideInInspector] public int debugVisibleMeteorCount;
        [HideInInspector] public string debugPreviewShowerId = "";
        [HideInInspector] public bool debugEventPlaying;
        [HideInInspector] public string debugEventMode = "IDLE";
        [HideInInspector] public string debugCurrentShowerId = "";
        [HideInInspector] public float debugEventElapsedSeconds = -1f;
        [HideInInspector] public float debugEventDurationSeconds;

        private VRCPlayerApi _localPlayer;
        private bool _debugEventActive;
        private float _debugStartTime;
        private int _debugEventId;
        private DateTime _debugUtc;
        private int _debugForcedShowerIndex = -1;
        private Vector3 _debugViewForward = Vector3.forward;
        private int _suppressedNaturalEventId = int.MinValue;

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
            bool forcedDebugEvent = _debugEventActive;
            if (_debugEventActive)
            {
                elapsed = Time.time - _debugStartTime;
                eventId = _debugEventId;
                utc = _debugUtc;
                float debugDuration = _debugForcedShowerIndex >= 0
                    ? DebugForcedPreviewDurationSeconds
                    : NaturalEventDurationSeconds;
                if (elapsed >= debugDuration)
                {
                    _debugEventActive = false;
                    forcedDebugEvent = false;
                    elapsed = -1f;
                }
            }
            if (!_debugEventActive)
            {
                float secondsIntoHour = utc.Minute * 60f + utc.Second + utc.Millisecond / 1000f;
                int naturalEventId = GetHourlyEventId(utc.Year, utc.Month, utc.Day, utc.Hour);
                if (secondsIntoHour < NaturalEventDurationSeconds && naturalEventId != _suppressedNaturalEventId)
                {
                    elapsed = secondsIntoHour;
                    eventId = naturalEventId;
                }
            }

            UpdateMeteorVisuals(eventId, elapsed, utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
                utc.Second + utc.Millisecond / 1000.0, _debugEventActive ? _debugForcedShowerIndex : -1,
                _debugViewForward);
            debugPreviewActive = _debugEventActive;
            debugPreviewElapsedSeconds = _debugEventActive ? elapsed : -1f;
            debugEventPlaying = elapsed >= 0f;
            debugEventMode = !debugEventPlaying ? "IDLE" : forcedDebugEvent && _debugForcedShowerIndex >= 0
                ? "FORCED" : "NATURAL";
            debugEventElapsedSeconds = debugEventPlaying ? elapsed : -1f;
            debugEventDurationSeconds = forcedDebugEvent && _debugForcedShowerIndex >= 0
                ? DebugForcedPreviewDurationSeconds : NaturalEventDurationSeconds;
            if (!debugEventPlaying) debugCurrentShowerId = "";
        }

        public void DebugTriggerHourlyEvent()
        {
            _debugUtc = Networking.GetNetworkDateTime();
            _debugEventId = GetHourlyEventId(_debugUtc.Year, _debugUtc.Month, _debugUtc.Day, _debugUtc.Hour);
            _debugStartTime = Time.time;
            _debugForcedShowerIndex = -1;
            _suppressedNaturalEventId = int.MinValue;
            _debugEventActive = true;
            UpdateMeteorVisuals(_debugEventId, 0f, _debugUtc.Year, _debugUtc.Month, _debugUtc.Day,
                _debugUtc.Hour, _debugUtc.Minute, _debugUtc.Second + _debugUtc.Millisecond / 1000.0,
                -1, _debugViewForward);
        }

        public void DebugTriggerSelectedShower()
        {
            int showerIndex = debugRequestedShowerIndex;
            Vector3 viewForward = debugRequestedViewForward;
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
            _suppressedNaturalEventId = int.MinValue;
            _debugStartTime = Time.time - DebugImmediatePreviewElapsed;
            _debugEventActive = true;
            debugPreviewActive = true;
            debugPreviewElapsedSeconds = DebugImmediatePreviewElapsed;
            debugPreviewShowerId = showerIds[showerIndex];
            UpdateMeteorVisuals(_debugEventId, DebugImmediatePreviewElapsed,
                _debugUtc.Year, _debugUtc.Month, _debugUtc.Day, _debugUtc.Hour, _debugUtc.Minute,
                _debugUtc.Second + _debugUtc.Millisecond / 1000.0, showerIndex, _debugViewForward);
            Debug.Log("[Stargazing Hill] Forced meteor shower preview: " + showerNamesJa[showerIndex] +
                      " / " + showerIds[showerIndex] + " (" + DebugForcedMeteorCount + " meteors over " +
                      DebugForcedPreviewDurationSeconds + " seconds, local-only accelerated QA).");
        }

        public void DebugStopHourlyEvent()
        {
            DateTime now = Networking.GetNetworkDateTime();
            _suppressedNaturalEventId = GetHourlyEventId(now.Year, now.Month, now.Day, now.Hour);
            _debugEventActive = false;
            _debugForcedShowerIndex = -1;
            debugPreviewActive = false;
            debugPreviewElapsedSeconds = -1f;
            debugVisibleMeteorCount = 0;
            debugPreviewShowerId = "";
            SetAllVisible(false);
        }

        public void DebugToggleHourlyEvent()
        {
            if (debugEventPlaying || _debugEventActive) DebugStopHourlyEvent();
            else DebugTriggerHourlyEvent();
        }

        public void DebugPreviewEventAtSecond(float elapsed)
        {
            int eventId = GetHourlyEventId(2026, 8, 13, 0);
            UpdateMeteorVisuals(eventId, Mathf.Clamp(elapsed, 0f, NaturalEventDurationSeconds - 0.001f),
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
            UpdateMeteorVisuals(eventId,
                Mathf.Clamp(elapsed, 0f, DebugForcedPreviewDurationSeconds - 0.001f),
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

        public static int CalculateVisualTier(float sample, float populationIndex)
        {
            float fireballChance = Mathf.Clamp(0.055f - (populationIndex - 2.1f) * 0.025f, 0.025f, 0.055f);
            float brightChance = Mathf.Clamp(0.33f - (populationIndex - 2.1f) * 0.15f, 0.20f, 0.33f);
            if (sample < fireballChance) return 2;
            return sample < fireballChance + brightChance ? 1 : 0;
        }

        public static float CalculateMeteorDuration(float velocityKilometersPerSecond, float variation)
        {
            float speed = Mathf.InverseLerp(20f, 71f, Mathf.Clamp(velocityKilometersPerSecond, 20f, 71f));
            return Mathf.Lerp(1.55f, 0.68f, speed) * Mathf.Lerp(0.86f, 1.14f, variation);
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

        public static float CalculateRadiantAltitudeFactor(
            float radiantWorldY, float latitude, float radiantDeclination)
        {
            if (radiantWorldY <= 0f) return 0f;
            float maximumAltitudeDegrees = 90f - Mathf.Abs(latitude - radiantDeclination);
            if (maximumAltitudeDegrees <= 0f) return 0f;
            float maximumWorldY = Mathf.Sin(Mathf.Min(90f, maximumAltitudeDegrees) * Mathf.Deg2Rad);
            if (maximumWorldY <= 0.0001f) return 0f;
            return Mathf.Clamp01(radiantWorldY / maximumWorldY);
        }

        public static int CalculateExpectedHourlyCount(
            float activity, int peakHourlyRate, float radiantWorldY,
            float latitude, float radiantDeclination)
        {
            float altitudeFactor = CalculateRadiantAltitudeFactor(
                radiantWorldY, latitude, radiantDeclination);
            return Mathf.Max(0, Mathf.RoundToInt(
                Mathf.Clamp01(activity) * Mathf.Max(0, peakHourlyRate) * altitudeFactor));
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
                debugVisibleMeteorCount = 0;
                SetAllVisible(false);
                return;
            }

            int count = Mathf.Min(meteorTransforms.Length, meteorRenderers.Length);
            if (count <= 0)
            {
                debugVisibleMeteorCount = 0;
                return;
            }

            bool forcedPreview = forcedShowerIndex >= 0 && forcedShowerIndex < showerIds.Length;
            int showerIndex = forcedPreview ? forcedShowerIndex :
                GetStrongestShowerIndex(year, month, day, hour, minute, second);
            debugCurrentShowerId = showerIndex < 0 ? "SPORADIC" : showerIds[showerIndex];
            Vector3 radiant = showerIndex < 0 ? Vector3.zero :
                RealSkyController.EquatorialDirectionToHorizontal(
                    radiantRightAscensionDegrees[showerIndex], radiantDeclinationDegrees[showerIndex],
                    year, month, day, hour, minute, second, latitudeDegrees, longitudeDegreesEast);
            Vector3 localViewForward = transform.InverseTransformDirection(
                EnsureSkywardViewForward(forcedViewForward)).normalized;
            if (forcedPreview)
            {
                Vector3 localUp = transform.InverseTransformDirection(Vector3.up).normalized;
                Vector3 localRight = Vector3.Cross(localUp, localViewForward).normalized;
                radiant = (localViewForward + localUp * 0.42f + localRight * 0.30f).normalized;
            }

            int targetCount;
            if (forcedPreview)
            {
                targetCount = DebugForcedMeteorCount;
            }
            else if (showerIndex < 0)
            {
                // One hour of background activity, represented as one or two scattered meteors.
                targetCount = DebugSampleValue(eventId, 0, 0, 9) < 0.5f ? 1 : 2;
            }
            else
            {
                float activity = CalculateDateActivity(year, month, day,
                    activeStartMonthDay[showerIndex], peakMonthDay[showerIndex], activeEndMonthDay[showerIndex]);
                targetCount = CalculateExpectedHourlyCount(activity,
                    GetJapanDarkSkyPeakHourlyRate(showerIndex), radiant.y,
                    latitudeDegrees, radiantDeclinationDegrees[showerIndex]);
                if (targetCount <= 0)
                {
                    targetCount = DebugSampleValue(eventId, 0, 0, 9) < 0.5f ? 1 : 2;
                    showerIndex = -1;
                    radiant = Vector3.zero;
                }
            }

            float scheduleDuration = forcedPreview
                ? DebugForcedPreviewDurationSeconds
                : NaturalEventDurationSeconds;
            int waveCount = Mathf.Max(1, Mathf.CeilToInt((float)targetCount / count));

            // Forced QA remains the historic 5-second wave so the first meteor appears immediately.
            // Natural events derive wave length from target count, distributing the whole expected hour over 180 seconds.
            const float waveLength = 5f;
            float currentWaveLength = forcedPreview ? waveLength : scheduleDuration / waveCount;
            int wave = Mathf.Min(waveCount - 1, Mathf.FloorToInt(elapsed / currentWaveLength));
            float waveTime = elapsed - wave * currentWaveLength;
            int firstEventIndex = wave * count;
            int eventsThisWave = Mathf.Min(count, targetCount - firstEventIndex);
            if (eventsThisWave <= 0)
            {
                debugVisibleMeteorCount = 0;
                SetAllVisible(false);
                return;
            }

            float slotSpacing = currentWaveLength / eventsThisWave;
            int visibleCount = 0;
            for (int slot = 0; slot < count; slot++)
            {
                int eventSlot = firstEventIndex + slot;
                bool scheduled = slot < eventsThisWave && eventSlot < targetCount;
                // Forced QA keeps the historic onset ramp. Centring a slot inside its spacing puts the first
                // meteor at 0.625s +/- jitter, which is not reliably before the 0.75s the forced preview
                // starts at, so pressing a debug button could show nothing at all.
                float jitter = (DebugSampleValue(eventId, wave, slot, 0) - 0.5f) * slotSpacing * 0.45f;
                float onset = forcedPreview
                    ? 0.35f + slot * 0.88f + DebugSampleValue(eventId, wave, slot, 0) * 0.28f
                    : (slot + 0.5f) * slotSpacing + jitter;
                float velocity = showerIndex < 0 ? 42f : geocentricVelocityKilometersPerSecond[showerIndex];
                float duration = CalculateMeteorDuration(
                    velocity, DebugSampleValue(eventId, wave, slot, 1));
                float progress = (waveTime - onset) / duration;
                bool visible = scheduled && progress >= 0f && progress <= 1f &&
                               elapsed < scheduleDuration;
                meteorRenderers[slot].enabled = visible;
                if (visible) ConfigureMeteor(eventId, wave, slot, Mathf.Clamp01(progress), radiant,
                    showerIndex, forcedPreview, localViewForward, velocity);
                if (visible) visibleCount++;
            }
            debugVisibleMeteorCount = visibleCount;
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
                float altitudeFactor = CalculateRadiantAltitudeFactor(
                    radiant.y, latitudeDegrees, radiantDeclinationDegrees[index]);
                float score = activity * altitudeFactor * GetJapanDarkSkyPeakHourlyRate(index);
                if (score > bestScore) { bestScore = score; bestIndex = index; }
            }
            return bestIndex;
        }

        // NAOJ "Major Meteor Showers": expected meteors per hour near maximum from Japan under a dark sky
        // (no twilight/moon impact; approximately limiting magnitude 5.5). This is deliberately not ZHR.
        private static int GetJapanDarkSkyPeakHourlyRate(int showerIndex)
        {
            if (showerIndex == 0) return 30; // Quadrantids
            if (showerIndex == 1) return 10; // April Lyrids
            if (showerIndex == 2) return 5;  // eta Aquariids
            if (showerIndex == 3) return 5;  // Southern delta Aquariids
            if (showerIndex == 4) return 40; // Perseids
            if (showerIndex == 5) return 2;  // October Draconids
            if (showerIndex == 6) return 10; // Orionids
            if (showerIndex == 7) return 3;  // Southern Taurids
            if (showerIndex == 8) return 2;  // Northern Taurids
            if (showerIndex == 9) return 4;  // Leonids
            if (showerIndex == 10) return 60; // Geminids
            return 0;
        }

        private void ConfigureMeteor(
            int eventId, int wave, int slot, float progress, Vector3 radiant, int showerIndex,
            bool forcedPreview, Vector3 localViewForward, float velocityKilometersPerSecond)
        {
            Transform meteor = meteorTransforms[slot];
            bool showerActive = showerIndex >= 0;
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

            float speed = Mathf.InverseLerp(20f, 71f, Mathf.Clamp(velocityKilometersPerSecond, 20f, 71f));
            float travelDistance = Mathf.Lerp(12f, 22f, speed);
            float travel = Mathf.Lerp(travelDistance * -0.5f, travelDistance * 0.5f, progress);
            meteor.localPosition = radial * skyRadius + motion * travel;
            meteor.localRotation = Quaternion.LookRotation(-radial, motion);
            float populationIndex = showerIndex < 0 ? 2.5f : populationIndices[showerIndex];
            int visualTier = CalculateVisualTier(
                DebugSampleValue(eventId, wave, slot, 6), populationIndex);
            if (forcedPreview && wave == 0 && slot == 0) visualTier = 2;
            if (meteorMaterials != null && visualTier < meteorMaterials.Length && meteorMaterials[visualTier] != null)
                meteorRenderers[slot].sharedMaterial = meteorMaterials[visualTier];

            float length = Mathf.Lerp(4.5f, 7f, speed) *
                           Mathf.Lerp(0.80f, 1.25f, DebugSampleValue(eventId, wave, slot, 5));
            if (visualTier == 1) length *= 1.30f;
            else if (visualTier == 2) length *= 1.70f;
            float fade = Mathf.Sin(progress * Mathf.PI);
            float width = visualTier == 0 ? 0.15f : visualTier == 1 ? 0.21f : 0.30f;
            meteor.localScale = new Vector3(
                width * fade, length * Mathf.Clamp01(fade * 2.5f), 1f);
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
                   zenithalHourlyRates != null && zenithalHourlyRates.Length == count &&
                   geocentricVelocityKilometersPerSecond != null &&
                   geocentricVelocityKilometersPerSecond.Length == count &&
                   populationIndices != null && populationIndices.Length == count;
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
