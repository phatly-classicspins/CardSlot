using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    public sealed record RestartConfirmArgs(string Title, string Note, string Yes, string No) : DialogArgs;

    /// <summary>features/core-gameplay.md — confirm the ↻ restart (boosters used are not refunded, GDD §5).</summary>
    public sealed class RestartConfirmDialog : DialogBase<bool>
    {
        private RestartConfirmDialogView _view;

        public RestartConfirmDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (RestartConfirmArgs)args;
            _view = await LoadViewAsync<RestartConfirmDialogView>(AssetKeys.RestartConfirmDialog, ct);
            _view.YesPressed += () => Close(true);
            _view.NoPressed += () => Close(false);
            _view.CloseRequested += () => Close(false);
            _view.Show(a.Title, a.Note, a.Yes, a.No);
        }
    }
}
