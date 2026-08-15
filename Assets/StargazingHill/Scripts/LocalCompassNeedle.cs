using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace StargazingHill
{
    /// <summary>Keeps a handheld compass needle aligned with the world's astronomical north.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalCompassNeedle : UdonSharpBehaviour
    {
        public const float ReturnDelaySeconds = 10f;

        public Transform needlePivot;
        public VRCObjectSync objectSync;
        public Rigidbody pickupRigidbody;

        private float _returnAtTime;
        private bool _held;
        private bool _returnPending;

        private void Start()
        {
            UpdateNeedle();
        }

        private void LateUpdate()
        {
            UpdateNeedle();
        }

        public override void OnPickup()
        {
            _held = true;
            _returnPending = false;
        }

        public override void OnDrop()
        {
            _held = false;
            _returnPending = true;
            StopMotion();
            _returnAtTime = Time.time + ReturnDelaySeconds;
            SendCustomEventDelayedSeconds(nameof(ReturnIfReady), ReturnDelaySeconds);
        }

        public void ReturnIfReady()
        {
            if (_held || !_returnPending) return;
            if (!Networking.IsOwner(gameObject))
            {
                _returnPending = false;
                return;
            }

            float remaining = _returnAtTime - Time.time;
            if (remaining > 0.05f)
            {
                SendCustomEventDelayedSeconds(nameof(ReturnIfReady), remaining);
                return;
            }

            _returnPending = false;
            StopMotion();
            if (objectSync != null) objectSync.Respawn();
        }

        private void UpdateNeedle()
        {
            if (needlePivot == null) return;

            // RealSkyController maps astronomical north to world +Z. Convert that fixed
            // direction into the movable compass body's local plane. This is intentionally
            // local-only: the pickup pose may sync, but no needle state is networked.
            Vector3 localNorth = transform.InverseTransformDirection(Vector3.forward);
            localNorth.y = 0f;
            if (localNorth.sqrMagnitude < 0.0001f) return;
            needlePivot.localRotation = Quaternion.LookRotation(localNorth.normalized, Vector3.up);
        }

        private void StopMotion()
        {
            if (pickupRigidbody == null) return;
            pickupRigidbody.velocity = Vector3.zero;
            pickupRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
