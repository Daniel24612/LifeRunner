using VContainer;
using VContainer.Unity;
using UnityEngine;

public class SceneLifetimeScope : LifetimeScope
{
    [Header("SettingsSO")]
    [SerializeField] private PlayerMovementSettings playerMovementSettings;
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.RegisterInstance(playerMovementSettings).AsSelf();

        builder.Register<GameplayInputReader>(Lifetime.Scoped);

        Debug.Log("Scene Registration Complte");
    }
}
