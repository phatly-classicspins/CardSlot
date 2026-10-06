using System;
using ClassicSpins.PrototypeFramework.Application;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Application
{
    /// <summary>What happened when a level was started.</summary>
    public readonly struct LevelStartInfo
    {
        public readonly int Level, AttemptNo;
        /// <summary>The booster unlocked (and gifted) by starting this level, or null.</summary>
        public readonly BoosterId? Unlocked;
        public LevelStartInfo(int level, int attemptNo, BoosterId? unlocked) { Level = level; AttemptNo = attemptNo; Unlocked = unlocked; }
    }

    /// <summary>The outcome of a win, already persisted when returned (G19).</summary>
    public readonly struct WinInfo
    {
        public readonly bool Saved;
        public readonly int Level, Reward, NextLevel;
        public readonly bool Replay, Hard, Final;
        public WinInfo(bool saved, int level, int reward, int nextLevel, bool replay, bool hard, bool final)
        {
            Saved = saved; Level = level; Reward = reward; NextLevel = nextLevel; Replay = replay; Hard = hard; Final = final;
        }
    }

    /// <summary>
    /// Level progression and its rewards (features/level-progression.md, GDD §5, CR-002). Every change
    /// is one transaction: mutate in memory, persist, and on a storage failure restore the previous
    /// state so nothing is shown that was not saved (G18, G19).
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
        public bool IsUnlocked(BoosterId id) => _store.Progress.UnlockedBoosters.Contains(id.ToString());

        /// <summary>Grant the starting coins once per install. Returns false if saving failed (rolled back).</summary>
        public bool EnsureStartCoins()
        {
            if (_store.Progress.StartCoinsGranted) return true;
            return Transact(p => p.StartCoinsGranted = true, CardSlotResources.Coin, _tuning.StartCoins);
        }

        /// <summary>Count the attempt and unlock a booster whose unlock level this is (gift included, once).</summary>
        public LevelStartInfo BeginLevel(int level)
        {
            BoosterId? unlocked = null;
            foreach (BoosterId id in Enum.GetValues(typeof(BoosterId)))
                if (_tuning.UnlockLevelOf(id) == level && !IsUnlocked(id)) unlocked = id;
            bool ok = Transact(p =>
            {
                p.Attempts++;
                if (unlocked.HasValue) p.UnlockedBoosters.Add(unlocked.Value.ToString());
            }, unlocked.HasValue ? CardSlotResources.Of(unlocked.Value) : default, unlocked.HasValue ? _tuning.UnlockGift : 0);
            return new LevelStartInfo(level, _store.Progress.Attempts, ok ? unlocked : null);
        }

        /// <summary>Reward and advance after a win; persisted before returning (the dialog shows saved values).</summary>
        public WinInfo CompleteLevel(int level, bool hard)
        {
            var p = _store.Progress;
            bool replay = level <= p.HighestCleared;
            int reward = replay ? _tuning.ReplayReward : _tuning.WinReward + (hard ? _tuning.WinRewardHardBonus : 0);
            int next = Math.Min(level + 1, _tuning.LevelCount);
            bool ok = Transact(m =>
            {
                m.HighestCleared = Math.Max(m.HighestCleared, level);
                m.CurrentLevel = next;
                m.Attempts = 0;
                m.WinsSinceInterstitial++;
            }, CardSlotResources.Coin, reward);
            return new WinInfo(ok, level, ok ? reward : 0, ok ? next : level, replay, hard, level >= _tuning.LevelCount);
        }

        public bool MarkFtueDone(string step)
        {
            if (_store.Progress.FtueCompleted.Contains(step)) return true;
            return Transact(p => p.FtueCompleted.Add(step), default, 0);
        }

        public bool IsFtueDone(string step) => _store.Progress.FtueCompleted.Contains(step);

        // mutate progress (+ optionally grant a wallet resource) and persist, or restore both
        private bool Transact(Action<ProgressModel> change, ResourceKey grant, long amount)
        {
            var before = _store.Progress.Clone();
            bool granted = false;
            try
            {
                change(_store.Progress);
                if (amount > 0) { _wallet.Grant(grant, amount, GrantSource.Reward); granted = true; }
                _store.Save();
                return true;
            }
            catch (Exception)
            {
                _store.Progress.CopyFrom(before);
                if (granted) _wallet.TrySpend(grant, amount, GrantSource.Compensation);
                return false;
            }
        }
    }
}
