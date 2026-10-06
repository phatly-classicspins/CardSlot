using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Win dialog (mock-ups win / win-hard / win-final v01). Renders given labels; relays the two choices.</summary>
    [DisallowMultipleComponent]
    public sealed class WinDialogView : CardDialogView
    {
        public event Action NextPressed, HomePressed;

        /// <param name="note">"More levels coming soon" on the last level, else null.</param>
        public void Show(string title, string subtitle, string reward, string primary, string home, string note, int[] fan)
        {
            Clear();
            var p = Panel(title, note != null ? 1140f : 1100f);
            Line(p, subtitle, 150f, DesignTokens.TypeHeading, DesignTokens.InkSoft);
            CardFan(p, 260f, fan);
            var pill = UiKit.Image("Reward", p, _pillSunken, (PanelWidth - 280f) / 2f, 560f, 280f, 128f, sliced: true);
            UiKit.Image("Coin", pill.transform, _coin, 18f, 16f, 88f, 88f);
            UiKit.Text("Amount", pill.transform, _font, reward, 64f, DesignTokens.Ink, 110f, 0f, 160f, 120f, TMPro.TextAlignmentOptions.Left);
            float y = 760f;
            if (note != null) { Line(p, note, 720f, 42f, DesignTokens.InkSoft, 60f); y = 800f; }
            Button(p, primary, ButtonStyle.Primary, y, 560f, 164f, () => NextPressed?.Invoke());
            Link(p, home, y + 210f, () => HomePressed?.Invoke());
        }
    }
}
