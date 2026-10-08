using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// The tunables of <c>docs/design/economy-sheet.md</c> §2. No coins and no boosters (CR-005): what is
    /// left is Continue (rewarded ad only) and interstitial pacing. The composition point may fill these
    /// from <c>IGameConfig</c>, so a balance change is a config change, never a code change (G20).
    /// </summary>
    public sealed class EconomyTuning
    {
        public int ContinueSlotAmount = 6;   // CR-006: one group of cards (was 4 groups before cards were counted singly)
        public int ContinueMaxPerAttempt = 1;
        public int InterstitialFirstLevel = 5;
        public int InterstitialEveryNWins = 2;
        public double InterstitialMinIntervalSeconds = 60;
        public int LevelCount = 30;

        public BoardRules BoardRules() => new BoardRules
        {
            ContinueSlotAmount = ContinueSlotAmount, ContinueMaxPerAttempt = ContinueMaxPerAttempt,
        };
    }
}
