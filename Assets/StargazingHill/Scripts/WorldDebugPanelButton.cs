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
        public const int ActionStopMeteor = 3;
        public const int ActionSkyMinusHour = 4;
        public const int ActionSkyPlusHour = 5;
        public const int ActionSkyReset = 6;

        [Header("Action")]
        public int action;
        public int showerIndex;

        [Header("References")]
        public GameObject panelRoot;
        public MeteorController meteorController;
        public RealSkyController skyController;

        public override void Interact()
        {
            if (action == ActionTogglePanel)
            {
                if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf);
                return;
            }

            if (action == ActionForcedShower)
            {
                TriggerForcedShower();
                return;
            }

            if (action == ActionNaturalEvent)
            {
                if (meteorController != null) meteorController.DebugTriggerHourlyEvent();
                return;
            }

            if (action == ActionStopMeteor)
            {
                if (meteorController != null) meteorController.DebugStopHourlyEvent();
                return;
            }

            if (action == ActionSkyMinusHour)
            {
                if (skyController != null)
                {
                    skyController.debugTimeOffsetHours -= 1f;
                    skyController.ApplyCurrentSkyRotation();
                }
                return;
            }

            if (action == ActionSkyPlusHour)
            {
                if (skyController != null) skyController.DebugAdvanceOneHour();
                return;
            }

            if (action == ActionSkyReset && skyController != null)
                skyController.DebugResetTimeOffset();
        }

        private void TriggerForcedShower()
        {
            if (meteorController == null || meteorController.showerIds == null ||
                showerIndex < 0 || showerIndex >= meteorController.showerIds.Length)
            {
                Debug.LogWarning("[Stargazing Hill] VR debug panel rejected invalid shower index " + showerIndex + ".");
                return;
            }

            VRCPlayerApi player = Networking.LocalPlayer;
            Vector3 viewForward = Vector3.forward;
            if (player != null)
                viewForward = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;

            meteorController.debugRequestedShowerIndex = showerIndex;
            meteorController.debugRequestedViewForward = viewForward;
            meteorController.DebugTriggerSelectedShower();
        }
    }
}
