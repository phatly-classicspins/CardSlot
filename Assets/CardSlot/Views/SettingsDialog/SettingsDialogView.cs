using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Settings dialog (mock-up settings-v01): three toggles and a close button. Relays the row index
    /// toggled; the controller owns the values and re-renders.</summary>
    [DisallowMultipleComponent]
    public sealed class SettingsDialogView : CardDialogView
    {
        public event Action<int> Toggled;
        public event Action ClosePressed;

        public void Show(string title, string[] rows, bool[] on, string onLabel, string offLabel)
        {
            Clear();
            var p = Panel(title, 820f);
            CloseButton(p, () => ClosePressed?.Invoke());
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                var row = UiKit.Image($"Row{i}", p, _rowSunken, 70f, 190f + i * 170f, PanelWidth - 140f, 130f, sliced: true);
                UiKit.Text("Name", row.transform, _font, rows[i], 50f, DesignTokens.Ink, 44f, 0f, 400f, 130f, TMPro.TextAlignmentOptions.Left);
                var track = UiKit.Image("Toggle", row.transform, on[i] ? _toggleOn : _toggleOff, PanelWidth - 140f - 200f, 21f, 170f, 88f);
                UiKit.Image("Knob", track.transform, _toggleKnob, on[i] ? 90f : 8f, 8f, 72f, 78f);
                UiKit.Text("State", track.transform, _font, on[i] ? onLabel : offLabel, 30f, DesignTokens.OnColor, on[i] ? 14f : 86f, 0f, 72f, 88f);
                UiKit.Button(row, () => Toggled?.Invoke(index));
            }
        }
    }
}
