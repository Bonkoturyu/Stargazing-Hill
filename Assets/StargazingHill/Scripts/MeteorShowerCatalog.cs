using UnityEngine;

namespace StargazingHill
{
    [CreateAssetMenu(menuName = "Stargazing Hill/Meteor Shower Catalog")]
    public class MeteorShowerCatalog : ScriptableObject
    {
        public string sourceTitle = "IMO Meteor Shower Calendar 2026";
        public string sourceUrl = "https://imo.net/files/meteor-shower/cal2026.pdf";
        public string verifiedDate = "2026-08-12";
        public string[] ids;
        public string[] namesJa;
        public int[] activeStartMonthDay;
        public int[] activeEndMonthDay;
        public int[] peakMonthDay;
        public float[] radiantRightAscensionDegrees;
        public float[] radiantDeclinationDegrees;
        public int[] zenithalHourlyRates;
    }
}
