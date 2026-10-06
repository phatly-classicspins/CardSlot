using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Domain;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <param name="IconKind">0 = Undo, 1 = Extra Space.</param>
    public sealed record BoosterUnlockArgs(string Title, string Desc, int IconKind, string Gift, string GotIt) : DialogArgs;

    /// <summary>features/ftue.md — a booster unlocks (its gift was saved before this opens).</summary>
    public sealed class BoosterUnlockDialog : DialogBase<Unit>
    {
        private BoosterUnlockDialogView _view;

        public BoosterUnlockDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (BoosterUnlockArgs)args;
            _view = await LoadViewAsync<BoosterUnlockDialogView>(AssetKeys.BoosterUnlockDialog, ct);
            _view.GotItPressed += () => Close(Unit.Default);
            _view.CloseRequested += () => Close(Unit.Default);
            _view.Show(a.Title, a.Desc, a.IconKind, a.Gift, a.GotIt);
        }
    }
}
