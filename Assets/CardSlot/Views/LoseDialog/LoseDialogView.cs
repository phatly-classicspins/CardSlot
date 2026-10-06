using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Lose dialog (mock-ups lose-offer / lose-offer-poor / lose-failed v01): an offer state and a
    /// failed state; which one to show is decided by the controller.</summary>
    [DisallowMultipleComponent]
    public sealed class LoseDialogView : CardDialogView
    {
        public event Action ContinueAdPressed, ContinueCoinsPressed, NoThanksPressed, RetryPressed, HomePressed;

        public void ShowOffer(string title, string adLabel, bool adEnabled, string coinsLabel, bool coinsEnabled, string noThanks, int[] fan)
        {
            Clear();
            var p = Panel(title, 1000f, danger: true);
            CardFan(p, 150f, fan);
            Button(p, adLabel, adEnabled ? ButtonStyle.Primary : ButtonStyle.Disabled, 470f, 680f, 164f, () => ContinueAdPressed?.Invoke(), _iconPlay);
            Button(p, coinsLabel, coinsEnabled ? ButtonStyle.Secondary : ButtonStyle.Disabled, 670f, 680f, 164f, () => ContinueCoinsPressed?.Invoke(), coinsEnabled ? _coin : _iconLock);
            Link(p, noThanks, 860f, () => NoThanksPressed?.Invoke());
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
