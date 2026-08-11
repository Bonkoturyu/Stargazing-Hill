using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldPlayerSettings : UdonSharpBehaviour
    {
        [Header("Comfortable open-field locomotion")]
        [Min(0f)] public float walkSpeed = 2f;
        [Min(0f)] public float runSpeed = 4f;
        [Min(0f)] public float strafeSpeed = 2f;
        [Min(0f)] public float jumpImpulse = 3.2f;
        [Min(0f)] public float gravityStrength = 1f;

        private void Start()
        {
            ApplyToLocalPlayer();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != null && player.isLocal) Apply(player);
        }

        public void ApplyToLocalPlayer()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null) Apply(player);
        }

        private void Apply(VRCPlayerApi player)
        {
            player.SetWalkSpeed(walkSpeed);
            player.SetRunSpeed(runSpeed);
            player.SetStrafeSpeed(strafeSpeed);
            player.SetJumpImpulse(jumpImpulse);
            player.SetGravityStrength(gravityStrength);
        }
    }
}
