using AudioSystem;
using SceneManagement;
using UnityEngine;
using ResourceManagement;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    [Header("Static data/SO")]
    [SerializeField] private InputSettings _inputSettings;
    [SerializeField] private SceneGroupsList _sceneGroupsList;
    [SerializeField] private ResourcesPreset _projectResourcesPreset;
    [Header("Components")]
    [SerializeField] private SceneLoader _sceneLoader;
    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private SoundManager _soundManager;
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.Register<AddresablesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
        builder.Register<SceneGroupManager>(Lifetime.Singleton);

        builder.RegisterInstance(_inputSettings).AsSelf();
        builder.RegisterInstance(_sceneGroupsList).AsSelf();
        builder.RegisterInstance(_projectResourcesPreset).AsSelf();

        builder.RegisterComponent(_loadingScreen);
        builder.RegisterComponent(_sceneLoader);
        builder.RegisterComponent(_soundManager);

        builder.RegisterEntryPoint<GameBootstrapper>();

        Debug.Log("Project Registration Complte");
    }
}