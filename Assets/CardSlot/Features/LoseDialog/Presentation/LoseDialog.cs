using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>The lose dialog's labels (localized) and which offers are available (CR-012 stage B). A null
    /// <c>Revive</c> / <c>RvSlot</c> label hides that offer; with neither, the dialog opens straight on the failed state.
    /// Both offers are by rewarded ad until coins return (stage C).</summary>
    public sealed record LoseArgs(
        string Title, string Revive, string ReviveNote, string RvSlot, bool AdReady, string NoThanks,
        string FailedTitle, string Subtitle, string Retry, string Home, int[] Fan) : DialogArgs;

    public enum LoseChoice { ReviveAd, RvSlotAd, Retry, Home }

    /// <summary>features/continue.md (→ revive.md, CR-012) — offer, then failed. "No thanks" switches state inside
    /// the dialog; the caller only hears the final choice.</summary>
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
            _view.RevivePressed += () => { if (_args.AdReady) Close(LoseChoice.ReviveAd); };
            _view.RvSlotPressed += () => { if (_args.AdReady) Close(LoseChoice.RvSlotAd); };
            _view.NoThanksPressed += ShowFailed;
            _view.RetryPressed += () => Close(LoseChoice.Retry);
            _view.HomePressed += () => Close(LoseChoice.Home);
            // Back on the offer = No thanks; on the failed state = Home (features/continue.md)
            _view.CloseRequested += () => { if (_failed) Close(LoseChoice.Home); else ShowFailed(); };
            if (_args.Revive != null || _args.RvSlot != null)
                _view.ShowOffer(_args.Title, _args.Revive, _args.ReviveNote, _args.RvSlot, _args.AdReady, _args.NoThanks, _args.Fan);
            else ShowFailed();
        }

        private void ShowFailed()
        {
            _failed = true;
            _view.ShowFailed(_args.FailedTitle, _args.Subtitle, _args.Retry, _args.Home, _args.Fan);
        }
    }
}
