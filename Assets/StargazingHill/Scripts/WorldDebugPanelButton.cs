using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldDebugPanelButton : UdonSharpBehaviour
    {
        public const int ActionTogglePanel = 0;
        public const int ActionForcedShower = 1;
        public const int ActionNaturalEvent = 2;
        public const int Action