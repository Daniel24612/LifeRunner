using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [Inject] private SceneGroupManager _groupManager;
        [SerializeField] private LoadingScreen _loadingUI;
        [SerializeField] private SceneGroupsList _sceneGroupsList;
        [SerializeField] private string _startSceneGroupName;

        public void Start()
        {
            if (_sceneGroupsList.TryGetGroupByName(_startSceneGroupName, out var sceneGroup))
                LoadGroup(sceneGroup).Forget();
        }   
        public async UniTask LoadGroup(SceneGroup group)
        {
            if (group == null || group.Scenes.Count == 0) return;

            await _loadingUI.SetActive(true);

            await _groupManager.LoadScenes(group, _loadingUI);

            // Маленькая задержка, чтобы игрок успел осознать 100% на баре (по желанию)
            await UniTask.Delay(500);

            await _loadingUI.SetActive(false);
        }

        public async void GoToMainMenu(SceneGroup menuGroup)
        {
            await LoadGroup(menuGroup);
        }
    }
}