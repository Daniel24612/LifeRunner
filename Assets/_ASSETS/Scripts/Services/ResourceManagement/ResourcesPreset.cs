using UnityEngine.AddressableAssets;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ResourceManagement
{
    [CreateAssetMenu(fileName = "ResourcesPreset", menuName = "Project/ResourcesPreset")]
    public class ResourcesPreset : ScriptableObject
    {
        [SerializeField] private List<AssetReference> _assetsReferences;
        public async UniTask PreloadAssets(IAssetProvider assetProvider, IProgress<float> progress = null)
        {
            var tasks = new List<UniTask>();
            float i = 0;
            foreach (var assetReference in _assetsReferences)
            {
                i++;
                var task = assetProvider.LoadAssetAsync<UnityEngine.Object>(assetReference.AssetGUID)
                    .ContinueWith(_ => progress?.Report(i / _assetsReferences.Count));
                tasks.Add(task);
            }
            progress?.Report(1);
            await UniTask.WhenAll(tasks);
        }
        public void ReleaseAssets(IAssetProvider assetProvider)
        {
            foreach(var assetReference in _assetsReferences)
            {
                assetProvider.ReleaseAsset(assetReference.AssetGUID);
            }
        }
    }
}