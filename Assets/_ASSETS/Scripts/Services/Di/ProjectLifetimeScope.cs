using AudioSystem;
using SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    [Header("Static data/SO")]
    [SerializeField] private InputSettings _inputSettings;
    [Header("Components")]
    [SerializeField] private SceneLoader _sceneLoader;
    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private SoundManager _soundManager;
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.Register<SceneGroupManager>(Lifetime.Singleton);

        builder.RegisterInstance(_inputSettings).AsSelf();

        builder.RegisterComponent(_loadingScreen);
        builder.RegisterComponent(_sceneLoader);
        builder.RegisterComponent(_soundManager);
    }
}