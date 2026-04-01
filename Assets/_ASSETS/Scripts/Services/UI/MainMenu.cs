using Cysharp.Threading.Tasks;
using ResourceManagement;
using SceneManagement;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using VContainer;
namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject _mainButtonsPanel;
        [SerializeField] LevelChoose _levelChoosePanel;
        [SerializeField] Button _playButton;
        [SerializeField] Button _settingsButton;
        [SerializeField] Button _exitButton;

        [SerializeField] AssetReference _levelsListReference;

        [Inject] SceneLoader _sceneLoader;
        [Inject] IAssetProvider _assetProvider;
        private SceneGroupsList _levelsList;
        private void Awake()
        {
            _levelsList = _assetProvider.GetAsset<SceneGroupsList>(_levelsListReference.AssetGUID);
            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _exitButton.onClick.AddListener(OnExitClicked);
            _levelChoosePanel.LevelChoosed += OnLevelSelected;
            _levelChoosePanel.Initialize(_levelsList.GetAllGroups().ToArray());
        }
        private void OnPlayClicked()
        {
            _mainButtonsPanel.SetActive(false);
            _levelChoosePanel.OpenLevelChoose();
        }
        private void OnSettingsClicked()
        {
            Debug.Log("Settings button clicked");
        }
        private void OnExitClicked()
        {
            Debug.Log("Exit button clicked");
        }
        private void OnLevelSelected(string levelSceneName)
        {
            _sceneLoader.LoadGroup(_levelsList.GetGroupByName(levelSceneName)).Forget();
        }
    }
}