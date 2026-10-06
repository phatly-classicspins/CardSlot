using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Restart confirmation (mock-up restart-confirm-v01).</summary>
    [DisallowMultipleComponent]
    public sealed class RestartConfirmDialogView : CardDialogView
    {
        public event Action YesPressed, NoPressed;

        public void Show(string title, string note, string yes, string no)
        {
            Clear();
            var p = Panel(title, 560f);
            Line(p, note, 150f, DesignTokens.TypeBody, DesignTokens.InkSoft);
            Button(p, yes, ButtonStyle.Secondary, 320f, 330f, 164f, () => YesPressed?.Invoke(), x: PanelWidth / 2f - 360f);
            Button(p, no, ButtonStyle.Primary, 320f, 330f, 164f, () => NoPressed?.Invoke(), x: PanelWidth / 2f + 30f);
        }
    }
}
