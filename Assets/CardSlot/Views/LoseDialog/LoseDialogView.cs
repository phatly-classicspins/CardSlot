using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Lose dialog (mock-ups lose-offer / lose-offer-poor / lose-failed v01): an offer state and a
    /// failed state; which one to show is decided by the controller.</summary>
    [DisallowMultipleComponent]
    public sealed class LoseDialogView : CardDialogView
    {
        public event Action ContinueAdPressed, NoThanksPressed, RetryPressed, HomePressed;

        public void ShowOffer(string title, string adLabel, bool adEnabled, string noThanks, int[] fan)
        {
            Clear();
            var p = Panel(title, 860f, danger: true);
            CardFan(p, 150f, fan);
            Button(p, adLabel, adEnabled ? ButtonStyle.Primary : ButtonStyle.Disabled, 500f, 680f, 164f, () => ContinueAdPressed?.Invoke(), _iconPlay);
            Link(p, noThanks, 710f, () => NoThanksPressed?.Invoke());
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
