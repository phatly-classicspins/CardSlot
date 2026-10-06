using System.Threading;
using Cysharp.Threading.Tasks;
using ClassicSpins.PrototypeFramework.Presentation;
using Game.Gen;
using Game.Views;

namespace Game.Presentation
{
    /// <param name="IconKind">0 = Undo, 1 = Extra Space.</param>
    public sealed record BoosterBuyArgs(string Title, string Name, string Desc, int IconKind, string CoinsLabel, bool CanAfford,
        string AdLabel, bool AdReady) : DialogArgs;

    public enum BoosterBuyChoice { Cancel, Coins, Ad }

    /// <summary>features/boosters.md — buy one booster with coins or a rewarded ad. Charging happens at the
    /// caller, through BoosterService, only after this returns (and, for the ad, only on completion — G19).</summary>
    public sealed class BoosterBuyDialog : DialogBase<BoosterBuyChoice>
    {
        private BoosterBuyDialogView _view;

        public BoosterBuyDialog(IAssetService assets) : base(assets) { }

        public override async UniTask OnCreateAsync(DialogArgs args, CancellationToken ct)
        {
            var a = (BoosterBuyArgs)args;
            _view = await LoadViewAsync<BoosterBuyDialogView>(AssetKeys.BoosterBuyDialog, ct);
            _view.CoinsPressed += () => { if (a.CanAfford) Close(BoosterBuyChoice.Coins); };
            _view.AdPressed += () => { if (a.AdReady) Close(BoosterBuyChoice.Ad); };
            _view.ClosePressed += () => Close(BoosterBuyChoice.Cancel);
            _view.CloseRequested += () => Close(BoosterBuyChoice.Cancel);
            _view.Show(a.Title, a.Name, a.Desc, a.IconKind, a.CoinsLabel, a.CanAfford, a.AdLabel, a.AdReady);
        }
    }
}
