using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ResourceManagement
{
    public class AddresablesAssetProvider : IAssetProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();

        public async UniTask InitializeAsync()
        {
            await Addressables.InitializeAsync().ToUniTask();
        }
        public async UniTask<T> LoadAssetAsync<T>(string assetReference) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetReference, out var handle))
            {
                if (handle.IsDone)
                    return handle.Convert<T>().Result;

                return await handle.Convert<T>().ToUniTask();
            }

            var newHandle = Addressables.LoadAssetAsync<T>(assetReference);
            _loadedAssets.Add(assetReference, newHandle);
            return await newHandle.ToUniTask();
        }
        public async UniTask<GameObject> InstantiateAsync(string assetReference, Vector3 pos, Quaternion rot)
        {
            return await Addressables.InstantiateAsync(assetReference, pos, rot).ToUniTask();
        }
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("Attempted to release a null instance.");
                return;
            }
            Addressables.ReleaseInstance(instance);
        }
        public void ReleaseAsset(string assetReference)
        {
            if( _loadedAssets.ContainsKey(assetReference))            
            {
                Addressables.Release(_loadedAssets[assetReference]);
                _loadedAssets.Remove(assetReference);
            }
        }
        public void Cleanup()
        {
            foreach (var handle in _loadedAssets.Values)
            {
                Addressables.Release(handle);
            }
            _loadedAssets.Clear();
        }
    }

    public interface IAssetProvider
    {
        UniTask InitializeAsync();
        UniTask<T> LoadAssetAsync<T>(string assetReference) where T : UnityEngine.Object;
        UniTask<GameObject> InstantiateAsync(string assetReference, Vector3 pos, Quaternion rot);
        void ReleaseInstance(GameObject instance);
        void ReleaseAsset(string assetReference);
        void Cleanup();
    }
}