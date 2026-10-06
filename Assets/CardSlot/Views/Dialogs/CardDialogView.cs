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
        [SerializeField] protected Sprite _coin, _iconPlay, _iconLock, _iconClose, _iconUndo, _iconSpace, _iconGear, _iconHolder;
        [SerializeField] protected Sprite _closeButton, _rowSunken, _toggleOn, _toggleOff, _toggleKnob, _pillSunken, _badge, _roundButton;
        [SerializeField] protected Sprite[] _cardFace = new Sprite[6];
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

        protected Button Button(Transform parent, string label, ButtonStyle style, float y, float w, float h, Action onClick, Sprite icon = null, float x = float.NaN)
        {
            var sprite = style == ButtonStyle.Primary ? _buttonPrimary : style == ButtonStyle.Secondary ? _buttonSecondary : _buttonDisabled;
            float left = float.IsNaN(x) ? (PanelWidth - w) / 2f : x;
            var img = UiKit.Image("Button", parent, sprite, left, y, w, h, sliced: true);
            float textX = 0f, textW = w;
            if (icon != null)
            {
                float s = h * 0.38f;
                var t = UiKit.Text("Label", img.transform, _font, label, h * 0.36f, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
                float labelWidth = t.GetPreferredValues(label ?? string.Empty).x;
                float total = s + 16f + labelWidth;
                textX = (w - total) / 2f + s + 16f; textW = labelWidth + 4f;
                t.rectTransform.anchoredPosition = new Vector2(textX, 0f); t.rectTransform.sizeDelta = new Vector2(textW, h - 14f);
                UiKit.Image("Icon", img.transform, icon, (w - total) / 2f, (h - 14f - s) / 2f, s, s).color = DesignTokens.OnColor;
            }
            else UiKit.Text("Label", img.transform, _font, label, h * 0.4f, DesignTokens.OnColor, 0f, 0f, w, h - 14f);
            var b = UiKit.Button(img, onClick);
            b.interactable = style != ButtonStyle.Disabled;
            return b;
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

        /// <summary>The big booster icon in its holder, with an optional "x2" gift badge.</summary>
        protected RectTransform IconHolder(Transform parent, Sprite icon, float y, string badge = null)
        {
            var holder = UiKit.Image("Holder", parent, _iconHolder, (PanelWidth - 260f) / 2f, y, 260f, 272f);
            UiKit.Image("Icon", holder.transform, icon, 50f, 46f, 160f, 160f).color = DesignTokens.Secondary;
            if (badge != null)
            {
                var b = UiKit.Image("Gift", holder.transform, _badge, 180f, 210f, 120f, 70f, sliced: true);
                UiKit.Text("Text", b.transform, _font, badge, 50f, DesignTokens.OnColor, 0f, 0f, 120f, 64f);
            }
            return holder.rectTransform;
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
