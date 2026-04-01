using Cysharp.Threading.Tasks;
using VContainer.Unity;
using ResourceManagement;
using UnityEngine;
using SceneManagement;

public class GameBootstrapper : IStartable
{
    private readonly IAssetProvider _assetProvider;
    private readonly SceneLoader _sceneLoader;
    private readonly ResourcesPreset _globalAssets;
    private readonly SceneGroupsList _sceneGroupsList;
    public GameBootstrapper(
        IAssetProvider assetProvider,
        SceneLoader sceneLoader,
        ResourcesPreset globalAssets,
        SceneGroupsList sceneGroupsList)
    {
        _assetProvider = assetProvider;
        _sceneLoader = sceneLoader;
        _globalAssets = globalAssets;
        _sceneGroupsList = sceneGroupsList;
    }
    public void Start()
    {
        StartAsync().Forget();
    }
    public async UniTaskVoid StartAsync()
    {
        Debug.Log("--- Game boot ---");

        if (_globalAssets != null)
        {
            await _globalAssets.PreloadAssets(_assetProvider);
        }

        // await SaveLoadService.LoadAsync();

        Debug.Log($"--- Initializing complete. Go ot scene num: 0 ---");

        // Предположим, у тебя есть ResourcesPreset для меню или ты просто грузишь по имени
        await _sceneLoader.LoadGroup(_sceneGroupsList.GetGroupByIndex(0));
    }
}