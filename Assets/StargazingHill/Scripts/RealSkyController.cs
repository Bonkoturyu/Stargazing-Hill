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
        public Transform moonTransform;
        public Renderer moonRenderer;
        public float moonRadius = 180f;

        [Header("Observatory (east longitude is positive)")]
        public string observatoryProfileId = "tokyo";
        [Range(-90f, 90f)] public float latitudeDegrees = 35.68f;
        [Range(-180f, 180f)] public float longitudeDegreesEast = 139.76f;
        [Range(10f, 30f)] public float updateIntervalSeconds = 15f;

        [Header("Debug (local only)")]
        public float debugTimeOffsetHours;

        private float _nextUpdateTime;
        private VRCPlayerApi _localPlayer;
        private Vector3 _moonDirection;
        private bool _moonVisible;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            ApplyCurrentSkyRotation();
            FollowLocalPlayer();
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
            if (_localPlayer == null) return;
            Vector3 observer = _localPlayer.GetPosition();
            if (celestialSphere != null) celestialSphere.position = observer;
            if (moonTransform != null)
            {
                moonTransform.position = observer + _moonDirection * moonRadius;
                Vector3 billboardUp = Mathf.Abs(_moonDirection.y) > 0.98f ? Vector3.forward : Vector3.up;
                moonTransform.rotation = Quaternion.LookRotation(-_moonDirection, billboardUp);
            }
        }

        public void ApplyCurrentSkyRotation()
        {
            DateTime utc = Networking.GetNetworkDateTime();
            if (!Mathf.Approximately(debugTimeOffsetHours, 0f)) utc = utc.AddHours(debugTimeOffsetHours);
            if (celestialSphere != null)
            {
                celestialSphere.localRotation = CalculateSkyRotation(
                    utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
                    utc.Second + utc.Millisecond / 1000.0,
                    latitudeDegrees, longitudeDegreesEast);
            }

            _moonDirection = CalculateMoonDirection(
                utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
                utc.Second + utc.Millisecond / 1000.0,
                latitudeDegrees, longitudeDegreesEast);
            _moonVisible = _moonDirection.y > 0f;
            if (moonRenderer != null) moonRenderer.enabled = _moonVisible;
            FollowLocalPlayer();
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
            double localSiderealDegrees = LocalSiderealDegrees(
                year, month, day, hour, minute, second, longitudeEast);
            float siderealRadians = (float)(localSiderealDegrees * Math.PI / 180.0);
            float latitudeRadians = latitude * Mathf.Deg2Rad;
            float sinLatitude = Mathf.Sin(latitudeRadians);
            float cosLatitude = Mathf.Cos(latitudeRadians);
            float sinSidereal = Mathf.Sin(siderealRadians);
            float cosSidereal = Mathf.Cos(siderealRadians);

            Vector3 northCelestialPole = new Vector3(0f, sinLatitude, cosLatitude);
            Vector3 rightAscensionSixHours = new Vector3(
                cosSidereal,
                cosLatitude * sinSidereal,
                -sinLatitude * sinSidereal);
            return Quaternion.LookRotation(rightAscensionSixHours.normalized, northCelestialPole.normalized);
        }

        // Low-cost lunar ephemeris: osculating orbit plus the dominant lunar perturbations,
        // followed by topocentric parallax on an oblate Earth. It is intentionally independent
        // of the observatory profile so any latitude/east-longitude pair can be supplied.
        public static Vector3 CalculateMoonDirection(
            int year, int month, int day, int hour, int minute, double second,
            float latitude, float longitudeEast)
        {
            double dayWithTime = day + (hour + (minute + second / 60.0) / 60.0) / 24.0;
            double jd = JulianDate(year, month, dayWithTime);
            double days = jd - 2451543.5;
            double node = NormalizeDegrees(125.1228 - 0.0529538083 * days);
            double inclination = 5.1454;
            double periapsis = NormalizeDegrees(318.0634 + 0.1643573223 * days);
            double eccentricity = 0.0549;
            double meanAnomaly = NormalizeDegrees(115.3654 + 13.0649929509 * days);
            double eccentricAnomaly = meanAnomaly + eccentricity * 180.0 / Math.PI *
                SinDegrees(meanAnomaly) * (1.0 + eccentricity * CosDegrees(meanAnomaly));
            double xOrbit = 60.2666 * (CosDegrees(eccentricAnomaly) - eccentricity);
            double yOrbit = 60.2666 * Math.Sqrt(1.0 - eccentricity * eccentricity) *
                            SinDegrees(eccentricAnomaly);
            double trueAnomaly = Math.Atan2(yOrbit, xOrbit) * 180.0 / Math.PI;
            double distanceEarthRadii = Math.Sqrt(xOrbit * xOrbit + yOrbit * yOrbit);
            double argument = trueAnomaly + periapsis;
            double eclipticX = distanceEarthRadii *
                (CosDegrees(node) * CosDegrees(argument) -
                 SinDegrees(node) * SinDegrees(argument) * CosDegrees(inclination));
            double eclipticY = distanceEarthRadii *
                (SinDegrees(node) * CosDegrees(argument) +
                 CosDegrees(node) * SinDegrees(argument) * CosDegrees(inclination));
            double eclipticZ = distanceEarthRadii * SinDegrees(argument) * SinDegrees(inclination);
            double eclipticLongitude = Math.Atan2(eclipticY, eclipticX) * 180.0 / Math.PI;
            double eclipticLatitude = Math.Atan2(
                eclipticZ, Math.Sqrt(eclipticX * eclipticX + eclipticY * eclipticY)) * 180.0 / Math.PI;

            double sunPeriapsis = NormalizeDegrees(282.9404 + 0.0000470935 * days);
            double sunMeanAnomaly = NormalizeDegrees(356.0470 + 0.9856002585 * days);
            double sunMeanLongitude = NormalizeDegrees(sunPeriapsis + sunMeanAnomaly);
            double moonMeanLongitude = NormalizeDegrees(node + periapsis + meanAnomaly);
            double elongation = NormalizeDegrees(moonMeanLongitude - sunMeanLongitude);
            double argumentLatitude = NormalizeDegrees(moonMeanLongitude - node);

            eclipticLongitude +=
                -1.274 * SinDegrees(meanAnomaly - 2.0 * elongation) +
                 0.658 * SinDegrees(2.0 * elongation) -
                 0.186 * SinDegrees(sunMeanAnomaly) -
                 0.059 * SinDegrees(2.0 * meanAnomaly - 2.0 * elongation) -
                 0.057 * SinDegrees(meanAnomaly - 2.0 * elongation + sunMeanAnomaly) +
                 0.053 * SinDegrees(meanAnomaly + 2.0 * elongation) +
                 0.046 * SinDegrees(2.0 * elongation - sunMeanAnomaly) +
                 0.041 * SinDegrees(meanAnomaly - sunMeanAnomaly) -
                 0.035 * SinDegrees(elongation) -
                 0.031 * SinDegrees(meanAnomaly + sunMeanAnomaly) -
                 0.015 * SinDegrees(2.0 * argumentLatitude - 2.0 * elongation) +
                 0.011 * SinDegrees(meanAnomaly - 4.0 * elongation);
            eclipticLatitude +=
                -0.173 * SinDegrees(argumentLatitude - 2.0 * elongation) -
                 0.055 * SinDegrees(meanAnomaly - argumentLatitude - 2.0 * elongation) -
                 0.046 * SinDegrees(meanAnomaly + argumentLatitude - 2.0 * elongation) +
                 0.033 * SinDegrees(argumentLatitude + 2.0 * elongation) +
                 0.017 * SinDegrees(2.0 * meanAnomaly + argumentLatitude);

            double x = distanceEarthRadii * CosDegrees(eclipticLongitude) * CosDegrees(eclipticLatitude);
            double y = distanceEarthRadii * SinDegrees(eclipticLongitude) * CosDegrees(eclipticLatitude);
            double z = distanceEarthRadii * SinDegrees(eclipticLatitude);
            double obliquity = 23.4393 - 0.0000003563 * days;
            double equatorialX = x;
            double equatorialY = y * CosDegrees(obliquity) - z * SinDegrees(obliquity);
            double equatorialZ = y * SinDegrees(obliquity) + z * CosDegrees(obliquity);

            double sidereal = LocalSiderealDegrees(year, month, day, hour, minute, second, longitudeEast);
            double latitudeRadians = latitude * Math.PI / 180.0;
            double geocentricLatitude = Math.Atan(0.99664719 * Math.Tan(latitudeRadians));
            double observerEquatorialRadius = Math.Cos(geocentricLatitude);
            double observerPolarRadius = 0.99664719 * Math.Sin(geocentricLatitude);
            equatorialX -= observerEquatorialRadius * CosDegrees(sidereal);
            equatorialY -= observerEquatorialRadius * SinDegrees(sidereal);
            equatorialZ -= observerPolarRadius;

            return EquatorialVectorToHorizontal(
                equatorialX, equatorialY, equatorialZ, latitude, sidereal);
        }

        public static Vector3 EquatorialDirectionToHorizontal(
            float rightAscensionDegrees, float declinationDegrees,
            int year, int month, int day, int hour, int minute, double second,
            float latitude, float longitudeEast)
        {
            double x = CosDegrees(declinationDegrees) * CosDegrees(rightAscensionDegrees);
            double y = CosDegrees(declinationDegrees) * SinDegrees(rightAscensionDegrees);
            double z = SinDegrees(declinationDegrees);
            double sidereal = LocalSiderealDegrees(year, month, day, hour, minute, second, longitudeEast);
            return EquatorialVectorToHorizontal(x, y, z, latitude, sidereal);
        }

        private static Vector3 EquatorialVectorToHorizontal(
            double x, double y, double z, float latitude, double siderealDegrees)
        {
            double magnitude = Math.Sqrt(x * x + y * y + z * z);
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
            double latitudeRadians = latitude * Math.PI / 180.0;
            double siderealRadians = siderealDegrees * Math.PI / 180.0;
            double east = -Math.Sin(siderealRadians) * x + Math.Cos(siderealRadians) * y;
            double north = -Math.Sin(latitudeRadians) * Math.Cos(siderealRadians) * x -
                           Math.Sin(latitudeRadians) * Math.Sin(siderealRadians) * y +
                           Math.Cos(latitudeRadians) * z;
            double up = Math.Cos(latitudeRadians) * Math.Cos(siderealRadians) * x +
                        Math.Cos(latitudeRadians) * Math.Sin(siderealRadians) * y +
                        Math.Sin(latitudeRadians) * z;
            return new Vector3((float)east, (float)up, (float)north).normalized;
        }

        private static double LocalSiderealDegrees(
            int year, int month, int day, int hour, int minute, double second, float longitudeEast)
        {
            double dayWithTime = day + (hour + (minute + second / 60.0) / 60.0) / 24.0;
            double julianDate = JulianDate(year, month, dayWithTime);
            double centuries = (julianDate - 2451545.0) / 36525.0;
            double greenwichSiderealDegrees = 280.46061837 +
                                              360.98564736629 * (julianDate - 2451545.0) +
                                              0.000387933 * centuries * centuries -
                                              centuries * centuries * centuries / 38710000.0;
            return NormalizeDegrees(greenwichSiderealDegrees + longitudeEast);
        }

        private static double JulianDate(int year, int month, double day)
        {
            if (month <= 2) { year -= 1; month += 12; }
            int century = year / 100;
            int correction = 2 - century + century / 4;
            return Math.Floor(365.25 * (year + 4716)) +
                   Math.Floor(30.6001 * (month + 1)) + day + correction - 1524.5;
        }

        private static double SinDegrees(double degrees) { return Math.Sin(degrees * Math.PI / 180.0); }
        private static double CosDegrees(double degrees) { return Math.Cos(degrees * Math.PI / 180.0); }
        private static double NormalizeDegrees(double degrees)
        {
            degrees %= 360.0;
            return degrees < 0.0 ? degrees + 360.0 : degrees;
        }
    }
}
