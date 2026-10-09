using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Domain;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>Everything the unlock popup shows, already localized (rule #4). <c>Booster</c> is the icon index; <c>Boosters</c>
    /// is the bar redrawn above the dim with the new tile active.</summary>
    public sealed record BoosterUnlockArgs(string Title, string Tip, string Gift, string Ok, int Booster, BoosterTileVisual[] Boosters) : DialogArgs;

    /// <summary>features/boosters.md v2 — the first start of level 5 / 8 / 10: the new booster and its gift (mock-up unlock-hand-v02).
    /// The gift is already saved when this shows; "Got it", the new tile or Back closes.</summary>
    public sealed class BoosterUnlockDialog : DialogBase<Unit>
    {
        private BoosterUnlockDialogView _view;

        public BoosterUnlockDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (BoosterUnlockArgs)args;
            _view = await LoadViewAsync<BoosterUnlockDialogView>(AssetKeys.BoosterUnlockDialog, ct);
            _view.CloseRequested += OnCloseRequested;
            _view.Show(a.Title, a.Tip, a.Gift, a.Ok, a.Booster, a.Boosters);
        }

        private void OnCloseRequested() => Close(Unit.Default);

        public override void OnDispose()
        {
            if (_view != null) _view.CloseRequested -= OnCloseRequested;
        }
    }
}
