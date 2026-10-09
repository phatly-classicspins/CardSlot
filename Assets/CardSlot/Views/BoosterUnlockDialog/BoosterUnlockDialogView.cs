using UnityEngine;

namespace Game.Views
{
    /// <summary>Booster unlock popup (mock-up unlock-hand-v02, CR-012 C2): the new booster's big icon with its gift badge, the tip,
    /// "Got it", and the booster bar above the dim with the new tile lit. Renders given labels; any of the buttons closes.</summary>
    [DisallowMultipleComponent]
    public sealed class BoosterUnlockDialogView : CardDialogView
    {
        public void Show(string title, string tip, string gift, string ok, int booster, BoosterTileVisual[] boosters)
        {
            Clear();
            var p = Panel(title, 860f);
            BoosterIcon(p, 130f, booster, gift);
            Line(p, tip, 470f, DesignTokens.TypeBody + 2f, DesignTokens.Ink);
            Button(p, ok, ButtonStyle.Primary, 600f, 560f, 164f, RaiseCloseRequested);
            Boosters(boosters, _ => RaiseCloseRequested(), only: booster);   // only the new tile is lifted above the dim
        }
    }
}
