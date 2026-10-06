using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>The lose dialog's labels (localized) and which offers are available. <c>OfferContinue</c> false
    /// opens straight on the failed state (Continue already used this attempt).</summary>
    public sealed record LoseArgs(
        bool OfferContinue, string Title, string ContinueAd, bool AdReady, string ContinueCoins, bool CanAfford, string NoThanks,
        string FailedTitle, string Subtitle, string Retry, string Home, int[] Fan) : DialogArgs;

    public enum LoseChoice { ContinueAd, ContinueCoins, Retry, Home }

    /// <summary>features/continue.md — offer, then failed. "No thanks" switches state inside the dialog; the
    /// caller only hears the final choice.</summary>
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
            _view.ContinueAdPressed += () => { if (_args.AdReady) Close(LoseChoice.ContinueAd); };
            _view.ContinueCoinsPressed += () => { if (_args.CanAfford) Close(LoseChoice.ContinueCoins); };
            _view.NoThanksPressed += ShowFailed;
            _view.RetryPressed += () => Close(LoseChoice.Retry);
            _view.HomePressed += () => Close(LoseChoice.Home);
            // Back on the offer = No thanks; on the failed state = Home (features/continue.md)
            _view.CloseRequested += () => { if (_failed) Close(LoseChoice.Home); else ShowFailed(); };
            if (_args.OfferContinue)
                _view.ShowOffer(_args.Title, _args.ContinueAd, _args.AdReady, _args.ContinueCoins, _args.CanAfford, _args.NoThanks, _args.Fan);
            else ShowFailed();
        }

        private void ShowFailed()
        {
            _failed = true;
            _view.ShowFailed(_args.FailedTitle, _args.Subtitle, _args.Retry, _args.Home, _args.Fan);
        }
    }
}
