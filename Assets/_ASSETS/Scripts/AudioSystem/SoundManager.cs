using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace AudioSystem
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundEmitter emitterPrefab;
        [SerializeField] private bool _collectionCheck = true;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxPoolSize = 100;
        [SerializeField] private int _maxSoundInstances = 30;

        private readonly List<SoundEmitter> _activeEmitters = new();
        private IObjectPool<SoundEmitter> _emitterPool;
        public readonly Queue<SoundEmitter> FrequentSoundEmitters = new();

        private void Awake()
        {
            InitializePool();        
        }
        void InitializePool()
        {
            _emitterPool = new ObjectPool<SoundEmitter>(
                CreateEmitter, 
                OnGetEmitter, 
                OnReleaseEmitter, 
                OnDestroyEmitter,
                _collectionCheck,
                _defaultCapacity,
                _maxPoolSize
                 );
        }

        public SoundEmitter Get()
        {
            return _emitterPool.Get();
        }
        public bool CanPlaySound(SoundData soundData)
        {
            if (!soundData.frequentSound) return true;

            if (FrequentSoundEmitters.Count >= _maxSoundInstances && FrequentSoundEmitters.TryDequeue(out var emitter))
            {
                try
                {
                    emitter.Stop();
                    return true;
                }
                catch
                {
                    Debug.Log("Sound emitter is already released");
                }
                return false;
            }

            return true;
        }
        public void Return(SoundEmitter emitter)
        {
            _emitterPool.Release(emitter);
        }


        private SoundEmitter CreateEmitter()
        {
            var emitter = Instantiate(emitterPrefab);
            emitter.gameObject.SetActive(false);
            return emitter;
        }
        private void OnGetEmitter(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            _activeEmitters.Add(emitter);
        }
        private void OnReleaseEmitter(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            _activeEmitters.Remove(emitter);
        }
        private void OnDestroyEmitter(SoundEmitter emitter)
        {
            Destroy(emitter.gameObject);
        }
    }
}
