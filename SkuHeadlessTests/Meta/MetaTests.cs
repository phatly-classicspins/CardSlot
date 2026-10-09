using System;
using System.Linq;
using ClassicSpins.PrototypeFramework.Application;
using Game.Application;
using ClassicSpins.PrototypeFramework.Domain;
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

    /// <summary>In-memory wallet: the framework port without its save / publish side effects.</summary>
    sealed class TestWallet : IWalletService
    {
        readonly System.Collections.Generic.Dictionary<ResourceKey, long> _b = new System.Collections.Generic.Dictionary<ResourceKey, long>();
        public long Balance(ResourceKey res) => _b.TryGetValue(res, out var v) ? v : 0;
        public bool CanAfford(ResourceKey res, long amount) => Balance(res) >= amount;
        public bool TrySpend(ResourceKey res, long amount, GrantSource sink) { if (!CanAfford(res, amount)) return false; _b[res] = Balance(res) - amount; return true; }
        public void Grant(ResourceKey res, long amount, GrantSource source) => _b[res] = Balance(res) + amount;
        public bool TryExchange(ResourceKey spend, long cost, Action<IWalletGrant> grant, GrantSource source) => throw new NotSupportedException();
    }

    public class LevelProgressTests
    {
        TestStore store; TestWallet wallet; EconomyTuning tuning; LevelProgressService svc;

        [SetUp]
        public void SetUp()
        {
            store = new TestStore(); wallet = new TestWallet(); tuning = new EconomyTuning();
            svc = new LevelProgressService(store, wallet, tuning);
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

        // ── coins (CR-012 C1, GDD v2.0 §7) ──────────────────────────────────────────────────────────────
        [Test]
        public void Start_coins_are_granted_once_per_install()
        {
            svc.EnsureStartCoins(); svc.EnsureStartCoins();
            Assert.That((svc.Coins, store.Progress.StartCoinsGranted), Is.EqualTo((100L, true)));
        }

        [Test]
        public void A_win_pays_the_reward_and_claim_x2_pays_it_once_more()
        {
            var w = svc.CompleteLevel(1);
            Assert.That((w.Reward, svc.Coins), Is.EqualTo((20, 20L)));
            Assert.That(svc.GrantWinBonus(w.Reward), Is.True);
            Assert.That(svc.Coins, Is.EqualTo(40L), "×2 = the reward once more");
        }

        [Test]
        public void G18_a_failed_save_takes_the_win_reward_back()
        {
            store.Fail = true;
            var w = svc.CompleteLevel(1);
            Assert.That((w.Saved, w.Reward, svc.Coins), Is.EqualTo((false, 0, 0L)));
        }

        [Test]
        public void Spending_coins_needs_the_balance_and_refunds_a_failed_save()
        {
            svc.EnsureStartCoins();
            Assert.That(svc.TrySpendCoins(150), Is.False, "100 coins cannot pay 150");
            Assert.That(svc.Coins, Is.EqualTo(100L));
            store.Fail = true;
            Assert.That(svc.TrySpendCoins(100), Is.False);
            Assert.That(svc.Coins, Is.EqualTo(100L), "a failed save gives the coins back");
            store.Fail = false;
            Assert.That((svc.TrySpendCoins(100), svc.Coins), Is.EqualTo((true, 0L)));
        }

        [Test]
        public void Revive_price_doubles_with_each_paid_revive_of_the_attempt()
        {
            Assert.That(new[] { 0, 1, 2 }.Select(tuning.RevivePriceAfter), Is.EqualTo(new long[] { 100, 200, 400 }));
        }

        // ── C2 boosters ────────────────────────────────────────────────────────────────────────────────
        [Test]
        public void Boosters_unlock_at_levels_5_8_10_with_a_gift_of_3_once()
        {
            Assert.That(svc.UnlockBoostersFor(4), Is.Empty);
            Assert.That(svc.UnlockBoostersFor(5), Is.EqualTo(new[] { BoosterId.Hand }));
            Assert.That(svc.UnlockBoostersFor(5), Is.Empty, "once");
            Assert.That(svc.Boosters(BoosterId.Hand), Is.EqualTo(3));
            Assert.That(svc.UnlockBoostersFor(12), Is.EqualTo(new[] { BoosterId.Shuffle, BoosterId.Remove }), "a save already past them catches up");
            Assert.That((svc.Boosters(BoosterId.Shuffle), svc.Boosters(BoosterId.Remove)), Is.EqualTo((3, 3)));
        }

        [Test]
        public void G18_a_failed_save_unlocks_nothing_and_gives_nothing()
        {
            store.Fail = true;
            Assert.That(svc.UnlockBoostersFor(5), Is.Empty);
            Assert.That((svc.IsUnlocked(BoosterId.Hand), svc.Boosters(BoosterId.Hand)), Is.EqualTo((false, 0)));
        }

        [Test]
        public void Using_a_booster_needs_one_and_a_failed_save_keeps_it()
        {
            Assert.That(svc.TryUseBooster(BoosterId.Remove), Is.False, "none owned");
            svc.UnlockBoostersFor(10);
            Assert.That(svc.TryUseBooster(BoosterId.Remove), Is.True);
            Assert.That(svc.Boosters(BoosterId.Remove), Is.EqualTo(2));
            store.Fail = true;
            Assert.That(svc.TryUseBooster(BoosterId.Remove), Is.False);
            Assert.That(svc.Boosters(BoosterId.Remove), Is.EqualTo(2));
        }

        [Test]
        public void Buying_a_booster_spends_its_price_and_a_failed_save_refunds_and_takes_it_back()
        {
            wallet.Grant(CardSlotResources.Coin, 100, GrantSource.Reward);
            Assert.That(svc.TryBuyBooster(BoosterId.Remove), Is.False, "120 > 100");
            Assert.That((svc.Coins, svc.Boosters(BoosterId.Remove)), Is.EqualTo((100L, 0)));
            Assert.That(svc.TryBuyBooster(BoosterId.Shuffle), Is.True);
            Assert.That((svc.Coins, svc.Boosters(BoosterId.Shuffle)), Is.EqualTo((20L, 1)));
            wallet.Grant(CardSlotResources.Coin, 200, GrantSource.Reward);
            store.Fail = true;
            Assert.That(svc.TryBuyBooster(BoosterId.Hand), Is.False);
            Assert.That((svc.Coins, svc.Boosters(BoosterId.Hand)), Is.EqualTo((220L, 0)), "G18: coins and booster both restored");
        }

        [Test]
        public void An_ad_gives_one_booster()
        {
            Assert.That(svc.GrantBoosterFromAd(BoosterId.Hand), Is.True);
            Assert.That(svc.Boosters(BoosterId.Hand), Is.EqualTo(1));
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
