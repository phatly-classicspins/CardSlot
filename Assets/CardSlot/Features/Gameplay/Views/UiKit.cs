using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Views
{
    /// <summary>
    /// Small builders for runtime uGUI in mock-up coordinates: (x, y) is the top-left corner measured from
    /// the parent's top-left, in reference px (1 px = 1 world unit). Pure presentation — no game state.
    /// </summary>
    public static class UiKit
    {
        public static RectTransform Node(string name, Transform parent, float x, float y, float w, float h, bool fromBottom = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var a = fromBottom ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
            rt.anchorMin = rt.anchorMax = rt.pivot = a;
            rt.anchoredPosition = fromBottom ? new Vector2(x, y) : new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, bool sliced = false, bool raycast = false, bool fromBottom = false)
        {
            var rt = Node(name, parent, x, y, w, h, fromBottom);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Fill(string name, Transform parent, Color color, bool raycast)
        {
            var rt = Stretch(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color,
            float x, float y, float w, float h, TextAlignmentOptions align = TextAlignmentOptions.Center, bool fromBottom = false)
        {
            var rt = Node(name, parent, x, y, w, h, fromBottom);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.SetText(value ?? string.Empty);
            return t;
        }

        public static Button Button(Image image, Action onClick)
        {
            image.raycastTarget = true;
            var b = image.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = image;
            // AddComponent may already have run a default ColorTint transition (e.g. the "disabled" tint while a
            // dialog's CanvasGroup is non-interactable during its show leg); None never undoes it, so reset it here
            image.canvasRenderer.SetColor(Color.white);
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        /// <summary>The coin counter (mock-ups *-coins-v02, CR-012 C1): a surface pill at the top-left screen corner with
        /// the coin overlapping its left end; returns the number text so the caller can update it.</summary>
        public static TextMeshProUGUI CoinPill(Transform parent, Sprite pill, Sprite coin, TMP_FontAsset font, string label)
        {
            const float x = 40f, y = 66f, w = 270f, h = 104f;
            Image("CoinPill", parent, pill, x, y, w, h, sliced: true);
            Image("Coin", parent, coin, x - 6f, y + 4f, 96f, 96f);
            return Text("Coins", parent, font, label ?? string.Empty, DesignTokens.TypeHud, DesignTokens.Ink, x + 84f, y, w - 100f, h - 8f);
        }
    }

    /// <summary>Relays a tap on a board element as its index (rule #9: EventSystem only, no polling;
    /// rule #10: the controller gets a game-unit index, never a screen position).</summary>
    public sealed class TapRelay : MonoBehaviour, IPointerClickHandler
    {
        public int Index;
        public Action<int> Tapped;

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke(Index);
    }
}
