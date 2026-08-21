using UdonSharp;
using UnityEngine;

namespace StargazingHill
{
    /// <summary>Local-only pickup that returns the debug panel to its dock after the last drop.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldDebugPanelPickup : UdonSharpBehaviour
    {
        // Single tuning point for the quiet period after a drop.
        public const float ReturnDelaySeconds = 10f;

        public Collider pickupCollider;
        // The grips sit on both sides of the panel, so the grab volume is two boxes.
        public Collider secondPickupCollider;
        public Rigidbody pickupRigidbody;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private float _returnAtTime;
        private bool _initialized;
        private bool _held;
        private bool _returnPending;

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialized = true;
        }

        private void OnDisable()
        {
            if (_initialized) ReturnNow();
            else SetGripsEnabled(true);
        }

        public override void OnPickup()
        {
            _held = true;
            _returnPending = false;
            // Keep the first hand as holder while child button colliders stay usable by the other hand.
            SetGripsEnabled(false);
        }

        public override void OnDrop()
        {
            _held = false;
            _returnPending = true;
            SetGripsEnabled(true);
            StopMotion();
            _returnAtTime = Time.time + ReturnDelaySeconds;
            SendCustomEventDelayedSeconds(nameof(ReturnIfReady), ReturnDelaySeconds);
        }

        public void ReturnIfReady()
        {
            if (_held || !_returnPending) return;

            float remaining = _returnAtTime - Time.time;
            if (remaining > 0.05f)
            {
                SendCustomEventDelayedSeconds(nameof(ReturnIfReady), remaining);
                return;
            }

            ReturnNow();
        }

        public void ReturnNow()
        {
            _held = false;
            _returnPending = false;
            StopMotion();
            if (_initialized)
            {
                transform.localPosition = _initialLocalPosition;
                transform.localRotation = _initialLocalRotation;
                transform.localScale = _initialLocalScale;
            }
            SetGripsEnabled(true);
        }

        private void SetGripsEnabled(bool enabled)
        {
            if (pickupCollider != null) pickupCollider.enabled = enabled;
            if (secondPickupCollider != null) secondPickupCollider.enabled = enabled;
        }

        private void StopMotion()
        {
            if (pickupRigidbody == null) return;
            pickupRigidbody.velocity = Vector3.zero;
            pickupRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
