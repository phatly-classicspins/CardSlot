using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Lose dialog (mock-ups lose-offer / lose-failed v01; CR-012 stage B): an offer state — Revive and/or
    /// RV Slot, both by rewarded ad — and a failed state. Which offers show and which state opens is decided by the
    /// controller; the view only stacks what it is given.</summary>
    [DisallowMultipleComponent]
    public sealed class LoseDialogView : CardDialogView
    {
        public event Action RevivePressed, RvSlotPressed, NoThanksPressed, RetryPressed, HomePressed;

        /// <summary><paramref name="revive"/> / <paramref name="rvSlot"/> null = that offer is not shown.</summary>
        public void ShowOffer(string title, string revive, string reviveNote, string rvSlot, bool adEnabled, string noThanks, int[] fan)
        {
            Clear();
            const float gap = 24f;
            float h = 440f + (revive != null ? 164f + 60f + gap : 0f) + (rvSlot != null ? 140f + gap : 0f) + 140f;
            var p = Panel(title, h, danger: true);
            CardFan(p, 150f, fan);
            float y = 470f;
            var style = adEnabled ? ButtonStyle.Primary : ButtonStyle.Disabled;
            if (revive != null)
            {
                Button(p, revive, style, y, 680f, 164f, () => RevivePressed?.Invoke(), _iconPlay);
                Line(p, reviveNote, y + 164f, DesignTokens.TypeLabel + 4f, DesignTokens.InkSoft, 56f);
                y += 164f + 60f + gap;
            }
            if (rvSlot != null)
            {
                Button(p, rvSlot, adEnabled ? (revive != null ? ButtonStyle.Secondary : ButtonStyle.Primary) : ButtonStyle.Disabled,
                    y, 560f, 140f, () => RvSlotPressed?.Invoke(), _iconPlay);
                y += 140f + gap;
            }
            Link(p, noThanks, y, () => NoThanksPressed?.Invoke());
        }

        public void ShowFailed(string title, string subtitle, string retry, string home, int[] fan)
        {
            Clear();
            var p = Panel(title, 1000f, danger: true);
            Line(p, subtitle, 150f, DesignTokens.TypeHeading, DesignTokens.InkSoft);
            CardFan(p, 260f, fan, dim: true);
            Button(p, retry, ButtonStyle.Primary, 620f, 560f, 164f, () => RetryPressed?.Invoke());
            Link(p, home, 830f, () => HomePressed?.Invoke());
        }
    }
}
