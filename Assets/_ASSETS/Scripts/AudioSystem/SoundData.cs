using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    public class SoundData : ScriptableObject
    {
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        public bool loop;
        public bool playOnAwake;
        public bool frequentSound;

        public bool mute;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;

        public int priority = 128;
        public float volume = 1f;
        public float pitch = 1f;
        public float panStereo;
        public float spatialBlend;
        public float reverbZoneMix = 1f;
        public float dopplerLevel = 1f;
        public float spread;

        public float minDistance = 1f;
        public float maxDistance = 500f;

        public bool ignoreListenerVolume;
        public bool ignoreListenerPause;

        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    }
    public class SoundBuilder
    {
        readonly SoundManager _soundManager;
        SoundData _soundData;
        Vector3 _position;
        bool _randomPitch;
        Vector2 _pitchMinMax;

        public SoundBuilder(SoundManager soundManager)
        {
            _soundManager = soundManager;
        }

        public SoundBuilder WithSoundData(SoundData soundData)
        {
            _soundData = soundData;
            return this;
        }
        public SoundBuilder WithPosition(Vector3 position)
        {
            _position = position;
            return this;
        }
        public SoundBuilder WithRandomPitch(float min = -0.05f, float max = 0.05f )
        {
            _randomPitch = true;
            _pitchMinMax = new(min, max);
            return this;
        }

        public void Play()
        {
            if (!_soundManager.CanPlaySound(_soundData)) return;

            var soundEmitter = _soundManager.Get();
            soundEmitter.Initialize(_soundData, _soundManager);
            soundEmitter.transform.position = _position;

            if (_randomPitch)
                if (_pitchMinMax.magnitude > 0)
                    soundEmitter.WithRandomPitch(_pitchMinMax.x, _pitchMinMax.y);
                else
                    soundEmitter.WithRandomPitch();

            if (_soundData.frequentSound)
            {
                _soundManager.FrequentSoundEmitters.Enqueue(soundEmitter);
            }
            soundEmitter.Play();
        }
    }
}