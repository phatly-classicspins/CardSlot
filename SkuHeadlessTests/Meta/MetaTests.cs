using System;
using ClassicSpins.PrototypeFramework.Application;
using Game.Application;
using Game.Domain;
using NUnit.Framework;

namespace CardSlot.SkuHeadlessTests.Meta
{
    /// <summary>In-memory save port that can be told to fail, to prove G18 rollbacks.</summary>
    sealed class TestStore : IProgressStore
    {
        public ProgressModel Progress { get; } = new ProgressModel();
        public SettingsModel Settings { get; } = new SettingsModel();
        public int Saves;
        public bool Fail;
        public void Save() { if (Fail) throw new System.IO.IOException("disk full"); Saves++; }
    }

    public class LevelProgressTests
    {
        TestStore store; EconomyTuning tuning; LevelProgressService svc;

        [SetUp]
        public void SetUp()
        {
            store = new TestStore(); tuning = new EconomyTuning();
            svc = new LevelProgressService(store, tuning);
        }

        [Test]
        public void Win_advances_and_a_replay_never_lowers_progress()
        {
            var w = svc.CompleteLevel(1);
            Assert.That((w.Saved, w.NextLevel, w.Replay), Is.EqualTo((true, 2, false)));
            svc.CompleteLevel(5);
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(5));
            w = svc.CompleteLevel(3);
            Assert.That(w.Replay, Is.True);
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(5), "a replay never lowers progress");
        }

        [Test]
        public void Level_30_first_clear_then_replay_and_never_past_30()
        {
            var first = svc.CompleteLevel(30);
            Assert.That((first.NextLevel, first.Final, first.Replay), Is.EqualTo((30, true, false)));
            var again = svc.CompleteLevel(30);
            Assert.That(again.Replay, Is.True);
        }

        [Test]
        public void Attempts_count_starts_and_reset_on_win()
        {
            Assert.That(svc.BeginLevel(1).AttemptNo, Is.EqualTo(1));
            Assert.That(svc.BeginLevel(1).AttemptNo, Is.EqualTo(2));
            svc.CompleteLevel(1);
            Assert.That(store.Progress.Attempts, Is.EqualTo(0));
        }

        [Test]
        public void G18_a_failed_save_on_win_rolls_back_progress()
        {
            store.Fail = true;
            var w = svc.CompleteLevel(1);
            Assert.That(w.Saved, Is.False);
            Assert.That(w.NextLevel, Is.EqualTo(1), "the dialog must not show unsaved progress (G19)");
            Assert.That(store.Progress.CurrentLevel, Is.EqualTo(1));
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(0));
        }

        [Test]
        public void Ftue_steps_are_recorded_once()
        {
            Assert.That(svc.IsFtueDone("ftue.l1.step1"), Is.False);
            svc.MarkFtueDone("ftue.l1.step1");
            svc.MarkFtueDone("ftue.l1.step1");
            Assert.That(store.Progress.FtueCompleted, Is.EqualTo(new[] { "ftue.l1.step1" }));
        }
    }

    public class AdPacingTests
    {
        [TestCase(4, 5, 999.0, false, TestName = "before the first level")]
        [TestCase(5, 1, 999.0, false, TestName = "not enough wins")]
        [TestCase(5, 2, 59.0, false, TestName = "too soon after the last ad")]
        [TestCase(5, 2, 60.0, true, TestName = "all three conditions hold")]
        public void Interstitial_rule(int level, int wins, double seconds, bool expected)
        {
            Assert.That(AdPacing.ShouldShow(level, wins, seconds, new EconomyTuning()), Is.EqualTo(expected));
        }

        [Test]
        public void Showing_resets_the_win_counter()
        {
            var store = new TestStore();
            store.Progress.WinsSinceInterstitial = 3;
            new AdPacing(store, new EconomyTuning()).RecordInterstitialShown();
            Assert.That(store.Progress.WinsSinceInterstitial, Is.EqualTo(0));
        }
    }
}
