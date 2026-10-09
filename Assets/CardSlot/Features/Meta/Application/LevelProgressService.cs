using System;
using ClassicSpins.PrototypeFramework.Application;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Application
{
    /// <summary>What happened when a level was started.</summary>
    public readonly struct LevelStartInfo
    {
        public readonly int Level, AttemptNo;
        public LevelStartInfo(int level, int attemptNo) { Level = level; AttemptNo = attemptNo; }
    }

    /// <summary>The outcome of a win, already persisted when returned (G19). <see cref="Reward"/> coins are
    /// already in the wallet.</summary>
    public readonly struct WinInfo
    {
        public readonly bool Saved;
        public readonly int Level, NextLevel, Reward;
        public readonly bool Replay, Final;
        public WinInfo(bool saved, int level, int nextLevel, int reward, bool replay, bool final)
        {
            Saved = saved; Level = level; NextLevel = nextLevel; Reward = reward; Replay = replay; Final = final;
        }
    }

    /// <summary>
    /// Level progression and coins (features/level-progression.md, GDD v2.0 §5, §7; CR-012 C1). Every change is
    /// one transaction: mutate in memory (and the wallet), persist, and on a storage failure restore the previous
    /// state so nothing is shown that was not saved (G18, G19). Coins move only through <see cref="IWalletService"/>.
    /// </summary>
    public sealed class LevelProgressService
    {
        private readonly IProgressStore _store;
        private readonly IWalletService _wallet;
        private readonly EconomyTuning _tuning;

        public LevelProgressService(IProgressStore store, IWalletService wallet, EconomyTuning tuning)
        {
            _store = store; _wallet = wallet; _tuning = tuning;
        }

        public int CurrentLevel => _store.Progress.CurrentLevel;
        public int HighestCleared => _store.Progress.HighestCleared;
        public long Coins => _wallet.Balance(CardSlotResources.Coin);

        /// <summary>Grant the starting coins once per install. False if saving failed (rolled back).</summary>
        public bool EnsureStartCoins()
        {
            if (_store.Progress.StartCoinsGranted) return true;
            return Transact(p => p.StartCoinsGranted = true, _tuning.StartCoins);
        }

        /// <summary>Count the attempt.</summary>
        public LevelStartInfo BeginLevel(int level)
        {
            Transact(p => p.Attempts++, 0);
            return new LevelStartInfo(level, _store.Progress.Attempts);
        }

        /// <summary>Advance and pay the win reward; persisted before returning (the dialog shows saved values).</summary>
        public WinInfo CompleteLevel(int level)
        {
            bool replay = level <= _store.Progress.HighestCleared;
            int next = Math.Min(level + 1, _tuning.LevelCount);
            int reward = _tuning.WinReward;
            bool ok = Transact(m =>
            {
                m.HighestCleared = Math.Max(m.HighestCleared, level);
                m.CurrentLevel = next;
                m.Attempts = 0;
                m.WinsSinceInterstitial++;
            }, reward);
            return new WinInfo(ok, level, ok ? next : level, ok ? reward : 0, replay, level >= _tuning.LevelCount);
        }

        /// <summary>"Claim ×N" after a completed rewarded ad: the extra (N − 1) × reward. False if saving failed.</summary>
        public bool GrantWinBonus(int reward) =>
            Transact(_ => { }, (long)reward * Math.Max(0, _tuning.WinAdMultiplier - 1));

        /// <summary>Spend coins (a revive). False — and nothing spent — if the balance is short or saving failed.</summary>
        public bool TrySpendCoins(long amount)
        {
            if (amount <= 0) return true;
            if (!_wallet.TrySpend(CardSlotResources.Coin, amount, GrantSource.Reward)) return false;
            try { _store.Save(); return true; }
            catch (Exception)
            {
                _wallet.Grant(CardSlotResources.Coin, amount, GrantSource.Compensation);
                return false;
            }
        }

        // ── boosters (CR-012 C2, features/boosters.md v2) ─────────────────────────────────────────────

        public int Boosters(BoosterId id) => Count(_store.Progress, id);
        public bool IsUnlocked(BoosterId id) => _store.Progress.BoostersUnlocked.Contains(Name(id));

        /// <summary>Unlock every booster whose level <paramref name="level"/> has reached and give its gift, once each
        /// (a save from before C2 past level 5 gets them at its next start). Returns the ones unlocked now, in bar
        /// order; empty if none, or if saving failed (rolled back, they come again next start).</summary>
        public BoosterId[] UnlockBoostersFor(int level)
        {
            var now = new System.Collections.Generic.List<BoosterId>();
            foreach (BoosterId id in Enum.GetValues(typeof(BoosterId)))
                if (level >= _tuning.UnlockLevel(id) && !IsUnlocked(id)) now.Add(id);
            if (now.Count == 0) return Array.Empty<BoosterId>();
            bool ok = Transact(p =>
            {
                foreach (var id in now) { p.BoostersUnlocked.Add(Name(id)); Add(p, id, _tuning.BoosterUnlockGift); }
            }, 0);
            return ok ? now.ToArray() : Array.Empty<BoosterId>();
        }

        /// <summary>Use one: false — nothing taken — if there is none or saving failed.</summary>
        public bool TryUseBooster(BoosterId id)
        {
            if (Boosters(id) <= 0) return false;
            return Transact(p => Add(p, id, -1), 0);
        }

        /// <summary>Buy <see cref="EconomyTuning.BoosterBuyAmount"/> for its coin price, as one transaction: false — no coins
        /// spent, no booster given — if the balance is short or saving failed (G18).</summary>
        public bool TryBuyBooster(BoosterId id)
        {
            long price = _tuning.Price(id);
            if (!_wallet.TrySpend(CardSlotResources.Coin, price, GrantSource.Reward)) return false;
            var before = _store.Progress.Clone();
            try
            {
                Add(_store.Progress, id, _tuning.BoosterBuyAmount);
                _store.Save();
                return true;
            }
            catch (Exception)
            {
                _store.Progress.CopyFrom(before);
                _wallet.Grant(CardSlotResources.Coin, price, GrantSource.Compensation);
                return false;
            }
        }

        /// <summary>"▶ Free" after a completed rewarded ad (G19). False if saving failed.</summary>
        public bool GrantBoosterFromAd(BoosterId id) => Transact(p => Add(p, id, _tuning.BoosterAdAmount), 0);

        private static string Name(BoosterId id) => id == BoosterId.Hand ? "booster_hand" : id == BoosterId.Shuffle ? "booster_shuffle" : "booster_remove";

        private static int Count(ProgressModel p, BoosterId id) =>
            id == BoosterId.Hand ? p.BoosterHand : id == BoosterId.Shuffle ? p.BoosterShuffle : p.BoosterRemove;

        private static void Add(ProgressModel p, BoosterId id, int n)
        {
            if (id == BoosterId.Hand) p.BoosterHand = Math.Max(0, p.BoosterHand + n);
            else if (id == BoosterId.Shuffle) p.BoosterShuffle = Math.Max(0, p.BoosterShuffle + n);
            else p.BoosterRemove = Math.Max(0, p.BoosterRemove + n);
        }

        public bool MarkFtueDone(string step)
        {
            if (_store.Progress.FtueCompleted.Contains(step)) return true;
            return Transact(p => p.FtueCompleted.Add(step), 0);
        }

        public bool IsFtueDone(string step) => _store.Progress.FtueCompleted.Contains(step);

        // mutate progress (+ optionally grant coins) and persist, or restore both
        private bool Transact(Action<ProgressModel> change, long coins)
        {
            var before = _store.Progress.Clone();
            bool granted = false;
            try
            {
                change(_store.Progress);
                if (coins > 0) { _wallet.Grant(CardSlotResources.Coin, coins, GrantSource.Reward); granted = true; }
                _store.Save();
                return true;
            }
            catch (Exception)
            {
                _store.Progress.CopyFrom(before);
                if (granted) _wallet.TrySpend(CardSlotResources.Coin, coins, GrantSource.Compensation);
                return false;
            }
        }
    }
}
