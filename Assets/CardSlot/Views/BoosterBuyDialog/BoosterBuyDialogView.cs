using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Booster purchase (mock-ups booster-buy / booster-buy-poor v01).</summary>
    [DisallowMultipleComponent]
    public sealed class BoosterBuyDialogView : CardDialogView
    {
        public event Action CoinsPressed, AdPressed, ClosePressed;

        /// <param name="iconKind">0 = Undo, 1 = Extra Space (which glyph to draw).</param>
        public void Show(string title, string name, string desc, int iconKind, string coinsLabel, bool coinsEnabled, string adLabel, bool adEnabled)
        {
            Clear();
            var p = Panel(title, 1130f);
            CloseButton(p, () => ClosePressed?.Invoke());
            IconHolder(p, iconKind == 0 ? _iconUndo : _iconSpace, 150f);
            Line(p, name, 440f, 60f, DesignTokens.Ink);
            Line(p, desc, 520f, DesignTokens.TypeBody, DesignTokens.Ink);
            Button(p, coinsLabel, coinsEnabled ? ButtonStyle.Secondary : ButtonStyle.Disabled, 680f, 620f, 164f, () => CoinsPressed?.Invoke(), coinsEnabled ? _coin : _iconLock);
            Button(p, adLabel, adEnabled ? ButtonStyle.Primary : ButtonStyle.Disabled, 880f, 620f, 164f, () => AdPressed?.Invoke(), _iconPlay);
        }
    }
}
