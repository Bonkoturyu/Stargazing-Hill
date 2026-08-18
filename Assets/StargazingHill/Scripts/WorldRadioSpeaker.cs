using UdonSharp;
using UnityEngine;

namespace StargazingHill
{
    /// <summary>Board-controlled local YamaPlayer speaker placed inside the picnic radio.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldRadioSpeaker : UdonSharpBehaviour
    {
        public const float DefaultLocalVolume = 0.85f;

        public AudioSource speakerSource;
        public AudioSource referenceSource;

        // The radio is audible on join; the settings board turns it off again per client.
        private bool _speakerEnabled = true;
        private float _localVolume = DefaultLocalVolume;

        private void Start()
        {
            ApplyGain();
        }

        private void Update()
        {
            ApplyGain();
        }

        public void SetSpeakerEnabled(bool enabled)
        {
            _speakerEnabled = enabled;
            ApplyGain();
        }

        public void SetLocalVolume(float volume)
        {
            _localVolume = Mathf.Clamp01(volume);
            ApplyGain();
        }

        private void ApplyGain()
        {
            if (speakerSource == null) return;
            // Keep the speaker registered and enabled so both AVPro and Unity video handlers
            // continue routing audio to it.  YamaPlayer rewrites the volume of every AudioSource
            // it owns whenever its master value changes, so the local gain is re-applied each
            // frame.  It is deliberately absolute rather than a multiplier on that master: the
            // YamaPlayer master defaults to 0.1, which left this speaker inaudible.  Mute is the
            // one master state the radio still follows, so silencing YamaPlayer silences it too.
            speakerSource.enabled = true;
            speakerSource.volume = _speakerEnabled ? _localVolume : 0f;
            speakerSource.mute = referenceSource != null && referenceSource.mute;
        }
    }
}
