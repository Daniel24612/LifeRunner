using UnityEngine;
using PrimeTween;
using SceneManagement;
using UnityEngine.AddressableAssets;
using VContainer;
using ResourceManagement;
using TMPro;
using UnityEngine.UI;
using System;
using UnityUtils;
namespace MainMenu
{
    public class LevelChoose : MonoBehaviour
    {
        [Inject] private IAssetProvider _assetProvider;
        [SerializeField] private GameObject _levelChoosePanel;
        [SerializeField] private RectTransform _content;
        [SerializeField] private AssetReference _levelButtonPrefabRef;
        [SerializeField] private Image _levelPreviewImage;
        [SerializeField] private Button _startButton;
        private GameObject _levelButtonPrefab;
        private Sprite _defaultPreviewSprite;

        //[SerializeField] private TweenSettings<Vector2> _listOpenAnim;
        //[SerializeField] private TweenSettings<Vector2> _listCloseAnim;

        private string _currentChoosedLevelName;
        public event Action<string> LevelChoosed = delegate { };

        public void Initialize(SceneGroup[] levels)
        {
            _levelButtonPrefab = _assetProvider.GetAsset<GameObject>(_levelButtonPrefabRef.RuntimeKey.ToString());
            _defaultPreviewSprite = _levelPreviewImage.sprite;
            _startButton.onClick.AddListener(StartLevel);
            _startButton.gameObject.SetActive(false);
            foreach (var level in levels)
            {
                var button = Instantiate(_levelButtonPrefab, _content);
                button.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = level.GroupName;
                button.GetComponent<Button>().onClick.AddListener(() => OnLevelChoosed(level));
            }
        }
        public void StartLevel()
        {
            LevelChoosed?.Invoke(_currentChoosedLevelName);
        }
        public void OpenLevelChoose()
        {
            _levelChoosePanel.SetActive(true);
        }
        public void CloseLevelChoose()
        {
            _levelChoosePanel.SetActive(false);
            _currentChoosedLevelName = null;
             ChangePreviewImage(_defaultPreviewSprite);
            _startButton.interactable = false;
            _startButton.gameObject.SetActive(false);
        }
        private void OnLevelChoosed(SceneGroup level)
        {
            if (level.PreviewImage != null)
                ChangePreviewImage(level.PreviewImage);
            _currentChoosedLevelName = level.GroupName;
            _startButton.interactable = true;
            _startButton.gameObject.SetActive(true);
        }
        private void ChangePreviewImage(Sprite sprite)
        {
            _levelPreviewImage.sprite = sprite;
        }
    }
}