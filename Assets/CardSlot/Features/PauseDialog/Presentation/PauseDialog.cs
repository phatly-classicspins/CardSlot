using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    public sealed record PauseArgs(string Title, string Resume, string Restart, string Home) : DialogArgs;

    public enum PauseChoice { Resume, Restart, Home, Settings }

    /// <summary>features/pause-settings.md — Pause. The gear closes Pause with <see cref="PauseChoice.Settings"/>;
    /// the screen swaps in Settings and brings Pause back afterwards (two panels never overlap).</summary>
    public sealed class PauseDialog : DialogBase<PauseChoice>
    {
        private PauseDialogView _view;

        public PauseDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (PauseArgs)args;
            _view = await LoadViewAsync<PauseDialogView>(AssetKeys.PauseDialog, ct);
            _view.ResumePressed += () => Close(PauseChoice.Resume);
            _view.RestartPressed += () => Close(PauseChoice.Restart);
            _view.HomePressed += () => Close(PauseChoice.Home);
            _view.CloseRequested += () => Close(PauseChoice.Resume);
            _view.SettingsPressed += () => Close(PauseChoice.Settings);
            _view.Show(a.Title, a.Resume, a.Restart, a.Home);
        }
    }
}
