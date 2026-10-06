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
        TestStore store; FakeWallet wallet; EconomyTuning tuning; LevelProgressService svc;

        [SetUp]
        public void SetUp()
        {
            store = new TestStore(); wallet = new FakeWallet(); tuning = new EconomyTuning();
            svc = new LevelProgressService(store, wallet, tuning);
        }

        long Coins => wallet.Balance(CardSlotResources.Coin);

        [Test]
        public void Start_coins_are_granted_once()
        {
            Assert.That(svc.EnsureStartCoins(), Is.True);
            Assert.That(svc.EnsureStartCoins(), Is.True);
            Assert.That(Coins, Is.EqualTo(100));
        }

        [Test]
        public void Win_rewards_normal_hard_and_replay_and_advances()
        {
            var w = svc.CompleteLevel(1, false);
            Assert.That((w.Saved, w.Reward, w.NextLevel, w.Replay), Is.EqualTo((true, 20, 2, false)));
            w = svc.CompleteLevel(5, true);
            Assert.That(w.Reward, Is.EqualTo(30));
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(5));
            w = svc.CompleteLevel(3, false);
            Assert.That((w.Replay, w.Reward), Is.EqualTo((true, 5)));
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(5), "a replay never lowers progress");
            Assert.That(Coins, Is.EqualTo(55));
        }

        [Test]
        public void Level_30_first_clear_then_replay_and_never_past_30()
        {
            var first = svc.CompleteLevel(30, true);
            Assert.That((first.Reward, first.NextLevel, first.Final, first.Replay), Is.EqualTo((30, 30, true, false)));
            var again = svc.CompleteLevel(30, true);
            Assert.That((again.Reward, again.Replay), Is.EqualTo((5, true)));
        }

        [Test]
        public void Attempts_count_starts_and_reset_on_win()
        {
            Assert.That(svc.BeginLevel(1).AttemptNo, Is.EqualTo(1));
            Assert.That(svc.BeginLevel(1).AttemptNo, Is.EqualTo(2));
            svc.CompleteLevel(1, false);
            Assert.That(store.Progress.Attempts, Is.EqualTo(0));
        }

        [Test]
        public void Boosters_unlock_at_their_level_with_the_gift_exactly_once()
        {
            Assert.That(svc.BeginLevel(4).Unlocked, Is.EqualTo(BoosterId.Undo));
            Assert.That(svc.BeginLevel(4).Unlocked, Is.Null);
            Assert.That(wallet.Balance(CardSlotResources.BoosterUndo), Is.EqualTo(2));
            Assert.That(svc.BeginLevel(7).Unlocked, Is.EqualTo(BoosterId.AddSlot));
            Assert.That(wallet.Balance(CardSlotResources.BoosterAddSlot), Is.EqualTo(2));
        }

        [Test]
        public void A_player_past_an_unlock_level_still_gets_it_one_booster_per_start()
        {
            Assert.That(svc.BeginLevel(1).Unlocked, Is.Null);
            Assert.That(svc.BeginLevel(12).Unlocked, Is.EqualTo(BoosterId.Undo), "lowest first");
            Assert.That(svc.BeginLevel(12).Unlocked, Is.EqualTo(BoosterId.AddSlot));
            Assert.That(svc.BeginLevel(12).Unlocked, Is.Null);
        }

        [Test]
        public void G18_a_failed_save_on_win_rolls_back_progress_and_coins()
        {
            store.Fail = true;
            var w = svc.CompleteLevel(1, false);
            Assert.That(w.Saved, Is.False);
            Assert.That(w.Reward, Is.EqualTo(0), "the dialog must not show an unsaved reward (G19)");
            Assert.That(store.Progress.CurrentLevel, Is.EqualTo(1));
            Assert.That(store.Progress.HighestCleared, Is.EqualTo(0));
            Assert.That(Coins, Is.EqualTo(0));
        }

        [Test]
        public void G18_a_failed_save_on_unlock_gives_no_gift_and_leaves_it_locked()
        {
            store.Fail = true;
            Assert.That(svc.BeginLevel(4).Unlocked, Is.Null);
            Assert.That(svc.IsUnlocked(BoosterId.Undo), Is.False);
            Assert.That(wallet.Balance(CardSlotResources.BoosterUndo), Is.EqualTo(0));
            store.Fail = false;
            Assert.That(svc.BeginLevel(4).Unlocked, Is.EqualTo(BoosterId.Undo), "retried next time");
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

    public class BoosterAndContinueTests
    {
        FakeWallet wallet; BoosterService svc;

        static LevelData Level()
        {
            var l = new LevelData { Id = "t", Slots = 2, BufferCapacity = 6 };
            l.Targets.AddRange(new[] { new TargetSpec(0, 3), new TargetSpec(0, 3), new TargetSpec(1, 3) });
            l.Stacks.Add(new StackSpec("bl", 0, 0, 150, 206, 0, 1, 1, 1));
            l.Stacks.Add(new StackSpec("r", 220, 0, 150, 206, 0, 0, 0, 0, 0, 0, 0));
            return l;
        }

        [SetUp]
        public void SetUp() { wallet = new FakeWallet(); svc = new BoosterService(wallet, new EconomyTuning()); }

        [Test]
        public void Undo_from_inventory_spends_one_and_undoes()
        {
            wallet.Seed(CardSlotResources.BoosterUndo, 2);
            var b = new BoardModel(Level());
            b.Tap(0);
            Assert.That(svc.TryUse(BoosterId.Undo, PaySource.Inventory, b), Is.True);
            Assert.That(svc.Count(BoosterId.Undo), Is.EqualTo(1));
            Assert.That(b.Buffer, Is.Empty);
        }

        [Test]
        public void Nothing_is_charged_when_the_board_cannot_take_the_booster()
        {
            wallet.Seed(CardSlotResources.BoosterUndo, 2);
            wallet.Seed(CardSlotResources.Coin, 500);
            var b = new BoardModel(Level());
            Assert.That(svc.TryUse(BoosterId.Undo, PaySource.Inventory, b), Is.False, "no tap yet");
            Assert.That(svc.TryUse(BoosterId.Undo, PaySource.Coins, b), Is.False);
            Assert.That(svc.Count(BoosterId.Undo), Is.EqualTo(2));
            Assert.That(wallet.Balance(CardSlotResources.Coin), Is.EqualTo(500));
        }

        [Test]
        public void Buying_with_coins_charges_the_price_or_nothing()
        {
            var b = new BoardModel(Level());
            wallet.Seed(CardSlotResources.Coin, 99);
            Assert.That(svc.CanBuy(BoosterId.AddSlot), Is.False);
            Assert.That(svc.TryUse(BoosterId.AddSlot, PaySource.Coins, b), Is.False);
            Assert.That((wallet.Balance(CardSlotResources.Coin), b.BufferCapacity), Is.EqualTo((99L, 6)));
            wallet.Seed(CardSlotResources.Coin, 100);
            Assert.That(svc.TryUse(BoosterId.AddSlot, PaySource.Coins, b), Is.True);
            Assert.That((wallet.Balance(CardSlotResources.Coin), b.BufferCapacity), Is.EqualTo((0L, 10)));
        }

        [Test]
        public void Ad_path_charges_nothing()
        {
            var b = new BoardModel(Level());
            Assert.That(svc.TryUse(BoosterId.AddSlot, PaySource.Ad, b), Is.True);
            Assert.That(b.BufferCapacity, Is.EqualTo(10));
        }

        [Test]
        public void Continue_with_coins_or_ad_only_after_an_overflow()
        {
            var b = new BoardModel(Level(), new EconomyTuning().BoardRules(), 2);
            wallet.Seed(CardSlotResources.Coin, 200);
            Assert.That(svc.TryContinue(PaySource.Coins, b), Is.Empty, "not lost yet");
            b.Tap(0);
            Assert.That(b.Result, Is.EqualTo(BoardResult.Lost));
            Assert.That(svc.TryContinue(PaySource.Inventory, b), Is.Empty);
            Assert.That(svc.TryContinue(PaySource.Coins, b), Is.Not.Empty);
            Assert.That(wallet.Balance(CardSlotResources.Coin), Is.EqualTo(80));
            Assert.That(b.Result, Is.EqualTo(BoardResult.Playing));
        }

        [Test]
        public void Continue_with_too_few_coins_changes_nothing()
        {
            var b = new BoardModel(Level(), null, 2);
            wallet.Seed(CardSlotResources.Coin, 119);
            b.Tap(0);
            Assert.That(svc.TryContinue(PaySource.Coins, b), Is.Empty);
            Assert.That((wallet.Balance(CardSlotResources.Coin), b.Result), Is.EqualTo((119L, BoardResult.Lost)));
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
