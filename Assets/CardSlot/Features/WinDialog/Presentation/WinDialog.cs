using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>Everything the Win dialog shows, already localized (rule #4). The reward was saved before this
    /// dialog was requested (G19) — the dialog only displays it.</summary>
    public sealed record WinArgs(string Title, string Subtitle, string Reward, string Primary, string Home, string Note, int[] Fan) : DialogArgs;

    public enum WinChoice { Next, Home }

    /// <summary>features/level-progression.md — the win dialog.</summary>
    public sealed class WinDialog : DialogBase<WinChoice>
    {
        private WinDialogView _view;

        public WinDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (WinArgs)args;
            _view = await LoadViewAsync<WinDialogView>(AssetKeys.WinDialog, ct);
            _view.NextPressed += () => Close(WinChoice.Next);
            _view.HomePressed += () => Close(WinChoice.Home);
            _view.CloseRequested += () => Close(WinChoice.Home);
            _view.Show(a.Title, a.Subtitle, a.Reward, a.Primary, a.Home, a.Note, a.Fan);
        }
    }
}
