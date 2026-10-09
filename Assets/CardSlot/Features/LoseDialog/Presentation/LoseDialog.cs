using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>The lose dialog's labels (localized) and which offers are available (CR-012 B, C1). A null <c>Revive</c> /
    /// <c>RvSlot</c> hides that offer; with neither, the dialog opens straight on the failed state. Revive is paid with
    /// <c>RevivePrice</c> coins (enabled when <c>ReviveAffordable</c>) or a rewarded ad; RV Slot by ad. <c>Boosters</c> (C2) draws the
    /// booster bar above the dim on the offer; null hides it.</summary>
    public sealed record LoseArgs(
        string Title, string Coins, string Revive, string RevivePrice, bool ReviveAffordable, string ReviveNote, string RvSlot,
        bool AdReady, string NoThanks, string FailedTitle, string Subtitle, string Retry, string Home, int[] Fan,
        BoosterTileVisual[] Boosters = null, string UseBooster = null) : DialogArgs;

    /// <summary>CR-012 C2: <c>Booster0..2</c> = a booster tile pressed on the offer (bar order: Hand, Shuffle, Remove).</summary>
    public enum LoseChoice { ReviveCoins, ReviveAd, RvSlotAd, Retry, Home, Booster0, Booster1, Booster2 }

    /// <summary>features/revive.md — offer, then failed. "No thanks" switches state inside the dialog; the caller only
    /// hears the final choice.</summary>
    public sealed class LoseDialog : DialogBase<LoseChoice>
    {
        private LoseDialogView _view;
        private LoseArgs _args;
        private bool _failed;

        public LoseDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            _args = (LoseArgs)args;
            _view = await LoadViewAsync<LoseDialogView>(AssetKeys.LoseDialog, ct);
            _view.ReviveCoinsPressed += () => { if (_args.ReviveAffordable) Close(LoseChoice.ReviveCoins); };
            _view.ReviveAdPressed += () => { if (_args.AdReady) Close(LoseChoice.ReviveAd); };
            _view.RvSlotPressed += () => { if (_args.AdReady) Close(LoseChoice.RvSlotAd); };
            _view.NoThanksPressed += ShowFailed;
            _view.BoosterPressed += i => { if (!_failed && i >= 0 && i < 3) Close(LoseChoice.Booster0 + i); };
            _view.RetryPressed += () => Close(LoseChoice.Retry);
            _view.HomePressed += () => Close(LoseChoice.Home);
            // Back on the offer = No thanks; on the failed state = Home (features/revive.md)
            _view.CloseRequested += () => { if (_failed) Close(LoseChoice.Home); else ShowFailed(); };
            if (_args.Revive != null || _args.RvSlot != null || _args.Boosters != null)
                _view.ShowOffer(_args.Title, _args.Coins, _args.Revive, _args.RevivePrice, _args.ReviveAffordable, _args.ReviveNote,
                    _args.RvSlot, _args.AdReady, _args.NoThanks, _args.Fan, _args.Boosters, _args.UseBooster);
            else ShowFailed();
        }

        private void ShowFailed()
        {
            _failed = true;
            _view.ShowFailed(_args.FailedTitle, _args.Subtitle, _args.Retry, _args.Home, _args.Coins, _args.Fan);
        }
    }
}
