using UnityEngine;
using Random = UnityEngine.Random;
using UnityUtils;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace AudioSystem
{
    public class SoundEmitter : MonoBehaviour
    {
        public SoundData Data { get; private set; }
        SoundManager _creator;
        AudioSource audioSource;

        private CancellationTokenSource _cts; 


        void Awake()
        {
            audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        public void Initialize(SoundData data, SoundManager creator)
        {
            Data = data;
            _creator = creator;
            audioSource.clip = data.clip;
            audioSource.outputAudioMixerGroup = data.mixerGroup;
            audioSource.loop = data.loop;
            audioSource.playOnAwake = data.playOnAwake;

            audioSource.mute = data.mute;
            audioSource.bypassEffects = data.bypassEffects;
            audioSource.bypassListenerEffects = data.bypassListenerEffects;
            audioSource.bypassReverbZones = data.bypassReverbZones;

            audioSource.priority = data.priority;
            audioSource.volume = data.volume;
            audioSource.pitch = data.pitch;
            audioSource.panStereo = data.panStereo;
            audioSource.spatialBlend = data.spatialBlend;
            audioSource.reverbZoneMix = data.reverbZoneMix;
            audioSource.dopplerLevel = data.dopplerLevel;
            audioSource.spread = data.spread;

            audioSource.minDistance = data.minDistance;
            audioSource.maxDistance = data.maxDistance;

            audioSource.ignoreListenerVolume = data.ignoreListenerVolume;
            audioSource.ignoreListenerPause = data.ignoreListenerPause;

            audioSource.rolloffMode = data.rolloffMode;
        }

        public void Play()
        {
            CleanUpCTS();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            audioSource.Play();

            if (!audioSource.loop)
                WaitForSoundEnd(_cts.Token).Forget();
        }
        public void Stop()
        {
            CleanUpCTS();
            audioSource?.Stop();
            _creator?.Return(this);
        }
        private async UniTaskVoid WaitForSoundEnd(CancellationToken token)
        {
            try
            {
                await UniTask.WaitWhile(() => audioSource.isPlaying, cancellationToken: token);
                _creator?.Return(this);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation if needed
            }
        }
        private void CleanUpCTS()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            audioSource.pitch += Random.Range(min, max); 
        }
        public void Clear()
        {
            Data = null;
        }
        private void OnDestroy()
        {
            CleanUpCTS();
        }
    }
}