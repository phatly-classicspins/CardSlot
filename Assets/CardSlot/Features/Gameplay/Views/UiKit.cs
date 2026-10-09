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

    /// <summary>One booster tile as the controller decided it (features/boosters.md v2). Exactly one of
    /// <see cref="Locked"/>, <see cref="Badge"/>, <see cref="Price"/> is set; all text arrives localized.</summary>
    public struct BoosterTileVisual
    {
        public string Name;
        /// <summary>"Lv 8" while the booster is locked, else null.</summary>
        public string Locked;
        /// <summary>How many are owned, or null when none.</summary>
        public string Badge;
        /// <summary>The coin price shown when none is owned, or null.</summary>
        public string Price;
        /// <summary>Highlighted: the tile being used (pick mode) or just unlocked.</summary>
        public bool Active;
    }

    /// <summary>The sprites the booster bar draws with (wired by <c>CardSlot/Content/Build</c>).</summary>
    public readonly struct BoosterBarArt
    {
        public readonly Sprite Tile, Badge, PriceTag, Lock, Coin;
        public readonly Sprite[] Icons;
        public readonly TMP_FontAsset Font;
        public BoosterBarArt(Sprite tile, Sprite[] icons, Sprite badge, Sprite priceTag, Sprite lockIcon, Sprite coin, TMP_FontAsset font)
        {
            Tile = tile; Icons = icons; Badge = badge; PriceTag = priceTag; Lock = lockIcon; Coin = coin; Font = font;
        }
    }

    /// <summary>The booster bar (mock-ups gameplay-boosters-v02, CR-012 C2): three tiles centred along the screen bottom. The board
    /// draws it, and the lose / unlock dialogs draw the same bar above the dim, at the same place.</summary>
    public static class BoosterBar
    {
        public static float TileX(int i, int n) =>
            (1080f - (n * DesignTokens.BoosterTileWidth + (n - 1) * DesignTokens.BoosterTileGap)) / 2f + i * (DesignTokens.BoosterTileWidth + DesignTokens.BoosterTileGap);

        /// <summary>Draw <paramref name="tiles"/> under <paramref name="parent"/> (a full-screen node); a press relays the
        /// tile index. <paramref name="only"/> ≥ 0 draws just that tile (pick mode: the active tile above the dim).</summary>
        public static void Draw(Transform parent, BoosterTileVisual[] tiles, in BoosterBarArt art, Action<int> pressed, int only = -1)
        {
            float w = DesignTokens.BoosterTileWidth, h = DesignTokens.BoosterTileHeight, y = DesignTokens.BoosterBarBottom;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (only >= 0 && i != only) continue;
                var t = tiles[i];
                float x = TileX(i, tiles.Length);
                if (t.Active)
                {
                    var glow = UiKit.Image($"Glow{i}", parent, art.Tile, x - 14f, y - 8f, w + 28f, h + 22f, fromBottom: true);
                    glow.color = DesignTokens.OnColor;
                }
                var tile = UiKit.Image($"Booster{i}", parent, art.Tile, x, y, w, h, fromBottom: true);
                if (t.Locked != null)
                {
                    tile.color = DesignTokens.CoveredTint;
                    UiKit.Image("Lock", tile.transform, art.Lock, (w - 80f) / 2f, 30f, 80f, 80f).color = DesignTokens.InkSoft;
                    UiKit.Text("Level", tile.transform, art.Font, t.Locked, 36f, DesignTokens.InkSoft, 0f, 112f, w, 50f);
                }
                else
                {
                    var icon = art.Icons != null && i < art.Icons.Length ? art.Icons[i] : null;
                    UiKit.Image("Icon", tile.transform, icon, (w - 90f) / 2f, 34f, 90f, 90f).color = DesignTokens.Secondary;
                    UiKit.Text("Name", parent, art.Font, t.Name, DesignTokens.TypeBoosterLabel, DesignTokens.Ink,
                        x - 20f, y - DesignTokens.BoosterLabelGap - DesignTokens.BoosterLabelHeight, w + 40f, DesignTokens.BoosterLabelHeight, fromBottom: true);
                }
                if (t.Badge != null)
                {
                    float b = DesignTokens.BoosterBadge;
                    var badge = UiKit.Image("Badge", tile.transform, art.Badge, w - 50f, -22f, b, b - 2f, sliced: true);
                    UiKit.Text("Count", badge.transform, art.Font, t.Badge, 40f, DesignTokens.OnColor, 0f, 0f, b, b - 10f);
                }
                if (t.Price != null)
                {
                    var tag = UiKit.Image("Price", tile.transform, art.PriceTag, 18f, h - 50f, w - 36f, 64f, sliced: true);
                    var label = UiKit.Text("Amount", tag.transform, art.Font, t.Price, 34f, DesignTokens.OnColor, 0f, 0f, w - 36f, 60f);
                    float tw = label.GetPreferredValues(t.Price).x, coin = 40f, total = coin + 6f + tw;
                    UiKit.Image("Coin", tag.transform, art.Coin, (w - 36f - total) / 2f, 10f, coin, coin);
                    label.rectTransform.anchoredPosition = new Vector2((w - 36f - total) / 2f + coin + 6f, 0f);
                    label.rectTransform.sizeDelta = new Vector2(tw + 4f, 60f);
                }
                int index = i;
                UiKit.Button(tile, () => pressed?.Invoke(index));
            }
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
