using System;

namespace Game.Application
{
    /// <summary>What happened when a level was started.</summary>
    public readonly struct LevelStartInfo
    {
        public readonly int Level, AttemptNo;
        public LevelStartInfo(int level, int attemptNo) { Level = level; AttemptNo = attemptNo; }
    }

    /// <summary>The outcome of a win, already persisted when returned (G19).</summary>
    public readonly struct WinInfo
    {
        public readonly bool Saved;
        public readonly int Level, NextLevel;
        public readonly bool Replay, Final;
        public WinInfo(bool saved, int level, int nextLevel, bool replay, bool final)
        {
            Saved = saved; Level = level; NextLevel = nextLevel; Replay = replay; Final = final;
        }
    }

    /// <summary>
    /// Level progression (features/level-progression.md, GDD §5, CR-002; no coin rewards since CR-005).
    /// Every change is one transaction: mutate in memory, persist, and on a storage failure restore the
    /// previous state so nothing is shown that was not saved (G18, G19).
    /// </summary>
    public sealed class LevelProgressService
    {
        private readonly IProgressStore _store;
        private readonly EconomyTuning _tuning;

        public LevelProgressService(IProgressStore store, EconomyTuning tuning)
        {
            _store = store; _tuning = tuning;
        }

        public int CurrentLevel => _store.Progress.CurrentLevel;
        public int HighestCleared => _store.Progress.HighestCleared;

        /// <summary>Count the attempt.</summary>
        public LevelStartInfo BeginLevel(int level)
        {
            Transact(p => p.Attempts++);
            return new LevelStartInfo(level, _store.Progress.Attempts);
        }

        /// <summary>Advance after a win; persisted before returning (the dialog shows saved values).</summary>
        public WinInfo CompleteLevel(int level)
        {
            bool replay = level <= _store.Progress.HighestCleared;
            int next = Math.Min(level + 1, _tuning.LevelCount);
            bool ok = Transact(m =>
            {
                m.HighestCleared = Math.Max(m.HighestCleared, level);
                m.CurrentLevel = next;
                m.Attempts = 0;
                m.WinsSinceInterstitial++;
            });
            return new WinInfo(ok, level, ok ? next : level, replay, level >= _tuning.LevelCount);
        }

        public bool MarkFtueDone(string step)
        {
            if (_store.Progress.FtueCompleted.Contains(step)) return true;
            return Transact(p => p.FtueCompleted.Add(step));
        }

        public bool IsFtueDone(string step) => _store.Progress.FtueCompleted.Contains(step);

        // mutate progress and persist, or restore it
        private bool Transact(Action<ProgressModel> change)
        {
            var before = _store.Progress.Clone();
            try
            {
                change(_store.Progress);
                _store.Save();
                return true;
            }
            catch (Exception)
            {
                _store.Progress.CopyFrom(before);
                return false;
            }
        }
    }
}
