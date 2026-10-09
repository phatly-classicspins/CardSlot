using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <summary>Everything the buy dialog shows, already localized (rule #4). <c>Booster</c> is the icon index (bar order);
    /// <c>Affordable</c> enables the coin button; <c>AdReady</c> enables "▶ Free" (the rewarded ad).</summary>
    public sealed record BoosterBuyArgs(string Title, string Name, string Tip, int Booster, string Price, bool Affordable,
        string NotEnough, string Ad, bool AdReady, string Coins) : DialogArgs;

    public enum BoosterBuyChoice { Coins, Ad, Close }

    /// <summary>features/boosters.md v2 — out of a booster: buy one with coins or watch an ad for one (mock-ups booster-buy-v02,
    /// booster-buy-poor-v02). The caller spends and grants; this only reports the choice.</summary>
    public sealed class BoosterBuyDialog : DialogBase<BoosterBuyChoice>
    {
        private BoosterBuyDialogView _view;

        public BoosterBuyDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (BoosterBuyArgs)args;
            _view = await LoadViewAsync<BoosterBuyDialogView>(AssetKeys.BoosterBuyDialog, ct);
            _view.CoinsPressed += () => { if (a.Affordable) Close(BoosterBuyChoice.Coins); };
            _view.AdPressed += () => { if (a.AdReady) Close(BoosterBuyChoice.Ad); };
            _view.CloseRequested += OnCloseRequested;
            _view.Show(a.Title, a.Name, a.Tip, a.Booster, a.Price, a.Affordable, a.NotEnough, a.Ad, a.AdReady, a.Coins);
        }

        private void OnCloseRequested() => Close(BoosterBuyChoice.Close);

        public override void OnDispose()
        {
            if (_view != null) _view.CloseRequested -= OnCloseRequested;
        }
    }
}
