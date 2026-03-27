using VContainer;
using VContainer.Unity;
using UnityEngine;
using AudioSystem;
using Player;

public class SceneLifetimeScope : LifetimeScope
{
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private GameplayInputReader inputReader;
    [Header("SettingsSO")]
    [SerializeField] private InputSettings inputSettings; 
    [SerializeField] private PlayerMovementSettings playerMovementSettings;
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.RegisterInstance(inputSettings).AsSelf();
        builder.RegisterInstance(playerMovementSettings).AsSelf();

        builder.Register<GameplayInputReader>(Lifetime.Scoped);

        builder.RegisterComponent<SoundManager>(soundManager);

        Debug.Log("Scene Registration Complte");
    }
}
