using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// The tunables of <c>docs/design/economy-sheet.md</c> §2 and GDD v2.0 §7. CR-012 stage B: Revive (free Removes)
    /// and RV Slot (more holding cells), coins (C1: start grant, win reward, ×2 by ad, revive price), plus
    /// interstitial pacing. The composition point may fill these from <c>IGameConfig</c>, so a balance change is a config
    /// change, never a code change (G20).
    /// </summary>
    public sealed class EconomyTuning
    {
        public int StartCoins = 100;            // GDD v2.0 §7, D-031
        public int WinReward = 20;
        public int WinAdMultiplier = 2;         // "Claim ×2" on the win dialog
        public int RevivePrice = 100;           // coins for the first revive of an attempt …
        public int RevivePriceMultiplier = 2;   // … ×2 for each further one
        public int ReviveRemoves = 2;           // R-22: poles a revive clears for free
        public int ReviveMaxPerAttempt = 3;     // GDD v2.0 §7 revive.max_per_level [XÁC NHẬN]
        public int RvSlotAmount = 8;            // R-23
        public int RvSlotMaxPerAttempt = 2;
        // C2 boosters (GDD v2.0 §4.1, §7, D-031): unlock level, coin price, how many one purchase gives, unlock gift
        public int HandUnlockLevel = 5, ShuffleUnlockLevel = 8, RemoveUnlockLevel = 10;
        public int HandPrice = 100, ShufflePrice = 80, RemovePrice = 120;
        public int BoosterBuyAmount = 1;
        public int BoosterAdAmount = 1;          // "▶ Free" in the buy dialog
        public int BoosterUnlockGift = 3;
        public int InterstitialFirstLevel = 5;
        public int InterstitialEveryNWins = 2;
        public double InterstitialMinIntervalSeconds = 60;
        public int LevelCount = 30;

        /// <summary>Coins for the revive after <paramref name="paidBefore"/> paid ones this attempt (100, 200, 400 …).</summary>
        public long RevivePriceAfter(int paidBefore)
        {
            long price = RevivePrice;
            for (int i = 0; i < paidBefore; i++) price *= RevivePriceMultiplier;
            return price;
        }

        public int UnlockLevel(BoosterId id) => id == BoosterId.Hand ? HandUnlockLevel : id == BoosterId.Shuffle ? ShuffleUnlockLevel : RemoveUnlockLevel;
        public int Price(BoosterId id) => id == BoosterId.Hand ? HandPrice : id == BoosterId.Shuffle ? ShufflePrice : RemovePrice;

        public BoardRules BoardRules() => new BoardRules
        {
            ReviveRemoves = ReviveRemoves, ReviveMaxPerAttempt = ReviveMaxPerAttempt,
            RvSlotAmount = RvSlotAmount, RvSlotMaxPerAttempt = RvSlotMaxPerAttempt,
        };
    }
}
