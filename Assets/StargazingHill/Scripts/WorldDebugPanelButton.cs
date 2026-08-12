using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    /// <summary>
    /// One button of the local-only VR debug panel. Every instance shares this behaviour and is
    /// distinguished by <see cref="action"/>, so the installer can build the whole panel from primitives
    /// without a script per button.
    ///
    /// Sync mode is None on purpose: the panel is a developer aid, and nothing it does may reach other
    /// players in an instance.
    /// </summary>
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

        private const float HighlightSeconds = 0.25f;

        public int action;
        public int showerIndex;
        public GameObject panelRoot;
        public MeteorController meteorController;
        public RealSkyController skyController;
        public Renderer visualRenderer;

        private bool _highlightColorCaptured;
        private Color _restingColor;

        public override void Interact()
        {
            Highlight();

            if (action == ActionTogglePanel)
            {
                if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf);
                return;
            }

            if (action == ActionForcedShower)
            {
                if (meteorController == null) return;
                meteorController.debugRequestedShowerIndex = showerIndex;
                meteorController.debugRequestedViewForward = GetLocalViewForward();
                meteorController.DebugTriggerSelectedShower();
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

            if (action == ActionSkyPlusHour)
            {
                if (skyController != null) skyController.DebugAdvanceOneHour();
                return;
            }

            if (action == ActionSkyMinusHour)
            {
                // RealSkyController only exposes a forward step, so step the offset directly and reapply.
                if (skyController == null) return;
                skyController.debugTimeOffsetHours -= 1f;
                skyController.ApplyCurrentSkyRotation();
                return;
            }

            if (action == ActionSkyReset)
            {
                if (skyController != null) skyController.DebugResetTimeOffset();
            }
        }

        /// <summary>
        /// Meteors are aimed at where the player is looking, so a forced shower is visible immediately
        /// instead of somewhere behind them.
        /// </summary>
        private Vector3 GetLocalViewForward()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return transform.forward;

            VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Vector3 forward = head.rotation * Vector3.forward;
            return forward.sqrMagnitude < 0.0001f ? transform.forward : forward.normalized;
        }

        private void Highlight()
        {
            if (visualRenderer == null) return;
            if (!_highlightColorCaptured)
            {
                _restingColor = visualRenderer.material.color;
                _highlightColorCaptured = true;
            }

            visualRenderer.material.color = _restingColor + new Color(0.22f, 0.30f, 0.38f, 0f);
            SendCustomEventDelayedSeconds(nameof(ClearHighlight), HighlightSeconds);
        }

        public void ClearHighlight()
        {
            if (visualRenderer == null || !_highlightColorCaptured) return;
            visualRenderer.material.color = _restingColor;
        }
    }
}
