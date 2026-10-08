using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// The tunables of <c>docs/design/economy-sheet.md</c> §2 and GDD v2.0 §7. CR-012 stage B: Revive (free Removes)
    /// and RV Slot (more holding cells), both by rewarded ad until coins come back (stage C), plus interstitial
    /// pacing. The composition point may fill these from <c>IGameConfig</c>, so a balance change is a config
    /// change, never a code change (G20).
    /// </summary>
    public sealed class EconomyTuning
    {
        public int ReviveRemoves = 2;           // R-22: poles a revive clears for free
        public int ReviveMaxPerAttempt = 3;     // GDD v2.0 §7 revive.max_per_level [XÁC NHẬN]
        public int RvSlotAmount = 8;            // R-23
        public int RvSlotMaxPerAttempt = 2;
        public int InterstitialFirstLevel = 5;
        public int InterstitialEveryNWins = 2;
        public double InterstitialMinIntervalSeconds = 60;
        public int LevelCount = 30;

        public BoardRules BoardRules() => new BoardRules
        {
            ReviveRemoves = ReviveRemoves, ReviveMaxPerAttempt = ReviveMaxPerAttempt,
            RvSlotAmount = RvSlotAmount, RvSlotMaxPerAttempt = RvSlotMaxPerAttempt,
        };
    }
}
