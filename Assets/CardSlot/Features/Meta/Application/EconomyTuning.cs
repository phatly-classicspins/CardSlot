using Game.Domain;

namespace Game.Application
{
    /// <summary>
    /// The tunables of <c>docs/design/economy-sheet.md</c> §2. Defaults equal the approved sheet; the
    /// composition point fills them from <c>IGameConfig</c> (config keys land in Bước 6), so a balance
    /// change is a config change, never a code change (G20).
    /// </summary>
    public sealed class EconomyTuning
    {
        public int StartCoins = 100;
        public int WinReward = 20;
        public int WinRewardHardBonus = 10;
        public int ReplayReward = 5;
        public int UndoPrice = 60;
        public int AddSlotPrice = 100;
        public int UnlockGift = 2;
        public int UndoUnlockLevel = 4;
        public int AddSlotUnlockLevel = 7;
        public int AddSlotAmount = 4;
        public int AddSlotMaxPerAttempt = 1;
        public int ContinuePrice = 120;
        public int ContinueSlotAmount = 4;
        public int ContinueMaxPerAttempt = 1;
        public int InterstitialFirstLevel = 5;
        public int InterstitialEveryNWins = 2;
        public double InterstitialMinIntervalSeconds = 60;
        public int RewardedBoosterAmount = 1;
        public int LevelCount = 30;

        public int PriceOf(BoosterId id) => id == BoosterId.Undo ? UndoPrice : AddSlotPrice;
        public int UnlockLevelOf(BoosterId id) => id == BoosterId.Undo ? UndoUnlockLevel : AddSlotUnlockLevel;

        public BoardRules BoardRules() => new BoardRules
        {
            AddSlotAmount = AddSlotAmount, AddSlotMaxPerAttempt = AddSlotMaxPerAttempt,
            ContinueSlotAmount = ContinueSlotAmount, ContinueMaxPerAttempt = ContinueMaxPerAttempt,
        };
    }
}
