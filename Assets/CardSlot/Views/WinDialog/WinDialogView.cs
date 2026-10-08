using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Win dialog (mock-ups win-coins-v02, win-final v01; CR-012 C1): the coin reward, "▶ Claim ×2" (rewarded ad,
    /// primary), Next (secondary, takes the plain reward) and Home. Renders given labels; relays the three choices.</summary>
    [DisallowMultipleComponent]
    public sealed class WinDialogView : CardDialogView
    {
        public event Action ClaimPressed, NextPressed, HomePressed;

        /// <param name="note">"More levels coming soon" on the last level, else null.</param>
        /// <param name="claimEnabled">False when the ad is not ready (the button shows the controller's label, greyed).</param>
        public void Show(string title, string subtitle, string reward, string claim, bool claimEnabled, string next, string home,
            string note, string coins, int[] fan)
        {
            Clear();
            float extra = note != null ? 60f : 0f;
            var p = Panel(title, 1200f + extra);
            CoinPill(coins);
            Line(p, subtitle, 150f, DesignTokens.TypeHeading, DesignTokens.InkSoft);
            CardFan(p, 240f, fan);
            // the reward chip: coin + "+20"
            var chip = UiKit.Node("Reward", p, 0f, 560f, PanelWidth, 96f);
            var t = UiKit.Text("Amount", chip, _font, reward, DesignTokens.TypeButton, DesignTokens.Ink, 0f, 0f, PanelWidth, 96f);
            float tw = t.GetPreferredValues(reward ?? string.Empty).x, coin = 84f, total = coin + 14f + tw;
            UiKit.Image("Coin", chip, _coin, (PanelWidth - total) / 2f, 6f, coin, coin);
            t.rectTransform.anchoredPosition = new Vector2((PanelWidth - total) / 2f + coin + 14f, 0f);
            t.rectTransform.sizeDelta = new Vector2(tw + 4f, 96f);
            if (note != null) Line(p, note, 650f, 42f, DesignTokens.InkSoft, 60f);
            Button(p, claim, claimEnabled ? ButtonStyle.Primary : ButtonStyle.Disabled, 690f + extra, 680f, 164f, () => ClaimPressed?.Invoke(), _iconPlay);
            Button(p, next, ButtonStyle.Secondary, 878f + extra, 560f, 140f, () => NextPressed?.Invoke());
            Link(p, home, 1036f + extra, () => HomePressed?.Invoke());
        }
    }
}
