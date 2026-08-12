using UnityEngine;

namespace StargazingHill
{
    [CreateAssetMenu(menuName = "Stargazing Hill/Observatory Profile")]
    public class ObservatoryProfile : ScriptableObject
    {
        public string profileId = "tokyo";
        public string displayName = "Tokyo";
        [Range(-90f, 90f)] public float latitudeDegrees = 35.68f;
        [Range(-180f, 180f)] public float longitudeDegreesEast = 139.76f;
    }
}
