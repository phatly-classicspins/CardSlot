using System;
using ClassicSpins.PrototypeFramework.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Views
{
    public enum ButtonStyle { Primary, Secondary, Disabled }

    /// <summary>
    /// Shared chrome for CardSlot dialogs (mock-ups <c>docs/mockups/*-v01.png</c>, direction B): the panel,
    /// title ribbon, chunky buttons, text links and close button, built as runtime uGUI inside the template's
    /// <c>SafeArea</c> node. The dim behind is the dialog service's (never a Backdrop node here). Sprites are
    /// wired by <c>CardSlot/Content/Build</c>. Everything shown arrives localized from the controller.
    /// </summary>
    public abstract class CardDialogView : DialogViewBase
    {
        [SerializeField] protected Sprite _panel, _ribbon, _ribbonDanger, _buttonPrimary, _buttonSecondary, _buttonDisabled;
        [SerializeField] protected Sprite _iconPlay, _iconLock, _iconClose, _iconGear;
        [SerializeField] protected Sprite _closeButton, _rowSunken, _toggleOn, _toggleOff, _toggleKnob, _roundButton;
        [SerializeField] protected Sprite _pill, _coin;
        // CR-012 C2: the booster bar redrawn above the dim, and the big booster icon of the buy / unlock dialogs
        [SerializeField] protected Sprite _iconHolder, _boostTile, _badge, _priceTag, _lock;
        [SerializeField] protected Sprite[] _boosterIcons = new Sprite[3];
        [SerializeField] protected Sprite[] _cardFace = new Sprite[8];
        [SerializeField] protected TMP_FontAsset _font;

        private RectTransform _root;

        /// <summary>Dialog show/hide (animation-list.md <c>dialog_show</c>/<c>dialog_hide</c>).</summary>
        public override ViewTransition CreateTransition() =>
            ViewTransition.Combine(new FadeTransition(DesignTokens.Motion.Dialog), new ScaleTransition(DesignTokens.Motion.DialogFrom, DesignTokens.Motion.Dialog));

        protected RectTransform Root
        {
            get
            {
                if (_root != null) return _root;
                var host = transform.Find("SafeArea") ?? transform;
                _root = UiKit.Stretch("Content", host);
                return _root;
            }
        }

        /// <summary>Remove everything built so far (a dialog that changes state rebuilds).</summary>
        protected void Clear()
        {
            for (int i = Root.childCount - 1; i >= 0; i--) Destroy(Root.GetChild(i).gameObject);
        }

        protected float Height => Root.rect.height > 1f ? Root.rect.height : 1920f;

        /// <summary>A centred panel of height <paramref name="h"/> with its title ribbon; returns the panel.</summary>
        protected RectTransform Panel(string title, float h, bool danger = false)
        {
            float side = DesignTokens.PanelSideInset, w = 1080f - 2f * side;
            var panel = UiKit.Image("Panel", Root, _panel, side, (Height - h) / 2f + 40f, w, h, sliced: true, raycast: true);
            var ribbon = UiKit.Image("Ribbon", panel.transform, danger ? _ribbonDanger : _ribbon, -DesignTokens.RibbonOverhang, -DesignTokens.RibbonRise,
                w + 2f * DesignTokens.RibbonOverhang, DesignTokens.RibbonHeight, sliced: true);
            UiKit.Text("Title", ribbon.transform, _font, title, DesignTokens.TitleSize(title?.Length ?? 0), DesignTokens.OnColor, 0f, 0f, w + 60f, 170f);
            return panel.rectTransform;
        }

        protected float PanelWidth => 1080f - 2f * DesignTokens.PanelSideInset;

        protected Button Button(Transform parent, string label, ButtonStyle style, float y, float w, float h, Action onClick, Sprite icon = null, float x = float.NaN, float fontSize = float.NaN)
        {
            float fs = float.IsNaN(fontSize) ? h * 0.36f : fontSize;
            var sprite = style == ButtonStyle.Primary ? _buttonPrimary : style == ButtonStyle.Secondary ? _buttonSecondary : _buttonDisabled;
            float left = float.IsNaN(x) ? (PanelWidth - w) / 2f : x;
            var img = UiKit.Image("Button", parent, sprite, left, y, w, h, sliced: true);
            float textX = 0f, textW = w;
            if (icon != null)
            {
                float s = h * 0.38f;
                var t = UiKit.Text("Label", img.transform, _font, label, fs, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
                float labelWidth = t.GetPreferredValues(label ?? string.Empty).x;
                float total = s + 16f + labelWidth;
                textX = (w - total) / 2f + s + 16f; textW = labelWidth + 4f;
                t.rectTransform.anchoredPosition = new Vector2(textX, 0f); t.rectTransform.sizeDelta = new Vector2(textW, h - 14f);
                UiKit.Image("Icon", img.transform, icon, (w - total) / 2f, (h - 14f - s) / 2f, s, s).color = DesignTokens.OnColor;
            }
            else UiKit.Text("Label", img.transform, _font, label, float.IsNaN(fontSize) ? h * 0.4f : fontSize, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
            var b = UiKit.Button(img, onClick);
            b.interactable = style != ButtonStyle.Disabled;
            return b;
        }

        /// <summary>A button that pays coins (mock-up lose-offer-coins-v02): label, coin, price, centred as one row.</summary>
        protected Button PriceButton(Transform parent, string label, string price, ButtonStyle style, float y, float x, float w, float h, float fontSize, Action onClick)
        {
            var sprite = style == ButtonStyle.Primary ? _buttonPrimary : style == ButtonStyle.Secondary ? _buttonSecondary : _buttonDisabled;
            var img = UiKit.Image("PriceButton", parent, sprite, x, y, w, h, sliced: true);
            float coin = fontSize * 1.1f, gap = 12f;
            var a = UiKit.Text("Label", img.transform, _font, label, fontSize, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
            var b = UiKit.Text("Price", img.transform, _font, price, fontSize, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
            float wa = a.GetPreferredValues(label ?? string.Empty).x, wb = b.GetPreferredValues(price ?? string.Empty).x;
            float left = (w - (wa + gap + coin + gap + wb)) / 2f;
            a.rectTransform.anchoredPosition = new Vector2(left, 0f); a.rectTransform.sizeDelta = new Vector2(wa + 4f, h - 14f);
            UiKit.Image("Coin", img.transform, _coin, left + wa + gap, (h - 14f - coin) / 2f, coin, coin);
            b.rectTransform.anchoredPosition = new Vector2(left + wa + gap + coin + gap, 0f); b.rectTransform.sizeDelta = new Vector2(wb + 4f, h - 14f);
            var button = UiKit.Button(img, onClick);
            button.interactable = style != ButtonStyle.Disabled;
            return button;
        }

        /// <summary>The coin counter above the dim (mock-ups win / lose-offer-coins-v02): the same pill and place as the HUD's, so the
        /// player watches the balance while the dialog is up.</summary>
        protected void CoinPill(string coins) { if (coins != null) UiKit.CoinPill(Root, _pill, _coin, _font, coins); }

        /// <summary>The booster bar at its board place, above the dim (mock-ups unlock-hand-v02, lose-offer-boosters-v02).</summary>
        protected void Boosters(BoosterTileVisual[] tiles, Action<int> pressed, int only = -1)
        {
            if (tiles == null) return;
            BoosterBar.Draw(Root, tiles, new BoosterBarArt(_boostTile, _boosterIcons, _badge, _priceTag, _lock, _coin, _font), pressed, only);
        }

        /// <summary>The big booster icon on its sunken holder, centred in the panel; <paramref name="gift"/> ("×3") adds a badge.</summary>
        protected void BoosterIcon(Transform panel, float y, int booster, string gift = null)
        {
            const float w = 260f, h = 272f;
            float x = (PanelWidth - w) / 2f;
            var holder = UiKit.Image("Holder", panel, _iconHolder, x, y, w, h);
            var icon = _boosterIcons != null && booster >= 0 && booster < _boosterIcons.Length ? _boosterIcons[booster] : null;
            UiKit.Image("Icon", holder.transform, icon, 60f, 60f, 140f, 140f).color = DesignTokens.Secondary;
            if (gift == null) return;
            var badge = UiKit.Image("Gift", holder.transform, _badge, 180f, 196f, 110f, 84f, sliced: true);
            UiKit.Text("Amount", badge.transform, _font, gift, 50f, DesignTokens.OnColor, 0f, 0f, 110f, 76f);
        }

        /// <summary>A text-only action ("Home", "No thanks").</summary>
        protected GameObject Link(Transform parent, string label, float y, Action onClick)
        {
            var hit = UiKit.Image("Link", parent, null, (PanelWidth - 400f) / 2f, y, 400f, 80f);
            hit.color = Color.clear;
            UiKit.Text("Label", hit.transform, _font, label, DesignTokens.TypeBody - 2f, DesignTokens.InkSoft, 0f, 0f, 400f, 80f);
            UiKit.Button(hit, onClick);
            return hit.gameObject;
        }

        protected void CloseButton(Transform panel, Action onClick)
        {
            var img = UiKit.Image("Close", panel, _closeButton, PanelWidth - 80f, -30f, 110f, 120f);
            UiKit.Image("Icon", img.transform, _iconClose, 27f, 23f, 56f, 56f).color = DesignTokens.OnColor;
            UiKit.Button(img, onClick);
        }

        /// <summary>A centred line inside the panel's margins; a longer (translated) string wraps instead of
        /// spilling past the panel edge.</summary>
        protected TextMeshProUGUI Line(Transform parent, string text, float y, float size, Color color, float h = 70f)
        {
            var t = UiKit.Text("Line", parent, _font, text, size, color, 60f, y, PanelWidth - 120f, h);
            t.enableWordWrapping = true;
            return t;
        }


        /// <summary>Three fanned cards (win / lose art in the mock-ups); <paramref name="dim"/> greys them.</summary>
        protected void CardFan(Transform parent, float y, int[] colors, bool dim = false)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                var arm = UiKit.Node($"Arm{i}", parent, PanelWidth / 2f, y + 300f, 0f, 0f);
                arm.localRotation = Quaternion.Euler(0f, 0f, -(i - (colors.Length - 1) / 2f) * 16f);
                var c = UiKit.Image($"Card{i}", arm, _cardFace[colors[i]], -75f, -300f, 150f, 216f);
                if (dim) c.color = DesignTokens.CoveredTint;
            }
        }
    }
}
