using System;
using UnityEngine;

namespace Game.Views
{
    /// <summary>Pause dialog (mock-up pause-v01).</summary>
    [DisallowMultipleComponent]
    public sealed class PauseDialogView : CardDialogView
    {
        public event Action ResumePressed, RestartPressed, HomePressed, SettingsPressed;

        public void Show(string title, string resume, string restart, string home)
        {
            Clear();
            var p = Panel(title, 1020f);
            Button(p, resume, ButtonStyle.Primary, 180f, 620f, 164f, () => ResumePressed?.Invoke());
            Button(p, restart, ButtonStyle.Secondary, 400f, 620f, 164f, () => RestartPressed?.Invoke());
            Button(p, home, ButtonStyle.Secondary, 600f, 620f, 164f, () => HomePressed?.Invoke());
            var gear = UiKit.Image("Settings", p, _roundButton, (PanelWidth - 120f) / 2f, 820f, 120f, 130f);
            UiKit.Image("Icon", gear.transform, _iconGear, 30f, 26f, 60f, 60f).color = DesignTokens.OnColor;
            UiKit.Button(gear, () => SettingsPressed?.Invoke());
        }
    }
}
