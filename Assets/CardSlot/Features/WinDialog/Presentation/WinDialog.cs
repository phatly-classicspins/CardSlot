using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>Everything the Win dialog shows, already localized (rule #4). Progress and the plain reward were saved
    /// before this dialog was requested (G19); <c>ClaimEnabled</c> is whether the ×2 rewarded ad is ready.</summary>
    public sealed record WinArgs(string Title, string Subtitle, string Reward, string Claim, bool ClaimEnabled, string Next,
        string Home, string Note, string Coins, int[] Fan) : DialogArgs;

    public enum WinChoice { ClaimDouble, Next, Home }

    /// <summary>features/level-progression.md — the win dialog (CR-012 C1: reward + Claim ×2).</summary>
    public sealed class WinDialog : DialogBase<WinChoice>
    {
        private WinDialogView _view;

        public WinDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (WinArgs)args;
            _view = await LoadViewAsync<WinDialogView>(AssetKeys.WinDialog, ct);
            _view.ClaimPressed += () => { if (a.ClaimEnabled) Close(WinChoice.ClaimDouble); };
            _view.NextPressed += () => Close(WinChoice.Next);
            _view.HomePressed += () => Close(WinChoice.Home);
            _view.CloseRequested += () => Close(WinChoice.Home);
            _view.Show(a.Title, a.Subtitle, a.Reward, a.Claim, a.ClaimEnabled, a.Next, a.Home, a.Note, a.Coins, a.Fan);
        }
    }
}
