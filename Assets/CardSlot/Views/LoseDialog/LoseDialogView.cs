using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Lose dialog (mock-ups lose-offer-coins-v02 / lose-offer-coins-poor-v02 / lose-failed v01; CR-012 B, C1): an offer
    /// state — Revive by coins or by a rewarded ad side by side, RV Slot by ad — and a failed state. Which offers show,
    /// whether each is enabled and which state opens are the controller's; the view only stacks what it is given.</summary>
    [DisallowMultipleComponent]
    public sealed class LoseDialogView : CardDialogView
    {
        public event Action ReviveCoinsPressed, ReviveAdPressed, RvSlotPressed, NoThanksPressed, RetryPressed, HomePressed;
        /// <summary>CR-012 C2: a booster tile of the bar drawn above the dim (index in bar order).</summary>
        public event Action<int> BoosterPressed;

        /// <summary><paramref name="revive"/> / <paramref name="rvSlot"/> null = that offer is not shown. <paramref name="boosters"/> non-null
        /// (mock-up lose-offer-boosters-v02): the booster bar stays usable above the dim, the card fan gives way to a hint line.</summary>
        public void ShowOffer(string title, string coins, string revive, string revivePrice, bool reviveAffordable, string reviveNote,
            string rvSlot, bool adEnabled, string noThanks, int[] fan, BoosterTileVisual[] boosters = null, string useBooster = null)
        {
            Clear();
            const float gap = 24f;
            bool bar = boosters != null;
            float top = bar ? 140f : 470f;
            float h = top - 30f + (revive != null ? 164f + 56f + gap : 0f) + (rvSlot != null ? 140f + gap : 0f) + 140f + (bar ? 100f : 0f);
            var p = Panel(title, h, danger: true);
            CoinPill(coins);
            if (!bar) CardFan(p, 150f, fan);
            float y = top;
            if (revive != null)
            {
                PriceButton(p, revive, revivePrice, reviveAffordable ? ButtonStyle.Primary : ButtonStyle.Disabled, y, 30f, 420f, 164f, 50f,
                    () => ReviveCoinsPressed?.Invoke());
                Button(p, revive, adEnabled ? ButtonStyle.Secondary : ButtonStyle.Disabled, y, 400f, 164f, () => ReviveAdPressed?.Invoke(),
                    _iconPlay, PanelWidth - 30f - 400f, 50f);
                Line(p, reviveNote, y + 164f, DesignTokens.TypeLabel + 4f, DesignTokens.InkSoft, 56f);
                y += 164f + 56f + gap;
            }
            if (rvSlot != null)
            {
                Button(p, rvSlot, adEnabled ? ButtonStyle.Secondary : ButtonStyle.Disabled, y, 560f, 140f, () => RvSlotPressed?.Invoke(), _iconPlay);
                y += 140f + gap;
            }
            Link(p, noThanks, y, () => NoThanksPressed?.Invoke());
            if (!bar) return;
            Line(p, useBooster, y + 110f, DesignTokens.TypeBody - 4f, DesignTokens.InkSoft, 60f);
            Boosters(boosters, i => BoosterPressed?.Invoke(i));
        }

        public void ShowFailed(string title, string subtitle, string retry, string home, string coins, int[] fan)
        {
            Clear();
            var p = Panel(title, 1000f, danger: true);
            CoinPill(coins);
            Line(p, subtitle, 150f, DesignTokens.TypeHeading, DesignTokens.InkSoft);
            CardFan(p, 260f, fan, dim: true);
            Button(p, retry, ButtonStyle.Primary, 620f, 560f, 164f, () => RetryPressed?.Invoke());
            Link(p, home, 830f, () => HomePressed?.Invoke());
        }
    }
}
