using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Application
{
    /// <summary>Wallet resources (D-023): coins and boosters are balances behind <c>IWalletService</c>,
    /// never fields of a save model (AD-5).</summary>
    public static class CardSlotResources
    {
        public static readonly ResourceKey Coin = new ResourceKey("coin");
        public static readonly ResourceKey BoosterUndo = new ResourceKey("booster_undo");
        public static readonly ResourceKey BoosterAddSlot = new ResourceKey("booster_add_slot");

        public static ResourceKey Of(BoosterId id) => id == BoosterId.Undo ? BoosterUndo : BoosterAddSlot;
    }

    public enum BoosterId { Undo, AddSlot }

    /// <summary>Level progress, FTUE and ad pacing (GDD §5, v1.2). Saved through the framework's
    /// <c>IUserData</c> envelope; shape changes need a new migrator (rule #2).</summary>
    public sealed class ProgressModel : IUserModel
    {
        public string Key => "cardslot.progress";
        public int CurrentLevel = 1;
        /// <summary>Highest level won (CR-002); a win at or below it is a replay.</summary>
        public int HighestCleared;
        /// <summary>Starts of the current level since its last win (CR-002, analytics <c>attempt_no</c>).</summary>
        public int Attempts;
        public bool StartCoinsGranted;
        public List<string> UnlockedBoosters = new List<string>();
        public List<string> FtueCompleted = new List<string>();
        public int WinsSinceInterstitial;

        public ProgressModel Clone() => new ProgressModel
        {
            CurrentLevel = CurrentLevel, HighestCleared = HighestCleared, Attempts = Attempts, StartCoinsGranted = StartCoinsGranted,
            UnlockedBoosters = new List<string>(UnlockedBoosters), FtueCompleted = new List<string>(FtueCompleted),
            WinsSinceInterstitial = WinsSinceInterstitial,
        };

        public void CopyFrom(ProgressModel o)
        {
            CurrentLevel = o.CurrentLevel; HighestCleared = o.HighestCleared; Attempts = o.Attempts; StartCoinsGranted = o.StartCoinsGranted;
            UnlockedBoosters = new List<string>(o.UnlockedBoosters); FtueCompleted = new List<string>(o.FtueCompleted);
            WinsSinceInterstitial = o.WinsSinceInterstitial;
        }
    }

    /// <summary>Player settings (features/pause-settings.md).</summary>
    public sealed class SettingsModel : IUserModel
    {
        public string Key => "cardslot.settings";
        public bool Sound = true;
        public bool Music = true;
        public bool Haptics = true;
    }

    /// <summary>
    /// The narrow save port the meta services use. The Infrastructure adapter sits on the framework's
    /// <c>IUserData</c>; tests use an in-memory store that can be told to fail (G18).
    /// </summary>
    public interface IProgressStore
    {
        ProgressModel Progress { get; }
        SettingsModel Settings { get; }
        /// <summary>Persist. May throw on a storage failure; callers roll back in memory.</summary>
        void Save();
    }
}
