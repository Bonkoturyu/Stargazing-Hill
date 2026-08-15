using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace StargazingHill
{
    /// <summary>Local opt-in switch for the YamaPlayer speaker placed inside the picnic radio.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldRadioSpeaker : UdonSharpBehaviour
    {
        public AudioSource speakerSource;
        public Text stateText;
        public Collider interactionCollider;

        private bool _useAllowed;
        private bool _speakerOn;

        private void Start()
        {
            ApplyState();
        }

        public override void Interact()
        {
            if (!_useAllowed) return;
            _speakerOn = !_speakerOn;
            ApplyState();
        }

        public void SetUseAllowed(bool allowed)
        {
            _useAllowed = allowed;
            if (!allowed) _speakerOn = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (interactionCollider != null) interactionCollider.enabled = _useAllowed;
            if (speakerSource != null) speakerSource.enabled = _useAllowed && _speakerOn;
            if (stateText != null)
            {
                stateText.gameObject.SetActive(_useAllowed);
                stateText.text = !_useAllowed ? "RADIO LOCKED" : (_speakerOn ? "RADIO ON" : "RADIO OFF");
            }
        }
    }
}
