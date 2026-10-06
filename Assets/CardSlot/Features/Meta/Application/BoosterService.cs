using System;
using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Application;
using ClassicSpins.PrototypeFramework.Domain;
using Game.Domain;

namespace Game.Application
{
    /// <summary>How a booster or Continue is paid for (features/boosters.md, continue.md).</summary>
    public enum PaySource { Inventory, Coins, Ad }

    /// <summary>
    /// Boosters and Continue: check the board can take it, charge, then apply — in that order, so a
    /// player is never charged for something that does nothing. Balances move only through
    /// <see cref="IWalletService"/>, whose spend is all-or-nothing (G18). The <see cref="PaySource.Ad"/>
    /// path is called only after the rewarded ad reported completion (G19); it charges nothing.
    /// </summary>
    public sealed class BoosterService
    {
        private readonly IWalletService _wallet;
        private readonly EconomyTuning _tuning;

        public BoosterService(IWalletService wallet, EconomyTuning tuning)
        {
            _wallet = wallet; _tuning = tuning;
        }

        public long Count(BoosterId id) => _wallet.Balance(CardSlotResources.Of(id));
        public bool CanBuy(BoosterId id) => _wallet.CanAfford(CardSlotResources.Coin, _tuning.PriceOf(id));
        public bool CanBuyContinue => _wallet.CanAfford(CardSlotResources.Coin, _tuning.ContinuePrice);

        public static bool CanApply(BoosterId id, BoardModel board) => id == BoosterId.Undo ? board.CanUndo : board.CanAddSlot;

        /// <summary>Use one booster on <paramref name="board"/>. False = nothing charged, nothing changed.</summary>
        public bool TryUse(BoosterId id, PaySource source, BoardModel board)
        {
            if (!CanApply(id, board)) return false;
            if (!TryCharge(CardSlotResources.Of(id), _tuning.PriceOf(id), source)) return false;
            bool applied = id == BoosterId.Undo ? board.Undo() : board.AddBufferSpace();
            if (!applied) Refund(CardSlotResources.Of(id), _tuning.PriceOf(id), source); // defensive: CanApply said yes
            return applied;
        }

        /// <summary>Continue after an overflow (R-18). Returns the steps to animate, or an empty list when
        /// it was refused (nothing charged).</summary>
        public IReadOnlyList<BoardStep> TryContinue(PaySource source, BoardModel board)
        {
            if (source == PaySource.Inventory || !board.CanContinue) return Array.Empty<BoardStep>();
            if (!TryCharge(default, _tuning.ContinuePrice, source)) return Array.Empty<BoardStep>();
            return board.Continue();
        }

        private bool TryCharge(ResourceKey item, int price, PaySource source)
        {
            switch (source)
            {
                case PaySource.Inventory: return _wallet.TrySpend(item, 1, GrantSource.Reward);
                case PaySource.Coins: return _wallet.TrySpend(CardSlotResources.Coin, price, GrantSource.Reward);
                case PaySource.Ad: return true;
                default: return false;
            }
        }

        private void Refund(ResourceKey item, int price, PaySource source)
        {
            if (source == PaySource.Inventory) _wallet.Grant(item, 1, GrantSource.Compensation);
            else if (source == PaySource.Coins) _wallet.Grant(CardSlotResources.Coin, price, GrantSource.Compensation);
        }
    }

    /// <summary>Interstitial pacing (features/ads.md). Pure rule plus the save bookkeeping.</summary>
    public sealed class AdPacing
    {
        private readonly IProgressStore _store;
        private readonly EconomyTuning _tuning;

        public AdPacing(IProgressStore store, EconomyTuning tuning) { _store = store; _tuning = tuning; }

        /// <summary>Show after Next on the Win dialog when all three hold: level ≥ first level, enough wins
        /// since the last interstitial, and enough seconds since the last ad of any kind.</summary>
        public static bool ShouldShow(int levelJustWon, int winsSinceInterstitial, double secondsSinceLastAd, EconomyTuning t) =>
            levelJustWon >= t.InterstitialFirstLevel
            && winsSinceInterstitial >= t.InterstitialEveryNWins
            && secondsSinceLastAd >= t.InterstitialMinIntervalSeconds;

        public bool ShouldShowAfterWin(int levelJustWon, double secondsSinceLastAd) =>
            ShouldShow(levelJustWon, _store.Progress.WinsSinceInterstitial, secondsSinceLastAd, _tuning);

        /// <summary>Record a shown interstitial. Ad pacing is not worth failing a flow over: a failed save
        /// just means the counter resets on the next successful one.</summary>
        public void RecordInterstitialShown()
        {
            _store.Progress.WinsSinceInterstitial = 0;
            try { _store.Save(); } catch (Exception) { /* see summary */ }
        }
    }
}
