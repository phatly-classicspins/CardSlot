using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Win dialog (mock-ups win / win-final v01; no hard levels). Renders given labels; relays the two choices.</summary>
    [DisallowMultipleComponent]
    public sealed class WinDialogView : CardDialogView
    {
        public event Action NextPressed, HomePressed;

        /// <param name="note">"More levels coming soon" on the last level, else null.</param>
        public void Show(string title, string subtitle, string primary, string home, string note, int[] fan)
        {
            Clear();
            var p = Panel(title, note != null ? 960f : 900f);
            Line(p, subtitle, 150f, DesignTokens.TypeHeading, DesignTokens.InkSoft);
            CardFan(p, 260f, fan);
            float y = 560f;
            if (note != null) { Line(p, note, 540f, 42f, DesignTokens.InkSoft, 60f); y = 620f; }
            Button(p, primary, ButtonStyle.Primary, y, 560f, 164f, () => NextPressed?.Invoke());
            Link(p, home, y + 210f, () => HomePressed?.Invoke());
        }
    }
}
