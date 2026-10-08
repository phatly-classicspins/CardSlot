using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Views
{
    /// <summary>What one stack looks like right now. Colours are glossary indices (0..5), top card first.</summary>
    public struct StackVisual
    {
        public int X, Y, W, H, Layer;
        public int[] Colors;
        public bool Covered;
        /// <summary>Drawn as a fan around a pivot below the stack (CR-004) instead of a straight pile.</summary>
        public bool Fan;
        /// <summary>The stack can be tapped right now (not covered, not empty): draw the hint outline (decided by the controller).</summary>
        public bool Hint;
    }

    /// <summary>
    /// One card flying after a tap (CR-007: a run leaves card by card, as in the reference). Indices are
    /// the controller's; the view only looks positions up. A source is a stack card (<see cref="FromStack"/>,
    /// <see cref="FromDepth"/> from the top, as drawn before the tap) or a holding groove (<see cref="FromHeld"/>);
    /// a destination is a peg position (<see cref="ToSlot"/>, <see cref="ToIndex"/>) or a groove
    /// (<see cref="ToHeld"/>, a groove index). <see cref="ToHeldFinal"/> is where that card sits once the tap has settled
    /// (−1 = it left the holding area again), <see cref="After"/> the flight it must wait for, and
    /// <see cref="Completes"/> marks a card whose peg fills up and leaves.
    /// </summary>
    public struct CardFlight
    {
        public int Color, FromStack, FromDepth, FromHeld, ToSlot, ToIndex, ToHeld, ToHeldFinal, After;
        public bool Completes;
        /// <summary>How many times <see cref="ToSlot"/>'s peg filled up earlier in this same change: 0 = the peg on screen,
        /// 1 = the one that replaced it, … A card for a replacement peg waits until the full one has left and the
        /// replacement has moved up (Phat: the next peg only takes cards after the old one is gone).</summary>
        public int SwapGen;
    }

    public struct TargetVisual
    {
        public bool Has;
        public int Color, Filled, Capacity;
        public string CountLabel;
    }

    /// <summary>Everything the board shows, already decided by the controller (rule: a View never branches on
    /// game state). Labels arrive localized.</summary>
    public sealed class BoardVisual
    {
        public StackVisual[] Stacks = Array.Empty<StackVisual>();
        public TargetVisual[] Targets = Array.Empty<TargetVisual>();
        /// <summary>CR-009: per slot, the colour of the target waiting behind it (−1 = none).</summary>
        public int[] Upcoming = Array.Empty<int>();
        public int[] Buffer = Array.Empty<int>();
        public int BufferCapacity;
        /// <summary>The groove of each holding-area card, in <see cref="Buffer"/> order: a card keeps its groove until
        /// it leaves, so the others never shift (Phat).</summary>
        public int[] BufferGrooves = Array.Empty<int>();
        public bool BufferWarn;
        public string HoldingLabel, BufferCountLabel, NextLabel;
    }

    /// <summary>
    /// The gameplay board (mock-up <c>docs/mockups/gameplay-v01.png</c>): HUD, target row, holding area and the
    /// tray of stacks, built as runtime uGUI from the approved sprites (art v1) on the 1080-wide rig. It renders
    /// what it is given and relays taps as stack indices; every decision is the controller's.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Cards — index = glossary colour")]
        [SerializeField] private Sprite[] _cardFace = new Sprite[6];
        [SerializeField] private Sprite[] _cardUnder = new Sprite[6];
        [SerializeField] private Sprite[] _cardMini = new Sprite[6];
        [SerializeField] private Sprite[] _chip = new Sprite[6];
        [SerializeField] private Sprite[] _targetBase = new Sprite[6];
        [Header("Board")]
        [SerializeField] private Sprite _hatch;
        [SerializeField] private Sprite _targetSlot;
        [SerializeField] private Sprite _trayRim;
        [SerializeField] private Sprite _trayInner;
        [SerializeField] private Sprite _bufferRail;
        [SerializeField] private Sprite _bufferRailWarn;
        [SerializeField] private Sprite _bufferCell;
        [SerializeField] private Sprite _bufferCellWarn;
        [SerializeField] private Sprite _ground;
        [Header("HUD, tutorial")]
        [SerializeField] private Sprite _pill;
        [SerializeField] private Sprite _roundButton;
        [SerializeField] private Sprite _iconPause;
        [SerializeField] private Sprite _iconRestart;
        [SerializeField] private Sprite _hand;
        [SerializeField] private Sprite _ring;
        [SerializeField] private TMP_FontAsset _font;
        [Tooltip("False when the board is drawn in 3D (CR-003): this view then draws only the HUD, labels and tap areas.")]
        [SerializeField] private bool _draw2DBoard = true;

        public event Action<int> StackTapped;
        public event Action PausePressed, RestartPressed;

        private RectTransform _root, _boardLayer, _tutorialLayer;
        private TextMeshProUGUI _levelLabel;

        private float Height => _root != null && _root.rect.height > 1f ? _root.rect.height : 1920f;
        private float Tall => Mathf.Max(0f, Height - 1920f);

        private void EnsureBuilt()
        {
            if (_root != null) return;
            _root = UiKit.Stretch("Root", transform);
            if (_draw2DBoard)
            {
                var ground = UiKit.Fill("Ground", _root, Color.white, false);
                ground.sprite = _ground;
            }
            _boardLayer = UiKit.Stretch("Board", _root);
            BuildHud();
            _tutorialLayer = UiKit.Stretch("Tutorial", _root);
        }

        private void BuildHud()
        {
            float m = DesignTokens.ScreenMargin, b = DesignTokens.RoundButton;
            var pill = UiKit.Image("LevelPill", _root, _pill, 540f - 125f, 70f, 250f, 104f, sliced: true);
            _levelLabel = UiKit.Text("Level", pill.transform, _font, string.Empty, DesignTokens.TypeHud, DesignTokens.Ink, 0f, 0f, 250f, 96f);
            var restart = UiKit.Image("Restart", _root, _roundButton, 1080f - m - 2f * b - 32f, DesignTokens.HudTop, b, b + 10f);
            UiKit.Image("Icon", restart.transform, _iconRestart, 26f, 20f, 52f, 52f).color = DesignTokens.OnColor;
            UiKit.Button(restart, () => RestartPressed?.Invoke());
            var pause = UiKit.Image("Pause", _root, _roundButton, 1080f - m - b, DesignTokens.HudTop, b, b + 10f);
            UiKit.Image("Icon", pause.transform, _iconPause, 28f, 22f, 48f, 48f).color = DesignTokens.OnColor;
            UiKit.Button(pause, () => PausePressed?.Invoke());
        }

        public void SetHud(string levelLabel)
        {
            EnsureBuilt();
            _levelLabel.SetText(levelLabel ?? string.Empty);
        }

        /// <summary>Redraw the whole board. 6a redraws after every tap; 6c replays the model's steps as motion.</summary>
        public void Render(BoardVisual v)
        {
            EnsureBuilt();
            for (int i = _boardLayer.childCount - 1; i >= 0; i--) Destroy(_boardLayer.GetChild(i).gameObject);
            if (!_draw2DBoard) { DrawLabels(v); return; }
            DrawTargets(v);
            DrawBuffer(v);
            DrawTray(v);
        }

        /// <summary>
        /// 3D mode (CR-003): invisible tap areas over the stacks the 3D view draws. <paramref name="worldRects"/>
        /// are world-space rectangles (game units); <paramref name="layers"/> orders them so a higher stack sits
        /// on top. Taps relay the stack index; whether the tap counts is the controller's call (R-4).
        /// </summary>
        public void SetHitAreas(Rect[] worldRects, int[] layers)
        {
            EnsureBuilt();
            var order = new List<int>();
            for (int i = 0; i < worldRects.Length; i++) if (worldRects[i].width > 0f) order.Add(i);
            order.Sort((a, b) => layers[a] != layers[b] ? layers[a].CompareTo(layers[b]) : a.CompareTo(b));
            var rect = _root.rect;
            foreach (int i in order)
            {
                var r = worldRects[i];
                // world → this view's local space (the Ui host may be inset by the safe area), then top-left coordinates
                var min = _root.InverseTransformPoint(new Vector3(r.xMin, r.yMin, _root.position.z));
                var max = _root.InverseTransformPoint(new Vector3(r.xMax, r.yMax, _root.position.z));
                float x = min.x - rect.xMin, y = rect.yMax - max.y;
                var hit = UiKit.Image($"Hit{i}", _boardLayer, null, x, y, max.x - min.x, max.y - min.y, raycast: true);
                hit.color = Color.clear;
                var relay = hit.gameObject.AddComponent<TapRelay>();
                relay.Index = i;
                relay.Tapped = index => StackTapped?.Invoke(index);
            }
        }

        // 3D mode: the text that sits over the 3D board (counts, holding area, next) — positions from the mock-up
        private void DrawLabels(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin;
            // no x/18 count: a pole is exactly 18 cards tall, so how full it is shows on the peg (Phat)
            UiKit.Text("HoldingLabel", _boardLayer, _font, v.HoldingLabel, DesignTokens.TypeLabel, DesignTokens.InkSoft, m + 30f, DesignTokens.BufferLabelTop, 400f, 40f, TextAlignmentOptions.Left);
            UiKit.Text("BufferCount", _boardLayer, _font, v.BufferCountLabel, DesignTokens.TypeLabel, v.BufferWarn ? DesignTokens.DangerText : DesignTokens.Ink,
                1080f - m - 30f - 300f, DesignTokens.BufferLabelTop, 300f, 40f, TextAlignmentOptions.Right);
            // CR-007: the queue is drawn as the back row of pegs in Board3DView, not as chips
        }

        private void DrawTargets(BoardVisual v)
        {
            int n = v.Targets.Length;
            float w = n == 2 ? DesignTokens.TargetSlotWidthTwo : DesignTokens.TargetSlotWidth;
            float gap = n == 2 ? DesignTokens.TargetSlotGapTwo : DesignTokens.TargetSlotGap;
            float x0 = 540f - (n * w + (n - 1) * gap) / 2f;
            for (int i = 0; i < n; i++)
            {
                var t = v.Targets[i];
                var slot = UiKit.Image($"Target{i}", _boardLayer, _targetSlot, x0 + i * (w + gap), DesignTokens.TargetsTop, w, DesignTokens.TargetsHeight + 8f, sliced: true);
                if (!t.Has) continue;
                for (int j = 0; j < t.Filled; j++)
                    UiKit.Image($"Card{j}", slot.transform, _cardFace[t.Color], w / 2f - 75f, DesignTokens.TargetCardTop - j * DesignTokens.TargetCardStep, DesignTokens.CardWidth, DesignTokens.CardHeight);
                UiKit.Image("Base", slot.transform, _targetBase[t.Color], w / 2f - 115f, 296f, 230f, 80f, sliced: true);
                UiKit.Text("Count", slot.transform, _font, t.CountLabel, DesignTokens.TypeCount, DesignTokens.OnColor, 0f, 300f, w, 60f);
            }
            if (v.Upcoming.Length > 0)
            {
                float x = 1080f - DesignTokens.ScreenMargin - 12f - v.Upcoming.Length * 44f;
                UiKit.Text("NextLabel", _boardLayer, _font, v.NextLabel, 28f, DesignTokens.InkSoft, x - 90f, 194f, 84f, 46f, TextAlignmentOptions.Right);
                for (int i = 0; i < v.Upcoming.Length; i++)
                    if (v.Upcoming[i] >= 0) UiKit.Image($"Next{i}", _boardLayer, _chip[v.Upcoming[i]], x + i * 44f, 194f, 34f, 50f);
            }
        }

        private void DrawBuffer(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin;
            UiKit.Text("HoldingLabel", _boardLayer, _font, v.HoldingLabel, DesignTokens.TypeLabel, DesignTokens.InkSoft, m + 30f, DesignTokens.BufferLabelTop, 400f, 40f, TextAlignmentOptions.Left);
            UiKit.Text("BufferCount", _boardLayer, _font, v.BufferCountLabel, DesignTokens.TypeLabel, v.BufferWarn ? DesignTokens.DangerText : DesignTokens.Ink,
                1080f - m - 30f - 300f, DesignTokens.BufferLabelTop, 300f, 40f, TextAlignmentOptions.Right);
            var rail = UiKit.Image("Rail", _boardLayer, v.BufferWarn ? _bufferRailWarn : _bufferRail, m, DesignTokens.BufferTop, 1080f - 2f * m, DesignTokens.BufferHeight, sliced: true);
            int cap = Mathf.Max(1, v.BufferCapacity);
            float inner = DesignTokens.BufferInnerWidth;
            float cw = Mathf.Min(DesignTokens.BufferCellWidth, Mathf.Floor((inner - 8f * (cap - 1)) / cap));
            float step = cap > 1 ? cw + Mathf.Floor((inner - cw * cap) / (cap - 1)) : 0f;
            for (int i = 0; i < cap; i++)
            {
                float x = DesignTokens.BufferInset + i * step;
                if (i < v.Buffer.Length)
                    UiKit.Image($"Card{i}", rail.transform, _cardMini[v.Buffer[i]], x, DesignTokens.BufferCellTop, cw, DesignTokens.BufferCellHeight + 6f);
                else
                    UiKit.Image($"Cell{i}", rail.transform, v.BufferWarn ? _bufferCellWarn : _bufferCell, x, DesignTokens.BufferCellTop, cw, DesignTokens.BufferCellHeight);
            }
        }

        private void DrawTray(BoardVisual v)
        {
            float m = DesignTokens.ScreenMargin, tall = Tall;
            float h = DesignTokens.TrayHeight + tall;
            UiKit.Image("TrayRim", _boardLayer, _trayRim, m, DesignTokens.TrayTop, 1080f - 2f * m, h + 14f, sliced: true);
            float inset = DesignTokens.TrayRimInset;
            var inner = UiKit.Image("TrayInner", _boardLayer, _trayInner, m + inset, DesignTokens.TrayTop + inset, 1080f - 2f * (m + inset), h - 2f * inset, sliced: true);
            float oy = Mathf.Round(tall / 2f);
            // draw lower layers first so higher layers sit on top
            var order = new List<int>();
            for (int i = 0; i < v.Stacks.Length; i++) order.Add(i);
            order.Sort((a, b) => v.Stacks[a].Layer != v.Stacks[b].Layer ? v.Stacks[a].Layer.CompareTo(v.Stacks[b].Layer) : a.CompareTo(b));
            foreach (int i in order)
            {
                var s = v.Stacks[i];
                if (s.Colors == null || s.Colors.Length == 0) continue;
                var stack = UiKit.Node($"Stack{i}", inner.transform, s.X, s.Y + oy, DesignTokens.CardWidth, DesignTokens.CardHeight + DesignTokens.CardStep * (s.Colors.Length - 1));
                for (int d = s.Colors.Length - 1; d >= 1; d--)
                {
                    var under = UiKit.Image($"Under{d}", stack, _cardUnder[s.Colors[d]], 0f, d * DesignTokens.CardStep, DesignTokens.CardWidth, DesignTokens.CardUnderHeight);
                    if (s.Covered) under.color = DesignTokens.CoveredTint;
                }
                var top = UiKit.Image("Top", stack, _cardFace[s.Colors[0]], 0f, 0f, DesignTokens.CardWidth, DesignTokens.CardHeight, raycast: true);
                if (s.Covered)
                {
                    top.color = DesignTokens.CoveredTint;
                    UiKit.Image("Hatch", top.transform, _hatch, 0f, 0f, DesignTokens.CardWidth, DesignTokens.CardBody);
                }
                var relay = top.gameObject.AddComponent<TapRelay>();
                relay.Index = i;
                relay.Tapped = index => StackTapped?.Invoke(index);
            }
        }

        /// <summary>FTUE (features/ftue.md): a hand + ring over <paramref name="pointAt"/> (world rect; empty = no
        /// hand) and a bubble with <paramref name="text"/>.</summary>
        public void ShowTutorial(Rect pointAt, string text)
        {
            EnsureBuilt();
            HideTutorial();
            if (pointAt.width > 0f)
            {
                var c = _root.InverseTransformPoint(new Vector3(pointAt.center.x, pointAt.center.y, _root.position.z));
                float x = c.x - _root.rect.xMin, y = _root.rect.yMax - c.y;
                var ring = UiKit.Image("Ring", _tutorialLayer, _ring, x - 85f, y - 85f, 170f, 170f);
                ring.color = new Color(1f, 1f, 1f, 0.85f);
                UiKit.Image("Hand", _tutorialLayer, _hand, x + 10f, y + 10f, 150f, 172f);
            }
            if (!string.IsNullOrEmpty(text))
            {
                var bubble = UiKit.Image("Bubble", _tutorialLayer, _pill, 120f, 760f, 840f, 120f, sliced: true);
                UiKit.Text("Text", bubble.transform, _font, text, 44f, DesignTokens.Ink, 20f, 0f, 800f, 112f);
            }
        }

        public void HideTutorial()
        {
            if (_tutorialLayer == null) return;
            for (int i = _tutorialLayer.childCount - 1; i >= 0; i--) Destroy(_tutorialLayer.GetChild(i).gameObject);
        }
    }
}
