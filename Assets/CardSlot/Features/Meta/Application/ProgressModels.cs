using System.Collections.Generic;
using ClassicSpins.PrototypeFramework.Domain;

namespace Game.Application
{
    /// <summary>Wallet resources of the SKU (GDD v2.0 §7): coins are the only soft currency.</summary>
    public static class CardSlotResources
    {
        public static readonly ResourceKey Coin = new ResourceKey("coin");
    }

    /// <summary>The three boosters of GDD v2.0 §2.7 (CR-012 C2). The order is the bar order on screen.</summary>
    public enum BoosterId { Hand = 0, Shuffle = 1, Remove = 2 }

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
        public List<string> FtueCompleted = new List<string>();
        public int WinsSinceInterstitial;
        /// <summary>CR-012 C1: the starting coins were granted (once per install).</summary>
        public bool StartCoinsGranted;
        /// <summary>CR-012 C2: boosters owned (features/boosters.md v2) and the ones unlocked so far (gift given).</summary>
        public int BoosterHand, BoosterShuffle, BoosterRemove;
        public List<string> BoostersUnlocked = new List<string>();

        public ProgressModel Clone() => new ProgressModel
        {
            CurrentLevel = CurrentLevel, HighestCleared = HighestCleared, Attempts = Attempts,
            FtueCompleted = new List<string>(FtueCompleted),
            WinsSinceInterstitial = WinsSinceInterstitial, StartCoinsGranted = StartCoinsGranted,
            BoosterHand = BoosterHand, BoosterShuffle = BoosterShuffle, BoosterRemove = BoosterRemove,
            BoostersUnlocked = new List<string>(BoostersUnlocked),
        };

        public void CopyFrom(ProgressModel o)
        {
            CurrentLevel = o.CurrentLevel; HighestCleared = o.HighestCleared; Attempts = o.Attempts;
            FtueCompleted = new List<string>(o.FtueCompleted);
            WinsSinceInterstitial = o.WinsSinceInterstitial; StartCoinsGranted = o.StartCoinsGranted;
            BoosterHand = o.BoosterHand; BoosterShuffle = o.BoosterShuffle; BoosterRemove = o.BoosterRemove;
            BoostersUnlocked = new List<string>(o.BoostersUnlocked);
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
