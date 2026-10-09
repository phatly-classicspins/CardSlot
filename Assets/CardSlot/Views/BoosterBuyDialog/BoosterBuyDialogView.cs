using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Booster buy dialog (mock-ups booster-buy-v02 / booster-buy-poor-v02, CR-012 C2): the booster's big icon, name and
    /// tip, a coin button (primary; greyed with a "not enough" line when short) and "▶ Free" (rewarded ad). Renders given labels;
    /// relays the two choices and the close button.</summary>
    [DisallowMultipleComponent]
    public sealed class BoosterBuyDialogView : CardDialogView
    {
        public event Action CoinsPressed, AdPressed;

        public void Show(string title, string name, string tip, int booster, string price, bool affordable, string notEnough,
            string ad, bool adEnabled, string coins)
        {
            Clear();
            var p = Panel(title, 1080f);
            CoinPill(coins);
            CloseButton(p, RaiseCloseRequested);
            BoosterIcon(p, 130f, booster);
            Line(p, name, 430f, DesignTokens.TypeButton, DesignTokens.Ink, 90f);
            Line(p, tip, 520f, DesignTokens.TypeBody, DesignTokens.InkSoft);
            CoinButton(p, price, affordable, 640f);
            if (!affordable) Line(p, notEnough, 806f, DesignTokens.TypeLabel + 4f, DesignTokens.InkSoft, 50f);
            Button(p, ad, adEnabled ? ButtonStyle.Secondary : ButtonStyle.Disabled, affordable ? 830f : 866f, 620f, 150f, () => AdPressed?.Invoke(), _iconPlay);
        }

        // coin + price centred on a primary button (the C1 price button without a leading label)
        private void CoinButton(Transform p, string price, bool enabled, float y)
        {
            const float w = 620f, h = 164f;
            var b = PriceButton(p, string.Empty, price, enabled ? ButtonStyle.Primary : ButtonStyle.Disabled, y, (PanelWidth - w) / 2f, w, h, 60f,
                () => CoinsPressed?.Invoke());
            b.interactable = enabled;
        }
    }
}
