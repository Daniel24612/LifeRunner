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
        private readonly Dictionary<string, byte> _loadedAssetsUsersCount = new Dictionary<string, byte>();

        // Инициализация оказывается автоматом
        //public async UniTask InitializeAsync()
        //{
        //    await Addressables.InitializeAsync().ToUniTask();
        //}

        /// <summary>
        /// Loads an asset asynchronously to memory and keeps track of it. If the asset is already loaded, it increases the user count and returns the existing asset.
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string assetReference) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetReference, out var handle))
            {
                _loadedAssetsUsersCount[assetReference]++;
                if (handle.IsDone)
                    await handle;

                return (T)handle.Result;
            }

            var newHandle = Addressables.LoadAssetAsync<T>(assetReference);
            _loadedAssets.Add(assetReference, newHandle);
            _loadedAssetsUsersCount.Add(assetReference, 1);
            await newHandle.ToUniTask();
            return (T)newHandle.Result;
        }
        public async UniTask<GameObject> InstantiateAsync(string assetReference, Vector3 pos, Quaternion rot)
        {
            return await Addressables.InstantiateAsync(assetReference, pos, rot).ToUniTask();
        }
        /// <summary>
        /// Before you get an asset, you must load it.
        /// </summary>
        /// <typeparam name="T">Unity object</typeparam>
        public T GetAsset<T>(string assetReference) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetReference, out var handle) && handle.IsDone)
            {
                return (T)handle.Result;
            }
            Debug.LogWarning($"Asset with reference '{assetReference}' is not loaded or still loading.");
            return null;
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
            if (_loadedAssets.TryGetValue(assetReference, out var handle))
            {
                _loadedAssetsUsersCount[assetReference]--;

                // Если больше никто не использует ассет
                if (_loadedAssetsUsersCount[assetReference] <= 0)
                {
                    Addressables.Release(handle);
                    _loadedAssets.Remove(assetReference); // Удаляем из кэша
                    _loadedAssetsUsersCount.Remove(assetReference);
                    Debug.Log($"Asset '{assetReference}' fully released.");
                }
            }
        }
        public void Cleanup()
        {
            foreach (var handle in _loadedAssets.Values)
            {
                Addressables.Release(handle);
            }
            _loadedAssets.Clear();
            _loadedAssetsUsersCount.Clear();
        }
    }

    public interface IAssetProvider
    {
        //UniTask InitializeAsync();
        UniTask<T> LoadAssetAsync<T>(string assetReference) where T : UnityEngine.Object;
        UniTask<GameObject> InstantiateAsync(string assetReference, Vector3 pos, Quaternion rot);
        T GetAsset<T>(string assetReference) where T : UnityEngine.Object;
        void ReleaseInstance(GameObject instance);
        void ReleaseAsset(string assetReference);
        void Cleanup();
    }
}