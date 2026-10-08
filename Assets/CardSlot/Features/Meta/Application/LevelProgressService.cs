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
