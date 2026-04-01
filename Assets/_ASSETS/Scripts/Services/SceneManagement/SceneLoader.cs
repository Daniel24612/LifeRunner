using Cysharp.Threading.Tasks;
using ResourceManagement;
using UnityEngine;
using VContainer;

namespace SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [Inject] private SceneGroupManager _groupManager;
        [Inject] private IAssetProvider _assetProvider;
        [SerializeField] private LoadingScreen _loadingUI;
        private SceneGroup _lastGroup;
        public async UniTask LoadGroup(SceneGroup group)
        {
            if (group == null || group.Scenes.Count == 0) return;

            _lastGroup?.ResourcesPreset?.ReleaseAssets(_assetProvider);
            _lastGroup = group;

            await _loadingUI.SetActive(true);
            if (group.ResourcesPreset != null)
            {
                _loadingUI.SetText("Loading Resources...");
                await group.ResourcesPreset.PreloadAssets(_assetProvider, _loadingUI);
            }

            _loadingUI.SetText("Loading Scene...");
            await _groupManager.LoadScenes(group, _loadingUI);

            // Маленькая задержка, чтобы игрок успел осознать 100% на баре (по желанию)
            _loadingUI.SetText("Finalizing...");
            await UniTask.Delay(1000);

            await _loadingUI.SetActive(false);
        }
    }
}