using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RealSkyController : UdonSharpBehaviour
    {
        [Header("Scene References")]
        public Transform celestialSphere;

        [Header("Tokyo Observatory")]
        public float latitudeDegrees = 35.68f;
        public float longitudeDegreesEast = 139.76f;
        [Range(10f, 30f)] public float updateIntervalSeconds = 15f;

        [Header("Debug (local only)")]
        public float debugTimeOffsetHours;

        private float _nextUpdateTime;
        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            FollowLocalPlayer();
            ApplyCurrentSkyRotation();
            _nextUpdateTime = Time.time + updateIntervalSeconds;
        }

        private void Update()
        {
            FollowLocalPlayer();
            if (Time.time < _nextUpdateTime) return;
            ApplyCurrentSkyRotation();
            _nextUpdateTime = Time.time + updateIntervalSeconds;
        }

        private void FollowLocalPlayer()
        {
            if (celestialSphere == null || _localPlayer == null) return;
            celestialSphere.position = _localPlayer.GetPosition();
        }

        public void ApplyCurrentSkyRotation()
        {
            if (celestialSphere == null) return;

            DateTime utc = Networking.GetNetworkDateTime();
            if (!Mathf.Approximately(debugTimeOffsetHours, 0f))
                utc = utc.AddHours(debugTimeOffsetHours);
            celestialSphere.localRotation = CalculateSkyRotation(
                utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
                utc.Second + utc.Millisecond / 1000.0,
                latitudeDegrees, longitudeDegreesEast);
        }

        public void DebugAdvanceOneHour()
        {
            debugTimeOffsetHours += 1f;
            ApplyCurrentSkyRotation();
        }

        public void DebugResetTimeOffset()
        {
            debugTimeOffsetHours = 0f;
            ApplyCurrentSkyRotation();
        }

        public static Quaternion CalculateSkyRotation(
            int year, int month, int day, int hour, int minute, double second,
            float latitude, float longitudeEast)
        {
            double dayWithTime = day + (hour + (minute + second / 60.0) / 60.0) / 24.0;
            double julianDate = JulianDate(year, month, dayWithTime);
            double centuries = (julianDate - 2451545.0) / 36525.0;
            double greenwichSiderealDegrees = 280.46061837 +
                                              360.98564736629 * (julianDate - 2451545.0) +
                                              0.000387933 * centuries * centuries -
                                              centuries * centuries * centuries / 38710000.0;
            double localSiderealDegrees = NormalizeDegrees(greenwichSiderealDegrees + longitudeEast);

            float siderealRadians = (float)(localSiderealDegrees * Math.PI / 180.0);
            float latitudeRadians = latitude * Mathf.Deg2Rad;
            float sinLatitude = Mathf.Sin(latitudeRadians);
            float cosLatitude = Mathf.Cos(latitudeRadians);
            float sinSidereal = Mathf.Sin(siderealRadians);
            float cosSidereal = Mathf.Cos(siderealRadians);

            // Equatorial +Y is the north celestial pole and +Z is RA=6h.
            Vector3 northCelestialPole = new Vector3(0f, sinLatitude, cosLatitude);
            Vector3 rightAscensionSixHours = new Vector3(
                cosSidereal,
                cosLatitude * sinSidereal,
                -sinLatitude * sinSidereal);
            return Quaternion.LookRotation(
                rightAscensionSixHours.normalized,
                northCelestialPole.normalized);
        }

        private static double JulianDate(int year, int month, double day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            int century = year / 100;
            int correction = 2 - century + century / 4;
            return Math.Floor(365.25 * (year + 4716)) +
                   Math.Floor(30.6001 * (month + 1)) +
                   day + correction - 1524.5;
        }

        private static double NormalizeDegrees(double degrees)
        {
            degrees %= 360.0;
            return degrees < 0.0 ? degrees + 360.0 : degrees;
        }
    }
}
