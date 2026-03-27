using UnityEngine;
using UnityEngine.Audio;
using UnityUtils;
namespace AudioSystem
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "Sounds/SoundData")]
    public class SoundData : ScriptableObject
    {
        private Object generator_obj;
            
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        public IAudioGenerator generator
        {
            get
            {
                return (IAudioGenerator)generator_obj;
            }
            set
            {
                generator_obj = (Object)value;
            }
        }
        public bool loop;
        public bool playOnAwake;
        public bool frequentSound;

        public bool mute;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;

        public int priority = 128;
        [Range(0, 1)] public float volume = 1f;
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
        Transform _parent;
        SoundEmitter _lastEmitter;

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
        public SoundBuilder WithParent(Transform parent)
        {
            _parent = parent;
            return this;
        }
        public SoundBuilder ClearData()
        {
            _soundData = null;
            _position = Vector3.zero;
            _randomPitch = false;
            _pitchMinMax = Vector2.zero;
            _parent = null;
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

            _parent?.SetChildren(soundEmitter.transform);

            soundEmitter.Play();

            _lastEmitter = soundEmitter;
        }
        /// <summary>
        /// Can get after play
        /// </summary>
        /// <returns></returns>
        public SoundEmitter GetLastEmitter()
        {
            return _lastEmitter;
        }
    }
}