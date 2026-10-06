using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Booster unlock popup (mock-ups unlock-undo / unlock-space v01).</summary>
    [DisallowMultipleComponent]
    public sealed class BoosterUnlockDialogView : CardDialogView
    {
        public event Action GotItPressed;

        /// <param name="iconKind">0 = Undo, 1 = Extra Space.</param>
        public void Show(string title, string desc, int iconKind, string gift, string gotIt)
        {
            Clear();
            var p = Panel(title, 980f);
            IconHolder(p, iconKind == 0 ? _iconUndo : _iconSpace, 170f, gift);
            Line(p, desc, 500f, DesignTokens.TypeBody, DesignTokens.Ink);
            Button(p, gotIt, ButtonStyle.Primary, 680f, 520f, 164f, () => GotItPressed?.Invoke());
        }
    }
}
