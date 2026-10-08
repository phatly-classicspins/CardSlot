using System;

namespace Game.Application
{
    /// <summary>Interstitial pacing (features/ads.md). Pure rule plus the save bookkeeping.</summary>
    public sealed class AdPacing
    {
        private readonly IProgressStore _store;
        private readonly EconomyTuning _tuning;

        public AdPacing(IProgressStore store, EconomyTuning tuning) { _store = store; _tuning = tuning; }

        /// <summary>Show after Next on the Win dialog when all three hold: level ≥ first level, enough wins
        /// since the last interstitial, and enough seconds since the last ad of any kind.</summary>
        public static bool ShouldShow(int levelJustWon, int winsSinceInterstitial, double secondsSinceLastAd, EconomyTuning t) =>
            levelJustWon >= t.InterstitialFirstLevel
            && winsSinceInterstitial >= t.InterstitialEveryNWins
            && secondsSinceLastAd >= t.InterstitialMinIntervalSeconds;

        public bool ShouldShowAfterWin(int levelJustWon, double secondsSinceLastAd) =>
            ShouldShow(levelJustWon, _store.Progress.WinsSinceInterstitial, secondsSinceLastAd, _tuning);

        /// <summary>Record a shown interstitial. Ad pacing is not worth failing a flow over: a failed save
        /// just means the counter resets on the next successful one.</summary>
        public void RecordInterstitialShown()
        {
            _store.Progress.WinsSinceInterstitial = 0;
            try { _store.Save(); } catch (Exception) { /* see summary */ }
        }
    }
}
