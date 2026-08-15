using UdonSharp;
using UnityEngine;

namespace StargazingHill
{
    /// <summary>Local settings-board pickup that returns to its dock ten seconds after drop.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldSettingsBoardPickup : UdonSharpBehaviour
    {
        public const float ReturnDelaySeconds = 10f;
        public Collider pickupCollider;
        public Rigidbody pickupRigidbody;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private float _returnAtTime;
        private bool _held;
        private bool _returnPending;

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
        }

        public override void OnPickup()
        {
            _held = true;
            _returnPending = false;
            if (pickupCollider != null) pickupCollider.enabled = false;
        }

        public override void OnDrop()
        {
            _held = false;
            _returnPending = true;
            if (pickupCollider != null) pickupCollider.enabled = true;
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
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
            if (pickupCollider != null) pickupCollider.enabled = true;
        }

        private void StopMotion()
        {
            if (pickupRigidbody == null) return;
            pickupRigidbody.velocity = Vector3.zero;
            pickupRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
