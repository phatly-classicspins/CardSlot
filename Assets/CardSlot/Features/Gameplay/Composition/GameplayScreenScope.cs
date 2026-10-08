using VContainer;
using ClassicSpins.PrototypeFramework.Composition;
using Game.Presentation;

namespace Game.Composition
{
    /// <summary>
    /// The Gameplay screen's scope (&lt;X&gt;ScreenScope : SceneLifetimeScope). Sits on the
    /// Gameplay.unity screen scene's single scope GameObject and registers GameplayScreen as the scope's
    /// entry screen. The parent (Root) and the typed GameplayParam are supplied by the scene service when
    /// the scene loads. It lives in Game.Composition — the only SKU tier that names a container
    /// type — while GameplayScreen lives in Game.Presentation. Scaffolded by Scaffold.Sync
    /// (generate-once).
    /// </summary>
    public sealed class GameplayScreenScope : SceneLifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryScreen<GameplayScreen>();
            // dialogs are resolved Transient from the scope that shows them (DialogService); they die with this screen
            builder.Register<WinDialog>(Lifetime.Transient);
            builder.Register<LoseDialog>(Lifetime.Transient);
            builder.Register<PauseDialog>(Lifetime.Transient);
            builder.Register<SettingsDialog>(Lifetime.Transient);
            builder.Register<RestartConfirmDialog>(Lifetime.Transient);
        }
    }
}
